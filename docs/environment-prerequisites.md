# Environment prerequisites (Edam development machine)

This exists because a whole class of startup failures — see `docs/HANDOFF.md` items 64–70 — came from
**environment and configuration drift discovered late**, deep inside the application, instead of being
caught before launch. The rule to carry forward:

> A missing database, an unpublished schema, or a malformed settings file must be reported **early and
> readably** — never discovered in a worker thread.

## 1. Configuration files must be valid JSON

`ConfigurationBuilder.AddJsonFile` parses with **`System.Text.Json`**, which allows **no comments and
no trailing commas**. A single `//` line makes the file invalid, and because `AppSettings` loads its
configuration in a **static constructor**, the failure used to surface as an opaque
`TypeInitializationException` (and left the type unusable for the process).

* `AppSettings.ConfigurationError` now carries the reason *and the path of the file that failed*.
* The first settings read throws a precise `InvalidOperationException` naming the file.
* The preflight (below) validates every `appsettings.json` before you launch anything.

## 2. Databases: Windows / SSPI authentication is the default and is always allowed

Connection strings use **Windows authentication** — `Integrated Security=True` — which means SQL Server
authenticates the **caller's Windows identity** and maps it to a login. Seeing your own Windows account
(`DOMAIN\user` or `MACHINE\user`) in a login error is **by design**, not a bug. The platform does not
require a SQL login anywhere; a connection string that wants one must say so explicitly
(`Integrated Security=False;User Id=…;Password=…`).

Windows authentication requires the caller's credentials, so it must be exercised from a **normal
terminal / the application itself**. A restricted (agent/sandboxed) shell reports
*"Failed to generate SSPI context"* or *"No credentials are available in the security package"* — that
is the shell, not the configuration.

`DataSources.VerifyDataSources()` (and `AppSettings.DataSourceProblems`) probe each configured data
source once at startup with a short connect timeout, so a stopped server cannot stall startup and a
missing database is reported early. Resolution never dead-ends: a key that nothing configures falls
back to the **default** data source (`DataSources.GetDataSource`), and only when *nothing* is
configured does the provider name the key that could not be resolved.

## 3. The reference data database (`Edam.Database`)

`Data.DataReferenceGet` — called by the domain/reference-data path — lives in the SQL Server database
`Edam.Database`, which is a **database project in this repository**:

| Database | Project |
|---|---|
| `Edam.Database` | `src/Edam.Libraries/Database/Edam.Database/Edam.Database.sqlproj` |
| `Edam.Dictionary` | `src/Edam.Libraries/Database/Edam.Dictionary/Edam.Dictionary.sqlproj` |
| `Edam.DiseaseSurveillance` | `src/Edam.Libraries/Database/Edam.HealthCare/Edam.DiseaseSurveillance/Edam.DiseaseSurveillance.sqlproj` |

There is **no** database project for `Edam.Lexicon`, although `LexiconConnectionString` targets it.

**To publish one:** open its `.sln` in Visual Studio → right-click the project → **Publish** → target
the local instance (`Data Source=.`). The login that publishes needs `db_owner` (or sysadmin) on that
instance; afterwards the objects — and the stored procedures the application calls — exist.

### The schema is not enough: the REGISTRY rows must be loaded

Publishing `Edam.Database` creates the tables **and now also loads the reference-data registry rows**:
`Scripts/PostDeployment.sql` is the project's **post-deployment** script and includes
`Scripts/Sql-Scripts/00040 - Data Registry Resources.sql` (every block guarded by `IF NOT EXISTS`, so a
re-publish is safe). Before this, those rows were a plain file that no publish ever ran, and the database
was **unusable** in a way that only showed up late — while editing an arguments file, the application
identifies (and inserts) the corresponding domain, and the insert was refused:

```
The INSERT statement conflicted with the FOREIGN KEY constraint "fk_DataDomainType".
The conflict occurred in database "Edam.Database", table "Data.DataDomainType", column 'IdNo'.
```

The domain carries a `TypeNo` that must exist in `Data.DataDomainType`; an empty registry means no
`TypeNo` can be valid. **If your database was published before this change**, load the registry once —
either re-publish the project, or run the script directly:

```powershell
sqlcmd -S . -E -C -d Edam.Database -i "src\Edam.Libraries\Database\Edam.Database\Scripts\Sql-Scripts\00040 - Data Registry Resources.sql"
```

Verify it, and expect **11** and a non-zero row count for every table:

```sql
SELECT (SELECT COUNT(*) FROM Data.DataDomainType)   AS DomainTypes,
       (SELECT COUNT(*) FROM Data.DataReferenceType) AS ReferenceTypes,
       (SELECT COUNT(*) FROM Data.DataKind)          AS Kinds;
```

`Cannot open database "X" requested by the login` from the application means the *authentication
succeeded* and the database could not be opened: normally it does not exist yet (publish it), or the
login has no user/CONNECT permission in it.

## 4. Run the preflight

```powershell
pwsh -File tools/verify-environment.ps1
pwsh -File tools/verify-environment.ps1 -Server ".\SQLEXPRESS"
```

It validates every source `appsettings.json`, reads the configured connection strings (Studio
`appsettings.json` + the app-data `Edam.Settings.json`), opens each named database with Windows
authentication, lists the database projects, and exits non-zero when something needs attention.
