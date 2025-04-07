namespace aitsi
{
	static class QueryProcessor
	{
		public static Dictionary<string, List<string>> assignmentsList = new Dictionary<string, List<string>>();
        public static string[] allowedValuesInAssignments = ["stmt", "assign", "while", "if"];
        public static string[] allowedValuesInQueries = ["boolean"];

        public static string evaluateAssignments(string assignments)
		{
			if (assignments == null || assignments.Length == 0) return "Nie podano deklaracji.";
			string[] assignmentsParts = assignments.Split(' ');

			if (checkDuplicates(assignmentsParts)) return "";
            try
			{
				for (int i = 0; i < assignmentsParts.Length; i++)
				{
					if (allowedValuesInAssignments.Contains(assignmentsParts[i].ToLower()))
					{

                        if (!assignmentsList.ContainsKey(assignmentsParts[i].ToLower()))
						{
							List<string> list = new List<string>();
							string tempKey = assignmentsParts[i].ToLower();
							do
							{
								++i;
								if (i >= assignmentsParts.Length) throw new Exception("B³êdnie zakoñczono deklaracje.");
								if (allowedValuesInAssignments.Contains(assignmentsParts[i])) throw new Exception("Nieodpowiedni szyk. Typ wartoœci nie powinien siê tu znaleŸæ. Typ: " + assignmentsParts[i]);
								list.Add(string.Concat(assignmentsParts[i].Trim().Split(';', ',')));
							} while (!assignmentsParts[i].Contains(';'));
							assignmentsList.Add(tempKey, list);
						}
						else
						{
                            if (assignmentsList.TryGetValue(assignmentsParts[i], out var list))
							{                          
								do
								{
									++i;
									if (i >= assignmentsParts.Length) throw new Exception("B³êdnie zakoñczono deklaracje.");
									if (allowedValuesInAssignments.Contains(assignmentsParts[i])) throw new Exception("Nieodpowiedni szyk. Typ wartoœci nie powinien siê tu znaleŸæ. Typ: " + assignmentsParts[i]);
									list.Add(string.Concat(assignmentsParts[i].Trim().Split(';', ',')));
								} while (!assignmentsParts[i].Contains(';'));                         
							}
							else throw new Exception("Nierozpoznany b³¹d sk³adni: " + assignmentsParts[i]);
                        }
					}
                    else throw new Exception("Nierozpoznany typ zmiennej: " + assignmentsParts[i]);
                }
            } catch (Exception e)
			{
				return e.ToString();
			}

			string response = "";
			foreach(string key in assignmentsList.Keys)
			{
				response += key + ":\n\t" + assignmentsList[key].Aggregate((current, next) => current + ", " + next) + "\n\n";
			}
			return response;
		}

		public static string evaluateQuery(string query)
		{
			if (query == null || query == "") return "Nie podano zapytania.";
			else
			{
				try
				{
                    string[] queryParts = query.Split(' ');

                    if (queryParts[0].ToLower() != "select") throw new Exception("Zapytanie nie rozpoczêto od 'Select'.");
                    bool has = false;
					if (allowedValuesInQueries.Contains(queryParts[1])) has = true;
					else 
					{
						foreach (string key in assignmentsList.Keys)
						{
							if (assignmentsList[key].Contains(queryParts[1]))
							{
								has = true;
								break;
							}
						} 
					}
					if (!has) throw new Exception("Podano nieprawid³ow¹ wartoœæ do zwrócenia. Podana wartoœæ: " + queryParts[1]);

                }
                catch(Exception e)
				{
                    return e.ToString();
                }
			}
			return "Podane zapytanie jest poprawne.";
		}

		private static bool checkDuplicates(string[] text)
		{
            var duplicates = text
                .GroupBy(i => i)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key);
			foreach (var d in duplicates)
				if (!allowedValuesInAssignments.Contains(d)) { 
					Console.WriteLine("Podano duplikaty nazw zmiennych. Duplikat: " + d);
					return true; 
				}
            return false;
		} 

	}
}