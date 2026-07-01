using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class OutputFormatParserTests
{
    [Theory]
    [InlineData("dot", DiagramFormat.Dot, false, null)]
    [InlineData("md-dot", DiagramFormat.Dot, true, "dot")]
    [InlineData("md-graphviz", DiagramFormat.Dot, true, "graphviz")]
    [InlineData("mermaid", DiagramFormat.Mermaid, false, null)]
    [InlineData("md-mermaid", DiagramFormat.Mermaid, true, "mermaid")]
    [InlineData("MD-GRAPHVIZ", DiagramFormat.Dot, true, "graphviz")]
    [InlineData("  md-graphviz  ", DiagramFormat.Dot, true, "graphviz")]
    public void ParsesSupportedFormats(string value, DiagramFormat expectedFormat, bool expectedMarkdownWrapper, string? expectedFenceLanguage)
    {
        var outputFormat = OutputFormatParser.Parse(value);

        Assert.Equal(expectedFormat, outputFormat.DiagramFormat);
        Assert.Equal(expectedMarkdownWrapper, outputFormat.MarkdownWrapper);
        Assert.Equal(expectedFenceLanguage, outputFormat.MarkdownFenceLanguage);
    }

    [Fact]
    public void ThrowsReadableErrorForUnknownFormat()
    {
        var exception = Assert.Throws<CliException>(() => OutputFormatParser.Parse("xyz"));

        Assert.Equal("Unknown format 'xyz'. Supported values: dot, md-dot, md-graphviz, mermaid, md-mermaid.", exception.Message);
    }
}
