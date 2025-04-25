using aitsi;
using aitsi.Parser;

class Program { 
    static void Main(String[] args)
    {
        string source = @"
        procedure main {
            x = 5 + 3;
            y = x + 1;
            while y {
                x = x + 1;
            }
        }";

        var lexer = new Lexer(source);
        var ast = new AST();
        var parser = new Parser(lexer, ast);

        try
        {
            parser.parseProcedure();

            QueryEvaluator.SetAST(ast);
            Console.WriteLine("test:");
            Console.WriteLine("Follows(1,2): " + QueryEvaluator.EvaluateFollows(1, 2));
            Console.WriteLine("Parent(3,4): " + QueryEvaluator.EvaluateParent(3, 4));

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
