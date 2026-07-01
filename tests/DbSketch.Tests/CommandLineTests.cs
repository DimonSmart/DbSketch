using System.CommandLine;
using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Cli.CommandLine;

namespace DimonSmart.DbSketch.Tests;

public sealed class CommandLineTests
{
    [Fact]
    public async Task RootWithoutArgumentsShowsHelpAndReturnsZero()
    {
        var output = new StringWriter();
        var exitCode = await InvokeAsync([], output, new StringWriter(), (_, _) => Task.FromResult(0));

        Assert.Equal(0, exitCode);
        Assert.Contains("Generate database schema diagrams from live databases.", output.ToString());
    }

    [Fact]
    public async Task VersionReturnsDbSketchVersionFormat()
    {
        var output = new StringWriter();
        var exitCode = await InvokeAsync(["--version"], output, new StringWriter(), (_, _) => Task.FromResult(0));

        Assert.Equal(0, exitCode);
        Assert.StartsWith("DbSketch ", output.ToString());
    }

    [Theory]
    [InlineData("generate --config dbsketch.yml", "dbsketch.yml")]
    [InlineData("generate -c dbsketch.yml", "dbsketch.yml")]
    public async Task ParsesConfigOption(string commandLine, string expectedPath)
    {
        CliOptions? captured = null;

        var exitCode = await InvokeAsync(commandLine.Split(' '), new StringWriter(), new StringWriter(), (options, _) =>
        {
            captured = options;
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedPath, captured?.ConfigPath);
    }

    [Fact]
    public async Task ParsesEqualsSyntax()
    {
        CliOptions? captured = null;

        var exitCode = await InvokeAsync(["generate", "--provider=postgres", "--connection=Host=localhost"], new StringWriter(), new StringWriter(), (options, _) =>
        {
            captured = options;
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.Equal("postgres", captured?.Provider);
        Assert.Equal("Host=localhost", captured?.ConnectionString);
    }

    [Fact]
    public async Task ParsesQuietNoProgressAndStdoutOutput()
    {
        CliOptions? captured = null;

        var exitCode = await InvokeAsync(["generate", "--quiet", "--no-progress", "--out", "-"], new StringWriter(), new StringWriter(), (options, _) =>
        {
            captured = options;
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.True(captured?.Quiet);
        Assert.True(captured?.NoProgress);
        Assert.Equal("-", captured?.OutputPath);
    }

    [Fact]
    public async Task RejectsQuietAndVerboseTogether()
    {
        var exitCode = await InvokeAsync(["generate", "--quiet", "--verbose"], new StringWriter(), new StringWriter(), (_, _) => Task.FromResult(0));

        Assert.NotEqual(0, exitCode);
    }

    private static Task<int> InvokeAsync(string[] args, TextWriter output, TextWriter error, Func<CliOptions, CancellationToken, Task<int>> handleGenerateAsync)
    {
        var root = DbSketchCommandLine.CreateRootCommand(handleGenerateAsync);
        return root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = output, Error = error });
    }
}
