using System.Text.RegularExpressions;
using static aitsi.QueryPreProcessor;
using static aitsi.QueryAssignmentsValidator;
using static aitsi.QueryProcessor.QueryValidator;
class Program { 
    static void Main(String[] args)
    {
        try
        {
            Console.WriteLine("Proszę podać zapytanie: ");
            string query = Console.ReadLine();
            var queryParts = Regex.Split(query, @"(?=select)", RegexOptions.IgnoreCase);
            if (queryParts.Length < 2)
                throw new Exception("Brak słowa 'select' w zapytaniu.");

            Console.WriteLine(evaluateAssignments(queryParts[0].Trim()));
            Console.WriteLine(evaluateQuery(queryParts[1].Trim()));
            QueryNode PQLTree = Parse(query.Trim());
            DrawTree(PQLTree);
            Console.WriteLine(evaluateQueryLogic(PQLTree));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }
}
