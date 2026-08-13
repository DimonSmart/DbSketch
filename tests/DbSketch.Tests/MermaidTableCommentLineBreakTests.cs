using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class MermaidTableCommentLineBreakTests
{
    [Fact]
    public void RendersTableCommentOnNewLineWhenEnabled()
    {
        var mermaid = new MermaidErRenderer().Render(Model(), Options(tableCommentsOnNewLine: true));

        Assert.Contains("dbo_Customers[\"dbo.Customers<br>Основная таблица\"] {", mermaid);
    }

    [Fact]
    public void KeepsInlineTableCommentByDefault()
    {
        var mermaid = new MermaidErRenderer().Render(Model(), Options());

        Assert.Contains("dbo_Customers[\"dbo.Customers (Основная таблица)\"] {", mermaid);
        Assert.DoesNotContain("<br>", mermaid);
    }

    [Fact]
    public void LineBreakOptionDoesNotEnableTableComments()
    {
        var mermaid = new MermaidErRenderer().Render(
            Model(),
            Options(tableCommentsOnNewLine: true, showTableComments: false));

        Assert.Contains("\"dbo.Customers\" {", mermaid);
        Assert.DoesNotContain("Основная таблица", mermaid);
    }

    [Fact]
    public void MermaidOptionInheritsFromDefaultsAndSupportsPerDiagramOverride()
    {
        var config = new DbSketchConfig
        {
            Provider = "postgres",
            ConnectionString = "Host=localhost",
            Defaults = new DefaultsConfig
            {
                Diagram = new DiagramConfig
                {
                    Mermaid = new MermaidConfig { TableCommentsOnNewLine = true }
                }
            },
            Diagrams =
            [
                new DiagramTargetConfig { Name = "inherited" },
                new DiagramTargetConfig
                {
                    Name = "override",
                    Diagram = new DiagramOverrideConfig
                    {
                        Mermaid = new MermaidOverrideConfig { TableCommentsOnNewLine = false }
                    }
                }
            ]
        };

        var resolved = GenerateOptionsResolver.Resolve(
            config,
            new CliOptions("dbsketch.yaml", null, false, false, false, false));

        Assert.True(resolved.Diagrams[0].Diagram.Mermaid.TableCommentsOnNewLine);
        Assert.False(resolved.Diagrams[1].Diagram.Mermaid.TableCommentsOnNewLine);
    }

    private static DatabaseModel Model() =>
        new(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Customers",
                    [new ColumnModel("Id", "int", false, true, false)],
                    "Основная таблица")
            ],
            []);

    private static DiagramRenderOptions Options(
        bool tableCommentsOnNewLine = false,
        bool showTableComments = true) =>
        new(
            "Database schema",
            DiagramDirection.LR,
            DiagramStyle.Classic,
            true,
            new DiagramLayoutOptions("{name} | {type} | {keys} | {nullable}", null),
            new DiagramShowOptions(true, false, false, true, true, true, true, showTableComments, false),
            new MermaidRenderOptions(false, tableCommentsOnNewLine),
            new DiagramCommentRenderOptions(null),
            new GraphvizDotRenderOptions(
                new GraphvizDotGraphRenderOptions(null, null, null, null, null),
                new GraphvizDotNodeRenderOptions(null, null),
                new GraphvizDotEdgeRenderOptions(null, null, null, null, null),
                new GraphvizDotTableRenderOptions(null, null, null)));
}
