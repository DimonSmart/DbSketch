using DbSketch.Core.Descriptions;
using DbSketch.Core.Model;

namespace DbSketch.Tests;

public sealed class DescriptionProviderTests
{
    [Fact]
    public async Task NullDescriptionProviderReturnsNullForTable()
    {
        var provider = new NullDescriptionProvider();

        var result = await provider.GetTableDescriptionAsync(Table(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task NullDescriptionProviderReturnsNullForColumn()
    {
        var provider = new NullDescriptionProvider();
        var table = Table();

        var result = await provider.GetColumnDescriptionAsync(table, table.Columns[0], CancellationToken.None);

        Assert.Null(result);
    }

    private static TableModel Table() =>
        new("dbo", "Users", [new ColumnModel("Id", "int", false, true, false)]);
}
