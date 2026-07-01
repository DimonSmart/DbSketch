namespace DimonSmart.DbSketch.Core.Rendering;

public interface IDiagramRendererFactory
{
    IDiagramRenderer Create(DiagramFormat format);
}

public sealed class DiagramRendererFactory : IDiagramRendererFactory
{
    public IDiagramRenderer Create(DiagramFormat format) => format switch
    {
        DiagramFormat.Dot => new GraphvizDotRenderer(),
        DiagramFormat.Mermaid => new MermaidErRenderer(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
