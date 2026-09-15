using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Edam.WinUI.Controls.Logging
{

   /// <summary>
   /// A single structured in-memory log entry surfaced to the WinUI diagnostics view.
   /// </summary>
   public sealed class InMemoryLogEntry
   {
      public DateTime TimestampUtc { get; set; }
      public String Category { get; set; }
      public LogLevel Level { get; set; }
      public String Message { get; set; }
      public Exception Exception { get; set; }
   }

   /// <summary>
   /// BL-4.1: thread-safe, in-memory Microsoft.Extensions.Logging (MEL) <see cref="ILoggerProvider"/>
   /// that surfaces log entries to the WinUI diagnostics view. Replaces the mutable static
   /// <c>ResultLog.LogMessageHandler</c> / <c>ResultLog.DefaultLog</c> wiring: entries are UTC-stamped,
   /// appended under a lock, and raised on the calling thread via <see cref="EntryLogged"/>.
   /// </summary>
   public sealed class InMemoryLoggerProvider : ILoggerProvider
   {
      private readonly object m_Sync = new object();
      private readonly List<InMemoryLogEntry> m_Entries = new List<InMemoryLogEntry>();

      /// <summary>Upper bound on retained entries (ring buffer behavior).</summary>
      public int MaxEntries { get; set; } = 5000;

      /// <summary>Raised on the logger's calling thread each time an entry is logged (thread-safe).</summary>
      public event Action<InMemoryLogEntry> EntryLogged;

      /// <summary>Snapshot of retained entries (copy, safe to read concurrently).</summary>
      public IReadOnlyList<InMemoryLogEntry> Entries
      {
         get { lock (m_Sync) { return m_Entries.ToArray(); } }
      }

      public ILogger CreateLogger(String categoryName)
      {
         return new InMemoryLogger(this, categoryName ?? String.Empty);
      }

      public void Dispose()
      {
      }

      internal void AddEntry(InMemoryLogEntry entry)
      {
         lock (m_Sync)
         {
            m_Entries.Add(entry);
            if (m_Entries.Count > MaxEntries && MaxEntries > 0)
               m_Entries.RemoveAt(0);
         }
         EntryLogged?.Invoke(entry);
      }

      private sealed class InMemoryLogger : ILogger
      {
         private readonly InMemoryLoggerProvider m_Owner;
         private readonly String m_Category;

         public InMemoryLogger(InMemoryLoggerProvider owner, String category)
         {
            m_Owner = owner;
            m_Category = category;
         }

         public IDisposable BeginScope<TState>(TState state)
         {
            return null;
         }

         public bool IsEnabled(LogLevel logLevel)
         {
            return true;
         }

         public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, String> formatter)
         {
            if (formatter == null)
               return;

            m_Owner.AddEntry(new InMemoryLogEntry
            {
               TimestampUtc = DateTime.UtcNow,
               Category = m_Category,
               Level = logLevel,
               Message = formatter(state, exception),
               Exception = exception
            });
         }
      }
   }

}
