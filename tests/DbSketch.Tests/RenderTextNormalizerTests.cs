using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class RenderTextNormalizerTests
{
    [Fact]
    public void NormalizeInlineComment_ReturnsNullForNull()
    {
        Assert.Null(RenderTextNormalizer.NormalizeInlineComment(null));
    }

    [Fact]
    public void NormalizeInlineComment_ReturnsNullForWhitespace()
    {
        Assert.Null(RenderTextNormalizer.NormalizeInlineComment(" \t\r\n "));
    }

    [Fact]
    public void NormalizeInlineComment_ConvertsMultilineCommentToSingleLine()
    {
        Assert.Equal("Line 1 Line 2", RenderTextNormalizer.NormalizeInlineComment("Line 1\r\nLine 2"));
    }

    [Fact]
    public void NormalizeInlineComment_CollapsesTabsAndNewlines()
    {
        Assert.Equal("A B C", RenderTextNormalizer.NormalizeInlineComment(" A\t\tB\n C "));
    }

    [Fact]
    public void NormalizeInlineComment_DoesNotEscapeQuotes()
    {
        Assert.Equal("User \"identifier\"", RenderTextNormalizer.NormalizeInlineComment("User \"identifier\""));
    }

    [Fact]
    public void NormalizeInlineComment_WithNullMaxLengthDoesNotTruncate()
    {
        Assert.Equal("Long comment", RenderTextNormalizer.NormalizeInlineComment("Long comment", null));
    }

    [Fact]
    public void NormalizeInlineComment_TruncatesAndAddsEllipsis()
    {
        Assert.Equal("Long comm…", RenderTextNormalizer.NormalizeInlineComment("Long comment", 10));
    }

    [Fact]
    public void NormalizeInlineComment_MaxLengthOneReturnsEllipsis()
    {
        Assert.Equal("…", RenderTextNormalizer.NormalizeInlineComment("Long comment", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NormalizeInlineComment_RejectsNonPositiveMaxLength(int maxLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderTextNormalizer.NormalizeInlineComment("Comment", maxLength));
    }
}
