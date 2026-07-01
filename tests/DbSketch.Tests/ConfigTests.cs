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
              format: dot
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
              format: md-dot
            diagram:
              title: "App schema"
              rankdir: TB
              compact: true
              show:
                schemaName: false
                columnTypes: true
                nullability: true
                primaryKeys: true
                foreignKeys: false
            descriptions:
              enabled: false
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("postgres", config.Provider);
        Assert.Equal("public.*", Assert.Single(config.Include.Tables));
        Assert.Equal("public.audit_*", Assert.Single(config.Exclude.Tables));
        Assert.Equal("md-dot", config.Output.Format);
        Assert.Equal("App schema", config.Diagram.Title);
        Assert.False(config.Diagram.Show.SchemaName);
        Assert.True(config.Diagram.Show.ColumnTypes);
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
    public void CliArgsOverrideConfigValues()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Path = "config.dot", Format = "dot" }
        };
        var cli = new CliOptions(null, "postgresql", "Host=cli", "cli.md", "md-dot", true, true);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal("postgres", resolved.Provider);
        Assert.Equal("Host=cli", resolved.ConnectionString);
        Assert.Equal("cli.md", resolved.OutputPath);
        Assert.Equal(DiagramFormat.Dot, resolved.OutputFormat.DiagramFormat);
        Assert.True(resolved.OutputFormat.MarkdownWrapper);
        Assert.Equal("dot", resolved.OutputFormat.MarkdownFenceLanguage);
        Assert.True(resolved.Verbose);
        Assert.True(resolved.DryRun);
    }

    [Theory]
    [InlineData("mermaid", DiagramFormat.Mermaid, false, null)]
    [InlineData("md-mermaid", DiagramFormat.Mermaid, true, "mermaid")]
    [InlineData("md-graphviz", DiagramFormat.Dot, true, "graphviz")]
    public void ResolverAcceptsOutputFormats(string format, DiagramFormat expectedFormat, bool expectedMarkdownWrapper, string? expectedFenceLanguage)
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Path = "config.dot", Format = format }
        };
        var cli = new CliOptions(null, null, null, null, null, false, false);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.Equal(expectedFormat, resolved.OutputFormat.DiagramFormat);
        Assert.Equal(expectedMarkdownWrapper, resolved.OutputFormat.MarkdownWrapper);
        Assert.Equal(expectedFenceLanguage, resolved.OutputFormat.MarkdownFenceLanguage);
    }

    [Fact]
    public void ResolverEnablesReadDescriptionsWhenConfigEnablesDescriptions()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Descriptions = new DescriptionsConfig { Enabled = true }
        };
        var cli = new CliOptions(null, null, null, null, null, false, false);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.True(resolved.ReadDescriptions);
    }

    [Fact]
    public void ResolverDisablesReadDescriptionsWhenDescriptionsBlockIsAbsent()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config"
        };
        var cli = new CliOptions(null, null, null, null, null, false, false);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.False(resolved.ReadDescriptions);
    }

    [Fact]
    public void ResolverDisablesReadDescriptionsWhenConfigDisablesDescriptions()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Descriptions = new DescriptionsConfig { Enabled = false }
        };
        var cli = new CliOptions(null, null, null, null, null, false, false);

        var resolved = GenerateOptionsResolver.Resolve(config, cli);

        Assert.False(resolved.ReadDescriptions);
    }

    [Fact]
    public void ResolverRejectsUnknownFormat()
    {
        var config = new DbSketchConfig
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Output = new OutputConfig { Path = "config.dot", Format = "unknown" }
        };
        var cli = new CliOptions(null, null, null, null, null, false, false);

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, cli));

        Assert.Equal("Unknown format 'unknown'. Supported values: dot, md-dot, md-graphviz, mermaid, md-mermaid.", exception.Message);
    }

    private static string WriteTempConfig(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        return path;
    }
}
