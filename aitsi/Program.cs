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
        var pkb = new PKB(ast);



        try
        {
            parser.parseProcedure();
            Console.WriteLine("Parsing completed successfully!\n");
            AST.PrintAST(ast.getRoot(), 0);
            pkb.PopulatePKB();
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
