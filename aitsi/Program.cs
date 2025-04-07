using aitsi;

class Program { 
    static void Main(String[] args)
    {
        Console.WriteLine("Proszę podać deklaracje zmiennych:");
        string assignments = Console.ReadLine();
        Console.WriteLine(QueryProcessor.evaluateAssignments(assignments));

        Console.WriteLine("Proszę podać zapytanie:");
        string query = Console.ReadLine();
        Console.WriteLine(QueryProcessor.evaluateQuery(query));
    }
}
