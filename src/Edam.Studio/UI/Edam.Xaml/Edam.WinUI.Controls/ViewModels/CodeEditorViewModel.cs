using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.UI.Xaml.Controls;

// -----------------------------------------------------------------------------
using Edam.Application;
using Edam.Helpers;
using Edam.Diagnostics;
using Edam.Data.AssetManagement.Helpers;
using Edam.Data.AssetManagement;
using Edam.WinUI.Controls.DataModels;
using Edam.WinUI.Controls.Common;
using Edam.Data.AssetProject;

namespace Edam.WinUI.Controls.ViewModels
{

   public class CodeEditorViewModel : ObservableObject
   {
      private const string DEFAULT_CODE_EDITOR_KEY = "DefaultCodeEditorKey";
      private const string CODE_EDITOR_URL_KEY = "CodeEditorUrl";

      private TextDocumentModel m_TextDocument;
      private ResultLog m_ResultsLog = new ResultLog();
      private Uri m_UrlSource;
      private static string m_CodeEditorPath = "";

      public DataTextMap DataTextMap { get; set; }
      public WebView2 CodeEditor = null;

      public Uri UrlSource
      {
         get { return m_UrlSource; }
         set
         {
            if (m_UrlSource != value)
            {
               m_UrlSource = value;
               OnPropertyChanged("UrlSource");
            }
         }
      }

      public TextDocumentModel TextDocument
      {
         get { return m_TextDocument; }
      }
      public string LanguageText
      {
         get { return m_TextDocument.LanguageText; }
         set { m_TextDocument.LanguageText = value; OnPropertyChanged(); }
      }

      public NotificationEvent NotifyCodeEditorEvent { get; set; }

      public CodeEditorViewModel()
      {
         Navigate(GetDefaultCodeEditorUri());
         m_TextDocument = new TextDocumentModel(this);
         DataTextMap = Project.GetDataTextMapByKey();
      }

      /// <summary>
      /// Get Default Code Editor URI
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      public static string GetDefaultCodeEditorUri(string path = null)
      {
         if (!String.IsNullOrWhiteSpace(m_CodeEditorPath))
         {
            return m_CodeEditorPath;
         }

         string key = AppSettings.GetSectionString(DEFAULT_CODE_EDITOR_KEY);
         if (string.IsNullOrEmpty(key))
         {
            key = CODE_EDITOR_URL_KEY;
         }
         string url = AppSettings.GetSectionString(key);
         if (string.IsNullOrEmpty(url))
         {
            return null;
         }

         // The editor's web assets ship WITH THE APPLICATION (…\web\monaco-editor\…), so the base is
         // the application folder — the caller may pass one, and the app-data folder stays a fallback
         // for layouts that carry them there. The process current directory is never used (ADR-0011).
         var relative = url.Replace('\\', '/').TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

         var bases = new List<string>();
         if (!String.IsNullOrWhiteSpace(path))
         {
            bases.Add(path);
         }
         bases.Add(AppContext.BaseDirectory);
         bases.Add(ConfigurationHelper.GetAbsoluteAppDataPath(String.Empty));

         foreach (var candidateBase in bases)
         {
            if (String.IsNullOrWhiteSpace(candidateBase))
            {
               continue;
            }

            var candidate = Path.Combine(candidateBase.TrimEnd('/', '\\'), relative);
            if (File.Exists(candidate))
            {
               m_CodeEditorPath = ConfigurationHelper.GetAbsoluteFileUri(candidate);
               return m_CodeEditorPath;
            }
         }

         // not found: still navigate to the application-folder location, so the WebView reports a
         // clear failure instead of showing nothing at all
         m_CodeEditorPath = ConfigurationHelper.GetAbsoluteFileUri(
            Path.Combine(AppContext.BaseDirectory.TrimEnd('/', '\\'), relative));
         return m_CodeEditorPath;
      }

      public async Task<ResultLog> SetEditorText(String text, String language)
      {
         ResultLog res = new ResultLog();

         string etext = (text.ReplaceLineEndings()).
            Replace("\r", "\\r").Replace("\n","\\n").Replace("\t","\\t").
            Replace("'","\\u0027");

         string lang = DataTextMap.MapText(language, DataTextMapDirection.From);
         try
         {
            var results = await CodeEditor.ExecuteScriptAsync(
               $"setEditorText('{etext}','{lang}');");
            res.Succeeded();
         }
         catch (Exception ex)
         {
            res.Failed(ex);
         }
         return res;
      }

      public async Task<string> GetEditorText()
      {
         var text = await CodeEditor.ExecuteScriptAsync("getEditorText();");
         text = text == null ? String.Empty :
            text.
               Replace("\\r","\r").Replace("\\n","\n").Replace("\\\"", "\"").
               Replace("\\u003C","<").Replace("\\t","   ").Trim().Trim('"');

         TextDocument.Text = text;
         return text;
      }

      public void NotifyEditorTextAvailable(TextDocumentModel textDocument)
      {
         if (NotifyCodeEditorEvent != null)
         {
            NotificationArgs args = new NotificationArgs();
            args.Type = NotificationType.AssetSaveTextRequested;
            args.EventData = textDocument;
            NotifyCodeEditorEvent(this, args);
         }
      }

      /// <summary>
      /// Request a save of the current editor content. Invoked when the user
      /// presses Ctrl-S (or Cmd-S) in the Monaco editor, which posts a 'save'
      /// message to the host. Reading the editor text sets TextDocument.Text,
      /// which raises the AssetSaveTextRequested notification.
      /// </summary>
      public async void SaveRequested()
      {
         await GetEditorText();
      }

      public void Navigate(string url)
      {
         try
         {
            m_ResultsLog.Clear();
            UrlSource = new Uri(url);
         }
         catch (Exception ex)
         {
            m_ResultsLog.Failed(ex);
         }
      }

   }

}

