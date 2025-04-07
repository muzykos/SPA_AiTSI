namespace aitsi
{
	static class QueryValidator
    {
        public static string[] allowedValuesInAssignments = ["stmt", "assign", "while", "if"];

        public static string evaluateAssignments(string assignments)
        {
            if (assignments == null || assignments.Length == 0) return "Nie podano deklaracji.";
            string[] assignmentsParts = assignments.Split(' ');

            try
            {
                checkDuplicates(assignmentsParts);

                for (int i = 0; i < assignmentsParts.Length; i++)
                {
                    if (allowedValuesInAssignments.Contains(assignmentsParts[i].ToLower()))
                    {

                        if (!QueryProcessor.assignmentsList.ContainsKey(assignmentsParts[i].ToLower()))
                        {
                            List<string> list = new List<string>();
                            string tempKey = assignmentsParts[i].ToLower();
                            do
                            {
                                ++i;
                                if (i >= assignmentsParts.Length) throw new Exception("B³êdnie zakoñczono deklaracje.");
                                if (allowedValuesInAssignments.Contains(assignmentsParts[i])) throw new Exception("Nieodpowiedni szyk. Typ wartoœci nie powinien siê tu znaleŸæ. Typ: " + assignmentsParts[i]);
                                list.Add(string.Concat(assignmentsParts[i].Trim().Split(';', ',')));
                                checkDuplicates(list.ToArray());
                            } while (!assignmentsParts[i].Contains(';'));
                            QueryProcessor.assignmentsList.Add(tempKey, list);
                        }
                        else
                        {
                            if (QueryProcessor.assignmentsList.TryGetValue(assignmentsParts[i], out var list))
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
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            string response = "";
            foreach (string key in QueryProcessor.assignmentsList.Keys)
            {
                response += key + ":\n\t" + QueryProcessor.assignmentsList[key].Aggregate((current, next) => current + ", " + next) + "\n\n";
            }
            return response;
        }

        private static void checkDuplicates(string[] text)
        {
            var duplicates = text
                .GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            foreach (var d in duplicates)
                if (!allowedValuesInAssignments.Contains(d)) throw new Exception("Podano duplikaty nazw zmiennych. Duplikat: " + d);
        }
    }
}