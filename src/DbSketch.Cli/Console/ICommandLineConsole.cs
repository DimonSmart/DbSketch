namespace DimonSmart.DbSketch.Cli.Console;

public interface ICommandLineConsole
{
    TextWriter Out { get; }

    TextWriter Error { get; }
}

public sealed class SystemCommandLineConsole : ICommandLineConsole
{
    public TextWriter Out => System.Console.Out;

    public TextWriter Error => System.Console.Error;
}
