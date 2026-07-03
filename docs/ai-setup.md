# AI-assisted Setup

Use this page when you want ChatGPT, Claude, Codex, or another coding assistant to add DbSketch to an existing repository.

The assistant should make a small, reviewable change: add the tool, create a config, document the command, and optionally add CI if the repository already has a suitable workflow.

## Recommended prompt

Paste this prompt into your coding assistant from the repository root:

```text
Add DbSketch database diagram generation to this repository.

Requirements:
- Create a minimal dbsketch.yml.
- Use provider and connectionString as the required settings.
- Read the connection string from ${DB_CONNECTION}.
- Use Mermaid Markdown output by default.
- Generate schema documentation under docs/db.
- Add or update a local .NET tool manifest with DimonSmart.DbSketch.
- Add a short README or docs note explaining how to run:
  dotnet tool restore
  dotnet tool run dbsketch -- generate --config dbsketch.yml
- If the repository already has CI, add a safe CI step that restores local tools and runs DbSketch using a secret named DB_CONNECTION.
- Do not hardcode real connection strings or passwords.
- Do not remove existing documentation or CI steps unless they are clearly obsolete.
```

## Minimal prompt

Use this shorter prompt when you only want the smallest working setup:

```text
Add DbSketch to this repository with the smallest working config. Generate a Mermaid Markdown schema diagram under docs/db/schema.md. Use ${DB_CONNECTION} for the connection string and document the command needed to run it locally.
```

## What to review

After the assistant finishes, review these files before committing:

- `dbsketch.yml`
- `.config/dotnet-tools.json`
- generated files under `docs/db`
- any README, docs, or CI workflow changes

Check that no real connection string, password, token, or production host was committed.

## Useful follow-up prompt

If the first result is too broad, ask the assistant to keep the setup smaller:

```text
Simplify the DbSketch setup. Keep only the minimum required config, one generated Mermaid Markdown diagram, and the local tool manifest. Move CI and advanced options to documentation instead of adding them now.
```
