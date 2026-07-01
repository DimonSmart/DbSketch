using DimonSmart.DbSketch.Cli.Console;
using DimonSmart.DbSketch.Cli.Generation;
using DimonSmart.DbSketch.Core.Config;

namespace DimonSmart.DbSketch.Cli;

public sealed class GenerateCommandHandler(DbSketchGenerator generator, ICommandLineConsole console)
{
    public async Task<int> HandleAsync(CliOptions cliOptions, CancellationToken cancellationToken)
    {
        var progress = new ProgressReporter(console, cliOptions.Quiet, cliOptions.NoProgress, cliOptions.Verbose);

        try
        {
            var config = cliOptions.ConfigPath is null ? new DbSketchConfig() : ConfigLoader.Load(cliOptions.ConfigPath);
            var resolved = GenerateOptionsResolver.Resolve(config, cliOptions);
            await generator.GenerateAsync(resolved, cancellationToken);
            return 0;
        }
        catch (Exception ex) when (ex is CliException or InvalidOperationException)
        {
            progress.Error(ex.Message);
            return 1;
        }
    }
}
