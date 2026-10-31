#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Edam.Settings.Contracts;
using Edam.Settings.Core;
using Edam.WinUI.Controls.DataModels;
using Edam.WinUI.Controls.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Edam.WinUI.Controls.Settings
{
   /// <summary>
   /// <b>The settings surface</b> (ADR-0016) — the user-initiated, revisitable half of the pair whose other half is the
   /// first-run prompt. It is <b>generated from the catalog</b>: this class knows nothing about which settings exist,
   /// so adding one is a registry entry and never a XAML edit.
   /// <para>
   /// Its three sections are deliberately kept apart, because mixing them is what makes a settings screen untrustworthy:
   /// <b>your values</b> (editable), <b>set for this installation</b> (a deployment fact, read-only), and <b>recorded
   /// state</b> (evidence that an action happened — not a preference). Every row states where its value came from and,
   /// when it is not immediate, that the change takes effect later — a screen that implies "saved" means "live" is the
   /// silent fallback this area exists to prevent.
   /// </para>
   /// </summary>
   public sealed class SettingsControl : UserControl
   {
      private const String Category = "Edam.WinUI.Controls.Settings";

      private readonly StackPanel m_Body = new StackPanel { Spacing = 18 };
      private readonly TextBlock m_Status = new TextBlock
      {
         FontSize = 12,
         TextWrapping = TextWrapping.Wrap,
         VerticalAlignment = VerticalAlignment.Center,
         Margin = new Thickness(12, 0, 0, 0)
      };

      public SettingsControl()
      {
         Content = BuildShell();
         Loaded += (sender, args) => Reload();
      }

      // ---- shell ---------------------------------------------------------------------------------

      private UIElement BuildShell()
      {
         var header = new StackPanel
         {
            Spacing = 4,
            Margin = new Thickness(24, 18, 24, 6)
         };

         header.Children.Add(new TextBlock
         {
            Text = "Settings",
            FontSize = 26,
            FontWeight = FontWeights.SemiBold
         });

         header.Children.Add(new TextBlock
         {
            Text = "The values this installation cannot invent. Each one says what it is, where its value came " +
               "from, and when a change takes effect.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Opacity = 0.75
         });

         var scroller = new ScrollViewer
         {
            Content = m_Body,
            Padding = new Thickness(24, 6, 24, 12),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
         };

         var footer = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Padding = new Thickness(24, 10, 24, 14)
         };

         var refresh = new Button { Content = "Refresh" };
         refresh.Click += (sender, args) => Reload();
         footer.Children.Add(refresh);

         var resetAll = new Button { Content = "Forget my answers" };
         resetAll.Click += async (sender, args) => await ForgetAllAsync();
         footer.Children.Add(resetAll);

         footer.Children.Add(m_Status);

         var root = new Grid();
         root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
         root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
         root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

         Grid.SetRow(header, 0);
         Grid.SetRow(scroller, 1);
         Grid.SetRow(footer, 2);
         root.Children.Add(header);
         root.Children.Add(scroller);
         root.Children.Add(footer);

         return root;
      }

      // ---- the generated content -----------------------------------------------------------------

      private void Reload()
      {
         m_Body.Children.Clear();

         try
         {
            var catalog = SettingsGateway.Catalog;

            // anything still unresolved is named here rather than left to a surprise prompt later
            var pending = ProjectServicesHelper.UserConfiguration.ToAsk();
            if (pending.Count > 0)
            {
               m_Body.Children.Add(Note(
                  "Still needed: " + String.Join(", ", pending.Select(a => a.Item.Title)) +
                  ". The application asks for these when it needs them; you may state them here instead.",
                  Colors.DarkGoldenrod));
            }

            foreach (var group in catalog.Groups)
            {
               var section = new StackPanel { Spacing = 8 };

               section.Children.Add(new TextBlock
               {
                  Text = group,
                  FontSize = 17,
                  FontWeight = FontWeights.SemiBold,
                  Margin = new Thickness(0, 10, 0, 0)
               });

               if (String.Equals(group, SettingsGroups.Recorded, StringComparison.OrdinalIgnoreCase))
               {
                  section.Children.Add(new TextBlock
                  {
                     Text = "Evidence that an action happened. It is kept on purpose and is not a preference — " +
                        "this is why a deleted starter project is never resurrected.",
                     FontSize = 11,
                     Opacity = 0.7,
                     TextWrapping = TextWrapping.Wrap
                  });
               }

               foreach (var item in catalog.InGroup(group))
               {
                  section.Children.Add(BuildRow(item));
               }

               m_Body.Children.Add(section);
            }
         }
         catch (Exception ex)
         {
            // a settings screen that throws is a settings screen that cannot be used to fix anything
            m_Body.Children.Add(Note("The settings could not be read: " + ex.Message, Colors.Firebrick));
            AppDiagnostics.Write(
               "Settings: the catalog could not be read — " + ex.Message, LogLevel.Error, Category);
         }
      }

      private UIElement BuildRow(SettingsItemInfo item)
      {
         var panel = new StackPanel { Spacing = 6 };

         var head = new Grid();
         head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
         head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

         var title = new TextBlock
         {
            Text = item.Title,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
         };

         var state = new TextBlock
         {
            Text = $"{SettingsText.State(item)}  ·  {SettingsText.Source(item.Source)}",
            FontSize = 11,
            Opacity = 0.7,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
         };

         Grid.SetColumn(title, 0);
         Grid.SetColumn(state, 1);
         head.Children.Add(title);
         head.Children.Add(state);

         panel.Children.Add(head);

         panel.Children.Add(new TextBlock
         {
            Text = item.WhatItIs,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85
         });

         if (item.IsEditable)
         {
            panel.Children.Add(BuildEditor(item));
         }
         else
         {
            panel.Children.Add(new TextBlock
            {
               Text = item.Display ?? "(not set)",
               FontFamily = new FontFamily("Consolas"),
               FontSize = 12,
               IsTextSelectionEnabled = true,
               TextWrapping = TextWrapping.Wrap
            });

            if (!String.IsNullOrWhiteSpace(item.Reason))
            {
               panel.Children.Add(new TextBlock
               {
                  Text = item.Reason,
                  FontSize = 11,
                  Opacity = 0.7,
                  TextWrapping = TextWrapping.Wrap
               });
            }
         }

         if (!String.IsNullOrWhiteSpace(item.Problem))
         {
            panel.Children.Add(new TextBlock
            {
               Text = item.Problem,
               FontSize = 12,
               Foreground = new SolidColorBrush(Colors.Firebrick),
               TextWrapping = TextWrapping.Wrap
            });
         }

         if (item.IsEditable && item.ApplyScope != SettingsApplyScope.Immediate)
         {
            panel.Children.Add(new TextBlock
            {
               Text = SettingsText.Apply(item.ApplyScope),
               FontSize = 11,
               Opacity = 0.7
            });
         }

         return new Border
         {
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 12, 14, 12),
            Child = panel
         };
      }

      private UIElement BuildEditor(SettingsItemInfo item)
      {
         var holder = new StackPanel { Spacing = 6 };

         var feedback = new TextBlock
         {
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.Firebrick),
            Visibility = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap
         };

         Control entry;
         Func<String?> read;

         switch (item.Kind)
         {
            case SettingsKind.Switch:
               var toggle = new ToggleSwitch
               {
                  OnContent = "On",
                  OffContent = "Off",
                  IsOn = String.Equals(item.Display, "true", StringComparison.OrdinalIgnoreCase)
               };
               entry = toggle;
               read = () => toggle.IsOn ? "true" : "false";
               break;

            case SettingsKind.Choice:
               var choice = new ComboBox
               {
                  ItemsSource = item.Choices,
                  SelectedItem = item.Display,
                  HorizontalAlignment = HorizontalAlignment.Left,
                  MinWidth = 260
               };
               entry = choice;
               read = () => choice.SelectedItem as String;
               break;

            default:
               var box = new TextBox
               {
                  Text = item.Display ?? String.Empty,
                  PlaceholderText = item.Default ?? "not set",
                  TextWrapping = TextWrapping.Wrap
               };
               entry = box;
               read = () => box.Text;
               break;
         }

         var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

         var save = new Button { Content = "Save", MinWidth = 80 };
         save.Click += async (sender, args) => await SaveAsync(item, read(), feedback);
         buttons.Children.Add(save);

         // reset only where there is an ANSWER to forget: the deployment's value is not the person's to clear
         if (item.Source == SettingsSource.User)
         {
            var reset = new Button { Content = "Reset to default" };
            reset.Click += async (sender, args) => await ResetAsync(item, feedback);
            buttons.Children.Add(reset);
         }

         holder.Children.Add(entry);
         holder.Children.Add(buttons);
         holder.Children.Add(feedback);

         return holder;
      }

      // ---- actions -------------------------------------------------------------------------------

      private async Task SaveAsync(SettingsItemInfo item, String? value, TextBlock feedback)
      {
         try
         {
            var result = await SettingsGateway.Service.SetAsync(item.Id, value);

            if (!result.Changed)
            {
               Show(feedback, result.Problem ?? "the value was refused");
               AppDiagnostics.Write(
                  $"Settings: REFUSED {item.Id} = '{value}' — {result.Problem}",
                  LogLevel.Warning, Category);
               return;
            }

            AppDiagnostics.Write(
               $"Settings: {item.Id} = '{value}' — saved; {SettingsText.Apply(result.ApplyScope)}",
               LogLevel.Information, Category);

            m_Status.Text = result.Problem is null
               ? $"Saved: {item.Title}."
               : $"{item.Title}: {result.Problem}";

            Reload();
         }
         catch (Exception ex)
         {
            Show(feedback, "the change could not be made: " + ex.Message);
            AppDiagnostics.Write(
               "Settings: " + item.Id + " could not be changed — " + ex.Message,
               LogLevel.Error, Category);
         }
      }

      private async Task ResetAsync(SettingsItemInfo item, TextBlock feedback)
      {
         try
         {
            var result = await SettingsGateway.Service.ResetAsync(item.Id);

            if (!result.Changed)
            {
               Show(feedback, result.Problem ?? "the value could not be reset");
               return;
            }

            AppDiagnostics.Write(
               $"Settings: {item.Id} was reset — the inherited value applies again.",
               LogLevel.Information, Category);

            m_Status.Text = $"Reset: {item.Title}.";
            Reload();
         }
         catch (Exception ex)
         {
            Show(feedback, "the value could not be reset: " + ex.Message);
         }
      }

      private async Task ForgetAllAsync()
      {
         if (XamlRoot is null)
         {
            return;
         }

         var dialog = new ContentDialog
         {
            Title = "Forget your answers?",
            Content = new TextBlock
            {
               Text = "Every value you stated is removed and the installation's configuration and documented " +
                  "defaults apply again. Recorded state — such as the starter project having been created — " +
                  "is kept, so nothing is re-offered or resurrected.",
               TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "Forget my answers",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
         };

         if (await dialog.ShowAsync() != ContentDialogResult.Primary)
         {
            return;
         }

         var forgotten = await SettingsGateway.Service.ResetAllAsync();

         AppDiagnostics.Write(
            $"Settings: {forgotten} answer(s) were forgotten (installation state kept).",
            LogLevel.Information, Category);

         m_Status.Text = $"Forgot {forgotten} answer(s); recorded state was kept.";
         Reload();
      }

      // ---- helpers -------------------------------------------------------------------------------

      private static void Show(TextBlock feedback, String message)
      {
         feedback.Text = message;
         feedback.Visibility = Visibility.Visible;
      }

      private static UIElement Note(String text, Color color) => new Border
      {
         BorderThickness = new Thickness(3, 0, 0, 0),
         BorderBrush = new SolidColorBrush(color),
         Padding = new Thickness(10, 6, 6, 6),
         Child = new TextBlock
         {
            Text = text,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
         }
      };
   }
}
