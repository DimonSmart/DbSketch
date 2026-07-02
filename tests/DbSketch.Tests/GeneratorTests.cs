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
    public async Task ReadsDatabaseOnceAndWritesMultipleFiles()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var secondPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var console = new FakeConsole();
        var readerFactory = new FakeReaderFactory();
        var generator = CreateGenerator(console, readerFactory);

        await generator.GenerateAsync(
            Options(
                Diagram("full", firstPath, include: []),
                Diagram("auth", secondPath, include: ["dbo.Users"])),
            CancellationToken.None);

        Assert.Equal(1, readerFactory.Reader.ReadCount);
        Assert.True(File.Exists(firstPath));
        Assert.True(File.Exists(secondPath));
        Assert.Contains("digraph DbSketch", File.ReadAllText(firstPath));
        Assert.Contains("digraph DbSketch", File.ReadAllText(secondPath));
    }

    [Fact]
    public async Task DryRunDoesNotWriteOutputButAppliesFiltersForAllDiagrams()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var secondPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(
            Options(
                [
                    Diagram("full", firstPath, include: []),
                    Diagram("auth", secondPath, include: ["dbo.Users"])
                ],
                dryRun: true),
            CancellationToken.None);

        Assert.False(File.Exists(firstPath));
        Assert.False(File.Exists(secondPath));
        Assert.Contains("Diagram: full", console.ErrorText);
        Assert.Contains("Tables: 2", console.ErrorText);
        Assert.Contains("Foreign keys: 1", console.ErrorText);
        Assert.Contains("Diagram: auth", console.ErrorText);
        Assert.Contains("Tables: 1", console.ErrorText);
        Assert.Contains("Foreign keys: 0", console.ErrorText);
        Assert.Contains("Dry run: output was not written.", console.ErrorText);
    }

    [Fact]
    public async Task QuietSuppressesProgressAndWarnings()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(Diagram("auth", "auth.dot", renderer: DiagramFormat.Mermaid, showTableComments: true), quiet: true, dryRun: true), CancellationToken.None);

        Assert.Equal("", console.ErrorText);
    }

    [Fact]
    public async Task NoProgressSuppressesProgressButKeepsWarnings()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(Diagram("auth", "auth.dot", renderer: DiagramFormat.Mermaid, showTableComments: true), noProgress: true, dryRun: true), CancellationToken.None);

        Assert.DoesNotContain("Reading database schema", console.ErrorText);
        Assert.Contains("Warning:", console.ErrorText);
        Assert.Contains("table comments", console.ErrorText);
    }

    [Fact]
    public async Task MermaidEmitsCapabilityWarning()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(Diagram("auth", "auth.dot", renderer: DiagramFormat.Mermaid), dryRun: true), CancellationToken.None);

        Assert.Contains("column ports", console.ErrorText);
    }

    [Fact]
    public async Task DotDoesNotEmitMermaidWarning()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(Options(Diagram("auth", "auth.dot", renderer: DiagramFormat.Dot), dryRun: true), CancellationToken.None);

        Assert.DoesNotContain("Mermaid ER", console.ErrorText);
    }

    [Fact]
    public async Task MermaidEmitsCustomLayoutWarning()
    {
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(
            Options(Diagram("auth", "auth.dot", renderer: DiagramFormat.Mermaid, columnLayout: "{name} | {keys}", tableHeaderLayout: "{fullName}"), dryRun: true),
            CancellationToken.None);

        Assert.Contains("custom columnLayout/tableHeaderLayout", console.ErrorText);
        Assert.Contains("Layout settings will be ignored", console.ErrorText);
    }

    [Fact]
    public async Task ForeignKeysRemainOnlyBetweenTablesInDiagram()
    {
        var fullPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var usersOnlyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dot");
        var console = new FakeConsole();
        var generator = CreateGenerator(console);

        await generator.GenerateAsync(
            Options(
                Diagram("full", fullPath, include: []),
                Diagram("users", usersOnlyPath, include: ["dbo.Users"])),
            CancellationToken.None);

        Assert.Contains("FK_Orders_Users", File.ReadAllText(fullPath));
        Assert.DoesNotContain("FK_Orders_Users", File.ReadAllText(usersOnlyPath));
    }

    private static DbSketchGenerator CreateGenerator(FakeConsole console, FakeReaderFactory? readerFactory = null) =>
        new(readerFactory ?? new FakeReaderFactory(), new DiagramRendererFactory(), new WildcardSchemaFilter(), console);

    private static ResolvedDiagramTarget Diagram(
        string name,
        string outputPath,
        DiagramFormat renderer = DiagramFormat.Dot,
        IReadOnlyList<string>? include = null,
        bool showTableComments = false,
        string? columnLayout = null,
        string? tableHeaderLayout = null) =>
        new(
            name,
            outputPath,
            renderer,
            new OutputFormat(OutputContainerFormat.Raw, null),
            new SchemaFilterOptions(include ?? [], []),
            new DiagramRenderOptions(
                "Database schema",
                DiagramDirection.LR,
                DiagramStyle.Classic,
                true,
                new DiagramLayoutOptions(columnLayout, tableHeaderLayout),
                new DiagramShowOptions(true, false, false, true, true, true, true, showTableComments, false),
                new MermaidRenderOptions(false),
                new DiagramCommentRenderOptions(null),
                ClassicDot()));

    private static GraphvizDotRenderOptions ClassicDot() =>
        new(
            new GraphvizDotGraphRenderOptions(null, null, null, null, null),
            new GraphvizDotNodeRenderOptions(null, null),
            new GraphvizDotEdgeRenderOptions(null, null, null, null, null),
            new GraphvizDotTableRenderOptions(null, null, null));

    private static ResolvedGenerateOptions Options(
        params ResolvedDiagramTarget[] diagrams) =>
        Options(diagrams, false, false, false);

    private static ResolvedGenerateOptions Options(
        ResolvedDiagramTarget diagram,
        bool quiet = false,
        bool noProgress = false,
        bool dryRun = false) =>
        Options([diagram], quiet, noProgress, dryRun);

    private static ResolvedGenerateOptions Options(
        ResolvedDiagramTarget[] diagrams,
        bool quiet = false,
        bool noProgress = false,
        bool dryRun = false) =>
        new(
            "sqlserver",
            "Server=test",
            false,
            quiet,
            noProgress,
            dryRun,
            false,
            null,
            new CommentOverridesConfig(),
            diagrams);

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
        public FakeReader Reader { get; } = new();

        public IDatabaseSchemaReader Create(string provider) => Reader;
    }

    private sealed class FakeReader : IDatabaseSchemaReader
    {
        public int ReadCount { get; private set; }

        public Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(new DatabaseModel(
                options.Provider,
                "Test",
                [
                    new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users"),
                    new TableModel("dbo", "Orders", [new ColumnModel("UserId", "int", false, false, true)])
                ],
                [
                    new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])
                ]));
        }
    }
}
