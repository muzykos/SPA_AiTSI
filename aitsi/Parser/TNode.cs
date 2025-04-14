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
        private TNode parentNode;
        private List<TNode> children;


        public void SetType (TType type)
        {
            this.type = type;
        }
     }
}
