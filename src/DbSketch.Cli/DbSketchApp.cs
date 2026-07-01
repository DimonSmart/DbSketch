using System.Reflection;
using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Rendering;
using DimonSmart.DbSketch.Core.Schema;
using DimonSmart.DbSketch.MySql;
using DimonSmart.DbSketch.Postgres;
using DimonSmart.DbSketch.SqlServer;

namespace DimonSmart.DbSketch.Cli;

public static class DbSketchApp
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                PrintHelp();
                return args.Length == 0 ? 1 : 0;
            }

            if (args is ["--version"] or ["-v"])
            {
                PrintVersion();
                return 0;
            }

            if (!string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
            {
                throw new CliException($"Unknown command '{args[0]}'.");
            }

            var cli = CliParser.ParseGenerate(args[1..]);
            var config = cli.ConfigPath is null ? new DbSketchConfig() : ConfigLoader.Load(cli.ConfigPath);
            await GenerateAsync(GenerateOptionsResolver.Resolve(config, cli), cancellationToken);
            return 0;
        }
        catch (Exception ex) when (ex is CliException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task GenerateAsync(ResolvedGenerateOptions options, CancellationToken cancellationToken)
    {
        Console.WriteLine("DbSketch");
        Console.WriteLine($"Provider: {options.Provider}");
        var reader = CreateReader(options.Provider);
        if (options.Verbose)
        {
            Console.WriteLine("Connection string: provided");
            Console.WriteLine($"Provider reader: {reader.GetType().Name}");
        }

        Console.WriteLine("Reading database schema...");
        var model = await reader.ReadAsync(new DatabaseReadOptions(options.Provider, options.ConnectionString), cancellationToken);
        var filtered = new WildcardSchemaFilter().Apply(model, options.Filter);
        if (options.Verbose)
        {
            Console.WriteLine($"Included tables: {string.Join(", ", options.Filter.IncludeTables)}");
            Console.WriteLine($"Excluded tables: {string.Join(", ", options.Filter.ExcludeTables)}");
        }

        Console.WriteLine($"Tables: {filtered.Tables.Count}");
        Console.WriteLine($"Foreign keys: {filtered.ForeignKeys.Count}");
        if (filtered.Tables.Count == 0)
        {
            Console.WriteLine("Warning: no tables found after filtering.");
        }

        if (options.DryRun)
        {
            Console.WriteLine("Dry run: output was not written.");
            return;
        }

        var renderer = new DiagramRendererFactory().Create(options.OutputFormat.DiagramFormat);
        var diagramText = renderer.Render(filtered, options.Diagram);
        var output = options.OutputFormat.MarkdownWrapper
            ? MarkdownDiagramWrapper.Wrap(diagramText, options.OutputFormat.DiagramFormat, options.Diagram.Title)
            : diagramText;
        var directory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Console.WriteLine($"Writing: {options.OutputPath}");
        await File.WriteAllTextAsync(options.OutputPath, output, cancellationToken);
        Console.WriteLine("Done.");
    }

    private static IDatabaseSchemaReader CreateReader(string provider) => provider switch
    {
        "sqlserver" => new SqlServerSchemaReader(),
        "postgres" => new PostgresSchemaReader(),
        "mysql" => new MySqlSchemaReader(),
        _ => throw new CliException($"Unknown provider '{provider}'.")
    };

    private static void PrintVersion() => Console.WriteLine($"DbSketch {GetVersion()}");

    private static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(DbSketchApp).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString(3)
            : informationalVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0";
        }

        version = version.Trim();
        if (version.StartsWith('v') && version.Length > 1 && char.IsDigit(version[1]))
        {
            version = version[1..];
        }

        var metadataStart = version.IndexOf('+', StringComparison.Ordinal);
        return metadataStart < 0 ? version : version[..metadataStart];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("DbSketch");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dbsketch --version");
        Console.WriteLine("  dbsketch generate --config dbsketch.yml");
        Console.WriteLine("  dbsketch generate --provider sqlserver --connection <string> --out docs/db/schema.dot --format dot");
        Console.WriteLine();
        Console.WriteLine("Supported formats: dot, md-dot, mermaid, md-mermaid.");
    }
}
