namespace DimonSmart.DbSketch.Cli.Console;

public sealed class ProgressReporter(ICommandLineConsole console, bool quiet, bool noProgress, bool verbose)
{
    public void Info(string message)
    {
        if (!quiet && !noProgress)
        {
            console.Error.WriteLine(message);
        }
    }

    public void Verbose(string message)
    {
        if (!quiet && verbose)
        {
            console.Error.WriteLine(message);
        }
    }

    public void Warning(string message)
    {
        if (!quiet)
        {
            console.Error.WriteLine($"Warning: {message}");
        }
    }

    public void Error(string message) => console.Error.WriteLine($"Error: {message}");
}
