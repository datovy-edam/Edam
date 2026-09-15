using Edam.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edam.Language.Python
{

   public class PythonResults : ResultLog, IResultsLog
   {

      public bool Success { get; set; } = false;
      public dynamic? Results
      {
         get { return ResultValueObject as dynamic; }
         set
         {
            ResultValueObject = value;
         }
      }

      public PythonResults() 
      {
      }
      public PythonResults(dynamic? results)
      {
         Results = results;
      }

   }

}
