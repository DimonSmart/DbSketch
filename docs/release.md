# Release

DbSketch releases are created from version tags.

## Publish the next tag

From a clean `main` branch:

```bat
publish-next-version.bat
```

The default bump is `patch`. Use `-Bump minor` or `-Bump major` for larger version increments:

```bat
publish-next-version.bat -Bump minor
```

To publish an exact version:

```bat
publish-next-version.bat -Version 0.2.0
```

The script fetches tags, switches to `main`, fast-forwards it from `origin`, creates an annotated `vMAJOR.MINOR.PATCH` tag, and pushes the branch and tag.

## GitHub Actions

Pushing a tag like `v0.1.0` runs the release workflow. The workflow restores, builds, runs normal tests, publishes portable Windows and Linux CLI archives, packs the .NET tool package, smoke-tests the package from a local source, and creates a GitHub Release with checksums.

NuGet package id: `DimonSmart.DbSketch`
CLI command: `dbsketch`

Manual integration tests are marked explicit and are not run by the normal release test command.
