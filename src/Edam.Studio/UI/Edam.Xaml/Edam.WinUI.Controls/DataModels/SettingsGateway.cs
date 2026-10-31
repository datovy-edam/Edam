#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Edam.Data.Projects.DependencyInjection;
using Edam.Settings.Contracts;
using Edam.Settings.Core;

namespace Edam.WinUI.Controls.DataModels
{
   /// <summary>
   /// The Studio's <b>composition</b> of the settings surface (ADR-0016): the platform's catalog, editor and service,
   /// wired to the Studio's own <b>apply hook</b> so that a change is <b>real</b> rather than merely stored.
   /// <para>
   /// There is deliberately <b>one</b> instance for the process: the first-run prompt, the settings page and anything
   /// added later all drive the same service, so "ask once", "remember the answer" and "apply it" cannot drift apart.
   /// No surface in this application builds its own catalog, its own store or its own validator.
   /// </para>
   /// </summary>
   public static class SettingsGateway
   {
      private static SettingsService? m_Service;

      /// <summary>The one settings service for this process.</summary>
      public static SettingsService Service => m_Service ??= Build();

      /// <summary>The catalog the surfaces render.</summary>
      public static ISettingsCatalog Catalog => Service.Catalog;

      private static SettingsService Build()
      {
         // the SAME answers service the first-run prompt uses (CF-4) — not a second one
         var user = ProjectServicesHelper.UserConfiguration;

         return new SettingsService(
            new ProjectSettingsCatalog(user),
            new ProjectSettingsEditor(user),
            new StudioSettingsApplier());
      }

      /// <summary>
      /// Making a stored value take effect is the host's work (ADR-0016): only the item the composed storage was built
      /// from needs anything doing, and recycling it is what the container dialog did before this surface existed —
      /// moved here so <b>every</b> surface gets it, not just the one that used to own the control.
      /// </summary>
      private sealed class StudioSettingsApplier : ISettingsApplier
      {
         public Task<String?> ApplyAsync(
            SettingsItemInfo item, String? value, CancellationToken ct = default)
         {
            try
            {
               if (String.Equals(item.Id, ConfigurableItems.CATALOG_CONNECTION_KEY,
                      StringComparison.OrdinalIgnoreCase))
               {
                  // the projects storage is rebuilt from the answers, so the change is live
                  ProjectServicesHelper.ResetStorage();
               }

               return Task.FromResult<String?>(null);
            }
            catch (Exception ex)
            {
               // the value is stored; failing to apply it is REPORTED so the page can say so plainly
               return Task.FromResult<String?>(String.IsNullOrWhiteSpace(ex.Message)
                  ? "the storage could not be rebuilt"
                  : ex.Message);
            }
         }
      }
   }
}
