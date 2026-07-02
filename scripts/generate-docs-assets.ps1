param(
    [switch] $SkipPng,
    [switch] $KeepContainer
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Require-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string] $InstallHint
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$Name' was not found. $InstallHint"
    }
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

$repoRoot = Resolve-RepoRoot
$containerName = "dbsketch-docs-northwind-$([Guid]::NewGuid().ToString('N'))"
$fixturePath = Join-Path $repoRoot "tests/DbSketch.Tests/TestData/Northwind/postgres-northwind-schema.sql"
$assetsDirectory = Join-Path $repoRoot "docs/assets"

Require-Command "dotnet" "Install the .NET SDK and make sure 'dotnet' is on PATH."
Require-Command "docker" "Install Docker Desktop or Docker Engine and make sure 'docker' is on PATH."
if (-not $SkipPng) {
    Require-Command "dot" "Install Graphviz and make sure the 'dot' CLI is on PATH, or rerun with -SkipPng."
}

if (-not (Test-Path $fixturePath)) {
    throw "Northwind fixture was not found: $fixturePath"
}

New-Item -ItemType Directory -Force $assetsDirectory | Out-Null

Push-Location $repoRoot
try {
    Write-Host "Starting PostgreSQL container $containerName..."
    $containerId = (& docker run --name $containerName `
        -e POSTGRES_DB=dbsketch_northwind `
        -e POSTGRES_USER=dbsketch `
        -e POSTGRES_PASSWORD=dbsketch `
        -p 127.0.0.1::5432 `
        -d postgres:16-alpine).Trim()

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
        throw "Failed to start PostgreSQL container. Check that Docker is running."
    }

    Write-Host "Waiting for PostgreSQL to become ready..."
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        & docker exec $containerName pg_isready -U dbsketch -d dbsketch_northwind *> $null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not $ready) {
        throw "PostgreSQL did not become ready within 60 seconds."
    }

    Write-Host "Applying Northwind schema fixture..."
    Get-Content -Raw $fixturePath | docker exec -i $containerName psql -U dbsketch -d dbsketch_northwind
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to apply Northwind schema fixture."
    }

    $portMapping = (& docker port $containerName 5432/tcp).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($portMapping)) {
        throw "Failed to resolve PostgreSQL host port."
    }

    $hostPort = ($portMapping -split ":")[-1]
    if ([string]::IsNullOrWhiteSpace($hostPort)) {
        throw "Failed to parse PostgreSQL host port from Docker mapping: $portMapping"
    }

    $env:DBSKETCH_DOCS_CONNECTION = "Host=127.0.0.1;Port=$hostPort;Database=dbsketch_northwind;Username=dbsketch;Password=dbsketch"

    Write-Host "Generating DOT, Mermaid, and Markdown examples..."
    Invoke-Checked "dotnet" @("run", "--project", "src/DbSketch.Cli/DbSketch.Cli.csproj", "--", "generate", "--config", "docs/examples/northwind.dbsketch.yml")

    if (-not $SkipPng) {
        Write-Host "Rendering docs/assets/northwind-schema.png with Graphviz..."
        Invoke-Checked "dot" @("-Tpng", "docs/examples/northwind.dot", "-o", "docs/assets/northwind-schema.png")
    }

    Write-Host "Docs assets generated."
}
finally {
    Pop-Location

    if ($KeepContainer) {
        Write-Host "Keeping PostgreSQL container for debugging: $containerName"
    }
    else {
        Write-Host "Removing PostgreSQL container $containerName..."
        & docker rm -f $containerName *> $null
    }
}
