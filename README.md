# DbSketch

DbSketch is a small C# CLI tool that reads a live database schema and writes a compact database structure diagram.

The MVP supports SQL Server, PostgreSQL, and MySQL. It reads schemas/namespaces, tables, columns, primary key markers, and real foreign key relationships, then applies include/exclude table filters before rendering DOT or Mermaid.

## Install

### Global tool

```bash
dotnet tool install --global DimonSmart.DbSketch
dbsketch generate --config dbsketch.yml
```

### Local tool in repository

```bash
dotnet new tool-manifest
dotnet tool install DimonSmart.DbSketch --version 0.1.0
dotnet tool restore
dotnet tool run dbsketch -- generate --config dbsketch.yml
```

### One-shot run

```bash
dotnet tool exec DimonSmart.DbSketch@0.1.0 -- generate --config dbsketch.yml
```

With .NET 10, `dnx` can also run the tool:

```bash
dnx DimonSmart.DbSketch@0.1.0 -- generate --config dbsketch.yml
```

### CI example

```yaml
- name: Restore local tools
  run: dotnet tool restore

- name: Generate DB schema diagram
  env:
    DB_CONNECTION: ${{ secrets.DB_CONNECTION }}
  run: dotnet tool run dbsketch -- generate --config dbsketch.yml
```

## Development

Build and test:

```bash
dotnet restore DbSketch.sln
dotnet build DbSketch.sln
dotnet test DbSketch.sln
dotnet pack src/DbSketch.Cli/DbSketch.Cli.csproj -c Release
```

The .NET tool package id is `DimonSmart.DbSketch`; the installed command remains `dbsketch`.

### Git hooks

This repository uses Husky.Net for local Git hooks.

After cloning the repository, run:

```bash
dotnet restore DbSketch.sln
```

The restore step installs local .NET tools and configures Git hooks automatically.

The pre-commit hook formats staged .NET files with `dotnet format` and re-stages only files that were already staged before the hook started.

To skip hooks for a single commit:

```bash
git commit --no-verify
```

To disable Husky installation in CI or special local environments:

```bash
HUSKY=0 dotnet restore DbSketch.sln
```

Direct CLI options can override config values:

```bash
dbsketch generate --provider sqlserver --connection "Server=.;Database=AppDb;Trusted_Connection=True;TrustServerCertificate=True" --renderer dot --format raw --out docs/db/schema.dot
```

## Config

```yaml
provider: sqlserver
connectionString: ${DB_CONNECTION}

include:
  tables:
    - "dbo.*"

exclude:
  tables:
    - "dbo.__EFMigrationsHistory"
    - "dbo.Log_*"

output:
  path: docs/db/schema.md
  format: markdown

diagram:
  renderer: mermaid
  title: "Database schema"
  direction: LR
  compact: true

  mermaid:
    emitDirection: false

  show:
    schemaName: true
    columnTypes: false
    nullability: false
    primaryKeys: true
    foreignKeys: true
    comments: false

comments:
  enabled: true
```

Provider aliases: `mssql` maps to `sqlserver`, and `postgresql` maps to `postgres`.

When `comments.enabled` is true, DbSketch reads database-native table and column comments into the internal schema model.

Current providers:

- SQL Server: `MS_Description` extended properties.
- PostgreSQL: `COMMENT ON TABLE` / `COMMENT ON COLUMN`.
- MySQL: `TABLE_COMMENT` / `COLUMN_COMMENT` from `information_schema`.

Comments are read when `comments.enabled: true` and rendered only when `diagram.show.comments: true`. Mermaid currently renders column comments. DOT renders table and column comments. Comments are disabled by default to keep diagrams compact.

```yaml
comments:
  enabled: true

diagram:
  show:
    comments: true
```

## Diagram And Output Formats

Supported values for `diagram.renderer` and `--renderer`:

- `dot`: Graphviz DOT renderer.
- `mermaid`: Mermaid ER renderer.

Supported values for `output.format` and `--format`:

- `raw`: write only diagram text.
- `markdown`: wrap diagram text in a fenced Markdown code block.

When `output.format: markdown` and `output.markdownFenceLanguage` is not set, DbSketch uses `mermaid` for the Mermaid renderer and `dot` for the DOT renderer.

`diagram.direction` stores the desired diagram direction.

For Mermaid ER diagrams, DbSketch does not emit `direction LR` by default. Some Markdown renderers display `direction` and `LR` as separate entities.

Set `diagram.mermaid.emitDirection: true` only when your Mermaid renderer correctly supports `direction` inside `erDiagram`.

Example Mermaid Markdown config:

```yaml
output:
  path: docs/db/schema.md
  format: markdown

diagram:
  renderer: mermaid
  direction: LR

  mermaid:
    emitDirection: false
```

```mermaid
erDiagram

  "dbo.Orders" }|--|| "dbo.Users" : "FK_Orders_Users"
```

## Manual Integration Tests

DbSketch has explicit manual integration tests that use Testcontainers and require Docker. They are not run by default.

Run them explicitly:

```bash
dotnet test DbSketch.sln --explicit only
dotnet test --filter-method "DimonSmart.DbSketch.Tests.Integration.PostgresNorthwindEndToEndTests.Generate_WithPostgresNorthwind_WritesDotSchema" --explicit only
dotnet test --filter-method "DimonSmart.DbSketch.Tests.Integration.PostgresCommentsTests.ReadAsync_WhenReadCommentsIsTrue_ReadsTableAndColumnComments" --explicit only
```

## Example DOT

```dot
digraph DbSketch {
  graph [
    rankdir=LR,
    labelloc="t",
    label="Database schema"
  ];

  "table_dbo_Orders":"col_UserId" -> "table_dbo_Users":"col_Id" [
    label="FK_Orders_Users"
  ];
}
```

## Not Supported Yet

DbSketch does not render SVG/PNG, run Graphviz or Mermaid CLI, generate DBML or PlantUML, infer relationships by naming convention, generate HTML docs, diff schemas, or provide a GUI.
