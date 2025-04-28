using System.Text.RegularExpressions;

namespace aitsi
{
    static class QueryPreProcessor
    {
        public static string[] allowedRelRefs = ["modifies", "uses", "parent", "parent*", "follows", "follows*", "calls", "calls*"];
        public static string[] declarationTypes = ["stmt", "assign", "while", "if", "variable", "constant", "prog_line"];

        public static string evaluateAssignments(string assignments)
        {
            if (string.IsNullOrWhiteSpace(assignments))
                throw new Exception("Nie podano deklaracji.");

            if (!assignments.EndsWith(';')) throw new Exception("Nie zakończono poprawnie deklaracji.");

            string[] declarations = assignments.Split(';', StringSplitOptions.RemoveEmptyEntries);
            Regex identPattern = new Regex(@"^[A-Za-z][A-Za-z0-9#]*$");

            foreach (string declaration in declarations)
            {
                if (string.IsNullOrEmpty(declaration.Trim()))
                    continue;

                string[] parts = declaration.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    throw new Exception($"Niepełna deklaracja: '{declaration.Trim()}' (brakuje identyfikatorów)");

                if (!declarationTypes.Contains(parts[0]))
                    throw new Exception($"Niepoprawny typ encji: '{parts[0]}' w deklaracji: '{declaration.Trim()}'");

                string[] synonyms = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in synonyms)
                {
                    string synonym = s.Trim();
                    if (!identPattern.IsMatch(synonym) || declarationTypes.Contains(synonym))
                        throw new Exception($"Niepoprawna nazwa identyfikatora: '{synonym}' w deklaracji: '{declaration.Trim()}'. (dozwolone tylko litery, cyfry i '#'. Pierwszy znak musi być literą)");
                }
            }

            return "Deklaracja poprawna.";
        }

        public static string evaluateQuery(string query)
        {
            if (query == null || query == "") return "Nie podano zapytania.";

            var matches = Regex.Matches(query, @"\"".*?\""|\w+(?:\.\w+)*[#*]?|[^\s\w]");
            string[] queryParts = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++) queryParts[i] += matches[i].Value;

            //foreach (var item in queryParts) Console.WriteLine(item);

            validateIfStartsWithSelect(queryParts[0]);
            string previous = "";

            for (int i = 2; i < queryParts.Length; i++)
            {
                switch (queryParts[i].Trim().ToLower())
                {
                    case "such":
                        if (queryParts[++i].Trim().ToLower() != "that") throw new Exception("Po 'such' nie wystąpiło 'that'.");
                        i += validateSuchThat(queryParts.Skip(i + 1).ToArray()) + 1;
                        previous = "such that";
                        break;
                    case "with":
                        validateWith(queryParts.Skip(i + 1).ToArray());
                        i += 5;
                        previous = "with";
                        break;
                    case "and":
                        switch (previous)
                        {
                            case "": throw new Exception("Podano wartość 'and' przed klauzulami 'such that' bądź 'with'.");
                            case "such that":
                                i += validateSuchThat(queryParts.Skip(i + 1).ToArray()) + 1;
                                break;
                            case "with":
                                validateWith(queryParts.Skip(i + 1).ToArray());
                                i += 5;
                                break;
                            default: throw new Exception("Wystąpił nieznany błąd. Funkcja: evaluateQuery");
                        }
                        break;
                    default:
                        throw new Exception("Zapytanie ma niepoprawną składnię. W miejscu such that lub with, wystąpiło: " + queryParts[i]);
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
            if (suchThat[1].Trim() != "(") throw new Exception("Nie podano nawiasu otwierającego po relacji.");

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
            var leftSide = with[0].Split('.');
            if (leftSide.Length != 2) throw new Exception("Lewa strona równania w 'with' nie posiada znaku '.'.");
            if (with[1].Trim() != "=") throw new Exception("Niepoprawna składnia 'with'. Zabrakło znaku '='.");
            return true;
        }

        public static QueryNode Parse(string input)
        {
            var query = new QueryNode();
            var lines = input.Split(';');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (declarationTypes.Any(k => trimmed.StartsWith(k)))
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
                    }
                }
                else if (trimmed.StartsWith("Select"))
                {
                    var match = Regex.Match(trimmed, @"Select\s+(\w+)\s+(.+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var selectVar = match.Groups[1].Value;
                        var remainingPart = match.Groups[2].Value;
                        var selectNode = new SelectNode(selectVar);
                        while (!string.IsNullOrEmpty(remainingPart))
                        {
                            if (remainingPart.StartsWith("such that", StringComparison.OrdinalIgnoreCase))
                            {
                                remainingPart = remainingPart.Substring(9).Trim();
                                var clauseMatch = Regex.Match(remainingPart, @"([\w\*]+)\s*\(([^,]+),\s*([^)]+)\)", RegexOptions.IgnoreCase);
                                if (clauseMatch.Success)
                                {
                                    var relation = clauseMatch.Groups[1].Value.Trim();
                                    var left = clauseMatch.Groups[2].Value.Trim();
                                    var right = clauseMatch.Groups[3].Value.Trim();
                                    var clauseNode = new ClauseNode(relation, left, right);
                                    selectNode.addChild(clauseNode);
                                    int Index = clauseMatch.Index + clauseMatch.Length;
                                    remainingPart = remainingPart.Substring(Index).Trim();
                                }
                            }
                            else if (remainingPart.StartsWith("with", StringComparison.OrdinalIgnoreCase))
                            {
                                remainingPart = remainingPart.Substring(4).Trim();
                                var withMatch = Regex.Match(remainingPart, @"(\w+\.\w+)\s*=\s*(\w+|""[^""]+"")", RegexOptions.IgnoreCase);
                                if (withMatch.Success)
                                {
                                    var lefta = withMatch.Groups[1].Value.Trim();
                                    var righta = withMatch.Groups[2].Value.Trim();
                                    var withNode = new WithNode(lefta, righta);
                                    selectNode.addChild(withNode);
                                    int Index = withMatch.Index + withMatch.Length;
                                    remainingPart = remainingPart.Substring(Index).Trim();
                                }
                            }
                            else if (remainingPart.StartsWith("and", StringComparison.OrdinalIgnoreCase))
                            {
                                remainingPart = remainingPart.Substring(3).Trim();
                                var clauseMatch = Regex.Match(remainingPart, @"([\w\*]+)\s*\(([^,]+),\s*([^)]+)\)", RegexOptions.IgnoreCase);
                                if (clauseMatch.Success)
                                {
                                    var relation = clauseMatch.Groups[1].Value.Trim();
                                    var left = clauseMatch.Groups[2].Value.Trim();
                                    var right = clauseMatch.Groups[3].Value.Trim();
                                    var clauseNode = new ClauseNode(relation, left, right);
                                    selectNode.addChild(clauseNode);
                                    int Index = clauseMatch.Index + clauseMatch.Length;
                                    remainingPart = remainingPart.Substring(Index).Trim();
                                }
                            }
                        }
                        selectNode.parent = query;
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

        public static List<WithNode> ParseWiths(string withPart)
        {
            var withList = new List<WithNode>();
            var parts = withPart.Split("and", StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var values = part.Split("=");
                withList.Add(new WithNode
                (
                    values[0].Trim(),
                    values[1].Trim()
                ));
            }

            return withList;
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
    }
}