using aitsi.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aitsi.PKB
{
     public class PKB
    {
        private AST ast;

        private HashSet<string> procedures = new();
        private HashSet<string> variables = new();
        private HashSet<string> constants = new();

        private Dictionary<int, TNode> statements = new();
        private Dictionary<string, List<int>> assignStmts = new();
        private Dictionary<string, List<int>> whileStmts = new();
        private Dictionary<string, List<int>> ifStmts = new();
        private Dictionary<string, List<int>> callStmts = new();

        private Dictionary<string, List<string>> modifies = new(); 
        private Dictionary<int, List<string>> modifiesStmt = new();
        private Dictionary<string, List<string>> uses = new(); 
        private Dictionary<int, List<string>> usesStmt = new();
        private Dictionary<int, int> follows = new(); 
        private Dictionary<int, HashSet<int>> followsStar = new();
        private Dictionary<int, int> parent = new(); 
        private Dictionary<int, HashSet<int>> parentStar = new();
        private Dictionary<string, List<string>> calls = new(); 
        private Dictionary<string, HashSet<string>> callsStar = new();

        private Dictionary<string, List<int>> procToStmts = new();

        public PKB(AST ast)
        {
            this.ast = ast;
        }

        public void printInfo()
        {
            Console.WriteLine("procedures "+ procedures.Count);
            Console.WriteLine("variables " + variables.Count);
            Console.WriteLine("constants " + constants.Count);
            Console.WriteLine("assignStmts " + assignStmts.Count);
            Console.WriteLine("whileStmts " + whileStmts.Count);
            Console.WriteLine("ifStmts " + ifStmts.Count);
            Console.WriteLine("callStmts " + callStmts.Count);
            Console.WriteLine("modifies " + modifies.Count);
            Console.WriteLine("modifiesStmt " + modifiesStmt.Count);
            Console.WriteLine("uses " + uses.Count);
            Console.WriteLine("usesStmt " + usesStmt.Count);
            Console.WriteLine("follows " + follows.Count);
            Console.WriteLine("usesStmt " + usesStmt.Count);
            Console.WriteLine("followsStar " + followsStar.Count);
            Console.WriteLine("parent " + parent.Count);
            Console.WriteLine("parentStar " + parentStar.Count);
            Console.WriteLine("calls " + calls.Count);
            Console.WriteLine("callsStar " + callsStar.Count);

        }
        public void ExtractInformation()
        {
            TNode root = ast.getRoot();
            if (root == null || root.getType() != TType.Program)
            {
                throw new Exception("Invalid AST: Root node is not a Program");
            }

            foreach (TNode procNode in root.getChildren())
            {
                if (procNode.getType() != TType.Procedure)
                    continue;

                string procName = procNode.getAttr();
                procedures.Add(procName);
                procToStmts[procName] = new List<int>();

                if (!modifies.ContainsKey(procName))
                    modifies[procName] = new List<string>();

                if (!uses.ContainsKey(procName))
                    uses[procName] = new List<string>();

                if (!calls.ContainsKey(procName))
                    calls[procName] = new List<string>();

                
                ProcessStatements(procNode, procName);
            }

            
            BuildFollowsStar();
            BuildParentStar();
            BuildCallsStar();
        }

        
        private void ProcessStatements(TNode procNode, string procName)
        {
            foreach (TNode stmtNode in procNode.getChildren())
            {
                ProcessStatement(stmtNode, procName, -1); 
            }
        }

        
        private void ProcessStatement(TNode stmtNode, string procName, int parentStmt)
        {
            if (stmtNode.getType() == TType.Else)
            {
        
                foreach (TNode childStmt in stmtNode.getChildren())
                {
                    ProcessStatement(childStmt, procName, parentStmt);
                }
                return;
            }


            int number = 1;
            int stmtNum = number++;

            statements[stmtNum] = stmtNode;
            procToStmts[procName].Add(stmtNum);

            if (parentStmt != -1)
            {
                parent[stmtNum] = parentStmt;
            }

            TNode? followsNode = stmtNode.getFollows();
            if (followsNode != null && int.TryParse(followsNode.getAttr(), out int followsStmt))
            {
                follows[stmtNum] = followsStmt;
            }

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
            variables.Add(varName);

            if (!assignStmts.ContainsKey(varName))
                assignStmts[varName] = new List<int>();
            assignStmts[varName].Add(stmtNum);

            if (!modifiesStmt.ContainsKey(stmtNum))
                modifiesStmt[stmtNum] = new List<string>();
            modifiesStmt[stmtNum].Add(varName);

            if (!modifies[procName].Contains(varName))
                modifies[procName].Add(varName);

            foreach (TNode exprNode in stmtNode.getChildren())
            {
                ExtractUsedVariables(exprNode, stmtNum, procName);
            }
        }

        private void ProcessWhileStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            variables.Add(varName);

            if (!whileStmts.ContainsKey(varName))
                whileStmts[varName] = new List<int>();
            whileStmts[varName].Add(stmtNum);

            if (!usesStmt.ContainsKey(stmtNum))
                usesStmt[stmtNum] = new List<string>();
            usesStmt[stmtNum].Add(varName);

            if (!uses[procName].Contains(varName))
                uses[procName].Add(varName);

            foreach (TNode childStmt in stmtNode.getChildren())
            {
                ProcessStatement(childStmt, procName, stmtNum);
            }
        }

        private void ProcessIfStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            variables.Add(varName);

            if (!ifStmts.ContainsKey(varName))
                ifStmts[varName] = new List<int>();
            ifStmts[varName].Add(stmtNum);

            if (!usesStmt.ContainsKey(stmtNum))
                usesStmt[stmtNum] = new List<string>();
            usesStmt[stmtNum].Add(varName);

            if (!uses[procName].Contains(varName))
                uses[procName].Add(varName);

            foreach (TNode childNode in stmtNode.getChildren())
            {
                if (childNode.getType() == TType.Else)
                {
                    foreach (TNode elseStmt in childNode.getChildren())
                    {
                        ProcessStatement(elseStmt, procName, stmtNum);
                    }
                }
                else
                {
                    ProcessStatement(childNode, procName, stmtNum);
                }
            }
        }

        private void ProcessCallStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string calledProc = stmtNode.getAttr();

            if (!callStmts.ContainsKey(calledProc))
                callStmts[calledProc] = new List<int>();
            callStmts[calledProc].Add(stmtNum);

            if (!calls[procName].Contains(calledProc))
                calls[procName].Add(calledProc);
        }

        private void ExtractUsedVariables(TNode exprNode, int stmtNum, string procName)
        {
            if (exprNode.getType() == TType.Variable)
            {
                string varName = exprNode.getAttr();
                variables.Add(varName);

                if (!usesStmt.ContainsKey(stmtNum))
                    usesStmt[stmtNum] = new List<string>();
                if (!usesStmt[stmtNum].Contains(varName))
                    usesStmt[stmtNum].Add(varName);

                if (!uses[procName].Contains(varName))
                    uses[procName].Add(varName);
            }
            else if (exprNode.getType() == TType.Constant)
            {
                constants.Add(exprNode.getAttr());
            }

            foreach (TNode child in exprNode.getChildren())
            {
                ExtractUsedVariables(child, stmtNum, procName);
            }
        }

        private void BuildFollowsStar()
        {
            foreach (var pair in follows)
            {
                int s1 = pair.Key;
                int s2 = pair.Value;

                if (!followsStar.ContainsKey(s1))
                    followsStar[s1] = new HashSet<int>();

                followsStar[s1].Add(s2);
            }

            bool changed;
            do
            {
                changed = false;

                foreach (var pair in followsStar.ToList())
                {
                    int s1 = pair.Key;
                    HashSet<int> s1Follows = pair.Value;
                    int initialCount = s1Follows.Count;

                    foreach (int s2 in s1Follows.ToList())
                    {
                        if (followsStar.ContainsKey(s2))
                        {
                            foreach (int s3 in followsStar[s2])
                            {
                                s1Follows.Add(s3);
                            }
                        }
                    }

                    if (s1Follows.Count > initialCount)
                        changed = true;
                }
            } while (changed);
        }

        private void BuildParentStar()
        {
            foreach (var pair in parent)
            {
                int child = pair.Key;
                int p = pair.Value;

                if (!parentStar.ContainsKey(p))
                    parentStar[p] = new HashSet<int>();

                parentStar[p].Add(child);
            }

            bool changed;
            do
            {
                changed = false;

                foreach (var pair in parentStar.ToList())
                {
                    int p1 = pair.Key;
                    HashSet<int> children = pair.Value;

                    foreach (int child in children.ToList())
                    {
                        if (parentStar.ContainsKey(child))
                        {
                            int initialCount = children.Count;

                            foreach (int grandchild in parentStar[child])
                            {
                                children.Add(grandchild);
                            }

                            if (children.Count > initialCount)
                                changed = true;
                        }
                    }
                }
            } while (changed);
        }

        private void BuildCallsStar()
        {
            foreach (var proc in procedures)
            {
                callsStar[proc] = new HashSet<string>();

                if (calls.ContainsKey(proc))
                {
                    foreach (string calledProc in calls[proc])
                    {
                        callsStar[proc].Add(calledProc);
                    }
                }
            }

            bool changed;
            do
            {
                changed = false;

                foreach (var pair in callsStar.ToList())
                {
                    string caller = pair.Key;
                    HashSet<string> callees = pair.Value;

                    foreach (string callee in callees.ToList())
                    {
                        if (callsStar.ContainsKey(callee))
                        {
                            int initialCount = callees.Count;

                            foreach (string transitiveCallee in callsStar[callee])
                            {
                                callees.Add(transitiveCallee);
                            }

                            if (callees.Count > initialCount)
                                changed = true;
                        }
                    }
                }
            } while (changed);
        }

        /* PKB Query Methods */

        public List<string> GetProcedures()
        {
            return procedures.ToList();
        }

        public List<string> GetVariables()
        {
            return variables.ToList();
        }

        public List<string> GetConstants()
        {
            return constants.ToList();
        }

        public List<int> GetStatements()
        {
            return statements.Keys.ToList();
        }
        public List<int> GetAssignStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return assignStmts.Values.SelectMany(list => list).ToList();

            return assignStmts.ContainsKey(variable) ? assignStmts[variable] : new List<int>();
        }
        public List<int> GetWhileStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return whileStmts.Values.SelectMany(list => list).ToList();

            return whileStmts.ContainsKey(variable) ? whileStmts[variable] : new List<int>();
        }
        public List<int> GetIfStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return ifStmts.Values.SelectMany(list => list).ToList();

            return ifStmts.ContainsKey(variable) ? ifStmts[variable] : new List<int>();
        }

        public List<int> GetCallStmts(string procedure = "")
        {
            if (string.IsNullOrEmpty(procedure))
                return callStmts.Values.SelectMany(list => list).ToList();

            return callStmts.ContainsKey(procedure) ? callStmts[procedure] : new List<int>();
        }
        public List<int> GetStmtsInProc(string procedure)
        {
            return procToStmts.ContainsKey(procedure) ? procToStmts[procedure] : new List<int>();
        }
        public List<string> GetModifiesProc(string procedure)
        {
            return modifies.ContainsKey(procedure) ? modifies[procedure] : new List<string>();
        }

        public List<string> GetProcModifies(string variable)
        {
            return modifies.Where(pair => pair.Value.Contains(variable))
                         .Select(pair => pair.Key)
                         .ToList();
        }
        public List<string> GetModifiesStmt(int stmtNum)
        {
            return modifiesStmt.ContainsKey(stmtNum) ? modifiesStmt[stmtNum] : new List<string>();
        }

        public List<int> GetStmtModifies(string variable)
        {
            return modifiesStmt.Where(pair => pair.Value.Contains(variable))
                              .Select(pair => pair.Key)
                              .ToList();
        }

        public List<string> GetUsesProc(string procedure)
        {
            return uses.ContainsKey(procedure) ? uses[procedure] : new List<string>();
        }

        public List<string> GetProcUses(string variable)
        {
            return uses.Where(pair => pair.Value.Contains(variable))
                     .Select(pair => pair.Key)
                     .ToList();
        }

        public List<string> GetUsesStmt(int stmtNum)
        {
            return usesStmt.ContainsKey(stmtNum) ? usesStmt[stmtNum] : new List<string>();
        }
        public List<int> GetStmtUses(string variable)
        {
            return usesStmt.Where(pair => pair.Value.Contains(variable))
                          .Select(pair => pair.Key)
                          .ToList();
        }

        public int GetFollows(int stmtNum)
        {
            return follows.ContainsKey(stmtNum) ? follows[stmtNum] : -1;
        }

        public int GetFollowedBy(int stmtNum)
        {
            return follows.FirstOrDefault(pair => pair.Value == stmtNum).Key;
        }

        public List<int> GetFollowsStar(int stmtNum)
        {
            return followsStar.ContainsKey(stmtNum) ? followsStar[stmtNum].ToList() : new List<int>();
        }

        public List<int> GetFollowedByStar(int stmtNum)
        {
            return followsStar.Where(pair => pair.Value.Contains(stmtNum))
                             .Select(pair => pair.Key)
                             .ToList();
        }

        public int GetParent(int stmtNum)
        {
            return parent.ContainsKey(stmtNum) ? parent[stmtNum] : -1;
        }

        public List<int> GetChildren(int stmtNum)
        {
            return parent.Where(pair => pair.Value == stmtNum)
                        .Select(pair => pair.Key)
                        .ToList();
        }

        public List<int> GetParentStar(int stmtNum)
        {
            List<int> ancestors = new();
            int current = GetParent(stmtNum);

            while (current != -1)
            {
                ancestors.Add(current);
                current = GetParent(current);
            }

            return ancestors;
        }
        public List<int> GetChildrenStar(int stmtNum)
        {
            return parentStar.ContainsKey(stmtNum) ? parentStar[stmtNum].ToList() : new List<int>();
        }

        public List<string> GetCalls(string procedure)
        {
            return calls.ContainsKey(procedure) ? calls[procedure] : new List<string>();
        }

        public List<string> GetCalledBy(string procedure)
        {
            return calls.Where(pair => pair.Value.Contains(procedure))
                      .Select(pair => pair.Key)
                      .ToList();
        }

        public List<string> GetCallsStar(string procedure)
        {
            return callsStar.ContainsKey(procedure) ? callsStar[procedure].ToList() : new List<string>();
        }

        public List<string> GetCalledByStar(string procedure)
        {
            return callsStar.Where(pair => pair.Value.Contains(procedure))
                           .Select(pair => pair.Key)
                           .ToList();
        }

        public bool ProcModifies(string procedure, string variable)
        {
            return modifies.ContainsKey(procedure) && modifies[procedure].Contains(variable);
        }

        public bool StmtModifies(int stmtNum, string variable)
        {
            return modifiesStmt.ContainsKey(stmtNum) && modifiesStmt[stmtNum].Contains(variable);
        }

        public bool ProcUses(string procedure, string variable)
        {
            return uses.ContainsKey(procedure) && uses[procedure].Contains(variable);
        }

        public bool StmtUses(int stmtNum, string variable)
        {
            return usesStmt.ContainsKey(stmtNum) && usesStmt[stmtNum].Contains(variable);
        }
        public bool Follows(int s1, int s2)
        {
            return follows.ContainsKey(s1) && follows[s1] == s2;
        }

        public bool FollowsStar(int s1, int s2)
        {
            return followsStar.ContainsKey(s1) && followsStar[s1].Contains(s2);
        }

        public bool Parent(int s1, int s2)
        {
            return parent.ContainsKey(s2) && parent[s2] == s1;
        }

        public bool ParentStar(int s1, int s2)
        {
            return GetParentStar(s2).Contains(s1);
        }
        public bool Calls(string p1, string p2)
        {
            return calls.ContainsKey(p1) && calls[p1].Contains(p2);
        }

        public bool CallsStar(string p1, string p2)
        {
            return callsStar.ContainsKey(p1) && callsStar[p1].Contains(p2);
        }
        public TNode? GetStatementNode(int stmtNum)
        {
            return statements.ContainsKey(stmtNum) ? statements[stmtNum] : null;
        }

        public TType? GetStatementType(int stmtNum)
        {
            return statements.ContainsKey(stmtNum) ? statements[stmtNum].getType() : null;
        }
        // GETTERY dla potrzebnych prywatnych map i tabel

        public Dictionary<int, TNode> GetStatementTable()
        {
            return statementTable;
        }

        public Dictionary<string, List<TNode>> GetVariableTable()
        {
            return variableTable;
        }

        public Dictionary<string, List<TNode>> GetConstantTable()
        {
            return constantTable;
        }

        public Dictionary<string, TNode> GetProcedureTable()
        {
            return procedureTable;
        }

        public Dictionary<TNode, HashSet<TNode>> GetModifiesMap()
        {
            return modifiesMap;
        }

        public Dictionary<TNode, HashSet<TNode>> GetUsesMap()
        {
            return usesMap;
        }

        public Dictionary<TNode, HashSet<TNode>> GetParentStarMap()
        {
            return parentStarMap;
        }

        public Dictionary<TNode, HashSet<TNode>> GetFollowsStarMap()
        {
            return followsStarMap;
        }

    }
}