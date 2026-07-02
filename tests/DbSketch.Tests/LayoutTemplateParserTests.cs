using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class LayoutTemplateParserTests
{
    private static readonly IReadOnlySet<string> Tokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "name",
        "type",
        "pk",
        "fk",
        "keys"
    };

    [Fact]
    public void SplitsByPipeAndTrimsCells()
    {
        var template = LayoutTemplateParser.Parse(" {name} | {type} | {keys} ", Tokens, "layout");

        Assert.Equal(["{name}", "{type}", "{keys}"], template.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void PreservesEmptyCells()
    {
        var template = LayoutTemplateParser.Parse("{name} || {fk} |", Tokens, "layout");

        Assert.Equal(["{name}", "", "{fk}", ""], template.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void SupportsEscapedPipe()
    {
        var template = LayoutTemplateParser.Parse("{name} \\| {type} | {keys}", Tokens, "layout");

        Assert.Equal(["{name} | {type}", "{keys}"], template.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void SupportsEscapedBackslash()
    {
        var template = LayoutTemplateParser.Parse("{name} \\\\ {type}", Tokens, "layout");

        Assert.Equal("{name} \\ {type}", Assert.Single(template.Cells).Text);
    }

    [Fact]
    public void DetectsUnknownToken()
    {
        var exception = Assert.Throws<LayoutTemplateException>(() => LayoutTemplateParser.Parse("{name} | {foo}", Tokens, "layout"));

        Assert.Equal("layout contains unknown token '{foo}'. Supported tokens: {name}, {type}, {pk}, {fk}, {keys}.", exception.Message);
    }

    [Fact]
    public void ReturnsTokenMetadataPerCell()
    {
        var template = LayoutTemplateParser.Parse("{name}: {type} | {fk}", Tokens, "layout");

        Assert.Equal(["name", "type"], template.Cells[0].Tokens);
        Assert.Equal(["fk"], template.Cells[1].Tokens);
    }
}
