using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Cli;

public static class OutputFormatParser
{
    private const string SupportedFormats = "dot, md-dot, md-graphviz, mermaid, md-mermaid";

    public static OutputFormat Parse(string? value)
    {
        var format = (value ?? "dot").Trim().ToLowerInvariant();
        return format switch
        {
            "dot" => new OutputFormat(DiagramFormat.Dot, markdownWrapper: false, markdownFenceLanguage: null),
            "md-dot" => new OutputFormat(DiagramFormat.Dot, markdownWrapper: true, markdownFenceLanguage: "dot"),
            "md-graphviz" => new OutputFormat(DiagramFormat.Dot, markdownWrapper: true, markdownFenceLanguage: "graphviz"),
            "mermaid" => new OutputFormat(DiagramFormat.Mermaid, markdownWrapper: false, markdownFenceLanguage: null),
            "md-mermaid" => new OutputFormat(DiagramFormat.Mermaid, markdownWrapper: true, markdownFenceLanguage: "mermaid"),
            _ => throw new CliException($"Unknown format '{format}'. Supported values: {SupportedFormats}.")
        };
    }
}
