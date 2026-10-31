// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Edam.WinUI.Controls.Application;
using Edam.WinUI.Helpers;
using EdamAppData = Edam.Application.AppData;

namespace Edam.Studio
{
   /// <summary>
   /// Provides application-specific behavior to supplement the default Application class.
   /// </summary>
   public partial class App : Microsoft.UI.Xaml.Application
   {
      // ---- what a C++/WinRT failure actually was ------------------------------------------------
      // A first-chance exception surfaces to a debugger as "Microsoft C++ exception: winrt::hresult_error" with
      // an address — no method, no HRESULT, nothing a person can act on. Translated into managed code it is a
      // COMException carrying both, so the hooks below record it: the HRESULT, the type and the stack, into the
      // diagnostics LOG FILE (the channel that survives the process). Only WinRT/COM-shaped failures are
      // recorded, and the last one is repeated at exit, so the log ends with an answer instead of a flood —
      // FirstChanceException fires for every CAUGHT exception too, and the XAML/WebView2 plumbing throws and
      // catches internally all the time.
      private const Int32 RO_E_CLOSED = unchecked((Int32)0x80000013);
      private const Int32 RPC_E_DISCONNECTED = unchecked((Int32)0x80010108);
      private const Int32 RPC_E_WRONG_THREAD = unchecked((Int32)0x8001010E);
      private const Int32 WIN32_OPERATION_ID_NOT_VALID = unchecked((Int32)0x800710DD);
      private const Int32 ELEMENT_NOT_FOUND = unchecked((Int32)0x80070490);

      private static String? m_LastWinRtFailure;

      /// <summary>
      /// Initializes the singleton application object.  This is the first line of authored code
      /// executed, and as such is the logical equivalent of main() or WinMain().
      /// </summary>
      public App()
      {
         StartupDiagnostics.Trace("App ctor: begin");
         this.InitializeComponent();
         StartupDiagnostics.Trace("App ctor: InitializeComponent completed");

         AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
         AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
         AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
         UnhandledException += OnXamlUnhandledException;
      }

      /// <summary>
      /// Invoked when the application is launched.
      /// </summary>
      /// <param name="args">Details about the launch request and process.</param>
      protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
      {
         try
         {
            StartupDiagnostics.Trace("OnLaunched: begin");
            ConfigureApplicationDataRoot();
            StartupDiagnostics.Trace("OnLaunched: application data root resolved");
            ApplicationHelper.InitializeApplication();
            StartupDiagnostics.Trace("OnLaunched: InitializeApplication completed");

            // a degraded app-data root is reported where the person can see it, now that diagnostics exist
            ReportApplicationDataRootProblem();

            m_window = new MainWindow();
            StartupDiagnostics.Trace("OnLaunched: MainWindow created");

            m_window.Activate();
            StartupDiagnostics.Trace("OnLaunched: MainWindow activated");

            ApplicationHelper.InitializeApplication(m_window);
            StartupDiagnostics.Trace("OnLaunched: completed");
         }
         catch (Exception ex)
         {
            // Without this a startup failure is silent (the user sees only a crash / exit code, and
            // the event log names Microsoft.UI.Xaml.dll but not the cause). Record it, then rethrow
            // so the failure stays visible while debugging.
            StartupDiagnostics.Report(ex);
            throw;
         }
      }

      /// <summary>
      /// Describe the security context the process is actually running in.
      /// <para>
      /// A location that is writable for the account can still be refused to <b>this</b> process: an AppContainer,
      /// a low-integrity token or a restricted token all answer <c>Access is denied</c> for a folder the same user
      /// can create from a normal shell. Rather than guess which one it is, the facts are recorded in the startup
      /// trace — the account, the integrity level and whether a package (AppContainer) SID is present.
      /// </para>
      /// </summary>
      /// <returns>One line describing the process' security context, or why it could not be read</returns>
      private static String DescribeProcessSecurity()
      {
         try
         {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
               var integrity = "unknown";
               var appContainer = "no";

               foreach (var group in identity.Groups)
               {
                  var value = group?.Value;
                  if (String.IsNullOrEmpty(value))
                  {
                     continue;
                  }

                  if (value.StartsWith("S-1-16-", StringComparison.OrdinalIgnoreCase))
                  {
                     integrity = value;
                  }
                  else if (value.StartsWith("S-1-15-2-", StringComparison.OrdinalIgnoreCase))
                  {
                     appContainer = "yes (" + value + ")";
                  }
               }

               return "user=" + identity.Name + " integrity=" + integrity + " appcontainer=" + appContainer +
                  " auth=" + identity.AuthenticationType + " interactive=" + Environment.UserInteractive;
            }
         }
         catch (Exception ex)
         {
            return "unavailable (" + ex.HResult.ToString("X8") + ": " + ex.Message.Trim() + ")";
         }
      }

      /// <summary>
      /// The per-user application-data root, and the ONE place package identity is asked for.
      /// <para>
      /// <c>ApplicationData.Current</c> needs package identity <b>and</b> a working application-data handle. When
      /// either is unavailable it throws <c>Access is denied</c> — and because this runs before any window exists,
      /// that used to be an <b>unexplained death on launch</b>: nothing on screen, and nothing to act on. A root
      /// is therefore <b>chosen and proved writable</b> here, in order, and the reason for every rejected
      /// candidate is kept so it can be <b>reported</b> rather than guessed at:
      /// packaged app data, an explicit override, a development-only loose-deployment root, then the per-user
      /// local folder and temp folder. If none can be written the application says so <b>visibly</b> and stops,
      /// because an app that cannot write anywhere cannot work.
      /// </para>
      /// <para>
      /// Deliberately <b>not</b> "Documents": on a machine where Documents is redirected to OneDrive it may be a
      /// cloud placeholder that refuses writes (observed on this machine: <c>…\OneDrive\Documents\Edam.Studio</c>
      /// is a reparse point and creating a folder inside it is denied), which would replace one unexplained
      /// startup death with another.
      /// </para>
      /// </summary>
      private static void ConfigureApplicationDataRoot()
      {
         var rejected = new List<String>();

         StartupDiagnostics.Trace("OnLaunched: security " + DescribeProcessSecurity());

         // 1. the packaged location — what everything expects when the deployment is healthy
         try
         {
            var id = Package.Current.Id;
            StartupDiagnostics.Trace(
               "OnLaunched: identity=" + id.Name + " family=" + id.FamilyName +
               " version=" + id.Version + " arch=" + id.Architecture);

            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            EdamAppData.SetApplicationDataLocation(localFolder.Path);
            StartupDiagnostics.Trace("OnLaunched: package-local app-data root configured");
            return;
         }
         catch (Exception ex)
         {
            rejected.Add("packaged application data (0x" + ex.HResult.ToString("X8") + ": " +
               ex.Message.Trim() + ")");
         }

         // an explicit root named by the environment, for hosts where the profile is not writable.
         // A machine can confine a process so thoroughly that its own %LOCALAPPDATA% refuses it while a plain
         // shell writes there happily; when that happens the user still needs to run the application, and
         // pointing it at a folder it CAN write is the difference between "unusable" and "works".
         var overrideRoot = Environment.GetEnvironmentVariable("EDAM_STUDIO_DATA_ROOT");

         if (!String.IsNullOrWhiteSpace(overrideRoot))
         {
            try
            {
               var probe = System.IO.Path.Combine(overrideRoot, "Edam.Studio");
               Directory.CreateDirectory(probe);

               EdamAppData.SetApplicationDataLocation(overrideRoot);
               m_ApplicationDataRootProblem = "This application is using the data root named by " +
                  "EDAM_STUDIO_DATA_ROOT ('" + probe + "').";

               StartupDiagnostics.Trace("OnLaunched: app-data root from EDAM_STUDIO_DATA_ROOT: '" + probe + "'");
               return;
            }
            catch (Exception ex)
            {
               rejected.Add("EDAM_STUDIO_DATA_ROOT '" + overrideRoot + "' (0x" + ex.HResult.ToString("X8") +
                  ": " + ex.Message.Trim() + ")");
            }
         }

         // 2. Visual Studio's loose/development AppX registration has package identity, but on some Windows
         // builds its ApplicationData redirection guard is unavailable. In that case ApplicationData.Current,
         // LOCALAPPDATA and TEMP can all be denied even though the account itself can write to them. Keep the
         // convenient VS deployment usable with a root beside the loose deployment, but only for a development
         // registration. A signed MSIX never takes this branch and continues to use its package LocalState.
         if (IsDevelopmentPackage())
         {
            try
            {
               var looseRoot = System.IO.Path.Combine(AppContext.BaseDirectory, ".local-data");
               var probe = System.IO.Path.Combine(looseRoot, "Edam.Studio");
               Directory.CreateDirectory(probe);

               EdamAppData.SetApplicationDataLocation(looseRoot);
               m_ApplicationDataRootProblem =
                  "Visual Studio's loose development deployment could not use packaged application data. " +
                  "For development only, this run is using '" + probe + "'. A signed MSIX uses package LocalState.";

               StartupDiagnostics.Trace(
                  "OnLaunched: loose development package app-data root configured at '" + probe + "'");
               return;
            }
            catch (Exception ex)
            {
               rejected.Add("loose development app-data root (0x" + ex.HResult.ToString("X8") + ": " +
                  ex.Message.Trim() + ")");
            }
         }

         // 3./4. per-user local folder, then temp — each one PROVED writable before it is adopted
         var candidates = new List<String>
         {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            System.IO.Path.GetTempPath()
         };

         foreach (var candidate in candidates)
         {
            if (String.IsNullOrWhiteSpace(candidate))
            {
               continue;
            }

            try
            {
               var probe = System.IO.Path.Combine(candidate, "Edam.Studio");
               Directory.CreateDirectory(probe);

               EdamAppData.SetApplicationDataLocation(candidate);
               m_ApplicationDataRootProblem =
                  "This application is not using its packaged application data: " +
                  String.Join("; ", rejected) + ". Its data is in '" + probe + "' for this run, so the answers " +
                  "recorded for this installation (the first-run markers) cannot be read or written.";

               StartupDiagnostics.Trace(
                  "OnLaunched: app-data root fell back to '" + probe + "' — " + m_ApplicationDataRootProblem);
               return;
            }
            catch (Exception ex)
            {
               rejected.Add("'" + candidate + "' (0x" + ex.HResult.ToString("X8") + ": " +
                  ex.Message.Trim() + ")");
            }
         }

         // nothing is writable: say it where the person is, instead of vanishing
         var detail =
            "EDAM Studio cannot start: it has nowhere to keep its data." + Environment.NewLine +
            Environment.NewLine +
            "Tried: " + String.Join("; ", rejected) + Environment.NewLine +
            Environment.NewLine +
            "Two things produce this, and they are easy to tell apart. Either the deployment is unhealthy — " +
            "re-deploy it (in Visual Studio: Clean, Rebuild, Deploy) and start it from its Start-menu entry, not " +
            "from the build output — or the process was started from a shell that is itself restricted (a " +
            "low-integrity or sandboxed terminal, such as an agent's shell, refuses these writes to every process " +
            "it starts even though the same account can create them from Explorer). If you are unsure, launch the " +
            "application from Explorer or the Start menu, where the restriction does not apply." + Environment.NewLine +
            Environment.NewLine +
            "Security context: " + DescribeProcessSecurity() + Environment.NewLine +
            Environment.NewLine +
            "If this machine will not let the application write to your profile at all, name a folder it can " +
            "write and start it again: set EDAM_STUDIO_DATA_ROOT (for example to a folder inside this " +
            "repository) and the application will keep its data there." + Environment.NewLine +
            Environment.NewLine +
            "Details were written to: " + StartupDiagnostics.TracePath;

         StartupDiagnostics.Trace("OnLaunched: FATAL — " + detail);
         ShowStartupFailure("EDAM Studio cannot start", detail);

         // stop here on purpose: continuing would throw somewhere less explicable, with no window to show it in
         Environment.Exit(1);
      }

      /// <summary>
      /// Returns true only for the loose AppX layout registered by Visual Studio. A signed MSIX may be
      /// developer-signed, but it is installed under WindowsApps and must keep its normal package LocalState.
      /// </summary>
      private static Boolean IsDevelopmentPackage()
      {
         try
         {
            var installedLocation = Package.Current.InstalledLocation.Path;
            return String.Equals(
               System.IO.Path.GetFileName(
                  installedLocation.TrimEnd(
                     System.IO.Path.DirectorySeparatorChar,
                     System.IO.Path.AltDirectorySeparatorChar)),
               "AppX",
               StringComparison.OrdinalIgnoreCase);
         }
         catch (Exception)
         {
            // An unpackaged process has no Package.Current; it must use the explicit override or normal fallback.
            return false;
         }
      }

      [System.Runtime.InteropServices.DllImport("user32.dll", CharSet =
         System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
      private static extern Int32 MessageBoxW(IntPtr hWnd, String text, String caption, UInt32 type);

      /// <summary>
      /// A native message box, because this failure happens <b>before</b> a window exists: a WinUI dialog would need
      /// a XamlRoot that does not exist yet, and the diagnostics panel needs the very app data that is missing.
      /// </summary>
      private static void ShowStartupFailure(String caption, String detail)
      {
         const UInt32 MB_ICONERROR = 0x00000010;
         const UInt32 MB_SETFOREGROUND = 0x00010000;

         try
         {
            MessageBoxW(IntPtr.Zero, detail, caption, MB_ICONERROR | MB_SETFOREGROUND);
         }
         catch
         {
            // a failure to explain the failure is still not allowed to throw
         }
      }

      private static String? m_ApplicationDataRootProblem;

      /// <summary>
      /// Report the app-data problem where the person can actually see it — the diagnostics panel, which only
      /// exists after <c>InitializeApplication</c>. Called from there so a startup degradation is never silent.
      /// </summary>
      public static void ReportApplicationDataRootProblem()
      {
         if (m_ApplicationDataRootProblem is null)
         {
            return;
         }

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            m_ApplicationDataRootProblem,
            Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio.Startup");

         m_ApplicationDataRootProblem = null;
      }

      private Window m_window;

      // ---- the diagnostics hooks -----------------------------------------------------------------

      /// <summary>
      /// Record a WinRT/COM-shaped failure the moment it is thrown — whether it is caught a line later (the
      /// usual case in XAML and WebView2 teardown) or becomes the failure that ends the process. Debug level, so
      /// expected internal throws do not look like defects, and the log keeps the reason.
      /// </summary>
      private static void OnFirstChanceException(
         object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
      {
         try
         {
            if (!IsWinRtShaped(e.Exception)) return;

            m_LastWinRtFailure = Describe(e.Exception);

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "WinRT failure on the way out (may be caught by the caller): " + m_LastWinRtFailure,
               Microsoft.Extensions.Logging.LogLevel.Debug, "Edam.Studio");
         }
         catch (Exception)
         {
            // a diagnostics hook must never be the reason something else fails
         }
      }

      /// <summary>A failure that reached the runtime without a handler: recorded with its stack, never swallowed.</summary>
      private static void OnUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
         => Record(e?.ExceptionObject as Exception, "unhandled (managed)");

      /// <summary>A failure on the UI thread that nothing handled: recorded with its stack, never swallowed.</summary>
      private static void OnXamlUnhandledException(
         object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
         => Record(e?.Exception, "unhandled (XAML)");

      /// <summary>
      /// The process is going: state what the last WinRT failure on the way out was, so the log file ends with a
      /// name (HRESULT, type, message, stack) instead of an address from a debugger.
      /// </summary>
      private static void OnProcessExit(object? sender, EventArgs e)
      {
         // ALWAYS state that the process reached an orderly exit: with the two "Application closing" lines this
         // makes the log decisive — if the last lines are these, anything a debugger reported after them was
         // thrown by the framework's own shutdown, not by the application.
         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            m_LastWinRtFailure is null
               ? "Process exit: orderly (no WinRT failure was seen on the way out)."
               : "Process exit: orderly, and the last WinRT failure on the way out was " + m_LastWinRtFailure,
            Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
      }

      private static void Record(Exception? ex, String what)
      {
         if (ex is null) return;

         try
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Application failure (" + what + "): " + Describe(ex),
               Microsoft.Extensions.Logging.LogLevel.Error, "Edam.Studio");

            StartupDiagnostics.Report(ex);
         }
         catch (Exception)
         {
            // recording must never replace the failure it is recording
         }
      }

      private static String Describe(Exception ex)
         => "0x" + ex.HResult.ToString("X8") + "  " + ex.GetType().FullName + ": " + ex.Message +
            Environment.NewLine + (ex.StackTrace ?? "(no stack)");

      /// <summary>
      /// True for the failures a C++/WinRT component reports: a COM exception, a type whose name says hresult, or
      /// an HRESULT from the small set that means "that object is gone / disconnected / wrong thread". It is
      /// deliberately <b>narrow</b>: a log that records every caught exception answers nothing.
      /// </summary>
      private static Boolean IsWinRtShaped(Exception ex)
      {
         if (ex is System.Runtime.InteropServices.COMException) return true;

         var name = ex.GetType().FullName ?? String.Empty;
         if (name.Contains("hresult", StringComparison.OrdinalIgnoreCase)) return true;

         return ex.HResult == RO_E_CLOSED
             || ex.HResult == RPC_E_DISCONNECTED
             || ex.HResult == RPC_E_WRONG_THREAD
             || ex.HResult == WIN32_OPERATION_ID_NOT_VALID
             || ex.HResult == ELEMENT_NOT_FOUND;
      }
   }
}
