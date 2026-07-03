# DbSketch

DbSketch turns a live database into version-controlled schema documentation.

It reads tables, columns, primary keys, foreign keys, and comments directly from SQL Server, PostgreSQL, or MySQL, then generates diagram-as-code files you can commit, review, render, reuse as LLM context, and refresh from CI.

## What it produces

DbSketch is a CLI documentation generator, not a visual database designer. Its primary output is text-based, Markdown-friendly documentation:

- `.dot`: Graphviz DOT, best for precise technical diagrams and column-to-column foreign key edges.
- `.mmd`: Mermaid ER, convenient for Markdown renderers that support Mermaid diagrams.
- `.md`: Markdown documentation that wraps DOT or Mermaid in a fenced code block.

These files can often be used directly in repositories, wikis, issue trackers, documentation sites, and other tools that understand diagram-as-code or Markdown-like formats.

They are also useful as compact context for LLM assistants. Instead of asking an agent to inspect the database with ad-hoc commands or parse creation scripts and migrations, you can give it generated schema documentation that already contains the relevant tables, columns, keys, relationships, and comments.

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
- Produces compact schema context for AI coding assistants

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
```

With only these two settings, DbSketch generates one Mermaid diagram named `main`, wraps it in Markdown, and writes it to `docs/db/schema.md`.

Add filters or focused diagrams when the project needs them:

```yaml
provider: postgres
connectionString: "${DB_CONNECTION}"

diagrams:
  - name: main
    include:
      tables:
        - "public.*"
    exclude:
      tables:
        - "public.__EFMigrationsHistory"
```

Start with the minimal config, then add focused diagrams, comments, filters, layout settings, or DOT output when the project needs them. See [Configuration](https://github.com/DimonSmart/DbSketch/blob/main/docs/configuration.md) for the full YAML reference.

## Output formats

- DOT: best for precise technical diagrams and column-to-column relationships.
- Mermaid: convenient for Markdown renderers that support Mermaid diagrams, with entity-level relationships.
- Markdown: useful for generated docs because it wraps DOT or Mermaid in a fenced block.

## Optional PNG or SVG rendering

DbSketch does not require image generation. Use the generated text output directly when your documentation platform can render DOT, Mermaid, or Markdown-like diagram formats.

Static images are useful for places that cannot render diagram source directly, such as package pages, PDFs, presentations, or documentation sites without Mermaid or Graphviz support. The images in this README are committed PNG files for that reason.

To render DOT output as an image, first generate a raw `.dot` file:

```yaml
diagrams:
  - name: main-dot
    title: Database schema
    output:
      format: raw
      path: docs/db/schema.dot
```

Install Graphviz:

```bash
# Windows
winget install graphviz

# macOS
brew install graphviz

# Ubuntu / Debian
sudo apt install graphviz
```

Then render PNG or SVG from the generated DOT file:

```bash
dot -Tpng docs/db/schema.dot -o docs/db/schema.png
dot -Tsvg docs/db/schema.dot -o docs/db/schema.svg
```

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
