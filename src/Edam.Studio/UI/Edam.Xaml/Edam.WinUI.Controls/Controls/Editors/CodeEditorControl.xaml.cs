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
      }

      /// <summary>
      /// Handle messages posted from the Monaco editor (see code-editor.html).
      /// A 'save' message (Ctrl-S / Cmd-S) triggers the save command.
      /// </summary>
      private void OnWebMessageReceived(
         WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
      {
         // postMessage('save') arrives as the JSON string "\"save\"".
         if (args.WebMessageAsJson == "\"save\"")
         {
            m_ViewModel.SaveRequested();
         }
      }

   }

}
