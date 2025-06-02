using aitsi.Parser;
using aitsi.PKB;

namespace aitsi.Parser
{
	public class DesignExtractor
    {
        private AST ast;
        private PKB.PKB pkb;

        private Dictionary<int, TNode> statementNodes;
        private int statementCounter;

        public DesignExtractor(AST ast)
        {
            this.ast = ast;
            this.pkb = new PKB.PKB(ast);
            this.statementNodes = new Dictionary<int, TNode>();
            this.statementCounter = 1;
        }

        public PKB.PKB Extract()
        {
            TNode root = ast.getRoot();
            if (root == null || root.getType() != TType.Program)
            {
                return null;
            }

            IdentifyEntities(root);

            ProcessProcedures(root);

            pkb.ExtractInformation();


            return pkb;
        }

        private void IdentifyEntities(TNode root)
        {
            foreach (TNode procNode in root.getChildren())
            {
                if (procNode.getType() == TType.Procedure)
                {
                    string procName = procNode.getAttr();
                }
            }

            IdentifyVariablesAndConstants(root);
        }

        private void IdentifyVariablesAndConstants(TNode node)
        {
            if (node.getType() == TType.Variable)
            {
                string varName = node.getAttr();
            }
            else if (node.getType() == TType.Constant)
            {
                string constValue = node.getAttr();
            }

            foreach (TNode child in node.getChildren())
            {
                IdentifyVariablesAndConstants(child);
            }
        }

        private void ProcessProcedures(TNode root)
        {
            statementCounter = 1;

            foreach (TNode procNode in root.getChildren())
            {
                if (procNode.getType() != TType.Procedure)
                    continue;

                string procName = procNode.getAttr();

                TNode previousStmt = null;
                foreach (TNode stmtNode in procNode.getChildren())
                {
                    if (previousStmt != null)
                    {
                        ast.setFollows(previousStmt, stmtNode);
                    }

                    ProcessStatement(stmtNode, procName, -1);
                    previousStmt = stmtNode;
                }
            }
        }

        private void ProcessStatement(TNode stmtNode, string procName, int parentStmt)
        {
            if (stmtNode.getType() == TType.Else)
            {
                TNode previousStmt = null;
                foreach (TNode childStmt in stmtNode.getChildren())
                {
                    if (previousStmt != null)
                    {
                        ast.setFollows(previousStmt, childStmt);
                    }

                    ProcessStatement(childStmt, procName, parentStmt);
                    previousStmt = childStmt;
                }
                return;
            }

            int stmtNum = statementCounter++;
            statementNodes[stmtNum] = stmtNode;

            /*if (parentStmt != -1)
            {
            }

            if (stmtNode.getFollows() != null)
            {
            }*/

            switch (stmtNode.getType())
            {
                case TType.Assign:
                    ProcessAssignStmt(stmtNode, stmtNum, procName);
                    break;

                case TType.While:
                    ProcessWhileStmt(stmtNode, stmtNum, procName);
                    break;

                case TType.If:
                    ProcessIfStmt(stmtNode, stmtNum, procName);
                    break;

                case TType.Call:
                    ProcessCallStmt(stmtNode, stmtNum, procName);
                    break;
            }
        }

        private void ProcessAssignStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            foreach (TNode exprNode in stmtNode.getChildren())
            {
                ExtractUsedVariables(exprNode, stmtNum, procName);
            }
        }

        private void ProcessWhileStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string controlVar = stmtNode.getAttr();

            TNode previousStmt = null;
            foreach (TNode childStmt in stmtNode.getChildren())
            {
                if (previousStmt != null)
                {
                    ast.setFollows(previousStmt, childStmt);
                }

                ProcessStatement(childStmt, procName, stmtNum);
                previousStmt = childStmt;
            }
        }

        private void ProcessIfStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string controlVar = stmtNode.getAttr();
            TNode previousThenStmt = null; 
            TNode elseNode = null;

            foreach (TNode childNode in stmtNode.getChildren())
            {
                if (childNode.getType() == TType.Else)
                {
                    elseNode = childNode;

                    TNode previousElseStmt = null;
                    foreach (TNode elseStmt in childNode.getChildren())
                    {
                        if (previousElseStmt != null)
                        {
                            ast.setFollows(previousElseStmt, elseStmt);
                        }

                        ProcessStatement(elseStmt, procName, stmtNum);
                        previousElseStmt = elseStmt;
                    }
                }
                else
                {
                    if (previousThenStmt != null)
                    {
                        ast.setFollows(previousThenStmt, childNode);
                    }

                    ProcessStatement(childNode, procName, stmtNum);
                    previousThenStmt = childNode;
                }
            }
        }

        private void ProcessCallStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string calledProc = stmtNode.getAttr();
        }

        private void ExtractUsedVariables(TNode exprNode, int stmtNum, string procName)
        {
            if (exprNode.getType() == TType.Variable)
            {
                string varName = exprNode.getAttr();
            }
            else if (exprNode.getType() == TType.Constant)
            {
                string constValue = exprNode.getAttr();
            }

            foreach (TNode child in exprNode.getChildren())
            {
                ExtractUsedVariables(child, stmtNum, procName);
            }
        }
    }
}