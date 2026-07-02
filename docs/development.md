# Development

Restore, build, test, and pack:

```bash
dotnet restore DbSketch.sln
dotnet build DbSketch.sln
dotnet test DbSketch.sln
dotnet pack src/DbSketch.Cli/DbSketch.Cli.csproj -c Release
```

## Git Hooks

This repository uses Husky.Net for local Git hooks.

After cloning the repository, run:

```bash
dotnet restore DbSketch.sln
```

The restore step installs local .NET tools and configures Git hooks automatically.

The pre-commit hook formats staged .NET files with `dotnet format` and re-stages only files that were already staged before the hook started.

Skip hooks for a single commit:

```bash
git commit --no-verify
```

Disable Husky installation in CI or special local environments:

```bash
HUSKY=0 dotnet restore DbSketch.sln
```

## Manual Integration Tests

DbSketch has explicit manual integration tests that use Testcontainers and require Docker. They are not run by default.

Run them explicitly:

```bash
dotnet test DbSketch.sln --explicit only
dotnet test --filter-method "DimonSmart.DbSketch.Tests.Integration.PostgresNorthwindEndToEndTests.Generate_WithPostgresNorthwind_WritesDotSchema" --explicit only
dotnet test --filter-method "DimonSmart.DbSketch.Tests.Integration.PostgresCommentsTests.ReadAsync_WhenReadCommentsIsTrue_ReadsTableAndColumnComments" --explicit only
```

## Docs Assets

The README illustration and Northwind examples are generated from the PostgreSQL fixture:

```text
tests/DbSketch.Tests/TestData/Northwind/postgres-northwind-schema.sql
```

Regenerate docs assets:

```powershell
scripts/generate-docs-assets.ps1
```

The script requires `dotnet`, Docker, and Graphviz `dot`. It starts a temporary PostgreSQL container, applies the fixture, runs DbSketch from source, writes DOT and Mermaid examples, and renders `docs/assets/northwind-schema.png`.

Skip PNG generation when Graphviz output is not needed:

```powershell
scripts/generate-docs-assets.ps1 -SkipPng
```

Keep the temporary container for debugging:

```powershell
scripts/generate-docs-assets.ps1 -KeepContainer
```
