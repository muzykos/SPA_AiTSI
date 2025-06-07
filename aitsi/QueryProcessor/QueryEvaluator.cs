using aitsi.Parser;
using System;
using ParserTNode = aitsi.Parser.TNode;
using PKBClass = aitsi.PKB.PKB;

namespace aitsi
{
    static class Evaluator
    {
        public static string Evaluate(QueryNode tree, PKBClass pkb)
        {
            var selectNode = tree.getChildByType("Select") as SelectNode;
            if (selectNode == null)
                throw new Exception("Brak węzła SELECT w drzewie zapytania.");

            var declarations = tree.children.OfType<DeclarationNode>().ToList();
            var clauses = selectNode.children.OfType<ClauseNode>().ToList();
            var withs = selectNode.children.OfType<WithNode>().ToList();
            var patterns = selectNode.children.OfType<PatternNode>().ToList();

            var selectedVariables = selectNode.variables;
            Dictionary<string, List<string>> valueCache = new();
            Dictionary<string, List<string>> possibleBindings = GetBindings(declarations, pkb, valueCache);
            Dictionary<string, HashSet<Dictionary<string, string>>> filteredCombosByVar = new();


            var withVars = withs
                .SelectMany(w => w.variables)
                .Select(v => v.Contains('.') ? v.Split('.')[0] : v)
                .Distinct()
                .Where(v => possibleBindings.ContainsKey(v))
                .ToList();

            if (withVars.Count > 0)
            {

                var combos = GenerateCombinations(
                    withVars.ToDictionary(v => v, v => possibleBindings[v])
                );

                var validCombos = combos
                    .Where(c => withs.All(w => WithSatisfied(w, c)))
                    .ToList();

                foreach (var var in withVars)
                {
                    var newVals = validCombos
                        .Where(c => c.ContainsKey(var))
                        .Select(c => c[var])
                        .Distinct()
                        .ToList();

                    possibleBindings[var] = newVals;
                    valueCache[var] = newVals;
                }
            }


            //pattern
            var assignPatterns = patterns.Where(p => declarations.Any(d => d.type == "assign" && d.variables.Contains(p.variables[0]))).ToList();
            var whilePatterns = patterns.Where(p => declarations.Any(d => d.type == "while" && d.variables.Contains(p.variables[0]))).ToList();
            var ifPatterns = patterns.Where(p => declarations.Any(d => d.type == "if" && d.variables.Contains(p.variables[0]))).ToList();


            void FilterPatternBindings(
                List<PatternNode> patternGroup,
                Func<PatternNode, string, string, PKBClass, List<int>> satisfyFunc)
            {
                var patternVars = patternGroup
                    .SelectMany(p => p.variables)
                    .Distinct()
                    .Where(v => possibleBindings.ContainsKey(v))
                    .ToList();

                if (!patternVars.Any()) return;

                var combos = GenerateCombinations(
                    patternVars.ToDictionary(v => v, v => possibleBindings[v])
                );

                var validCombos = combos
                    .Where(c =>
                    {
                        foreach (var pattern in patternGroup)
                        {
                            if (!c.ContainsKey(pattern.variables[0]) || !c.ContainsKey(pattern.variables[1]))
                                return false;

                            var stmt = c[pattern.variables[0]];
                            var varName = c[pattern.variables[1]];

                            var matches = satisfyFunc(pattern, stmt, varName, pkb);
                            if (!matches.Contains(int.Parse(stmt)))
                                return false;
                        }
                        return true;
                    })
                    .ToList();


                foreach (var var in patternVars)
                {
                    var newVals = validCombos
                        .Where(c => c.ContainsKey(var))
                        .Select(c => c[var])
                        .Distinct()
                        .ToList();

                    possibleBindings[var] = newVals;
                    valueCache[var] = newVals;

                    if (!filteredCombosByVar.ContainsKey(var))
                        filteredCombosByVar[var] = new();

                    foreach (var combo in validCombos.Where(c => c.ContainsKey(var)))
                        filteredCombosByVar[var].Add(combo);
                }
            }


            List<int> PatternAssignSatisfied(PatternNode pattern, string stmt, string varName, PKBClass pkb)
            {
              //  Console.WriteLine("PatternAssignSatisfied - varName: " + varName);
                List<int> results = pkb.GetAssignStmts(varName);
                return results;
            }

            List<int> PatternWhileSatisfied(PatternNode pattern, string stmt, string varName, PKBClass pkb)
            {
              //  Console.WriteLine("PatternWhileSatisfied - varName: " + varName);
                List<int> results = pkb.GetWhileStmts(varName);
                return results;
            }

            List<int> PatternIfSatisfied(PatternNode pattern, string stmt, string varName, PKBClass pkb)
            {
              //  Console.WriteLine("PatternIfSatisfied - varName: " + varName);
                return pkb.GetIfStmts(varName);
            }

            FilterPatternBindings(assignPatterns, PatternAssignSatisfied);
            FilterPatternBindings(whilePatterns, PatternWhileSatisfied);
            FilterPatternBindings(ifPatterns, PatternIfSatisfied);

            //koniec patterns

            bool IsSymmetricRelation(string rel)
            {
                rel = rel.ToLower();
                return rel == "follows" || rel == "follows*" || rel == "parent" || rel == "parent*" || rel == "next" || rel == "next*";
            }

            // BOOLEAN
            if (selectedVariables.Count == 1 && selectedVariables[0].Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
            {
                if (clauses.Count == 0 && withs.Count == 0) return "true";

                foreach (var clause in clauses)
                {
                    string rel = clause.relation.ToLower();
                    string leftRaw = clause.variables[0];
                    string rightRaw = clause.variables[1];

                    bool leftIsUnderscore = leftRaw == "_";
                    bool rightIsUnderscore = rightRaw == "_";

                    if (IsSymmetricRelation(rel) && leftRaw == rightRaw && !leftIsUnderscore)
                        return "false";
                }

                if (filteredCombosByVar.Values.Any(set => set.Any()))
                    return "true";

                foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
                {
                    string leftRaw = clause.variables[0];
                    string rightRaw = clause.variables[1];

                    bool leftIsUnderscore = leftRaw == "_";
                    bool rightIsUnderscore = rightRaw == "_";

                    var leftVals = leftIsUnderscore
                        ? new List<string> { "_" }
                        : GetValuesForVariable(leftRaw, declarations, pkb, valueCache);

                    var rightVals = rightIsUnderscore
                        ? new List<string> { "_" }
                        : GetValuesForVariable(rightRaw, declarations, pkb, valueCache);

                    foreach (var l in leftVals)
                    {
                        foreach (var r in rightVals)
                        {
                            if (ClauseSatisfied(clause, l, r, pkb))
                                return "true";
                        }
                    }
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

                        if ((clause.variables[0] == clause.variables[1]) && left == right && IsSymmetricRelation(clause.relation))
                        {
                            allSatisfied = false;
                            break;
                        }

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

            foreach (var clause in clauses)
            {
                if (IsSymmetricRelation(clause.relation)
                    && clause.variables[0] == clause.variables[1]
                    && clause.variables[0] != "_")
                {
                    return "none";
                }
            }

            var resultSet = filteredCombosByVar.ContainsKey(selectedVariable)
                 ? filteredCombosByVar[selectedVariable].Select(c => c[selectedVariable]).Distinct().ToList()
                 : possibleBindings[selectedVariable];

            if (!resultSet.Any())
                return "none";

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
                    if (right == "_")
                    {
                        resultSet = resultSet
                            .Where(l => ClauseSatisfied(clause, l, "_", pkb))
                            .ToList();
                    }
                    else
                    {
                        resultSet = resultSet
                            .Where(l => rightVals.Any(r => ClauseSatisfied(clause, l, r, pkb)))
                            .ToList();
                    }
                }

                else if (right == selectedVariable)
                {
                    if (left == "_")
                    {
                        resultSet = resultSet
                            .Where(r => ClauseSatisfied(clause, "_", r, pkb))
                            .ToList();
                    }
                    else
                    {
                        resultSet = resultSet
                            .Where(r => leftVals.Any(l => ClauseSatisfied(clause, l, r, pkb)))
                            .ToList();
                    }
                }
                if (!resultSet.Any())
                    return "none";
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

        private static List<string> GetValuesForVariable(string var, List<DeclarationNode> declarations, PKBClass pkb, Dictionary<string, List<string>> cache)
        {
            var trimmedVar = var.Trim();

            if (trimmedVar == "_") return new List<string>();
            if (int.TryParse(trimmedVar, out _)) return new List<string> { trimmedVar };
            if (trimmedVar.StartsWith("\"") && trimmedVar.EndsWith("\"")) return new List<string> { trimmedVar.Trim('"') };

            if (cache.ContainsKey(trimmedVar))
                return cache[trimmedVar];

            var declType = declarations
                .FirstOrDefault(d => d.variables.Any(v => v.Trim() == trimmedVar))
                ?.type;

            if (declType == null) return new List<string>();

            var vals = GetValuesForDeclarationType(declType, pkb);
            cache[trimmedVar] = vals;
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
                "call" => pkb.GetCallStmts().Select(x => x.ToString()).ToList(),
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
            //Console.WriteLine("Czy lewy to podloga:");
            //Console.WriteLine(leftIsUnderscore);
            //Console.WriteLine("lewa:");
            //Console.WriteLine(li);
            //Console.WriteLine("prawa:");
            //Console.WriteLine(ri);

            if ((l == r) && (!leftIsUnderscore) && (rel == "follows" || rel == "follows*" || rel == "parent" || rel == "parent*" || rel == "next" || rel == "calls" || rel == "calls*"))
                return false;

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
                    if (!leftIsUnderscore && !rightIsUnderscore)
                    {
                        return IsInt(l) && IsInt(r) && pkb.Follows(li, ri);
                    }
                    else if (!leftIsUnderscore && rightIsUnderscore)
                    {
                        return IsInt(l) && pkb.GetFollows(li) != -1;
                    }
                    else if (leftIsUnderscore && !rightIsUnderscore) //błędy z Follows zawsze w tym przypadku
                    {
                        return IsInt(r) && pkb.GetFollowedBy(ri) != 0;
                    }
                    else
                    {
                        // Oba są "_"
                        return pkb.GetFollowsMap().Count > 0;
                    }



                case "follows*":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.GetFollowsStar(li).Count > 0 || pkb.GetFollowedByStar(ri).Count > 0;

                    return IsInt(l) && IsInt(r) && pkb.FollowsStar(li, ri);

                case "parent":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.Parent(li, ri) || pkb.GetChildren(li).Count > 0 || pkb.GetParent(ri) != -1;

                    return IsInt(l) && IsInt(r) && pkb.Parent(li, ri);

                case "parent*":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.ParentStar(li, ri) || pkb.GetChildrenStar(li).Count > 0 || pkb.GetParentStar(ri).Count > 0;

                    return IsInt(l) && IsInt(r) && pkb.ParentStar(li, ri);

                case "next":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.GetNext(li).Count > 0 || pkb.GetPrevious(ri).Count > 0;

                    return IsInt(l) && IsInt(r) && pkb.GetNext(li).Contains(ri);

                case "next*":
                    if (leftIsUnderscore || rightIsUnderscore)
                        return pkb.GetNextStar(li).Count > 0 || pkb.GetPreviousStar(ri).Count > 0;

                    return IsInt(l) && IsInt(r) && pkb.GetNextStar(li).Contains(ri);


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

        private static bool WithSatisfied(WithNode with, Dictionary<string, string> tuple)
        {
            string left = with.variables[0];
            string right = with.variables[1];

            string leftVal = GetWithValue(left, tuple);
            string rightVal = GetWithValue(right, tuple);

            return leftVal == rightVal;
        }

        //private static bool PatternSatisfied(PatternNode pattern, string stmt, PKBClass pkb)
        //{
        //    if (!int.TryParse(stmt, out int stmtNum))
        //        return false;

        //    var type = pkb.GetStatementType(stmtNum);
        //    string expectedVar = pattern.variables[0].Trim('"');

        //    return type switch
        //    {
        //        TType.Assign => pkb.GetModifiesStmt(stmtNum).Contains(expectedVar),
        //        TType.While => pkb.GetUsesStmt(stmtNum).Contains(expectedVar),
        //        TType.If => pkb.GetUsesStmt(stmtNum).Contains(expectedVar),
        //        _ => false
        //    };
        //}

        private static bool PatternSatisfied(PatternNode pattern, string stmt, PKBClass pkb)
        {
            if (!int.TryParse(stmt, out int stmtNum))
                return false;

            var stmtType = pkb.GetStatementType(stmtNum);
            var varName = pattern.variables[1].Trim('"');

            return stmtType switch
            {
                TType.Assign => pkb.GetModifiesStmt(stmtNum).Contains(varName),
                TType.While => pkb.GetUsesStmt(stmtNum).Contains(varName),
                TType.If => pkb.GetUsesStmt(stmtNum).Contains(varName),
                _ => false
            };
        }


        private static string GetWithValue(string input, Dictionary<string, string> tuple)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
                return input.Trim('"');

            if (int.TryParse(input, out _))
                return input;

            if (input.Contains("."))
            {
                var parts = input.Split('.');
                string var = parts[0];
                string attr = parts[1];

                if (!tuple.ContainsKey(var)) return "";

                string val = tuple[var];

                return attr switch
                {
                    "procName" => val,
                    "varName" => val,
                    "value" => val,
                    "stmt#" => val,
                    "stmtNum" => val,
                    _ => ""
                };
            }

            return tuple.ContainsKey(input) ? tuple[input] : "";
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