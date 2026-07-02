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

function Resolve-GraphvizDot {
    $command = Get-Command "dot" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $knownPaths = @(
        "C:\Program Files\Graphviz\bin\dot.exe",
        "C:\Program Files (x86)\Graphviz\bin\dot.exe"
    )

    foreach ($path in $knownPaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "Required tool 'dot' was not found. Install Graphviz and make sure 'dot' is on PATH, or rerun with -SkipPng."
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
    $dotPath = Resolve-GraphvizDot
}

& docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker is installed, but the Docker daemon is not reachable. Start Docker Desktop or Docker Engine and rerun the script."
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
    Get-Content -Raw $fixturePath | docker exec -i $containerName psql -h 127.0.0.1 -U dbsketch -d dbsketch_northwind
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
        Write-Host "Rendering docs/assets/northwind-schema-compact.png with Graphviz..."
        Invoke-Checked $dotPath @("-Tpng", "docs/examples/northwind.compact.dot", "-o", "docs/assets/northwind-schema-compact.png")

        Write-Host "Rendering docs/assets/northwind-schema-full.png with Graphviz..."
        Invoke-Checked $dotPath @("-Tpng", "docs/examples/northwind.full.dot", "-o", "docs/assets/northwind-schema-full.png")

        Write-Host "Rendering docs/assets/northwind-schema.png with Graphviz..."
        Invoke-Checked $dotPath @("-Tpng", "docs/examples/northwind.compact.dot", "-o", "docs/assets/northwind-schema.png")

        $styleNames = @("classic", "readable", "compact", "soft", "blueprint", "contrast")
        foreach ($styleName in $styleNames) {
            Write-Host "Rendering docs/assets/northwind-style-$styleName.png with Graphviz..."
            Invoke-Checked $dotPath @("-Tpng", "docs/examples/northwind.style.$styleName.dot", "-o", "docs/assets/northwind-style-$styleName.png")
        }
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
        try {
            & docker rm -f $containerName *> $null
        }
        catch {
            Write-Warning "Failed to remove PostgreSQL container '$containerName'. Remove it manually if it exists."
        }
    }
}
