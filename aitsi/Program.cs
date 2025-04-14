using System.Text.RegularExpressions;
using static aitsi.QueryPreProcessor;
using static aitsi.QueryAssignmentsValidator;

class Program { 
    static void Main(String[] args)
    {
        try
        {
            Console.WriteLine("Proszę podać zapytanie: ");
            string query = Console.ReadLine();
            var queryParts = Regex.Split(query, "select", RegexOptions.IgnoreCase);
            Console.WriteLine(evaluateAssignments(queryParts[0]));
            Console.WriteLine(evaluateQuery("Select" + queryParts[1]));
            QueryNode PQLTree = Parse(queryParts[0] + queryParts[1]);
            DrawTree(PQLTree);
        }catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }
}
