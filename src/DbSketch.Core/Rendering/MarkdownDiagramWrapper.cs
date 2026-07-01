using System.Text;

namespace DimonSmart.DbSketch.Core.Rendering;

public static class MarkdownDiagramWrapper
{
    public static string Wrap(string diagramText, MarkdownRenderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FenceLanguage))
        {
            throw new ArgumentException("Markdown fence language is required.", "fenceLanguage");
        }

        var builder = new StringBuilder();
        var header = NormalizeBlock(options.Header);
        if (!string.IsNullOrEmpty(header))
        {
            builder.AppendLine(header);
            builder.AppendLine();
        }

        builder.AppendLine($"```{options.FenceLanguage}");
        builder.Append(diagramText);
        if (!EndsWithLineEnding(diagramText))
        {
            builder.AppendLine();
        }

        builder.AppendLine("```");

        var footer = NormalizeBlock(options.Footer);
        if (!string.IsNullOrEmpty(footer))
        {
            builder.AppendLine();
            builder.AppendLine(footer);
        }

        return builder.ToString();
    }

    private static string? NormalizeBlock(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.TrimEnd();

        return normalized.Length == 0 ? string.Empty : normalized;
    }

    private static bool EndsWithLineEnding(string value) =>
        value.EndsWith("\n", StringComparison.Ordinal) ||
        value.EndsWith("\r", StringComparison.Ordinal);
}
