using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class MermaidRendererTests
{
    [Fact]
    public void DoesNotEmitDirectionByDefault()
    {
        var mermaid = Render(Model());

        Assert.Contains("erDiagram", mermaid);
        Assert.DoesNotContain("direction LR", mermaid);
    }

    [Fact]
    public void EmitsDirectionWhenEnabled()
    {
        var mermaid = Render(Model(), emitDirection: true);

        Assert.Contains("erDiagram", mermaid);
        Assert.Contains("  direction LR", mermaid);
        Assert.Contains("erDiagram\n  direction LR\n", mermaid.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData(DiagramDirection.LR, "direction LR")]
    [InlineData(DiagramDirection.RL, "direction RL")]
    [InlineData(DiagramDirection.TB, "direction TB")]
    [InlineData(DiagramDirection.BT, "direction BT")]
    public void EmitsConfiguredDirectionWhenEnabled(DiagramDirection direction, string expected)
    {
        var mermaid = Render(Model(), direction: direction, emitDirection: true);

        Assert.Contains(expected, mermaid);
    }

    [Fact]
    public void GeneratesEntityBlocks()
    {
        var mermaid = Render(Model());

        Assert.Contains("  \"dbo.Users\" {", mermaid);
        Assert.Contains("  \"dbo.Orders\" {", mermaid);
    }

    [Fact]
    public void CanHideSchemaNames()
    {
        var mermaid = Render(Model(), showSchemaName: false);

        Assert.Contains("  \"Users\" {", mermaid);
        Assert.DoesNotContain("\"dbo.Users\" {", mermaid);
    }

    [Fact]
    public void WritesPkAndFkMarkers()
    {
        var mermaid = Render(Model(), showColumnTypes: true);

        Assert.Contains("int Id PK", mermaid);
        Assert.Contains("int UserId FK", mermaid);
    }

    [Fact]
    public void UsesGenericTypeWhenColumnTypesAreHidden()
    {
        var mermaid = Render(Model(), showColumnTypes: false);

        Assert.Contains("column Id PK", mermaid);
        Assert.Contains("column UserId FK", mermaid);
    }

    [Fact]
    public void WritesNullabilityMarkers()
    {
        var mermaid = Render(Model(), showColumnTypes: true, showNullability: true);

        Assert.Contains("int Id PK NOT_NULL", mermaid);
        Assert.Contains("nvarchar_100 Name NULL", mermaid);
    }

    [Fact]
    public void GeneratesForeignKeyRelationship()
    {
        var mermaid = Render(Model());

        Assert.Contains("\"dbo.Orders\" }|--|| \"dbo.Users\" : \"FK_Orders_Users\"", mermaid);
    }

    [Fact]
    public void NullableForeignKeyUsesOptionalSourceCardinality()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)]),
                new TableModel("dbo", "Orders", [new ColumnModel("UserId", "int", true, false, true)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);

        var mermaid = Render(model);

        Assert.Contains("\"dbo.Orders\" }o--|| \"dbo.Users\" : \"FK_Orders_Users\"", mermaid);
    }

    [Fact]
    public void NormalizesTypes()
    {
        var mermaid = Render(Model(), showColumnTypes: true);

        Assert.Contains("nvarchar_100 Name", mermaid);
        Assert.Contains("decimal_18_2 Total", mermaid);
    }

    [Fact]
    public void HandlesWeirdNamesWithSpacesAndSymbols()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("weird schema", "User \"Orders\"", [new ColumnModel("User Id", "timestamp with time zone", false, false, false), new ColumnModel("1st-value", "", true, false, false)])],
            []);

        var mermaid = Render(model, showColumnTypes: true);

        Assert.Contains("\"weird schema.User \\\"Orders\\\"\" {", mermaid);
        Assert.Contains("timestamp_with_time_zone User_Id", mermaid);
        Assert.Contains("unknown _1st_value", mermaid);
    }

    [Fact]
    public void DoesNotRenderCommentsByDefault()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User identifier")], "Application users")],
            []);

        var mermaid = Render(model, showColumnTypes: true);

        Assert.DoesNotContain("User identifier", mermaid);
        Assert.DoesNotContain("Application users", mermaid);
    }

    [Fact]
    public void RendersColumnCommentsWhenEnabled()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User identifier")])],
            []);

        var mermaid = Render(model, showColumnTypes: true, showComments: true);

        Assert.Contains("int Id PK \"User identifier\"", mermaid);
    }

    [Fact]
    public void EscapesColumnComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User \"identifier\"\nLine 2")])],
            []);

        var mermaid = Render(model, showColumnTypes: true, showComments: true);

        Assert.Contains("int Id PK \"User \\\"identifier\\\" Line 2\"", mermaid);
    }

    [Fact]
    public void DoesNotRenderTableComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users")],
            []);

        var mermaid = Render(model, showComments: true);

        Assert.DoesNotContain("Application users", mermaid);
    }

    private static string Render(
        DatabaseModel model,
        bool showSchemaName = true,
        bool showColumnTypes = false,
        bool showNullability = false,
        DiagramDirection direction = DiagramDirection.LR,
        bool emitDirection = false,
        bool showComments = false) =>
        new MermaidErRenderer().Render(
            model,
            new DiagramRenderOptions(
                "Database schema",
                direction,
                true,
                new DiagramShowOptions(showSchemaName, showColumnTypes, showNullability, true, true, showComments),
                new MermaidRenderOptions(emitDirection)));

    private static DatabaseModel Model() =>
        new(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("Name", "nvarchar(100)", true, false, false)]),
                new TableModel("dbo", "Orders", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("UserId", "int", false, false, true), new ColumnModel("Total", "decimal(18,2)", false, false, false)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);
}
