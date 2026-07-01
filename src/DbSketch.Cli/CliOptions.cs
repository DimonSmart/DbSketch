namespace DimonSmart.DbSketch.Cli;

public sealed record CliOptions(string? ConfigPath, string? Provider, string? ConnectionString, string? OutputPath, string? Renderer, string? Format, bool Verbose, bool DryRun);

public static class CliParser
{
    public static CliOptions ParseGenerate(string[] args)
    {
        string? config = null;
        string? provider = null;
        string? connection = null;
        string? output = null;
        string? renderer = null;
        string? format = null;
        var verbose = false;
        var dryRun = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-c":
                case "--config":
                    config = ReadValue(args, ref i, arg);
                    break;
                case "--provider":
                    provider = ReadValue(args, ref i, arg);
                    break;
                case "--connection":
                    connection = ReadValue(args, ref i, arg);
                    break;
                case "-o":
                case "--out":
                    output = ReadValue(args, ref i, arg);
                    break;
                case "--format":
                    format = ReadValue(args, ref i, arg);
                    break;
                case "--renderer":
                    renderer = ReadValue(args, ref i, arg);
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new CliException($"Unknown option '{arg}'.");
            }
        }

        return new CliOptions(config, provider, connection, output, renderer, format, verbose, dryRun);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            throw new CliException($"Option '{option}' requires a value.");
        }

        index++;
        return args[index];
    }
}
