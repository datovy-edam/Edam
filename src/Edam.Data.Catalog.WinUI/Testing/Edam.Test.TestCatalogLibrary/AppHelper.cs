

// -----------------------------------------------------------------------------
using Edam.Data.CatalogModel;

namespace Edam.Test.TestCatalogLibrary;

public class AppHelper
{

   private static ModelCatalogService? _catalogInstance;
   public static ModelCatalogService? CatalogInstance
   {
      get { return _catalogInstance; }
   }

   /// <summary>
   /// Initialize the test harness against the pure-Model (EF-free) in-memory
   /// catalog service (BL-7.2 step 5). Replaces the former EF-backed
   /// <c>CatalogBuilderServiceInstance</c> from <c>Edam.Data.CatalogDb</c> so the
   /// test library no longer references the retired CatalogDb layer. Catalog behavior is now
   /// covered by the provider/HTTP conformance runners and <c>Edam.Data.Catalog.Tests</c>.
   /// </summary>
   public static void InitializeTest()
   {
      if (_catalogInstance != null)
      {
         return;
      }

      _catalogInstance = ModelCatalogService.Default;
   }

}
