# DbSketch

[![CI](https://github.com/DimonSmart/DbSketch/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/DimonSmart/DbSketch/actions/workflows/ci.yml)
[![Release](https://github.com/DimonSmart/DbSketch/actions/workflows/release.yml/badge.svg)](https://github.com/DimonSmart/DbSketch/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/DimonSmart.DbSketch.svg)](https://www.nuget.org/packages/DimonSmart.DbSketch)

DbSketch turns a live database into version-controlled schema documentation.

It reads tables, columns, primary keys, foreign keys, and comments directly from SQL Server, PostgreSQL, or MySQL, then generates diagram-as-code files you can commit, review, render, reuse as LLM context, and refresh from CI.

DbSketch is useful when both humans and AI coding assistants need a compact, reviewable schema snapshot. Instead of asking an agent to inspect the database with ad-hoc commands or parse creation scripts and migrations, you can give it generated schema documentation that already contains the relevant tables, columns, keys, relationships, and comments.

## What it produces

DbSketch is a CLI documentation generator, not a visual database designer. Its primary output is text-based, Markdown-friendly documentation:

- `.dot`: Graphviz DOT, best for precise technical diagrams and column-to-column foreign key edges.
- `.mmd`: Mermaid ER, convenient for Markdown renderers that support Mermaid diagrams.
- `.md`: Markdown documentation that wraps DOT or Mermaid in a fenced code block.

These files can often be used directly in repositories, wikis, issue trackers, documentation sites, and other tools that understand diagram-as-code or Markdown-like formats.

## What it looks like

Compact schema diagram:

![Compact DbSketch generated Northwind database schema](https://raw.githubusercontent.com/DimonSmart/DbSketch/main/docs/assets/northwind-schema-compact.png)

Detailed schema diagram with comments and column metadata:

![Full DbSketch generated Northwind database schema](https://raw.githubusercontent.com/DimonSmart/DbSketch/main/docs/assets/northwind-schema-full.png)

## Why DbSketch?

- Reads live SQL Server, PostgreSQL, and MySQL schemas
- Generates Graphviz DOT, Mermaid ER, or Markdown-wrapped output
- Supports multiple focused diagrams from one config
- Preserves precise column-to-column foreign key edges in DOT
- Can include database-native table and column comments
- Works locally, in CI, or as a repository documentation step
- Produces compact schema context for AI coding assistants

## AI-assisted setup

Want ChatGPT, Claude, Codex, or another coding assistant to add DbSketch to an existing repository? See [AI-assisted setup](https://github.com/DimonSmart/DbSketch/blob/main/docs/ai-setup.md) for copy-paste prompts and review guidance.

## Documentation

Start here:

- [Getting started](https://github.com/DimonSmart/DbSketch/blob/main/docs/getting-started.md)
- [AI-assisted setup](https://github.com/DimonSmart/DbSketch/blob/main/docs/ai-setup.md)

Reference:

- [Configuration](https://github.com/DimonSmart/DbSketch/blob/main/docs/configuration.md)
- [Full config example](https://github.com/DimonSmart/DbSketch/blob/main/docs/examples/full-config.md)
- [Renderers](https://github.com/DimonSmart/DbSketch/blob/main/docs/renderers.md)
- [Database comments](https://github.com/DimonSmart/DbSketch/blob/main/docs/comments.md)
- [CI and automation](https://github.com/DimonSmart/DbSketch/blob/main/docs/ci.md)

Examples and development:

- [Northwind example](https://github.com/DimonSmart/DbSketch/blob/main/docs/examples/northwind.md)
- [Development](https://github.com/DimonSmart/DbSketch/blob/main/docs/development.md)

## Contributing

Issues and pull requests are welcome, especially focused fixes, provider improvements, renderer improvements, and documentation updates.
