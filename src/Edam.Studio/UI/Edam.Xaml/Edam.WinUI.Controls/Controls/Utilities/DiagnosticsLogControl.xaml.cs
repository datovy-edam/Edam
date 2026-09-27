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
      /// Put the whole log on the clipboard — one line per entry (timestamp + message) — so it can be
      /// pasted into a report, an issue or a message for investigation. Individual entries are selectable
      /// in the list as well.
      /// </summary>
      private void CopyAllButton_Click(object sender, RoutedEventArgs e)
      {
         var text = new System.Text.StringBuilder();

         foreach (var item in m_ViewModel.Items)
         {
            if (item == null)
            {
               continue;
            }

            text.AppendLine(item.LoggedDateTimeText + "  " + item.ErrorMessage);
         }

         CopyToClipboard(text.ToString());
      }

      /// <summary>Copy text to the clipboard; a failed copy is ignored (it must never disturb the view).</summary>
      private static void CopyToClipboard(string text)
      {
         try
         {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(text ?? String.Empty);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
         }
         catch (Exception)
         {
            // ignore
         }
      }
   }

}
