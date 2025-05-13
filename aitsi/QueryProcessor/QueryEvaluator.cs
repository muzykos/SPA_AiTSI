using aitsi.Parser;
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
                throw new Exception("Brak wêz³a SELECT w drzewie zapytania.");

            var declarations = tree.children.OfType<DeclarationNode>().ToList();
            var clauses = selectNode.children.OfType<ClauseNode>().ToList();
            var withs = selectNode.children.OfType<WithNode>().ToList();

            string selectedVariable = selectNode.variables[0];
            Dictionary<string, List<string>> possibleBindings = GetBindings(declarations, pkb);

            if (selectedVariable.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
            {
                if (clauses.Count == 0) return "true";

                foreach (var clause in clauses)
                {
                    var left = clause.variables[0];
                    var right = clause.variables[1];

                    var leftVals = GetValuesForVariable(left, declarations, pkb);
                    var rightVals = GetValuesForVariable(right, declarations, pkb);

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

            if (!possibleBindings.ContainsKey(selectedVariable))
                return "brak wyników";

            var resultSet = possibleBindings[selectedVariable];

            foreach (var clause in clauses)
            {
                var left = clause.variables[0];
                var right = clause.variables[1];

                var leftVals = GetValuesForVariable(left, declarations, pkb);
                var rightVals = GetValuesForVariable(right, declarations, pkb);

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
                else
                {
                    // nie dotyczy selectedVariable 
                }
            }
                return resultSet.Any() ? string.Join(", ", resultSet.Distinct()) : "brak wyników";
        }

        private static Dictionary<string, List<string>> GetBindings(List<DeclarationNode> declarations, PKBClass pkb)
        {
            var bindings = new Dictionary<string, List<string>>();

            foreach (var decl in declarations)
            {
                foreach (var variable in decl.variables)
                {
                    Console.WriteLine($"Selected variable: '{variable}'");

                    if (variable.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
                    {
                        bindings[variable] = new List<string> { "true" };
                    }
                    else
                    {
                        bindings[variable] = decl.type switch
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
                        //Console.WriteLine($"typ: {decl.type}");
                        //Console.WriteLine($"Bindings for {variable}: {string.Join(", ", bindings[variable])}");
                    }
                }
            }

            return bindings;
        }


        private static bool ClauseSatisfied(ClauseNode clause, string leftVal, string rightVal, PKBClass pkb)
        {
            string rel = clause.relation.ToLower();
            string l = leftVal.Trim('"');
            string r = rightVal.Trim('"');

            bool IsInt(string s) => int.TryParse(s, out _);
            int li = IsInt(l) ? int.Parse(l) : -1;
            int ri = IsInt(r) ? int.Parse(r) : -1;


            return rel switch
            {
                "modifies" => IsInt(l)
                    ? pkb.StmtModifies(li, r)
                    : pkb.ProcModifies(l, r),

                "uses" => IsInt(l)
                    ? pkb.StmtUses(li, r)
                    : pkb.ProcUses(l, r),

                "parent" => IsInt(l) && IsInt(r) && pkb.Parent(li, ri),
                "parent*" => IsInt(l) && IsInt(r) && pkb.ParentStar(li, ri),

                "follows" => IsInt(l) && IsInt(r) && pkb.Follows(li, ri),
                "follows*" => IsInt(l) && IsInt(r) && pkb.FollowsStar(li, ri),

                "calls" => pkb.Calls(l, r),
                "calls*" => pkb.CallsStar(l, r),

                _ => false
            };
        }

        private static List<string> GetValuesForVariable(string var, List<DeclarationNode> declarations, PKBClass pkb)
        {
            if (int.TryParse(var, out _)) return new List<string> { var };
            if (var.StartsWith("\"") && var.EndsWith("\"")) return new List<string> { var };

            //  declarations.Select(d => $"{d.type}: {string.Join(", ", d.variables)}");


            var declType = declarations.FirstOrDefault(d => d.variables.Contains(var))?.type;
            if (declType == null) return new List<string>();

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


        //private static bool IsVariable(string s)
        //{
        //    if (string.IsNullOrEmpty(s)) return false;
        //    if (s.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase)) return false;
        //    if (int.TryParse(s, out _)) return false;
        //    if (s.StartsWith("\"") && s.EndsWith("\"")) return false;
        //    return true;
        //}

    }
}
