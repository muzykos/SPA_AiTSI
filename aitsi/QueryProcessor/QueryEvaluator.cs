using aitsi.Parser;

namespace aitsi
{
    static class QueryEvaluator
    {
        private static AST ast;

        public static void SetAST(AST astTree)
        {
            ast = astTree;
        }

        public static bool EvaluateFollows(int stmt1, int stmt2)
        {
            if (ast == null) throw new Exception("AST nie zosta³ ustawiony.");
            aitsi.Parser.TNode root = ast.getRoot();
            return FindFollows(root, stmt1, stmt2);
        }

        public static bool EvaluateParent(int stmt1, int stmt2)
        {
            if (ast == null) throw new Exception("AST nie zosta³ ustawiony.");
            aitsi.Parser.TNode root = ast.getRoot();
            return FindParent(root, stmt1, stmt2);
        }

        private static bool FindFollows(aitsi.Parser.TNode node, int stmt1, int stmt2)
        {
            if (node == null) return false;

            if (node.getAttr() == stmt1.ToString())
            {
                var follows = node.getFollows();
                if (follows != null && follows.getAttr() == stmt2.ToString())
                    return true;
            }

            foreach (var child in node.getChildren())
            {
                if (FindFollows(child, stmt1, stmt2))
                    return true;
            }

            return false;
        }

        private static bool FindParent(aitsi.Parser.TNode node, int stmt1, int stmt2)
        {
            if (node == null) return false;

            if (node.getAttr() == stmt1.ToString())
            {
                foreach (var child in node.getChildren())
                {
                    if (child.getAttr() == stmt2.ToString())
                        return true;
                }
            }

            foreach (var child in node.getChildren())
            {
                if (FindParent(child, stmt1, stmt2))
                    return true;
            }

            return false;
        }
    }
}
