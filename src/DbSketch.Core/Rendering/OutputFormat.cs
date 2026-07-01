namespace DimonSmart.DbSketch.Core.Rendering;

public enum DiagramFormat
{
    Dot,
    Mermaid
}

public sealed record OutputFormat
{
    public OutputFormat(DiagramFormat diagramFormat, bool markdownWrapper, string? markdownFenceLanguage)
    {
        if (markdownWrapper && string.IsNullOrWhiteSpace(markdownFenceLanguage))
        {
            throw new ArgumentException("Markdown fence language is required for Markdown output formats.", nameof(markdownFenceLanguage));
        }

        DiagramFormat = diagramFormat;
        MarkdownWrapper = markdownWrapper;
        MarkdownFenceLanguage = markdownWrapper ? markdownFenceLanguage : null;
    }

    public DiagramFormat DiagramFormat { get; }

    public bool MarkdownWrapper { get; }

    public string? MarkdownFenceLanguage { get; }
}
