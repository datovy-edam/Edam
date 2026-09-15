using Edam.Language.Python;
using System.Diagnostics;
using Edam.Test.Library.Application;

using languages = Edam.Language;
using Edam.Data.Lexicon.Semantics;

namespace Edam.Test.Python
{

   [TestClass]
   public class TestPythonHelpers
   {

      [TestInitialize]
      public void InitializeEnvironment()
      {
         ApplicationHelpers.InitializeApplication();
         Debug.Print("Initialized...");
      }

      [TestMethod]
      public void TestPythonModules()
      {
         string scriptName = TextSimilarityService.SCRIPT_SEMANTIC_TEXT_SIMILARITY;
         string methodName = TextSimilarityService.METHOD_GET_TEXT_SIMILARITY_SCORE;

         languages.Parameters parameters = new languages.Parameters();
         parameters.Add("text1", "The person starved");
         parameters.Add("text2", "The man is starving");
         var results = PythonHelper.ExecuteScript(
            scriptName, methodName, parameters);
         if (results.Success)
         {
            dynamic? returnData = results.Results;
         }
      }

   }

}
