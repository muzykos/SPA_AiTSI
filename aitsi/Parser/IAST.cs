using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aitsi.Parser
{
    public interface IAST
    {
        TNode createTNode(TType nodeType, string attr, int stmtNumber);

        void setRoot(TNode node);
        TNode getRoot();

        void setAttr(TNode node, string attr);
        string getAttr(TNode node);

        TType getType(TNode node);

        bool setChild(TNode parent, TNode child);
        List<TNode> getChildren(TNode parent);
        bool isParent(TNode parent, TNode child);

        void setFollows(TNode a, TNode b);
        TNode getFollows(TNode a);
        bool isFollowed(TNode a, TNode b);

        TNode getParent(TNode child);
    }
}
