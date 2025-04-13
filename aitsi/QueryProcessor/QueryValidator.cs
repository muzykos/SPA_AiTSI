using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aitsi.QueryProcessor
{
    internal class QueryValidator
    {
        public static string[] allowedValuesInReturnParameter = ["boolean"];

        private static bool validateReturnParameter(string returnParameter)
        {
            if (allowedValuesInReturnParameter.Contains(returnParameter)) return true;        
            foreach (string key in QueryPreProcessor.assignmentsList.Keys)
                if (QueryPreProcessor.assignmentsList[key].Contains(returnParameter)) return true;

            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + returnParameter);
        }

        private static bool validateIfStmtRef(string value)
        {
            if (value == "_") return true;
            if (int.TryParse(value, out _)) return true;

            foreach (string key in QueryPreProcessor.assignmentsList.Keys)
                if (QueryPreProcessor.assignmentsList[key].Contains(value)) return true;

            throw new Exception("Podano nieprawidłową wartość do zwrócenia. Podana wartość: " + value);
        }
    }
}
