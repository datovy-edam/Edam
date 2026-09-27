using System;

namespace Edam.WinUI.Controls.Configuration;

/// <summary>
/// The <b>readiness gate</b> for first-run questions (CF-4 / ADR-0013): the application must not ask a
/// person who has not signed in. The answer is a <b>per-user preference</b> (so on a shared machine the
/// wrong person could answer it), and creating content may need an <b>authenticated, authorized</b>
/// session — a container can be backed by PostgreSQL or a service with a credential (LM-4).
/// <para>
/// The signal is <see cref="Edam.Application.Session.IsUserLogged"/>, set by
/// <c>LoginViewModel.PersistUser</c> through <c>Session.SetUser</c> when login succeeds. Note the trap:
/// <c>Session.LoggedUser</c> <b>fabricates</b> an empty user when nobody is logged in — it is never null —
/// so readiness must be asked through <c>IsUserLogged</c>, never by null-checking the user.
/// </para>
/// </summary>
public static class ConfigurationReadiness
{
   /// <summary>True once the session has an active logged-in user.</summary>
   public static bool IsSignedIn
   {
      get
      {
         try
         {
            return Edam.Application.Session.IsUserLogged;
         }
         catch (Exception)
         {
            // an unavailable session means "not signed in" — never a crash
            return false;
         }
      }
   }

   /// <summary>A short phrase for diagnostics: whether the ask is allowed yet, and why not.</summary>
   public static string Describe() => IsSignedIn ? "signed in" : "waiting for sign-in";
}
