using System.Text.RegularExpressions;

namespace aitsi
{
    static class QueryPreProcessor
    {
        public static Dictionary<string, List<string>> assignmentsList = new Dictionary<string, List<string>>();
        public static string[] allowedRelRefs = ["modifies", "uses", "parent", "follows", "parent*", "follows*"];
        public static string[] DeclarationTypes = ["stmt", "assign", "while", "if", "variable", "constant", "prog_line"];

        public static QueryNode Parse(string input)
        {
            var query = new QueryNode();
            var lines = input.Split(';');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (DeclarationTypes.Any(k => trimmed.StartsWith(k)))
                {
                    var match = Regex.Match(trimmed, @"(\w+)\s+([\w,\s]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var group2Value = match.Groups[2].Value;
                        var variables = new List<string>(group2Value.Split(',', StringSplitOptions.TrimEntries).Where(v => !string.IsNullOrWhiteSpace(v))
                        );
                        var declaration = new DeclarationNode
                        (                     
                            match.Groups[1].Value,
                            variables
                        );
                        query.addChild(declaration);
                        //query.Children.Add(declaration);
                    }
                }
                else if (trimmed.StartsWith("Select"))
                {
                    var match = Regex.Match(trimmed, @"Select\s+(\w+)\s+such that\s+(.+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var selectVar = match.Groups[1].Value;
                        var clausePart = match.Groups[2].Value;

                        var clauses = ParseClauses(clausePart);

                        var selectNode = new SelectNode
                        (
                            selectVar,
                            clauses
                        );
                       
                        query.addChild(selectNode);
                    }
                }
            }
            return query;
        }

        public static List<ClauseNode> ParseClauses(string clausePart)
        {
            var clauseList = new List<ClauseNode>();
            var parts = clausePart.Split("and", StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var match = Regex.Match(part, @"([\w\*]+)\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)");
                if (match.Success)
                {
                    clauseList.Add(new ClauseNode
                    (
                        match.Groups[1].Value,
                        match.Groups[2].Value,
                        match.Groups[3].Value
                    ));
                }
            }
            return clauseList;
        }
        public static void DrawTree(Node node, string indent = "", bool isLast = true)
        {
            Console.Write(indent);
            Console.Write(isLast ? "└── " : "├── ");
            Console.WriteLine(node.name);

            indent += isLast ? "    " : "│   ";

            for (int i = 0; i < node.children.Count; i++)
            {
                DrawTree(node.children[i], indent, i == node.children.Count - 1);
            }
        }

        public static string evaluateQuery(string query)
        {
            if (query == null || query == "") return "Nie podano zapytania.";
            
            var matches = Regex.Matches(query, @"\"".*?\""|\w+[#*]?|[^\s\w]");
            string[] queryParts = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++) queryParts[i] += matches[i].Value;

            //foreach (var item in queryParts) Console.WriteLine(item);

            validateIfStartsWithSelect(queryParts[0]);

            for (int i = 2; i < queryParts.Length; i++)
            {
                switch (queryParts[i].Trim().ToLower())
                {
                    case "such":
                        if (queryParts[++i].Trim().ToLower() != "that") throw new Exception("Po 'such' nie wystąpiło 'that'.");
                        i += validateSuchThat(queryParts.Skip(i + 1).ToArray()) + 1;
                        break;
                    case "with":
                        validateWith(queryParts.Skip(i + 1).ToArray());
                        i += 5;
                        break;
                    default:
                        return "Zapytanie ma niepoprawną składnię. W miejscu such that lub with, wystąpiło: " + queryParts[i];
                }
            }

            return "Podane zapytanie jest poprawne składniowo.";
        }

        private static bool validateIfStartsWithSelect(string firstValue)
        {
            if (firstValue.Trim().ToLower() != "select") throw new Exception("Zapytanie nie rozpoczęto od 'Select'.");
            return true;
        }

        private static int validateSuchThat(string[] suchThat)
        {
            if (!allowedRelRefs.Contains(suchThat[0].Trim().ToLower())) throw new Exception("Podano nieodpowiednią wartość po 'such that': " + suchThat[0]);
            if (suchThat[1].Trim() != "(") throw new Exception("Nie podano nawiasu otwierającego po rederencji.");

            for (int i = 2; i < suchThat.Length; i++)
            {
                if (suchThat[i].Trim() == ")")
                {
                    if (i == 2) throw new Exception("Nie podano wartości do sprawdzenia w referencji.");
                    else return i;
                }
            }
            throw new Exception("Nie zamknięto nawiasu po referencji.");
        }

        private static bool validateWith(string[] with)
        {
            if (with[1].Trim() != ".") throw new Exception("Niepoprawna składnia 'with'. Zabrakło znaku '.' pomiędzy synonimem a nazwą atrybutu.");
            if (with[3].Trim() != "=") throw new Exception("Niepoprawna składnia 'with'. Zabrakło znaku '='.");
            return true;
        }
    }
}