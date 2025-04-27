//using aitsi.Parser;

//namespace aitsi
//{
//    static class QueryEvaluator
//    {
//        private static AST ast;

//        public static void SetAST(AST astTree)
//        {
//            ast = astTree;
//        }

//        public static bool EvaluateFollows(int stmt1, int stmt2)
//        {
//            if (ast == null) throw new Exception("AST nie zosta³ ustawiony.");
//            aitsi.Parser.TNode root = ast.getRoot();
//            return FindFollows(root, stmt1, stmt2);
//        }

//        public static bool EvaluateParent(int stmt1, int stmt2)
//        {
//            if (ast == null) throw new Exception("AST nie zosta³ ustawiony.");
//            aitsi.Parser.TNode root = ast.getRoot();
//            return FindParent(root, stmt1, stmt2);
//        }

//        private static bool FindFollows(aitsi.Parser.TNode node, int stmt1, int stmt2)
//        {
//            if (node == null) return false;

//            if (node.getAttr() == stmt1.ToString())
//            {
//                var follows = node.getFollows();
//                if (follows != null && follows.getAttr() == stmt2.ToString())
//                    return true;
//            }

//            foreach (var child in node.getChildren())
//            {
//                if (FindFollows(child, stmt1, stmt2))
//                    return true;
//            }

//            return false;
//        }

//        private static bool FindParent(aitsi.Parser.TNode node, int stmt1, int stmt2)
//        {
//            if (node == null) return false;

//            if (node.getAttr() == stmt1.ToString())
//            {
//                foreach (var child in node.getChildren())
//                {
//                    if (child.getAttr() == stmt2.ToString())
//                        return true;
//                }
//            }

//            foreach (var child in node.getChildren())
//            {
//                if (FindParent(child, stmt1, stmt2))
//                    return true;
//            }

//            return false;
//        }
//    }
//}

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
            //wszystkie mozliwe wartosci dla zmiennej
            return pkb.GetEntitiesForSynonym(variable, tree);
        }

        private static List<string> ApplyClause(List<string> currentResults, ClauseNode clause, PKB pkb)
        {
            // TODO: przefiltruj currentResults na podstawie relacji w PKB
            return pkb.FilterByClause(currentResults, clause);
        }

        private static List<string> ApplyWith(List<string> currentResults, WithNode with, PKB pkb)
        {
            // TODO: przefiltruj currentResults na podstawie porównania z with
            return pkb.FilterByWith(currentResults, with);
        }
    }
}
