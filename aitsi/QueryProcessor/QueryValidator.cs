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
            SelectNode select = (SelectNode)tree.getChildByName("select");
            validateClauses(select);
            validateWiths(select);

            return "Drzewo jest poprawne logicznie.";
        }

        private static void validateWiths(SelectNode tree)
        {
            Node[] declarations = tree.getChildreenByName("With");
            foreach (Node declaration in declarations)
            {
                validateSynonym(tree.parent, declaration.variables[0].Substring(0, declaration.variables[0].IndexOf('.')));
                validateRef(declaration.variables[1]);
            }
        }

        private static bool validateRef(string value)
        {
            if(validateIfInteger(value)) return true;
            string pattern = "(\"?)";
            if (!validateIfIDENT(Regex.Replace(value, pattern, String.Empty))) return true;
            return false;
        }

        private static void validateClauses(SelectNode tree)
        {
            Node[] declarations = tree.getChildreenByName("Clause");
            foreach (Node declaration in declarations)
            {
                switch (declaration.relation)
                {
                    case "Modifies":
                    case "Uses":
                        validateIfStmtRef(tree.parent, declaration.variables[0]);
                        validateIfEntRef(tree.parent, declaration.variables[1]);
                        break;
                    case "Parent":
                    case "Parent*":
                    case "Follows":
                    case "Follows*":
                        Console.WriteLine("validateClauses: " + declaration.variables[0]);
                        Console.WriteLine("validateClauses: " + declaration.variables[1]);
                        validateIfStmtRef(tree.parent, declaration.variables[0]);
                        validateIfStmtRef(tree.parent, declaration.variables[1]);                        
                        break;
                    default:
                        throw new Exception("Podana relacja nie została jeszcze zaimplementowana.");
                }
            }
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

        private static bool validateSynonym(Node tree, string value)
        {
            Node[] declarations = tree.getChildreenByName("Declaration");
            if(!validateIfIDENT(value)) throw new Exception("Podana wartość nie jest synonimem. Wartość: " + value);
            foreach (Node declaration in declarations)
                if (declaration.variables.Contains(value)) return true;
            return false;
        }

        private static bool validateIfStmtRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (validateIfInteger(value)) return true;
            if (validateSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako stmtRef. Podana wartość: " + value);
        }

        private static bool validateIfEntRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (validateSynonym(tree, value)) return true;
            if(value.StartsWith("\"") && value.EndsWith("\"") && validateIfIDENT(value.Substring(1, value.Length-2)))return true;
            throw new Exception("Podano nieprawidłową wartość jako entRef. Podana wartość: " + value);
        }

        private static bool validateIfName(string value)
        {
            if (!Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9]*$")) return false;
            return true;
        }

        private static bool validateIfInteger(string value)
        {
            if (!Regex.IsMatch(value, @"^[0-9]*$")) return false;
            return true;
        }

        private static bool validateIfIDENT(string value)
        {
            if (!Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9#]*$")) return false;
            return true;
        }
    }
}
