using System;
using System.IO;
using System.Text;

namespace Edam.Studio
{
   /// <summary>
   /// Records startup failures.
   /// <para>
   /// A WinUI app that throws while launching dies with only an exit code (or a stowed exception
   /// inside <c>Microsoft.UI.Xaml.dll</c>), which tells nobody anything. <see cref="App.OnLaunched"/>
   /// runs application initialization — app-data creation, the secured vault, dependency injection —
   /// <b>before</b> a window exists, so any failure there is silent. This writes it where it can be
   /// read (next to the executable, which is always writable for a development build).
   /// </para>
   /// </summary>
   public static class StartupDiagnostics
   {
      /// <summary>Where the last startup failure was recorded.</summary>
      public static string LogPath =>
         Path.Combine(AppContext.BaseDirectory, "startup-error.log");

      /// <summary>Where the startup trace is written (development aid).</summary>
      public static string TracePath =>
         Path.Combine(AppContext.BaseDirectory, "startup-trace.log");

      /// <summary>
      /// Record a startup milestone. Useful because a failure inside the XAML framework (or before
      /// <c>OnLaunched</c> runs) never reaches managed code, so the last milestone written names the
      /// step that failed.
      /// </summary>
      public static void Trace(string message)
      {
         try
         {
            File.AppendAllText(TracePath,
               DateTimeOffset.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine);
         }
         catch
         {
            // deliberately swallowed
         }
      }

      /// <summary>Record a startup failure. Never throws — diagnostics must not mask the cause.</summary>
      public static void Report(Exception exception)
      {
         if (exception is null) return;

         try
         {
            var text = new StringBuilder()
               .AppendLine("--- " + DateTimeOffset.Now.ToString("u"))
               .AppendLine("base directory : " + AppContext.BaseDirectory)
               .AppendLine("current dir    : " + Environment.CurrentDirectory)
               .AppendLine(exception.ToString())
               .ToString();

            File.AppendAllText(LogPath, text);
            System.Diagnostics.Debug.WriteLine(text);
         }
         catch
         {
            // deliberately swallowed
         }
      }
   }
}
