using System;
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

         InMemoryLoggerProvider.Shared
            .CreateLogger(category ?? "Edam.Studio")
            .Log(level, message);
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
