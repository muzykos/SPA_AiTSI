//using aitsi.Parser;
//using ParserTNode = aitsi.Parser.TNode;


//namespace aitsi
//{
//    static class Evaluator
//    {
//        //public static string Evaluate(QueryNode tree, PKB pkb)
//        //{
//        //    SelectNode select = (SelectNode)tree.getChildByType("Select");
//        //    string selectedVariable = select.variables[0];

//        //    List<string> results = GetAllPossibleValues(selectedVariable, tree, pkb);

//        //    foreach (var clause in select.children.OfType<ClauseNode>())
//        //    {
//        //        results = ApplyClause(results, clause, pkb);
//        //    }

//        //    foreach (var with in select.children.OfType<WithNode>())
//        //    {
//        //        results = ApplyWith(results, with, pkb);
//        //    }

//        //    if (selectedVariable.ToLower() == "boolean")
//        //        return results.Any() ? "true" : "false";

//        //    return string.Join(", ", results);
//        //}

//        public static string Evaluate(QueryNode tree, PKB pkb)
//        {
//            var select = (SelectNode)tree.getChildByType("Select");
//            var declarations = tree.children.OfType<DeclarationNode>().ToList();

//            var clauses = select.children.OfType<ClauseNode>().ToList();
//            var withs = select.children.OfType<WithNode>().ToList();

//            string selectedVariable = select.variables[0];
//            List<string> resultSet = GetValuesForSelect(selectedVariable, declarations, pkb);

//            foreach (var clause in clauses)
//            {
//                resultSet = resultSet
//                    .Where(res => ClauseSatisfied(clause, res, selectedVariable, pkb))
//                    .ToList();
//            }

//            foreach (var with in withs)
//            {
//                resultSet = resultSet
//                    .Where(res => WithSatisfied(with, res))
//                    .ToList();
//            }

//            if (selectedVariable.ToLower() == "boolean")
//                return resultSet.Any() ? "true" : "false";

//            return string.Join(", ", resultSet.Distinct());
//        }


//        private static List<string> GetValuesForSelect(string var, List<DeclarationNode> declarations, PKB pkb)
//        {
//            foreach (var decl in declarations)
//            {
//                if (decl.variables.Contains(var))
//                {
//                    switch (decl.type)
//                    {
//                        case "stmt": return pkb.GetStatements().Select(x => x.ToString()).ToList();
//                        case "assign": return pkb.GetAssignStmts().Select(x => x.ToString()).ToList();
//                        case "while": return pkb.GetWhileStmts().Select(x => x.ToString()).ToList();
//                        case "if": return pkb.GetIfStmts().Select(x => x.ToString()).ToList();
//                        case "variable": return pkb.GetVariables();
//                        case "constant": return pkb.GetConstants();
//                        case "procedure": return pkb.GetProcedures();
//                    }
//                }
//            }

//            return new List<string>();
//        }

//        private static bool ClauseSatisfied(ClauseNode clause, string currentVal, string selectedVariable, PKB pkb)
//        {
//            string left = clause.variables[0];
//            string right = clause.variables[1];
//            string rel = clause.relation.ToLower();

//            string actualLeft = left == selectedVariable ? currentVal : left;
//            string actualRight = right == selectedVariable ? currentVal : right;

//            bool IsStmt(string s) => int.TryParse(s, out _);

//            switch (rel)
//            {
//                case "modifies":
//                    if (IsStmt(actualLeft)) return pkb.StmtModifies(int.Parse(actualLeft), actualRight);
//                    else return pkb.ProcModifies(actualLeft, actualRight);

//                case "uses":
//                    if (IsStmt(actualLeft)) return pkb.StmtUses(int.Parse(actualLeft), actualRight);
//                    else return pkb.ProcUses(actualLeft, actualRight);

//                case "parent":
//                    return IsStmt(actualLeft) && IsStmt(actualRight) && pkb.Parent(int.Parse(actualLeft), int.Parse(actualRight));

//                case "parent*":
//                    return IsStmt(actualLeft) && IsStmt(actualRight) && pkb.ParentStar(int.Parse(actualLeft), int.Parse(actualRight));

//                case "follows":
//                    return IsStmt(actualLeft) && IsStmt(actualRight) && pkb.Follows(int.Parse(actualLeft), int.Parse(actualRight));

//                case "follows*":
//                    return IsStmt(actualLeft) && IsStmt(actualRight) && pkb.FollowsStar(int.Parse(actualLeft), int.Parse(actualRight));

//                case "calls":
//                    return pkb.Calls(actualLeft, actualRight);

//                case "calls*":
//                    return pkb.CallsStar(actualLeft, actualRight);

//                default:
//                    return false;
//            }
//        }

//        private static bool WithSatisfied(WithNode with, string currentVal)
//        {
//            return with.variables.Contains(currentVal);
//        }
//    }
//}