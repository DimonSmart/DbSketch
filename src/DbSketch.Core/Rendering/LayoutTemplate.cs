using System.Text;
using System.Text.RegularExpressions;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed record LayoutCellTemplate(string Text, IReadOnlySet<string> Tokens);

public sealed record LayoutTemplate(IReadOnlyList<LayoutCellTemplate> Cells);

public sealed class LayoutTemplateException(string message) : Exception(message);

public static partial class LayoutTemplateParser
{
    public static LayoutTemplate Parse(
        string layout,
        IReadOnlySet<string> supportedTokens,
        string configPath)
    {
        if (string.IsNullOrWhiteSpace(layout))
        {
            throw new LayoutTemplateException($"{configPath} must not be empty.");
        }

        var cells = SplitCells(layout)
            .Select(cell => CreateCell(cell, supportedTokens, configPath))
            .ToArray();

        return new LayoutTemplate(cells);
    }

    private static LayoutCellTemplate CreateCell(string text, IReadOnlySet<string> supportedTokens, string configPath)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in TokenPattern().Matches(text))
        {
            var token = match.Groups["token"].Value;
            if (!supportedTokens.Contains(token))
            {
                throw new LayoutTemplateException(
                    $"{configPath} contains unknown token '{{{token}}}'. Supported tokens: {FormatSupportedTokens(supportedTokens)}.");
            }

            tokens.Add(token);
        }

        return new LayoutCellTemplate(text.Trim(), tokens);
    }

    private static IReadOnlyList<string> SplitCells(string layout)
    {
        var cells = new List<string>();
        var builder = new StringBuilder();
        for (var i = 0; i < layout.Length; i++)
        {
            var character = layout[i];
            if (character == '\\' && i + 1 < layout.Length)
            {
                var next = layout[i + 1];
                if (next is '|' or '\\')
                {
                    builder.Append(next);
                    i++;
                    continue;
                }
            }

            if (character == '|')
            {
                cells.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(character);
        }

        cells.Add(builder.ToString().Trim());
        return cells;
    }

    private static string FormatSupportedTokens(IReadOnlySet<string> supportedTokens) =>
        string.Join(", ", supportedTokens.Select(token => $"{{{token}}}"));

    [GeneratedRegex(@"\{(?<token>[^{}]+)\}")]
    private static partial Regex TokenPattern();
}

public sealed record RenderedLayoutCell(
    string Text,
    bool ContainsNameToken,
    bool ContainsFkToken,
    bool ContainsKeysToken);

public static class ColumnLayoutFormatter
{
    public static readonly IReadOnlySet<string> SupportedTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "name",
        "type",
        "nullability",
        "pk",
        "fk",
        "keys",
        "comment"
    };

    public static IReadOnlyList<RenderedLayoutCell> Format(
        ColumnModel column,
        LayoutTemplate template,
        DiagramCommentRenderOptions comments) =>
        template.Cells
            .Select(cell => FormatCell(cell, GetValues(column, comments)))
            .ToArray();

    private static Dictionary<string, string> GetValues(ColumnModel column, DiagramCommentRenderOptions comments) =>
        new(StringComparer.Ordinal)
        {
            ["name"] = column.Name,
            ["type"] = column.StoreType,
            ["nullability"] = column.IsNullable ? "NULL" : "NOT NULL",
            ["pk"] = column.IsPrimaryKey ? "PK" : "",
            ["fk"] = column.IsForeignKey ? "FK" : "",
            ["keys"] = string.Join(' ', new[] { column.IsPrimaryKey ? "PK" : null, column.IsForeignKey ? "FK" : null }.Where(value => value is not null))!,
            ["comment"] = RenderTextNormalizer.NormalizeInlineComment(column.Comment, comments.MaxLength) ?? ""
        };

    private static RenderedLayoutCell FormatCell(LayoutCellTemplate cell, Dictionary<string, string> values) =>
        new(
            Cleanup(RenderTokens(cell.Text, values)),
            cell.Tokens.Contains("name"),
            cell.Tokens.Contains("fk"),
            cell.Tokens.Contains("keys"));

    private static string RenderTokens(string text, Dictionary<string, string> values)
    {
        foreach (var (token, value) in values)
        {
            text = text.Replace($"{{{token}}}", value, StringComparison.Ordinal);
        }

        return text;
    }

    private static string Cleanup(string value)
    {
        var trimmed = value.Trim();
        while (trimmed.Length > 0)
        {
            var withoutWhitespace = trimmed.TrimEnd();
            if (withoutWhitespace.Length == 0 || !IsTrailingSeparator(withoutWhitespace[^1]))
            {
                return withoutWhitespace;
            }

            trimmed = withoutWhitespace[..^1].TrimEnd();
        }

        return "";
    }

    private static bool IsTrailingSeparator(char value) =>
        value is ':' or '-' or '–' or '—' or '/' or ',' or ';';
}

public static class TableHeaderLayoutFormatter
{
    public static readonly IReadOnlySet<string> SupportedTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "schema",
        "table",
        "name",
        "fullName",
        "comment"
    };

    public static IReadOnlyList<RenderedLayoutCell> Format(
        TableModel table,
        LayoutTemplate template,
        DiagramCommentRenderOptions comments) =>
        template.Cells
            .Select(cell => FormatCell(cell, GetValues(table, comments)))
            .ToArray();

    private static Dictionary<string, string> GetValues(TableModel table, DiagramCommentRenderOptions comments) =>
        new(StringComparer.Ordinal)
        {
            ["schema"] = table.SchemaName,
            ["table"] = table.Name,
            ["name"] = table.Name,
            ["fullName"] = table.FullName,
            ["comment"] = RenderTextNormalizer.NormalizeInlineComment(table.Comment, comments.MaxLength) ?? ""
        };

    private static RenderedLayoutCell FormatCell(LayoutCellTemplate cell, Dictionary<string, string> values) =>
        new(
            Cleanup(RenderTokens(cell.Text, values)),
            cell.Tokens.Contains("name") || cell.Tokens.Contains("table") || cell.Tokens.Contains("fullName"),
            ContainsFkToken: false,
            ContainsKeysToken: false);

    private static string RenderTokens(string text, Dictionary<string, string> values)
    {
        foreach (var (token, value) in values)
        {
            text = text.Replace($"{{{token}}}", value, StringComparison.Ordinal);
        }

        return text;
    }

    private static string Cleanup(string value)
    {
        var trimmed = value.Trim();
        while (trimmed.Length > 0)
        {
            var withoutWhitespace = trimmed.TrimEnd();
            if (withoutWhitespace.Length == 0 || !IsTrailingSeparator(withoutWhitespace[^1]))
            {
                return withoutWhitespace;
            }

            trimmed = withoutWhitespace[..^1].TrimEnd();
        }

        return "";
    }

    private static bool IsTrailingSeparator(char value) =>
        value is ':' or '-' or '–' or '—' or '/' or ',' or ';';
}
