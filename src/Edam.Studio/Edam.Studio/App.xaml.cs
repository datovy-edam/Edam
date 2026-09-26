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
      /// <summary>
      /// Initializes the singleton application object.  This is the first line of authored code
      /// executed, and as such is the logical equivalent of main() or WinMain().
      /// </summary>
      public App()
      {
         StartupDiagnostics.Trace("App ctor: begin");
         this.InitializeComponent();
         StartupDiagnostics.Trace("App ctor: InitializeComponent completed");
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
   }
}
