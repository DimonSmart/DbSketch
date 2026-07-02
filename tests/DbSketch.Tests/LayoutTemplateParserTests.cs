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

    [Fact]
    public void ParseTokenWithoutModifiers()
    {
        var template = LayoutTemplateParser.Parse("{name}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.Equal("name", token.Token);
        Assert.Equal(new LayoutTextStyle(), token.Style);
    }

    [Fact]
    public void ParseTokenWithBoldModifier()
    {
        var template = LayoutTemplateParser.Parse("{name:bold}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.True(token.Style.Bold);
    }

    [Fact]
    public void ParseTokenWithMultipleModifiers()
    {
        var template = LayoutTemplateParser.Parse("{name:bold,font=Times}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.Equal("name", token.Token);
        Assert.True(token.Style.Bold);
        Assert.Equal("Times", token.Style.Font);
    }

    [Fact]
    public void ParseTokenWithColorModifier()
    {
        var template = LayoutTemplateParser.Parse("{type:color=#666666}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.Equal("#666666", token.Style.Color);
    }

    [Fact]
    public void ParseTokenWithFontModifier()
    {
        var template = LayoutTemplateParser.Parse("{name:font=Times New Roman}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.Equal("Times New Roman", token.Style.Font);
    }

    [Fact]
    public void ParseTokenWithFontSizeModifier()
    {
        var template = LayoutTemplateParser.Parse("{name:fontSize=9}", Tokens, "layout");

        var token = Assert.IsType<TokenLayoutPartTemplate>(Assert.Single(Assert.Single(template.Cells).Lines[0].Parts));
        Assert.Equal(9, token.Style.FontSize);
    }

    [Fact]
    public void ParseLayoutWithLineBreak()
    {
        var template = LayoutTemplateParser.Parse("{name}\n{type}", Tokens, "layout");

        Assert.Equal(2, Assert.Single(template.Cells).Lines.Count);
    }

    [Fact]
    public void StyledKeysTokenAddsTokenMetadata()
    {
        var template = LayoutTemplateParser.Parse("{keys:color=#666666}", Tokens, "layout");

        Assert.Contains("keys", Assert.Single(template.Cells).Tokens);
    }

    [Fact]
    public void RejectUnknownModifier()
    {
        var exception = Assert.Throws<LayoutTemplateException>(() => LayoutTemplateParser.Parse("{name:shadow}", Tokens, "layout"));

        Assert.Equal("layout contains unknown modifier 'shadow' in '{name:shadow}'.", exception.Message);
    }

    [Fact]
    public void RejectInvalidColor()
    {
        var exception = Assert.Throws<LayoutTemplateException>(() => LayoutTemplateParser.Parse("{name:color=red}", Tokens, "layout"));

        Assert.Equal("layout modifier 'color' must be a hex color like #666666.", exception.Message);
    }

    [Fact]
    public void RejectInvalidFontSize()
    {
        var exception = Assert.Throws<LayoutTemplateException>(() => LayoutTemplateParser.Parse("{name:fontSize=1000}", Tokens, "layout"));

        Assert.Equal("layout modifier 'fontSize' must be an integer from 6 to 48.", exception.Message);
    }

    [Fact]
    public void RejectUnsafeFont()
    {
        var exception = Assert.Throws<LayoutTemplateException>(() => LayoutTemplateParser.Parse("{name:font=<script>}", Tokens, "layout"));

        Assert.Equal("layout modifier 'font' must contain only letters, digits, spaces, '_', '-', '.', and be at most 64 characters.", exception.Message);
    }
}
