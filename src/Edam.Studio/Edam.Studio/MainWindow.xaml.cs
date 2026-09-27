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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using UIApp = Edam.Application.Settings;

namespace Edam.Studio
{
   /// <summary>
   /// An empty window that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainWindow : Window
   {
      /// <summary>Guards the one-time configuration ask (CF-4 / ADR-0013).</summary>
      private bool m_ConfigurationAsked;

      public MainWindow()
      {
         StartupDiagnostics.Trace("MainWindow ctor: begin");
         this.InitializeComponent();
         StartupDiagnostics.Trace("MainWindow ctor: InitializeComponent completed");
         Title = "EDAM Studio";
         UIApp.AppSettings.VerifySetConnectionString();

         // CF-4 (ADR-0013): ask about the values the configuration cannot invent — AFTER the shell is up
         // (a ContentDialog needs a XamlRoot and the UI thread, so it can never run during OnLaunched),
         // once, and always skippable. An ask that fails must never break startup.
         Activated += async (sender, args) =>
         {
            if (m_ConfigurationAsked || Content?.XamlRoot is null)
            {
               return;
            }
            m_ConfigurationAsked = true;

            try
            {
               await Edam.WinUI.Controls.Configuration.ConfigurationPrompt.ShowIfNeededAsync(
                  Content.XamlRoot,
                  Edam.WinUI.Controls.DataModels.ProjectServicesHelper.UserConfiguration);
            }
            catch (System.Exception ex)
            {
               StartupDiagnostics.Trace("Configuration prompt failed: " + ex.Message);
            }
         };

         StartupDiagnostics.Trace("MainWindow ctor: completed");
      }

      private void myButton_Click(object sender, RoutedEventArgs e)
      {
      }
   }
}
