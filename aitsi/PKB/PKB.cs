using aitsi.Parser;

namespace aitsi.Parser
{
    class PKB
    {
        public static string[][] parentTable;
        public static string[][] followsTable;
        public static string[][] modifiesTable;
        public static string[][] usesTable;
        public static string[][] callsTable;
        public static string[][] varTable;
        public static Proc[] procTable;
        public static TNode programTree;


        private AST ast;  

        private Dictionary<int, TNode> statementTable;  
        private Dictionary<string, List<TNode>> variableTable; 
        private Dictionary<string, List<TNode>> constantTable;
        private Dictionary<string, TNode> procedureTable; 

        private Dictionary<TNode, HashSet<TNode>> modifiesMap;  
        private Dictionary<TNode, HashSet<TNode>> usesMap; 
        private Dictionary<TNode, HashSet<TNode>> parentStarMap;  
        private Dictionary<TNode, HashSet<TNode>> followsStarMap; 

        public PKB(AST ast)
        {
            this.ast = ast;

            statementTable = new Dictionary<int, TNode>();
            variableTable = new Dictionary<string, List<TNode>>();
            constantTable = new Dictionary<string, List<TNode>>();
            procedureTable = new Dictionary<string, TNode>();

            modifiesMap = new Dictionary<TNode, HashSet<TNode>>();
            usesMap = new Dictionary<TNode, HashSet<TNode>>();
            parentStarMap = new Dictionary<TNode, HashSet<TNode>>();
            followsStarMap = new Dictionary<TNode, HashSet<TNode>>();

        }

        public void printPKB()
        {
            Console.WriteLine("statements "+statementTable.Count);
            Console.WriteLine("variables "+variableTable.Count);
            Console.WriteLine("constants " + constantTable.Count);
            Console.WriteLine("procedures " + procedureTable.Count);
            Console.WriteLine("modifies " +  modifiesMap.Count);
            Console.WriteLine("uses " + usesMap.Count);
            Console.WriteLine("parent " + parentStarMap.Count);
            Console.WriteLine("follows " + followsStarMap.Count);
        }

        public void PopulatePKB()
        {
            TNode root = ast.getRoot();
            if (root != null && ast.getType(root) == TType.Procedure)
            {
                string procName = ast.getAttr(root);
                procedureTable[procName] = root;

                ProcessStatements(root, ast.getChildren(root));
            }
        }

        private void ProcessStatements(TNode parent, List<TNode> statements)
        {
            foreach (var stmt in statements)
            {
                int stmtNum = GetStatementNumber(stmt);
                //if (stmtNum > 0)
                //{
                    statementTable[stmtNum] = stmt;
                //}

                switch (ast.getType(stmt))
                {
                    case TType.Assign:
                        ProcessAssign(parent, stmt);
                        break;
                    case TType.While:
                        ProcessWhile(parent, stmt);
                        break;
                }
            }

            ComputeTransitiveClosure();
        }

        private void ProcessAssign(TNode parent, TNode assignNode)
        {
            string varName = ast.getAttr(assignNode);

            AddToModifies(assignNode, varName);
            AddToModifies(parent, varName); 

            List<TNode> children = ast.getChildren(assignNode);
            if (children.Count > 0)
            {
                ProcessExpression(assignNode, parent, children[0]);
            }
        }

        private void ProcessWhile(TNode parent, TNode whileNode)
        {
            string varName = ast.getAttr(whileNode);

            AddToUses(whileNode, varName);
            AddToUses(parent, varName);

            ProcessStatements(whileNode, ast.getChildren(whileNode));
        }

        private void ProcessExpression(TNode stmt, TNode parent, TNode expr)
        {
            if (expr == null) return;

            switch (ast.getType(expr))
            {
                case TType.Variable:
                    string varName = ast.getAttr(expr);
                    AddToUses(stmt, varName);
                    AddToUses(parent, varName);

                    if (!variableTable.ContainsKey(varName))
                    {
                        variableTable[varName] = new List<TNode>();
                    }
                    variableTable[varName].Add(expr);
                    break;

                case TType.Constant:
                    string constValue = ast.getAttr(expr);

                    if (!constantTable.ContainsKey(constValue))
                    {
                        constantTable[constValue] = new List<TNode>();
                    }
                    constantTable[constValue].Add(expr);
                    break;

                case TType.Plus:
                    List<TNode> children = ast.getChildren(expr);
                    if (children.Count >= 2)
                    {
                        ProcessExpression(stmt, parent, children[0]); 
                        ProcessExpression(stmt, parent, children[1]); 
                    }
                    break;
            }
        }

        private int GetStatementNumber(TNode node)
        {
            if (node == null) return -1;

            //TType type = ast.getType(node);
            //if (type == TType.Assign || type == TType.While)
            {
                // Extract statement number based on your implementation
                //return int.Parse(node.getAttr().Split(':')[0]);
            }

            return 1;
        }

        private void AddToModifies(TNode node, string varName)
        {
            if (!modifiesMap.ContainsKey(node))
            {
                modifiesMap[node] = new HashSet<TNode>();
            }

            TNode varNode = GetOrCreateVariableNode(varName);
            modifiesMap[node].Add(varNode);
        }

        private void AddToUses(TNode node, string varName)
        {
            if (!usesMap.ContainsKey(node))
            {
                usesMap[node] = new HashSet<TNode>();
            }

            TNode varNode = GetOrCreateVariableNode(varName);
            usesMap[node].Add(varNode);
        }

        private TNode GetOrCreateVariableNode(string varName)
        {
            if (variableTable.ContainsKey(varName) && variableTable[varName].Count > 0)
            {
                return variableTable[varName][0];
            }

            TNode varNode = ast.createTNode(TType.Variable, varName, -1);

            if (!variableTable.ContainsKey(varName))
            {
                variableTable[varName] = new List<TNode>();
            }
            variableTable[varName].Add(varNode);

            return varNode;
        }

        private void ComputeTransitiveClosure()
        {
            // Parent* relationship
            foreach (var entry in statementTable)
            {
                TNode stmt = entry.Value;
                parentStarMap[stmt] = new HashSet<TNode>();

                // Direct parent
                TNode parent = ast.getParent(stmt);
                if (parent != null && ast.getType(parent) != TType.Procedure)
                {
                    parentStarMap[stmt].Add(parent);

                    // Add all ancestors
                    while ((parent = ast.getParent(parent)) != null && ast.getType(parent) != TType.Procedure)
                    {
                        parentStarMap[stmt].Add(parent);
                    }
                }
            }

            // Follows* relationship
            foreach (var entry in statementTable)
            {
                TNode stmt = entry.Value;
                followsStarMap[stmt] = new HashSet<TNode>();

                // Direct follows
                TNode follows = ast.getFollows(stmt);
                if (follows != null)
                {
                    followsStarMap[stmt].Add(follows);

                    // Add all statements that follow transitively
                    while ((follows = ast.getFollows(follows)) != null)
                    {
                        followsStarMap[stmt].Add(follows);
                    }
                }
            }
        }
    }

 
}