using System.Text.RegularExpressions;

namespace aitsi.QueryProcessor
{
    internal class QueryValidator
    {
        public static string[] allowedValuesInReturnParameter = ["boolean"];

        public static string evaluateQueryLogic(QueryNode tree)
        {
            checkDuplicates(tree);
            validateReturnParameters(tree);
            SelectNode select = (SelectNode)tree.getChildByName("select");
            validateClauses(select);
            validateWiths(select);
            validatePatterns(select);

            return "Drzewo jest poprawne logicznie.";
        }

        private static void validateWiths(SelectNode tree)
        {
            Node[] declarations = tree.getChildreenByName("With");
            foreach (Node declaration in declarations)
            {
                if (validateRef(tree.parent, declaration.variables[0]) != validateRef(tree.parent, declaration.variables[1])) throw new Exception("Wartości podane w 'with' muszą być tego samego typu.");
            }
        }

        private static void validatePatterns(SelectNode tree)
        {
            Node[] declarations = tree.getChildreenByName("Pattern");
            string[] assignPatternTypes = ["any", "exact", "subexpression"];
            foreach (Node declaration in declarations)
            {
                validateIfVarRef(tree.parent, declaration.variables[1]);
                switch (declaration.variables.Count())
                {
                    case 4:
                        if (!validateIfSynonym(tree.parent, declaration.variables[0], "if")) throw new Exception("Podano niepoprawny synonim do patterna w stylu 'if'.");
                        break;
                    case 3:
                        if (declaration.type == "any" && validateIfSynonym(tree.parent, declaration.variables[0], "while"))break;                
                        else if (assignPatternTypes.Contains(declaration.type) && validateIfSynonym(tree.parent, declaration.variables[0], "assign"))break;                                                  
                        throw new Exception("Podany pattern nie pasuje do żadnego ze znanych rodzajów patterna.");
                    default:
                        throw new Exception("Błąd logiczny pattern. Liczba argumentów nie odpowiada żadnemu z rodzaji patterna.");
                }
            }
        }

        private static string validateRef(Node tree, string value)
        {
            string isAttrRef = validateIfAttrRef(tree, value);
            if (isAttrRef != null) return isAttrRef;
            if (validateIfSynonym(tree, value, "prog_line")) return "integer";
            string pattern = "(\"?)";
            if (validateIfIDENT(Regex.Replace(value, pattern, String.Empty))) return "charstr";
            if (validateIfInteger(value)) return "integer";
            throw new Exception("Podano błędną wartość w 'with'. Wartość: " + value);
        }

        private static string validateIfAttrRef(Node tree, string value)
        {
            if (value == null) return null;
            var parts = value.Split('.');
            if (parts.Length < 2) return null;
            if (!validateIfSynonym(tree, parts[0]))return null;
            switch (parts[1])
            {
                case "procName":
                case "varName":
                    return "charstr";
                case "value":
                case "stmt#":
                    return "integer";
                default:
                    throw new Exception("Nazwa atrybutu w while jest niepoprawna. Błędna część: " + value);
            }
        }

        private static void validateClauses(SelectNode tree)
        {
            Node[] declarations = tree.getChildreenByName("Clause");
            foreach (Node declaration in declarations)
            {
                switch (declaration.relation.ToLower())
                {
                    case "modifies":
                    case "uses":
                        try
                        {
                            validateIfStmtRef(tree.parent, declaration.variables[0]);
                        }
                        catch (Exception e)
                        {
                            validateIfProcRef(tree.parent, declaration.variables[0]);
                        }
                        validateIfVarRef(tree.parent, declaration.variables[1]);
                        break;
                    case "parent":
                    case "parent*":
                    case "follows":
                    case "follows*":
                        validateIfStmtRef(tree.parent, declaration.variables[0]);
                        validateIfStmtRef(tree.parent, declaration.variables[1]);                        
                        break;
                    case "calls":
                    case "calls*":
                        validateIfProcRef(tree.parent, declaration.variables[0]);
                        validateIfProcRef(tree.parent, declaration.variables[1]);
                        break;
                    case "next":
                    case "next*":
                        validateIfLineRef(tree.parent, declaration.variables[0]);
                        validateIfLineRef(tree.parent, declaration.variables[1]);
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

        private static bool validateReturnParameters(QueryNode tree)
        {          
            foreach (string value in tree.getChildByName("select").variables)            
                if (!allowedValuesInReturnParameter.Contains(value.ToLower()) && (!validateIfSynonym(tree, value) || validateIfAttrRef(tree, value)!=null)) throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + value);
            
            return true;

            //if (tree.getChildByName("select").variables.Count() > 1) throw new Exception("Zapytanie Select może zwracać tylko jedną wartość.");
            //string returnValue = tree.getChildByName("select").variables[0];
            //if (allowedValuesInReturnParameter.Contains(returnValue.ToLower())) return true;
            //if (validateIfSynonym(tree, returnValue)) return true;
            //throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + returnValue);
        }

        private static bool validateIfSynonym(Node tree, string value, string type = "")
        {
            Node[] declarations = tree.getChildreenByName("Declaration");
            if(!validateIfIDENT(value)) return false;
            foreach (Node declaration in declarations)
            {
                if (declaration.variables.Contains(value) && type != "" && declaration.type.ToLower() == type) return true;
                else if (declaration.variables.Contains(value) && type == "") return true;
            }          
            return false;
        }

        private static bool validateIfStmtRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (validateIfInteger(value)) return true;
            if (validateIfSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako stmtRef. Podana wartość: " + value);
        }

        private static bool validateIfLineRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (validateIfInteger(value)) return true;
            if (validateIfSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako lineRef. Podana wartość: " + value);
        }

        private static bool validateIfProcRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (value.StartsWith("\"") && value.EndsWith("\"") && validateIfIDENT(value.Substring(1, value.Length - 2))) return true;
            if (validateIfSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako procRef. Podana wartość: " + value);
        }

        private static bool validateIfVarRef(Node tree, string value)
        {
            if (value == "_") return true;
            if (value.StartsWith("\"") && value.EndsWith("\"") && validateIfIDENT(value.Substring(1, value.Length - 2))) return true;
            if (validateIfSynonym(tree, value)) return true;
            throw new Exception("Podano nieprawidłową wartość jako varRef. Podana wartość: " + value);
        }

        private static bool validateIfInteger(string value)
        {
            if (!Regex.IsMatch(value, @"^[0-9]*$")) return false;
            return true;
        }

        private static bool validateIfIDENT(string value)
        {
            if (!Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9]*(#)?$")) return false;
            return true;
        }
    }
}
