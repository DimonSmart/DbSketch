# Getting Started

DbSketch is distributed as a .NET tool. The package id is `DimonSmart.DbSketch`; the installed command is `dbsketch`.

## Global Tool

```bash
dotnet tool install --global DimonSmart.DbSketch
dbsketch generate --config dbsketch.yml
```

## Local Tool

Use a local tool when you want every contributor and CI run to use the repository-pinned version.

```bash
dotnet new tool-manifest
dotnet tool install DimonSmart.DbSketch
dotnet tool restore
dotnet tool run dbsketch -- generate --config dbsketch.yml
```

## One-Shot Run

```bash
dotnet tool exec DimonSmart.DbSketch -- generate --config dbsketch.yml
```

With .NET 10, `dnx` can also run the tool:

```bash
dnx DimonSmart.DbSketch -- generate --config dbsketch.yml
```

## Connection String

Keep the connection string out of the config file and read it from the environment:

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"
```

Then run:

```bash
DB_CONNECTION="Host=localhost;Database=app;Username=app;Password=secret" dbsketch generate --config dbsketch.yml
```

On PowerShell:

```powershell
$env:DB_CONNECTION = "Host=localhost;Database=app;Username=app;Password=secret"
dbsketch generate --config dbsketch.yml
```

## CI Example

```yaml
- name: Restore local tools
  run: dotnet tool restore

- name: Generate DB schema diagrams
  env:
    DB_CONNECTION: ${{ secrets.DB_CONNECTION }}
  run: dotnet tool run dbsketch -- generate --config dbsketch.yml
```

Generated files are written to each diagram's `output.path`. Put those paths under a docs folder, such as `docs/db/schema.md`, when you want schema diagrams to be reviewed with normal documentation changes.

## Useful Commands

```bash
dbsketch
dbsketch --help
dbsketch generate --help
dbsketch generate --config dbsketch.yml
dbsketch generate --config dbsketch.yml --diagram auth
dbsketch generate --config dbsketch.yml --dry-run
dbsketch generate --config dbsketch.yml --quiet
dbsketch generate --config dbsketch.yml --verbose
```

`--config` is required for `generate`.
Use `--diagram <name>` to generate one named diagram from `diagrams`.
Use `--dry-run` to read the schema, apply comments and filters, print table and foreign-key counts, and skip file writes.
Use `--quiet` to suppress all non-error output.
Use `--no-progress` to suppress progress messages while keeping warnings.
Use `--verbose` for diagnostic output.
