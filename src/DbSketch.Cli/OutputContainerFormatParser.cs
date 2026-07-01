using DimonSmart.DbSketch.Core.Rendering;

namespace DimonSmart.DbSketch.Cli;

public static class OutputContainerFormatParser
{
    public static OutputContainerFormat Parse(string? value)
    {
        var format = (value ?? "raw").Trim().ToLowerInvariant();
        return format switch
        {
            "raw" => OutputContainerFormat.Raw,
            "markdown" => OutputContainerFormat.Markdown,
            "md" => OutputContainerFormat.Markdown,
            _ => throw new CliException($"Unknown output format '{format}'. Supported values: raw, markdown.")
        };
    }
}

public static class DiagramRendererParser
{
    public static DiagramFormat Parse(string? value)
    {
        var renderer = (value ?? "dot").Trim().ToLowerInvariant();
        return renderer switch
        {
            "dot" => DiagramFormat.Dot,
            "mermaid" => DiagramFormat.Mermaid,
            _ => throw new CliException($"Unknown diagram renderer '{renderer}'. Supported values: dot, mermaid.")
        };
    }
}

public static class DiagramDirectionParser
{
    public static DiagramDirection Parse(string? value)
    {
        var direction = (value ?? "LR").Trim().ToUpperInvariant();
        return direction switch
        {
            "TB" => DiagramDirection.TB,
            "BT" => DiagramDirection.BT,
            "LR" => DiagramDirection.LR,
            "RL" => DiagramDirection.RL,
            _ => throw new CliException($"Unknown diagram direction '{direction}'. Supported values: TB, BT, LR, RL.")
        };
    }
}
