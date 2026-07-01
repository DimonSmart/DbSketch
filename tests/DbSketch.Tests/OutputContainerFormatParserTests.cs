using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class OutputContainerFormatParserTests
{
    [Theory]
    [InlineData("raw", OutputContainerFormat.Raw)]
    [InlineData("markdown", OutputContainerFormat.Markdown)]
    [InlineData("md", OutputContainerFormat.Markdown)]
    [InlineData("MARKDOWN", OutputContainerFormat.Markdown)]
    [InlineData("  markdown  ", OutputContainerFormat.Markdown)]
    public void ParsesSupportedOutputFormats(string value, OutputContainerFormat expectedFormat)
    {
        var outputFormat = OutputContainerFormatParser.Parse(value);

        Assert.Equal(expectedFormat, outputFormat);
    }

    [Theory]
    [InlineData("dot", DiagramFormat.Dot)]
    [InlineData("mermaid", DiagramFormat.Mermaid)]
    [InlineData("MERMAID", DiagramFormat.Mermaid)]
    [InlineData("  Dot  ", DiagramFormat.Dot)]
    public void ParsesSupportedDiagramRenderers(string value, DiagramFormat expectedRenderer)
    {
        var renderer = DiagramRendererParser.Parse(value);

        Assert.Equal(expectedRenderer, renderer);
    }

    [Fact]
    public void ThrowsReadableErrorForUnknownOutputFormat()
    {
        var exception = Assert.Throws<CliException>(() => OutputContainerFormatParser.Parse("xyz"));

        Assert.Equal("Unknown output format 'xyz'. Supported values: raw, markdown.", exception.Message);
    }

    [Fact]
    public void ThrowsReadableErrorForUnknownRenderer()
    {
        var exception = Assert.Throws<CliException>(() => DiagramRendererParser.Parse("xyz"));

        Assert.Equal("Unknown diagram renderer 'xyz'. Supported values: dot, mermaid.", exception.Message);
    }
}
