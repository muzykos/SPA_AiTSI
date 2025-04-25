using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aitsi.Parser
{
    public class TNode
    {
        private TType type;
        private int stmtNumber;
        private string attr;
        private TNode? parentNode;
        private List<TNode> children = [];
        private TNode? follows;

        public TNode()
        {
            this.type = default;
            this.attr = "";
        }

        public TNode(TType type, string attr)
        {
            this.type = type;
            this.attr = attr;
        }

        public void setType(TType type)
        {
            this.type = type;
        }

        public TType getType()
        {
            return this.type;
        }

        public void setAttr(string attr)
        {
            this.attr = attr;
        }

        public string getAttr()
        {
            return this.attr;
        }

        public void addChild(TNode child)
        {
            children.Add(child);
        }

        public void setParent(TNode parent)
        {
            parentNode = parent;
        }

        public List<TNode> getChildren()
        {
            return children;
        }

        public void setFollows(TNode b)
        {
            follows = b;
        }

        public TNode? getFollows()
        {
            return follows;
        }

        public TNode? getParent()
        {
            return parentNode;
        }
    }
}
