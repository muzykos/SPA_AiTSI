using aitsi;
using aitsi.Parser;
using aitsi.PKB;

class Program
{
    static void Main(String[] args)
    {
        string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        string filePath = Path.Combine(projectPath, "program.txt");
        string source = File.ReadAllText(filePath);

        var lexer = new Lexer(source);
        var ast = new AST();
        var parser = new Parser(lexer, ast);
        var pkb = new PKB(ast);



        try
        {
            parser.parseProgram();
            Console.WriteLine("Parsing completed successfully!\n");
            var path = Path.Combine(projectPath, "ast_output.txt");
            using (var writer = new StreamWriter(path))
            {
                AST.PrintAST(ast.getRoot(), 0, writer);
            }

            Console.WriteLine("Current directory: " + projectPath);
            Console.WriteLine("AST has been written to ast_output.txt");

            pkb.ExtractInformation();
            pkb.printInfo();
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
