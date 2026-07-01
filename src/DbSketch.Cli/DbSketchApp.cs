using System.CommandLine;
using DimonSmart.DbSketch.Cli.CommandLine;
using DimonSmart.DbSketch.Cli.Console;
using DimonSmart.DbSketch.Cli.Generation;
using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Cli;

public static class DbSketchApp
{
    public static Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var console = new SystemCommandLineConsole();
        var generator = new DbSketchGenerator(
            new DatabaseSchemaReaderFactory(),
            new DiagramRendererFactory(),
            new WildcardSchemaFilter(),
            console);
        var handler = new GenerateCommandHandler(generator, console);
        var rootCommand = DbSketchCommandLine.CreateRootCommand(handler);
        return rootCommand.Parse(args).InvokeAsync(new InvocationConfiguration
        {
            Output = console.Out,
            Error = console.Error,
            EnableDefaultExceptionHandler = false
        }, cancellationToken);
    }
}
