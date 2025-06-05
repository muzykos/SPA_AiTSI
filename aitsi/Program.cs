using aitsi;
using aitsi.Parser;
using aitsi.PKB;
using System.Text.RegularExpressions;
using static aitsi.QueryPreProcessor;
using static aitsi.QueryProcessor.QueryValidator;

class Program
{
    static void Main(String[] args)
    {
        //moje
        string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        string filePath = Path.Combine(projectPath, "kod_simple.txt");
        string source = File.ReadAllText(filePath);

        //stare
        //string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
     //   string filePath = Path.Combine("kod_simple.txt");
       // string source = File.ReadAllText(filePath);

        var lexer = new Lexer(source);
        var ast = new AST();
        var parser = new Parser(lexer, ast);
        var designExtractor = new DesignExtractor(ast);

        try
        {
            parser.parseProgram();
            var path = Path.Combine("ast_output.txt");
            using (var writer = new StreamWriter(path))
            {
                AST.PrintAST(ast.getRoot(), 0, writer);
            }
            var pkb = designExtractor.Extract();
            Console.WriteLine("Ready");

            while (true)
            {
                string declarations = Console.ReadLine()?.Trim();
                string query = Console.ReadLine()?.Trim();

                try
                {                   
                    //Console.WriteLine("Deklaracja: " + evaluateAssignments(declarations.Trim()));
                    evaluateAssignments(declarations.Trim());
                    evaluateQuery(query.Trim());

                    QueryNode pqlTree = Parse(declarations.Trim() + query.Trim());

                    //Console.WriteLine("\n Struktura zapytania:");
                    //DrawTree(pqlTree);

                    //Console.WriteLine(evaluateQueryLogic(pqlTree));
                    evaluateQueryLogic(pqlTree);

                    //Console.WriteLine("\n Wynik zapytania:");
                    string result = Evaluator.Evaluate(pqlTree, pkb);
                    Console.WriteLine(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine("# Błąd: " + e.Message);
                }




            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"# Parsing error: {ex.Message}");
        }



    }

}

