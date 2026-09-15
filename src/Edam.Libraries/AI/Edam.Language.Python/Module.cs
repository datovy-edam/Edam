using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edam.Language.Python
{

   public class Module
   {
      public string Path { get; set; }
      public PyObject? Instance { get; set; }

      public Module(string path, PyObject? instance) 
      {
         Path = path;
         Instance = instance;
      }

   }

}
