<#
.SYNOPSIS
    Installs RogueBit from a release, on Windows.

.DESCRIPTION
    The releases hold self contained builds, so nothing has to be installed
    first. No .NET, no SDK, no clone, no build.

        irm https://raw.githubusercontent.com/Stiven-Gjekaj/RogueBit/main/scripts/install.ps1 | iex

.PARAMETER Version
    Which release to install. The newest by default.

.PARAMETER To
    Where the game goes. This directory is put on your PATH.

.PARAMETER Uninstall
    Take it away again and stop.
#>

[CmdletBinding()]
param(
    [string] $Version = $env:ROGUEBIT_VERSION,
    [string] $To = $env:ROGUEBIT_HOME,
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repo = 'Stiven-Gjekaj/RogueBit'

if (-not $To) { $To = Join-Path $env:LOCALAPPDATA 'Programs\RogueBit' }

function Fail($message) { Write-Error $message; exit 1 }

# --------------------------------------------------------------- the PATH ---

function Add-ToPath($directory) {
    $current = [Environment]::GetEnvironmentVariable('Path', 'User')
    $parts = @($current -split ';' | Where-Object { $_ })

    if ($parts -contains $directory) { return $false }

    [Environment]::SetEnvironmentVariable('Path', (@($parts) + $directory) -join ';', 'User')
    return $true
}

function Remove-FromPath($directory) {
    $current = [Environment]::GetEnvironmentVariable('Path', 'User')
    $parts = @($current -split ';' | Where-Object { $_ -and $_ -ne $directory })
    [Environment]::SetEnvironmentVariable('Path', $parts -join ';', 'User')
}

# ------------------------------------------------------------ uninstalling ---

if ($Uninstall) {
    if (Test-Path $To) { Remove-Item -Recurse -Force $To }
    Remove-FromPath $To

    Write-Host "Removed $To and took it off your PATH."
    Write-Host 'Saved runs and scores are left alone. They live in'
    Write-Host "  $(Join-Path $env:APPDATA 'RogueBit')"
    exit 0
}

# --------------------------------------------------------------- the build ---

$architecture = $env:PROCESSOR_ARCHITECTURE

if ($architecture -ne 'AMD64') {
    Fail "There is no $architecture build. The releases hold win-x64, linux-x64 and osx-arm64."
}

$asset = 'RogueBit-win-x64.zip'

# ------------------------------------------------------------- the release ---

if (-not $Version) {
    Write-Host 'Looking for the newest release...'

    # Not /releases/latest. That endpoint skips pre-releases, and every RogueBit
    # release so far is one, so it answers 404 here. This asks for the newest
    # release of any kind instead.
    $releases = Invoke-RestMethod "https://api.github.com/repos/$repo/releases?per_page=1"
    $Version = @($releases)[0].tag_name

    if (-not $Version) { Fail 'Could not work out the newest release. Pass one with -Version.' }
}

$base = "https://github.com/$repo/releases/download/$Version"
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("roguebit-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $work | Out-Null

try {
    Write-Host "Fetching RogueBit $Version for win-x64..."

    $archive = Join-Path $work $asset
    Invoke-WebRequest "$base/$asset" -OutFile $archive
    Invoke-WebRequest "$base/SHA256SUMS" -OutFile (Join-Path $work 'SHA256SUMS')

    # ----------------------------------------------------------- the check ---

    $got = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()

    $want = Get-Content (Join-Path $work 'SHA256SUMS') |
        Where-Object { $_ -match "^([0-9a-f]{64})\s+$([regex]::Escape($asset))$" } |
        ForEach-Object { $Matches[1] } |
        Select-Object -First 1

    if (-not $want) { Fail "SHA256SUMS has no line for $asset." }

    if ($got -ne $want) {
        Fail "The download does not match its checksum.`n  wanted $want`n  got    $got`nNothing has been installed."
    }

    Write-Host 'Checksum matches.'

    # -------------------------------------------------------- unpacking ---

    Write-Host "Unpacking into $To..."

    if (Test-Path $To) { Remove-Item -Recurse -Force $To }
    New-Item -ItemType Directory -Path $To -Force | Out-Null
    Expand-Archive -Path $archive -DestinationPath $To -Force

    $program = Join-Path $To 'RogueBit.exe'
    if (-not (Test-Path $program)) { Fail "The archive holds no RogueBit.exe. Nothing has been installed." }

    # Windows marks anything that came from the internet, and refuses to start
    # it without a prompt. The build is not signed, so without this the first
    # run is a dialogue rather than a game.
    Get-ChildItem $To -Recurse -File | Unblock-File -ErrorAction SilentlyContinue
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

# --------------------------------------------------------------- proving it ---

# Starting it is the only proof that the download was the right build for this
# machine. An archive can arrive whole, match its checksum, and still not run.
$help = & $program --help 2>&1

if ($LASTEXITCODE -ne 0) {
    Fail "RogueBit is installed at $To but will not start.`nTry running $program yourself to see what it says."
}

$added = Add-ToPath $To

Write-Host ''
Write-Host "RogueBit $Version is in $To."

if ($added) {
    Write-Host ''
    Write-Host 'That directory has been added to your PATH. Open a new terminal, then:'
    Write-Host '  roguebit'
}
else {
    Write-Host ''
    Write-Host 'Start it with:  roguebit'
}

Write-Host ''
Write-Host 'The game opens a window, so it needs a desktop session.'

# Asked of the build that was installed rather than assumed. Older releases
# have no such option, and pointing somebody at one is worse than saying
# nothing.
if ($help -match '--print-floor') {
    Write-Host 'To see it work without opening one, print a floor as text:'
    Write-Host "  roguebit --print-floor --seed 4242 --depth 3"
}

Write-Host ''
Write-Host 'To remove it again, run this script with -Uninstall.'
