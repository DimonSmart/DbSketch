param(
    [string] $Remote = "origin",
    [string] $Branch = "main",
    [string] $TagPrefix = "v",
    [string] $FirstVersion = "0.1.0",
    [ValidateSet("patch", "minor", "major")]
    [string] $Bump = "patch",
    [string] $Version
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Git {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        $command = "git " + ($Arguments -join " ")
        $message = ($output | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($message)) {
            $message = "exit code $exitCode"
        }

        throw "$command failed: $message"
    }

    if ($null -eq $output) {
        return @()
    }

    return @($output | ForEach-Object { $_.ToString() } | Where-Object { $_ -ne "" })
}

function Test-Version {
    param([string] $Value)

    return $Value -match '^\d+\.\d+\.\d+$'
}

function Get-NextVersion {
    param(
        [string] $CurrentVersion,
        [string] $BumpKind
    )

    $parts = $CurrentVersion.Split(".") | ForEach-Object { [int] $_ }
    switch ($BumpKind) {
        "patch" { $parts[2]++ }
        "minor" {
            $parts[1]++
            $parts[2] = 0
        }
        "major" {
            $parts[0]++
            $parts[1] = 0
            $parts[2] = 0
        }
    }

    return "$($parts[0]).$($parts[1]).$($parts[2])"
}

if ($TagPrefix -notmatch '^[A-Za-z0-9._-]*$') {
    throw "TagPrefix contains unsupported characters."
}

if (-not (Test-Version $FirstVersion)) {
    throw "FirstVersion must use MAJOR.MINOR.PATCH format."
}

$repoRoot = (Invoke-Git "rev-parse" "--show-toplevel" | Select-Object -First 1).ToString().Trim()
Set-Location $repoRoot

$status = @(Invoke-Git "status" "--porcelain")
if ($status.Count -gt 0) {
    throw "Working tree is not clean. Commit or stash changes before publishing a version tag."
}

Invoke-Git "fetch" "--prune" "--tags" $Remote | Out-Null
Invoke-Git "switch" $Branch | Out-Null
Invoke-Git "pull" "--ff-only" $Remote $Branch | Out-Null

$escapedPrefix = [regex]::Escape($TagPrefix)
$tagPattern = "^$escapedPrefix\d+\.\d+\.\d+$"
$allTags = @(Invoke-Git "tag" "--list" "$TagPrefix*.*.*" "--sort=-v:refname")
$previousTag = $allTags | Where-Object { $_ -match $tagPattern } | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ([string]::IsNullOrWhiteSpace($previousTag)) {
        $Version = $FirstVersion
    }
    else {
        $previousVersion = $previousTag.Substring($TagPrefix.Length)
        $Version = Get-NextVersion $previousVersion $Bump
    }
}
elseif (-not (Test-Version $Version)) {
    throw "Version must use MAJOR.MINOR.PATCH format."
}

$nextTag = "$TagPrefix$Version"
$existingTag = @(Invoke-Git "tag" "--list" $nextTag)
if ($existingTag.Count -gt 0) {
    throw "Tag '$nextTag' already exists."
}

Invoke-Git "tag" "-a" $nextTag "-m" "Release $nextTag" | Out-Null
Invoke-Git "push" $Remote $Branch $nextTag | Out-Null

Write-Host "Previous tag: $(if ([string]::IsNullOrWhiteSpace($previousTag)) { '<none>' } else { $previousTag })"
Write-Host "Next tag: $nextTag"
Write-Host "Branch: $Branch"
Write-Host "Remote: $Remote"
Write-Host "Published $nextTag."
