using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Edam.WinUI.Controls.ViewModels;
using Edam.WinUI.Controls.DataModels;

namespace Edam.WinUI.Controls.Editors
{

   public sealed partial class CodeEditorControl : UserControl
   {

      private CodeEditorViewModel m_ViewModel;
      public CodeEditorViewModel ViewModel
      {
         get { return m_ViewModel; }
      }
      public TextDocumentModel TextDocument
      {
         get { return m_ViewModel.TextDocument; }
      }

      public CodeEditorControl()
      {
         this.InitializeComponent();
         m_ViewModel = new CodeEditorViewModel();
         DataContext = m_ViewModel;
         m_ViewModel.CodeEditor = CodeEditor;
         CodeEditor.WebMessageReceived += OnWebMessageReceived;

         // the WebView2 and the page it hosts must be able to take keyboard focus
         Loaded += (s, e) => CodeEditor.Focus(FocusState.Programmatic);
      }

      /// <summary>
      /// Handle messages posted from the Monaco editor (see code-editor.html):
      /// <c>save</c> (Ctrl-S) runs the save command, while <c>ready</c> / <c>key</c> / <c>error:…</c> are
      /// reported back to the host — so an editor that does not work says WHY instead of doing nothing.
      /// </summary>
      private void OnWebMessageReceived(
         WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
      {
         string message;
         try
         {
            message = args.TryGetWebMessageAsString();
         }
         catch (Exception)
         {
            return;
         }

         if (String.IsNullOrWhiteSpace(message))
         {
            return;
         }

         if (message == "save")
         {
            m_ViewModel.SaveRequested();
            return;
         }

         var text = message switch
         {
            "ready" => "Code editor loaded (Monaco ready).",
            "key" => "Code editor is receiving keyboard input.",
            _ => message.StartsWith("error", StringComparison.OrdinalIgnoreCase)
               ? "Code editor problem: " + message
               : null
         };

         if (text != null)
         {
            System.Diagnostics.Debug.WriteLine(text);
            m_ViewModel.NotifyCodeEditor(text);
         }
      }

   }

}
