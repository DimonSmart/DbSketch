using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class IndexRenderingTests
{
    private static readonly TableModel Table = new("dbo", "Orders", [new ColumnModel("Id", "int", false, false, false), new ColumnModel("Code", "varchar", false, false, false)]);

    [Theory]
    [InlineData(false, "IDX")]
    [InlineData(true, "UQ")]
    public void Classify_SimpleIndex_ReturnsExpectedMarker(bool unique, string expected)
    {
        var index = new IndexModel(new TableRef("dbo", "Orders"), "IX_Code", unique, [new IndexKeyColumn("Code", IndexSortDirection.Asc)]);
        var marker = IndexIndicatorClassifier.Format(IndexIndicatorClassifier.Classify(Table, Table.Columns[1], [index]));
        Assert.Equal(expected, marker);
    }

    [Fact]
    public void Classify_CompositeIndex_ReturnsComplexMarker()
    {
        var index = new IndexModel(new TableRef("dbo", "Orders"), "IX_Composite", false, [new IndexKeyColumn("Id"), new IndexKeyColumn("Code", IndexSortDirection.Desc)]);
        Assert.Equal("*", IndexIndicatorClassifier.Format(IndexIndicatorClassifier.Classify(Table, Table.Columns[1], [index])));
    }

    [Fact]
    public void Wrap_ShowIndexes_EmitsSectionBeforeFooterAndExcludesPrimaryKeyBackingIndex()
    {
        var indexes = new[]
        {
            new IndexModel(new TableRef("dbo", "Orders"), "PK_Orders", true, [new IndexKeyColumn("Id")], IsPrimaryKeyBacking: true),
            new IndexModel(new TableRef("dbo", "Orders"), "UX_Code", true, [new IndexKeyColumn("Code", IndexSortDirection.Desc)])
        };
        var result = MarkdownDiagramWrapper.Wrap("digraph {}\n", new MarkdownRenderOptions("dot", "# Schema", "Footer", true), indexes);
        Assert.Contains("| `dbo.Orders` | `UX_Code` | ✓ | `Code DESC`", result);
        Assert.DoesNotContain("PK_Orders", result);
        Assert.True(result.IndexOf("## Indexes", StringComparison.Ordinal) < result.IndexOf("Footer", StringComparison.Ordinal));
    }
}
