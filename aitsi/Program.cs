using aitsi;
using static aitsi.QueryPreProcessor;

class Program { 
    static void Main(String[] args)
    {
        string query = Console.ReadLine();
        QueryNode PQLTree = Parse(query);
        DrawTree(PQLTree);
    }
}
