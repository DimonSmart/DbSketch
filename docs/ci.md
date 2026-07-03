# CI and Automation

Use this page when DbSketch becomes part of the repository workflow: shared tool versions, CI verification, or automated schema documentation refreshes.

For the first local run, start with [Getting started](getting-started.md).

## Pin the tool version

For a team repository or CI, prefer a local .NET tool manifest. It keeps every contributor and CI run on the same DbSketch version.

```bash
dotnet new tool-manifest
dotnet tool install DimonSmart.DbSketch
```

Commit the generated `.config/dotnet-tools.json` file.

Run DbSketch through the local tool manifest:

```bash
dotnet tool run dbsketch -- generate --config dbsketch.yml
```

Other contributors and CI can restore the pinned tool version with:

```bash
dotnet tool restore
```

## Use an environment variable for the connection string

For committed configs, keep the real connection string out of `dbsketch.yml` and read it from an environment variable:

```yaml
provider: sqlserver
connectionString: "${DB_CONNECTION}"
```

Wrap the placeholder in quotes because connection strings often contain YAML-sensitive characters.

For a local run with this config, set the variable before running DbSketch.

On Bash:

```bash
export DB_CONNECTION="Server=localhost;Database=app;User Id=app;Password=secret;TrustServerCertificate=True"
```

On PowerShell:

```powershell
$env:DB_CONNECTION = "Server=localhost;Database=app;User Id=app;Password=secret;TrustServerCertificate=True"
```

## GitHub Actions example

This workflow restores the local tool version, reads the database connection string from a GitHub secret, and verifies that schema documentation can be generated.

```yaml
name: Generate database docs

on:
  workflow_dispatch:
  pull_request:
    paths:
      - dbsketch.yml
      - .config/dotnet-tools.json
      - .github/workflows/dbsketch.yml

jobs:
  dbsketch:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore local tools
        run: dotnet tool restore

      - name: Generate database schema docs
        env:
          DB_CONNECTION: ${{ secrets.DB_CONNECTION }}
        run: dotnet tool run dbsketch -- generate --config dbsketch.yml
```

By default, DbSketch writes the generated Markdown file to `docs/db/schema.md`. Explicit diagrams can override `output.path`; keep generated files under a documentation folder when you want them reviewed with normal documentation changes.

## CI behavior

The example above validates generation, but it does not push generated files back to the repository.

For pull requests, this is usually the safest default: CI checks whether schema documentation can be generated, and the developer commits the updated files explicitly.

For scheduled documentation refreshes, use a separate workflow with write permissions and a trusted connection string. Avoid exposing production database secrets to workflows that run untrusted pull request code.

## Useful CI options

```bash
dotnet tool run dbsketch -- generate --config dbsketch.yml --diagram auth
dotnet tool run dbsketch -- generate --config dbsketch.yml --dry-run
dotnet tool run dbsketch -- generate --config dbsketch.yml --quiet
dotnet tool run dbsketch -- generate --config dbsketch.yml --no-progress
```

Use `--diagram <name>` to generate one named diagram. If `diagrams` is omitted, the available diagram is `main`.

Use `--dry-run` to read the schema, apply comments and filters, print table and foreign-key counts, and skip file writes.

Use `--quiet` to suppress all non-error output.

Use `--no-progress` to suppress progress messages while keeping warnings.

## One-shot run

For temporary local experiments, DbSketch can be run without a global install or a repository tool manifest:

```bash
dotnet tool exec DimonSmart.DbSketch -- generate --config dbsketch.yml
```

Use this for quick experiments only. For repeatable project setup, prefer a local tool manifest.
