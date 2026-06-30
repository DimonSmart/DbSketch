using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Tests;

public sealed class FilteringTests
{
    [Fact]
    public void IncludeEmptyMeansAllTables()
    {
        var filtered = Filter([], []);

        Assert.Equal(3, filtered.Tables.Count);
    }

    [Fact]
    public void IncludePatternIncludesOnlyMatchingTables()
    {
        var filtered = Filter(["dbo.*"], []);

        Assert.Equal(["dbo.Users", "dbo.Orders"], filtered.Tables.Select(table => table.FullName));
    }

    [Fact]
    public void ExcludeRemovesMatchedTables()
    {
        var filtered = Filter([], ["dbo.Orders"]);

        Assert.Equal(["dbo.Users", "sales.Orders"], filtered.Tables.Select(table => table.FullName));
    }

    [Fact]
    public void ExcludeIsAppliedAfterInclude()
    {
        var filtered = Filter(["*.Orders"], ["dbo.*"]);

        Assert.Equal("sales.Orders", Assert.Single(filtered.Tables).FullName);
    }

    [Fact]
    public void ForeignKeyIsRemovedWhenOneSideTableIsExcluded()
    {
        var filtered = Filter([], ["dbo.Users"]);

        Assert.Empty(filtered.ForeignKeys);
    }

    [Fact]
    public void WildcardMatchingIsCaseInsensitive()
    {
        var filtered = Filter(["DBO.u?ers"], []);

        Assert.Equal("dbo.Users", Assert.Single(filtered.Tables).FullName);
    }

    private static DatabaseModel Filter(IReadOnlyList<string> include, IReadOnlyList<string> exclude) =>
        new WildcardSchemaFilter().Apply(Model(), new SchemaFilterOptions(include, exclude));

    private static DatabaseModel Model() =>
        new(
            "sqlserver",
            "AppDb",
            [
                new TableModel("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)]),
                new TableModel("dbo", "Orders", [new ColumnModel("UserId", "int", false, false, true)]),
                new TableModel("sales", "Orders", [new ColumnModel("Id", "int", false, true, false)])
            ],
            [
                new ForeignKeyModel("FK_Orders_Users", new TableRef("dbo", "Orders"), ["UserId"], new TableRef("dbo", "Users"), ["Id"])
            ]);
}
