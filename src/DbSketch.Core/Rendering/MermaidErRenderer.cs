using System.Text;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed class MermaidErRenderer : IDiagramRenderer
{
    public DiagramRendererCapabilities Capabilities { get; } = new(
        SupportsColumnToColumnRelationships: false,
        ColumnLayout: ColumnLayoutSupport.ProjectionOnly,
        SupportsTableHeaderLayout: false,
        SupportsStyledLayout: false);

    public string Render(DatabaseModel model, DiagramRenderOptions options)
    {
        var builder = new StringBuilder();

        builder.AppendLine("erDiagram");
        if (options.Mermaid.EmitDirection)
        {
            builder.AppendLine($"  direction {FormatDirection(options.Direction)}");
        }

        builder.AppendLine();
        var columnLayout = MermaidColumnLayoutPlan.Create(ParseColumnLayout(options.Layout.ColumnLayout));
        var entities = CreateEntities(model.Tables, options);

        foreach (var table in model.Tables.OrderBy(table => table.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AppendTable(builder, entities[table], options, columnLayout);
        }

        foreach (var foreignKey in model.ForeignKeys.OrderBy(fk => fk.Name, StringComparer.OrdinalIgnoreCase))
        {
            AppendForeignKey(builder, model.Tables, entities, foreignKey, options);
        }

        return builder.ToString();
    }

    private static string FormatDirection(DiagramDirection direction) => direction.ToString();

    private static void AppendTable(StringBuilder builder, MermaidEntity entity, DiagramRenderOptions options, MermaidColumnLayoutPlan layout)
    {
        builder.AppendLine($"  {entity.Declaration} {{");
        foreach (var column in entity.Table.Columns)
        {
            builder.AppendLine($"    {FormatColumn(column, options, layout)}");
        }

        builder.AppendLine("  }");
        builder.AppendLine();
    }

    private static string FormatColumn(ColumnModel column, DiagramRenderOptions options, MermaidColumnLayoutPlan layout)
    {
        var parts = new List<string>
        {
            layout.HasType ? NormalizeType(column.StoreType) : "column",
            NormalizeAttributeName(column.Name)
        };

        var keys = new List<string>();

        if ((layout.HasKeys || layout.HasPk) && column.IsPrimaryKey)
        {
            keys.Add("PK");
        }

        if ((layout.HasKeys || layout.HasFk) && column.IsForeignKey)
        {
            keys.Add("FK");
        }

        if (keys.Count > 0)
        {
            parts.Add(string.Join(", ", keys));
        }

        var rendered = string.Join(' ', parts);
        var comment = FormatColumnComment(column, options, layout);
        return comment is null ? rendered : $"{rendered} {comment}";
    }

    private static string? FormatColumnComment(ColumnModel column, DiagramRenderOptions options, MermaidColumnLayoutPlan layout)
    {
        if (layout.HasNullable && column.IsNullable)
        {
            return FormatAttributeComment("NULL", options.Comments.MaxLength);
        }

        if (layout.HasNullability)
        {
            return FormatAttributeComment(column.IsNullable ? "NULL" : "NOT NULL", options.Comments.MaxLength);
        }

        if (layout.HasComment)
        {
            return FormatAttributeComment(column.Comment, options.Comments.MaxLength);
        }

        return null;
    }

    private static string? FormatAttributeComment(string? value, int? maxLength)
    {
        var normalized = RenderTextNormalizer.NormalizeInlineComment(value, maxLength);
        if (normalized is null)
        {
            return null;
        }

        var safeComment = normalized.Replace("\"", "'", StringComparison.Ordinal);
        return $"\"{safeComment}\"";
    }

    private static void AppendForeignKey(
        StringBuilder builder,
        IReadOnlyList<TableModel> tables,
        IReadOnlyDictionary<TableModel, MermaidEntity> entities,
        ForeignKeyModel foreignKey,
        DiagramRenderOptions options)
    {
        var source = FindTable(tables, foreignKey.SourceTable);
        var target = FindTable(tables, foreignKey.TargetTable);
        if (source is null || target is null)
        {
            return;
        }

        if (!options.Show.SelfReferencingForeignKeys && IsSelfReferencing(foreignKey))
        {
            return;
        }

        var sourceCardinality = IsNullableForeignKey(source, foreignKey) ? "}o" : "}|";
        var label = options.Show.ForeignKeyLabels ? FormatQuotedLabel(foreignKey.Name) : "\"\"";
        builder.AppendLine($"  {entities[source].Reference} {sourceCardinality}--|| {entities[target].Reference} : {label}");
    }

    private static bool IsNullableForeignKey(TableModel source, ForeignKeyModel foreignKey)
    {
        foreach (var sourceColumnName in foreignKey.SourceColumns)
        {
            var column = source.Columns.FirstOrDefault(column => string.Equals(column.Name, sourceColumnName, StringComparison.OrdinalIgnoreCase));
            if (column is not null && column.IsNullable)
            {
                return true;
            }
        }

        return false;
    }

    private static TableModel? FindTable(IReadOnlyList<TableModel> tables, TableRef tableRef) =>
        tables.FirstOrDefault(table =>
            string.Equals(table.SchemaName, tableRef.SchemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(table.Name, tableRef.TableName, StringComparison.OrdinalIgnoreCase));

    private static bool IsSelfReferencing(ForeignKeyModel foreignKey) =>
        string.Equals(foreignKey.SourceTable.SchemaName, foreignKey.TargetTable.SchemaName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(foreignKey.SourceTable.TableName, foreignKey.TargetTable.TableName, StringComparison.OrdinalIgnoreCase);

    private static string GetTableDisplayName(TableModel table, DiagramRenderOptions options) =>
        options.Show.SchemaName ? table.FullName : table.Name;

    private static IReadOnlyDictionary<TableModel, MermaidEntity> CreateEntities(IReadOnlyList<TableModel> tables, DiagramRenderOptions options)
    {
        var entities = new Dictionary<TableModel, MermaidEntity>();
        var identifierCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var table in tables.OrderBy(table => table.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(table => table.FullName, StringComparer.Ordinal))
        {
            var comment = options.Show.TableComments
                ? RenderTextNormalizer.NormalizeInlineComment(table.Comment, options.Comments.MaxLength)
                : null;
            var displayName = GetTableDisplayName(table, options);

            if (comment is null)
            {
                var reference = FormatQuotedLabel(displayName);
                entities.Add(table, new MermaidEntity(table, reference, reference));
                continue;
            }

            var identifier = CreateUniqueEntityIdentifier(table, identifierCounts);
            var displayLabel = options.Mermaid.TableCommentsOnNewLine
                ? $"{displayName}<br>{comment}"
                : $"{displayName} ({comment})";
            var declaration = $"{identifier}[{FormatQuotedLabel(displayLabel)}]";
            entities.Add(table, new MermaidEntity(table, identifier, declaration));
        }

        return entities;
    }

    private static string CreateUniqueEntityIdentifier(TableModel table, IDictionary<string, int> identifierCounts)
    {
        var baseIdentifier = NormalizeEntityIdentifier($"{table.SchemaName}_{table.Name}");
        identifierCounts.TryGetValue(baseIdentifier, out var count);
        identifierCounts[baseIdentifier] = count + 1;
        return count == 0 ? baseIdentifier : $"{baseIdentifier}_{count + 1}";
    }

    private static string FormatQuotedLabel(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static string NormalizeEntityIdentifier(string value)
    {
        var builder = new StringBuilder();
        var previousWasUnderscore = false;

        foreach (var character in value)
        {
            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            {
                builder.Append(character);
                previousWasUnderscore = false;
            }
            else if (!previousWasUnderscore)
            {
                builder.Append('_');
                previousWasUnderscore = true;
            }
        }

        var normalized = builder.ToString().Trim('_');
        if (normalized.Length == 0)
        {
            return "entity";
        }

        return char.IsDigit(normalized[0]) ? $"_{normalized}" : normalized;
    }

    private static string NormalizeType(string value)
    {
        var normalized = NormalizeToken(value, prefixDigit: false, fallback: "unknown");
        return normalized.Length == 0 ? "unknown" : normalized;
    }

    private static string NormalizeAttributeName(string value) =>
        NormalizeToken(value, prefixDigit: true, fallback: "column");

    private static string NormalizeToken(string value, bool prefixDigit, string fallback)
    {
        var builder = new StringBuilder();
        var previousWasUnderscore = false;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
                previousWasUnderscore = false;
                continue;
            }

            if (!previousWasUnderscore)
            {
                builder.Append('_');
                previousWasUnderscore = true;
            }
        }

        var normalized = builder.ToString().Trim('_');
        if (normalized.Length == 0)
        {
            return fallback;
        }

        return prefixDigit && char.IsDigit(normalized[0]) ? $"_{normalized}" : normalized;
    }

    private static LayoutTemplate ParseColumnLayout(string? layout) =>
        layout is null
            ? throw new InvalidOperationException("diagram.columnLayout is required.")
            : LayoutTemplateParser.Parse(layout, ColumnLayoutFormatter.SupportedTokens, "diagram.columnLayout");

    private sealed record MermaidColumnLayoutPlan(
        bool HasType,
        bool HasKeys,
        bool HasPk,
        bool HasFk,
        bool HasNullable,
        bool HasNullability,
        bool HasComment)
    {
        public static MermaidColumnLayoutPlan Create(LayoutTemplate template)
        {
            var tokens = template.GetTokenSequence();
            return new MermaidColumnLayoutPlan(
                tokens.Contains("type", StringComparer.Ordinal),
                tokens.Contains("keys", StringComparer.Ordinal),
                tokens.Contains("pk", StringComparer.Ordinal),
                tokens.Contains("fk", StringComparer.Ordinal),
                tokens.Contains("nullable", StringComparer.Ordinal),
                tokens.Contains("nullability", StringComparer.Ordinal),
                tokens.Contains("comment", StringComparer.Ordinal));
        }
    }

    private sealed record MermaidEntity(TableModel Table, string Reference, string Declaration);
}
