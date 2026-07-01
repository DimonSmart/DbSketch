using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class OutputFormatParserTests
{
    [Theory]
    [InlineData("dot", DiagramFormat.Dot, false)]
    [InlineData("md-dot", DiagramFormat.Dot, true)]
    [InlineData("mermaid", DiagramFormat.Mermaid, false)]
    [InlineData("md-mermaid", DiagramFormat.Mermaid, true)]
    [InlineData("MD-MERMAID", DiagramFormat.Mermaid, true)]
    [InlineData("  mermaid  ", DiagramFormat.Mermaid, false)]
    public void ParsesSupportedFormats(string value, DiagramFormat expectedFormat, bool expectedMarkdownWrapper)
    {
        var outputFormat = OutputFormatParser.Parse(value);

        Assert.Equal(expectedFormat, outputFormat.DiagramFormat);
        Assert.Equal(expectedMarkdownWrapper, outputFormat.MarkdownWrapper);
    }

    [Fact]
    public void ThrowsReadableErrorForUnknownFormat()
    {
        var exception = Assert.Throws<CliException>(() => OutputFormatParser.Parse("xyz"));

        Assert.Equal("Unknown format 'xyz'. Supported values: dot, md-dot, mermaid, md-mermaid.", exception.Message);
    }
}
