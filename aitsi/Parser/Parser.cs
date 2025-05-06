namespace aitsi.Parser
{
    class Parser
    {
        private Lexer lexer;
        private TNode currentNode;
        private AST ast;
        private int stmtNumber = 1;

        public Parser(Lexer lexer, AST ast)
        {
            this.lexer = lexer;
            currentNode = lexer.getNextNode();
            this.ast = ast;
        }

        private void parse(TType type)
        {
            Console.WriteLine($"Token: {currentNode.getType()} '{currentNode.getAttr()}'");
            if (currentNode.getType() == type)
                currentNode = lexer.getNextNode();
            else
                throw new Exception($"Expected {type}, got {currentNode.getType()}");
        }

        public void parseProcedure()
        {
            parse(TType.Procedure);
            string name = currentNode.getAttr();

            parse(TType.Name);
            TNode procNode = ast.createTNode(TType.Procedure, name, -1);
            ast.setRoot(procNode);

            parse(TType.LBrace);
            var stmtNodes = parseStatementList();
            parse(TType.RBrace);

            foreach (var stmt in stmtNodes)
                ast.setChild(procNode, stmt);

            for (int i = 0; i < stmtNodes.Count - 1; i++)
                ast.setFollows(stmtNodes[i], stmtNodes[i + 1]);
        }

        private TNode ParseCall()
        {
            parse(TType.Call);
            string procName = currentNode.getAttr();
            parse(TType.Name);
            parse(TType.SemiColon);

            var callNode = ast.createTNode(TType.Call, procName, stmtNumber++);
            return callNode;
        }


        private List<TNode> parseStatementList()
        {
            var stmts = new List<TNode>();
            while (currentNode.getType() == TType.Name ||
                   currentNode.getType() == TType.While ||
                   currentNode.getType() == TType.Call)
            {
                var stmt = parseStatement();
                stmts.Add(stmt);
            }
            return stmts;
        }


        private TNode parseStatement()
        {
            switch (currentNode.getType())
            {
                case TType.Name:
                    return parseAssign();
                case TType.While:
                    return parseWhile();
                case TType.Call:
                    return ParseCall();
                default:
                    throw new Exception($"Unexpected token {currentNode.getType()}");
            }
        }


        private TNode parseAssign()
        {
            string varName = currentNode.getAttr();
            parse(TType.Name);
            parse(TType.Assign);
            var exprNode = parseExpr();
            parse(TType.SemiColon);

            TNode assignNode = ast.createTNode(TType.Assign, varName, stmtNumber++);

            // Link expression to assign
            ast.setChild(assignNode, exprNode);

            return assignNode;
        }

        private TNode parseWhile()
        {
            parse(TType.While);
            string varName = currentNode.getAttr();
            parse(TType.Name);
            parse(TType.LBrace);

            var whileNode = ast.createTNode(TType.While, varName, stmtNumber++);
            var stmtList = parseStatementList();
            parse(TType.RBrace);

            // Add all child statements to while node
            foreach (var stmt in stmtList)
                ast.setChild(whileNode, stmt);

            // Set FOLLOWS for statements inside the loop
            for (int i = 0; i < stmtList.Count - 1; i++)
                ast.setFollows(stmtList[i], stmtList[i + 1]);

            return whileNode;
        }

        private TNode parseExpr()
        {
            var left = parseFactor();
            while (currentNode.getType() == TType.Plus)
            {
                parse(TType.Plus);
                var right = parseFactor();

                TNode plusNode = ast.createTNode(TType.Plus, "+", -1);
                ast.setChild(plusNode, left);
                ast.setChild(plusNode, right);
                left = plusNode;
            }
            return left;
        }

        private TNode parseFactor()
        {
            if (currentNode.getType() == TType.Name)
            {
                var varName = currentNode.getAttr();
                parse(TType.Name);
                return ast.createTNode(TType.Variable, varName, -1);
            }
            else if (currentNode.getType() == TType.Constant)
            {
                var constValue = currentNode.getAttr();
                parse(TType.Constant);
                return ast.createTNode(TType.Constant, constValue, -1);
            }
            throw new Exception("Expected variable or constant");
        }
    }
}