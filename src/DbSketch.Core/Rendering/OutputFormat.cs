namespace DimonSmart.DbSketch.Core.Rendering;

public enum DiagramFormat
{
    Dot,
    Mermaid
}

public sealed record OutputFormat(DiagramFormat DiagramFormat, bool MarkdownWrapper);
