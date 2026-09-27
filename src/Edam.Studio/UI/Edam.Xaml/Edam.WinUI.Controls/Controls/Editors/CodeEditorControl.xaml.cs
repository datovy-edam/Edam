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

         Loaded += OnEditorLoaded;
      }

      /// <summary>
      /// Serve the editor page from a <b>virtual host</b> instead of <c>file://</c>. A file:// page is a
      /// per-file opaque origin, which makes uncaught script errors unreadable ("Script error.") and
      /// typically blocks Monaco's web workers; mapped to a virtual host the page is same-origin. If the
      /// mapping cannot be made, the already-resolved file URI is left in place.
      /// </summary>
      private async void OnEditorLoaded(object sender, RoutedEventArgs e)
      {
         const string EDITOR_VIRTUAL_HOST = "edam.editor";

         try
         {
            await CodeEditor.EnsureCoreWebView2Async();

            var webRoot = CodeEditorViewModel.CodeEditorWebRoot;
            var virtualUri = CodeEditorViewModel.GetVirtualCodeEditorUri(EDITOR_VIRTUAL_HOST);

            if (CodeEditor.CoreWebView2 != null && !String.IsNullOrWhiteSpace(virtualUri) &&
                !String.IsNullOrWhiteSpace(webRoot) && Directory.Exists(webRoot))
            {
               CodeEditor.CoreWebView2.SetVirtualHostNameToFolderMapping(
                  EDITOR_VIRTUAL_HOST, webRoot, CoreWebView2HostResourceAccessKind.Allow);
               m_ViewModel.UrlSource = new Uri(virtualUri);
            }
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine(
               "Code editor virtual host mapping unavailable: " + ex.Message);
         }

         CodeEditor.Focus(FocusState.Programmatic);
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

         // "Script error." is deliberately opaque for cross-origin scripts (file:// pages): it is logged
         // but not presented as a problem, since the editor is demonstrably working once 'ready' arrives.
         var text = message switch
         {
            "ready" => "Code editor loaded (Monaco ready).",
            "key" => "Code editor is receiving keyboard input.",
            _ when message.StartsWith("error", StringComparison.OrdinalIgnoreCase) =>
               message.IndexOf("script error", StringComparison.OrdinalIgnoreCase) >= 0
                  ? "Code editor reported a hidden script error (page origin hides the detail)."
                  : "Code editor problem: " + message,
            _ => null
         };

         if (text != null)
         {
            System.Diagnostics.Debug.WriteLine(text);
            m_ViewModel.NotifyCodeEditor(text);
         }
      }

   }

}
