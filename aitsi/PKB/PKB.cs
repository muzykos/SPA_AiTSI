using aitsi.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

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
        private int stmtCounter = 1;
        private Dictionary<int, HashSet<int>> next = new();
        private Dictionary<int, HashSet<int>> nextStar = new();

        private Dictionary<string, List<int>> procToStmts = new();

        public PKB(AST ast)
        {
            this.ast = ast;
        }

        public void printInfo()
        {
            Console.WriteLine("procedures " + procedures.Count);
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

            foreach(string var in assignStmts.Keys)
                foreach(var value in assignStmts[var])Console.WriteLine("key: " + var + ", value: " + value);

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
            BuildModifiesRelationships();
            BuildUsesRelationships();
            BuildNext();
            BuildNextStar();
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


                 int stmtNum = stmtCounter++;

            statements[stmtNum] = stmtNode;
            procToStmts[procName].Add(stmtNum);

            if (parentStmt != -1)
            {
                parent[stmtNum] = parentStmt;
            }

            TNode? followsNode = stmtNode.getFollows();
            if (followsNode != null)
            {
                follows[stmtNum] = followsNode.getStmtNumber();
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

            if (!modifiesStmt.ContainsKey(stmtNum))
                modifiesStmt[stmtNum] = new List<string>();
            //modifiesStmt[stmtNum ].Add(varName);

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

            if (!modifiesStmt.ContainsKey(stmtNum))
                modifiesStmt[stmtNum] = new List<string>();
            //modifiesStmt[stmtNum].Add(varName);

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

            if (!modifiesStmt.ContainsKey(stmtNum))
                modifiesStmt[stmtNum] = new List<string>();

            if (!usesStmt.ContainsKey(stmtNum))
                usesStmt[stmtNum] = new List<string>();
        }

        private void BuildModifiesRelationships()
        {
            bool changed;

            do
            {
                changed = false;

                foreach (var procName in procedures)
                {
                    var procStmts = procToStmts[procName];
                    
                    foreach (var stmtNum in procStmts)
                    {
                        var stmt = statements[stmtNum];
                        
                        if (stmt.getType() == TType.Call)
                        {
                            string calledProc = stmt.getAttr();
                            
                            if (modifies.ContainsKey(calledProc))
                            {
                                foreach (var modifiedVar in modifies[calledProc])
                                {
                                    if (!modifiesStmt[stmtNum].Contains(modifiedVar))
                                    {
                                        modifiesStmt[stmtNum].Add(modifiedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);

            do
            {
                changed = false;

                foreach (var stmtNum in statements.Keys)
                {
                    var stmt = statements[stmtNum];

                    if (stmt.getType() == TType.While || stmt.getType() == TType.If)
                    {
                        var childStmts = GetAllChildStatements(stmtNum);

                        foreach (var childStmt in childStmts)
                        {
                            if (modifiesStmt.ContainsKey(childStmt))
                            {
                                foreach (var modifiedVar in modifiesStmt[childStmt])
                                {
                                    if (!modifiesStmt[stmtNum].Contains(modifiedVar))
                                    {
                                        modifiesStmt[stmtNum].Add(modifiedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);

            foreach (var procName in procedures)
            {
                var procStmts = procToStmts[procName];

                foreach (var stmtNum in procStmts)
                {
                    if (modifiesStmt.ContainsKey(stmtNum))
                    {
                        foreach (var modifiedVar in modifiesStmt[stmtNum])
                        {
                            if (!modifies[procName].Contains(modifiedVar))
                            {
                                modifies[procName].Add(modifiedVar);
                            }
                        }
                    }
                }
            }

            do
            {
                changed = false;

                foreach (var procName in procedures)
                {
                    if (calls.ContainsKey(procName))
                    {
                        foreach (var calledProc in calls[procName])
                        {
                            if (modifies.ContainsKey(calledProc))
                            {
                                foreach (var modifiedVar in modifies[calledProc])
                                {
                                    if (!modifies[procName].Contains(modifiedVar))
                                    {
                                        modifies[procName].Add(modifiedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);
        }

        private void BuildUsesRelationships()
        {
            bool changed;
            do
            {
                changed = false;

                foreach (var procName in procedures)
                {
                    var procStmts = procToStmts[procName];

                    foreach (var stmtNum in procStmts)
                    {
                        var stmt = statements[stmtNum];

                        if (stmt.getType() == TType.Call)
                        {
                            string calledProc = stmt.getAttr();

                            if (uses.ContainsKey(calledProc))
                            {
                                foreach (var usedVar in uses[calledProc])
                                {
                                    if (!usesStmt[stmtNum].Contains(usedVar))
                                    {
                                        usesStmt[stmtNum].Add(usedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);

            do
            {
                changed = false;

                foreach (var stmtNum in statements.Keys)
                {
                    var stmt = statements[stmtNum];

                    if (stmt.getType() == TType.While || stmt.getType() == TType.If)
                    {
                        var childStmts = GetAllChildStatements(stmtNum);

                        foreach (var childStmt in childStmts)
                        {
                            if (usesStmt.ContainsKey(childStmt))
                            {
                                foreach (var usedVar in usesStmt[childStmt])
                                {
                                    if (!usesStmt[stmtNum].Contains(usedVar))
                                    {
                                        usesStmt[stmtNum].Add(usedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);

            foreach (var procName in procedures)
            {
                var procStmts = procToStmts[procName];

                foreach (var stmtNum in procStmts)
                {
                    if (usesStmt.ContainsKey(stmtNum))
                    {
                        foreach (var usedVar in usesStmt[stmtNum])
                        {
                            if (!uses[procName].Contains(usedVar))
                            {
                                uses[procName].Add(usedVar);
                            }
                        }
                    }
                }
            }

            do
            {
                changed = false;

                foreach (var procName in procedures)
                {
                    if (calls.ContainsKey(procName))
                    {
                        foreach (var calledProc in calls[procName])
                        {
                            if (uses.ContainsKey(calledProc))
                            {
                                foreach (var usedVar in uses[calledProc])
                                {
                                    if (!uses[procName].Contains(usedVar))
                                    {
                                        uses[procName].Add(usedVar);
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);
        }
        private List<int> GetAllChildStatements(int parentStmt)
        {
            var childStmts = new List<int>();

            foreach (var kvp in parent)
            {
                if (kvp.Value == parentStmt)
                {
                    childStmts.Add(kvp.Key);
                    childStmts.AddRange(GetAllChildStatements(kvp.Key));
                }
            }

            return childStmts;
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

        private void BuildNext()
        {
            foreach (string procName in procedures)
            {
                List<int> procStmts = procToStmts[procName];
                if (procStmts.Count == 0) continue;

                BuildNextForProcedure(procStmts);
            }
        }

        private void BuildNextForProcedure(List<int> procStmts)
        {
            var sortedStmts = procStmts.OrderBy(s => s).ToList();

            for (int i = 0; i < sortedStmts.Count; i++)
            {
                int currentStmt = sortedStmts[i];
                TNode currentNode = statements[currentStmt];

                BuildNextForStatement(currentStmt, currentNode, procStmts);
            }
        }

        private void BuildNextForStatement(int stmtNum, TNode stmtNode, List<int> procStmts)
        {
            if (!next.ContainsKey(stmtNum))
                next[stmtNum] = new HashSet<int>();

            switch (stmtNode.getType())
            {
                case TType.Assign:
                case TType.Call:

                    AddDirectNext(stmtNum, procStmts);
                    break;

                case TType.While:

                    BuildNextForWhile(stmtNum, stmtNode, procStmts);
                    break;

                case TType.If:

                    BuildNextForIf(stmtNum, stmtNode, procStmts);
                    break;
            }
        }

        private void AddDirectNext(int stmtNum, List<int> procStmts)
        {
            if (follows.ContainsKey(stmtNum))
            {
                next[stmtNum].Add(follows[stmtNum]);
            }
            else
            {
                int parentStmt = GetParent(stmtNum);
                if (parentStmt != -1)
                {
                    TNode parentNode = statements[parentStmt];

                    if (parentNode.getType() == TType.While)
                    {
                        next[stmtNum].Add(parentStmt);
                    }
                    else if (parentNode.getType() == TType.If)
                    {
                        if (follows.ContainsKey(parentStmt))
                        {
                            next[stmtNum].Add(follows[parentStmt]);
                        }
                    }
                }
            }
        }

        private void BuildNextForWhile(int whileStmt, TNode whileNode, List<int> procStmts)
        {
            var loopChildren = GetChildren(whileStmt);

            if (loopChildren.Count > 0)
            {
                int firstInLoop = loopChildren.OrderBy(s => s).First();
                next[whileStmt].Add(firstInLoop);
            }

            if (follows.ContainsKey(whileStmt))
            {
                next[whileStmt].Add(follows[whileStmt]);
            }
        }

        private void BuildNextForIf(int ifStmt, TNode ifNode, List<int> procStmts)
        {
            var ifChildren = new List<int>();
            var elseChildren = new List<int>();

            foreach (TNode child in ifNode.getChildren())
            {
                if (child.getType() == TType.Else)
                {
                    foreach (TNode elseChild in child.getChildren())
                    {
                        if (statements.ContainsValue(elseChild))
                        {
                            int elseStmtNum = statements.FirstOrDefault(kvp => kvp.Value == elseChild).Key;
                            if (elseStmtNum != 0)
                                elseChildren.Add(elseStmtNum);
                        }
                    }
                }
                else
                {
                    if (statements.ContainsValue(child))
                    {
                        int childStmtNum = statements.FirstOrDefault(kvp => kvp.Value == child).Key;
                        if (childStmtNum != 0)
                            ifChildren.Add(childStmtNum);
                    }
                }
            }

            if (ifChildren.Count > 0)
            {
                int firstInThen = ifChildren.OrderBy(s => s).First();
                next[ifStmt].Add(firstInThen);
            }

            if (elseChildren.Count > 0)
            {
                int firstInElse = elseChildren.OrderBy(s => s).First();
                next[ifStmt].Add(firstInElse);
            }

            if (elseChildren.Count == 0 && follows.ContainsKey(ifStmt))
            {
                next[ifStmt].Add(follows[ifStmt]);
            }
        }

        private void BuildNextStar()
        {
            foreach (var pair in next)
            {
                int n1 = pair.Key;
                HashSet<int> directNext = pair.Value;

                if (!nextStar.ContainsKey(n1))
                    nextStar[n1] = new HashSet<int>();

                foreach (int n2 in directNext)
                {
                    nextStar[n1].Add(n2);
                }
            }

            bool changed;
            do
            {
                changed = false;

                foreach (var pair in nextStar.ToList())
                {
                    int n1 = pair.Key;
                    HashSet<int> n1Next = pair.Value;
                    int initialCount = n1Next.Count;

                    foreach (int n2 in n1Next.ToList())
                    {
                        if (nextStar.ContainsKey(n2))
                        {
                            foreach (int n3 in nextStar[n2])
                            {
                                n1Next.Add(n3);
                            }
                        }
                    }

                    if (n1Next.Count > initialCount)
                        changed = true;
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

            if (string.IsNullOrEmpty(variable) || variable == "_")
            {
                List<int> result = new List<int>();
                foreach (var var in ifStmts.Values)
                    foreach (var value in var) {
                        result.Add(value);
                    }
                return result;
            }

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

        public List<int> GetNext(int stmtNum)
        {
            return next.ContainsKey(stmtNum) ? next[stmtNum].ToList() : new List<int>();
        }

        public List<int> GetPrevious(int stmtNum)
        {
            return next.Where(pair => pair.Value.Contains(stmtNum))
                      .Select(pair => pair.Key)
                      .ToList();
        }

        public List<int> GetNextStar(int stmtNum)
        {
            return nextStar.ContainsKey(stmtNum) ? nextStar[stmtNum].ToList() : new List<int>();
        }

        public List<int> GetPreviousStar(int stmtNum)
        {
            return nextStar.Where(pair => pair.Value.Contains(stmtNum))
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

        public Dictionary<int, int> GetFollowsMap()
        {
            return follows;
        }
        // GETTERY dla potrzebnych prywatnych map i tabel

        //public Dictionary<int, TNode> GetStatementTable()
        //{
        //    return statementTable;
        //}

        //public Dictionary<string, List<TNode>> GetVariableTable()
        //{
        //    return variableTable;
        //}

        //public Dictionary<string, List<TNode>> GetConstantTable()
        //{
        //    return constantTable;
        //}

        //public Dictionary<string, TNode> GetProcedureTable()
        //{
        //    return procedureTable;
        //}

        //public Dictionary<TNode, HashSet<TNode>> GetModifiesMap()
        //{
        //    return modifiesMap;
        //}

        //public Dictionary<TNode, HashSet<TNode>> GetUsesMap()
        //{
        //    return usesMap;
        //}

        //public Dictionary<TNode, HashSet<TNode>> GetParentStarMap()
        //{
        //    return parentStarMap;
        //}

        //public Dictionary<TNode, HashSet<TNode>> GetFollowsStarMap()
        //{
        //    return followsStarMap;
        //}

    }
}