using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

using Edam.WinUI.Controls.DataModels;
using Edam.WinUI.Controls.ViewModels;
using Edam.Data.Books;

namespace Edam.Test.Studio
{
   /// <summary>
   /// Tests for the Booklet model (Area 1).
   /// NOTE: This is scaffolding. It must be compiled and verified on a machine
   /// with the Windows App SDK / Visual Studio MSIX tooling (WinUI 3 build).
   /// </summary>
   [TestClass]
   public class BookModelTests
   {
      /// <summary>
      /// A BookModel should be constructible from a DataMapContext.
      /// </summary>
      [TestMethod]
      public void BookModel_Construct_FromContext()
      {
         var context = new DataMapContext();

         // BookModel requires context.UseCase.Book to be non-null.
         var model = new BookModel(context);

         Assert.IsNotNull(model);
         Assert.IsNotNull(model.Items);
         Assert.AreEqual(0, model.Items.Count);
      }

      /// <summary>
      /// FindBooklet should return null when no booklet has the given ID.
      /// </summary>
      [TestMethod]
      public void BookModel_FindBooklet_NotFound_ReturnsNull()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);

         var booklet = model.FindBooklet("does-not-exist");

         Assert.IsNull(booklet);
      }

      /// <summary>
      /// ClearAll should clear the item list without throwing (no ListView set).
      /// </summary>
      [TestMethod]
      public void BookModel_ClearAll_WithoutListView_DoesNotThrow()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);

         model.ClearAll();

         Assert.IsNotNull(model.Items);
         Assert.AreEqual(0, model.Items.Count);
      }

      /// <summary>
      /// CreateCell (UI-free) should build a text-cell model with the correct
      /// type and store it in the selected booklet (BL-1.6).
      /// </summary>
      [TestMethod]
      public void BookModel_CreateTextCell_CreatesCellModel()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var cell = model.CreateCell(BookletCellType.Text, "ref-1");

         Assert.IsNotNull(cell);
         Assert.AreEqual(BookletCellType.Text, cell.CellType);
         Assert.AreEqual(BookletTextType.Markdown, cell.TextType);
         Assert.AreEqual("ref-1", cell.ReferenceId);
         Assert.AreEqual(cell, model.SelectedBooklet.SelectedCell);
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// CreateCell (UI-free) should build a code-cell model with the correct
      /// type and store it in the selected booklet (BL-1.7).
      /// </summary>
      [TestMethod]
      public void BookModel_CreateCodeCell_CreatesCellModel()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var cell = model.CreateCell(BookletCellType.Code, "ref-1");

         Assert.IsNotNull(cell);
         Assert.AreEqual(BookletCellType.Code, cell.CellType);
         Assert.AreEqual(BookletTextType.JSONata, cell.TextType);
         Assert.AreEqual("ref-1", cell.ReferenceId);
         Assert.AreEqual(cell, model.SelectedBooklet.SelectedCell);
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// MoveCellDown (pure) should swap a cell with the following one in the
      /// selected booklet's item list (BL-1.12 reorder).
      /// </summary>
      [TestMethod]
      public void BookModel_MoveCellDown_SwapsAndMoves()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var c1 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c3 = model.CreateCell(BookletCellType.Text, "ref-1");
         var original = new List<BookletCellInfo> { c1, c2, c3 };

         Assert.AreEqual(3, model.SelectedBooklet.Items.Count);
         int indexBefore = model.SelectedBooklet.Items.IndexOf(c2);

         bool moved = model.MoveCellDown(c2);

         Assert.IsTrue(moved);
         Assert.AreEqual(3, model.SelectedBooklet.Items.Count);
         Assert.AreEqual(indexBefore + 1, model.SelectedBooklet.Items.IndexOf(c2));
         Assert.AreEqual(c3, model.SelectedBooklet.Items[indexBefore]);
         // order otherwise preserved
         Assert.IsTrue(model.SelectedBooklet.Items.SequenceEqual(
            new List<BookletCellInfo> { c1, c3, c2 }));
      }

      /// <summary>
      /// MoveCellDown on the final cell should be a no-op (BL-1.12 boundary).
      /// </summary>
      [TestMethod]
      public void BookModel_MoveCellDown_LastCell_DoesNotMove()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var c1 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Text, "ref-1");
         var orderBefore = new List<BookletCellInfo> { c1, c2 };

         bool moved = model.MoveCellDown(c2);

         Assert.IsFalse(moved);
         Assert.IsTrue(model.SelectedBooklet.Items.SequenceEqual(orderBefore));
      }

      /// <summary>
      /// DeleteCell should remove the cell from the selected booklet and leave
      /// the remaining cells in order (BL-1.12 delete).
      /// </summary>
      [TestMethod]
      public void BookModel_DeleteCell_RemovesFromSelectedBooklet()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var c1 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c3 = model.CreateCell(BookletCellType.Text, "ref-1");

         bool removed = model.DeleteCell(c2);

         Assert.IsTrue(removed);
         Assert.AreEqual(2, model.SelectedBooklet.Items.Count);
         Assert.IsFalse(model.SelectedBooklet.Items.Contains(c2));
         Assert.IsTrue(model.SelectedBooklet.Items.SequenceEqual(
            new List<BookletCellInfo> { c1, c3 }));
      }

      /// <summary>
      /// Deleting the selected cell should also clear the SelectedCell
      /// reference (BL-1.12).
      /// </summary>
      [TestMethod]
      public void BookModel_DeleteCell_ClearsSelectedCell()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();

         var c1 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Text, "ref-1");
         model.SelectedBooklet.SelectedCell = c2;

         model.DeleteCell(c2);

         Assert.IsNull(model.SelectedBooklet.SelectedCell);
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// BookViewModel.DeleteCell with no argument should delete the currently
      /// selected cell (BL-1.12).
      /// </summary>
      [TestMethod]
      public void BookViewModel_DeleteCell_NoArg_UsesSelectedCell()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();
         var vm = new BookViewModel { Model = model };

         var c1 = model.CreateCell(BookletCellType.Text, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Text, "ref-1");
         model.SelectedBooklet.SelectedCell = c2;

         vm.DeleteCell();

         Assert.IsFalse(model.SelectedBooklet.Items.Contains(c2));
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// BookViewModel.DeleteCell with an explicit cell should delete it
      /// (BL-1.12).
      /// </summary>
      [TestMethod]
      public void BookViewModel_DeleteCell_WithExplicitCell()
      {
         var context = new DataMapContext();
         var model = new BookModel(context);
         model.SelectedBooklet = new BookletInfo();
         var vm = new BookViewModel { Model = model };

         var c1 = model.CreateCell(BookletCellType.Code, "ref-1");
         var c2 = model.CreateCell(BookletCellType.Code, "ref-1");

         vm.DeleteCell(c1);

         Assert.IsFalse(model.SelectedBooklet.Items.Contains(c1));
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// Adding a text cell should create a BookletCellInfo of type Text and
      /// add it to the selected booklet (BL-1.6).
      /// NOTE: AddControl instantiates WinUI controls (ListView, BookletTextCellControl)
      /// which require an initialized WinUI/XAML UI host. This test is therefore
      /// skipped in the headless host and needs a WinUI test host or a model/control
      /// refactor to run.
      /// </summary>
      [TestMethod]
      [Ignore("Requires a WinUI UI host to instantiate ListView and cell controls.")]
      public void BookModel_AddTextCell_CreatesTextCell()
      {
         var context = new DataMapContext();
         context.BookletViewList = new Microsoft.UI.Xaml.Controls.ListView();
         var model = new BookModel(context);
         var viewModel = new BookViewModel { Model = model };
         model.SelectedBooklet = new BookletInfo();

         var cell = model.AddControl(viewModel, BookletCellType.Text, "ref-1");

         Assert.IsNotNull(cell);
         Assert.AreEqual(BookletCellType.Text, cell.CellType);
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }

      /// <summary>
      /// Adding a code cell should create a BookletCellInfo of type Code
      /// (BL-1.7). Same UI-host caveat as the text-cell test.
      /// </summary>
      [TestMethod]
      [Ignore("Requires a WinUI UI host to instantiate ListView and cell controls.")]
      public void BookModel_AddCodeCell_CreatesCodeCell()
      {
         var context = new DataMapContext();
         context.BookletViewList = new Microsoft.UI.Xaml.Controls.ListView();
         var model = new BookModel(context);
         var viewModel = new BookViewModel { Model = model };
         model.SelectedBooklet = new BookletInfo();

         var cell = model.AddControl(viewModel, BookletCellType.Code, "ref-1");

         Assert.IsNotNull(cell);
         Assert.AreEqual(BookletCellType.Code, cell.CellType);
         Assert.AreEqual(1, model.SelectedBooklet.Items.Count);
      }
   }
}
