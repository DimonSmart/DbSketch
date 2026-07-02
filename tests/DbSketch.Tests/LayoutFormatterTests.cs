using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class LayoutFormatterTests
{
    [Fact]
    public void ColumnLayoutFormatsStyledRuns()
    {
        var template = LayoutTemplateParser.Parse("{name:bold} {type:color=#666666}", ColumnLayoutFormatter.SupportedTokens, "layout");

        var cell = Assert.Single(ColumnLayoutFormatter.Format(Column(), template, new DiagramCommentRenderOptions(null)));
        Assert.Equal("Id int", cell.Text);
        Assert.True(cell.Lines[0].Runs[0].Style.Bold);
        Assert.Equal(" ", cell.Lines[0].Runs[1].Text);
        Assert.Equal("#666666", cell.Lines[0].Runs[2].Style.Color);
    }

    [Fact]
    public void ColumnLayoutSkipsEmptyCommentLine()
    {
        var template = LayoutTemplateParser.Parse("{name:bold} {type}\n{comment:color=#666666,fontSize=9}", ColumnLayoutFormatter.SupportedTokens, "layout");

        var cell = Assert.Single(ColumnLayoutFormatter.Format(Column(comment: null), template, new DiagramCommentRenderOptions(null)));

        Assert.Single(cell.Lines);
        Assert.Equal("Id int", cell.Text);
    }

    [Fact]
    public void ColumnLayoutCleansTrailingSeparatorWhenCommentIsEmpty()
    {
        var template = LayoutTemplateParser.Parse("{name}: {type} - {comment} | {keys}", ColumnLayoutFormatter.SupportedTokens, "layout");

        var firstCell = ColumnLayoutFormatter.Format(Column(comment: null), template, new DiagramCommentRenderOptions(null))[0];

        Assert.Equal("Id: int", firstCell.Text);
    }

    [Fact]
    public void TableHeaderLayoutFormatsStyledRuns()
    {
        var template = LayoutTemplateParser.Parse("{table:bold}\n{comment:color=#666666,fontSize=9}", TableHeaderLayoutFormatter.SupportedTokens, "layout");
        var table = new TableModel("dbo", "Users", [Column()], "Application users");

        var cell = Assert.Single(TableHeaderLayoutFormatter.Format(table, template, new DiagramCommentRenderOptions(null)));

        Assert.Equal(2, cell.Lines.Count);
        Assert.Equal("Users\nApplication users", cell.Text);
        Assert.True(cell.Lines[0].Runs[0].Style.Bold);
        Assert.Equal("#666666", cell.Lines[1].Runs[0].Style.Color);
        Assert.Equal(9, cell.Lines[1].Runs[0].Style.FontSize);
    }

    private static ColumnModel Column(string? comment = "Identifier") =>
        new("Id", "int", false, true, false, comment);
}
