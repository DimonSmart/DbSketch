using System.Text;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public static class MarkdownDiagramWrapper
{
    public static string Wrap(string diagramText, MarkdownRenderOptions options, IReadOnlyList<IndexModel>? indexes = null)
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

        if (options.ShowIndexes)
        {
            builder.AppendLine();
            AppendIndexes(builder, indexes ?? []);
        }

        var footer = NormalizeBlock(options.Footer);
        if (!string.IsNullOrEmpty(footer))
        {
            builder.AppendLine();
            builder.AppendLine(footer);
        }

        return builder.ToString();
    }

    private static void AppendIndexes(StringBuilder builder, IReadOnlyList<IndexModel> indexes)
    {
        builder.AppendLine("## Indexes");
        builder.AppendLine();
        var visible = indexes.Where(index => !index.IsPrimaryKeyBacking)
            .OrderBy(index => index.Table.FullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(index => index.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (visible.Length == 0) { builder.AppendLine("No indexes."); return; }
        builder.AppendLine("| Table | Index | Unique | Columns | Include | Filter | Comment |");
        builder.AppendLine("|---|---|---:|---|---|---|---|");
        foreach (var index in visible)
        {
            var columns = string.Join(", ", index.KeyColumns.Select(key => $"{key.Name} {FormatDirection(key.Direction)}"));
            builder.AppendLine($"| `{EscapeCode(index.Table.FullName)}` | `{EscapeCode(index.Name)}` | {(index.IsUnique ? "✓" : "")} | `{EscapeCode(columns)}` | `{EscapeCode(string.Join(", ", index.IncludedColumns ?? []))}` | `{EscapeCode(index.Filter ?? "")}` | {EscapeText(index.Comment ?? "")} |");
        }
    }

    private static string FormatDirection(IndexSortDirection direction) => direction switch { IndexSortDirection.Asc => "ASC", IndexSortDirection.Desc => "DESC", _ => "" };
    private static string EscapeCode(string value) => value.Replace("`", "\\`", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
    private static string EscapeText(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

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
