using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edam.Data.Projects.DependencyInjection;
using Edam.WinUI.Controls.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Edam.WinUI.Controls.Configuration;

/// <summary>
/// <b>CF-4 / ADR-0013</b> — the <b>one</b> ask surface, generated <b>from the registry</b> so no item is
/// hardcoded in the UI. It explains each item (<i>what this is</i> / <i>what it affects</i>), validates
/// where the user answers (an unusable answer keeps the dialog open with the reason), stores the answers
/// (overlay or packaged, per the item's policy), records every ask/answer/deferral in the
/// <b>diagnostics panel</b>, and is always <b>skippable</b>: "Later" changes nothing and the application
/// continues with its documented fallback.
/// <para>
/// It must be shown <b>after the shell is up</b> — a <c>ContentDialog</c> needs a <c>XamlRoot</c> and the
/// UI thread, so it can never run during <c>OnLaunched</c>.
/// </para>
/// </summary>
public static class ConfigurationPrompt
{
   private const string Category = "Edam.WinUI.Controls.Configuration";

   /// <summary>The item's value boxes, so Save can validate and store exactly what was typed.</summary>
   private sealed record Entry(ConfigurableAsk Ask, TextBox Box);

   /// <summary>
   /// Ask about everything that still needs the user's input. Returns true when at least one answer was
   /// saved.
   /// </summary>
   /// <param name="xamlRoot">The shell's XAML root (the dialog cannot be shown without one).</param>
   /// <param name="user">The answers service (registry + overlay + packaged marker).</param>
   /// <param name="completeAction">
   /// Completes the <b>action</b> behind an answered item (DP-3 / ADR-0012) — for the starter project,
   /// creating it. Returns null on success, or why it failed; on failure the dialog stays open with the
   /// reason, and because the marker is only written when the action really happened, the question
   /// remains pending (ADR-0013 decision 12).
   /// </param>
   public static async Task<bool> ShowIfNeededAsync(
      XamlRoot xamlRoot, UserConfiguration user,
      Func<ConfigurableItemInfo, string, Task<string?>>? completeAction = null)
   {
      if (xamlRoot is null || user is null)
      {
         return false;
      }

      var asks = user.ToAsk();
      if (asks.Count == 0)
      {
         return false;
      }

      AppDiagnostics.Write(
         $"Configuration needs input: {string.Join(", ", asks.Select(a => a.Item.Id))}",
         LogLevel.Information, Category);

      var entries = new List<Entry>();
      var body = new StackPanel { Spacing = 12 };
      var problemText = new TextBlock
      {
         TextWrapping = TextWrapping.Wrap,
         Visibility = Visibility.Collapsed,
         FontSize = 12
      };

      foreach (var ask in asks)
      {
         var block = new StackPanel { Spacing = 4 };

         block.Children.Add(new TextBlock
         {
            Text = ask.Item.WhatItIs,
            TextWrapping = TextWrapping.Wrap
         });

         block.Children.Add(new TextBlock
         {
            Text = ask.Item.WhatItAffects,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Opacity = 0.7
         });

         if (!string.IsNullOrWhiteSpace(ask.Problem))
         {
            block.Children.Add(new TextBlock
            {
               Text = ask.Problem,
               TextWrapping = TextWrapping.Wrap,
               FontSize = 12
            });
         }

         var box = new TextBox
         {
            Header = ask.Item.Title,
            Text = ask.Current ?? ask.Item.Default ?? string.Empty
         };

         block.Children.Add(box);
         entries.Add(new Entry(ask, box));
         body.Children.Add(block);
      }

      body.Children.Add(problemText);

      var dialog = new ContentDialog
      {
         XamlRoot = xamlRoot,
         Title = "Before we start",
         PrimaryButtonText = "Save",
         CloseButtonText = "Later",
         DefaultButton = ContentDialogButton.Primary,
         Content = body
      };

      var saved = false;

      dialog.PrimaryButtonClick += async (sender, args) =>
      {
         // validating may await the ACTION, so hold the dialog open across the await
         var deferral = args.GetDeferral();

         try
         {
            // validate WHERE the user answered (ADR-0013 decision 7): an unusable answer keeps the dialog
            // open with the reason, and nothing is written
            var problems = new List<string>();

            foreach (var entry in entries)
            {
               var problem = user.Answer(entry.Ask.Item, entry.Box.Text);
               if (problem is not null)
               {
                  problems.Add($"{entry.Ask.Item.Title}: {problem}");
               }
            }

            if (problems.Count > 0)
            {
               problemText.Text = string.Join("\n", problems);
               problemText.Visibility = Visibility.Visible;
               args.Cancel = true;
               return;
            }

            // the answer is stored — now attempt the ACTION it implies (DP-3). An action-backed item is
            // not complete until the action happened: the marker is written by the action, and a failure
            // here keeps the question pending and tells the person why.
            if (completeAction is not null)
            {
               foreach (var entry in entries)
               {
                  if (!entry.Ask.Item.IsActionBacked)
                  {
                     continue;
                  }

                  var actionProblem = await completeAction(entry.Ask.Item, entry.Box.Text);
                  if (actionProblem is not null)
                  {
                     problems.Add($"{entry.Ask.Item.Title}: {actionProblem}");
                  }
               }
            }

            if (problems.Count > 0)
            {
               problemText.Text = string.Join("\n", problems);
               problemText.Visibility = Visibility.Visible;
               args.Cancel = true;
               return;
            }

            saved = true;

            foreach (var entry in entries)
            {
               AppDiagnostics.Write(
                  $"Configuration answered: {entry.Ask.Item.Id} = '{entry.Box.Text}'",
                  LogLevel.Information, Category);
            }
         }
         finally
         {
            deferral.Complete();
         }
      };

      var result = await dialog.ShowAsync();

      if (result != ContentDialogResult.Primary)
      {
         // always skippable: nothing changes, and the deferral is on the record (decision 9)
         foreach (var entry in entries)
         {
            AppDiagnostics.Write(
               $"Configuration deferred: {entry.Ask.Item.Id}",
               LogLevel.Information, Category);
         }
         return false;
      }

      return saved;
   }
}
