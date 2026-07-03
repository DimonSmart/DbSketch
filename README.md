# DbSketch

DbSketch turns a live database into version-controlled schema documentation.

It reads tables, columns, primary keys, foreign keys, and comments directly from SQL Server, PostgreSQL, or MySQL, then generates diagram source files you can commit, review, and refresh from CI.

## What it produces

DbSketch is a CLI documentation generator, not a visual database designer. It generates text-based diagram files:

- `.dot`: Graphviz DOT, best for precise technical diagrams and column-to-column foreign key edges.
- `.mmd`: Mermaid ER, convenient for GitHub Markdown and other Mermaid-compatible renderers.
- `.md`: Markdown documentation that wraps DOT or Mermaid in a fenced code block.

When you need a ready-to-display image for a README, NuGet package page, or documentation site, generate a PNG from the DOT output with Graphviz and commit that image alongside the generated source.

Compact layout, using `tableHeaderLayout: "{fullName}"` and `columnLayout: "{name}"`:

![Compact DbSketch generated Northwind database schema](https://raw.githubusercontent.com/DimonSmart/DbSketch/main/docs/assets/northwind-schema-compact.png)

Full layout, using `tableHeaderLayout: "{fullName} | {comment}"` and `columnLayout: "{name} | {type} | {comment} | {keys}"`:

![Full DbSketch generated Northwind database schema](https://raw.githubusercontent.com/DimonSmart/DbSketch/main/docs/assets/northwind-schema-full.png)

## Why DbSketch?

- Reads live SQL Server, PostgreSQL, and MySQL schemas
- Generates Graphviz DOT, Mermaid ER, or Markdown-wrapped output
- Supports multiple focused diagrams from one config
- Preserves precise column-to-column foreign key edges in DOT
- Can include database-native table and column comments
- Works locally, in CI, or as a repository documentation step

## Quick start

```bash
dotnet tool install --global DimonSmart.DbSketch
dbsketch generate --config dbsketch.yml
```

DbSketch also works as a local repository tool. See [Getting started](https://github.com/DimonSmart/DbSketch/blob/main/docs/getting-started.md) for local tool, one-shot, `dnx`, and CI examples.

## Typical workflow

1. Create `dbsketch.yml` in the repository.
2. Read the database connection string from an environment variable such as `DB_CONNECTION`.
3. Run `dbsketch generate --config dbsketch.yml` locally.
4. Commit the generated files under `docs/db` or another documentation folder.
5. Add the same command to CI when you want schema documentation to be refreshed automatically.

## Starter config

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"

defaults:
  output:
    format: markdown
  diagram:
    renderer: dot
    direction: LR

diagrams:
  - name: main
    title: Database schema
    include:
      tables:
        - "public.*"
    exclude:
      tables:
        - "public.__EFMigrationsHistory"
    output:
      path: docs/db/schema.md
```

Start with one diagram, then add focused diagrams, comments, filters, layout settings, or Mermaid output when the project needs them. See [Configuration](https://github.com/DimonSmart/DbSketch/blob/main/docs/configuration.md) for the full YAML reference.

## Output formats

- DOT: best for precise technical diagrams and column-to-column relationships.
- Mermaid: convenient for GitHub Markdown, with entity-level relationships.
- Markdown: useful for generated docs because it wraps DOT or Mermaid in a fenced block.

## Documentation

- [Getting started](https://github.com/DimonSmart/DbSketch/blob/main/docs/getting-started.md)
- [Configuration](https://github.com/DimonSmart/DbSketch/blob/main/docs/configuration.md)
- [Renderers](https://github.com/DimonSmart/DbSketch/blob/main/docs/renderers.md)
- [Database comments](https://github.com/DimonSmart/DbSketch/blob/main/docs/comments.md)
- [Northwind example](https://github.com/DimonSmart/DbSketch/blob/main/docs/examples/northwind.md)
- [Development](https://github.com/DimonSmart/DbSketch/blob/main/docs/development.md)

## Use with AI assistants

Want to add DbSketch to an existing repository? Paste this into ChatGPT, Claude, or Codex:

> Add DbSketch database diagram generation to this repository. Create a `dbsketch.yml` that reads `DB_CONNECTION`, generates focused database diagrams under `docs/db`, and adds a CI step to refresh them.

## Contributing

Issues and pull requests are welcome, especially focused fixes, provider improvements, renderer improvements, and documentation updates.
