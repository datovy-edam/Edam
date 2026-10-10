using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
// -----------------------------------------------------------------------------
using Edam.WinUI.Controls.ViewModels;

namespace Edam.WinUI.Controls.Utilities
{

   public sealed partial class DiagnosticsLogControl : UserControl
   {
      DiagnosticsLogViewModel m_ViewModel = new DiagnosticsLogViewModel();

      public DiagnosticsLogControl()
      {
         this.InitializeComponent();
         DataContext = m_ViewModel;
      }

      private void ClearViewButton_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.ClearView();
      }

      /// <summary>
      /// Put the <b>selected</b> messages on the clipboard — one line per message (timestamp + text). The
      /// button is enabled only when something is selected and the panel states how many, so what lands on the
      /// clipboard is never a guess: someone investigating an issue needs a few lines, not the whole log.
      /// </summary>
      private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
      {
         CopyToClipboard(BuildText(m_ViewModel.SelectedItems));
      }

      /// <summary>
      /// Put the whole log on the clipboard — one line per entry (timestamp + message) — so it can be
      /// pasted into a report, an issue or a message for investigation. Individual entries are selectable
      /// in the list as well.
      /// </summary>
      private void CopyAllButton_Click(object sender, RoutedEventArgs e)
      {
         CopyToClipboard(BuildText(m_ViewModel.Items));
      }

      /// <summary>
      /// Copy exactly the message that was right-clicked — without disturbing what is ticked (the context
      /// menu is the one-click path for "this line", the ticks are the path for several).
      /// </summary>
      private void CopyEntry_Click(object sender, RoutedEventArgs e)
      {
         var element = sender as FrameworkElement;
         var entry = element?.Tag as DiagnosticsEntry ?? element?.DataContext as DiagnosticsEntry;
         if (entry == null)
         {
            return;
         }

         CopyToClipboard(BuildText(new[] { entry }));
      }

      /// <summary>
      /// The clipboard text: the log <b>FILE</b> path first (the channel that cannot be lost to a clipboard or
      /// a closed window), then one line per message — the ONE place a copied message is rendered.
      /// </summary>
      private static string BuildText(IEnumerable<DiagnosticsEntry> entries)
      {
         var text = new System.Text.StringBuilder();

         // the file mirror is the reliable channel, so name it in the copied text
         text.AppendLine("--- Edam diagnostics (full log file: " +
            Edam.WinUI.Controls.Logging.AppDiagnostics.LogFilePath + ") ---");

         foreach (var entry in entries)
         {
            if (entry == null)
            {
               continue;
            }

            text.AppendLine(entry.Line);
         }

         return text.ToString();
      }

      /// <summary>Copy text to the clipboard; a failed copy is ignored (it must never disturb the view).</summary>
      private static void CopyToClipboard(string text)
      {
         try
         {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(text ?? String.Empty);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            // without Flush the content can be lost when the source application closes
            Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
         }
         catch (Exception)
         {
            // ignore
         }
      }
   }

}
