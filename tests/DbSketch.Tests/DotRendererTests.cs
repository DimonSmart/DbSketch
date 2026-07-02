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
    public void DotRendererUsesDiagramDirection()
    {
        var dot = Render(Model(), direction: DiagramDirection.TB);

        Assert.Contains("rankdir=TB", dot);
    }

    [Fact]
    public void DotRendererIgnoresMermaidEmitDirection()
    {
        var dot = Render(Model(), direction: DiagramDirection.LR, mermaidEmitDirection: false);

        Assert.Contains("rankdir=LR", dot);
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

        Assert.Contains("<TD PORT=\"col_UserId\" ALIGN=\"LEFT\">UserId</TD><TD WIDTH=\"24\"></TD><TD PORT=\"col_UserId_fk\" WIDTH=\"24\" ALIGN=\"CENTER\"><FONT POINT-SIZE=\"9\">FK</FONT></TD>", dot);
    }

    [Fact]
    public void CreatesColumnToColumnFkEdge()
    {
        var dot = Render(Model());

        Assert.Contains("\"table_dbo_Orders\":\"col_UserId_fk\":e -> \"table_dbo_Users\":\"col_Id\":w", dot);
    }

    [Fact]
    public void RendersForeignKeyLabelsByDefault()
    {
        var dot = Render(Model());

        Assert.Contains("label=\"FK_Orders_Users\"", dot);
    }

    [Fact]
    public void CanHideForeignKeyLabels()
    {
        var dot = Render(Model(), showForeignKeyLabels: false);

        Assert.Contains("\"table_dbo_Orders\":\"col_UserId_fk\":e -> \"table_dbo_Users\":\"col_Id\":w;", dot);
        Assert.DoesNotContain("label=\"FK_Orders_Users\"", dot);
    }

    [Fact]
    public void CanHideSelfReferencingForeignKeys()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Employees", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("ManagerId", "int", true, false, true)])],
            [new ForeignKeyModel("FK_Employees_Manager", new TableRef("dbo", "Employees"), ["ManagerId"], new TableRef("dbo", "Employees"), ["Id"])]);

        var dot = Render(model, showSelfReferencingForeignKeys: false);

        Assert.Contains("<TD PORT=\"col_ManagerId\" ALIGN=\"LEFT\">ManagerId</TD><TD WIDTH=\"24\"></TD><TD PORT=\"col_ManagerId_fk\" WIDTH=\"24\" ALIGN=\"CENTER\"><FONT POINT-SIZE=\"9\">FK</FONT></TD>", dot);
        Assert.DoesNotContain("FK_Employees_Manager", dot);
        Assert.DoesNotContain("\"table_dbo_Employees\":\"col_ManagerId_fk\":e -> \"table_dbo_Employees\":\"col_Id\":w", dot);
    }

    [Fact]
    public void RendersPrimaryKeyAndForeignKeyMarkersInSeparateCells()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "UserRoles", [new ColumnModel("UserId", "int", false, true, true)])],
            []);

        var dot = Render(model);

        Assert.Contains("<TD PORT=\"col_UserId\" ALIGN=\"LEFT\">UserId</TD><TD WIDTH=\"24\" ALIGN=\"CENTER\"><FONT POINT-SIZE=\"9\">PK</FONT></TD><TD PORT=\"col_UserId_fk\" WIDTH=\"24\" ALIGN=\"CENTER\"><FONT POINT-SIZE=\"9\">FK</FONT></TD>", dot);
    }

    [Fact]
    public void LeftAlignsTableAndColumnText()
    {
        var dot = Render(Model());

        Assert.Contains("<TD COLSPAN=\"3\" BGCOLOR=\"#EEEEEE\" ALIGN=\"LEFT\"><B>dbo.Users</B></TD>", dot);
        Assert.Contains("<TD PORT=\"col_Name\" ALIGN=\"LEFT\">Name</TD>", dot);
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

        Assert.Contains("\"table_dbo_Source\":\"col_A_fk\":e -> \"table_dbo_Target\":\"col_X\":w", dot);
        Assert.Contains("\"table_dbo_Source\":\"col_B_fk\":e -> \"table_dbo_Target\":\"col_Y\":w", dot);
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
    public void DoesNotRenderCommentsByDefault()
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

    [Fact]
    public void RendersTableAndColumnCommentsWhenEnabled()
    {
        var model = CommentsModel();

        var dot = Render(model, showTableComments: true, showColumnComments: true);

        Assert.Contains("Application users", dot);
        Assert.Contains("User identifier", dot);
    }

    [Fact]
    public void RendersOnlyTableCommentsWhenEnabled()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [new ColumnModel("Id", "int", false, true, false, "User identifier")],
                    "Application users"),
                new TableModel(
                    "dbo",
                    "Orders",
                    [new ColumnModel("UserId", "int", false, false, true)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);

        var dot = Render(model, showTableComments: true);

        Assert.Contains("Application users", dot);
        Assert.DoesNotContain("User identifier", dot);
        Assert.Contains("PORT=\"col_Id\"", dot);
        Assert.Contains("\"table_dbo_Orders\":\"col_UserId_fk\":e -> \"table_dbo_Users\":\"col_Id\":w", dot);
    }

    [Fact]
    public void RendersOnlyColumnCommentsWhenEnabled()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [new ColumnModel("Id", "int", false, true, false, "User identifier")],
                    "Application users"),
                new TableModel(
                    "dbo",
                    "Orders",
                    [new ColumnModel("UserId", "int", false, false, true)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);

        var dot = Render(model, showColumnComments: true);

        Assert.DoesNotContain("Application users", dot);
        Assert.Contains("User identifier", dot);
        Assert.Contains("PORT=\"col_Id\"", dot);
        Assert.Contains("\"table_dbo_Orders\":\"col_UserId_fk\":e -> \"table_dbo_Users\":\"col_Id\":w", dot);
    }

    [Fact]
    public void TruncatesCommentsWhenMaxLengthIsConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [new ColumnModel("Id", "int", false, true, false, "Long column comment")],
                    "Long table comment")
            ],
            []);

        var dot = Render(model, showTableComments: true, showColumnComments: true, maxCommentLength: 10);

        Assert.Contains("Long tabl…", dot);
        Assert.Contains("Long colu…", dot);
        Assert.DoesNotContain("Long table comment", dot);
        Assert.DoesNotContain("Long column comment", dot);
    }

    [Fact]
    public void DoesNotTruncateCommentsByDefault()
    {
        var model = CommentsModel();

        var dot = Render(model, showTableComments: true, showColumnComments: true);

        Assert.Contains("Application users", dot);
        Assert.Contains("User identifier", dot);
    }

    [Fact]
    public void EscapesComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [new ColumnModel("Id", "int", false, true, false, "User <identifier> & key")],
                    "Application \"users\" table")
            ],
            []);

        var dot = Render(model, showTableComments: true, showColumnComments: true);

        Assert.Contains("Application &quot;users&quot; table", dot);
        Assert.Contains("User &lt;identifier&gt; &amp; key", dot);
        Assert.Contains("PORT=\"col_Id\"", dot);
    }

    [Fact]
    public void LayoutRendersOnlyPrimaryKeyMarkerWhenConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, true)])],
            []);

        var dot = Render(model, columnLayout: "{name} | {pk}");

        Assert.Contains("<TD PORT=\"col_Id\" ALIGN=\"LEFT\">Id</TD><TD ALIGN=\"CENTER\">PK</TD>", dot);
        Assert.DoesNotContain(">FK</TD>", dot);
        Assert.DoesNotContain("col_Id_fk", dot);
    }

    [Fact]
    public void LayoutRendersPrimaryKeyAndForeignKeyInSeparateCells()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "UserRoles", [new ColumnModel("UserId", "int", false, true, true)])],
            []);

        var dot = Render(model, columnLayout: "{name} | {pk} | {fk}");

        Assert.Contains("<TD PORT=\"col_UserId\" ALIGN=\"LEFT\">UserId</TD><TD ALIGN=\"CENTER\">PK</TD><TD PORT=\"col_UserId_fk\" ALIGN=\"CENTER\">FK</TD>", dot);
    }

    [Fact]
    public void LayoutRendersPrimaryKeyAndForeignKeyInOneCell()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "UserRoles", [new ColumnModel("UserId", "int", false, true, true)])],
            []);

        var dot = Render(model, columnLayout: "{name} | {keys}");

        Assert.Contains("<TD PORT=\"col_UserId\" ALIGN=\"LEFT\">UserId</TD><TD PORT=\"col_UserId_fk\" ALIGN=\"CENTER\">PK FK</TD>", dot);
    }

    [Fact]
    public void LayoutRendersColumnNameAndTypeInSeparateCells()
    {
        var dot = Render(Model(), columnLayout: "{name} | {type}");

        Assert.Contains("<TD PORT=\"col_Name\" ALIGN=\"LEFT\">Name</TD><TD ALIGN=\"CENTER\">nvarchar(100)</TD>", dot);
    }

    [Fact]
    public void LayoutRendersColumnNameAndTypeInOneCell()
    {
        var dot = Render(Model(), columnLayout: "{name}: {type}");

        Assert.Contains("<TD PORT=\"col_Name\" ALIGN=\"LEFT\">Name: nvarchar(100)</TD>", dot);
    }

    [Fact]
    public void LayoutRendersColumnNameAndTypeWithCustomSeparator()
    {
        var dot = Render(Model(), columnLayout: "{name} :: {type}");

        Assert.Contains("<TD PORT=\"col_Name\" ALIGN=\"LEFT\">Name :: nvarchar(100)</TD>", dot);
    }

    [Fact]
    public void LayoutRendersColumnCommentAndCleansEmptyCommentSeparator()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [
                        new ColumnModel("Id", "int", false, true, false, "User identifier"),
                        new ColumnModel("Name", "nvarchar(100)", true, false, false)
                    ])
            ],
            []);

        var dot = Render(model, columnLayout: "{name}: {type} - {comment} | {keys}");

        Assert.Contains("Id: int - User identifier", dot);
        Assert.Contains("<TD PORT=\"col_Name\" ALIGN=\"LEFT\">Name: nvarchar(100)</TD>", dot);
    }

    [Fact]
    public void LayoutTruncatesColumnComment()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "Long column comment")])],
            []);

        var dot = Render(model, columnLayout: "{name} - {comment}", maxCommentLength: 10);

        Assert.Contains("Id - Long colu…", dot);
        Assert.DoesNotContain("Long column comment", dot);
    }

    [Fact]
    public void LayoutEscapesHtmlSensitiveCharactersInColumnCells()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("A&B", "nvarchar<100>", true, false, false, "Use \"quotes\"")])],
            []);

        var dot = Render(model, columnLayout: "{name}: {type} | {comment}");

        Assert.Contains("A&amp;B: nvarchar&lt;100&gt;", dot);
        Assert.Contains("Use &quot;quotes&quot;", dot);
    }

    [Fact]
    public void LayoutForeignKeyEdgeUsesFkCellPortWhenSeparateCellExists()
    {
        var dot = Render(Model(), columnLayout: "{name}: {type} | {fk}");

        Assert.Contains("<TD PORT=\"col_UserId\" ALIGN=\"LEFT\">UserId: int</TD><TD PORT=\"col_UserId_fk\" ALIGN=\"CENTER\">FK</TD>", dot);
        Assert.Contains("\"table_dbo_Orders\":\"col_UserId_fk\":e -> \"table_dbo_Users\":\"col_Id\":w", dot);
    }

    [Fact]
    public void LayoutForeignKeyEdgeFallsBackToMainPortWhenNoFkCellExists()
    {
        var dot = Render(Model(), columnLayout: "{name}: {type}");

        Assert.DoesNotContain("col_UserId_fk", dot);
        Assert.Contains("\"table_dbo_Orders\":\"col_UserId\":e -> \"table_dbo_Users\":\"col_Id\":w", dot);
    }

    [Fact]
    public void HeaderLayoutRendersFullName()
    {
        var dot = Render(Model(), tableHeaderLayout: "{fullName}");

        Assert.Contains("<TABLE BORDER=\"0\" CELLBORDER=\"0\" CELLSPACING=\"0\"><TR><TD ALIGN=\"LEFT\">dbo.Users</TD></TR></TABLE>", dot);
    }

    [Fact]
    public void HeaderLayoutRendersSchemaAndTableInSeparateCells()
    {
        var dot = Render(Model(), tableHeaderLayout: "{schema} | {table}");

        Assert.Contains("<TD ALIGN=\"LEFT\">dbo</TD><TD ALIGN=\"LEFT\">Users</TD>", dot);
    }

    [Fact]
    public void HeaderLayoutRendersAndTruncatesTableComment()
    {
        var model = CommentsModel();

        var dot = Render(model, tableHeaderLayout: "{fullName} | {comment}", maxCommentLength: 10);

        Assert.Contains("<TD ALIGN=\"LEFT\">dbo.Users</TD><TD ALIGN=\"LEFT\">Applicati…</TD>", dot);
        Assert.DoesNotContain("Application users", dot);
    }

    [Fact]
    public void HeaderLayoutEscapesHtmlSensitiveCharacters()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "A&B", [new ColumnModel("Id", "int", false, true, false)], "Use <tags> & \"quotes\"")],
            []);

        var dot = Render(model, tableHeaderLayout: "{fullName} | {comment}");

        Assert.Contains("dbo.A&amp;B", dot);
        Assert.Contains("Use &lt;tags&gt; &amp; &quot;quotes&quot;", dot);
    }

    private static string Render(
        DatabaseModel model,
        DiagramDirection direction = DiagramDirection.LR,
        bool mermaidEmitDirection = false,
        bool showForeignKeyLabels = true,
        bool showSelfReferencingForeignKeys = true,
        bool showTableComments = false,
        bool showColumnComments = false,
        int? maxCommentLength = null,
        string? columnLayout = null,
        string? tableHeaderLayout = null) =>
        new GraphvizDotRenderer().Render(
            model,
            new DiagramRenderOptions(
                "Database schema",
                direction,
                true,
                new DiagramLayoutOptions(columnLayout, tableHeaderLayout),
                new DiagramShowOptions(true, false, false, true, true, showForeignKeyLabels, showSelfReferencingForeignKeys, showTableComments, showColumnComments),
                new MermaidRenderOptions(mermaidEmitDirection),
                new DiagramCommentRenderOptions(maxCommentLength)));

    private static DatabaseModel CommentsModel() =>
        new(
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
