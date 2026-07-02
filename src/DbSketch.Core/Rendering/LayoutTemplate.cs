using System.Text;
using System.Text.RegularExpressions;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed record LayoutTemplate(IReadOnlyList<LayoutCellTemplate> Cells);

public sealed record LayoutCellTemplate(
    IReadOnlyList<LayoutLineTemplate> Lines,
    IReadOnlySet<string> Tokens)
{
    public string Text => string.Join('\n', Lines.Select(line => line.Text));
}

public sealed record LayoutLineTemplate(IReadOnlyList<LayoutPartTemplate> Parts)
{
    public string Text => string.Concat(Parts.Select(part => part.Text));
}

public abstract record LayoutPartTemplate
{
    public abstract string Text { get; }
}

public sealed record LiteralLayoutPartTemplate(string Value) : LayoutPartTemplate
{
    public override string Text => Value;
}

public sealed record TokenLayoutPartTemplate(string Token, LayoutTextStyle Style) : LayoutPartTemplate
{
    public override string Text => $"{{{Token}}}";
}

public sealed record LayoutTextStyle(
    bool Bold = false,
    bool Italic = false,
    string? Color = null,
    string? Font = null,
    int? FontSize = null);

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
        var lines = NormalizeLineBreaks(text.Trim())
            .Split('\n')
            .Select(line => CreateLine(line, supportedTokens, configPath, tokens))
            .ToArray();

        return new LayoutCellTemplate(lines, tokens);
    }

    private static LayoutLineTemplate CreateLine(
        string text,
        IReadOnlySet<string> supportedTokens,
        string configPath,
        HashSet<string> tokens)
    {
        var parts = new List<LayoutPartTemplate>();
        var offset = 0;
        foreach (Match match in TokenPattern().Matches(text))
        {
            if (match.Index > offset)
            {
                parts.Add(new LiteralLayoutPartTemplate(text[offset..match.Index]));
            }

            var tokenText = match.Groups["token"].Value;
            var token = tokenText;
            var modifierText = "";
            var modifierIndex = tokenText.IndexOf(':', StringComparison.Ordinal);
            if (modifierIndex >= 0)
            {
                token = tokenText[..modifierIndex];
                modifierText = tokenText[(modifierIndex + 1)..];
            }

            if (!supportedTokens.Contains(token))
            {
                throw new LayoutTemplateException(
                    $"{configPath} contains unknown token '{{{token}}}'. Supported tokens: {FormatSupportedTokens(supportedTokens)}.");
            }

            tokens.Add(token);
            parts.Add(new TokenLayoutPartTemplate(token, ParseStyle(modifierText, configPath, match.Value)));
            offset = match.Index + match.Length;
        }

        if (offset < text.Length)
        {
            parts.Add(new LiteralLayoutPartTemplate(text[offset..]));
        }

        return new LayoutLineTemplate(parts);
    }

    private static LayoutTextStyle ParseStyle(string modifierText, string configPath, string tokenText)
    {
        var bold = false;
        var italic = false;
        string? color = null;
        string? font = null;
        int? fontSize = null;

        if (modifierText.Length == 0)
        {
            return new LayoutTextStyle();
        }

        foreach (var modifier in modifierText.Split(',', StringSplitOptions.TrimEntries))
        {
            if (modifier.Length == 0)
            {
                throw new LayoutTemplateException($"{configPath} contains unknown modifier '' in '{tokenText}'.");
            }

            var separatorIndex = modifier.IndexOf('=', StringComparison.Ordinal);
            var name = separatorIndex < 0 ? modifier : modifier[..separatorIndex];
            var value = separatorIndex < 0 ? null : modifier[(separatorIndex + 1)..];

            switch (name)
            {
                case "bold" when value is null:
                    bold = true;
                    break;
                case "italic" when value is null:
                    italic = true;
                    break;
                case "color" when value is not null:
                    if (!HexColorPattern().IsMatch(value))
                    {
                        throw new LayoutTemplateException($"{configPath} modifier 'color' must be a hex color like #666666.");
                    }

                    color = value;
                    break;
                case "font" when value is not null:
                    if (!SafeFontPattern().IsMatch(value))
                    {
                        throw new LayoutTemplateException($"{configPath} modifier 'font' must contain only letters, digits, spaces, '_', '-', '.', and be at most 64 characters.");
                    }

                    font = value;
                    break;
                case "fontSize" when value is not null:
                    if (!int.TryParse(value, out var parsedFontSize) || parsedFontSize is < 6 or > 48)
                    {
                        throw new LayoutTemplateException($"{configPath} modifier 'fontSize' must be an integer from 6 to 48.");
                    }

                    fontSize = parsedFontSize;
                    break;
                default:
                    throw new LayoutTemplateException($"{configPath} contains unknown modifier '{name}' in '{tokenText}'.");
            }
        }

        return new LayoutTextStyle(bold, italic, color, font, fontSize);
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

    private static string NormalizeLineBreaks(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private static string FormatSupportedTokens(IReadOnlySet<string> supportedTokens) =>
        string.Join(", ", supportedTokens.Select(token => $"{{{token}}}"));

    [GeneratedRegex(@"\{(?<token>[^{}]+)\}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();

    [GeneratedRegex("^[A-Za-z0-9 _\\-.]{1,64}$")]
    private static partial Regex SafeFontPattern();
}

public sealed record RenderedLayoutCell(
    IReadOnlyList<RenderedLayoutLine> Lines,
    bool ContainsNameToken,
    bool ContainsFkToken,
    bool ContainsKeysToken)
{
    public string Text => string.Join('\n', Lines.Select(line => line.Text));
}

public sealed record RenderedLayoutLine(IReadOnlyList<RenderedTextRun> Runs)
{
    public string Text => string.Concat(Runs.Select(run => run.Text));
}

public sealed record RenderedTextRun(
    string Text,
    string? Token,
    LayoutTextStyle Style);

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
            LayoutRunFormatter.FormatLines(cell.Lines, values),
            cell.Tokens.Contains("name"),
            cell.Tokens.Contains("fk"),
            cell.Tokens.Contains("keys"));
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
            LayoutRunFormatter.FormatLines(cell.Lines, values),
            cell.Tokens.Contains("name") || cell.Tokens.Contains("table") || cell.Tokens.Contains("fullName"),
            ContainsFkToken: false,
            ContainsKeysToken: false);
}

internal static class LayoutRunFormatter
{
    public static IReadOnlyList<RenderedLayoutLine> FormatLines(
        IReadOnlyList<LayoutLineTemplate> lines,
        Dictionary<string, string> values)
    {
        var rendered = lines
            .Select(line => Cleanup(FormatLine(line, values)))
            .Where(runs => runs.Count > 0)
            .Select(runs => new RenderedLayoutLine(runs))
            .ToArray();

        return rendered.Length == 0 ? [new RenderedLayoutLine([])] : rendered;
    }

    private static List<RenderedTextRun> FormatLine(LayoutLineTemplate line, Dictionary<string, string> values)
    {
        var runs = new List<RenderedTextRun>();
        foreach (var part in line.Parts)
        {
            switch (part)
            {
                case LiteralLayoutPartTemplate literal:
                    runs.Add(new RenderedTextRun(literal.Value, null, new LayoutTextStyle()));
                    break;
                case TokenLayoutPartTemplate token:
                    runs.Add(new RenderedTextRun(values[token.Token], token.Token, token.Style));
                    break;
            }
        }

        return runs;
    }

    private static List<RenderedTextRun> Cleanup(List<RenderedTextRun> runs)
    {
        runs = runs.Where(run => run.Text.Length > 0).ToList();
        TrimStart(runs);
        TrimEnd(runs);
        RemoveTrailingSeparators(runs);
        return runs.Where(run => run.Text.Length > 0).ToList();
    }

    private static void TrimStart(List<RenderedTextRun> runs)
    {
        while (runs.Count > 0)
        {
            var first = runs[0];
            var trimmed = first.Text.TrimStart();
            if (trimmed.Length == first.Text.Length)
            {
                return;
            }

            if (trimmed.Length == 0)
            {
                runs.RemoveAt(0);
                continue;
            }

            runs[0] = first with { Text = trimmed };
            return;
        }
    }

    private static void TrimEnd(List<RenderedTextRun> runs)
    {
        while (runs.Count > 0)
        {
            var lastIndex = runs.Count - 1;
            var last = runs[lastIndex];
            var trimmed = last.Text.TrimEnd();
            if (trimmed.Length == last.Text.Length)
            {
                return;
            }

            if (trimmed.Length == 0)
            {
                runs.RemoveAt(lastIndex);
                continue;
            }

            runs[lastIndex] = last with { Text = trimmed };
            return;
        }
    }

    private static void RemoveTrailingSeparators(List<RenderedTextRun> runs)
    {
        while (runs.Count > 0)
        {
            TrimEnd(runs);
            if (runs.Count == 0)
            {
                return;
            }

            var lastIndex = runs.Count - 1;
            var last = runs[lastIndex];
            if (!IsTrailingSeparator(last.Text[^1]))
            {
                return;
            }

            var text = last.Text[..^1];
            if (text.Length == 0)
            {
                runs.RemoveAt(lastIndex);
            }
            else
            {
                runs[lastIndex] = last with { Text = text };
            }
        }
    }

    private static bool IsTrailingSeparator(char value) =>
        value is ':' or '-' or '–' or '—' or '/' or ',' or ';';
}
