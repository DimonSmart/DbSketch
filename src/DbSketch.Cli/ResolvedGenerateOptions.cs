using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Rendering;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Cli;

public sealed record ResolvedGenerateOptions(string Provider, string ConnectionString, string OutputPath, string Format, bool Verbose, bool DryRun, SchemaFilterOptions Filter, DiagramRenderOptions Diagram);

public static class GenerateOptionsResolver
{
    public static ResolvedGenerateOptions Resolve(DbSketchConfig config, CliOptions cli)
    {
        var provider = NormalizeProvider(cli.Provider ?? config.Provider);
        var connectionString = cli.ConnectionString ?? config.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new CliException("Missing connection string.");
        }

        var format = (cli.Format ?? config.Output.Format ?? "dot").Trim().ToLowerInvariant();
        if (format is not ("dot" or "md-dot"))
        {
            throw new CliException($"Unknown format '{format}'. Supported values: dot, md-dot.");
        }

        return new ResolvedGenerateOptions(
            provider,
            connectionString,
            cli.OutputPath ?? config.Output.Path ?? "dbsketch.dot",
            format,
            cli.Verbose,
            cli.DryRun,
            new SchemaFilterOptions(config.Include.Tables, config.Exclude.Tables),
            new DiagramRenderOptions(
                string.IsNullOrWhiteSpace(config.Diagram.Title) ? "Database schema" : config.Diagram.Title,
                string.IsNullOrWhiteSpace(config.Diagram.Rankdir) ? "LR" : config.Diagram.Rankdir,
                config.Diagram.Compact,
                new DiagramShowOptions(config.Diagram.Show.SchemaName, config.Diagram.Show.ColumnTypes, config.Diagram.Show.Nullability, config.Diagram.Show.PrimaryKeys, config.Diagram.Show.ForeignKeys)));
    }

    public static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new CliException("Missing provider.");
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "mssql" => "sqlserver",
            "sqlserver" => "sqlserver",
            "postgresql" => "postgres",
            "postgres" => "postgres",
            "mysql" => "mysql",
            var value => throw new CliException($"Unknown provider '{value}'. Supported values: sqlserver, postgres, mysql.")
        };
    }
}
