namespace aitsi.QueryProcessor
{
    internal class QueryValidator
    {
        public static string[] allowedValuesInReturnParameter = ["boolean"];

        public static string evaluateQueryLogic(QueryNode tree)
        {
            
            validateReturnParameter(tree);                      
            
            return "Sprawdzam";
        }
        private static bool validateReturnParameter(QueryNode tree)
        {
            string returnValue = tree.getChildByName("select").variables[0];
            Node[] declarations = tree.getChildreenByName("Declaration");

            if (allowedValuesInReturnParameter.Contains(returnValue)) return true;
            if (int.TryParse(returnValue, out _)) return true;
            foreach (Node declaration in declarations)
                if (declaration.variables.Contains(returnValue)) return true;
            
            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + returnValue);
        }

        private static bool validateIfStmtRef(string value)
        {
            if (value == "_") return true;
            if (int.TryParse(value, out _)) return true;

            foreach (string key in QueryPreProcessor.assignmentsList.Keys)
                if (QueryPreProcessor.assignmentsList[key].Contains(value)) return true;

            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + value);
        }

        private static bool checkIfIDENT(QueryNode tree, string value)
        {
            Node[] declarations = tree.getChildreenByName("Declaration");

            if (allowedValuesInReturnParameter.Contains(value)) return true;
            if (int.TryParse(value, out _)) return true;
            foreach (Node declaration in declarations)
                if (declaration.variables.Contains(value)) return true;

            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + value);
        }
    }
}
