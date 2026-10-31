using System;
using System.Collections.Generic;
using System.Threading;
using Edam.Application;
using Edam.Data.Projects.DependencyInjection;
using Edam.WinUI.Controls.Logging;
using Microsoft.Extensions.Logging;

namespace Edam.WinUI.Controls.Configuration;

/// <summary>
/// The <b>packaged</b> per-user state (CF-4 / ADR-0013 decision 6): the WinUI implementation of
/// <see cref="IStateStore"/> over the app's own settings, used for the <b>once-per-installation
/// marker</b>.
/// <para>
/// It is deliberately <b>not</b> the overlay: a reset of the user's answers must not clear the marker
/// (ADR-0012 decision 4 — a deleted project is never resurrected), and the marker must survive the user
/// deleting the overlay file by hand. Packaged settings being unavailable is never fatal.
/// </para>
/// <para>
/// <b>Development deployments (ADR-0017).</b> A loose Visual Studio deployment can have package identity
/// while Windows refuses it application data, so <c>ApplicationData.Current</c> throws there. The app
/// already moves its data root beside the loose deployment for exactly that case; this store follows the
/// same rule for the <b>markers</b>, keeping them in <c>Edam.PackagedState.json</c> under the same resolved
/// data root. That is a different file from the overlay (<c>Edam.UserState.json</c>), so resetting the
/// answers still cannot clear an installation marker. A signed MSIX never takes that path: its packaged
/// settings work and the file is not consulted.
/// </para>
/// </summary>
public sealed class PackagedStateStore : IStateStore
{
   /// <summary>The file used when packaged settings are unavailable — separate from the overlay's.</summary>
   public const String DEVELOPMENT_FILE_NAME = "Edam.PackagedState.json";

   private const String Category = "Edam.WinUI.Controls.Configuration";

   private static readonly Object m_Sync = new Object();
   private static Boolean? m_PackagedAvailable;
   private static Int32 m_NoticeSent;
   private static FileStateStore? m_Fallback;

   /// <summary>
   /// Where the markers actually are — answered rather than assumed, because "this value came from the
   /// package" and "this value came from a file in the deployment" are different facts.
   /// </summary>
   public String Location => PackagedAvailable()
      ? "ApplicationData.Current.LocalSettings"
      : Fallback().Location;

   private static FileStateStore Fallback()
   {
      lock (m_Sync)
      {
         return m_Fallback ??= new FileStateStore(
            AppData.GetApplicationDataLocation(), DEVELOPMENT_FILE_NAME);
      }
   }

   /// <summary>
   /// True when packaged settings can be reached. Probed <b>once</b>, because the answer cannot change
   /// during a run — and reported once, because a marker that silently stops persisting is exactly the
   /// defect this store exists to prevent.
   /// </summary>
   private static Boolean PackagedAvailable()
   {
      lock (m_Sync)
      {
         if (m_PackagedAvailable.HasValue)
         {
            return m_PackagedAvailable.Value;
         }

         try
         {
            _ = Windows.Storage.ApplicationData.Current.LocalSettings.Values.Count;
            m_PackagedAvailable = true;
         }
         catch (Exception ex)
         {
            m_PackagedAvailable = false;
            Notice("Packaged state is unavailable (" + ex.Message.Trim() + "). For this deployment the " +
               "once-per-installation markers are kept in '" + Fallback().Location + "' instead; a signed " +
               "MSIX uses its package settings.");
         }

         return m_PackagedAvailable.Value;
      }
   }

   /// <summary>Packaged settings stopped working mid-run: continue in the file rather than lose the write.</summary>
   private static void Demote(Exception ex)
   {
      lock (m_Sync)
      {
         m_PackagedAvailable = false;
      }

      Notice("Packaged state became unavailable (" + ex.Message.Trim() + "); continuing in '" +
         Fallback().Location + "'.");
   }

   private static void Notice(String message)
   {
      if (Interlocked.Exchange(ref m_NoticeSent, 1) == 0)
      {
         AppDiagnostics.Write(message, LogLevel.Warning, Category);
      }
   }

   public String? Read(String key)
   {
      if (PackagedAvailable())
      {
         try
         {
            return Windows.Storage.ApplicationData.Current.LocalSettings.Values[key]?.ToString();
         }
         catch (Exception ex)
         {
            Demote(ex);
         }
      }

      return Fallback().Read(key);
   }

   public void Write(String key, String? value)
   {
      if (String.IsNullOrWhiteSpace(key))
      {
         return;
      }

      if (PackagedAvailable())
      {
         try
         {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            if (value is null)
            {
               settings.Remove(key);
            }
            else
            {
               settings[key] = value;
            }

            return;
         }
         catch (Exception ex)
         {
            Demote(ex);
         }
      }

      Fallback().Write(key, value);
   }

   public IReadOnlyDictionary<String, String?> ReadAll()
   {
      if (PackagedAvailable())
      {
         var values = new Dictionary<String, String?>(StringComparer.OrdinalIgnoreCase);

         try
         {
            foreach (var pair in Windows.Storage.ApplicationData.Current.LocalSettings.Values)
            {
               values[pair.Key] = pair.Value?.ToString();
            }

            return values;
         }
         catch (Exception ex)
         {
            Demote(ex);
         }
      }

      return Fallback().ReadAll();
   }
}
