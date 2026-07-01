using System.Text;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed class GraphvizDotRenderer : IDiagramRenderer
{
    public DiagramRendererCapabilities Capabilities { get; } = new(SupportsColumnToColumnRelationships: true);

    public string Render(DatabaseModel model, DiagramRenderOptions options)
    {
        var encoder = new DotIdEncoder();
        var builder = new StringBuilder();

        builder.AppendLine("digraph DbSketch {");
        builder.AppendLine("  graph [");
        builder.AppendLine($"    rankdir={encoder.EscapeDotString(FormatDirection(options.Direction))},");
        builder.AppendLine("    labelloc=\"t\",");
        builder.AppendLine($"    label=\"{encoder.EscapeDotString(options.Title)}\"");
        builder.AppendLine("  ];");
        builder.AppendLine();
        builder.AppendLine("  node [");
        builder.AppendLine("    shape=plain");
        builder.AppendLine("  ];");
        builder.AppendLine();
        builder.AppendLine("  edge [");
        builder.AppendLine("    fontsize=10");
        builder.AppendLine("  ];");
        builder.AppendLine();

        foreach (var table in model.Tables.OrderBy(table => table.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AppendTable(builder, encoder, table, options);
        }

        foreach (var foreignKey in model.ForeignKeys.OrderBy(fk => fk.Name, StringComparer.OrdinalIgnoreCase))
        {
            AppendForeignKey(builder, encoder, model.Tables, foreignKey);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string FormatDirection(DiagramDirection direction) => direction.ToString();

    private static void AppendTable(StringBuilder builder, DotIdEncoder encoder, TableModel table, DiagramRenderOptions options)
    {
        builder.AppendLine($"  \"{encoder.EscapeDotString(encoder.GetTableNodeId(table))}\" [");
        builder.AppendLine("    label=<");
        builder.AppendLine("      <TABLE BORDER=\"1\" CELLBORDER=\"1\" CELLSPACING=\"0\">");
        var title = options.Show.SchemaName ? table.FullName : table.Name;
        builder.AppendLine($"        <TR><TD BGCOLOR=\"#EEEEEE\"><B>{encoder.EscapeLabel(title)}</B></TD></TR>");
        foreach (var column in table.Columns)
        {
            var port = encoder.GetColumnPortId(table, column);
            builder.AppendLine($"        <TR><TD PORT=\"{encoder.EscapeLabel(port)}\">{encoder.EscapeLabel(FormatColumn(column, options))}</TD></TR>");
        }

        builder.AppendLine("      </TABLE>");
        builder.AppendLine("    >");
        builder.AppendLine("  ];");
        builder.AppendLine();
    }

    private static string FormatColumn(ColumnModel column, DiagramRenderOptions options)
    {
        var parts = new List<string>();
        if (options.Show.PrimaryKeys && column.IsPrimaryKey)
        {
            parts.Add("PK");
        }
        else if (options.Show.ForeignKeys && column.IsForeignKey)
        {
            parts.Add("FK");
        }

        parts.Add(column.Name);
        if (options.Show.ColumnTypes)
        {
            parts.Add(column.StoreType);
        }

        if (options.Show.Nullability)
        {
            parts.Add(column.IsNullable ? "NULL" : "NOT NULL");
        }

        return string.Join(' ', parts);
    }

    private static void AppendForeignKey(StringBuilder builder, DotIdEncoder encoder, IReadOnlyList<TableModel> tables, ForeignKeyModel foreignKey)
    {
        var source = FindTable(tables, foreignKey.SourceTable);
        var target = FindTable(tables, foreignKey.TargetTable);
        if (source is null || target is null)
        {
            return;
        }

        if (foreignKey.SourceColumns.Count == foreignKey.TargetColumns.Count)
        {
            for (var i = 0; i < foreignKey.SourceColumns.Count; i++)
            {
                var sourceColumn = source.Columns.FirstOrDefault(column => string.Equals(column.Name, foreignKey.SourceColumns[i], StringComparison.OrdinalIgnoreCase));
                var targetColumn = target.Columns.FirstOrDefault(column => string.Equals(column.Name, foreignKey.TargetColumns[i], StringComparison.OrdinalIgnoreCase));
                if (sourceColumn is null || targetColumn is null)
                {
                    AppendTableEdge(builder, encoder, source, target, foreignKey.Name);
                    return;
                }

                var label = i == 0 ? foreignKey.Name : null;
                AppendColumnEdge(builder, encoder, source, sourceColumn, target, targetColumn, label);
            }

            return;
        }

        AppendTableEdge(builder, encoder, source, target, foreignKey.Name);
    }

    private static TableModel? FindTable(IReadOnlyList<TableModel> tables, TableRef tableRef) =>
        tables.FirstOrDefault(table =>
            string.Equals(table.SchemaName, tableRef.SchemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(table.Name, tableRef.TableName, StringComparison.OrdinalIgnoreCase));

    private static void AppendColumnEdge(StringBuilder builder, DotIdEncoder encoder, TableModel source, ColumnModel sourceColumn, TableModel target, ColumnModel targetColumn, string? label)
    {
        builder.Append($"  \"{encoder.EscapeDotString(encoder.GetTableNodeId(source))}\":\"{encoder.EscapeDotString(encoder.GetColumnPortId(source, sourceColumn))}\"");
        builder.Append($" -> \"{encoder.EscapeDotString(encoder.GetTableNodeId(target))}\":\"{encoder.EscapeDotString(encoder.GetColumnPortId(target, targetColumn))}\"");
        AppendEdgeAttributes(builder, encoder, label);
    }

    private static void AppendTableEdge(StringBuilder builder, DotIdEncoder encoder, TableModel source, TableModel target, string label)
    {
        builder.Append($"  \"{encoder.EscapeDotString(encoder.GetTableNodeId(source))}\" -> \"{encoder.EscapeDotString(encoder.GetTableNodeId(target))}\"");
        AppendEdgeAttributes(builder, encoder, label);
    }

    private static void AppendEdgeAttributes(StringBuilder builder, DotIdEncoder encoder, string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            builder.AppendLine(";");
            return;
        }

        builder.AppendLine(" [");
        builder.AppendLine($"    label=\"{encoder.EscapeDotString(label)}\"");
        builder.AppendLine("  ];");
    }
}
