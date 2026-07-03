# Getting Started

DbSketch is distributed as a .NET tool. The package id is `DimonSmart.DbSketch`; the installed command is `dbsketch`.

This page starts with the shortest path from an empty repository to the first generated database diagram. Advanced installation, CI, and command options are listed after the first-run flow.

## Quick Start

### 1. Create `dbsketch.yml`

Create a `dbsketch.yml` file in your repository root:

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"
```

Only `provider` and `connectionString` are required. Almost all other settings have defaults.

Supported providers:

- `postgres`
- `sqlserver`
- `mysql`

Provider aliases are also supported: `postgresql` maps to `postgres`, and `mssql` maps to `sqlserver`.

Keep the real connection string out of the config file and read it from an environment variable. Wrap the placeholder in quotes because connection strings often contain YAML-sensitive characters.

### 2. Install DbSketch

Install DbSketch as a global .NET tool:

```bash
dotnet tool install --global DimonSmart.DbSketch
```

### 3. Set the connection string

On Bash:

```bash
export DB_CONNECTION="Host=localhost;Database=app;Username=app;Password=secret"
```

On PowerShell:

```powershell
$env:DB_CONNECTION = "Host=localhost;Database=app;Username=app;Password=secret"
```

Use the connection string format for your database provider.

### 4. Generate the diagram

```bash
dbsketch generate --config dbsketch.yml
```

### 5. Check the output

With the minimal config, DbSketch generates one Mermaid diagram named `main`, wraps it in Markdown, and writes it to:

```text
docs/db/schema.md
```

The generated file can be committed with the rest of your documentation.

## What the Minimal Config Does

The minimal config uses these defaults:

| Setting | Default |
| --- | --- |
| Diagram renderer | `mermaid` |
| Output format | `markdown` |
| Diagram name | `main` |
| Output path | `docs/db/schema.md` |

Start with this config, then add filters, multiple diagrams, comments, layout settings, DOT output, or CI integration only when the project needs them.

For the full YAML reference, see [Configuration](configuration.md).

## Local Tool

Use a local tool when you want every contributor and CI run to use the repository-pinned version.

```bash
dotnet new tool-manifest
dotnet tool install DimonSmart.DbSketch
dotnet tool run dbsketch -- generate --config dbsketch.yml
```

Other contributors can restore the pinned tool version with:

```bash
dotnet tool restore
```

## One-Shot Run

Use a one-shot run when you want to try DbSketch without installing it globally or adding it to the repository tool manifest.

```bash
dotnet tool exec DimonSmart.DbSketch -- generate --config dbsketch.yml
```

With .NET 10, `dnx` can also run the tool:

```bash
dnx DimonSmart.DbSketch -- generate --config dbsketch.yml
```

## CI Example

A typical CI job restores the local tool version, reads the connection string from a secret, and regenerates the schema documentation.

```yaml
- name: Restore local tools
  run: dotnet tool restore

- name: Generate DB schema diagrams
  env:
    DB_CONNECTION: ${{ secrets.DB_CONNECTION }}
  run: dotnet tool run dbsketch -- generate --config dbsketch.yml
```

By default, the generated file is written to `docs/db/schema.md`. Explicit diagrams can override `output.path`; keep generated paths under a docs folder when you want schema diagrams to be reviewed with normal documentation changes.

## Useful Commands

```bash
dbsketch
dbsketch --help
dbsketch generate --help
dbsketch generate --config dbsketch.yml
dbsketch generate --config dbsketch.yml --diagram auth
dbsketch generate --config dbsketch.yml --dry-run
dbsketch generate --config dbsketch.yml --quiet
dbsketch generate --config dbsketch.yml --no-progress
dbsketch generate --config dbsketch.yml --verbose
```

`--config` is required for `generate`.
Use `--diagram <name>` to generate one named diagram. If `diagrams` is omitted, the available diagram is `main`.
Use `--dry-run` to read the schema, apply comments and filters, print table and foreign-key counts, and skip file writes.
Use `--quiet` to suppress all non-error output.
Use `--no-progress` to suppress progress messages while keeping warnings.
Use `--verbose` for diagnostic output.
