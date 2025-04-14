using aitsi;
using static aitsi.QueryPreProcessor;

class Program { 
    static void Main(String[] args)
    {
        Console.WriteLine("Proszę podać deklaracje zmiennych:");
        string assignments = Console.ReadLine();
        Console.WriteLine(QueryAssignmentsValidator.evaluateAssignments(assignments));

        Console.WriteLine("Proszę podać zapytanie:");
        string query = Console.ReadLine();
        Console.WriteLine(QueryPreProcessor.evaluateQuery(query));
        QueryNode PQLTree = QueryPreProcessor.Parse(query);
    }
}
