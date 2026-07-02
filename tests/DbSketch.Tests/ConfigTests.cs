using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void ReadsMultiDiagramYamlConfig()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            defaults:
              output:
                format: markdown
                markdown:
                  fenceLanguage: dot
                  header: |
                    # App schema
              diagram:
                renderer: dot
                direction: LR
                compact: true
                show:
                  schemaName: false
                  columnTypes: true
                  foreignKeyLabels: false
                  selfReferencingForeignKeys: false
                  tableComments: true
                comments:
                  maxLength: 80
            comments:
              enabled: true
              overrides:
                tables:
                  - schema: dbo
                    name: Users
                    comment: Application users
                    columns:
                      Id: User identifier
            diagrams:
              - name: full
                title: Full schema
                include:
                  tables:
                    - "public.*"
                exclude:
                  tables:
                    - "public.audit_*"
                output:
                  path: docs/db/full.md
              - name: auth
                diagram:
                  renderer: mermaid
                  mermaid:
                    emitDirection: true
                output:
                  path: docs/db/auth.mmd
                  format: raw
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("postgres", config.Provider);
        Assert.Equal("Host=localhost", config.ConnectionString);
        Assert.Equal("markdown", config.Defaults.Output.Format);
        Assert.Equal("dot", config.Defaults.Output.Markdown.FenceLanguage);
        Assert.Contains("# App schema", config.Defaults.Output.Markdown.Header);
        Assert.Equal("dot", config.Defaults.Diagram.Renderer);
        Assert.False(config.Defaults.Diagram.Show.SchemaName);
        Assert.True(config.Defaults.Diagram.Show.ColumnTypes);
        Assert.False(config.Defaults.Diagram.Show.ForeignKeyLabels);
        Assert.False(config.Defaults.Diagram.Show.SelfReferencingForeignKeys);
        Assert.True(config.Defaults.Diagram.Show.TableComments);
        Assert.Equal(80, config.Defaults.Diagram.Comments.MaxLength);
        Assert.True(config.Comments.Enabled);
        Assert.Equal(2, config.Diagrams.Count);
        Assert.Equal("full", config.Diagrams[0].Name);
        Assert.Equal("public.*", Assert.Single(config.Diagrams[0].Include.Tables));
        Assert.Equal("public.audit_*", Assert.Single(config.Diagrams[0].Exclude.Tables));
        Assert.Equal("docs/db/full.md", config.Diagrams[0].Output?.Path);
        Assert.Equal("mermaid", config.Diagrams[1].Diagram?.Renderer);
        Assert.True(config.Diagrams[1].Diagram?.Mermaid?.EmitDirection);

        var tableOverride = Assert.Single(config.Comments.Overrides.Tables);
        Assert.Equal("dbo", tableOverride.Schema);
        Assert.Equal("Users", tableOverride.Name);
        Assert.Equal("Application users", tableOverride.Comment);
        Assert.Equal("User identifier", tableOverride.Columns["Id"]);
    }

    [Fact]
    public void ExpandsEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("DBSKETCH_TEST_CONNECTION", "Host=test");

        var expanded = ConfigLoader.ExpandEnvironmentVariables("connectionString: ${DBSKETCH_TEST_CONNECTION}");

        Assert.Equal("connectionString: Host=test", expanded);
    }

    [Fact]
    public void LoadsEnvironmentVariableFallbackInYaml()
    {
        Environment.SetEnvironmentVariable("DBSKETCH_TEST_FALLBACK_CONNECTION", null);
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: "${DBSKETCH_TEST_FALLBACK_CONNECTION:-Host=localhost;Database=app}"
            diagrams:
              - name: full
                output:
                  path: docs/db/full.dot
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("Host=localhost;Database=app", config.ConnectionString);
    }

    [Fact]
    public void ThrowsReadableErrorWhenEnvVarIsMissing()
    {
        Environment.SetEnvironmentVariable("DBSKETCH_MISSING_CONNECTION", null);

        var exception = Assert.Throws<CliException>(() => ConfigLoader.ExpandEnvironmentVariables("${DBSKETCH_MISSING_CONNECTION}"));

        Assert.Equal("Environment variable 'DBSKETCH_MISSING_CONNECTION' is not defined.", exception.Message);
    }

    [Fact]
    public void RejectsOldTopLevelSingleDiagramConfig()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            output:
              path: docs/db/schema.dot
            diagram:
              renderer: dot
            include:
              tables:
                - "public.*"
            """);

        var exception = Assert.Throws<CliException>(() => ConfigLoader.Load(path));

        Assert.Contains("output", exception.Message);
    }

    [Theory]
    [InlineData(null, "--config is required.")]
    [InlineData("", "--config is required.")]
    public void ResolverRequiresConfigPath(string? configPath, string expectedMessage)
    {
        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(ValidConfig(), EmptyCli(configPath: configPath)));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void ResolverRejectsMissingProvider()
    {
        var config = ValidConfig() with { Provider = null };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("provider is required.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsMissingConnectionString()
    {
        var config = ValidConfig() with { ConnectionString = null };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("connectionString is required.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsMissingDiagrams()
    {
        var config = ValidConfig() with { Diagrams = [] };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams must contain at least one diagram.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsDiagramWithoutName()
    {
        var config = ValidConfig() with { Diagrams = [new DiagramTargetConfig { Output = new OutputOverrideConfig { Path = "schema.dot" } }] };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams[].name is required.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsDuplicateDiagramNames()
    {
        var config = ValidConfig() with
        {
            Diagrams =
            [
                Diagram("auth", "auth.dot"),
                Diagram("AUTH", "auth2.dot")
            ]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("Duplicate diagram name 'AUTH'.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsDiagramWithoutOutputPath()
    {
        var config = ValidConfig() with { Diagrams = [new DiagramTargetConfig { Name = "auth", Output = new OutputOverrideConfig() }] };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams['auth'].output.path is required.", exception.Message);
    }

    [Fact]
    public void ResolverCreatesMultipleDiagramTargets()
    {
        var config = ValidConfig() with
        {
            Diagrams =
            [
                Diagram("full", "full.dot", include: ["public.*"]),
                Diagram("auth", "auth.dot", include: ["public.Users"], exclude: ["public.audit_*"])
            ]
        };

        var resolved = GenerateOptionsResolver.Resolve(config, EmptyCli());

        Assert.Equal(2, resolved.Diagrams.Count);
        Assert.Equal("full", resolved.Diagrams[0].Name);
        Assert.Equal("full.dot", resolved.Diagrams[0].OutputPath);
        Assert.Equal("public.Users", Assert.Single(resolved.Diagrams[1].Filter.IncludeTables));
        Assert.Equal("public.audit_*", Assert.Single(resolved.Diagrams[1].Filter.ExcludeTables));
    }

    [Fact]
    public void DiagramInheritsRendererAndOutputFormatFromDefaults()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Output = new OutputDefaultsConfig { Format = "markdown" },
                Diagram = new DiagramConfig { Renderer = "mermaid" }
            }
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.Equal(DiagramFormat.Mermaid, diagram.DiagramRenderer);
        Assert.Equal(OutputContainerFormat.Markdown, diagram.Output.Format);
        Assert.Equal("mermaid", diagram.Output.Markdown?.FenceLanguage);
    }

    [Fact]
    public void DiagramOverridesRendererAndOutputFormat()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Output = new OutputDefaultsConfig { Format = "markdown" },
                Diagram = new DiagramConfig { Renderer = "dot" }
            },
            Diagrams =
            [
                Diagram(
                    "auth",
                    "auth.mmd",
                    outputFormat: "raw",
                    diagram: new DiagramOverrideConfig { Renderer = "mermaid", Direction = "TB", Compact = false })
            ]
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.Equal(DiagramFormat.Mermaid, diagram.DiagramRenderer);
        Assert.Equal(OutputContainerFormat.Raw, diagram.Output.Format);
        Assert.Equal(DiagramDirection.TB, diagram.Diagram.Direction);
        Assert.False(diagram.Diagram.Compact);
    }

    [Fact]
    public void DiagramShowOverridesInheritUnspecifiedDefaults()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Diagram = new DiagramConfig
                {
                    Show = new DiagramShowConfig { SchemaName = true, ColumnTypes = false, ForeignKeys = true, ForeignKeyLabels = false, SelfReferencingForeignKeys = false }
                }
            },
            Diagrams =
            [
                Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig { Show = new DiagramShowOverrideConfig { ColumnTypes = true } })
            ]
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.True(diagram.Diagram.Show.SchemaName);
        Assert.True(diagram.Diagram.Show.ColumnTypes);
        Assert.True(diagram.Diagram.Show.ForeignKeys);
        Assert.False(diagram.Diagram.Show.ForeignKeyLabels);
        Assert.False(diagram.Diagram.Show.SelfReferencingForeignKeys);
    }

    [Fact]
    public void SelectsOnlyRequestedDiagramCaseInsensitive()
    {
        var config = ValidConfig() with
        {
            Diagrams =
            [
                Diagram("full", "full.dot"),
                Diagram("auth", "auth.dot")
            ]
        };

        var resolved = GenerateOptionsResolver.Resolve(config, EmptyCli(diagramName: "AUTH"));

        var diagram = Assert.Single(resolved.Diagrams);
        Assert.Equal("auth", diagram.Name);
    }

    [Fact]
    public void RejectsUnknownRequestedDiagramWithAvailableNames()
    {
        var config = ValidConfig() with
        {
            Diagrams =
            [
                Diagram("full", "full.dot"),
                Diagram("sales", "sales.dot"),
                Diagram("audit", "audit.dot")
            ]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli(diagramName: "auth")));

        Assert.Equal("Diagram 'auth' was not found. Available diagrams: full, sales, audit.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnknownRenderer()
    {
        var config = ValidConfig() with
        {
            Diagrams = [Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig { Renderer = "plantuml" })]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("Unknown diagram renderer 'plantuml'. Supported values: dot, mermaid.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnknownOutputFormat()
    {
        var config = ValidConfig() with
        {
            Diagrams = [Diagram("auth", "auth.dot", outputFormat: "unknown")]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("Unknown output format 'unknown'. Supported values: raw, markdown.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsNonPositiveCommentMaxLength()
    {
        var config = ValidConfig() with
        {
            Diagrams = [Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig { Comments = new DiagramCommentsOverrideConfig { MaxLength = 0 } })]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams['auth'].diagram.comments.maxLength must be greater than zero.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsCommentOverrideWithoutSchema()
    {
        var config = ValidConfig() with
        {
            Comments = new CommentsConfig
            {
                Overrides = new CommentOverridesConfig { Tables = [new TableCommentOverrideConfig { Name = "Users" }] }
            }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("comments.overrides.tables[].schema is required.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsDuplicateCommentOverrides()
    {
        var config = ValidConfig() with
        {
            Comments = new CommentsConfig
            {
                Overrides = new CommentOverridesConfig
                {
                    Tables =
                    [
                        new TableCommentOverrideConfig { Schema = "dbo", Name = "Users" },
                        new TableCommentOverrideConfig { Schema = "DBO", Name = "users" }
                    ]
                }
            }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("Duplicate comment override for table 'DBO.users'.", exception.Message);
    }

    [Fact]
    public void ResolverUsesConfiguredCommandTimeout()
    {
        var config = ValidConfig() with { Database = new DatabaseConfig { CommandTimeoutSeconds = 30 } };

        var resolved = GenerateOptionsResolver.Resolve(config, EmptyCli());

        Assert.Equal(30, resolved.CommandTimeoutSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolverRejectsNonPositiveCommandTimeout(int timeout)
    {
        var config = ValidConfig() with { Database = new DatabaseConfig { CommandTimeoutSeconds = timeout } };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("database.commandTimeoutSeconds must be greater than zero.", exception.Message);
    }

    [Fact]
    public void ReadsLayoutYamlConfig()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            defaults:
              diagram:
                columnLayout: "{name}: {type} | {pk} | {fk}"
                tableHeaderLayout: "{schema}.{table} | {comment}"
            diagrams:
              - name: auth
                diagram:
                  columnLayout: "{name} | {keys}"
                  tableHeaderLayout: "{schema} | {table}"
                output:
                  path: docs/db/auth.dot
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("{name}: {type} | {pk} | {fk}", config.Defaults.Diagram.ColumnLayout);
        Assert.Equal("{schema}.{table} | {comment}", config.Defaults.Diagram.TableHeaderLayout);
        Assert.Equal("{name} | {keys}", config.Diagrams[0].Diagram?.ColumnLayout);
        Assert.Equal("{schema} | {table}", config.Diagrams[0].Diagram?.TableHeaderLayout);
    }

    [Fact]
    public void ReadsDotReadableYamlConfig()
    {
        var path = WriteTempConfig("""
            provider: postgres
            connectionString: Host=localhost
            defaults:
              diagram:
                style: readable
                dot:
                  edge:
                    color: "#555555"
            diagrams:
              - name: auth
                output:
                  path: docs/db/auth.dot
            """);

        var config = ConfigLoader.Load(path);
        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli(configPath: path)).Diagrams);

        Assert.Equal("readable", config.Defaults.Diagram.Style);
        Assert.Equal("#555555", config.Defaults.Diagram.Dot.Edge.Color);
        Assert.Equal(DiagramStyle.Readable, diagram.Diagram.Style);
        Assert.Equal("Helvetica", diagram.Diagram.Dot.Graph.FontName);
        Assert.Equal(16, diagram.Diagram.Dot.Graph.FontSize);
        Assert.Equal(0.55, diagram.Diagram.Dot.Graph.Nodesep);
        Assert.Equal("#555555", diagram.Diagram.Dot.Edge.Color);
        Assert.Equal(4, diagram.Diagram.Dot.Table.CellPadding);
    }

    [Fact]
    public void DiagramDotOverridesDefaults()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Diagram = new DiagramConfig
                {
                    Style = "readable",
                    Dot = new DotConfig { Edge = new DotEdgeConfig { Color = "#555555" } }
                }
            },
            Diagrams =
            [
                Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig
                {
                    Style = "classic",
                    Dot = new DotOverrideConfig { Edge = new DotEdgeOverrideConfig { Color = "#333333" } }
                })
            ]
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.Equal(DiagramStyle.Classic, diagram.Diagram.Style);
        Assert.Equal("#333333", diagram.Diagram.Dot.Edge.Color);
        Assert.Null(diagram.Diagram.Dot.Graph.FontName);
    }

    [Fact]
    public void DiagramLayoutInheritsDefaultsWhenOverrideIsNotSet()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Diagram = new DiagramConfig
                {
                    ColumnLayout = "{name}: {type}",
                    TableHeaderLayout = "{fullName}"
                }
            }
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.Equal("{name}: {type}", diagram.Diagram.Layout.ColumnLayout);
        Assert.Equal("{fullName}", diagram.Diagram.Layout.TableHeaderLayout);
    }

    [Fact]
    public void DiagramLayoutOverridesDefaults()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig
            {
                Diagram = new DiagramConfig
                {
                    ColumnLayout = "{name}: {type}",
                    TableHeaderLayout = "{fullName}"
                }
            },
            Diagrams =
            [
                Diagram(
                    "auth",
                    "auth.dot",
                    diagram: new DiagramOverrideConfig
                    {
                        ColumnLayout = "{name} | {keys}",
                        TableHeaderLayout = "{schema} | {table}"
                    })
            ]
        };

        var diagram = Assert.Single(GenerateOptionsResolver.Resolve(config, EmptyCli()).Diagrams);

        Assert.Equal("{name} | {keys}", diagram.Diagram.Layout.ColumnLayout);
        Assert.Equal("{schema} | {table}", diagram.Diagram.Layout.TableHeaderLayout);
    }

    [Fact]
    public void ResolverRejectsUnknownColumnLayoutToken()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { ColumnLayout = "{name} | {foo}" } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.columnLayout contains unknown token '{foo}'. Supported tokens: {name}, {type}, {nullability}, {pk}, {fk}, {keys}, {comment}.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnknownTableHeaderLayoutToken()
    {
        var config = ValidConfig() with
        {
            Diagrams = [Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig { TableHeaderLayout = "{fullName} | {foo}" })]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams['auth'].diagram.tableHeaderLayout contains unknown token '{foo}'. Supported tokens: {schema}, {table}, {name}, {fullName}, {comment}.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsEmptyColumnLayout()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { ColumnLayout = "  " } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.columnLayout must not be empty.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsEmptyTableHeaderLayout()
    {
        var config = ValidConfig() with
        {
            Diagrams = [Diagram("auth", "auth.dot", diagram: new DiagramOverrideConfig { TableHeaderLayout = "" })]
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("diagrams['auth'].diagram.tableHeaderLayout must not be empty.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnknownStyle()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { Style = "fancy" } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.style must be one of: classic, readable.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsInvalidDotColor()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { Dot = new DotConfig { Edge = new DotEdgeConfig { Color = "red" } } } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.dot.edge.color must be a hex color like #666666.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsInvalidDotFontSize()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { Dot = new DotConfig { Node = new DotNodeConfig { FontSize = 1000 } } } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.dot.node.fontSize must be from 6 to 72.", exception.Message);
    }

    [Fact]
    public void ResolverRejectsUnsafeDotFont()
    {
        var config = ValidConfig() with
        {
            Defaults = new DefaultsConfig { Diagram = new DiagramConfig { Dot = new DotConfig { Graph = new DotGraphConfig { FontName = "<script>" } } } }
        };

        var exception = Assert.Throws<CliException>(() => GenerateOptionsResolver.Resolve(config, EmptyCli()));

        Assert.Equal("defaults.diagram.dot.graph.fontName must contain only letters, digits, spaces, '_', '-', '.', and be at most 64 characters.", exception.Message);
    }

    private static DbSketchConfig ValidConfig() =>
        new()
        {
            Provider = "sqlserver",
            ConnectionString = "Server=config",
            Diagrams = [Diagram("full", "schema.dot")]
        };

    private static DiagramTargetConfig Diagram(
        string name,
        string path,
        IReadOnlyList<string>? include = null,
        IReadOnlyList<string>? exclude = null,
        string? outputFormat = null,
        DiagramOverrideConfig? diagram = null) =>
        new()
        {
            Name = name,
            Include = new IncludeExcludeConfig { Tables = include?.ToList() ?? [] },
            Exclude = new IncludeExcludeConfig { Tables = exclude?.ToList() ?? [] },
            Output = new OutputOverrideConfig { Path = path, Format = outputFormat },
            Diagram = diagram
        };

    private static CliOptions EmptyCli(string? configPath = "dbsketch.yml", string? diagramName = null) =>
        new(configPath, diagramName, false, false, false, false);

    private static string WriteTempConfig(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        return path;
    }
}
