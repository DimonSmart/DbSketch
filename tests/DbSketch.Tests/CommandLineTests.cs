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
    public async Task ParsesDiagramOption()
    {
        CliOptions? captured = null;

        var exitCode = await InvokeAsync(["generate", "--config=dbsketch.yml", "--diagram=auth"], new StringWriter(), new StringWriter(), (options, _) =>
        {
            captured = options;
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.Equal("auth", captured?.DiagramName);
    }

    [Fact]
    public async Task ParsesQuietNoProgressVerboseAndDryRun()
    {
        CliOptions? captured = null;

        var exitCode = await InvokeAsync(["generate", "--config", "dbsketch.yml", "--quiet", "--no-progress", "--dry-run"], new StringWriter(), new StringWriter(), (options, _) =>
        {
            captured = options;
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.True(captured?.Quiet);
        Assert.True(captured?.NoProgress);
        Assert.True(captured?.DryRun);
    }

    [Fact]
    public async Task RejectsQuietAndVerboseTogether()
    {
        var exitCode = await InvokeAsync(["generate", "--quiet", "--verbose"], new StringWriter(), new StringWriter(), (_, _) => Task.FromResult(0));

        Assert.NotEqual(0, exitCode);
    }

    [Theory]
    [InlineData("--provider")]
    [InlineData("--connection")]
    [InlineData("--out")]
    [InlineData("--renderer")]
    [InlineData("--format")]
    public async Task RejectsRemovedGenerateOptions(string option)
    {
        var exitCode = await InvokeAsync(["generate", option, "value"], new StringWriter(), new StringWriter(), (_, _) => Task.FromResult(0));

        Assert.NotEqual(0, exitCode);
    }

    private static Task<int> InvokeAsync(string[] args, TextWriter output, TextWriter error, Func<CliOptions, CancellationToken, Task<int>> handleGenerateAsync)
    {
        var root = DbSketchCommandLine.CreateRootCommand(handleGenerateAsync);
        return root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = output, Error = error });
    }
}
