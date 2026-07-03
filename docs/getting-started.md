# Getting Started

DbSketch is distributed as a .NET tool. The package id is `DimonSmart.DbSketch`; the installed command is `dbsketch`.

Use this guide to create the smallest useful config and generate the first database diagram.

## 1. Create `dbsketch.yml`

Create a `dbsketch.yml` file in your repository root:

```yaml
provider: postgres
connectionString: "Host=localhost;Database=app;Username=app;Password=<password>"
```

Only `provider` and `connectionString` are required. With this minimal config, DbSketch generates a Mermaid diagram wrapped in Markdown.

Supported providers:

- `postgres`
- `sqlserver`
- `mysql`

Use the connection string format for your database provider. Wrap the connection string in quotes because connection strings often contain YAML-sensitive characters.

## 2. Install DbSketch

Install DbSketch as a global .NET tool:

```bash
dotnet tool install --global DimonSmart.DbSketch
```

If `dnx` is available, you can also run DbSketch without installing it globally:

```bash
dnx DimonSmart.DbSketch -- generate --config dbsketch.yml
```

## 3. Generate the diagram

```bash
dbsketch generate --config dbsketch.yml
```

## 4. Open the generated file

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
- Need CI, pinned local tools, secrets, or non-interactive command options? See [CI and automation](ci.md).
