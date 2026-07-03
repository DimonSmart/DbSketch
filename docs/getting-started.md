# Getting Started

DbSketch is distributed as a .NET tool. The package id is `DimonSmart.DbSketch`; the installed command is `dbsketch`.

Use this guide to create the smallest useful config and generate the first database diagram.

## 1. Create `dbsketch.yml`

Create a `dbsketch.yml` file in your repository root:

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"
```

Only `provider` and `connectionString` are required. With this minimal config, DbSketch generates a Mermaid diagram wrapped in Markdown.

Supported providers:

- `postgres`
- `sqlserver`
- `mysql`

Provider aliases are also supported: `postgresql` maps to `postgres`, and `mssql` maps to `sqlserver`.

Keep the real connection string out of the config file and read it from an environment variable. Wrap the placeholder in quotes because connection strings often contain YAML-sensitive characters.

## 2. Install DbSketch

Install DbSketch as a global .NET tool:

```bash
dotnet tool install --global DimonSmart.DbSketch
```

A global install is the simplest option for the first run. For team repositories and CI, use a local tool manifest later. See [CI and automation](ci.md).

## 3. Set the connection string

On Bash:

```bash
export DB_CONNECTION="Host=localhost;Database=app;Username=app;Password=secret"
```

On PowerShell:

```powershell
$env:DB_CONNECTION = "Host=localhost;Database=app;Username=app;Password=secret"
```

Use the connection string format for your database provider.

## 4. Generate the diagram

```bash
dbsketch generate --config dbsketch.yml
```

## 5. Open the generated file

With the minimal config, DbSketch generates one diagram named `main` and writes it to:

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

Start here, then add options only when the project needs them.

## Next Steps

- Need filters, focused diagrams, custom output paths, or layout settings? See [Configuration](configuration.md).
- Need DOT, Mermaid, Markdown, PNG, or SVG details? See [Renderers](renderers.md).
- Need table and column comments? See [Database comments](comments.md).
- Need CI, pinned local tools, or non-interactive command options? See [CI and automation](ci.md).
