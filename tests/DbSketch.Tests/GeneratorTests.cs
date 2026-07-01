using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Cli.Console;
using DimonSmart.DbSketch.Cli.Generation;
using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Tests;

public sealed class GeneratorTests
{
    [Fact]
    public async Task WritesFileOutput()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(outputPath: path), CancellationToken.None);

        Assert.True(File.Exists(path));
        Assert.Contains("digraph DbSketch", File.ReadAllText(path));
    }

    [Fact]
    public async Task DryRunDoesNotWriteOutput()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(outputPath: path, dryRun: true), CancellationToken.None);

        Assert.False(File.Exists(path));
        Assert.Contains("Dry run", console.ErrorText);
    }

    [Fact]
    public async Task StdoutOutputWritesGeneratedTextToOut()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(outputPath: "-"), CancellationToken.None);

        Assert.Contains("digraph DbSketch", console.OutText);
        Assert.DoesNotContain("Reading database schema", console.OutText);
    }

    [Fact]
    public async Task QuietSuppressesProgressAndWarnings()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(renderer: DiagramFormat.Mermaid, quiet: true, showTableComments: true, dryRun: true), CancellationToken.None);

        Assert.Equal("", console.ErrorText);
    }

    [Fact]
    public async Task NoProgressSuppressesProgressButKeepsWarnings()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(renderer: DiagramFormat.Mermaid, noProgress: true, showTableComments: true, dryRun: true), CancellationToken.None);

        Assert.DoesNotContain("Reading database schema", console.ErrorText);
        Assert.Contains("Warning:", console.ErrorText);
        Assert.Contains("table comments", console.ErrorText);
    }

    [Fact]
    public async Task MermaidEmitsCapabilityWarning()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(renderer: DiagramFormat.Mermaid, dryRun: true), CancellationToken.None);

        Assert.Contains("column ports", console.ErrorText);
    }

    [Fact]
    public async Task DotDoesNotEmitMermaidWarning()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(renderer: DiagramFormat.Dot, dryRun: true), CancellationToken.None);

        Assert.DoesNotContain("Mermaid ER", console.ErrorText);
    }

    private static DbSketchGenerator CreateGenerator(FakeConsole console) =>
        new(new FakeReaderFactory(), new DiagramRendererFactory(), new WildcardSchemaFilter(), console);

    private static ResolvedGenerateOptions Options(
        string outputPath = "dbsketch.dot",
        DiagramFormat renderer = DiagramFormat.Dot,
        bool quiet = false,
        bool noProgress = false,
        bool dryRun = false,
        bool showTableComments = false) =>
        new(
            "sqlserver",
            "Server=test",
            outputPath,
            renderer,
            new OutputFormat(OutputContainerFormat.Raw, null),
            false,
            quiet,
            noProgress,
            dryRun,
            new SchemaFilterOptions([], []),
            new DiagramRenderOptions(
                "Database schema",
                DiagramDirection.LR,
                true,
                new DiagramShowOptions(true, false, false, true, true, showTableComments, false),
                new MermaidRenderOptions(false),
                new DiagramCommentRenderOptions(null)),
            false,
            null,
            new CommentOverridesConfig());

    private sealed class FakeConsole : ICommandLineConsole
    {
        private readonly StringWriter _out = new();
        private readonly StringWriter _error = new();

        public TextWriter Out => _out;

        public TextWriter Error => _error;

        public string OutText => _out.ToString();

        public string ErrorText => _error.ToString();
    }

    private sealed class FakeReaderFactory : IDatabaseSchemaReaderFactory
    {
        public IDatabaseSchemaReader Create(string provider) => new FakeReader();
    }

    private sealed class FakeReader : IDatabaseSchemaReader
    {
        public Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken) =>
            Task.FromResult(new DatabaseModel(
                options.Provider,
                "Test",
                [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users")],
                []));
    }
}
