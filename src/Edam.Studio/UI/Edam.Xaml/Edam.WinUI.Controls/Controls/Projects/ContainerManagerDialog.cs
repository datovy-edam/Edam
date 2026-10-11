using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.DependencyInjection;
using Edam.WinUI.Controls.DataModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Edam.WinUI.Controls.Projects
{
   /// <summary>
   /// <b>The container manager</b> (ADR-0014 decision 8 / LM-9): <b>one</b> surface that reviews and manages the
   /// catalog's containers, so a person never has to guess where their data is or edit a configuration file to
   /// change it.
   /// <list type="bullet">
   /// <item>the <b>list</b> shows each container by its <b>description</b> (the display name), with the immutable
   /// name, how it is implemented and how much it holds beside it;</item>
   /// <item><b>Add</b> creates a container (name validated by the registry's own rule) and seeds its shared
   /// locations, so a project created there is runnable rather than inert;</item>
   /// <item><b>Save description</b> edits the only part of a container that is not identity — an edit that
   /// enlistment can never revert (the contract makes enlist idempotent, and the providers now agree);</item>
   /// <item><b>Remove</b> takes the container and <b>everything it holds</b> (items and their content), with the
   /// counts shown first;</item>
   /// <item>the <b>connection string</b> is stated, with its source, and may be overridden deliberately
   /// (Integrated Security only — a password is refused, because no secret is ever stored).</item>
   /// </list>
   /// <para>
   /// It is a <c>ContentDialog</c>, not a second window: a modal over the shell needs only the panel's
   /// <c>XamlRoot</c>, and WinUI allows <b>one</b> dialog at a time — so the removal confirmation is a state
   /// inside this dialog, never a dialog of its own (the lesson from items 88–93).
   /// </para>
   /// </summary>
   public static class ContainerManagerDialog
   {
      public static async Task ShowAsync(XamlRoot root, Func<Task> refresh)
      {
         var summaries = new List<ContainerSummary>();
         ContainerSummary? selected = null;

         // ---- the surfaces ---------------------------------------------------------------------------
         var list = new ListView
         {
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 240,
            MinWidth = 260,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Colors.LightGray),
         };

         var detail = new StackPanel { Spacing = 8, MinWidth = 320, MaxWidth = 420 };

         var storageText = new TextBlock
         {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.DimGray),
         };

         var connectionBox = new TextBox
         {
            Header = "Catalog connection string (Integrated Security only — a password is refused)",
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = false,
         };

         var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };

         var body = new Grid { ColumnSpacing = 12 };
         body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
         body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
         Grid.SetColumn(list, 0);
         Grid.SetColumn(detail, 1);
         body.Children.Add(list);
         body.Children.Add(detail);

         var header = new StackPanel { Spacing = 6 };
         header.Children.Add(storageText);
         header.Children.Add(connectionBox);

         var connectionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
         var saveConnection = new Button { Content = "Use my value" };
         var clearConnection = new Button { Content = "Use the deployment's value" };
         connectionRow.Children.Add(saveConnection);
         connectionRow.Children.Add(clearConnection);
         header.Children.Add(connectionRow);
         header.Children.Add(status);

         var content = new StackPanel { Spacing = 12 };
         content.Children.Add(header);
         content.Children.Add(body);

         var dialog = new ContentDialog
         {
            XamlRoot = root,
            Title = "Catalog containers",
            PrimaryButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
         };

         // ---- helpers (local functions: they own this dialog's state) --------------------------------
         void Report(string message, bool failed)
         {
            status.Text = message;
            status.Foreground = new SolidColorBrush(failed ? Colors.Brown : Colors.Navy);
         }

         static string KindOf(ContainerType type) => type switch
         {
            ContainerType.FileSystem => "file system",
            ContainerType.PostgreSql => "PostgreSQL",
            ContainerType.MsSql => "MS-SQL",
            ContainerType.Service => "web API",
            _ => "database",
         };

         static string KindKey(ContainerType type) => type switch
         {
            ContainerType.FileSystem => "filesystem",
            ContainerType.PostgreSql => "postgres",
            ContainerType.MsSql => "mssql",
            _ => "postgres",
         };

         TextBlock Line(string text, double size = 12, bool dim = false) => new()
         {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = size,
            Foreground = new SolidColorBrush(dim ? Colors.DimGray : Colors.Black),
         };

         static string Describe(ContainerSummary summary)
            => summary.Description is { Length: > 0 }
               ? summary.Description
               : "(no description yet)";

         static string Detail(ContainerSummary summary, bool isDefault)
            => summary.ContainerId
               + (isDefault ? "  ·  default" : string.Empty)
               + "  ·  " + KindOf(summary.Type)
               + "  ·  " + summary.Items + " item(s)";

         async Task ReloadAsync()
         {
            var previous = selected?.ContainerId;

            var loaded = await ProjectServicesHelper.ContainerSummariesAsync();
            summaries.Clear();
            summaries.AddRange(loaded);

            var configured = ProjectServicesHelper.ConfiguredContainerName;
            list.Items.Clear();

            foreach (var summary in summaries)
            {
               var isDefault = string.Equals(summary.ContainerId, configured,
                  StringComparison.OrdinalIgnoreCase);

               list.Items.Add(new ListViewItem
               {
                  Content = new StackPanel
                  {
                     Children =
                     {
                        Line(Describe(summary), 13),
                        Line(Detail(summary, isDefault), 11, dim: true),
                     },
                  },
                  Tag = summary,
               });
            }

            storageText.Text = await ProjectServicesHelper.DescribeStorageAsync(previous);
            connectionBox.Text = ProjectServicesHelper.CatalogConnectionText;

            var keep = summaries.FirstOrDefault(s => string.Equals(
               s.ContainerId, previous, StringComparison.OrdinalIgnoreCase))
               ?? summaries.FirstOrDefault(s => string.Equals(
                  s.ContainerId, configured, StringComparison.OrdinalIgnoreCase))
               ?? summaries.FirstOrDefault();

            if (keep is not null)
            {
               foreach (var item in list.Items.OfType<ListViewItem>())
               {
                  if (ReferenceEquals(item.Tag, keep)) list.SelectedItem = item;
               }
            }
            else
            {
               RenderEmpty();
            }
         }

         void RenderEmpty()
         {
            selected = null;
            detail.Children.Clear();
            detail.Children.Add(Line("This catalog has no containers yet.", 13));
            var add = new Button { Content = "Add a container" };
            add.Click += (_, _) => RenderAdd();
            detail.Children.Add(add);
         }

         void RenderDetail(ContainerSummary summary)
         {
            selected = summary;
            var isDefault = string.Equals(summary.ContainerId,
               ProjectServicesHelper.ConfiguredContainerName, StringComparison.OrdinalIgnoreCase);

            detail.Children.Clear();
            detail.Children.Add(Line(isDefault ? "Container (default for this installation)" : "Container", 13));

            // the NAME is identity: it is the address authority and the namespace content lives under
            detail.Children.Add(Line("Name — not editable: " + summary.ContainerId, 11, dim: true));
            detail.Children.Add(Line("Implementation: " + KindOf(summary.Type)
               + (string.IsNullOrWhiteSpace(summary.ContainerUri) ? string.Empty : " — " + summary.ContainerUri)
               + "  ·  " + summary.Items + " item(s)", 11, dim: true));

            var description = new TextBox
            {
               Header = "Description (the display name)",
               Text = summary.Description ?? string.Empty,
               TextWrapping = TextWrapping.Wrap,
               AcceptsReturn = true,
            };
            detail.Children.Add(description);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var save = new Button { Content = "Save description" };
            var remove = new Button { Content = "Remove container" };
            var add = new Button { Content = "Add container" };
            row.Children.Add(save);
            row.Children.Add(remove);
            row.Children.Add(add);
            detail.Children.Add(row);

            save.Click += async (_, _) =>
            {
               Report("Saving…", failed: false);
               var problem = await ProjectServicesHelper.SaveContainerDescriptionAsync(
                  summary.ContainerId, description.Text);
               Report(problem ?? "Description saved.", problem is not null);
               await ReloadAsync();
               await refresh();
            };

            remove.Click += (_, _) => RenderRemoveConfirm(summary);
            add.Click += (_, _) => RenderAdd();
         }

         async void RenderRemoveConfirm(ContainerSummary summary)
         {
            detail.Children.Clear();
            detail.Children.Add(Line("Remove '" + Describe(summary) + "'?", 13));

            var contents = await ProjectServicesHelper.DescribeContainerContentsAsync(summary.ContainerId);
            detail.Children.Add(Line("This removes " + contents + " — every catalog entry and its content — " +
               "and cannot be undone.", 11));

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var confirm = new Button { Content = "Remove" };
            var cancel = new Button { Content = "Cancel" };
            row.Children.Add(confirm);
            row.Children.Add(cancel);
            detail.Children.Add(row);

            cancel.Click += (_, _) => RenderDetail(summary);

            confirm.Click += async (_, _) =>
            {
               var removed = await ProjectServicesHelper.DeleteContainerAsync(summary.ContainerId);

               Report(removed
                  ? "Removed '" + summary.ContainerId + "' with everything it held."
                  : "Not removed — the diagnostics panel says why.", !removed);

               selected = null;
               await ReloadAsync();
               await refresh();
            };
         }

         void RenderAdd()
         {
            detail.Children.Clear();
            detail.Children.Add(Line("Add a container", 13));

            var name = new TextBox
            {
               Header = "Name (its address; letters, digits, '.', '-', '_')",
               Text = "Edam." + DateTime.Now.ToString("yyyyMMdd"),
            };
            var description = new TextBox
            {
               Header = "Description (the display name)",
               TextWrapping = TextWrapping.Wrap,
            };
            var kind = new ComboBox
            {
               Header = "How it is implemented",
               ItemsSource = new[] { "MS-SQL", "PostgreSQL", "File system" },
               SelectedIndex = 0,
            };

            detail.Children.Add(name);
            detail.Children.Add(description);
            detail.Children.Add(kind);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var create = new Button { Content = "Create" };
            var cancel = new Button { Content = "Cancel" };
            row.Children.Add(create);
            row.Children.Add(cancel);
            detail.Children.Add(row);

            cancel.Click += (_, _) =>
            {
               if (selected is not null) RenderDetail(selected); else RenderEmpty();
            };

            create.Click += async (_, _) =>
            {
               var key = (kind.SelectedItem as string) switch
               {
                  "PostgreSQL" => "postgres",
                  "File system" => "filesystem",
                  _ => "mssql",
               };

               Report("Creating…", failed: false);
               var problem = await ProjectServicesHelper.AddContainerAsync(
                  name.Text, description.Text, key);

               Report(problem ?? "Container '" + name.Text.Trim() + "' is ready (its shared locations " +
                  "were seeded, so a project created in it can run).", problem is not null);

               await ReloadAsync();
               await refresh();
            };
         }

         // ---- wiring --------------------------------------------------------------------------------
         list.SelectionChanged += (_, _) =>
         {
            if (list.SelectedItem is ListViewItem item && item.Tag is ContainerSummary summary)
            {
               RenderDetail(summary);
            }
         };

         saveConnection.Click += async (_, _) =>
         {
            var problem = await ProjectServicesHelper.SetCatalogConnectionAsync(connectionBox.Text);
            Report(problem ?? "Saved: the application now uses your connection string.", problem is not null);
            await ReloadAsync();
            await refresh();
         };

         clearConnection.Click += async (_, _) =>
         {
            var problem = await ProjectServicesHelper.SetCatalogConnectionAsync(null);
            Report(problem ?? "Cleared: the deployment's connection string applies again.", problem is not null);
            await ReloadAsync();
            await refresh();
         };

         await ReloadAsync();
         await dialog.ShowAsync();
      }
   }
}
