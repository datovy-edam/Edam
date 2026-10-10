// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about the WinUI project, see http://aka.ms/winui-project-info.
using UIApp = Edam.Application.Settings;

namespace Edam.Studio
{
   /// <summary>
   /// An empty window that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainWindow : Window
   {
      /// <summary>Traces the "no XamlRoot yet" case once, instead of on every signal.</summary>
      private bool m_ConfigurationRootMissingTraced;

      /// <summary>Traces the "waiting for sign-in" case once, instead of on every signal.</summary>
      private bool m_ConfigurationWaitingTraced;

      /// <summary>Signal-independent safety net: ticks until the ask has been made (or gives up).</summary>
      private Microsoft.UI.Dispatching.DispatcherQueueTimer? m_ConfigurationTimer;
      private int m_ConfigurationTicks;

      /// <summary>
      /// True once the window has been <b>closed</b>. Every ask trigger checks it, and it is set before the
      /// triggers are detached — a signal must never reach a window whose object is already gone.
      /// </summary>
      private bool m_Closed;

      /// <summary>The shell (the window's content), kept so its Loaded handler can be detached on close.</summary>
      private FrameworkElement? m_Shell;

      public MainWindow()
      {
         StartupDiagnostics.Trace("MainWindow ctor: begin");
         this.InitializeComponent();
         StartupDiagnostics.Trace("MainWindow ctor: InitializeComponent completed");
         Title = "EDAM Studio";
         UIApp.AppSettings.VerifySetConnectionString();

         // CF-4 (ADR-0013): ask about the values the configuration cannot invent — AFTER the shell is up.
         // A ContentDialog needs a XamlRoot and the UI thread, so it can never run during OnLaunched: the
         // root content's Loaded event is the earliest moment the dialog can actually be shown, and
         // Activated is kept as a fallback (whichever happens first, once).
         if (Content is FrameworkElement shell)
         {
            m_Shell = shell;
            shell.Loaded += OnShellLoaded;
         }

         Activated += OnWindowActivated;

         // Two signal-based attempts missed the moment (a XamlRoot that did not exist yet; then a window
         // that never regained focus after sign-in), so the ask ALSO runs on a small, self-stopping poll:
         // the readiness gate decides when it is allowed, so no signal can be missed. It stops as soon as
         // the ask has been made, and gives up after ~5 minutes.
         m_ConfigurationTimer = DispatcherQueue.CreateTimer();
         m_ConfigurationTimer.Interval = System.TimeSpan.FromSeconds(2);
         m_ConfigurationTimer.Tick += OnConfigurationTick;
         m_ConfigurationTimer.Start();

         // ...and it stops for good when the window CLOSES. Without this the Activated handler outlived the
         // window and reading Content threw
         //   "The operation identifier is not valid. The WinUI Desktop Window object has already been closed."
         // on the way out — a crash while closing an application, from a signal that no longer had a window.
         Closed += OnWindowClosed;

         StartupDiagnostics.Trace("MainWindow ctor: completed");
      }

      private void OnShellLoaded(object sender, RoutedEventArgs e) => AskForConfiguration();

      private void OnWindowActivated(object sender, WindowActivatedEventArgs args) => AskForConfiguration();

      private void OnConfigurationTick(
         Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
      {
         if (m_Closed ||
             Edam.WinUI.Controls.DataModels.ProjectServicesHelper.ConfigurationAskedThisRun ||
             ++m_ConfigurationTicks > 150)
         {
            m_ConfigurationTimer?.Stop();
            return;
         }

         AskForConfiguration();
      }

      /// <summary>
      /// The window is gone: <b>nothing may run afterwards</b>. The poll is stopped, the shell's Loaded
      /// handler is detached and the window's own handlers are released, so no later signal can reach a
      /// closed window object.
      /// </summary>
      private void OnWindowClosed(object sender, WindowEventArgs args)
      {
         m_Closed = true;

         try
         {
            m_ConfigurationTimer?.Stop();
            m_ConfigurationTimer = null;

            if (m_Shell is not null)
            {
               m_Shell.Loaded -= OnShellLoaded;
               m_Shell = null;
            }

            Activated -= OnWindowActivated;
            Closed -= OnWindowClosed;
         }
         catch (Exception)
         {
            // closing must never fail, whatever the window object reports
         }
      }

      /// <summary>
      /// Ask once, as soon as the shell can actually host a dialog. Every outcome is reported — to the
      /// diagnostics panel and the startup trace — so "nothing happened" is never silent.
      /// </summary>
      private void AskForConfiguration()
      {
         // the window is closed: there is no content to read and no dialog to show (the Activated event can
         // still arrive during teardown — reading Content then throws a COM error)
         if (m_Closed)
         {
            return;
         }

         try
         {
            // retry until the ask has REALLY been shown: a failed attempt (for example another ContentDialog
            // being open at that moment) stays retryable, which is why the shared helper owns the guard
            if (Edam.WinUI.Controls.DataModels.ProjectServicesHelper.ConfigurationAskedThisRun)
            {
               return;
            }

            // A (ADR-0013): never ask a person who has not signed in — the answer is a per-user preference
            // and creating content may need an authenticated session. The one-shot is deliberately NOT
            // consumed while waiting, so the ask still happens once sign-in completes.
            if (!Edam.WinUI.Controls.Configuration.ConfigurationReadiness.IsSignedIn)
            {
               if (!m_ConfigurationWaitingTraced)
               {
                  m_ConfigurationWaitingTraced = true;
                  Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                     "Configuration check: waiting for sign-in before asking (" +
                     Edam.WinUI.Controls.Configuration.ConfigurationReadiness.Describe() + ").",
                     Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
               }
               return;
            }

            var root = Content?.XamlRoot;
            if (root is null)
            {
               if (!m_ConfigurationRootMissingTraced)
               {
                  m_ConfigurationRootMissingTraced = true;
                  StartupDiagnostics.Trace(
                     "Configuration ask: no XamlRoot yet (waiting for the shell to load)");
               }
               return;
            }

            m_ConfigurationTimer?.Stop();
            _ = AskForConfigurationAsync(root);
         }
         catch (Exception ex)
         {
            // a signal that arrives while the window is being torn down must not crash the application; the
            // reason is on the record instead (a closed-window COM error is expected here, and harmless)
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Configuration check: skipped (" + ex.Message + ").",
               Microsoft.Extensions.Logging.LogLevel.Debug, "Edam.Studio");
         }
      }

      /// <summary>Ask, store and report — and never throw into startup.</summary>
      private async System.Threading.Tasks.Task AskForConfigurationAsync(
         Microsoft.UI.Xaml.XamlRoot root)
      {
         const string category = "Edam.Studio";

         try
         {
            var user = Edam.WinUI.Controls.DataModels.ProjectServicesHelper.UserConfiguration;
            var asks = user.ToAsk();

            if (asks.Count == 0)
            {
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Configuration check: nothing to ask.", 
                  Microsoft.Extensions.Logging.LogLevel.Information, category);
               return;
            }

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Configuration check: " + asks.Count + " item(s) need input: " + asks[0].Item.Id +
               (asks.Count > 1 ? " (+" + (asks.Count - 1) + " more)" : string.Empty),
               Microsoft.Extensions.Logging.LogLevel.Information, category);

            var saved = await Edam.WinUI.Controls.Configuration.ConfigurationPrompt
               .ShowIfNeededAsync(root, user);

            StartupDiagnostics.Trace("Configuration ask: shown, saved=" + saved);
         }
         catch (System.Exception ex)
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Configuration ask failed: " + ex.Message,
               Microsoft.Extensions.Logging.LogLevel.Warning, category);
            StartupDiagnostics.Trace("Configuration ask failed: " + ex.Message);
         }
      }
   }
}
