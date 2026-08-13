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
    public void RendersPrimaryKeyAndForeignKeyMarkersTogether()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "UserRoles", [new ColumnModel("UserId", "int", false, true, true)])],
            []);

        var mermaid = Render(model, showColumnTypes: true);

        Assert.Contains("int UserId PK, FK", mermaid);
    }

    [Fact]
    public void UsesGenericTypeWhenTypeTokenIsOmitted()
    {
        var mermaid = Render(Model(), columnLayout: "{name} | {keys}");

        Assert.Contains("column Id PK", mermaid);
        Assert.Contains("column UserId FK", mermaid);
    }

    [Fact]
    public void RendersFullNullabilityWhenNullabilityTokenIsConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [
                        new ColumnModel("Id", "int", false, true, false),
                        new ColumnModel("Name", "nvarchar(100)", true, false, false)
                    ])
            ],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {keys} | {nullability}");

        Assert.Contains("int Id PK \"NOT NULL\"", mermaid);
        Assert.Contains("nvarchar_100 Name \"NULL\"", mermaid);
    }

    [Fact]
    public void RendersNullableMarkerWhenNullableTokenIsConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [
                        new ColumnModel("Id", "int", false, true, false),
                        new ColumnModel("Name", "nvarchar(100)", true, false, false),
                        new ColumnModel("Email", "nvarchar(200)", false, false, false)
                    ])
            ],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {keys} | {nullable}");

        Assert.Contains("int Id PK", mermaid);
        Assert.Contains("nvarchar_100 Name \"NULL\"", mermaid);
        Assert.Contains("nvarchar_200 Email", mermaid);
        Assert.DoesNotContain("int Id PK \"NULL\"", mermaid);
        Assert.DoesNotContain("nvarchar_200 Email \"NULL\"", mermaid);
        Assert.DoesNotContain("NOT NULL", mermaid);
    }

    [Fact]
    public void DefaultColumnLayoutRendersNullableColumns()
    {
        var mermaid = Render(Model());

        Assert.Contains("nvarchar_100 Name \"NULL\"", mermaid);
        Assert.DoesNotContain("int Id PK \"NULL\"", mermaid);
    }

    [Fact]
    public void GeneratesForeignKeyRelationship()
    {
        var mermaid = Render(Model());

        Assert.Contains("\"dbo.Orders\" }|--|| \"dbo.Users\" : \"FK_Orders_Users\"", mermaid);
    }

    [Fact]
    public void CanHideForeignKeyLabels()
    {
        var mermaid = Render(Model(), showForeignKeyLabels: false);

        Assert.Contains("\"dbo.Orders\" }|--|| \"dbo.Users\" : \"\"", mermaid);
        Assert.DoesNotContain("\"FK_Orders_Users\"", mermaid);
    }

    [Fact]
    public void CanHideSelfReferencingForeignKeys()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Employees", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("ManagerId", "int", true, false, true)])],
            [new ForeignKeyModel("FK_Employees_Manager", new TableRef("dbo", "Employees"), ["ManagerId"], new TableRef("dbo", "Employees"), ["Id"])]);

        var mermaid = Render(model, showSelfReferencingForeignKeys: false);

        Assert.Contains("int ManagerId FK", mermaid);
        Assert.DoesNotContain("FK_Employees_Manager", mermaid);
        Assert.DoesNotContain("}o--||", mermaid);
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
    public void DoesNotRenderColumnCommentsByDefault()
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
    public void RendersColumnCommentsWhenCommentTokenIsConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User identifier")])],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {keys} | {comment}");

        Assert.Contains("int Id PK \"User identifier\"", mermaid);
    }

    [Fact]
    public void NullableMarkerTakesPrecedenceOverColumnCommentInMermaid()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [
                        new ColumnModel("Name", "nvarchar(100)", true, false, false, "Display name")
                    ])
            ],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {nullable} | {comment}");

        Assert.Contains("nvarchar_100 Name \"NULL\"", mermaid);
        Assert.DoesNotContain("NULL; Display name", mermaid);
        Assert.DoesNotContain("\"Display name\"", mermaid);
    }

    [Fact]
    public void NonNullableColumnCanStillRenderCommentWhenNullableAndCommentTokensAreConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel(
                    "dbo",
                    "Users",
                    [
                        new ColumnModel("Email", "nvarchar(200)", false, false, false, "Email address")
                    ])
            ],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {nullable} | {comment}");

        Assert.Contains("nvarchar_200 Email \"Email address\"", mermaid);
        Assert.DoesNotContain("nvarchar_200 Email \"NULL\"", mermaid);
    }

    [Fact]
    public void EscapesColumnComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User \"identifier\"\nLine 2")])],
            []);

        var mermaid = Render(model, columnLayout: "{name} | {type} | {keys} | {comment}");

        Assert.Contains("int Id PK \"User 'identifier' Line 2\"", mermaid);
    }

    [Fact]
    public void RendersTableCommentsInEntityAliases()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users")],
            []);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("dbo_Users[\"dbo.Users (Application users)\"] {", mermaid);
    }

    [Fact]
    public void DoesNotRenderTableCommentsWhenDisabled()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users")],
            []);

        var withoutTableComments = Render(model);
        var withTableComments = Render(model, showTableComments: true);

        Assert.Contains("\"dbo.Users\" {", withoutTableComments);
        Assert.DoesNotContain("Application users", withoutTableComments);
        Assert.NotEqual(withoutTableComments, withTableComments);
    }

    [Fact]
    public void DoesNotCreateAliasForWhitespaceTableComment()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], " \r\n ")],
            []);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("\"dbo.Users\" {", mermaid);
        Assert.DoesNotContain("dbo_Users[", mermaid);
    }

    [Fact]
    public void RendersTableCommentsWithoutSchemaName()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users")],
            []);

        var mermaid = Render(model, showSchemaName: false, showTableComments: true);

        Assert.Contains("dbo_Users[\"Users (Application users)\"] {", mermaid);
    }

    [Fact]
    public void NormalizesTruncatesAndEscapesTableComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "User \"identifier\"\nLine 2")],
            []);

        var mermaid = Render(model, showTableComments: true, maxCommentLength: 18);

        Assert.Contains("dbo_Users[\"dbo.Users (User \\\"identifier\\\"…)\"] {", mermaid);
    }

    [Fact]
    public void PreservesUnicodeTableComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Пользователи 👤")],
            []);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("dbo_Users[\"dbo.Users (Пользователи 👤)\"] {", mermaid);
    }

    [Fact]
    public void RelationshipsUseEntityReferencesForCommentedTables()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users"),
                new TableModel("dbo", "Orders", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("UserId", "int", false, false, true)], "Customer orders")
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("dbo_Orders }|--|| dbo_Users : \"FK_Orders_Users\"", mermaid);
        Assert.DoesNotContain("dbo.Orders (Customer orders) }|", mermaid);
    }

    [Fact]
    public void RelationshipsSupportOneCommentedTable()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)], "Application users"),
                new TableModel("dbo", "Orders", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("UserId", "int", false, false, true)])
            ],
            [new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])]);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("\"dbo.Orders\" }|--|| dbo_Users : \"FK_Orders_Users\"", mermaid);
    }

    [Fact]
    public void CreatesDistinctDeterministicIdentifiersForCollidingTableNames()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [
                new TableModel("dbo", "A-B", [new ColumnModel("Id", "int", false, true, false), new ColumnModel("ParentId", "int", false, false, true)], "First"),
                new TableModel("dbo", "A B", [new ColumnModel("Id", "int", false, true, false)], "Second")
            ],
            [new ForeignKeyModel("FK_A_B", new TableRef("dbo", "A-B"), ["ParentId"], new TableRef("dbo", "A B"), ["Id"])]);

        var mermaid = Render(model, showTableComments: true);

        Assert.Contains("dbo_A_B[\"dbo.A B (Second)\"] {", mermaid);
        Assert.Contains("dbo_A_B_2[\"dbo.A-B (First)\"] {", mermaid);
        Assert.Contains("dbo_A_B_2 }|--|| dbo_A_B : \"FK_A_B\"", mermaid);
    }

    [Fact]
    public void TruncatesColumnCommentsWhenMaxLengthIsConfigured()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "Long column comment")])],
            []);

        var mermaid = Render(model, maxCommentLength: 10, columnLayout: "{name} | {type} | {keys} | {comment}");

        Assert.Contains("int Id PK \"Long colu…\"", mermaid);
        Assert.DoesNotContain("Long column comment", mermaid);
    }

    [Fact]
    public void EscapesTruncatedColumnComments()
    {
        var model = new DatabaseModel(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "User \"identifier\" with long text")])],
            []);

        var mermaid = Render(model, maxCommentLength: 18, columnLayout: "{name} | {type} | {keys} | {comment}");

        Assert.Contains("int Id PK \"User 'identifier'…\"", mermaid);
    }

    [Fact]
    public void UsesColumnLayoutProjection()
    {
        var withoutKeys = Render(Model(), columnLayout: "{name} | {type}");
        var withKeys = Render(Model(), columnLayout: "{name}: {type} | {pk} | {fk}");

        Assert.DoesNotContain("int Id PK", withoutKeys);
        Assert.Contains("int Id PK", withKeys);
        Assert.DoesNotContain("Id: int", withKeys);
    }

    [Fact]
    public void IgnoresTableHeaderLayout()
    {
        var withoutLayout = Render(Model());
        var withLayout = Render(Model(), tableHeaderLayout: "{schema} | {table}");

        Assert.Equal(withoutLayout, withLayout);
        Assert.DoesNotContain("dbo | Users", withLayout);
    }

    private static string Render(
        DatabaseModel model,
        bool showSchemaName = true,
        bool showColumnTypes = false,
        bool showNullability = false,
        DiagramDirection direction = DiagramDirection.LR,
        bool emitDirection = false,
        bool showForeignKeyLabels = true,
        bool showSelfReferencingForeignKeys = true,
        bool showTableComments = false,
        bool showColumnComments = false,
        int? maxCommentLength = null,
        string? columnLayout = null,
        string? tableHeaderLayout = null) =>
        new MermaidErRenderer().Render(
            model,
            new DiagramRenderOptions(
                "Database schema",
                direction,
                DiagramStyle.Classic,
                true,
                new DiagramLayoutOptions(columnLayout ?? "{name} | {type} | {keys} | {nullable}", tableHeaderLayout),
                new DiagramShowOptions(showSchemaName, showColumnTypes, showNullability, true, true, showForeignKeyLabels, showSelfReferencingForeignKeys, showTableComments, showColumnComments),
                new MermaidRenderOptions(emitDirection),
                new DiagramCommentRenderOptions(maxCommentLength),
                new GraphvizDotRenderOptions(
                    new GraphvizDotGraphRenderOptions(null, null, null, null, null),
                    new GraphvizDotNodeRenderOptions(null, null),
                    new GraphvizDotEdgeRenderOptions(null, null, null, null, null),
                    new GraphvizDotTableRenderOptions(null, null, null))));

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
