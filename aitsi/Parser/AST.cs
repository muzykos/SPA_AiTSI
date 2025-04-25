using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aitsi.Parser
{
    class AST : IAST
    {
        private TNode root;
        //private HashSet<TNode> nodes = new();
        public AST() { }

        public TNode createTNode(TType nodeType, string attr, int stmtNumber)
        {
            TNode node = new();
            node.setType(nodeType);
            node.setAttr(attr);
            return node;
        }

        public void setRoot(TNode node)
        {
            this.root = node;
        }

        public TNode getRoot()
        {
            return this.root;
        }

        public void setAttr(TNode node, string attr)
        {
            node.setAttr(attr);
        }

        public string getAttr(TNode node)
        {
            return node.getAttr();
        }

        public TType getType(TNode node)
        {
            return node.getType();
        }

        public bool setChild(TNode parent, TNode child)
        {
            parent.addChild(child);
            child.setParent(parent);
            return true;
        }

        public List<TNode> getChildren(TNode parent)
        {
            return parent.getChildren();
        }

        public bool isParent(TNode parent, TNode child)
        {
            return (parent.getChildren().Contains(child));
        }

        public void setFollows(TNode a, TNode b)
        {
            a.setFollows(b);
        }

        public TNode getFollows(TNode a)
        {
            return a.getFollows();
        }

        public bool isFollowed(TNode a, TNode b)
        {
            return a.getFollows() == b;
        }

        public TNode getParent(TNode child)
        {
            return child.getParent();
        }

        public static void PrintAST(TNode node, int indent)
        {
            Console.WriteLine(new string(' ', indent * 2) + $"{node.getType()} ({node.getAttr()})");

            foreach (var child in node.getChildren())
            {
                PrintAST(child, indent + 1);
            }

            var follows = node.getFollows();
            if (follows != null)
            {
                Console.WriteLine(new string(' ', indent * 2) + $"FOLLOWS -> {follows.getType()} ({follows.getAttr()})");
            }
        }
    }
}
