using System.Text.RegularExpressions;

namespace aitsi
{
	static class QueryPreProcessor
	{
		public static Dictionary<string, List<string>> assignmentsList = new Dictionary<string, List<string>>();
        public static string[] allowedRelRefs = ["modifies", "uses", "parent", "follows", "parent*", "follows*"];
		public static string evaluateQuery(string query)
		{
			if (query == null || query == "") return "Nie podano zapytania.";
			try
			{
                var matches = Regex.Matches(query, @"\"".*?\""|\w+[#*]?|[^\s\w]");
                string[] queryParts = new string[matches.Count];

                for (int i = 0; i < matches.Count; i++)queryParts[i] += matches[i].Value;

                //foreach (var item in queryParts) Console.WriteLine(item);

                validateIfStartsWithSelect(queryParts[0]);

                for (int i = 2; i < queryParts.Length; i++)
                {
                    switch (queryParts[i].Trim().ToLower())
                    {
                        case "such":
                            if (queryParts[++i].Trim().ToLower() != "that") throw new Exception("Po 'such' nie wyst¹pi³o 'that'.");
                            i += validateSuchThat(queryParts.Skip(i + 1).ToArray()) + 1;
                            break;
                        case "with":
                            validateWith(queryParts.Skip(i + 1).ToArray());
                            i += 5;
                            break;
                        default:
                            return "Zapytanie ma niepoprawn¹ sk³adniê. W miejscu such that lub with, wyst¹pi³o: " + queryParts[i];
                    }
                }         
            }
            catch (Exception e)
			{
                return e.ToString();
            }

			return "Podane zapytanie jest poprawne sk³adniowo.";
		}

		private static bool validateIfStartsWithSelect(string firstValue)
		{
            if (firstValue.Trim().ToLower() != "select") throw new Exception("Zapytanie nie rozpoczêto od 'Select'.");
            return true;
		}

        private static int validateSuchThat(string[] suchThat)
        {
            if (!allowedRelRefs.Contains(suchThat[0].Trim().ToLower())) throw new Exception("Podano nieodpowiedni¹ wartoœæ po 'such that': " + suchThat[0]);
            if (suchThat[1].Trim() != "(") throw new Exception("Nie podano nawiasu otwieraj¹cego po rederencji.");

            for (int i = 2; i < suchThat.Length; i++)
            {
                if (suchThat[i].Trim() == ")")
                {
                    if (i == 2) throw new Exception("Nie podano wartoœci do sprawdzenia w referencji.");
                    else return i;
                }              
            }
            throw new Exception("Nie zamkniêto nawiasu po referencji.");            
        }

        private static bool validateWith(string[] with)
        {
            if (with[1].Trim() != ".") throw new Exception("Niepoprawna sk³adnia 'with'. Zabrak³o znaku '.' pomiêdzy synonimem a nazw¹ atrybutu.");
            if (with[3].Trim() != "=") throw new Exception("Niepoprawna sk³adnia 'with'. Zabrak³o znaku '='.");
            return true;
        }
    }
}