using Python.Runtime;
using Edam.Application;
using System.Dynamic;

using languages = Edam.Language;

namespace Edam.Language.Python
{

   public class PythonHelper
   {
      
      public static string GetPythonDllPath()
      {
         return AppSettings.GetSectionString("PythonDllPath");
      }

      public static string GetPythonModulesPath()
      {
         return AppSettings.GetSectionString("PythonModulesPath");
      }

      /// <summary>
      /// Get Python Module Script Path.
      /// </summary>
      /// <param name="scriptName">script name</param>
      /// <param name="scriptPath">script path</param>
      /// <returns>script full path is returned</returns>
      public static string GetPythonModulePath(
         string scriptName, string? scriptPath = null)
      {
         string ppath = String.IsNullOrWhiteSpace(scriptPath) ? 
            String.Empty : "\\" + scriptPath;
         return GetPythonModulesPath() + ppath + scriptName;
      }

      /// <summary>
      /// Run Python Script and return results.
      /// </summary>
      /// <param name="scriptName">python script name</param>
      /// <param name="functionName">function name</param>
      /// <param name="parameters">parameters for function</param>
      public static PythonResults? ExecuteScript(
         string scriptName, string functionName, 
         languages.Parameters? parameters = null)
      {
         PythonResults? results = null;
         using (Interpreter interpreter = new Interpreter())
         {
            results = interpreter.RunScript(
               scriptName, functionName, parameters);
         }

         return results;
         //string mpath = GetPythonModulesPath() + scriptName;
         //string ddlPath = GetPythonDllPath();

         //Runtime.PythonDLL = ddlPath;

         //PythonEngine.Initialize();

         //// prepare results
         //PyObject? resultsObject = null;
         //PythonResults? results = new PythonResults();

         //// using Python Global Interpreter Lock
         //using (Py.GIL())
         //{
         //   // prepare resources to invoke python functions
         //   dynamic os = Py.Import("os");
         //   dynamic sys = Py.Import("sys");

         //   sys.path.append(os.path.dirname(os.path.expanduser(mpath)));
         //   var fromFile = Py.Import(Path.GetFileNameWithoutExtension(mpath));

         //   // any parameters?
         //   if (parameters != null)
         //   {
         //      // setup parameters
         //      var plist = parameters.ToPyObjects();

         //      // invoke functions and get results...
         //      resultsObject = fromFile.InvokeMethod(functionName, plist);
         //   }
         //   else
         //   {
         //      // invoke functions and get results...
         //      resultsObject = fromFile.InvokeMethod(functionName);
         //   }

         //   results.Results = resultsObject;
         //   results.Success = true;
         //}

         //return results;
      }

   }

}
