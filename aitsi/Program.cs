using System.Text.RegularExpressions;
using static aitsi.QueryPreProcessor;
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
            QueryNode PQLTree = Parse(query.Trim());
            DrawTree(PQLTree);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }
}
