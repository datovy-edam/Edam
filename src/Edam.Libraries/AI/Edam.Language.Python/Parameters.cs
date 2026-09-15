using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;

using languages = Edam.Language;

namespace Edam.Language.Python
{

   /// <summary>
   /// Helper to prepare Python parameters list.
   /// </summary>
   public class Parameters
   {
      private languages.Parameters _parameters = null;
      public Parameters(languages.Parameters parameters)
      {
         _parameters = parameters;
      }

      /// <summary>
      /// Prepare a list of PyObjects that will be used as method parameters.
      /// </summary>
      /// <returns>PyObject array is returned</returns>
      public PyObject[] ToPyObjects()
      {
         PyObject[] result = new PyObject[_parameters.Items.Count];
         int c = 0;
         foreach(object item in _parameters.Items.Values)
         {
            var type = item.GetType();
            if (type.Name == "String")
            {
               result[c++] = new PyString((string) item);
            }
            else if (type.Name == "Int32")
            {
               result[c++] = new PyInt((int)item);
            }
            else if (type.Name == "Single")
            {
               result[c++] = new PyFloat((float)item);
            }
         }
         return result;
      }

   }

}
