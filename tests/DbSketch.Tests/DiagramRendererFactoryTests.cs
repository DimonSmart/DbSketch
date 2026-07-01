using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Tests;

public sealed class DiagramRendererFactoryTests
{
    [Fact]
    public void CreatesDotRenderer()
    {
        var renderer = new DiagramRendererFactory().Create(DiagramFormat.Dot);

        Assert.IsType<GraphvizDotRenderer>(renderer);
        Assert.True(renderer.Capabilities.SupportsColumnToColumnRelationships);
    }

    [Fact]
    public void CreatesMermaidRenderer()
    {
        var renderer = new DiagramRendererFactory().Create(DiagramFormat.Mermaid);

        Assert.IsType<MermaidErRenderer>(renderer);
        Assert.False(renderer.Capabilities.SupportsColumnToColumnRelationships);
    }
}
