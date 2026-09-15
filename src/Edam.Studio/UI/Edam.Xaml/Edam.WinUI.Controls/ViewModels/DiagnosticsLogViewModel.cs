using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
using Edam.Helpers;
using Edam.Diagnostics;
using Edam.WinUI.Controls.Logging;
using System.Collections.ObjectModel;

namespace Edam.WinUI.Controls.ViewModels
{

   public class DiagnosticsLogViewModel : ObservableObject
   {
      private readonly ResultLog m_ResultLog = new ResultLog();
      private readonly InMemoryLoggerProvider m_Provider;
      private readonly ObservableCollection<IMessageLogEntry> m_Items;

      public ObservableCollection<IMessageLogEntry> Items
      {
         get { return m_Items; }
      }

      public DiagnosticsLogViewModel()
      {
         m_Items = new ObservableCollection<IMessageLogEntry>();
         m_Provider = new InMemoryLoggerProvider();
         m_Provider.EntryLogged += OnEntryLogged;

         // BL-4.1: bind this log's MEL logger to the thread-safe in-memory provider,
         // replacing the mutable static ResultLog.LogMessageHandler subscription.
         // (A real LoggerFactory may also AddProvider this provider at the app root.)
         m_ResultLog.Logger = m_Provider.CreateLogger(
            GetType().FullName ?? GetType().Name);

         MessageLogEntry entry = new MessageLogEntry();
         entry.Message = "Diagnostics Log Started";
         entry.Severity = SeverityLevel.Info;
         m_ResultLog.Write(entry);
      }

      public void ClearView()
      {
         Items.Clear();
      }

      private void OnEntryLogged(InMemoryLogEntry e)
      {
         if (e == null)
            return;

         // NOTE: for the live WinUI view, marshal to the UI thread before adding to the
         // ObservableCollection (the provider is thread-safe; this VM is the UI boundary).
         MessageLogEntry entry = new MessageLogEntry();
         entry.Message = e.Message;
         entry.Source = e.Category;
         entry.Severity = ToSeverity(e.Level);
         m_Items.Add(entry);
      }

      private static SeverityLevel ToSeverity(Microsoft.Extensions.Logging.LogLevel level)
      {
         switch (level)
         {
            case Microsoft.Extensions.Logging.LogLevel.Critical:
               return SeverityLevel.Critical;
            case Microsoft.Extensions.Logging.LogLevel.Error:
               return SeverityLevel.Fatal;
            case Microsoft.Extensions.Logging.LogLevel.Warning:
               return SeverityLevel.Warning;
            case Microsoft.Extensions.Logging.LogLevel.Debug:
               return SeverityLevel.Debug;
            case Microsoft.Extensions.Logging.LogLevel.Trace:
               return SeverityLevel.Debug;
            default:
               return SeverityLevel.Info;
         }
      }

   }

}
