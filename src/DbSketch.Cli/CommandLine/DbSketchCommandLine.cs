using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace DimonSmart.DbSketch.Cli.CommandLine;

public static class DbSketchCommandLine
{
    public static RootCommand CreateRootCommand(GenerateCommandHandler handler)
    {
        return CreateRootCommand(handler.HandleAsync);
    }

    public static RootCommand CreateRootCommand(Func<CliOptions, CancellationToken, Task<int>> handleGenerateAsync)
    {
        var root = new RootCommand("Generate database schema diagrams from live databases.");
        root.SetAction(parseResult => new HelpAction().Invoke(parseResult));

        foreach (var versionOption in root.Options.OfType<VersionOption>())
        {
            versionOption.Action = new DbSketchVersionAction();
        }

        root.Subcommands.Add(CreateGenerateCommand(handleGenerateAsync));
        return root;
    }

    private static Command CreateGenerateCommand(Func<CliOptions, CancellationToken, Task<int>> handleGenerateAsync)
    {
        var config = new Option<string>("--config", "-c") { Description = "YAML config path." };
        var provider = new Option<string>("--provider") { Description = "Database provider: sqlserver, postgres, mysql. Aliases: mssql, postgresql." };
        var connection = new Option<string>("--connection") { Description = "Database connection string." };
        var output = new Option<string>("--out", "-o") { Description = "Output path. Use \"-\" to write diagram text to stdout." };
        var renderer = new Option<string>("--renderer") { Description = "Diagram renderer: dot, mermaid." };
        var format = new Option<string>("--format") { Description = "Output format: raw, markdown." };
        var verbose = new Option<bool>("--verbose") { Description = "Print detailed diagnostic information." };
        var quiet = new Option<bool>("--quiet") { Description = "Print only errors. Do not print progress messages." };
        var noProgress = new Option<bool>("--no-progress") { Description = "Do not print progress messages, but still allow non-progress warnings/errors." };
        var dryRun = new Option<bool>("--dry-run") { Description = "Read schema and apply config, but do not write output." };

        var command = new Command("generate", "Read a live database schema and generate a diagram.");
        command.Options.Add(config);
        command.Options.Add(provider);
        command.Options.Add(connection);
        command.Options.Add(output);
        command.Options.Add(renderer);
        command.Options.Add(format);
        command.Options.Add(verbose);
        command.Options.Add(quiet);
        command.Options.Add(noProgress);
        command.Options.Add(dryRun);
        command.Validators.Add(result =>
        {
            if (result.GetValue(quiet) && result.GetValue(verbose))
            {
                result.AddError("--quiet and --verbose cannot be used together.");
            }
        });
        command.SetAction((parseResult, cancellationToken) =>
        {
            var outputPath = parseResult.GetValue(output);
            var verboseValue = parseResult.GetValue(verbose);
            var options = new CliOptions(
                parseResult.GetValue(config),
                parseResult.GetValue(provider),
                parseResult.GetValue(connection),
                outputPath,
                parseResult.GetValue(renderer),
                parseResult.GetValue(format),
                verboseValue,
                parseResult.GetValue(quiet),
                parseResult.GetValue(noProgress) || (outputPath == "-" && !verboseValue),
                parseResult.GetValue(dryRun));

            return handleGenerateAsync(options, cancellationToken);
        });

        return command;
    }

    private sealed class DbSketchVersionAction : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            parseResult.InvocationConfiguration.Output.WriteLine($"DbSketch {DbSketchVersion.GetVersion()}");
            return 0;
        }
    }
}
