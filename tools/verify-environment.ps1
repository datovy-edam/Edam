<#
.SYNOPSIS
    Preflight for an Edam development machine: configuration parses, and the databases the
    configuration names are actually usable.

.DESCRIPTION
    Checks, in order:
      1. every source appsettings.json parses as JSON (JSON allows no comments and no trailing commas —
         a malformed file used to kill startup with an opaque TypeInitializationException);
      2. the connection strings configured for the Studio (appsettings.json) and, when present, the
         data source in the app-data Edam.Settings.json; the reference-data key must actually be
         configured;
      3. the database projects in this repository (what to publish when a database is missing);
      4. what the SERVER says: the databases that exist on the target instance;
      5. for each configured database — DOES IT EXIST? if not, which project to publish; if it exists,
         can this login open it? (a login can authenticate and still have no user in that database).

    Windows / SSPI authentication (Integrated Security=True) is used throughout — the platform's
    default, and the only mode it relies on: it never requires a SQL login.

    Exits non-zero when something needs attention.

.NOTES
    Run this from a NORMAL terminal. Windows authentication needs the caller's credentials, so a
    restricted/agent shell reports "Failed to generate SSPI context" or "No credentials are available
    in the security package" — that is a limitation of the shell, not of the configuration, and this
    script reports it as INCONCLUSIVE rather than as a missing database.

.EXAMPLE
    pwsh -File tools/verify-environment.ps1
.EXAMPLE
    pwsh -File tools/verify-environment.ps1 -Server ".\SQLEXPRESS"
#>
[CmdletBinding()]
param(
    [string] $Server = ".",
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [int]    $TimeoutSeconds = 5
)

$ErrorActionPreference = "Continue"
$problems = @()
$notes = @()
$inconclusive = 0

function Get-DatabaseName([string] $connectionString) {
    foreach ($part in ($connectionString -split ";")) {
        $kv = $part -split "=", 2
        if ($kv.Count -eq 2) {
            $k = $kv[0].Trim().ToLowerInvariant()
            if ($k -in @("initial catalog", "database")) { return $kv[1].Trim() }
        }
    }
    return "(unknown)"
}

function Test-WindowsAuth([string] $connectionString) {
    return ($connectionString -match "(?i)integrated\s+security\s*=\s*(true|sspi)")
}

function Test-AuthBlocked([string] $text) {
    return ($text -match "(?i)SSPI|security package|Encryption not supported|unable to establish connection")
}

function Invoke-Sql([string] $database, [string] $query) {
    $result = [pscustomobject]@{ Ok = $false; Text = "" }
    $output = & $script:sqlcmd -S $Server -E -C -l $TimeoutSeconds -d $database -h -1 -W -Q $query 2>&1
    $result.Text = (($output | Out-String).Trim())
    $result.Ok = ($LASTEXITCODE -eq 0)
    return $result
}

# ---- 1. configuration parses ---------------------------------------------------------------------
Write-Host "1/5  validating configuration files" -ForegroundColor Cyan
$configs = Get-ChildItem -Path $RepoRoot -Recurse -Filter "appsettings.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }
foreach ($file in $configs) {
    try { $null = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($file.FullName)) }
    catch { $problems += "INVALID JSON: $($file.FullName)`n        $($_.Exception.Message)" }
}
Write-Host "     checked $($configs.Count) file(s)"

# ---- 2. what the configuration names -------------------------------------------------------------
Write-Host "2/5  reading configured data sources" -ForegroundColor Cyan
$wanted = @()

$studioConfig = Join-Path $RepoRoot "src\Edam.Studio\Edam.Studio\appsettings.json"
if (Test-Path $studioConfig) {
    $root = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($studioConfig)).RootElement
    $cs = [System.Text.Json.JsonElement]::new()
    if ($root.TryGetProperty("ConnectionStrings", [ref] $cs)) {
        foreach ($p in $cs.EnumerateObject()) {
            $wanted += [pscustomobject]@{ Source = "appsettings.json"; Key = $p.Name; Connection = $p.Value.GetString() }
        }
    }
    $ref = [System.Text.Json.JsonElement]::new()
    if ($root.TryGetProperty("ReferenceData", [ref] $ref)) {
        $keyEl = [System.Text.Json.JsonElement]::new()
        if ($ref.TryGetProperty("ConnectionStringKey", [ref] $keyEl)) {
            $key = $keyEl.GetString()
            if ($key -and -not ($wanted | Where-Object { $_.Key -eq $key })) {
                $problems += "the reference-data key '$key' is requested but no 'ConnectionStrings:$key' is configured"
            }
        }
    }
}

$settingsPath = Join-Path $RepoRoot "app-data\Edam.Settings.json"
if (Test-Path $settingsPath) {
    $sroot = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($settingsPath)).RootElement
    $ds = [System.Text.Json.JsonElement]::new()
    if ($sroot.TryGetProperty("DataSource", [ref] $ds)) {
        $def = [System.Text.Json.JsonElement]::new()
        if ($ds.TryGetProperty("DefaultConnectionString", [ref] $def)) {
            $wanted += [pscustomobject]@{ Source = "Edam.Settings.json"; Key = "(default)"; Connection = $def.GetString() }
        }
    }
    else {
        $notes += "app-data/Edam.Settings.json carries no 'DataSource' section (ADR-0010 keeps machine values out of the seed; the app-data layer or appsettings.json must supply them)"
    }
}
Write-Host "     found $($wanted.Count) connection string(s)"

# ---- 3. database projects in this repository -----------------------------------------------------
Write-Host "3/5  database projects in this repository" -ForegroundColor Cyan
$projectFor = @{}
foreach ($p in (Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.sqlproj" -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" })) {
    $projectFor[$p.BaseName] = $p.FullName
    Write-Host "     $($p.BaseName)"
}

# ---- 4. what the server says --------------------------------------------------------------------
Write-Host "4/5  asking the server (Windows/SSPI authentication)" -ForegroundColor Cyan
$script:sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
$serverDatabases = $null

if (-not $script:sqlcmd) {
    $notes += "sqlcmd not found on PATH — the database probe was skipped"
}
else {
    $listed = Invoke-Sql "master" "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"
    if (-not $listed.Ok) {
        if (Test-AuthBlocked $listed.Text) {
            $inconclusive++
            $notes += "could not authenticate from THIS shell: Windows/SSPI authentication needs a normal token — re-run from a normal terminal. This is NOT evidence that databases are missing."
        }
        else {
            $problems += "this login cannot connect to instance '$Server' at all:`n        $($listed.Text)`n        A login for the Windows account is required on the instance (being a local administrator does not by itself grant access)."
        }
    }
    else {
        $serverDatabases = @($listed.Text -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })
        Write-Host "     instance has $($serverDatabases.Count) database(s): $($serverDatabases -join ', ')"
    }
}

# ---- 5. classify each configured database -------------------------------------------------------
Write-Host "5/5  checking each configured database" -ForegroundColor Cyan
foreach ($w in $wanted) {
    $db = Get-DatabaseName $w.Connection
    $auth = if (Test-WindowsAuth $w.Connection) { "Windows auth" } else { "SQL login (not the platform default)" }
    if (-not (Test-WindowsAuth $w.Connection)) {
        $notes += "$($w.Key): does not use Integrated Security (Windows auth) — the platform's default and only mode it relies on"
    }

    if ($null -ne $serverDatabases -and -not ($serverDatabases -contains $db)) {
        $hint = if ($projectFor.ContainsKey($db)) {
            "publish $($projectFor[$db])"
        } else {
            "no database project for it exists in this repository — find its source (backup/script) or point the connection string elsewhere"
        }
        $problems += "database '$db' ($($w.Key), $auth) DOES NOT EXIST on '$Server' — $hint"
        continue
    }

    $probe = Invoke-Sql $db "SET NOCOUNT ON; SELECT 1;"
    if ($probe.Ok) {
        Write-Host "     OK   $db  ($($w.Key), $auth)" -ForegroundColor Green
        continue
    }

    if (Test-AuthBlocked $probe.Text) {
        $inconclusive++
        $notes += "could not authenticate from THIS shell for '$db' ($($w.Key)) — re-run from a normal terminal; NOT evidence that the database is missing."
    }
    elseif ($probe.Text -match "(?i)cannot open database|login failed") {
        $problems += "the login CANNOT OPEN '$db' ($($w.Key)) although the database exists:`n        $($probe.Text)`n        The Windows account needs a user in that database, e.g. CREATE USER [DOMAIN\user] FOR LOGIN [DOMAIN\user]; ALTER ROLE db_datareader ADD MEMBER [DOMAIN\user];"
    }
    else {
        $problems += "cannot open database '$db' ($($w.Key), from $($w.Source)):`n        $($probe.Text)"
    }
}

# ---- report -------------------------------------------------------------------------------------
Write-Host ""
if ($problems.Count -gt 0) {
    Write-Host "PROBLEMS ($($problems.Count)):" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}
if ($notes.Count -gt 0) {
    Write-Host "NOTES:" -ForegroundColor Yellow
    $notes | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($problems.Count -eq 0) {
    if ($inconclusive -gt 0) {
        Write-Host "Environment OK as far as this shell can tell — the database probe was INCONCLUSIVE ($inconclusive)." -ForegroundColor Yellow
        Write-Host "Windows/SSPI authentication needs a normal token, so re-run this from a normal terminal." -ForegroundColor Yellow
    }
    else {
        Write-Host "Environment OK." -ForegroundColor Green
    }
    exit 0
}
exit 1
