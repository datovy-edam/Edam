using Edam.Data.Dictionary;
using Edam.Data.Dictionary.Api;
using Edam.Test.Library.Application;
using System.Diagnostics;
namespace Edam.Test.Dictionary
{

   [TestClass]
   public class TestDictionary
   {

      [TestInitialize]
      public void InitializeEnvironment()
      {
         ApplicationHelpers.InitializeApplication();
         Debug.Print("Initialized...");
      }

      [TestMethod]
      public void InitializeDictionaries()
      {
         var context = new DictionaryContext();
         context.Database.EnsureCreated();
         context.Dispose();
      }

      [TestMethod]
      public void TestLookup()
      {
         FreeDictionaryApi api = new FreeDictionaryApi();
         api.LookUp("faculty");
      }

      private void ProcessLookUpEntry(LookUpResultInfo results)
      {
         if (results.CurrentTerm == null)
         {
            return;
         }

      }

      [TestMethod]
      public void TestLookupAll()
      {
         FreeDictionaryApi api = new FreeDictionaryApi();
         api.LookUp(topCount: 50000);
         api.Dispose();
      }

   }

}
