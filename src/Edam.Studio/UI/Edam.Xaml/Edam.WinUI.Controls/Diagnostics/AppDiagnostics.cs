using System;
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
   }

}
