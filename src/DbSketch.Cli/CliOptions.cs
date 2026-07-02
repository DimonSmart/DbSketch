namespace DimonSmart.DbSketch.Cli;

public sealed record CliOptions(
    string? ConfigPath,
    string? DiagramName,
    bool Verbose,
    bool Quiet,
    bool NoProgress,
    bool DryRun);
