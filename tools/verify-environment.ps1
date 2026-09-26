<#
.SYNOPSIS
    Preflight for an Edam development machine: the configuration parses, and the databases that
    configuration names can actually be opened.

.DESCRIPTION
    Checks, in order:
      1. every source appsettings.json parses as JSON (JSON allows no comments and no trailing commas
         — a malformed file used to kill startup with an opaque TypeInitializationException);
      2. the connection strings configured for the Studio (appsettings.json) and, when present, the
         data source in the app-data Edam.Settings.json;
      3. each named database can be opened, using WINDOWS / SSPI AUTHENTICATION when the connection
         string asks for it (Integrated Security=True — the platform's default, and the only mode the
         platform itself relies on: it never requires a SQL login);
      4. which database projects exist and therefore what to publish when a database is missing.

    Exits non-zero when anything needs attention.

.NOTES
    Run this from a NORMAL terminal. Windows authentication needs the caller's credentials, so a
    restricted/agent shell reports "Failed to generate SSPI context" or "No credentials are available
    in the security package" — that is a limitation of the shell, not of the configuration.

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

# ---- 1. configuration parses -------------------------------------------------------------------
Write-Host "1/4  validating configuration files" -ForegroundColor Cyan
$configs = Get-ChildItem -Path $RepoRoot -Recurse -Filter "appsettings.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }
foreach ($file in $configs) {
    try { $null = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($file.FullName)) }
    catch { $problems += "INVALID JSON: $($file.FullName)`n        $($_.Exception.Message)" }
}
Write-Host "     checked $($configs.Count) file(s)"

# ---- 2. what the configuration names -----------------------------------------------------------
Write-Host "2/4  reading configured data sources" -ForegroundColor Cyan
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
    } else {
        $notes += "app-data/Edam.Settings.json carries no 'DataSource' section (ADR-0010 keeps machine values out of the seed; the app-data layer or appsettings.json must supply them)"
    }
}
Write-Host "     found $($wanted.Count) connection string(s)"

# ---- 3. can they be opened? (Windows/SSPI auth) --------------------------------------------------
Write-Host "3/4  opening each database (Windows/SSPI authentication)" -ForegroundColor Cyan
$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (-not $sqlcmd) {
    $notes += "sqlcmd not found on PATH — skipped the database probe"
} else {
    foreach ($w in $wanted) {
        if (-not (Test-WindowsAuth $w.Connection)) {
            $notes += "$($w.Key): does not use Integrated Security (Windows auth) — the platform's default and only mode it relies on"
        }
        $db = Get-DatabaseName $w.Connection
        $output = & $sqlcmd -S $Server -E -C -l $TimeoutSeconds -d $db -h -1 -W -Q "SET NOCOUNT ON; SELECT 1;" 2>&1
        $text = ($output | Out-String).Trim()

        if ($LASTEXITCODE -ne 0) {
            # Distinguish "this shell cannot authenticate" from "the database is not there": Windows
            # authentication needs the caller's credentials, which restricted shells do not have.
            if ($text -match "(?i)SSPI|security package|Encryption not supported|unable to establish connection") {
                $inconclusive++
                $notes += "could not authenticate from THIS shell for '$db' ($($w.Key)): Windows/SSPI authentication needs a normal token — re-run from a normal terminal. This is NOT evidence that the database is missing."
            }
            else {
                $problems += "cannot open database '$db' ($($w.Key), from $($w.Source)):`n        $text"
            }
        }
        else {
            Write-Host "     OK   $db  ($($w.Key))" -ForegroundColor Green
        }
    }
}

# ---- 4. what to publish -------------------------------------------------------------------------
Write-Host "4/4  database projects in this repository" -ForegroundColor Cyan
$projects = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.sqlproj" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }
foreach ($p in $projects) {
    Write-Host "     $($p.BaseName)  ->  $($p.FullName)"
}
if ($projects.Count -gt 0) {
    $notes += "to publish one: open its .sln in Visual Studio, right-click the project, Publish, target '$Server'"
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
        Write-Host "Environment OK as far as this shell can tell — the database probe was INCONCLUSIVE ($inconclusive):" -ForegroundColor Yellow
        Write-Host "Windows/SSPI authentication needs a normal token, so re-run this from a normal terminal." -ForegroundColor Yellow
    }
    else {
        Write-Host "Environment OK." -ForegroundColor Green
    }
    exit 0
}
exit 1
