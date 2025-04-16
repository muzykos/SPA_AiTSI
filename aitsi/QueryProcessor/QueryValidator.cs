using System.Text.RegularExpressions;

namespace aitsi.QueryProcessor
{
    internal class QueryValidator
    {
        public static string[] allowedValuesInReturnParameter = ["boolean"];

        public static string evaluateQueryLogic(QueryNode tree)
        {
            checkDuplicates(tree);
            validateReturnParameter(tree);                      
            
            return "Sprawdzam";
        }

        private static void checkDuplicates(QueryNode tree)
        {
            Node[] declarations = tree.getChildreenByName("Declaration");
            List<string> variables = new List<string>();
            foreach (Node declaration in declarations)
            {
                foreach(string variable in declaration.variables)
                {
                    if (variables.Contains(variable)) throw new Exception("Na liście zmiennych znajdują się duplikaty. Duplikat: " + variable);
                    variables.Add(variable);
                }
            }
        }

        private static bool validateReturnParameter(QueryNode tree)
        {
            if (tree.getChildByName("select").variables.Count() > 1) throw new Exception("Zapytanie Select może zwracać tylko jedną wartość."); 
            string returnValue = tree.getChildByName("select").variables[0];
            if (allowedValuesInReturnParameter.Contains(returnValue)) return true;
            if (validateSynonym(tree, returnValue)) return true;
            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + returnValue);
        }

        private static bool validateSynonym(QueryNode tree, string value)
        {
            Node[] declarations = tree.getChildreenByName("Declaration");
            foreach (Node declaration in declarations)
                if (declaration.variables.Contains(value)) return true;
            return false;
        }

        private static bool validateIfStmtRef(QueryNode tree, string value)
        {
            if (value == "_") return true;
            if (int.TryParse(value, out _)) return true;
            if (validateSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako stmtRef. Podana wartość: " + value);
        }

        private static bool checkIfIDENT(string value)
        {
            if (!Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9]*$")) throw new Exception("Zmienna może składać się tylko z liter, cyfr i '#'. Błędna zmienna: " + value);
            return true;
        }
    }
}
