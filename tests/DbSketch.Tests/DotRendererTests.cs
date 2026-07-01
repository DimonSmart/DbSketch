using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class DotRendererTests
{
    [Fact]
    public void GeneratesValidLookingDotHeader()
    {
        var dot = Render(Model());

        Assert.Contains("digraph DbSketch", dot);
        Assert.Contains("rankdir=LR", dot);
        Assert.Contains("shape=plain", dot);
    }

    [Fact]
    public void GeneratesOneNodePerTable()
    {
        var dot = Render(Model());

        Assert.Contains("\"table_dbo_Users\"", dot);
        Assert.Contains("\"table_dbo_Orders\"", dot);
    }

    [Fact]
    public void EscapesLabels()
    {
        var model = new DatabaseModel("sqlserver", null, [new TableModel("odd", "A&B<\"T\">", [new ColumnModel("Name&<", "text", true, false, false)])], []);

        var dot = Render(model);

        Assert.Contains("odd.A&amp;B&lt;&quot;T&quot;&gt;", dot);
        Assert.Contains("Name&amp;&lt;", dot);
    }

    [Fact]
    public void CreatesPortsForColumns()
    {
        var dot = Render(Model());

        Assert.Contains("PORT=\"col_UserId\"", dot);
    }

    [Fact]
    public void CreatesColumnToColumnFkEdge()
    {
        var dot = Render(Model());

        Assert.Contains("\"table_dbo_Orders\":\"col_UserId\" -> \"table_dbo_Users\":\"col_Id\"", dot);
    }

    [Fact]
    public void HandlesCompositeForeignKey()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Source", [new ColumnModel("A", "int", false, false, true), new ColumnModel("B", "int", false, false, true)]),
                new TableModel("dbo", "Target", [new ColumnModel("X", "int", false, true, false), new ColumnModel("Y", "int", false, true, false)])
            ],
            [new ForeignKeyModel("FK_Source_Target", new TableRef("dbo", "Source"), ["A", "B"], new TableRef("dbo", "Target"), ["X", "Y"])]);

        var dot = Render(model);

        Assert.Contains("\"table_dbo_Source\":\"col_A\" -> \"table_dbo_Target\":\"col_X\"", dot);
        Assert.Contains("\"table_dbo_Source\":\"col_B\" -> \"table_dbo_Target\":\"col_Y\"", dot);
    }

    [Fact]
    public void HandlesWeirdNamesWithSpacesAndSymbols()
    {
        var model = new DatabaseModel("sqlserver", null, [new TableModel("weird schema", "User-Orders", [new ColumnModel("User Id", "int", false, false, false)])], []);

        var dot = Render(model);

        Assert.Contains("\"table_weird_schema_User_Orders\"", dot);
        Assert.Contains("PORT=\"col_User_Id\"", dot);
    }

    [Fact]
    public void DoesNotDuplicatePortIdsAfterSanitizationConflict()
    {
        var model = new DatabaseModel("sqlserver", null, [new TableModel("dbo", "T", [new ColumnModel("A-B", "int", false, false, false), new ColumnModel("A B", "int", false, false, false)])], []);

        var dot = Render(model);

        Assert.Contains("PORT=\"col_A_B\"", dot);
        Assert.Contains("PORT=\"col_A_B_2\"", dot);
    }

    [Fact]
    public void IgnoresCommentsForNow()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [new ColumnModel("Id", "int", false, true, false, "User identifier")],
                    "Application users")
            ],
            []);

        var dot = Render(model);

        Assert.DoesNotContain("Application users", dot);
        Assert.DoesNotContain("User identifier", dot);
    }

    private static string Render(DatabaseModel model) =>
        new GraphvizDotRenderer().Render(model, new DiagramRenderOptions("Database schema", "LR", true, new DiagramShowOptions(true, false, false, true, true)));

    private static DatabaseModel Model() =>
        new(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("Name", "nvarchar(100)", true, false, false)]),
                new TableModel("dbo", "Orders", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("UserId", "int", false, false, true)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);
}
