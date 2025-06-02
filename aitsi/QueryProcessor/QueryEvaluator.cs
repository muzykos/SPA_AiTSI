using aitsi.Parser;
using ParserTNode = aitsi.Parser.TNode;
using PKBClass = aitsi.PKB.PKB;

namespace aitsi
{
    static class Evaluator
    {
        //public static string Evaluate(QueryNode tree, PKBClass pkb)
        //{
        //    var selectNode = tree.getChildByType("Select") as SelectNode;
        //    if (selectNode == null)
        //        throw new Exception("Brak w�z�a SELECT w drzewie zapytania.");

        //    var declarations = tree.children.OfType<DeclarationNode>().ToList();
        //    var clauses = selectNode.children.OfType<ClauseNode>().ToList();
        //    var withs = selectNode.children.OfType<WithNode>().ToList();

        //    string selectedVariable = selectNode.variables[0];
        //    Dictionary<string, List<string>> valueCache = new();
        //    Dictionary<string, List<string>> possibleBindings = GetBindings(declarations, pkb, valueCache);

        //    // najpierw boolean bo jak cos znajduje to mozna zakonczyc 
        //    if (selectedVariable.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
        //    {
        //        if (clauses.Count == 0) return "true";

        //        foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
        //        {
        //            var leftVals = GetValuesForVariable(clause.variables[0], declarations, pkb, valueCache);
        //            var rightVals = GetValuesForVariable(clause.variables[1], declarations, pkb, valueCache);

        //            Console.WriteLine($"variables: {clause.variables[1]}, {clause.variables[0]}");

        //        //    Console.WriteLine("wariable ", clause.variables[0], " ", clause.variables[1]);

        //            foreach (var l in leftVals)
        //            {
        //                foreach (var r in rightVals)
        //                {
        //                    Console.WriteLine($"Checking clause: {clause.relation}({r}, {l})");
        //                    if (ClauseSatisfied(clause, l, r, pkb))
        //                    {
        //                        Console.WriteLine("Clause satisfied!");
        //                        return "true";
        //                    }
        //                }
        //            }
        //        }
        //        Console.WriteLine("twoja stara");
        //        return "false";
        //    }

        //    if (!possibleBindings.ContainsKey(selectedVariable))
        //        return "brak wynik�w";

        //    var resultSet = possibleBindings[selectedVariable];

        //    foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
        //    {
        //        string left = clause.variables[0];
        //        string right = clause.variables[1];

        //        //var leftVals = GetValuesForVariable(left, declarations, pkb, valueCache);
        //        //var rightVals = GetValuesForVariable(right, declarations, pkb, valueCache);

        //        //var leftIsVar = declarations.Any(d => d.variables.Contains(clause.variables[0]));
        //        //var rightIsVar = declarations.Any(d => d.variables.Contains(clause.variables[1]));

        //        var leftIsVar = declarations.SelectMany(d => d.variables).Any(v => v.Trim() == left);
        //        var rightIsVar = declarations.SelectMany(d => d.variables).Any(v => v.Trim() == right);


        //        var leftVals = leftIsVar
        //            ? GetValuesForVariable(clause.variables[0], declarations, pkb, valueCache)
        //            : new List<string> { clause.variables[0] };

        //        var rightVals = rightIsVar
        //            ? GetValuesForVariable(clause.variables[1], declarations, pkb, valueCache)
        //            : new List<string> { clause.variables[1] };


        //        if (left == selectedVariable)
        //        {
        //            resultSet = resultSet
        //                .Where(l => rightVals.Any(r => ClauseSatisfied(clause, l, r, pkb)))
        //                .ToList();
        //        }
        //        else if (right == selectedVariable)
        //        {
        //            resultSet = resultSet
        //                .Where(r => leftVals.Any(l => ClauseSatisfied(clause, l, r, pkb)))
        //                .ToList();
        //        }
        //    }

        //    return resultSet.Any() ? string.Join(", ", resultSet.Distinct()) : "brak wynik�w";
        //}

        public static string Evaluate(QueryNode tree, PKBClass pkb)
        {
            var selectNode = tree.getChildByType("Select") as SelectNode;
            if (selectNode == null)
                throw new Exception("Brak w�z�a SELECT w drzewie zapytania.");

            var declarations = tree.children.OfType<DeclarationNode>().ToList();
            var clauses = selectNode.children.OfType<ClauseNode>().ToList();
            var withs = selectNode.children.OfType<WithNode>().ToList();

            var selectedVariables = selectNode.variables;
            Dictionary<string, List<string>> valueCache = new();
            Dictionary<string, List<string>> possibleBindings = GetBindings(declarations, pkb, valueCache);

            // BOOLEAN
            if (selectedVariables.Count == 1 && selectedVariables[0].Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
            {
                if (clauses.Count == 0) return "true";

                foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
                {
                    var leftVals = GetValuesForVariable(clause.variables[0], declarations, pkb, valueCache);
                    var rightVals = GetValuesForVariable(clause.variables[1], declarations, pkb, valueCache);

                    foreach (var l in leftVals)
                        foreach (var r in rightVals)
                            if (ClauseSatisfied(clause, l, r, pkb))
                                return "true";
                }
                return "false";
            }

            // tuple
            if (selectedVariables.Count > 1)
            {
                var valueSets = selectedVariables
                    .ToDictionary(var => var, var => GetValuesForVariable(var, declarations, pkb, valueCache));

                var cartesianProduct = GenerateCombinations(valueSets);

                var validTuples = new List<string>();

                foreach (var tuple in cartesianProduct)
                {
                    bool allSatisfied = true;

                    foreach (var clause in clauses)
                    {
                        var left = tuple.ContainsKey(clause.variables[0]) ? tuple[clause.variables[0]] : clause.variables[0];
                        var right = tuple.ContainsKey(clause.variables[1]) ? tuple[clause.variables[1]] : clause.variables[1];

                        if (!ClauseSatisfied(clause, left, right, pkb))
                        {
                            allSatisfied = false;
                            break;
                        }
                    }

                    if (allSatisfied)
                    {
                        var resultLine = string.Join(" ", selectedVariables.Select(v => tuple[v]));
                        validTuples.Add(resultLine);
                    }
                }

                return validTuples.Any() ? string.Join(", ", validTuples) : "none";
            }

            // zwykle
            string selectedVariable = selectedVariables[0];
            if (!possibleBindings.ContainsKey(selectedVariable))
                return "none";

            var resultSet = possibleBindings[selectedVariable];

            foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
            {
                string left = clause.variables[0];
                string right = clause.variables[1];

                var leftIsVar = declarations.SelectMany(d => d.variables).Any(v => v.Trim() == left);
                var rightIsVar = declarations.SelectMany(d => d.variables).Any(v => v.Trim() == right);

                var leftVals = leftIsVar
                    ? GetValuesForVariable(left, declarations, pkb, valueCache)
                    : new List<string> { left };

                var rightVals = rightIsVar
                    ? GetValuesForVariable(right, declarations, pkb, valueCache)
                    : new List<string> { right };

                if (left == selectedVariable)
                {
                    resultSet = resultSet
                        .Where(l => rightVals.Any(r => ClauseSatisfied(clause, l, r, pkb)))
                        .ToList();
                }
                else if (right == selectedVariable)
                {
                    resultSet = resultSet
                        .Where(r => leftVals.Any(l => ClauseSatisfied(clause, l, r, pkb)))
                        .ToList();
                }
            }

            return resultSet.Any() ? string.Join(", ", resultSet.Distinct()) : "none";
        }


        private static Dictionary<string, List<string>> GetBindings(List<DeclarationNode> declarations, PKBClass pkb, Dictionary<string, List<string>> cache)
        {
            var bindings = new Dictionary<string, List<string>>();

            foreach (var decl in declarations)
            {
                foreach (var variable in decl.variables)
                {
                    if (variable.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
                    {
                        bindings[variable] = new List<string> { "true" };
                    }
                    else
                    {
                        var vals = GetValuesForDeclarationType(decl.type, pkb);
                        bindings[variable] = vals;
                        cache[variable] = vals;
                    }
                }
            }

            return bindings;
        }

        //private static List<string> GetValuesForVariable(string var, List<DeclarationNode> declarations, PKBClass pkb, Dictionary<string, List<string>> cache)
        //{
        //    if (int.TryParse(var, out _) || (var.StartsWith("\"") && var.EndsWith("\"")))
        //        return new List<string> { var };

        //    if (cache.ContainsKey(var))
        //        return cache[var];

        //    var declType = declarations.FirstOrDefault(d => d.variables.Contains(var))?.type;
        //    if (declType == null)
        //        return new List<string>();

        //    var vals = GetValuesForDeclarationType(declType, pkb);
        //    cache[var] = vals;
        //    return vals;
        //}

        //private static List<string> GetValuesForVariable(string var, List<DeclarationNode> declarations, PKBClass pkb, Dictionary<string, List<string>> cache)
        //{
        //    if (int.TryParse(var, out _))
        //        return new List<string> { var };

        //    if (var.StartsWith("\"") && var.EndsWith("\""))
        //        return new List<string> { var.Trim('"') };

        //    if (cache.ContainsKey(var))
        //        return cache[var];

        //    var declType = declarations.FirstOrDefault(d => d.variables.Contains(var))?.type;
        //    if (declType == null)
        //        return new List<string>();

        //    var vals = GetValuesForDeclarationType(declType, pkb);
        //    cache[var] = vals;
        //    return vals;
        //}

        private static List<string> GetValuesForVariable(string var, List<DeclarationNode> declarations, PKBClass pkb, Dictionary<string, List<string>> cache)
        {
            var trimmedVar = var.Trim();

            if (trimmedVar == "_")
            {
                return new List<string>();
            }

            if (int.TryParse(trimmedVar, out _))
                return new List<string> { trimmedVar };

            if (trimmedVar.StartsWith("\"") && trimmedVar.EndsWith("\""))
                return new List<string> { trimmedVar.Trim('"') };

            //if (declarations.SelectMany(d => d.variables).Any(v => v.Trim() == trimmedVar))
            //{
            //    if (cache.ContainsKey(trimmedVar))
            //        return cache[trimmedVar];
            //}

            var declType = declarations
                .FirstOrDefault(d => d.variables.Any(v => v.Trim() == trimmedVar))
                ?.type;

            if (declType == null)
                return new List<string>();

            var vals = GetValuesForDeclarationType(declType, pkb);
            cache[trimmedVar] = vals;

            //Console.WriteLine($"[DEBUG] Values for variable '{trimmedVar}': {string.Join(", ", vals)}");
            return vals;
        }


        private static List<string> GetValuesForDeclarationType(string declType, PKBClass pkb)
        {
            return declType switch
            {
                "stmt" or "prog_line" => pkb.GetStatements().Select(x => x.ToString()).ToList(),
                "assign" => pkb.GetAssignStmts().Select(x => x.ToString()).ToList(),
                "while" => pkb.GetWhileStmts().Select(x => x.ToString()).ToList(),
                "if" => pkb.GetIfStmts().Select(x => x.ToString()).ToList(),
                "variable" => pkb.GetVariables(),
                "constant" => pkb.GetConstants(),
                "procedure" => pkb.GetProcedures(),
                _ => new List<string>()
            };
        }

        private static bool ClauseSatisfied(ClauseNode clause, string leftVal, string rightVal, PKBClass pkb)
        {
            string rel = clause.relation.ToLower();
            string l = leftVal.Trim('"');
            string r = rightVal.Trim('"');

            bool IsInt(string s) => int.TryParse(s, out _);
            bool leftIsUnderscore = l == "_";
            bool rightIsUnderscore = r == "_";

            int li = IsInt(l) ? int.Parse(l) : -1;
            int ri = IsInt(r) ? int.Parse(r) : -1;

            //console.writeline($"checking clause: {clause.relation}({leftval}, {rightval})");


            switch (rel)
            {
                case "modifies":
                    if (leftIsUnderscore && !rightIsUnderscore)
                        return pkb.GetStmtModifies(r).Count > 0 || pkb.GetProcModifies(r).Count > 0;

                    if (!leftIsUnderscore && rightIsUnderscore)
                        return IsInt(l) ? pkb.GetModifiesStmt(li).Count > 0 : pkb.GetModifiesProc(l).Count > 0;

                    return IsInt(l) ? pkb.StmtModifies(li, r) : pkb.ProcModifies(l, r);

                case "uses":
                    if (leftIsUnderscore && !rightIsUnderscore)
                        return pkb.GetStmtUses(r).Count > 0 || pkb.GetProcUses(r).Count > 0;

                    if (!leftIsUnderscore && rightIsUnderscore)
                        return IsInt(l) ? pkb.GetUsesStmt(li).Count > 0 : pkb.GetUsesProc(l).Count > 0;

                    return IsInt(l) ? pkb.StmtUses(li, r) : pkb.ProcUses(l, r);


                case "calls":
                    if (leftIsUnderscore && !rightIsUnderscore)
                        return pkb.GetCalledBy(r).Count > 0;

                    if (!leftIsUnderscore && rightIsUnderscore)
                        return pkb.GetCalls(l).Count > 0;

                    return pkb.Calls(l, r);

                case "calls*":
                    if (leftIsUnderscore && !rightIsUnderscore)
                        return pkb.GetCalledByStar(r).Count > 0;

                    if (!leftIsUnderscore && rightIsUnderscore)
                        return pkb.GetCallsStar(l).Count > 0;

                    return pkb.CallsStar(l, r);


                case "follows":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.GetFollows(li) != -1 || pkb.GetFollowedBy(ri) != -1;

                    return pkb.Follows(li, ri);

                case "follows*":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.GetFollowsStar(li).Count > 0 || pkb.GetFollowedByStar(ri).Count > 0;

                    return pkb.FollowsStar(li, ri);

                case "parent":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.Parent(li, ri) || pkb.GetChildren(li).Count > 0 || pkb.GetParent(ri) != -1;

                    return pkb.Parent(li, ri);

                case "parent*":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.ParentStar(li, ri) || pkb.GetChildrenStar(li).Count > 0 || pkb.GetParentStar(ri).Count > 0;

                    return pkb.ParentStar(li, ri);

                default:
                    return false;

                //    "uses" => leftIsUnderscore || rightIsUnderscore ? pkb.AnyUses(l, r) : IsInt(l) ? pkb.StmtUses(li, r) : pkb.ProcUses(l, r),
                //"parent" => IsInt(l) && IsInt(r) && pkb.Parent(li, ri),
                //"parent*" => IsInt(l) && IsInt(r) && pkb.ParentStar(li, ri),
                //"follows" => IsInt(l) && IsInt(r) && pkb.Follows(li, ri),
                //"follows*" => IsInt(l) && IsInt(r) && pkb.FollowsStar(li, ri),
                //"calls" => leftIsUnderscore || rightIsUnderscore ? pkb.AnyCalls(l, r) : pkb.Calls(l, r),
                //"calls*" => leftIsUnderscore || rightIsUnderscore ? pkb.AnyCallsStar(l, r) : pkb.CallsStar(l, r),
                //_ => false
            }
        }

        //sortowanie po koszciw
        private static int EstimateClauseCost(ClauseNode clause, Dictionary<string, List<string>> cache)
        {
            int leftSize = cache.ContainsKey(clause.variables[0]) ? cache[clause.variables[0]].Count : 1000;
            int rightSize = cache.ContainsKey(clause.variables[1]) ? cache[clause.variables[1]].Count : 1000;
            return leftSize * rightSize; //liczba kombinacji - im wiecej tym mniej oplacalne sprawdzanie
        }

        private static List<Dictionary<string, string>> GenerateCombinations(Dictionary<string, List<string>> variableValues)
        {
            var result = new List<Dictionary<string, string>>();

            void Recurse(Dictionary<string, string> current, List<string> keysLeft)
            {
                if (!keysLeft.Any())
                {
                    result.Add(new Dictionary<string, string>(current));
                    return;
                }

                var key = keysLeft[0];
                foreach (var val in variableValues[key])
                {
                    current[key] = val;
                    Recurse(current, keysLeft.Skip(1).ToList());
                }
            }

            Recurse(new Dictionary<string, string>(), variableValues.Keys.ToList());
            return result;
        }


    }
}