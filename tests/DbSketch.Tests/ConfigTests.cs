using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void ReadsMinimalYamlConfig()
    {
        var path = WriteTempConfig("""
            provider: sqlserver
            connectionString: Server=.;Database=AppDb
            output:
              path: docs/db/schema.dot
              format: raw
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("sqlserver", config.Provider);
        Assert.Equal("Server=.;Database=AppDb", config.ConnectionString);
        Assert.Equal("docs/db/schema.dot", config.Output.Path);
    }

    [Fact]
    public void ReadsFullYamlConfig()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            include:
              tables:
                - "public.*"
            exclude:
              tables:
                - "public.audit_*"
            output:
              path: schema.md
              format: markdown
            diagram:
              renderer: mermaid
              title: "App schema"
              direction: LR
              compact: true
              mermaid:
                emitDirection: false
              show:
                schemaName: false
                columnTypes: true
                nullability: true
                primaryKeys: true
                foreignKeys: false
                comments: true
            comments:
              enabled: false
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("postgres", config.Provider);
        Assert.Equal("public.*", Assert.Single(config.Include.Tables));
        Assert.Equal("public.audit_*", Assert.Single(config.Exclude.Tables));
        Assert.Equal("markdown", config.Output.Format);
        Assert.Equal("mermaid", config.Diagram.Renderer);
        Assert.Equal("App schema", config.Diagram.Title);
        Assert.Equal("LR", config.Diagram.Direction);
        Assert.False(config.Diagram.Mermaid.EmitDirection);
        Assert.False(config.Diagram.Show.SchemaName);
        Assert.True(config.Diagram.Show.ColumnTypes);
        Assert.True(config.Diagram.Show.Comments);
        Assert.False(config.Comments.Enabled);
    }

    [Fact]
    public void ExpandsEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("DBSKETCH_TEST_CONNECTION", "Host=test");

        var expanded = ConfigLoader.ExpandEnvironmentVariables("connectionString: ${DBSKETCH_TEST_CONNECTION}");

        Assert.Equal("connectionString: Host=test", expanded);
    }

    [Fact]
    public void ThrowsReadableErrorWhenEnvVarIsMissing()
    {
        Environment.SetEnvironmentVariable("DBSKETCH_MISSING_CONNECTION", null);

        var exception = Assert.Throws<CliException>(() => ConfigLoader.ExpandEnvironmentVariables("${DBSKETCH_MISSING_CONNECTION}"));

        Assert.Equal("Environment variable 'DBSKETCH_MISSING_CONNECTION' is not defined.", exception.Message);
    }

    [Fact]
    public void RejectsOldRankdirConfigProperty()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            diagram:
              rankdir: LR
            """);

        var exception = Assert.Throws<CliException>(() => ConfigLoader.Load(path));

        Assert.Contains("rankdir", exception.Message);
    }

    [Fact]
    public void RejectsOldCommentsConfigAlias()
    {
        var oldKey = string.Concat("de", "scription", "s");
        var path = WriteTempConfig($$"""
            provider: postgres
            connectionString: Host=localhost
            {{oldKey}}:
              enabled: true
            """);

        var exception = Assert.Throws<CliException>(() => ConfigLoader.Load(path));

        Assert.Contains(oldKey, exception.Message);
    }

    [Fact]
    public void CliArgsOverrideConfigValues()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Path = "config.dot", Format = "raw" },
            Diagram = new DiagramConfig { Renderer = "dot" }
        };
        var cli = new CliOptions(null, "postgresql", "Host=cli", "cli.md", "mermaid", "markdown", true, true);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal("postgres", resolved.Provider);
        Assert.Equal("Host=cli", resolved.ConnectionString);
        Assert.Equal("cli.md", resolved.OutputPath);
        Assert.Equal(DiagramFormat.Mermaid, resolved.DiagramRenderer);
        Assert.Equal(OutputContainerFormat.Markdown, resolved.Output.Format);
        Assert.Equal("mermaid", resolved.Output.MarkdownFenceLanguage);
        Assert.True(resolved.Verbose);
        Assert.True(resolved.DryRun);
    }

    [Fact]
    public void ResolverUsesDiagramRendererFromConfig()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagram = new DiagramConfig { Renderer = "mermaid" }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(DiagramFormat.Mermaid, resolved.DiagramRenderer);
    }

    [Fact]
    public void ResolverUsesCliRendererOverride()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagram = new DiagramConfig { Renderer = "dot" }
        };
        var cli = new CliOptions(null, null, null, null, "mermaid", null, false, false);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(DiagramFormat.Mermaid, resolved.DiagramRenderer);
    }

    [Fact]
    public void ResolverUsesDiagramDirectionAndMermaidOptionsFromConfig()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagram = new DiagramConfig
            {
                Renderer = "mermaid",
                Direction = "TB",
                Mermaid = new MermaidConfig { EmitDirection = true },
                Show = new DiagramShowConfig { Comments = true }
            }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(DiagramFormat.Mermaid, resolved.DiagramRenderer);
        Assert.Equal(DiagramDirection.TB, resolved.Diagram.Direction);
        Assert.True(resolved.Diagram.Mermaid.EmitDirection);
        Assert.True(resolved.Diagram.Show.Comments);
    }

    [Fact]
    public void ResolverRejectsUnknownRenderer()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagram = new DiagramConfig { Renderer = "plantuml" }
        };
        var cli = EmptyCli();

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, cli));

        Assert.Equal("Unknown diagram renderer 'plantuml'. Supported values: dot, mermaid.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnknownDirection()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagram = new DiagramConfig { Direction = "LEFT" }
        };
        var cli = EmptyCli();

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, cli));

        Assert.Equal("Unknown diagram direction 'LEFT'. Supported values: TB, BT, LR, RL.", exception.Message);
    }

    [Fact]
    public void ResolverUsesMarkdownFenceLanguageFromRendererWhenOutputIsMarkdown()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Format = "markdown" },
            Diagram = new DiagramConfig { Renderer = "mermaid" }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(OutputContainerFormat.Markdown, resolved.Output.Format);
        Assert.Equal("mermaid", resolved.Output.MarkdownFenceLanguage);
    }

    [Fact]
    public void ResolverAllowsMarkdownFenceLanguageOverride()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Format = "markdown", MarkdownFenceLanguage = "graphviz" },
            Diagram = new DiagramConfig { Renderer = "dot" }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(OutputContainerFormat.Markdown, resolved.Output.Format);
        Assert.Equal("graphviz", resolved.Output.MarkdownFenceLanguage);
    }

    [Fact]
    public void ResolverEnablesReadCommentsWhenConfigEnablesComments()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Comments = new CommentsConfig { Enabled = true }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.True(resolved.ReadComments);
    }

    [Fact]
    public void ResolverDisablesReadCommentsWhenCommentsBlockIsAbsent()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config"
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.False(resolved.ReadComments);
    }

    [Fact]
    public void ResolverDisablesReadCommentsWhenConfigDisablesComments()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Comments = new CommentsConfig { Enabled = false }
        };
        var cli = EmptyCli();

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.False(resolved.ReadComments);
    }

    [Fact]
    public void ResolverRejectsUnknownOutputFormat()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Path = "config.dot", Format = "unknown" }
        };
        var cli = EmptyCli();

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, cli));

        Assert.Equal("Unknown output format 'unknown'. Supported values: raw, markdown.", exception.Message);
    }

    [Fact]
    public void CliParserReadsRendererAndFormat()
    {
        var cli = CliParser.ParseGenerate(["--renderer", "mermaid", "--format", "markdown"]);

        Assert.Equal("mermaid", cli.Renderer);
        Assert.Equal("markdown", cli.Format);
    }

    private static CliOptions EmptyCli() => new(null, null, null, null, null, null, false, false);

    private static string WriteTempConfig(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        return path;
    }
}
