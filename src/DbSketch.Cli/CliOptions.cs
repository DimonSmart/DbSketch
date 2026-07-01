namespace DimonSmart.DbSketch.Cli;

public sealed record CliOptions(
    string? ConfigPath,
    string? Provider,
    string? ConnectionString,
    string? OutputPath,
    string? Renderer,
    string? Format,
    bool Verbose,
    bool Quiet,
    bool NoProgress,
    bool DryRun);
