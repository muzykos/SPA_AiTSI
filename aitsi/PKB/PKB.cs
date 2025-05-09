using aitsi.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aitsi.PKB
{
    /// <summary>
    /// Program Knowledge Base (PKB) - Stores and manages information extracted from the AST
    /// </summary>
     class PKB
    {
        private AST ast;

        // Basic program entities
        private HashSet<string> procedures = new();
        private HashSet<string> variables = new();
        private HashSet<string> constants = new();

        // Statement information
        private Dictionary<int, TNode> statements = new();
        private Dictionary<string, List<int>> assignStmts = new();
        private Dictionary<string, List<int>> whileStmts = new();
        private Dictionary<string, List<int>> ifStmts = new();
        private Dictionary<string, List<int>> callStmts = new();

        // Relationships
        private Dictionary<string, List<string>> modifies = new(); // proc -> vars
        private Dictionary<int, List<string>> modifiesStmt = new(); // stmt -> vars
        private Dictionary<string, List<string>> uses = new(); // proc -> vars
        private Dictionary<int, List<string>> usesStmt = new(); // stmt -> vars
        private Dictionary<int, int> follows = new(); // stmt -> stmt
        private Dictionary<int, HashSet<int>> followsStar = new(); // stmt -> stmts
        private Dictionary<int, int> parent = new(); // stmt -> stmt
        private Dictionary<int, HashSet<int>> parentStar = new(); // stmt -> stmts
        private Dictionary<string, List<string>> calls = new(); // proc -> procs
        private Dictionary<string, HashSet<string>> callsStar = new(); // proc -> procs

        // Procedure-statement relationships
        private Dictionary<string, List<int>> procToStmts = new();

        /// <summary>
        /// Initializes a new instance of the PKB with the given AST
        /// </summary>
        /// <param name="ast">The AST to extract information from</param>
        public PKB(AST ast)
        {
            this.ast = ast;
        }

        /// <summary>
        /// Extracts all relevant information from the AST
        /// </summary>
        /// 
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

            // Process each procedure in the program
            foreach (TNode procNode in root.getChildren())
            {
                if (procNode.getType() != TType.Procedure)
                    continue;

                string procName = procNode.getAttr();
                procedures.Add(procName);
                procToStmts[procName] = new List<int>();

                // Initialize collections for this procedure
                if (!modifies.ContainsKey(procName))
                    modifies[procName] = new List<string>();

                if (!uses.ContainsKey(procName))
                    uses[procName] = new List<string>();

                if (!calls.ContainsKey(procName))
                    calls[procName] = new List<string>();

                // Process all statements in this procedure
                ProcessStatements(procNode, procName);
            }

            // Build the transitive closure relationships
            BuildFollowsStar();
            BuildParentStar();
            BuildCallsStar();
        }

        /// <summary>
        /// Processes all statements in a procedure and extracts relevant information
        /// </summary>
        /// <param name="procNode">The procedure node</param>
        /// <param name="procName">The name of the procedure</param>
        private void ProcessStatements(TNode procNode, string procName)
        {
            foreach (TNode stmtNode in procNode.getChildren())
            {
                ProcessStatement(stmtNode, procName, -1); // -1 indicates no parent statement
            }
        }

        /// <summary>
        /// Processes a single statement and extracts information from it
        /// </summary>
        /// <param name="stmtNode">The statement node</param>
        /// <param name="procName">The procedure this statement belongs to</param>
        /// <param name="parentStmt">The parent statement number or -1 if none</param>
        private void ProcessStatement(TNode stmtNode, string procName, int parentStmt)
        {
            if (stmtNode.getType() == TType.Else)
            {
                // For else nodes, just process all statements inside
                foreach (TNode childStmt in stmtNode.getChildren())
                {
                    ProcessStatement(childStmt, procName, parentStmt);
                }
                return;
            }

            // Extract statement number
            int number = 1;
            //int stmtNum = int.Parse(stmtNode.getAttr());
            int stmtNum = number++;

            // Register the statement
            statements[stmtNum] = stmtNode;
            procToStmts[procName].Add(stmtNum);

            // Set parent relationship if applicable
            if (parentStmt != -1)
            {
                parent[stmtNum] = parentStmt;
            }

            // Set follows relationship if applicable
            TNode? followsNode = stmtNode.getFollows();
            if (followsNode != null && int.TryParse(followsNode.getAttr(), out int followsStmt))
            {
                follows[stmtNum] = followsStmt;
            }

            // Process statement based on type
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

        /// <summary>
        /// Processes an assignment statement
        /// </summary>
        private void ProcessAssignStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            variables.Add(varName);

            // Register this as an assignment statement
            if (!assignStmts.ContainsKey(varName))
                assignStmts[varName] = new List<int>();
            assignStmts[varName].Add(stmtNum);

            // This statement modifies the variable
            if (!modifiesStmt.ContainsKey(stmtNum))
                modifiesStmt[stmtNum] = new List<string>();
            modifiesStmt[stmtNum].Add(varName);

            // The procedure also modifies this variable
            if (!modifies[procName].Contains(varName))
                modifies[procName].Add(varName);

            // Process the right-hand side expression to find all variables used
            foreach (TNode exprNode in stmtNode.getChildren())
            {
                ExtractUsedVariables(exprNode, stmtNum, procName);
            }
        }

        /// <summary>
        /// Processes a while statement
        /// </summary>
        private void ProcessWhileStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            variables.Add(varName);

            // Register this as a while statement
            if (!whileStmts.ContainsKey(varName))
                whileStmts[varName] = new List<int>();
            whileStmts[varName].Add(stmtNum);

            // This statement uses the variable in its condition
            if (!usesStmt.ContainsKey(stmtNum))
                usesStmt[stmtNum] = new List<string>();
            usesStmt[stmtNum].Add(varName);

            // The procedure also uses this variable
            if (!uses[procName].Contains(varName))
                uses[procName].Add(varName);

            // Process all statements inside the while loop
            foreach (TNode childStmt in stmtNode.getChildren())
            {
                ProcessStatement(childStmt, procName, stmtNum);
            }
        }

        /// <summary>
        /// Processes an if statement
        /// </summary>
        private void ProcessIfStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string varName = stmtNode.getAttr();
            variables.Add(varName);

            // Register this as an if statement
            if (!ifStmts.ContainsKey(varName))
                ifStmts[varName] = new List<int>();
            ifStmts[varName].Add(stmtNum);

            // This statement uses the variable in its condition
            if (!usesStmt.ContainsKey(stmtNum))
                usesStmt[stmtNum] = new List<string>();
            usesStmt[stmtNum].Add(varName);

            // The procedure also uses this variable
            if (!uses[procName].Contains(varName))
                uses[procName].Add(varName);

            // Process all statements inside the if and else branches
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

        /// <summary>
        /// Processes a call statement
        /// </summary>
        private void ProcessCallStmt(TNode stmtNode, int stmtNum, string procName)
        {
            string calledProc = stmtNode.getAttr();

            // Register this as a call statement
            if (!callStmts.ContainsKey(calledProc))
                callStmts[calledProc] = new List<int>();
            callStmts[calledProc].Add(stmtNum);

            // Add to calls relationship
            if (!calls[procName].Contains(calledProc))
                calls[procName].Add(calledProc);
        }

        /// <summary>
        /// Recursively extracts used variables from an expression
        /// </summary>
        private void ExtractUsedVariables(TNode exprNode, int stmtNum, string procName)
        {
            if (exprNode.getType() == TType.Variable)
            {
                string varName = exprNode.getAttr();
                variables.Add(varName);

                // This statement uses this variable
                if (!usesStmt.ContainsKey(stmtNum))
                    usesStmt[stmtNum] = new List<string>();
                if (!usesStmt[stmtNum].Contains(varName))
                    usesStmt[stmtNum].Add(varName);

                // The procedure also uses this variable
                if (!uses[procName].Contains(varName))
                    uses[procName].Add(varName);
            }
            else if (exprNode.getType() == TType.Constant)
            {
                constants.Add(exprNode.getAttr());
            }

            // Recursively process all children of this expression
            foreach (TNode child in exprNode.getChildren())
            {
                ExtractUsedVariables(child, stmtNum, procName);
            }
        }

        /// <summary>
        /// Builds the transitive closure for Follows relationship
        /// </summary>
        private void BuildFollowsStar()
        {
            // Initialize with direct follows relationships
            foreach (var pair in follows)
            {
                int s1 = pair.Key;
                int s2 = pair.Value;

                if (!followsStar.ContainsKey(s1))
                    followsStar[s1] = new HashSet<int>();

                followsStar[s1].Add(s2);
            }

            // Compute transitive closure
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

        /// <summary>
        /// Builds the transitive closure for Parent relationship
        /// </summary>
        private void BuildParentStar()
        {
            // Initialize with direct parent relationships
            foreach (var pair in parent)
            {
                int child = pair.Key;
                int p = pair.Value;

                if (!parentStar.ContainsKey(p))
                    parentStar[p] = new HashSet<int>();

                parentStar[p].Add(child);
            }

            // Compute transitive closure
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

        /// <summary>
        /// Builds the transitive closure for Calls relationship
        /// </summary>
        private void BuildCallsStar()
        {
            // Initialize with direct calls relationships
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

            // Compute transitive closure
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

        /// <summary>
        /// Gets all procedures in the program
        /// </summary>
        public List<string> GetProcedures()
        {
            return procedures.ToList();
        }

        /// <summary>
        /// Gets all variables in the program
        /// </summary>
        public List<string> GetVariables()
        {
            return variables.ToList();
        }

        /// <summary>
        /// Gets all constants in the program
        /// </summary>
        public List<string> GetConstants()
        {
            return constants.ToList();
        }

        /// <summary>
        /// Gets all statement numbers in the program
        /// </summary>
        public List<int> GetStatements()
        {
            return statements.Keys.ToList();
        }

        /// <summary>
        /// Gets all assignment statements involving the given variable
        /// </summary>
        public List<int> GetAssignStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return assignStmts.Values.SelectMany(list => list).ToList();

            return assignStmts.ContainsKey(variable) ? assignStmts[variable] : new List<int>();
        }

        /// <summary>
        /// Gets all while statements involving the given variable
        /// </summary>
        public List<int> GetWhileStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return whileStmts.Values.SelectMany(list => list).ToList();

            return whileStmts.ContainsKey(variable) ? whileStmts[variable] : new List<int>();
        }

        /// <summary>
        /// Gets all if statements involving the given variable
        /// </summary>
        public List<int> GetIfStmts(string variable = "")
        {
            if (string.IsNullOrEmpty(variable))
                return ifStmts.Values.SelectMany(list => list).ToList();

            return ifStmts.ContainsKey(variable) ? ifStmts[variable] : new List<int>();
        }

        /// <summary>
        /// Gets all call statements calling the given procedure
        /// </summary>
        public List<int> GetCallStmts(string procedure = "")
        {
            if (string.IsNullOrEmpty(procedure))
                return callStmts.Values.SelectMany(list => list).ToList();

            return callStmts.ContainsKey(procedure) ? callStmts[procedure] : new List<int>();
        }

        /// <summary>
        /// Gets all statements in the given procedure
        /// </summary>
        public List<int> GetStmtsInProc(string procedure)
        {
            return procToStmts.ContainsKey(procedure) ? procToStmts[procedure] : new List<int>();
        }

        /// <summary>
        /// Gets all variables modified by the given procedure
        /// </summary>
        public List<string> GetModifiesProc(string procedure)
        {
            return modifies.ContainsKey(procedure) ? modifies[procedure] : new List<string>();
        }

        /// <summary>
        /// Gets all procedures that modify the given variable
        /// </summary>
        public List<string> GetProcModifies(string variable)
        {
            return modifies.Where(pair => pair.Value.Contains(variable))
                         .Select(pair => pair.Key)
                         .ToList();
        }

        /// <summary>
        /// Gets all variables modified by the given statement
        /// </summary>
        public List<string> GetModifiesStmt(int stmtNum)
        {
            return modifiesStmt.ContainsKey(stmtNum) ? modifiesStmt[stmtNum] : new List<string>();
        }

        /// <summary>
        /// Gets all statements that modify the given variable
        /// </summary>
        public List<int> GetStmtModifies(string variable)
        {
            return modifiesStmt.Where(pair => pair.Value.Contains(variable))
                              .Select(pair => pair.Key)
                              .ToList();
        }

        /// <summary>
        /// Gets all variables used by the given procedure
        /// </summary>
        public List<string> GetUsesProc(string procedure)
        {
            return uses.ContainsKey(procedure) ? uses[procedure] : new List<string>();
        }

        /// <summary>
        /// Gets all procedures that use the given variable
        /// </summary>
        public List<string> GetProcUses(string variable)
        {
            return uses.Where(pair => pair.Value.Contains(variable))
                     .Select(pair => pair.Key)
                     .ToList();
        }

        /// <summary>
        /// Gets all variables used by the given statement
        /// </summary>
        public List<string> GetUsesStmt(int stmtNum)
        {
            return usesStmt.ContainsKey(stmtNum) ? usesStmt[stmtNum] : new List<string>();
        }

        /// <summary>
        /// Gets all statements that use the given variable
        /// </summary>
        public List<int> GetStmtUses(string variable)
        {
            return usesStmt.Where(pair => pair.Value.Contains(variable))
                          .Select(pair => pair.Key)
                          .ToList();
        }

        /// <summary>
        /// Gets the statement that follows the given statement
        /// </summary>
        public int GetFollows(int stmtNum)
        {
            return follows.ContainsKey(stmtNum) ? follows[stmtNum] : -1;
        }

        /// <summary>
        /// Gets the statement that the given statement follows
        /// </summary>
        public int GetFollowedBy(int stmtNum)
        {
            return follows.FirstOrDefault(pair => pair.Value == stmtNum).Key;
        }

        /// <summary>
        /// Gets all statements that follow the given statement (transitive)
        /// </summary>
        public List<int> GetFollowsStar(int stmtNum)
        {
            return followsStar.ContainsKey(stmtNum) ? followsStar[stmtNum].ToList() : new List<int>();
        }

        /// <summary>
        /// Gets all statements that the given statement follows (transitive)
        /// </summary>
        public List<int> GetFollowedByStar(int stmtNum)
        {
            return followsStar.Where(pair => pair.Value.Contains(stmtNum))
                             .Select(pair => pair.Key)
                             .ToList();
        }

        /// <summary>
        /// Gets the parent statement of the given statement
        /// </summary>
        public int GetParent(int stmtNum)
        {
            return parent.ContainsKey(stmtNum) ? parent[stmtNum] : -1;
        }

        /// <summary>
        /// Gets all child statements of the given parent statement
        /// </summary>
        public List<int> GetChildren(int stmtNum)
        {
            return parent.Where(pair => pair.Value == stmtNum)
                        .Select(pair => pair.Key)
                        .ToList();
        }

        /// <summary>
        /// Gets all ancestor statements of the given statement (transitive)
        /// </summary>
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

        /// <summary>
        /// Gets all descendant statements of the given statement (transitive)
        /// </summary>
        public List<int> GetChildrenStar(int stmtNum)
        {
            return parentStar.ContainsKey(stmtNum) ? parentStar[stmtNum].ToList() : new List<int>();
        }

        /// <summary>
        /// Gets all procedures called by the given procedure (direct calls)
        /// </summary>
        public List<string> GetCalls(string procedure)
        {
            return calls.ContainsKey(procedure) ? calls[procedure] : new List<string>();
        }

        /// <summary>
        /// Gets all procedures that call the given procedure (direct calls)
        /// </summary>
        public List<string> GetCalledBy(string procedure)
        {
            return calls.Where(pair => pair.Value.Contains(procedure))
                      .Select(pair => pair.Key)
                      .ToList();
        }

        /// <summary>
        /// Gets all procedures called by the given procedure (transitive)
        /// </summary>
        public List<string> GetCallsStar(string procedure)
        {
            return callsStar.ContainsKey(procedure) ? callsStar[procedure].ToList() : new List<string>();
        }

        /// <summary>
        /// Gets all procedures that call the given procedure (transitive)
        /// </summary>
        public List<string> GetCalledByStar(string procedure)
        {
            return callsStar.Where(pair => pair.Value.Contains(procedure))
                           .Select(pair => pair.Key)
                           .ToList();
        }

        /// <summary>
        /// Checks if a procedure modifies a variable
        /// </summary>
        public bool ProcModifies(string procedure, string variable)
        {
            return modifies.ContainsKey(procedure) && modifies[procedure].Contains(variable);
        }

        /// <summary>
        /// Checks if a statement modifies a variable
        /// </summary>
        public bool StmtModifies(int stmtNum, string variable)
        {
            return modifiesStmt.ContainsKey(stmtNum) && modifiesStmt[stmtNum].Contains(variable);
        }

        /// <summary>
        /// Checks if a procedure uses a variable
        /// </summary>
        public bool ProcUses(string procedure, string variable)
        {
            return uses.ContainsKey(procedure) && uses[procedure].Contains(variable);
        }

        /// <summary>
        /// Checks if a statement uses a variable
        /// </summary>
        public bool StmtUses(int stmtNum, string variable)
        {
            return usesStmt.ContainsKey(stmtNum) && usesStmt[stmtNum].Contains(variable);
        }

        /// <summary>
        /// Checks if a statement directly follows another
        /// </summary>
        public bool Follows(int s1, int s2)
        {
            return follows.ContainsKey(s1) && follows[s1] == s2;
        }

        /// <summary>
        /// Checks if a statement transitively follows another
        /// </summary>
        public bool FollowsStar(int s1, int s2)
        {
            return followsStar.ContainsKey(s1) && followsStar[s1].Contains(s2);
        }

        /// <summary>
        /// Checks if a statement is the parent of another
        /// </summary>
        public bool Parent(int s1, int s2)
        {
            return parent.ContainsKey(s2) && parent[s2] == s1;
        }

        /// <summary>
        /// Checks if a statement is an ancestor of another
        /// </summary>
        public bool ParentStar(int s1, int s2)
        {
            return GetParentStar(s2).Contains(s1);
        }

        /// <summary>
        /// Checks if a procedure calls another
        /// </summary>
        public bool Calls(string p1, string p2)
        {
            return calls.ContainsKey(p1) && calls[p1].Contains(p2);
        }

        /// <summary>
        /// Checks if a procedure transitively calls another
        /// </summary>
        public bool CallsStar(string p1, string p2)
        {
            return callsStar.ContainsKey(p1) && callsStar[p1].Contains(p2);
        }

        /// <summary>
        /// Gets the node for a specific statement
        /// </summary>
        public TNode? GetStatementNode(int stmtNum)
        {
            return statements.ContainsKey(stmtNum) ? statements[stmtNum] : null;
        }

        /// <summary>
        /// Gets the statement type for a specific statement
        /// </summary>
        public TType? GetStatementType(int stmtNum)
        {
            return statements.ContainsKey(stmtNum) ? statements[stmtNum].getType() : null;
        }
    }
}