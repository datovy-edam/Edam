using Edam.Diagnostics;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using languages = Edam.Language;

// -----------------------------------------------------------------------------
// https://pythonnet.github.io/pythonnet/dotnet.html

namespace Edam.Language.Python
{

   /// <summary>
   /// Encapsulate the Python Intepreter.
   /// </summary>
   public class Interpreter : IDisposable, IInterpreter
   {

      /// <summary>
      /// Global Interpreter Lock.
      /// </summary>
      private Py.GILState? _GIL = null;

      private dynamic _os;
      private dynamic _sys;

      private List<Module> _registeredScripts = new List<Module>();

      public Interpreter()
      {
         Runtime.PythonDLL = PythonHelper.GetPythonDllPath();
         PythonEngine.Initialize();

         _os = Py.Import("os");
         _sys = Py.Import("sys");
      }

      /// <summary>
      /// Get Global Interpter Lock.
      /// </summary>
      /// <returns></returns>
      public Py.GILState GetLock()
      {
         _GIL = _GIL ?? Py.GIL();
         return _GIL;
      }

      /// <summary>
      /// Release Global Interpreter Lock by disposing it.
      /// </summary>
      public void ReleaseLock()
      {
         Dispose();
      }

      /// <summary>
      /// Get Script using given script name and path.
      /// </summary>
      /// <param name="scriptName">script / module name</param>
      /// <param name="scriptPath">script path (optional)</param>
      /// <returns>PyObject instance is returned</returns>
      public Module GetModule(string scriptName, string? scriptPath = null)
      {
         string mpath =
            PythonHelper.GetPythonModulePath(scriptName, scriptPath);
         var fullPath = Path.GetFileNameWithoutExtension(mpath);

         // module already loaded? if so, just return it...
         Module? module = _registeredScripts.Find((x) => x.Path == fullPath);
         if (module != null)
         {
            return new Module(module.Path, module.Instance);
         }

         // load new module and register it...
         _sys.path.append(_os.path.dirname(_os.path.expanduser(mpath)));
         var mod = Py.Import(fullPath);

         var newModule = new Module(fullPath, mod);
         _registeredScripts.Add(newModule);

         return newModule;
      }

      /// <summary>
      /// Run Script.
      /// </summary>
      /// <param name="scriptName">script name</param>
      /// <param name="functionName">function name</param>
      /// <param name="parameters">parameters for function (optional)</param>
      /// <param name="scriptPath">script path (optional)</param>
      /// <returns></returns>
      public PythonResults RunScript(
         string scriptName, string functionName, 
         languages.Parameters? parameters = null, string? scriptPath = null)
      {
         GetLock();
         Parameters pyParameters = new Parameters(parameters);
         Module module = GetModule(scriptName, scriptPath);

         PyObject? resultsObject = null;
         PythonResults? results = new PythonResults();

         // any parameters?
         if (pyParameters != null)
         {
            // setup parameters
            var plist = pyParameters.ToPyObjects();

            // invoke functions and get results...
            resultsObject = module.Instance.InvokeMethod(functionName, plist);
         }
         else
         {
            // invoke functions and get results...
            resultsObject = module.Instance.InvokeMethod(functionName);
         }

         results.Results = resultsObject;
         results.Success = true;

         return results;
      }

      /// <summary>
      /// Execute Script.
      /// </summary>
      /// <param name="scriptName">script name</param>
      /// <param name="functionName">function name</param>
      /// <param name="parameters">parameters for function (optional)</param>
      /// <param name="scriptPath">script path (optional)</param>
      /// <returns></returns>
      public IResultsLog ExecuteScript(
         string scriptName, string functionName,
         languages.Parameters? parameters = null, string? scriptPath = null)
      {
         return RunScript(scriptName, functionName, parameters, scriptPath);
      }

      /// <summary>
      /// Dispose / release interpreter lock resources.
      /// </summary>
      public void Dispose()
      {
         if (_registeredScripts != null)
         {
            _registeredScripts.Clear();
         }
         if (_GIL != null)
         {
            _GIL.Dispose();
            _GIL = null;
         }
      }

   }

}
