using System;
using System.IO;
using Edam.Application;
using Edam.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Edam.WinUI.Controls.Logging
{

   /// <summary>
   /// The single place application code writes <b>diagnostics</b>: entries land in the shared
   /// <see cref="InMemoryLoggerProvider"/> that the WinUI diagnostics view subscribes to — so a message
   /// never has to be smuggled into a title bar, a status line or a tooltip.
   /// <para>
   /// Callers do not need a logger factory. A composition root may additionally bind the legacy
   /// <c>Edam.Diagnostics.Log</c> facade (and anything else using MEL) to the same provider:
   /// <c>factory.AddProvider(InMemoryLoggerProvider.Shared); Log.UseLogging(factory);</c>
   /// </para>
   /// </summary>
   public static class AppDiagnostics
   {
      private static Boolean m_ResultLogBridged;
      private static readonly object FileGate = new object();
      private static String m_LogFilePath;

      /// <summary>
      /// Where diagnostics are ALSO written as plain lines — so the log can be opened, copied or attached
      /// even when the in-app panel cannot be copied, or after the application has closed. Best effort: a
      /// log file that cannot be written must never disturb the application.
      /// </summary>
      public static String LogFilePath
      {
         get
         {
            if (m_LogFilePath is null)
            {
               try
               {
                  var root = AppData.GetApplicationDataLocation();
                  m_LogFilePath = String.IsNullOrWhiteSpace(root)
                     ? "(no app-data location)"
                     : Path.Combine(root, "Edam.Diagnostics.log");
               }
               catch (Exception)
               {
                  m_LogFilePath = "(unavailable)";
               }
            }

            return m_LogFilePath;
         }
      }

      static AppDiagnostics()
      {
         BridgeResultLog();
      }

      /// <summary>Write a diagnostic message to the diagnostics view.</summary>
      /// <param name="message">message to show</param>
      /// <param name="level">MEL severity (defaults to Information)</param>
      /// <param name="category">optional source/category</param>
      public static void Write(String message,
         LogLevel level = LogLevel.Information, String category = null)
      {
         if (String.IsNullOrWhiteSpace(message))
         {
            return;
         }

         WriteToFile(level, message, category ?? "Edam.Studio");

         InMemoryLoggerProvider.Shared
            .CreateLogger(category ?? "Edam.Studio")
            .Log(level, message);
      }

      /// <summary>Append one line to <see cref="LogFilePath"/>; never throws, never blocks the caller.</summary>
      private static void WriteToFile(LogLevel level, String message, String category)
      {
         try
         {
            var path = LogFilePath;
            if (String.IsNullOrWhiteSpace(path) || path.StartsWith("(", StringComparison.Ordinal))
            {
               return;
            }

            var folder = Path.GetDirectoryName(path);
            if (!String.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
            {
               Directory.CreateDirectory(folder);
            }

            lock (FileGate)
            {
               File.AppendAllText(path,
                  DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " +
                  level + "  " + category + "  " + message + Environment.NewLine);
            }
         }
         catch (Exception)
         {
            // a log file that cannot be written is never fatal
         }
      }

      /// <summary>
      /// Route <see cref="ResultLog"/> messages here. A <c>ResultLog</c> instance raises its <b>static</b>
      /// <c>LogMessageHandler</c> delegate from <c>Write</c> and from every <c>LogMessage(...)</c> overload,
      /// and the diagnostics panel used to subscribe to it directly — the BL-4.1 MEL migration removed
      /// that subscription and left the delegate <b>null</b>, so those messages went nowhere. Bridging it
      /// restores that contract: adding a message through a <c>ResultLog</c> instance shows up in the
      /// diagnostics view. The entry is not cancelled, so other handlers/behaviour are unaffected.
      /// </summary>
      private static void BridgeResultLog()
      {
         if (m_ResultLogBridged)
         {
            return;
         }
         m_ResultLogBridged = true;

         ResultLog.LogMessageHandler += OnResultLogMessage;
      }

      /// <summary>
      /// Forward a <b>ResultLog</b> message into the diagnostics view. The message is read through
      /// <see cref="IMessageLogEntry"/> (<c>ErrorMessage</c> composes the text with any attached exception)
      /// and is <b>not</b> cancelled, so other subscribers and existing behaviour are unaffected.
      /// </summary>
      private static void OnResultLogMessage(Object sender, LogMessageEventArgs e)
      {
         var message = e?.Message;
         if (message == null)
         {
            return;
         }

         Write(message.ErrorMessage,
            ToLogLevel(message.Severity),
            e.ParentLog != null ? e.ParentLog.GetType().Name : null);
      }

      /// <summary>Map the legacy severity onto MEL (mirrors DiagnosticsLogViewModel.ToSeverity).</summary>
      private static LogLevel ToLogLevel(SeverityLevel severity)
      {
         switch (severity)
         {
            case SeverityLevel.Critical:
               return LogLevel.Critical;
            case SeverityLevel.Fatal:
               return LogLevel.Error;
            case SeverityLevel.Warning:
               return LogLevel.Warning;
            case SeverityLevel.Debug:
               return LogLevel.Debug;
            case SeverityLevel.Unknown:
               return LogLevel.Trace;
            default:
               return LogLevel.Information;
         }
      }
   }

}
