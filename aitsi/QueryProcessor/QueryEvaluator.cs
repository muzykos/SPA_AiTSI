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
            Dictionary<string, List<string>> valueCache = new();
            Dictionary<string, List<string>> possibleBindings = GetBindings(declarations, pkb, valueCache);

            // najpierw boolean bo jak cos znajduje to mozna zakonczyc 
            if (selectedVariable.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
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

            if (!possibleBindings.ContainsKey(selectedVariable))
                return "brak wyników";

            var resultSet = possibleBindings[selectedVariable];

            foreach (var clause in clauses.OrderBy(c => EstimateClauseCost(c, valueCache)))
            {
                string left = clause.variables[0];
                string right = clause.variables[1];

                var leftVals = GetValuesForVariable(left, declarations, pkb, valueCache);
                var rightVals = GetValuesForVariable(right, declarations, pkb, valueCache);

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

            return resultSet.Any() ? string.Join(", ", resultSet.Distinct()) : "brak wyników";
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
            if (int.TryParse(var, out _) || (var.StartsWith("\"") && var.EndsWith("\"")))
                return new List<string> { var };

            if (cache.ContainsKey(var))
                return cache[var];

            var declType = declarations.FirstOrDefault(d => d.variables.Contains(var))?.type;
            if (declType == null)
                return new List<string>();

            var vals = GetValuesForDeclarationType(declType, pkb);
            cache[var] = vals;
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
            int li = IsInt(l) ? int.Parse(l) : -1;
            int ri = IsInt(r) ? int.Parse(r) : -1;

            return rel switch
            {
                "modifies" => IsInt(l) ? pkb.StmtModifies(li, r) : pkb.ProcModifies(l, r),
                "uses" => IsInt(l) ? pkb.StmtUses(li, r) : pkb.ProcUses(l, r),
                "parent" => IsInt(l) && IsInt(r) && pkb.Parent(li, ri),
                "parent*" => IsInt(l) && IsInt(r) && pkb.ParentStar(li, ri),
                "follows" => IsInt(l) && IsInt(r) && pkb.Follows(li, ri),
                "follows*" => IsInt(l) && IsInt(r) && pkb.FollowsStar(li, ri),
                "calls" => pkb.Calls(l, r),
                "calls*" => pkb.CallsStar(l, r),
                _ => false
            };
        }

        //sortowanie po koszciw
        private static int EstimateClauseCost(ClauseNode clause, Dictionary<string, List<string>> cache)
        {
            int leftSize = cache.ContainsKey(clause.variables[0]) ? cache[clause.variables[0]].Count : 1000;
            int rightSize = cache.ContainsKey(clause.variables[1]) ? cache[clause.variables[1]].Count : 1000;
            return leftSize * rightSize;
        }
    }
}
