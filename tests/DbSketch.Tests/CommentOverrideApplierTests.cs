using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Tests;

public sealed class CommentOverrideApplierTests
{
    [Fact]
    public void OverridesTableComment()
    {
        var result = CommentOverrideApplier.Apply(Model(), Overrides(new TableCommentOverrideConfig { Schema = "dbo", Name = "Users", Comment = "YAML table comment" }));

        Assert.Equal("YAML table comment", result.Tables.Single().Comment);
    }

    [Fact]
    public void OverridesColumnComment()
    {
        var result = CommentOverrideApplier.Apply(
            Model(),
            Overrides(new TableCommentOverrideConfig
            {
                Schema = "dbo",
                Name = "Users",
                Columns = new Dictionary<string, string?> { ["id"] = "YAML column comment" }
            }));

        Assert.Equal("YAML column comment", result.Tables.Single().Columns.Single().Comment);
    }

    [Fact]
    public void KeepsDatabaseCommentWhenOverrideIsMissing()
    {
        var result = CommentOverrideApplier.Apply(Model(), new CommentOverridesConfig());

        Assert.Equal("DB table comment", result.Tables.Single().Comment);
        Assert.Equal("DB column comment", result.Tables.Single().Columns.Single().Comment);
    }

    [Fact]
    public void IgnoresEmptyOverrideComment()
    {
        var result = CommentOverrideApplier.Apply(
            Model(),
            Overrides(new TableCommentOverrideConfig
            {
                Schema = "dbo",
                Name = "Users",
                Comment = " ",
                Columns = new Dictionary<string, string?> { ["Id"] = "" }
            }));

        Assert.Equal("DB table comment", result.Tables.Single().Comment);
        Assert.Equal("DB column comment", result.Tables.Single().Columns.Single().Comment);
    }

    [Fact]
    public void AppliesOverridesWhenDatabaseCommentsAreDisabled()
    {
        var model = new DatabaseModel("sqlserver", null, [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)])], []);

        var result = CommentOverrideApplier.Apply(
            model,
            Overrides(new TableCommentOverrideConfig
            {
                Schema = "dbo",
                Name = "Users",
                Comment = "YAML table comment",
                Columns = new Dictionary<string, string?> { ["Id"] = "YAML column comment" }
            }));

        Assert.Equal("YAML table comment", result.Tables.Single().Comment);
        Assert.Equal("YAML column comment", result.Tables.Single().Columns.Single().Comment);
    }

    [Fact]
    public void IgnoresUnknownTableOverride()
    {
        var result = CommentOverrideApplier.Apply(Model(), Overrides(new TableCommentOverrideConfig { Schema = "dbo", Name = "Orders", Comment = "Unknown" }));

        Assert.Equal("DB table comment", result.Tables.Single().Comment);
    }

    [Fact]
    public void IgnoresUnknownColumnOverride()
    {
        var result = CommentOverrideApplier.Apply(
            Model(),
            Overrides(new TableCommentOverrideConfig
            {
                Schema = "dbo",
                Name = "Users",
                Columns = new Dictionary<string, string?> { ["Missing"] = "Unknown" }
            }));

        Assert.Equal("DB column comment", result.Tables.Single().Columns.Single().Comment);
    }

    [Fact]
    public void RejectsDuplicateTableOverrides()
    {
        var overrides = new CommentOverridesConfig
        {
            Tables =
            [
                new TableCommentOverrideConfig { Schema = "dbo", Name = "Users" },
                new TableCommentOverrideConfig { Schema = "DBO", Name = "users" }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => CommentOverrideApplier.Apply(Model(), overrides));

        Assert.Equal("Duplicate comment override for table 'DBO.users'.", exception.Message);
    }

    private static CommentOverridesConfig Overrides(TableCommentOverrideConfig tableOverride) =>
        new() { Tables = [tableOverride] };

    private static DatabaseModel Model() =>
        new(
            "sqlserver",
            null,
            [new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false, "DB column comment")], "DB table comment")],
            []);
}
