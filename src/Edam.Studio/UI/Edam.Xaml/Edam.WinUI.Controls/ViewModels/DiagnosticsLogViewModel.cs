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
      private readonly ObservableCollection<DiagnosticsEntry> m_Items;

      /// <summary>
      /// The UI thread this view lives on. The provider is shared application-wide and raises its event on
      /// the <b>calling</b> thread, so the observable collection must be touched through this queue.
      /// </summary>
      private readonly Microsoft.UI.Dispatching.DispatcherQueue m_Dispatcher;

      /// <summary>
      /// The log's lines. Each is a <see cref="DiagnosticsEntry"/> so a message can be <b>selected</b> —
      /// one, several, any combination — and copied on its own (the panel used to offer only "one or all").
      /// </summary>
      public ObservableCollection<DiagnosticsEntry> Items
      {
         get { return m_Items; }
      }

      /// <summary>How many messages are selected for copying (0 means the copy action means the whole log).</summary>
      public Int32 SelectedCount => m_Items.Count(i => i.IsSelected);

      /// <summary>True when at least one message is selected, so "copy selected" has something to do.</summary>
      public Boolean HasSelection => SelectedCount > 0;

      /// <summary>
      /// What the copy action will do, stated on the panel: a selection, or everything. The person should not
      /// have to guess what a Copy button will put on the clipboard.
      /// </summary>
      public String CopySelectionText =>
         SelectedCount == 0
            ? "nothing selected — Copy takes every message"
            : (SelectedCount == 1
               ? "1 message selected"
               : SelectedCount + " messages selected");

      /// <summary>The currently selected messages, in the order they appear in the log.</summary>
      public IReadOnlyList<DiagnosticsEntry> SelectedItems =>
         m_Items.Where(i => i.IsSelected).ToList();

      public DiagnosticsLogViewModel()
      {
         m_Dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
         m_Items = new ObservableCollection<DiagnosticsEntry>();
         m_Provider = InMemoryLoggerProvider.Shared;
         m_Provider.EntryLogged += OnEntryLogged;

         // Show whatever was logged BEFORE this view existed (startup diagnostics, configuration checks).
         // Without this, those messages are invisible — which is precisely the "nothing happened" problem
         // this panel exists to prevent.
         foreach (var existing in m_Provider.Entries)
         {
            AddEntry(existing);
         }

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

      private void OnEntryLogged(InMemoryLogEntry e) => AddEntry(e);

      private void AddEntry(InMemoryLogEntry e)
      {
         if (e == null)
            return;

         // NOTE: for the live WinUI view, marshal to the UI thread before adding to the
         // ObservableCollection (the provider is thread-safe; this VM is the UI boundary).
         MessageLogEntry entry = new MessageLogEntry();
         entry.Message = e.Message;
         entry.Source = e.Category;
         entry.Severity = ToSeverity(e.Level);

         // the view's own item: the entry plus whether it is selected for copying
         var item = new DiagnosticsEntry(entry);
         item.SelectionChanged += OnEntrySelectionChanged;

         // the provider raises on the CALLING thread (it is shared application-wide), so this UI
         // collection must be updated on the UI thread
         if (m_Dispatcher != null && !m_Dispatcher.HasThreadAccess)
         {
            m_Dispatcher.TryEnqueue(() => m_Items.Add(item));
         }
         else
         {
            m_Items.Add(item);
         }
      }

      /// <summary>Re-state what a copy would take, whenever a message is selected or deselected.</summary>
      private void OnEntrySelectionChanged(object sender, EventArgs e)
      {
         OnPropertyChanged(nameof(SelectedCount));
         OnPropertyChanged(nameof(HasSelection));
         OnPropertyChanged(nameof(CopySelectionText));
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
