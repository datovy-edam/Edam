using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// -----------------------------------------------------------------------------
using Edam.Data.Books;
using Edam.WinUI.Controls.Booklets;
using Edam.WinUI.Controls.ViewModels;
using Edam.Helpers;
using System.Collections.ObjectModel;

namespace Edam.WinUI.Controls.DataModels
{

   /// <summary>
   /// This class manage the inner Book and provide helpers to manage UI 
   /// resources and controls.
   /// </summary>
   public class BookModel : ObservableObject
   {

      #region -- 1.00 - Properties and Fields declarations

      private DataMapContext m_Context;
      public DataMapContext Context
      {
         get { return m_Context; }
      }

      /// <summary>
      /// Items show all added booklets and (code and text) cells...
      /// </summary>
      public ListView ListView
      {
         get { return m_Context.BookletViewList; }
      }

      private ObservableCollection<IBookCellView> m_Items;
      public ObservableCollection<IBookCellView> Items
      {
         get { return m_Items; }
         set
         {
            if (m_Items != value)
            {
               m_Items = value;
               OnPropertyChanged(nameof(Items));
            }
         }
      }

      private BookInfo m_Book;
      public BookInfo Book
      {
         get { return m_Book; }
      }

      public BookletInfo SelectedBooklet
      {
         get { return Book.SelectedBooklet; }
         set 
         { 
            Book.SelectedBooklet = value;
         }
      }

      #endregion
      #region -- 1.50 - Constructure

      public BookModel(DataMapContext context)
      {
         if (context.UseCase.Book == null)
         {
            throw new Exception(
               "Expected an instance of a BookInfo null was found");
         }
         m_Book = context.UseCase.Book;
         m_Context = context;
         Items = new ObservableCollection<IBookCellView>();
      }

      #endregion
      #region -- 4.00 - Book Booklet and Cells support

      /// <summary>
      /// Find a booklet... by booklet ID
      /// </summary>
      /// <param name="bookletId">booklet ID to find</param>
      /// <returns>returns instance of BookletInfo if found, else null</returns>
      public BookletInfo FindBooklet(string bookletId)
      {
         return Book.Find(bookletId);
      }

      /// <summary>
      /// Clear all adquired resources including the ListView entries and Book/
      /// Booklet/Cell inner Map Items and Controls...
      /// </summary>
      public void ClearAll()
      {
         Items.Clear();
         if (ListView != null)
         {
            ListView.Items.Clear();
         }
         //foreach(var booklet in Book.Items)
         //{
         //   booklet.Items.Clear();
         //}
         //Book.Items.Clear();
      }

      /// <summary>
      /// Create and store a booklet cell model (data only — no UI control).
      /// This is the UI-free part of <see cref="AddControl"/> so cell creation
      /// can be unit-tested without a WinUI host.
      /// </summary>
      /// <param name="cellType">cell type (code or text)</param>
      /// <param name="referenceId">parent map-item reference id</param>
      /// <param name="bookletCell">(optional) existing booklet cell to reuse</param>
      /// <returns>the created (or given) booklet cell</returns>
      public BookletCellInfo CreateCell(
         BookletCellType cellType, string referenceId,
         BookletCellInfo bookletCell = null)
      {
         if (SelectedBooklet == null)
         {
            SelectedBooklet = new BookletInfo();
         }

         BookletCellInfo cell = bookletCell ?? new BookletCellInfo
         {
            BookletId = SelectedBooklet.BookletId,
            CellType = cellType,
            ReferenceId = referenceId,
            TextType = cellType == BookletCellType.Text ?
               BookletTextType.Markdown : BookletTextType.JSONata
         };

         SelectedBooklet.SelectedCell = cell;

         if (bookletCell == null)
         {
            SelectedBooklet.Items.Add(cell);
         }

         return cell;
      }

      /// <summary>
      /// Move a cell one position down within the selected booklet (pure —
      /// no UI side-effects). Reorder logic decoupled from RefreshMapItem so
      /// it can be unit-tested without a WinUI host.
      /// </summary>
      /// <param name="cell">cell to move down</param>
      /// <returns>true if the cell was moved; false otherwise</returns>
      public bool MoveCellDown(BookletCellInfo cell)
      {
         if (cell == null || SelectedBooklet == null)
         {
            return false;
         }

         var items = SelectedBooklet.Items;
         if (items.Count < 2)
         {
            return false;
         }

         for (int i = 0; i < items.Count; i++)
         {
            if (cell.CellId != items[i].CellId)
            {
               continue;
            }

            if (i + 1 >= items.Count)
            {
               // already last — nothing to move
               return false;
            }

            items[i] = items[i + 1];
            items[i + 1] = cell;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Delete a cell from the selected booklet (pure model removal). When a
      /// UI control instance is present it is also removed from the ListView.
      /// </summary>
      /// <param name="cell">cell to delete</param>
      /// <returns>true if the cell was removed; false otherwise</returns>
      public bool DeleteCell(BookletCellInfo cell)
      {
         if (cell == null || SelectedBooklet == null)
         {
            return false;
         }

         bool removed = SelectedBooklet.Items.Remove(cell);

         // clear the selected-cell reference if it pointed at the deleted cell
         if (removed && ReferenceEquals(SelectedBooklet.SelectedCell, cell))
         {
            SelectedBooklet.SelectedCell = null;
         }

         // remove the cell's UI control from the list view, when present
         if (ListView != null && cell.Instance != null &&
            ListView.Items.Contains(cell.Instance))
         {
            ListView.Items.Remove(cell.Instance);
         }

         return removed;
      }

      /// <summary>
      /// Add booklet cell control to currently viewed map-item.
      /// </summary>
      /// <param name="model">instance of BookViewModel</param>
      /// <param name="cellType">cell type (code or text)</param>
      /// <param name="referenceId">parent map-item reference id</param>
      /// <param name="bookletCell">(optional) booklet cell</param>
      /// <returns>booklset cell as added</returns>
      public BookletCellInfo AddControl(
         BookViewModel model, BookletCellType cellType, string referenceId,
         BookletCellInfo bookletCell = null)
      {
         // create the booklet cell model (pure data, no UI), then wire the
         // WinUI control onto it.
         BookletCellInfo cell = CreateCell(cellType, referenceId, bookletCell);

         CellViewModel cellModel = new CellViewModel();
         cellModel.ViewModel = model;

         IBookCellView control = null;
         switch(cellType)
         {
            case BookletCellType.Code:
               var cctrl = new BookletCodeCellControl
               {
                  ViewModel = cellModel,
                  Tag = cell
               };
               cctrl.FramePanel.Tag = cell;
               cctrl.SetInputText(
                  Context.LanguageInstance.GetDefaultEmptyCodeText());
               cctrl.SetCell(cell);
               control = cctrl;
               cellModel.BaseControl = cctrl;
               break;
            case BookletCellType.Text:
            default:
               var tctrl = new BookletTextCellControl
               {
                  ViewModel = cellModel,
                  Tag = cell
               };
               tctrl.FramePanel.Tag = cell;
               tctrl.SetCell(cell);
               control = tctrl;
               cellModel.BaseControl = tctrl;
               break;
         }

         if (control != null)
         {
            ListView.Items.Add(control);
            //Items.Add(control);
            cell.Instance = control;
         }

         var itm = Book.Items.Find(
            (x) => x.BookletId == SelectedBooklet.BookletId);
         if (itm == null)
         {
            Book.Items.Add(SelectedBooklet);
         }

         return cell;
      }

      #endregion

   }

}
