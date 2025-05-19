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
        string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        string filePath = Path.Combine(projectPath, "program.txt");
        string source = File.ReadAllText(filePath);

        var lexer = new Lexer(source);
        var ast = new AST();
        var parser = new Parser(lexer, ast);
        var designExtractor = new DesignExtractor(ast);
        var pkb = designExtractor.Extract();



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


        // try
        // {
        //     Console.WriteLine("Proszę podać zapytanie: ");
        //     string query = Console.ReadLine();
        //     var queryParts = Regex.Split(query, @"(?=select)", RegexOptions.IgnoreCase);
        //     if (queryParts.Length < 2)
        //         throw new Exception("Brak słowa 'select' w zapytaniu.");
        //     Console.WriteLine(evaluateAssignments(queryParts[0].Trim()));
        //     Console.WriteLine(evaluateQuery(queryParts[1].Trim()));
        //     QueryNode PQLTree = Parse(query.Trim());
        //     DrawTree(PQLTree);
        //     Console.WriteLine(evaluateQueryLogic(PQLTree));
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e.Message);
        // }
    }

}
