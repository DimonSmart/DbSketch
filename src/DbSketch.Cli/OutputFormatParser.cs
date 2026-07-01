using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Cli;

public static class OutputFormatParser
{
    private const string SupportedFormats = "dot, md-dot, mermaid, md-mermaid";

    public static OutputFormat Parse(string? value)
    {
        var format = (value ?? "dot").Trim().ToLowerInvariant();
        return format switch
        {
            "dot" => new OutputFormat(DiagramFormat.Dot, MarkdownWrapper: false),
            "md-dot" => new OutputFormat(DiagramFormat.Dot, MarkdownWrapper: true),
            "mermaid" => new OutputFormat(DiagramFormat.Mermaid, MarkdownWrapper: false),
            "md-mermaid" => new OutputFormat(DiagramFormat.Mermaid, MarkdownWrapper: true),
            _ => throw new CliException($"Unknown format '{format}'. Supported values: {SupportedFormats}.")
        };
    }
}
