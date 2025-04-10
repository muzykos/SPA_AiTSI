namespace aitsi
{
	static class QueryProcessor
	{
		public static Dictionary<string, List<string>> assignmentsList = new Dictionary<string, List<string>>();
        public static string[] allowedValuesInReturnParameter = ["boolean"];

		public static string evaluateQuery(string query)
		{
			if (query == null || query == "") return "Nie podano zapytania.";
			else
			{
				try
				{
                    string[] queryParts = query.Split(' ');
                    validateIfStartsWithSelect(queryParts[0]);
                    validateReturnParameter(queryParts[1]);

                    if(queryParts.Length > 1)
                    switch (queryParts[2])
                    {
                        case "such":
                            if(queryParts[3] != "that") throw new Exception("Po such nie wystąpiło 'that'.");
                            validateSuchThat("");
                            break;
                        case "with":
                            validateWith("");
                            break;
                        default:
                            return "Zapytanie ma niepoprawną składnię. W miejscu such that lub with, wystąpiło: " + queryParts[2];
                    }
                }
                catch(Exception e)
				{
                    return e.ToString();
                }
			}
			return "Podane zapytanie jest poprawne.";
		}

		private static bool validateIfStartsWithSelect(string firstValue)
		{
            if (firstValue.ToLower() != "select") throw new Exception("Zapytanie nie rozpoczęto od 'Select'.");
            return true;
		}

        private static bool validateReturnParameter(string returnParameter)
        {
            if (allowedValuesInReturnParameter.Contains(returnParameter)) return true;

            else            
                foreach (string key in assignmentsList.Keys)
                    if (assignmentsList[key].Contains(returnParameter)) return true;              
            
            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + returnParameter);
        }

        private static bool validateSuchThat(string suchThat)
        {
            throw new Exception("Niezaimplementowana funkcja validateSuchThat");
        }

        private static bool validateWith(string with)
        {
            throw new Exception("Niezaimplementowana funkcja validateWith");
        }

    }
}