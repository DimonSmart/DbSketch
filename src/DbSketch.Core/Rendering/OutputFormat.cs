namespace DimonSmart.DbSketch.Core.Rendering;

public enum DiagramFormat
{
    Dot,
    Mermaid
}

public enum OutputContainerFormat
{
    Raw,
    Markdown
}

public sealed record OutputFormat(OutputContainerFormat Format, MarkdownRenderOptions? Markdown);

public sealed record MarkdownRenderOptions(string FenceLanguage, string? Header, string? Footer);
