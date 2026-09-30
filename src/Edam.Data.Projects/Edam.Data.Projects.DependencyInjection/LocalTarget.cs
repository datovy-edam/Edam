namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// Is this a <b>local</b> target? ADR-0012 offers and creates the starter project only on a local
/// installation — a file-system binding, or a catalog whose server is <b>this machine</b> — so a shared or
/// remote collection is never given one. The test is deliberately simple and readable rather than clever:
/// a server of <c>.</c>, <c>(local)</c>, <c>localhost</c>, <c>127.0.0.1</c> or <c>::1</c> (or a server not
/// stated at all) is local; anything else is not (LM-8 uses this for the MS-SQL catalog store).
/// </summary>
public static class LocalTarget
{
   private static readonly string[] ServerKeys =
      { "server", "data source", "datasource", "host", "address", "addr", "network address" };

   private static readonly string[] LocalServers =
      { ".", "(local)", "localhost", "127.0.0.1", "::1", "(localdb)" };

   /// <summary>True when the connection string names this machine (or names no server at all).</summary>
   public static Boolean IsLocalServer(String? connectionString)
   {
      var server = ServerName(connectionString);
      if (String.IsNullOrWhiteSpace(server))
      {
         return true;   // nothing stated: treated as local (the caller already requires a local target)
      }

      // `host,port` and `.\\INSTANCE` both mean the same machine
      var name = server.Split(',')[0].Split('\\')[0].Trim();

      foreach (var local in LocalServers)
      {
         if (String.Equals(name, local, StringComparison.OrdinalIgnoreCase))
         {
            return true;
         }
      }

      return name.StartsWith("127.", StringComparison.Ordinal) ||
             String.Equals(name, "::1", StringComparison.Ordinal);
   }

   /// <summary>The server named by a connection string, or null (tolerant of separators and spacing).</summary>
   public static String? ServerName(String? connectionString)
   {
      if (String.IsNullOrWhiteSpace(connectionString))
      {
         return null;
      }

      foreach (var part in connectionString!.Split(';'))
      {
         var separator = part.IndexOf('=');
         if (separator <= 0)
         {
            continue;
         }

         var key = part[..separator].Trim();
         foreach (var candidate in ServerKeys)
         {
            if (String.Equals(key, candidate, StringComparison.OrdinalIgnoreCase))
            {
               return part[(separator + 1)..].Trim();
            }
         }
      }

      return null;
   }
}
