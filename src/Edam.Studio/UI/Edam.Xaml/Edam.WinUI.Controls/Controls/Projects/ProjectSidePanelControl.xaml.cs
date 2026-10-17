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
using Edam.WinUI.Controls.ViewModels;
using Edam.WinUI.Controls.Common;
using Edam.WinUI.Controls.DataModels;

namespace Edam.WinUI.Controls.Projects
{
   public sealed partial class ProjectSidePanelControl : UserControl
   {
      private ProjectViewerViewModel m_ViewModel;
      public ProjectViewerViewModel ViewModel
      {
         get { return m_ViewModel; }
         set { m_ViewModel = value; }
      }
      public ProjectSidePanelControl()
      {
         this.InitializeComponent();
         m_ViewModel = new ProjectViewerViewModel();
         DataContext = m_ViewModel;

         // DP-5: the platform can change the set of projects BEHIND this panel — the first-run starter
         // project is created by the configuration prompt, which knows nothing about this tree. Without
         // this, the panel keeps saying "no projects yet" until the application is restarted, although the
         // project exists. The subscription follows the control's own lifecycle, so a panel that is gone is
         // not kept alive by it (and re-loading does not subscribe twice).
         Loaded += (sender, args) =>
         {
            ProjectServicesHelper.ProjectsChanged -= OnProjectsChanged;
            ProjectServicesHelper.ProjectsChanged += OnProjectsChanged;
         };
         Unloaded += (sender, args) =>
            ProjectServicesHelper.ProjectsChanged -= OnProjectsChanged;
      }

      /// <summary>
      /// Reload the projects tree when the set of projects changed elsewhere — marshalled to the UI thread,
      /// whatever raised the change, because the tree is a bound UI object.
      /// </summary>
      private void OnProjectsChanged()
      {
         if (DispatcherQueue.HasThreadAccess)
         {
            m_ViewModel.FetchProjectFolderInfo();
         }
         else
         {
            DispatcherQueue.TryEnqueue(() => m_ViewModel.FetchProjectFolderInfo());
         }
      }

      private void ProjectRefresh_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.FetchProjectFolderInfo();
      }

      private void ProjectAdd_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.ProjectAddNew();
      }

      /// <summary>
      /// The <b>container manager</b> (LM-9 / ADR-0014 decision 8): ONE surface that reviews the catalog's
      /// containers, adds one, edits a description, removes a container <b>with everything it holds</b>, and
      /// states (or lets a person deliberately override) where the catalog keeps the data. It replaces the
      /// separate info and remove buttons — the dialog <i>is</i> the information surface.
      /// </summary>
      private async void ContainerManager_Click(object sender, RoutedEventArgs e)
      {
         await ContainerManagerDialog.ShowAsync(this.XamlRoot, m_ViewModel.RefreshCollectionsAsync);

         // and once more when the modal CLOSES: the containers may have been added, described or removed inside
         // it, and the selector beside this button must show what is really there — not the list it was built
         // with when the panel first loaded
         await m_ViewModel.RefreshCollectionsAsync();
      }

      private void TreeViewItem_PointerPressed(
         object sender, PointerRoutedEventArgs e)
      {
      }

      private void TreeItemCopy_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.ItemCopy();
      }

      private void TreeAddItems_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.SetItem(TreeView.SelectedItem);
         m_ViewModel.ProjectAddItems();
      }

      private void ProjectRename_Click(object sender, RoutedEventArgs e)
      {

      }

      #region -- 4.00 - Select Item Support Methods...

      /// <summary>
      /// The Arguments are selected... notify others.
      /// </summary>
      /// <param name="selectedItem">selected item</param>
      private async void ItemSelected(object selectedItem = null)
      {
         object sitem = selectedItem == null ?
            TreeView.SelectedItem : selectedItem;
         var results = await m_ViewModel.TreeItemSelected(sitem);

         if (results.Success)
         {
            string mess = String.IsNullOrWhiteSpace(results.ReturnText) ?
               String.Empty : results.ReturnText;

            if (m_ViewModel.NotifyEventCompletion != null)
            {
               NotificationArgs args = new NotificationArgs();
               args.Type = NotificationType.ProjectItemSelected;
               args.ResultsLog = results;
               args.EventData = results.Data;
               m_ViewModel.NotifyEventCompletion(this, args);
            }
            //AssetEditorControl.SetEditorText(
            //   results.Data, results.ReturnText);
         }
      }

      private void TextBlock_DoubleTapped(
         object sender, DoubleTappedRoutedEventArgs e)
      {
         ItemSelected();
      }

      /// <summary>
      /// Create a <b>skeleton arguments document</b> in a project's Arguments folder. Offered only there (§2.2),
      /// never overwriting anything, and written through the platform's resource seam so it lands correctly for
      /// a file-system or a catalog project alike. It carries every §2.3 section with placeholders, so it is
      /// <b>born valid</b> and Validate (Ctrl+Shift+V) names what is still to be filled in.
      /// </summary>
      private async void TreeAddArgumentsFile_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            // The node comes from the MENU ITEM's own Tag (the template binds it to the item the menu belongs
            // to). A right-click does not necessarily SELECT a node, so TreeView.SelectedItem is the wrong source
            // — leaning on it made this command return early and report into a collapsed panel, which is exactly
            // what "nothing happened" was. The current selection stays only as a last resort.
            var folder = (sender as MenuFlyoutItem)?.Tag as ProjectItem
               ?? m_ViewModel.SelectedItem;

            if (folder is null || folder.Item?.Full is null || folder.Item.Full.Length == 0)
            {
               await ShowArgumentsProblemAsync(
                  "No project folder was identified. Right-click the project's \"Arguments\" folder and choose " +
                  "this command again.");
               return;
            }

            var projectName = ProjectServicesHelper.ProjectNameFromResourcePath(folder.Item.Full)
               ?? folder.Name;

            var stated = await AskForArgumentsFileNameAsync(projectName);
            if (String.IsNullOrWhiteSpace(stated)) return;

            var name = stated!.Trim();
            if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
               name = name.Substring(0, name.Length - ".json".Length);
            }
            if (!name.EndsWith(".Args", StringComparison.OrdinalIgnoreCase))
            {
               name += ".Args";
            }
            name += ".json";

            var path = folder.Item.Full.Replace('\\', '/').TrimEnd('/') + "/" + name;
            var collectionId = m_ViewModel.SelectedUriItem?.UriText;

            if (await ProjectServicesHelper.ReadProjectTextAsync(path, collectionId) is not null)
            {
               await ShowArgumentsProblemAsync("'" + name + "' is already there — nothing was overwritten.");
               return;
            }

            var created = await ProjectServicesHelper.WriteProjectTextAsync(
               path, ArgumentsSkeleton(projectName, name), collectionId);

            if (!created)
            {
               await ShowArgumentsProblemAsync(
                  "The arguments skeleton '" + path + "' could not be created. The diagnostics panel carries the " +
                  "detail — the collection may be read-only, or the platform may not write there yet.");
               return;
            }

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Created the arguments skeleton '" + path + "' — press Validate (Ctrl+Shift+V) to see what is " +
               "still to be filled in.",
               Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");

            // the tree lists the new document without a restart, and the panel (which says where it went and what
            // is still to be filled in) shows itself
            ProjectServicesHelper.NotifyProjectsChanged();
            ProjectServicesHelper.RequestAttention();
         }
         catch (Exception ex)
         {
            await ShowArgumentsProblemAsync("The arguments file could not be created: " + ex.Message);
         }
      }

      /// <summary>
      /// A command the person INVOKED has to answer them. Its outcome is shown where the command was pressed and
      /// not merely written to a panel that may be collapsed — going quiet is precisely what "nothing happened"
      /// looked like.
      /// </summary>
      private async System.Threading.Tasks.Task ShowArgumentsProblemAsync(String message)
      {
         try
         {
            var dialog = new ContentDialog
            {
               XamlRoot = this.XamlRoot,
               Title = "New arguments file",
               Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
               CloseButtonText = "Close",
            };

            await dialog.ShowAsync();
         }
         catch (Exception)
         {
            // the panel below still carries it
         }

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(message,
            Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");

         ProjectServicesHelper.RequestAttention();
      }

      /// <summary>Ask for the file name; null when the person cancels.</summary>
      private async System.Threading.Tasks.Task<String?> AskForArgumentsFileNameAsync(String projectName)
      {
         var input = new TextBox
         {
            Text = "NewProcess.Args.json",
            PlaceholderText = "name.Args.json",
         };

         var dialog = new ContentDialog
         {
            XamlRoot = this.XamlRoot,
            Title = "New arguments file",
            Content = new StackPanel
            {
               Spacing = 8,
               Children =
               {
                  new TextBlock
                  {
                     Text = "Created in " + projectName + "/Arguments from the specification's skeleton " +
                            "(§2.3), with placeholders for what only you know.",
                     TextWrapping = TextWrapping.Wrap,
                  },
                  input,
               },
            },
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
         };

         var answer = await dialog.ShowAsync();
         return answer == ContentDialogResult.Primary ? input.Text : null;
      }

      /// <summary>
      /// The skeleton: every section §2.3 defines, with the project's <b>name</b> already correct (step 4 demands
      /// it) and a procedure the runner really knows (step 5), so the document validates from the first second.
      /// The placeholders are exactly what the specification cannot know — they are reported as warnings until
      /// they are filled in, which is the point of creating the file from here.
      /// </summary>
      private static String ArgumentsSkeleton(String projectName, String fileName)
      {
         var process = System.IO.Path.GetFileNameWithoutExtension(
            System.IO.Path.GetFileNameWithoutExtension(fileName));
         if (String.IsNullOrWhiteSpace(process)) process = "NewProcess";

         return """
         {
            "@context": { "edam": "http://www.datovy.com/edam/arguments" },
            "Domain": {
               "DomainId": "<project>",
               "Description": ""
            },
            "Namespace": {
               "OrganizationDomainId": "",
               "Uri": "",
               "Prefix": "",
               "Extension": "",
               "RootElementName": ""
            },
            "Project": {
               "Name": "<project>",
               "VersionId": "v1r0"
            },
            "Process": {
               "RecordId": null,
               "Name": "<project>.<process>",
               "OrganizationId": "Datovy",
               "OrganizationDomainUri": null,
               "ProcedureName": "XsdToAssets",
               "ProcedureTag": "",
               "ScanFilesFolder": false,
               "SchemaType": 1,
               "NextProcess": null,
               "NextProcedure": []
            },
            "InputFile": {
               "Extension": "xsd",
               "Name": "<process>",
               "Path": "./Files",
               "Full": null
            },
            "OutputFile": {
               "Extension": "xlsx",
               "Name": "<process>",
               "Path": "./Documents",
               "Full": null
            },
            "UriList": [
               "./Files/<process>.xsd"
            ],
            "InspectArguments": {
               "ListLength": "1",
               "MaxThreshold": "1"
            },
            "ConnectionString": "",
            "ElementTransform": null,
            "TextMapFilePath": "./TextMaps/XsdTextMap.json"
         }
         """.Replace("<project>", projectName).Replace("<process>", process);
      }

      private void TreeItemEdit_Click(object sender, RoutedEventArgs e)
      {
         ItemSelected();
      }

      #endregion
      #region -- 4.00 - Manage Item Text Viewing and Editing

      private void ItemText_PointerPressed(
         object sender, PointerRoutedEventArgs e)
      {
         var txtBox = e.OriginalSource as TextBlock;
         if (txtBox != null)
         {
            m_ViewModel.TreeItemSetupEditor(txtBox.DataContext);
            ItemSelected(txtBox.DataContext);
         }
      }

      private void ItemTextEdit_KeyDown(object sender, KeyRoutedEventArgs e)
      {
         if (e.Key == Windows.System.VirtualKey.Enter)
         {
            m_ViewModel.TreeItemSetupEditor(TreeView.SelectedItem, false);
         }
         else if (e.Key == Windows.System.VirtualKey.Escape)
         {
            m_ViewModel.TreeItemSetupEditor(TreeView.SelectedItem, false, true);
         }
      }

      #endregion
      #region -- 4.00 - Delete Tree Item (File or Folder)

      private void TreeItemDelete_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.SetItem(TreeView.SelectedItem);
         m_ViewModel.DeleteTreeItem(TreeView);
      }

      private void TreeViewItem_KeyDown(object sender, KeyRoutedEventArgs e)
      {
         if (e.Key == Windows.System.VirtualKey.Delete ||
            e.Key == Windows.System.VirtualKey.Back)
         {
            m_ViewModel.SetItem(TreeView.SelectedItem);
            m_ViewModel.DeleteTreeItem(TreeView);
            e.Handled = true;
         }
      }

      #endregion

   }
}
