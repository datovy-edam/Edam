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
            EdamAppData.SetApplicationDataLocation(
               Windows.Storage.ApplicationData.Current.LocalFolder.Path);
            StartupDiagnostics.Trace("OnLaunched: package-local app-data root configured");
            ApplicationHelper.InitializeApplication();
            StartupDiagnostics.Trace("OnLaunched: InitializeApplication completed");

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
