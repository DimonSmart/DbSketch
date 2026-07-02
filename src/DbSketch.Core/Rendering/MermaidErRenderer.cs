using System.Text;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed class MermaidErRenderer : IDiagramRenderer
{
    public DiagramRendererCapabilities Capabilities { get; } = new(
        SupportsColumnToColumnRelationships: false,
        SupportsCustomTableLayouts: false);

    public string Render(DatabaseModel model, DiagramRenderOptions options)
    {
        var builder = new StringBuilder();

        builder.AppendLine("erDiagram");
        if (options.Mermaid.EmitDirection)
        {
            builder.AppendLine($"  direction {FormatDirection(options.Direction)}");
        }

        builder.AppendLine();

        foreach (var table in model.Tables.OrderBy(table => table.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AppendTable(builder, table, options);
        }

        foreach (var foreignKey in model.ForeignKeys.OrderBy(fk => fk.Name, StringComparer.OrdinalIgnoreCase))
        {
            AppendForeignKey(builder, model.Tables, foreignKey, options);
        }

        return builder.ToString();
    }

    private static string FormatDirection(DiagramDirection direction) => direction.ToString();

    private static void AppendTable(StringBuilder builder, TableModel table, DiagramRenderOptions options)
    {
        builder.AppendLine($"  {FormatEntityName(GetTableDisplayName(table, options))} {{");
        foreach (var column in table.Columns)
        {
            builder.AppendLine($"    {FormatColumn(column, options)}");
        }

        builder.AppendLine("  }");
        builder.AppendLine();
    }

    private static string FormatColumn(ColumnModel column, DiagramRenderOptions options)
    {
        var parts = new List<string>
        {
            options.Show.ColumnTypes ? NormalizeType(column.StoreType) : "column",
            NormalizeAttributeName(column.Name)
        };

        if (options.Show.PrimaryKeys && column.IsPrimaryKey)
        {
            parts.Add("PK");
        }

        if (options.Show.ForeignKeys && column.IsForeignKey)
        {
            parts.Add("FK");
        }

        if (options.Show.Nullability)
        {
            parts.Add(column.IsNullable ? "NULL" : "NOT_NULL");
        }

        var rendered = string.Join(' ', parts);
        var comment = options.Show.ColumnComments
            ? FormatAttributeComment(column.Comment, options.Comments.MaxLength)
            : null;
        return comment is null ? rendered : $"{rendered} {comment}";
    }

    private static string? FormatAttributeComment(string? value, int? maxLength)
    {
        var normalized = RenderTextNormalizer.NormalizeInlineComment(value, maxLength);
        return normalized is null ? null : $"\"{normalized.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static void AppendForeignKey(StringBuilder builder, IReadOnlyList<TableModel> tables, ForeignKeyModel foreignKey, DiagramRenderOptions options)
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
        var label = options.Show.ForeignKeyLabels ? FormatEntityName(foreignKey.Name) : "\"\"";
        builder.AppendLine($"  {FormatEntityName(GetTableDisplayName(source, options))} {sourceCardinality}--|| {FormatEntityName(GetTableDisplayName(target, options))} : {label}");
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

    private static string FormatEntityName(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

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
}
