namespace aitsi
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
    }

    class varTable
    {
        public int insertVar(string varName)
        {
            return 1;
        }

        public string getVarName(int index)
        {

        }

        public int getVarIndex(string varName) { }

        public int getSize() { }

        public bool isIn(string varName) { }
    }

    class Ast
    {
        public TNode createTNode(TType type) { }

        public void setRoot(TNode node) { }

        public void setAttr(TNode node, string attr) { }

        public void setFirstChild(TNode p, TNode c) { }

        public void setChildOfLink(TNode c, TNode p) { }

        public TNode getRoot() { }

        public TType getType(TNode node) { }

        public string getAttr(TNode node) { }

        public TNode getFirstChild(TNode p) { }

        public void setParent(TNode p, TNode c) { }

        public TNode getParent(TNode c) { }

        public TNode getParentStar(TNode c) { }

        public void setFollows(TNode p, TNode c) { }

        public TNode getFollows(TNode n) { }

    }

    class Modifies
    {
        public void setModifies(TNode node, TNode var) { }

        List<TNode> getModified(TNode node) { }

        bool isModified(TNode var,TNode node) {  }
    }

    class Parent {
    void setParent(TNode p,TNode c) { }
    List<TNode> getChildren(TNode node) { }

    bool isParent(TNode p,TNode c) { }

    }

    class Follows {
        void setFollows(TNode n1, TNode n2) { }
        List<TNode> getFollowed(TNode node) { }

        bool isFollowed(TNode n1, TNode n2) { }
    }

    class Uses {
        void setUses(TNode p, TNode c) { }
        List<TNode> getUsed(TNode node) { }

        bool isUsed(TNode p, TNode c) { }
    }

    class Calls {
        void setCalls(TNode p, TNode c) { }
        List<TNode> getCalled(TNode node) { }

        bool isCalled(TNode p, TNode c) { }
    }

    class Proc {
        public int insertProc(string varName)
        {
            return 1;
        }

        public string getProcName(int index)
        {

        }

        public int getProcIndex(string varName) { }

        public int getSize() { }

        public bool isIn(string varName) { }
    }
}