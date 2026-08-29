<#
.SYNOPSIS
    Releases Metacraft.VcsHardware: bumps the version, tags, pushes, and runs
    the publish workflow, streaming its output.

.DESCRIPTION
    VcsHardware has no release branches, so this is a simpler cousin of
    SimSuite's Build/New-Release.ps1:
      1. Prompts for a version number (or takes it as an argument).
      2. Bumps <Version> in Source/Metacraft.VcsHardware.csproj.
      3. Commits with "Prepare release v<version>.".
      4. Tags master v<version> (lightweight).
      5. Pushes master and the tag to origin (after confirmation).
      6. Dispatches the publish workflow against that tag and watches it.

    The version is typed once and written to both the csproj and the tag, so
    they cannot disagree. The workflow re-checks that they match before it
    builds or signs anything.

.PARAMETER Version
    The version to release, e.g. 2.0.1. If omitted, you are prompted.

.PARAMETER Yes
    Skip the confirmation prompt before pushing to origin.

.PARAMETER NoWatch
    Push and dispatch, but don't wait for the workflow to finish.

.EXAMPLE
    .\Release.ps1
    .\Release.ps1 2.0.1
    .\Release.ps1 2.0.1 -Yes
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version,

    [switch]$Yes,

    [switch]$NoWatch
)

$ErrorActionPreference = 'Stop'

$Workflow = 'publish-nuget.yml'
$Branch   = 'master'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Invoke-Git {
    # Runs git, streaming its output, and throws if it fails.
    $gitArgs = $args
    & git @gitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($gitArgs -join ' ') failed (exit code $LASTEXITCODE)."
    }
}

function Get-GitValue {
    # Runs git and returns trimmed stdout (for read-only queries).
    $gitArgs = $args
    $out = & git @gitArgs
    return ($out | Out-String).Trim()
}

function Write-Step($message) {
    Write-Host ""
    Write-Host ">> $message" -ForegroundColor Cyan
}

function Invoke-Capture {
    # Runs a native command, capturing stdout and stderr together as a string.
    # Windows PowerShell turns redirected native stderr into ErrorRecords, which
    # would abort the script under $ErrorActionPreference = 'Stop' even on
    # success, so relax it here and report failure via the exit code instead.
    param([string]$FilePath, [string[]]$Arguments)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $FilePath @Arguments 2>&1 | Out-String
        return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
    } finally {
        $ErrorActionPreference = $previous
    }
}

function Get-GhPath {
    # winget installs gh but existing shells won't have it on PATH until they
    # are restarted, so fall back to the known install location.
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $fallback = Join-Path $env:ProgramFiles 'GitHub CLI\gh.exe'
    if (Test-Path $fallback) { return $fallback }

    throw "GitHub CLI (gh) not found. Install it with: winget install --id GitHub.cli"
}

# ---------------------------------------------------------------------------
# Locate the repo and version file
# ---------------------------------------------------------------------------
$RepoRoot = Get-GitValue rev-parse --show-toplevel
if (-not $RepoRoot) { throw "Not inside a git repository." }
Set-Location $RepoRoot

$VersionFile = Join-Path $RepoRoot 'Source/Metacraft.VcsHardware.csproj'
if (-not (Test-Path $VersionFile)) {
    throw "Version file not found: $VersionFile"
}

$Gh = Get-GhPath

# ---------------------------------------------------------------------------
# Prompt for / validate the version
# ---------------------------------------------------------------------------
# NOTE: read with ReadAllText (UTF-8), never Get-Content: in Windows PowerShell
# Get-Content decodes BOM-less files as ANSI, which would mangle any non-ASCII
# characters in the file a little further on every release.
$VersionRegex   = [regex]'<Version>([^<]+)</Version>'
$CurrentVersion = $VersionRegex.Match([System.IO.File]::ReadAllText($VersionFile)).Groups[1].Value
if (-not $CurrentVersion) {
    throw "Could not find a <Version> element in $VersionFile."
}
Write-Host "Current version: $CurrentVersion" -ForegroundColor DarkGray

if (-not $Version) {
    $Version = Read-Host "Enter the new version (e.g. 2.0.1)"
}
$Version = $Version.Trim().TrimStart('v', 'V')

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version '$Version' is not in the expected #.#.# format."
}
if ($Version -eq $CurrentVersion) {
    throw "Version $Version is the same as the current version."
}

$Tag = "v$Version"

# ---------------------------------------------------------------------------
# Pre-flight safety checks
# ---------------------------------------------------------------------------
Write-Step "Pre-flight checks"

# Must be on the release branch.
$CurrentBranch = Get-GitValue rev-parse --abbrev-ref HEAD
if ($CurrentBranch -ne $Branch) {
    throw "On branch '$CurrentBranch'. Switch to '$Branch' before releasing."
}

# Working tree must be clean.
if (Get-GitValue status --porcelain) {
    throw "Working tree is not clean. Commit or stash your changes first."
}

# The tag must not already exist, locally or on origin. NuGet versions are
# immutable, so a recycled tag is almost always a mistake.
if (Get-GitValue tag --list $Tag) {
    throw "Tag $Tag already exists locally. Delete it first: git tag -d $Tag"
}

Invoke-Git fetch origin --quiet
if (Get-GitValue ls-remote --tags origin $Tag) {
    throw "Tag $Tag already exists on origin."
}

# Warn if the local branch is behind origin (avoids surprises at push).
$behind = Get-GitValue rev-list --count "$Branch..origin/$Branch"
if ($behind -and [int]$behind -gt 0) {
    throw "Local '$Branch' is $behind commit(s) behind origin/$Branch. Pull first, then re-run."
}

# Confirm gh is usable before we start making commits.
if ((Invoke-Capture $Gh @('auth', 'status')).ExitCode -ne 0) {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

Write-Host "OK." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Bump the version, commit, tag
# ---------------------------------------------------------------------------
Write-Step "Bumping version to $Version in Source/Metacraft.VcsHardware.csproj"
$content = [System.IO.File]::ReadAllText($VersionFile)
$updated = $VersionRegex.Replace($content, "<Version>$Version</Version>", 1)
# Write back as UTF-8 without a BOM to match the existing file.
[System.IO.File]::WriteAllText($VersionFile, $updated, (New-Object System.Text.UTF8Encoding($false)))

Invoke-Git add -- $VersionFile
Invoke-Git commit -m "Prepare release v$Version."

Write-Step "Tagging $Tag"
Invoke-Git tag $Tag

# ---------------------------------------------------------------------------
# Push (the first irreversible, outward step)
# ---------------------------------------------------------------------------
Write-Step "Ready to push to origin and publish"
Write-Host "  - $Branch"
Write-Host "  - tag $Tag"
Write-Host "  - then dispatch $Workflow against $Tag"

if (-not $Yes) {
    $answer = Read-Host "Push and publish now? [Y/n]"
    if ($answer -and $answer -notmatch '^(y|yes)$') {
        Write-Host ""
        Write-Host "Skipped. The release is prepared locally. To finish later:" -ForegroundColor Yellow
        Write-Host "    git push origin $Branch"
        Write-Host "    git push origin $Tag"
        Write-Host "    gh workflow run $Workflow --ref $Tag"
        return
    }
}

Invoke-Git push origin $Branch
Invoke-Git push origin $Tag

# ---------------------------------------------------------------------------
# Dispatch the publish workflow and watch it
# ---------------------------------------------------------------------------
Write-Step "Dispatching $Workflow against $Tag"
$dispatch = Invoke-Capture $Gh @('workflow', 'run', $Workflow, '--ref', $Tag)
if ($dispatch.ExitCode -ne 0) {
    throw "Failed to dispatch the workflow:`n$($dispatch.Output)"
}
$dispatchOutput = $dispatch.Output
Write-Host $dispatchOutput.Trim()

if ($NoWatch) {
    Write-Host ""
    Write-Host "Dispatched. Not watching (-NoWatch)." -ForegroundColor Yellow
    return
}

# gh usually prints the created run's URL, which saves us hunting for it. When
# it doesn't, fall back to matching the run by its title, which the workflow
# sets from the ref via run-name.
$runId = $null
if ($dispatchOutput -match 'actions/runs/(\d+)') {
    $runId = $Matches[1]
} else {
    Write-Host "No run URL returned; looking up the run by title..." -ForegroundColor DarkGray
    $deadline = (Get-Date).AddSeconds(60)
    while (-not $runId -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $runId = & $Gh run list --workflow=$Workflow --json databaseId,displayTitle |
                 ConvertFrom-Json |
                 Where-Object { $_.displayTitle -eq "Release $Tag" } |
                 Select-Object -First 1 -ExpandProperty databaseId
    }
    if (-not $runId) {
        throw "Could not find the workflow run. Check: gh run list --workflow=$Workflow"
    }
}

Write-Step "Watching run $runId"
& $Gh run watch $runId --exit-status
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "The publish workflow failed. Failed step log:" -ForegroundColor Red
    Write-Host "    gh run view $runId --log-failed" -ForegroundColor Red
    throw "Publish failed for v$Version."
}

Write-Host ""
Write-Host "Release v$Version published." -ForegroundColor Green
