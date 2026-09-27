using System;
using System.Collections.Generic;
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
/// deleting the overlay file by hand. Packaged settings being unavailable is never fatal: reads report
/// "nothing stated" and a failed write is <b>logged</b> rather than thrown.
/// </para>
/// </summary>
public sealed class PackagedStateStore : IStateStore
{
   public string Location => "ApplicationData.Current.LocalSettings";

   public string? Read(string key)
   {
      try
      {
         var value = Windows.Storage.ApplicationData.Current.LocalSettings.Values[key];
         return value?.ToString();
      }
      catch (Exception)
      {
         return null;
      }
   }

   public void Write(string key, string? value)
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
      }
      catch (Exception ex)
      {
         // never fatal — but never silent either
         AppDiagnostics.Write(
            $"Could not write packaged state '{key}': {ex.Message}",
            LogLevel.Warning, "Edam.WinUI.Controls.Configuration");
      }
   }

   public IReadOnlyDictionary<string, string?> ReadAll()
   {
      var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

      try
      {
         foreach (var pair in Windows.Storage.ApplicationData.Current.LocalSettings.Values)
         {
            values[pair.Key] = pair.Value?.ToString();
         }
      }
      catch (Exception)
      {
         // unavailable packaged settings mean "nothing stated"
      }

      return values;
   }
}
