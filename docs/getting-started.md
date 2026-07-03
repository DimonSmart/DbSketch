# Getting Started

DbSketch is distributed as a .NET tool. The package id is `DimonSmart.DbSketch`; the installed command is `dbsketch`.

Install the .NET 10 SDK before installing or running the tool.

Use this guide to create the smallest useful config and generate the first database diagram.

## 1. Create `dbsketch.yml`

Create a `dbsketch.yml` file in your repository root:

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"
```

Only `provider` and `connectionString` are required. With this minimal config, DbSketch generates a Mermaid diagram wrapped in Markdown.

Set `DB_CONNECTION` to the connection string for your database provider. Wrap the placeholder in quotes because connection strings often contain YAML-sensitive characters.

Supported providers:

| Provider | More examples |
| --- | --- |
| `postgres` | [PostgreSQL](https://www.connectionstrings.com/postgresql/) |
| `sqlserver` | [SQL Server](https://www.connectionstrings.com/sql-server/) |
| `mysql` | [MySQL](https://www.connectionstrings.com/mysql/) |

## 2. Install DbSketch

Install DbSketch as a global .NET tool:

```bash
dotnet tool install --global DimonSmart.DbSketch
```

For team repositories, CI, or pinned tool versions, use a local tool manifest instead. See [CI and automation](ci.md).

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

## Next Steps

- Want an AI assistant to add DbSketch to an existing repository? See [AI-assisted setup](ai-setup.md).
- Need a complete config example? See [Full config example](examples/full-config.md).
- Need filters, focused diagrams, custom output paths, or layout settings? See [Configuration](configuration.md).
- Need DOT, Mermaid, Markdown, PNG, or SVG details? See [Renderers](renderers.md).
- Need table and column comments? See [Database comments](comments.md).
- Need CI, pinned local tools, secrets, or non-interactive command options? See [CI and automation](ci.md).
