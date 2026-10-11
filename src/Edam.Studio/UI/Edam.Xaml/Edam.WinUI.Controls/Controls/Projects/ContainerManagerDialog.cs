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
   /// <b>The container manager</b> (ADR-0014 decision 11 / LM-9): <b>one</b> surface to review and manage the
   /// catalog's containers, and to see where their data is.
   /// <para>
   /// <b>Two tabs, and the split is deliberate.</b> <b>Containers</b> is where a container is <i>selected and
   /// managed</i> — the list, its description (the display name, the only editable part), adding and removing.
   /// <b>Storage</b> states <i>where the data is</i>: every container in this catalog is held by <b>one</b>
   /// storage, so that tab describes the catalog's storage and says so plainly; the only per-container facts are
   /// the container's <b>namespace</b> inside it and how much it holds. A connection string is therefore the
   /// catalog's, never a container's — and the field only appears when the catalog actually <i>is</i> reached by
   /// one (a database); a file-system catalog states a root path and a service catalog a base URI, and neither
   /// has a connection string to edit.
   /// </para>
   /// <para>
   /// It is a <c>ContentDialog</c> — one modal over the shell, using the panel's <c>XamlRoot</c> — and it lays its
   /// content out <b>vertically</b>: WinUI caps a dialog at roughly 548 DIPs by default, which is what silently
   /// clipped a side-by-side layout. The cap is raised here and the content scrolls inside the dialog, so nothing
   /// can be cut off on a narrow window.
   /// </para>
   /// </summary>
   public static class ContainerManagerDialog
   {
      public static async Task ShowAsync(XamlRoot root, Func<Task> refresh)
      {
         var summaries = new List<ContainerSummary>();
         ContainerSummary? selected = null;
         string? selectAfterReload = null;

         // ---- the surfaces ---------------------------------------------------------------------------
         var list = new ListView
         {
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 200,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Colors.LightGray),
         };

         var detail = new StackPanel { Spacing = 8 };

         var storageShared = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
         var storageContainer = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
         var storageNote = new TextBlock
         {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.DimGray),
         };

         var connectionBox = new TextBox
         {
            Header = "Catalog connection string (Integrated Security only — a password is refused)",
            TextWrapping = TextWrapping.Wrap,
         };

         var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };

         // ---- the tabs ------------------------------------------------------------------------------
         var containersTab = new TabViewItem
         {
            Header = "Containers",
            IsClosable = false,
            Content = new ScrollViewer
            {
               VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
               MaxHeight = 420,
               Content = new StackPanel { Spacing = 10, Children = { list, detail } },
            },
         };

         var connectionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
         var saveConnection = new Button { Content = "Use my value" };
         var clearConnection = new Button { Content = "Use the deployment's value" };
         connectionRow.Children.Add(saveConnection);
         connectionRow.Children.Add(clearConnection);

         var storageContent = new StackPanel
         {
            Spacing = 10,
            Children = { storageShared, storageContainer, storageNote, connectionBox, connectionRow },
         };

         var storageTab = new TabViewItem
         {
            Header = "Storage",
            IsClosable = false,
            Content = new ScrollViewer
            {
               VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
               MaxHeight = 420,
               Content = storageContent,
            },
         };

         var tabs = new TabView
         {
            IsAddTabButtonVisible = false,
            CanDragTabs = false,
            CanReorderTabs = false,
         };
         tabs.TabItems.Add(containersTab);
         tabs.TabItems.Add(storageTab);

         var content = new StackPanel { Spacing = 10, Children = { tabs, status } };

         var dialog = new ContentDialog
         {
            XamlRoot = root,
            Title = "Catalog containers",
            PrimaryButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
         };

         // a dialog is capped by a theme resource (~548 DIPs); this one needs more room, and its content
         // scrolls, so raising the cap can never clip anything
         dialog.Resources["ContentDialogMaxWidth"] = 900d;
         dialog.Resources["ContentDialogMinWidth"] = 640d;

         // ---- helpers (local functions own this dialog's state) --------------------------------------
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

         TextBlock Line(string text, double size = 12, bool dim = false) => new()
         {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = size,
            Foreground = new SolidColorBrush(dim ? Colors.DimGray : Colors.Black),
         };

         static string DisplayName(ContainerSummary summary)
            => summary.Description is { Length: > 0 } ? summary.Description : "(no description yet)";

         bool IsDefault(ContainerSummary summary) => string.Equals(
            summary.ContainerId, ProjectServicesHelper.ConfiguredContainerName,
            StringComparison.OrdinalIgnoreCase);

         void ShowStorage()
         {
            // ONE catalog, ONE storage: this is the catalog's storage, shared by every container — and the
            // container's own part of it is its namespace
            storageShared.Text = "This catalog stores everything in ONE place, shared by every container: " +
               ProjectServicesHelper.StorageKind + " — " + ProjectServicesHelper.CatalogLocationText +
               "  (stated by " + ProjectServicesHelper.CatalogConnectionSource + "; " +
               (ProjectServicesHelper.IsLocalTarget ? "local" : "shared/remote") + ").";

            storageContainer.Text = selected is null
               ? "Select a container on the Containers tab to see its namespace in that storage."
               : "This container: namespace '" + selected.ContainerId + "'  ·  " +
                 KindOf(selected.Type) + "  ·  " + selected.Items + " item(s).";

            // a connection string belongs to a catalog that IS reached by one — not to a file-system root
            // or a service URI, and never to a container
            var connectionBacked = ProjectServicesHelper.CatalogIsConnectionStringBacked;
            connectionBox.Visibility = connectionBacked ? Visibility.Visible : Visibility.Collapsed;
            connectionRow.Visibility = connectionBacked ? Visibility.Visible : Visibility.Collapsed;

            connectionBox.Text = ProjectServicesHelper.CatalogConnectionText;

            storageNote.Text = connectionBacked
               ? "The connection string is the catalog's, not a container's. Keeping your own value overrides " +
                 "the deployment's, and 'Use the deployment's value' gives it back."
               : "This catalog is not managed by a connection string: its storage is " +
                 ProjectServicesHelper.StorageKind + ", stated by the deployment. Nothing to edit here.";
         }

         void RenderEmpty()
         {
            selected = null;
            detail.Children.Clear();
            detail.Children.Add(Line("This catalog has no containers yet.", 13));

            var add = new Button { Content = "Add a container" };
            add.Click += (_, _) => RenderAdd();
            detail.Children.Add(add);

            ShowStorage();
         }

         void RenderDetail(ContainerSummary summary)
         {
            selected = summary;
            var isDefault = IsDefault(summary);

            detail.Children.Clear();
            detail.Children.Add(Line(isDefault ? "Container (default for this installation)" : "Container", 13));

            // the NAME is identity — the address authority and the namespace content lives under
            detail.Children.Add(Line("Name — not editable: " + summary.ContainerId, 11, dim: true));
            detail.Children.Add(Line("Holds " + summary.Items + " item(s)  ·  " +
               KindOf(summary.Type), 11, dim: true));

            var description = new TextBox
            {
               Header = "Description (the display name; the only editable part)",
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

            ShowStorage();
         }

         async void RenderRemoveConfirm(ContainerSummary summary)
         {
            detail.Children.Clear();
            detail.Children.Add(Line("Remove '" + DisplayName(summary) + "'?", 13));

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
               Header = "Name (its address — letters, digits, '.', '-', '_')",
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

               Report(problem ?? "Container '" + name.Text.Trim() + "' is ready (its shared locations were " +
                  "seeded, so a project created in it can run).", problem is not null);

               // show what was just made: reload SELECTS it, instead of falling back to the previous container
               selectAfterReload = name.Text.Trim();
               await ReloadAsync();
               await refresh();
            };
         }

         async Task ReloadAsync()
         {
            var previous = selectAfterReload ?? selected?.ContainerId;
            selectAfterReload = null;

            var loaded = await ProjectServicesHelper.ContainerSummariesAsync();
            summaries.Clear();
            summaries.AddRange(loaded);

            list.Items.Clear();

            foreach (var summary in summaries)
            {
               list.Items.Add(new ListViewItem
               {
                  Content = new StackPanel
                  {
                     Children =
                     {
                        Line(DisplayName(summary), 13),
                        Line(summary.ContainerId + (IsDefault(summary) ? "  ·  default" : string.Empty) +
                           "  ·  " + KindOf(summary.Type) + "  ·  " + summary.Items + " item(s)", 11, dim: true),
                     },
                  },
                  Tag = summary,
               });
            }

            var configured = ProjectServicesHelper.ConfiguredContainerName;
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

               RenderDetail(keep);
            }
            else
            {
               RenderEmpty();
            }
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
