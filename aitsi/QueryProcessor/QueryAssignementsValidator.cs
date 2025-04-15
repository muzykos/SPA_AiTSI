using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace aitsi
{
	static class QueryAssignmentsValidator
    {
        public static string[] allowedValuesInAssignments = ["stmt", "assign", "while", "if", "variable", "constant", "prog_line"];

        public static string evaluateAssignments(string assignments)
        {
            if (assignments == null || assignments.Length == 0) return "Nie podano deklaracji.";
            var matches = Regex.Matches(assignments, @"\w+|[^\s\w]");
            string[] assignmentsParts = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                assignmentsParts[i] = matches[i].Value;
            }
 
            checkDuplicates(assignmentsParts);

            for (int i = 0; i < assignmentsParts.Length; i++)
            {
                if (allowedValuesInAssignments.Contains(assignmentsParts[i].ToLower()))
                {

                    if (!QueryPreProcessor.assignmentsList.ContainsKey(assignmentsParts[i].ToLower()))
                    {
                        List<string> list = new List<string>();
                        string tempKey = assignmentsParts[i++].ToLower();
                        do
                        {
                            if (i >= assignmentsParts.Length) throw new Exception("B³êdnie zakoñczono deklaracje.");
                            if (allowedValuesInAssignments.Contains(assignmentsParts[i])) throw new Exception("Nieodpowiedni szyk. Typ wartoœci nie powinien siê tu znaleŸæ. Typ: " + assignmentsParts[i]);
                            if (assignmentsParts[i] == ",") continue;
                            if (assignmentsParts[i] == ";") throw new Exception("Nieodpowiedni szyk. Znak ';' nie powinien siê tu znaleŸæ.");
                            list.Add(string.Concat(assignmentsParts[i].Trim()));
                            checkDuplicates(list.ToArray());
                        } while (!assignmentsParts[++i].Contains(';'));
                        QueryPreProcessor.assignmentsList.Add(tempKey, list);
                    }
                    else
                    {
                        if (QueryPreProcessor.assignmentsList.TryGetValue(assignmentsParts[i], out var list))
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
            
            return returnResponse();
        }

        private static string returnResponse()
        {
            string response = "";
            foreach (string key in QueryPreProcessor.assignmentsList.Keys)
            {
                response += key + ":\n\t" + QueryPreProcessor.assignmentsList[key].Aggregate((current, next) => current + ", " + next) + "\n\n";
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
                if (!allowedValuesInAssignments.Contains(d) && !d.Trim().Equals(";") && !d.Trim().Equals(",")) throw new Exception("Podano duplikaty nazw zmiennych. Duplikat: " + d);
            
        }
    }
}