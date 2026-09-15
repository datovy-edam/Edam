using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Edam.UI.Catalog.Models;
using Edam.Data.CatalogModel;
using Edam.Diagnostics;
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
// https://learn.microsoft.com/en-us/windows/apps/design/controls/tab-view

namespace Edam.UI.Catalog.Controls;

public sealed partial class EditorTabsControl : UserControl
{
   private int _counter = 0;
   private MonacoEditorControl _MonacoEditor;
   private MonacoEditorControl _SelectedEditor;
   private EditorTabsViewModel _viewModel = new EditorTabsViewModel();
   public EditorTabsViewModel ViewModel
   {
      get { return _viewModel; }
   }

   public EditorTabsControl()
   {
      this.InitializeComponent();
      this.DataContext = _viewModel;
   }

   /// <summary>
   /// Manage Notification Async.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="args"></param>
   /// <returns></returns>
   public async Task ManageNotificationAsync(
       object? sender, ItemContentNotificationArgs args)
   {
      string content = null;
      string docName = string.Empty;
      CatalogPathItem pathItem = null;

      var citem = args.ItemContent.ItemInstance as CatalogItemInfo;
      pathItem = citem.Tag as CatalogPathItem;

      if (args.ItemContent == null || args.ItemContent.ItemInstance == null ||
            String.IsNullOrWhiteSpace(args.ItemContent.Content))
      {
         content = string.Empty;
         docName = "[editing document name]";
      }
      else
      {
         content = args.ItemContent.Content;
         docName = citem.Title;
      }

      args.Results.Clear();
      switch (args.Type)
      {
         case ItemContentNotificationType.GetContent:
            args.ItemContent.Content = await _SelectedEditor.GetContentAsync();
            args.Results.Succeeded();
            break;
         case ItemContentNotificationType.SetContent:
            ViewModel.CurrentPathItem = pathItem;

            AddTab(pathItem, content);

            args.Results.Succeeded();
            break;
         default:
            args.Results.Failed(EventCode.Failed.ToString());
            break;
      }
   }

   /// <summary>
   /// Add Tab...
   /// </summary>
   /// <param name="item"></param>
   /// <param name="content"></param>
   private void AddTab(CatalogPathItem item, string content)
   {
      MonacoEditorViewModel model = new MonacoEditorViewModel
      {
         CurrentPathItem = item,
         Content = content,
         DocumentName = item.TreeItem.Title,
         ModelIndex = _counter
      };

      ViewModel.EditorTabs.Add(model);
      EditorTabs.SelectedItem = model;
      _counter++;
   }

   /// <summary>
   /// Close Editor while saving its content if it has changed.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="args"></param>
   private async void TabView_TabCloseRequested(
       TabView sender, TabViewTabCloseRequestedEventArgs args)
   {
      string tbLabel = "Tab [" + ViewModel.CurrentPathItem.Full + "]";
      string func = nameof(EditorTabsControl) + "::TabCloseRequest";
      MonacoEditorViewModel model =
          ViewModel.EditorTabs[EditorTabs.SelectedIndex];

      // If the document is modified, prompt to save / discard / cancel.
      if (model.IsDirty)
      {
         var choice = await PromptSaveDiscardCancelAsync(model);
         if (choice == CloseChoice.Cancel)
         {
            return; // keep the tab open
         }
         if (choice == CloseChoice.Save)
         {
            await ViewModel.UpdateModel(model);
            model.IsDirty = false;
         }
      }

      var done = ViewModel.EditorTabs.Remove(model);
      if (!done)
      {
         ResultLog.Trace(tbLabel + " was not removed...", func,
             SeverityLevel.Fatal);
         return;
      }

      ResultLog.Trace(tbLabel + " was removed...", func, SeverityLevel.Info);
   }

   private enum CloseChoice { Save, Discard, Cancel }

   /// <summary>
   /// Prompt the user to save, discard, or cancel closing a modified document.
   /// </summary>
   private async Task<CloseChoice> PromptSaveDiscardCancelAsync(
      MonacoEditorViewModel model)
   {
      var dialog = new ContentDialog
      {
         Title = "Unsaved changes",
         Content = $"Save changes to '{model.DocumentName}'?",
         PrimaryButtonText = "Save",
         SecondaryButtonText = "Discard",
         CloseButtonText = "Cancel",
         DefaultButton = ContentDialogButton.Primary,
         XamlRoot = this.XamlRoot
      };
      var result = await dialog.ShowAsync();
      switch (result)
      {
         case ContentDialogResult.Primary:
            return CloseChoice.Save;
         case ContentDialogResult.Secondary:
            return CloseChoice.Discard;
         default:
            return CloseChoice.Cancel;
      }
   }

   private void TabView_SizeChanged(object sender, SizeChangedEventArgs args)
   {
      double asize = this.ActualHeight - (TabMenu.ActualHeight);
      EditorTabs.Height = asize;
   }

   private async void MonacoEditor_Loaded(object sender, RoutedEventArgs e)
   {
      var model = EditorTabs.SelectedItem as MonacoEditorViewModel;
      var editorControl = sender as MonacoEditorControl;
      if (editorControl != null && editorControl.Tag == null)
      {
         model.EditorInstance = editorControl.ViewModel.EditorInstance;
         _MonacoEditor = editorControl;
         _MonacoEditor.Tag = model;
         _MonacoEditor.EditorSaveRequested += EditorSaveRequestedHandler;
         _MonacoEditor.EditorContentChanged += EditorContentChangedHandler;
         await editorControl.InitializeEditorControlAsync(model);
      }
   }

   /// <summary>
   /// Handle a Ctrl-S / Cmd-S save request from the editor by persisting the
   /// current document content.
   /// </summary>
   private async void EditorSaveRequestedHandler(object? sender, EventArgs e)
   {
      var model = EditorTabs.SelectedItem as MonacoEditorViewModel;
      if (model != null)
      {
         await ViewModel.UpdateModel(model);
         model.IsDirty = false;
      }
   }

   /// <summary>
   /// Mark the current document as modified when the editor content changes.
   /// </summary>
   private void EditorContentChangedHandler(object? sender, EventArgs e)
   {
      var model = EditorTabs.SelectedItem as MonacoEditorViewModel;
      if (model != null)
      {
         model.IsDirty = true;
      }
   }

}
