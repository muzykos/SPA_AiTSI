namespace aitsi.Parser
{
    class Lexer
    {
        private string text;
        private int pos = 0;
        private char currentChar;

        public Lexer(string text)
        {
            this.text = text;
            this.currentChar = text.Length > 0 ? text[0] : '\0';
        }

        private void advance()
        {
            pos++;
            currentChar = pos < text.Length ? text[pos] : '\0';
        }

        private void skipWhiteSpace()
        {
            while (char.IsWhiteSpace(currentChar))
            {
                advance();
            }
        }

        private string name()
        {
            var result = "";
            while (char.IsLetterOrDigit(currentChar))
            {
                result += currentChar;
                advance();
            }
            return result;
        }

        private string number()
        {
            var result = "";
            while (char.IsDigit(currentChar))
            {
                result += currentChar;
                advance();
            }
            return result;
        }

        public TNode getNextNode()
        {
            skipWhiteSpace();

            if (currentChar == '\0')
            {
                TNode tNode = new TNode();
                tNode.setType(TType.EOF);
                tNode.setAttr("");
                return tNode;
            }

            if (char.IsLetter(currentChar))
            {
                var name = this.name();
                if (name == "procedure") return new TNode(TType.Procedure, name);
                if (name == "while") return new TNode(TType.While, name);
                if (name == "call") return new TNode(TType.Call,name);
                return new TNode(TType.Name, name);
            }

            if (char.IsDigit(currentChar))
            {
                return new TNode(TType.Constant, number());
            }

            switch (currentChar)
            {
                case '=':
                    advance();
                    return new TNode(TType.Assign,"=");
                case '+':
                    advance();
                    return new TNode(TType.Plus,"+");
                case '{':
                    advance();
                    return new TNode(TType.LBrace,"{");
                case '}':
                    advance();
                    return new TNode(TType.RBrace,"}");
                case ';':
                    advance();
                    return new TNode(TType.SemiColon,";");
            }

            throw new Exception($"Unexpected character: {currentChar}");
        }
    }
}