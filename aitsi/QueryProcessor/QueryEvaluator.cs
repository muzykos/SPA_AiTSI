using aitsi.Parser;
using ParserTNode = aitsi.Parser.TNode;


namespace aitsi
{
    static class Evaluator
    {
        public static string Evaluate(QueryNode tree, PKB pkb)
        {
            SelectNode select = (SelectNode)tree.getChildByType("Select");
            string selectedVariable = select.variables[0];

            List<string> results = GetAllPossibleValues(selectedVariable, tree, pkb);

            foreach (var clause in select.children.OfType<ClauseNode>())
            {
                results = ApplyClause(results, clause, pkb);
            }

            foreach (var with in select.children.OfType<WithNode>())
            {
                results = ApplyWith(results, with, pkb);
            }

            if (selectedVariable.ToLower() == "boolean")
                return results.Any() ? "true" : "false";

            return string.Join(", ", results);
        }

        private static List<string> GetAllPossibleValues(string variable, QueryNode tree, PKB pkb)
        {
            var entityType = tree.getChildByType("Declaration")?.type ?? "";

            if (entityType == "stmt")
            {
                return pkb.GetStatementTable().Keys.Select(x => x.ToString()).ToList();
            }
            else if (entityType == "variable")
            {
                return pkb.GetVariableTable().Keys.ToList();
            }
            else if (entityType == "constant")
            {
                return pkb.GetConstantTable().Keys.ToList();
            }
            else if (entityType == "procedure")
            {
                return pkb.GetProcedureTable().Keys.ToList();
            }

            return new List<string>();
        }

        private static List<string> ApplyClause(List<string> currentResults, ClauseNode clause, PKB pkb)
        {
            return FilterByClause(currentResults, clause, pkb);
        }

        private static List<string> ApplyWith(List<string> currentResults, WithNode with, PKB pkb)
        {
            return FilterByWith(currentResults, with, pkb);
        }



        private static List<string> FilterByClause(List<string> currentResults, ClauseNode clause, PKB pkb)
        {
            List<string> filteredResults = new();

            var modifiesMap = pkb.GetModifiesMap();
            var usesMap = pkb.GetUsesMap();
            var parentStarMap = pkb.GetParentStarMap();
            var followsStarMap = pkb.GetFollowsStarMap();

            foreach (var result in currentResults)
            {
                if (int.TryParse(result, out int stmtNumber))
                {
                    var statementTable = pkb.GetStatementTable();
                    if (statementTable.TryGetValue(stmtNumber, out ParserTNode stmtNode))
                    {
                        bool satisfies = false;

                        if (clause.relation == "Modifies")
                        {
                            satisfies = modifiesMap.TryGetValue(stmtNode, out var modifiedVars) &&
                                        modifiedVars.Any(v => v.getAttr() == clause.variables[1]);
                        }
                        else if (clause.relation == "Uses")
                        {
                            satisfies = usesMap.TryGetValue(stmtNode, out var usedVars) &&
                                        usedVars.Any(v => v.getAttr() == clause.variables[1]);
                        }
                        else if (clause.relation == "Parent")
                        {
                            satisfies = parentStarMap.TryGetValue(stmtNode, out var parents) &&
                                        parents.Any(p => p.getAttr() == clause.variables[1]);
                        }
                        else if (clause.relation == "Follows")
                        {
                            satisfies = followsStarMap.TryGetValue(stmtNode, out var followers) &&
                                        followers.Any(f => f.getAttr() == clause.variables[1]);
                        }

                        if (satisfies)
                        {
                            filteredResults.Add(result);
                        }
                    }
                }
            }

            return filteredResults;
        }

        private static List<string> FilterByWith(List<string> currentResults, WithNode with, PKB pkb)
        {
            List<string> filteredResults = new();

            foreach (var result in currentResults)
            {
                if (with.variables.Contains(result))
                {
                    filteredResults.Add(result);
                }
            }

            return filteredResults;
        }
    }
}