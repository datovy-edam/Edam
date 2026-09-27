using System;

namespace Edam.WinUI.Controls.Configuration;

/// <summary>
/// The <b>readiness gate</b> for first-run questions (CF-4 / ADR-0013): the application must not ask a
/// person who has not signed in — the answer is a <b>per-user preference</b> (so a shared machine could
/// otherwise get the wrong person's answer), and creating content may need an authenticated, authorized
/// session.
/// <para>
/// <b>Why this cannot rely on <c>Session.IsUserLogged</c> alone:</b> that is
/// <c>Session.LoggedUser.IsActive</c>, and <c>UserLoggedInfo.IsActive</c> is written in exactly two
/// places — <c>ClearFields()</c> sets it <b>false</b>, and <c>ReadData(reader)</c> sets it from the
/// <b>database</b>. A login that never reads the account row — a local/PIN sign-in, which is what a
/// Studio with no identity connection does — leaves <c>IsActive == false</c> even though the person
/// <i>is</i> signed in, so a gate keyed on it never opens. (That is exactly why the prompt stayed away
/// after signing in.)
/// </para>
/// <para>
/// The reliable signal is therefore the code that <b>performs</b> the login:
/// <c>LoginViewModel.PersistUser</c> calls <see cref="MarkSignedIn"/> as it stores the session user.
/// <c>Session.IsUserLogged</c> and a non-empty <c>Session.UserId</c> are accepted as secondary evidence.
/// </para>
/// </summary>
public static class ConfigurationReadiness
{
   private static Boolean m_SignedIn;

   /// <summary>
   /// Record that a person has signed in — called by the login flow itself, so readiness never depends on
   /// a flag that the login path may not populate.
   /// </summary>
   public static void MarkSignedIn() => m_SignedIn = true;

   /// <summary>True once a person has signed in — host-marked, or evidenced by the session.</summary>
   public static bool IsSignedIn
   {
      get
      {
         if (m_SignedIn)
         {
            return true;
         }

         try
         {
            return Edam.Application.Session.IsUserLogged ||
               !String.IsNullOrWhiteSpace(Edam.Application.Session.UserId);
         }
         catch (Exception)
         {
            // an unavailable session means "not signed in" — never a crash
            return false;
         }
      }
   }

   /// <summary>A short phrase for diagnostics: whether the ask is allowed yet, and on what evidence.</summary>
   public static string Describe()
   {
      try
      {
         if (m_SignedIn)
         {
            return "signed in (marked by the login flow)";
         }

         if (Edam.Application.Session.IsUserLogged)
         {
            return "signed in (the session reports an active user)";
         }

         if (!String.IsNullOrWhiteSpace(Edam.Application.Session.UserId))
         {
            return "signed in (the session has a user id)";
         }
      }
      catch (Exception)
      {
         // fall through to "waiting"
      }

      return "waiting for sign-in";
   }
}
