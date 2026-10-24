// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Edam.WinUI.Controls.ViewModels;
using Edam.WinUI.Controls.DataModels;
using Edam.WinUI.Controls.Common;
using Edam.InOut;
using Edam.WinUI.Controls.Editors;

namespace Edam.WinUI.Controls.Projects
{

   public sealed partial class ProjectFileEditorControl : UserControl
   {

      private ProjectViewerViewModel m_ViewModel;
      public ProjectViewerViewModel ViewModel
      {
         get
         {
            return m_ViewModel;
         }
      }

      public TextBlock FileNameTextBlock
      {
         get
         {
            return LoadedFileName;
         }
      }

      public NotificationEvent AssetViewerCommandEvent { get; set; }
      CodeEditorControl EditorControl { get; set; }

      public ProjectFileEditorControl()
        {
            this.InitializeComponent();

         AssetViewerCommandEvent = null;
         m_ViewModel = new ProjectViewerViewModel();

         EditorControl = new CodeEditorControl();
         EditorControl.ViewModel.NotifyCodeEditorEvent =
            ManageNotification;
         EditorHost.Children.Add(EditorControl);
      }

      public void ManageNotification(object sender, NotificationArgs args)
      {
         if (args.Type == NotificationType.AssetSaveTextRequested)
         {
            TextDocumentModel text = args.EventData as TextDocumentModel;
            if (text != null)
            {
               m_ViewModel.ItemSave(LoadedFileName.Text, text.Text);

               // Ctrl-S saves through here, and the banner's Save through SaveText — the SAME report must come
               // out of both, or one of the two ways of saving looks like it did nothing.
               ReportSaved(LoadedFileName.Text);
            }
            return;
         }
         else if (args.Type != NotificationType.AssetDataSetAvailable)
         {
            return;
         }

         // This is the editor's DOCUMENT title area: it reports file/data-set activity only. Diagnostics
         // (including the code editor's own status) go to the diagnostics view — see AppDiagnostics.
         m_ViewModel.NotifiedMessageText = "Loaded " + args.MessageText;
      }

      /// <summary>
      /// Say that the document was saved — <b>one</b> definition, used by the banner's Save and by the editor's
      /// Ctrl-S, so the two cannot diverge (item 131's lesson: a command must answer, however it was invoked).
      /// </summary>
      private void ReportSaved(String name)
      {
         m_ViewModel.NotifiedMessageText = "Saved " + name;

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            "Saved '" + name + "'.",
            Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
      }

      public void SetEditorText(ProjectItem item, string text)
      {
         if (!m_ViewModel.NewItemSelected(item))
         {
            EditorControl.TextDocument.SetText(
               Microsoft.UI.Text.TextSetOptions.None, text,
               item == null ? String.Empty : item.Item.ExtensionName);
            return;
         }
         PlayButton.Visibility = item.CanExecute ?
            Visibility.Visible : Visibility.Collapsed;
         EditorControl.TextDocument.SetText(
            Microsoft.UI.Text.TextSetOptions.None, text,
            item.Item.ExtensionName);
      }

      #region -- 4.00 - Save Editor Text in File or Storage...

      /// <summary>
      /// <b>S3-6: the banner's Save does what its label says.</b> It used to read the editor text and then throw
      /// it away — the <c>ItemSave</c> call was commented out — so the only way to save was the editor's own
      /// Ctrl-S, and a button that looks like Save and does nothing is the same defect class as the silent command
      /// (item 133). It now saves through the <b>same</b> path Ctrl-S uses (<c>ItemSave</c>), so the two ways of
      /// saving cannot diverge, and it <b>answers</b>: the document title states it, the diagnostics record it, and a
      /// failure is reported instead of swallowed.
      /// </summary>
      public async void SaveText()
      {
         try
         {
            var text = await EditorControl.ViewModel.GetEditorText();
            if (text is null) return;

            m_ViewModel.ItemSave(LoadedFileName.Text, text);

            ReportSaved(LoadedFileName.Text);
         }
         catch (Exception ex)
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "'" + LoadedFileName.Text + "' could not be saved: " + ex.Message,
               Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
         }
      }

      private void ProjectSave_Click(object sender, RoutedEventArgs e)
      {
         SaveText();
      }

      /// <summary>
      /// <b>Validate the document being EDITED</b> (the Projects specification §2.3) — the text as it stands,
      /// unsaved edits included, because that is what the person is looking at; a file already written to disk
      /// tells them nothing about what they have typed since.
      /// <para>
      /// On demand only (nothing validates behind theirback), and the findings go to the diagnostics panel, which
      /// <b>expands itself</b> when there are any — so the answer is visible without hunting for it. A clean
      /// document changes nothing on screen but the one line on the record.
      /// </para>
      /// </summary>
      private async void ProjectValidate_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            var text = await EditorControl.ViewModel.GetEditorText();
            var path = m_ViewModel.SelectedItem == null ? null : m_ViewModel.SelectedItem.Item.Full;

            var summary = await ProjectServicesHelper.ValidateArgumentsAsync(
               path, text,
               ProjectServicesHelper.ProjectNameFromResourcePath(path),
               m_ViewModel.SelectedUriItem == null ? null : m_ViewModel.SelectedUriItem.UriText);

            if (summary is not null)
            {
               // issues were found: reveal the panel that holds them, rather than hoping the person looks
               ProjectServicesHelper.RequestAttention();
            }
         }
         catch (Exception ex)
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Arguments document validation failed: " + ex.Message,
               Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
         }
      }

      private void EditorControl_KeyDown(object sender, KeyRoutedEventArgs e)
      {
         //if (e.Key == Windows.System.VirtualKey.S)
         //{
         //   Windows.UI.Core.CoreVirtualKeyStates ctrlKey =
         //      Microsoft.UI.Input.InputKeyboardSource.
         //         GetKeyStateForCurrentThread(
         //            Windows.System.VirtualKey.Control);
         //   if (ctrlKey == CoreVirtualKeyStates.Down)
         //   {
         //      SaveText();
         //   }
         //}
      }

      #endregion

      private void ProjectPlay_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.NotifiedMessageText = String.Empty;
         if (AssetViewerCommandEvent != null && ViewModel.SelectedItem != null)
         {
            NotificationArgs a = new NotificationArgs();
            a.Type = NotificationType.RunProjectItem;
            a.EventData = ViewModel.SelectedItem;
            AssetViewerCommandEvent(this, a);
         }
      }

      private void ProjectClean_Click(object sender, RoutedEventArgs e)
      {

      }

   }

}
