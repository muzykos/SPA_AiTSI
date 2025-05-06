using aitsi;
using aitsi.Parser;

class Program
{
    static void Main(String[] args)
    {
        string filePath = "/home/muzykos-laptop/Projects/SPA_AiTSI/aitsi/program.txt"; // Ensure this file exists in the working directory
        string source = File.ReadAllText(filePath);

        var lexer = new Lexer(source);
        var ast = new AST();
        var parser = new Parser(lexer, ast);

        try
        {
            parser.parseProgram();
            Console.WriteLine("Parsing completed successfully!\n");
            AST.PrintAST(ast.getRoot(), 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parsing error: {ex.Message}");
        }

        // Console.WriteLine("Proszę podać deklaracje zmiennych:");
        // string assignments = Console.ReadLine();
        // Console.WriteLine(QueryValidator.evaluateAssignments(assignments));

        // Console.WriteLine("Proszę podać zapytanie:");
        // string query = Console.ReadLine();
        // Console.WriteLine(QueryProcessor.evaluateQuery(query));
    }

}
