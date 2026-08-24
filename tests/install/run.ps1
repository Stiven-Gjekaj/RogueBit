<#
.SYNOPSIS
    Checks how scripts/install.ps1 works out which release is the newest.

.DESCRIPTION
    The companion of run.sh, for the same reason: the install workflow only
    ever sees the path where the API answers, so the fallback would first run
    on somebody's machine on the day the API was down.

    The functions are lifted out of the shipped script rather than copied here.
    A test that holds its own copy of the thing it tests passes whatever the
    shipped copy does.
#>

# No Set-StrictMode here on purpose. The functions run without it in the
# installer, and a test that ran them under stricter rules than the shipped
# script uses would fail on things the shipped script is fine with.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$here = Join-Path $root 'tests/install'
$script = Join-Path $root 'scripts/install.ps1'

$source = Get-Content -Raw $script
$wanted = @('Read-Retrying', 'Get-NewestTagFromFeed')
$found = 0

foreach ($name in $wanted) {
    # From the line that opens the function to the line that closes it at the
    # left margin. Finding nothing would otherwise be silence, and silence
    # here is a test that ran against a function that no longer exists.
    $match = [regex]::Match($source, "(?ms)^function $name\(.*?^\}")
    if (-not $match.Success) { throw "Could not find $name in $script. Was it renamed?" }

    . ([scriptblock]::Create($match.Value))
    $found++
}

if ($found -ne $wanted.Count) { throw "Found $found of $($wanted.Count) functions." }

$failures = 0

function Check($what, $got, $wanted) {
    if ($got -eq $wanted) {
        Write-Host "ok   $what"
    } else {
        Write-Host "FAIL $what : wanted [$wanted], got [$got]"
        $script:failures++
    }
}

# ------------------------------------------------------------- the parsing ---

[xml] $feed = Get-Content -Raw (Join-Path $here 'releases.atom')

# The newest entry in the fixture is deliberately named something other than
# its tag, because a release name is free text and the tag is not.
Check 'the feed names the newest release' (Get-NewestTagFromFeed $feed) 'v0.3.0-alpha'

[xml] $empty = '<feed xmlns="http://www.w3.org/2005/Atom"><title>none yet</title></feed>'
Check 'a feed with no releases in it names none' (Get-NewestTagFromFeed $empty) $null

# -------------------------------------------------------------- the asking ---

$counter = Join-Path ([System.IO.Path]::GetTempPath()) ("roguebit-tries-" + [guid]::NewGuid())
Set-Content -Path $counter -Value '0'

function Bump {
    $n = [int] (Get-Content -Raw $counter) + 1
    Set-Content -Path $counter -Value $n
    return $n
}

Check 'a refusal every time gives back nothing' `
    (Read-Retrying { Bump | Out-Null; throw 'no' }) $null
Check 'and it asked three times before giving up' `
    ([int] (Get-Content -Raw $counter)) 3

Set-Content -Path $counter -Value '0'
Check 'a refusal that clears up is waited out' `
    (Read-Retrying { if ((Bump) -lt 3) { throw 'no' }; 'v0.3.0-alpha' }) 'v0.3.0-alpha'

Remove-Item $counter -ErrorAction SilentlyContinue

# ------------------------------------------------------------------------------

if ($failures -ne 0) { throw "$failures check(s) failed" }
Write-Host ''
Write-Host 'all good'
