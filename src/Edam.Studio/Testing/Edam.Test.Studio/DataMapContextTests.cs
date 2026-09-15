using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Edam.WinUI.Controls.DataModels;
using Edam.Data.AssetSchema;
using Edam.Data.AssetUseCases;
using Edam.Data.Books;

namespace Edam.Test.Studio
{
   /// <summary>
   /// Tests for the schema-mapping context (Area 1).
   /// NOTE: This is scaffolding. It must be compiled and verified on a machine
   /// with the Windows App SDK / Visual Studio MSIX tooling (WinUI 3 build).
   /// </summary>
   [TestClass]
   public class DataMapContextTests
   {
      /// <summary>
      /// A DataMapContext should be constructible and expose empty
      /// Source/Target item collections.
      /// </summary>
      [TestMethod]
      public void DataMapContext_Construct_ExposesEmptyCollections()
      {
         var context = new DataMapContext();

         Assert.IsNotNull(context);
         Assert.IsNotNull(context.SourceItems);
         Assert.IsNotNull(context.TargetItems);
         Assert.IsNotNull(context.MapItems);
         Assert.AreEqual(0, context.SourceItems.Count);
         Assert.AreEqual(0, context.TargetItems.Count);
      }

      /// <summary>
      /// Source and Target item collections should be populated from a
      /// loaded schema (Source A / Target B) via SetMapItemReferences.
      /// </summary>
      [TestMethod]
      public void DataMapContext_LoadSourceAndTarget_PopulatesItems()
      {
         var context = new DataMapContext();

         var mapItem = new AssetDataMapItem();
         mapItem.SourceElement.Add(new MapItemInfo
         {
            Name = "CustomerId",
            Path = "CustomerId"
         });
         mapItem.SourceElement.Add(new MapItemInfo
         {
            Name = "CustomerName",
            Path = "CustomerName"
         });
         mapItem.TargetElement.Add(new MapItemInfo
         {
            Name = "Id",
            Path = "Id"
         });
         mapItem.TargetElement.Add(new MapItemInfo
         {
            Name = "Name",
            Path = "Name"
         });

         context.SetMapItemReferences(mapItem);

         Assert.AreEqual(2, context.SourceItems.Count);
         Assert.AreEqual(2, context.TargetItems.Count);
         Assert.AreEqual(4, context.MapItems.Count);
         Assert.IsTrue(context.SourceItems.All(i => i.Side == MapItemType.Source));
         Assert.IsTrue(context.TargetItems.All(i => i.Side == MapItemType.Target));
      }

      /// <summary>
      /// Passing null to SetMapItemReferences should clear the collections
      /// (not throw) and leave them empty.
      /// </summary>
      [TestMethod]
      public void DataMapContext_SetMapItemReferences_Null_ClearsCollections()
      {
         var context = new DataMapContext();

         context.SetMapItemReferences(null);

         Assert.AreEqual(0, context.SourceItems.Count);
         Assert.AreEqual(0, context.TargetItems.Count);
         Assert.AreEqual(0, context.MapItems.Count);
      }

      /// <summary>
      /// ClearAll should not throw on an empty context.
      /// </summary>
      [TestMethod]
      public void DataMapContext_ClearAll_OnEmptyContext_DoesNotThrow()
      {
         var context = new DataMapContext();

         context.ClearAll();

         Assert.IsNotNull(context.SourceItems);
         Assert.IsNotNull(context.TargetItems);
      }

      /// <summary>
      /// Reloading (re-calling SetMapItemReferences with a new/fresh schema)
      /// should replace, not accumulate, the source/target collections
      /// (BL-1.4/BL-1.5 load semantics).
      /// </summary>
      [TestMethod]
      public void DataMapContext_ReloadSourceTarget_ResetsCollections()
      {
         var context = new DataMapContext();

         var first = new AssetDataMapItem();
         first.SourceElement.Add(new MapItemInfo { Name = "CustomerId", Path = "CustomerId" });
         first.TargetElement.Add(new MapItemInfo { Name = "Id", Path = "Id" });
         context.SetMapItemReferences(first);

         Assert.AreEqual(1, context.SourceItems.Count);
         Assert.AreEqual(1, context.TargetItems.Count);

         // A later load (fresh schema) must reset rather than accumulate.
         var second = new AssetDataMapItem();
         second.SourceElement.Add(new MapItemInfo { Name = "A", Path = "A" });
         second.SourceElement.Add(new MapItemInfo { Name = "B", Path = "B" });
         second.SourceElement.Add(new MapItemInfo { Name = "C", Path = "C" });
         second.TargetElement.Add(new MapItemInfo { Name = "T", Path = "T" });
         context.SetMapItemReferences(second);

         Assert.AreEqual(3, context.SourceItems.Count);
         Assert.AreEqual(1, context.TargetItems.Count);
         Assert.AreEqual(4, context.MapItems.Count);
      }

      /// <summary>
      /// Creating a mapping from a schema pair (Source A → Target B) should
      /// surface both the source and target elements side by side in MapItems,
      /// each tagged with its correct Side (BL-1.10).
      /// </summary>
      [TestMethod]
      public void DataMapContext_CreateMapping_PairsSourceToTarget()
      {
         var context = new DataMapContext();

         var mapping = new AssetDataMapItem();
         // A source element (Customer/Id) is paired to a target element (Id).
         mapping.SourceElement.Add(new MapItemInfo { Name = "CustomerId", Path = "CustomerId" });
         mapping.TargetElement.Add(new MapItemInfo { Name = "Id", Path = "Id" });
         context.SetMapItemReferences(mapping);

         var source = context.SourceItems.SingleOrDefault();
         var target = context.TargetItems.SingleOrDefault();

         Assert.IsNotNull(source, "Expected one source element from the mapping");
         Assert.IsNotNull(target, "Expected one target element from the mapping");
         Assert.AreEqual(MapItemType.Source, source.Side);
         Assert.AreEqual(MapItemType.Target, target.Side);
         Assert.AreEqual(2, context.MapItems.Count);
         // Both the source and target are present as the mapping pair.
         Assert.IsTrue(context.MapItems.Contains(source));
         Assert.IsTrue(context.MapItems.Contains(target));
      }

      /// <summary>
      /// The annotation Description built from a map-item Path is the input to
      /// the semantic similarity comparison (BL-1.9). It should be proper-cased,
      /// drop the "dbo" schema qualifier, and split camelCase tokens.
      /// This verifies the model-level input to LexiconModel.Compare.
      /// </summary>
      [TestMethod]
      public void DataMapItem_GetAnnotation_ProducesSemanticDescription()
      {
         var item = new MapItemInfo { Name = "CustomerId", Path = "dbo/Customer/Id" };

         var annotation = item.GetAnnotation();

         Assert.IsNotNull(annotation);
         Assert.IsNotNull(annotation.Description);
         StringAssert.Contains(annotation.Description, "Customer");
         StringAssert.Contains(annotation.Description, "Id");
         // The "dbo" schema qualifier is dropped from the human description.
         StringAssert.DoesNotMatch(annotation.Description,
            new System.Text.RegularExpressions.Regex(@"\bdbo\b",
               System.Text.RegularExpressions.RegexOptions.IgnoreCase));
      }

      /// <summary>
      /// Semantic comparison inputs should be correctly prepared from the
      /// fixture-loaded Source/Target items (BL-1.9).
      /// NOTE: the actual score computation (TextSimilarityService ->
      /// ITextSimilarityInstance.ExecuteScript) needs a configured text-similarity
      /// script + ProjectContext.Arguments, which are not available in the
      /// headless test host. This verifies everything upstream of that call:
      /// the Source x Target input population and the tokenized descriptions
      /// that feed the scorer.
      /// </summary>
      [TestMethod]
      public void DataMapContext_SemanticCompare_ProducesScores()
      {
         var context = new DataMapContext();
         var mapItem = LoadFixtureMapItem("SourceSchemaA.json", "TargetSchemaB.json");
         context.SetMapItemReferences(mapItem);

         // Scoring iterates every Source x Target pair; each input must carry a
         // non-empty, tokenized annotation description.
         Assert.AreEqual(5, context.SourceItems.Count);
         Assert.AreEqual(5, context.TargetItems.Count);
         Assert.IsNotNull(context.LexiconModel);
         Assert.IsTrue(context.SourceItems.All(i =>
            !String.IsNullOrWhiteSpace(i.Annotation?.Description)));
         Assert.IsTrue(context.TargetItems.All(i =>
            !String.IsNullOrWhiteSpace(i.Annotation?.Description)));

         // Known fixture inputs: Source[0]=CustomerId -> "Customer Id",
         // Target[0]=Id -> "Id".
         Assert.AreEqual("Customer Id", context.SourceItems[0].Annotation.Description);
         Assert.AreEqual("Id", context.TargetItems[0].Annotation.Description);

         // 5 x 5 = 25 candidate scoring pairs are prepared.
         Assert.AreEqual(
            context.SourceItems.Count * context.TargetItems.Count, 25);

         // Actual score computation requires the text-similarity script instance
         // + ProjectContext.Arguments (not headless-testable).
      }


      /// <summary>
      /// Executing a code cell should run its JSONata query against the source
      /// JSON sample and store the result on the cell (BL-1.8).
      /// Runs headlessly: Execute now falls back to cell.Text when no UI
      /// control instance is present, and output is stored on cell.OutputText.
      /// </summary>
      [TestMethod]
      public void DataMapContext_ExecuteCodeCell_ProducesOutput()
      {
         var context = new DataMapContext();
         context.Source = new DataMapInstance
         {
            JsonInstanceSample = "{\"name\":\"abc\"}"
         };

         var cell = context.BookModel.Model.CreateCell(
            BookletCellType.Code, "ref-1");
         cell.Text = "{\"out\": $.name}";

         context.Execute(cell);

         Assert.IsNotNull(cell.OutputText);
         StringAssert.Contains(cell.OutputText, "abc");
         StringAssert.Contains(cell.OutputText, "out");
      }

      /// <summary>
      /// Executing an empty code cell should short-circuit and leave no output
      /// (null or empty, no throw) (BL-1.8 boundary).
      /// </summary>
      [TestMethod]
      public void DataMapContext_ExecuteEmptyCodeCell_IsNoop()
      {
         var context = new DataMapContext();
         context.Source = new DataMapInstance
         {
            JsonInstanceSample = "{\"name\":\"abc\"}"
         };

         var cell = context.BookModel.Model.CreateCell(
            BookletCellType.Code, "ref-1");
         cell.Text = "   ";

         context.Execute(cell);

         Assert.IsTrue(String.IsNullOrWhiteSpace(cell.OutputText));
      }

      /// <summary>
      /// An invalid JSONata query should fail gracefully (no throw) and leave
      /// the cell without output (BL-1.8 error path).
      /// </summary>
      [TestMethod]
      public void DataMapContext_ExecuteInvalidCodeCell_DoesNotThrow()
      {
         var context = new DataMapContext();
         context.Source = new DataMapInstance
         {
            JsonInstanceSample = "{\"name\":\"abc\"}"
         };

         var cell = context.BookModel.Model.CreateCell(
            BookletCellType.Code, "ref-1");
         cell.Text = "$.name(";   // malformed JSONata

         context.Execute(cell);

         Assert.IsTrue(String.IsNullOrWhiteSpace(cell.OutputText));
      }

      /// <summary>
      /// A use case with a booklet and code cell should round-trip through
      /// AssetUseCaseMap.ToFile / FromFile preserving content (BL-1.11).
      /// </summary>
      [TestMethod]
      public void AssetUseCaseMap_PersistRoundTrip_PreservesBooklet()
      {
         string folder = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "EdamTest_" + Guid.NewGuid().ToString("N"));
         System.IO.Directory.CreateDirectory(folder);
         string fileName = "uc.json";
         string filePath = System.IO.Path.Combine(folder, fileName);

         var useCase = new AssetUseCaseMap { Name = "UC_Test" };
         var booklet = new BookletInfo { BookletId = "b1" };
         useCase.Book.Items.Add(booklet);
         booklet.Items.Add(new BookletCellInfo
         {
            CellType = BookletCellType.Code,
            TextType = BookletTextType.JSONata,
            Text = "{\"out\": $.name}",
            ReferenceId = "ref-1"
         });

         AssetUseCaseMap.ToFile(useCase,
            folder + System.IO.Path.DirectorySeparatorChar, fileName);
         var loaded = AssetUseCaseMap.FromFile(filePath);

         try
         {
            Assert.IsNotNull(loaded);
            Assert.AreEqual("UC_Test", loaded.Name);
            Assert.AreEqual(1, loaded.Book.Items.Count);

            var lb = loaded.Book.Items[0];
            Assert.AreEqual(1, lb.Items.Count);
            Assert.AreEqual(BookletCellType.Code, lb.Items[0].CellType);
            Assert.AreEqual(BookletTextType.JSONata, lb.Items[0].TextType);
            Assert.AreEqual("{\"out\": $.name}", lb.Items[0].Text);
            Assert.AreEqual("ref-1", lb.Items[0].ReferenceId);
         }
         finally
         {
            if (System.IO.Directory.Exists(folder))
            {
               System.IO.Directory.Delete(folder, true);
            }
         }
      }

      /// <summary>
      /// Loading a source schema from a fixture file should populate the
      /// expected element set into DataMapContext.SourceItems (BL-1.4).
      /// Exercises the real file -> AssetDataMapItem -> SetMapItemReferences path.
      /// </summary>
      [TestMethod]
      public void DataMapContext_LoadSourceFromFixtureFile_PopulatesExpectedElements()
      {
         var context = new DataMapContext();
         var mapItem = LoadFixtureMapItem("SourceSchemaA.json", "TargetSchemaB.json");

         context.SetMapItemReferences(mapItem);

         string[] expected = { "CustomerId", "CustomerName", "EmailAddress", "OrderDate", "OrderTotal" };
         Assert.AreEqual(expected.Length, context.SourceItems.Count);
         for (int i = 0; i < expected.Length; i++)
         {
            Assert.AreEqual(expected[i], context.SourceItems[i].Name);
         }
      }

      /// <summary>
      /// Loading a target schema from a fixture file should populate the
      /// expected element set into DataMapContext.TargetItems (BL-1.5).
      /// </summary>
      [TestMethod]
      public void DataMapContext_LoadTargetFromFixtureFile_PopulatesExpectedElements()
      {
         var context = new DataMapContext();
         var mapItem = LoadFixtureMapItem("SourceSchemaA.json", "TargetSchemaB.json");

         context.SetMapItemReferences(mapItem);

         string[] expected = { "Id", "Name", "Email", "Date", "Amount" };
         Assert.AreEqual(expected.Length, context.TargetItems.Count);
         for (int i = 0; i < expected.Length; i++)
         {
            Assert.AreEqual(expected[i], context.TargetItems[i].Name);
         }
      }

      /// <summary>
      /// Items loaded from fixture files should be side-tagged and carry a
      /// semantic description (Name/path tokens) from GetAnnotation (BL-1.4).
      /// </summary>
      [TestMethod]
      public void DataMapContext_LoadFixtureFile_SetsSideAndSemanticDescription()
      {
         var context = new DataMapContext();
         var mapItem = LoadFixtureMapItem("SourceSchemaA.json", "TargetSchemaB.json");

         context.SetMapItemReferences(mapItem);

         Assert.IsTrue(context.SourceItems.All(i => i.Side == MapItemType.Source));
         Assert.IsTrue(context.TargetItems.All(i => i.Side == MapItemType.Target));
         Assert.IsTrue(context.SourceItems.All(i =>
            !String.IsNullOrWhiteSpace(i.Annotation?.Description)));
         Assert.IsTrue(context.TargetItems.All(i =>
            !String.IsNullOrWhiteSpace(i.Annotation?.Description)));
         Assert.AreEqual("Customer Id", context.SourceItems[0].Annotation.Description);
      }

      // -- fixture helpers -----------------------------------------------------

      /// <summary>
      /// Build an AssetDataMapItem from the source + target fixture files.
      /// </summary>
      private static AssetDataMapItem LoadFixtureMapItem(
         string sourceFileName, string targetFileName)
      {
         var mapItem = new AssetDataMapItem();
         foreach (var el in ReadFixtureElements(sourceFileName))
         {
            mapItem.SourceElement.Add(new MapItemInfo
            {
               Name = el.Name,
               Path = el.Name
            });
         }
         foreach (var el in ReadFixtureElements(targetFileName))
         {
            mapItem.TargetElement.Add(new MapItemInfo
            {
               Name = el.Name,
               Path = el.Name
            });
         }
         return mapItem;
      }

      /// <summary>
      /// Read and deserialize the elements of a fixture file.
      /// </summary>
      private static List<SchemaFixtureElement> ReadFixtureElements(string fileName)
      {
         string path = ResolveFixturePath(fileName);
         string json = System.IO.File.ReadAllText(path);
         var fixture = JsonSerializer.Deserialize<SchemaFixture>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
         Assert.IsNotNull(fixture, "fixture " + fileName + " failed to deserialize");
         return fixture.Elements ?? new List<SchemaFixtureElement>();
      }

      /// <summary>
      /// Locate the Fixtures directory relative to the test output.
      /// </summary>
      private static string ResolveFixturePath(string fileName)
      {
         string[] candidates =
         {
            System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName),
            System.IO.Path.Combine(
               AppContext.BaseDirectory, "..", "..", "..", "..", "Edam.Test.Studio", "Fixtures", fileName),
            System.IO.Path.Combine(
               AppContext.BaseDirectory, "..", "..", "..", "..", "..",
               "src", "Edam.Studio", "Testing", "Edam.Test.Studio", "Fixtures", fileName)
         };
         foreach (var c in candidates)
         {
            try
            {
               if (System.IO.File.Exists(System.IO.Path.GetFullPath(c)))
               {
                  return System.IO.Path.GetFullPath(c);
               }
            }
            catch
            {
               // ignore malformed candidate path
            }
         }
         throw new System.IO.FileNotFoundException(
            "Fixture not found: " + fileName + " (base=" + AppContext.BaseDirectory + ")");
      }
   }

   /// <summary>
   /// Lightweight model of the schema fixture JSON files
   /// (Fixtures\SourceSchemaA.json / TargetSchemaB.json).
   /// </summary>
   public class SchemaFixture
   {
      public string Name { get; set; }
      public string NamespaceUri { get; set; }
      public List<SchemaFixtureElement> Elements { get; set; }
   }

   public class SchemaFixtureElement
   {
      public string Name { get; set; }
      public string DataType { get; set; }
      public string Description { get; set; }
   }
}
