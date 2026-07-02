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
            AppendForeignKey(builder, encoder, model.Tables, foreignKey, options);
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
        var markerColumnCount = GetMarkerColumnCount(options);
        var titleColumnSpan = FormatColumnSpan(markerColumnCount + 1);
        builder.AppendLine($"        <TR><TD{titleColumnSpan} BGCOLOR=\"#EEEEEE\" ALIGN=\"LEFT\"><B>{encoder.EscapeLabel(title)}</B></TD></TR>");
        var tableComment = options.Show.TableComments
            ? RenderTextNormalizer.NormalizeInlineComment(table.Comment, options.Comments.MaxLength)
            : null;
        if (tableComment is not null)
        {
            builder.AppendLine($"        <TR><TD{titleColumnSpan} BGCOLOR=\"#F7F7F7\" ALIGN=\"LEFT\"><FONT POINT-SIZE=\"9\">{encoder.EscapeLabel(tableComment)}</FONT></TD></TR>");
        }

        foreach (var column in table.Columns)
        {
            AppendColumn(builder, encoder, table, column, options, markerColumnCount);
        }

        builder.AppendLine("      </TABLE>");
        builder.AppendLine("    >");
        builder.AppendLine("  ];");
        builder.AppendLine();
    }

    private static int GetMarkerColumnCount(DiagramRenderOptions options) =>
        options.Show.PrimaryKeys || options.Show.ForeignKeys ? 2 : 0;

    private static string FormatColumnSpan(int columnCount) =>
        columnCount <= 1 ? "" : $" COLSPAN=\"{columnCount}\"";

    private static void AppendColumn(StringBuilder builder, DotIdEncoder encoder, TableModel table, ColumnModel column, DiagramRenderOptions options, int markerColumnCount)
    {
        var port = encoder.GetColumnPortId(table, column);
        var columnComment = options.Show.ColumnComments
            ? RenderTextNormalizer.NormalizeInlineComment(column.Comment, options.Comments.MaxLength)
            : null;

        builder.Append("        <TR>");
        builder.Append($"<TD PORT=\"{encoder.EscapeLabel(port)}\" ALIGN=\"LEFT\">{encoder.EscapeLabel(FormatColumnDetails(column, options))}");
        if (columnComment is not null)
        {
            builder.Append($"<BR/><FONT POINT-SIZE=\"9\">{encoder.EscapeLabel(columnComment)}</FONT>");
        }

        builder.Append("</TD>");
        if (markerColumnCount > 0)
        {
            AppendMarkerCell(builder, encoder, null, options.Show.PrimaryKeys && column.IsPrimaryKey ? "PK" : null);
            AppendMarkerCell(builder, encoder, column.IsForeignKey ? GetForeignKeyPortId(port) : null, options.Show.ForeignKeys && column.IsForeignKey ? "FK" : null);
        }

        builder.AppendLine("</TR>");
    }

    private static string GetForeignKeyPortId(string columnPortId) => $"{columnPortId}_fk";

    private static void AppendMarkerCell(StringBuilder builder, DotIdEncoder encoder, string? port, string? marker)
    {
        var portAttribute = port is null ? "" : $" PORT=\"{encoder.EscapeLabel(port)}\"";
        if (marker is null)
        {
            builder.Append($"<TD{portAttribute} WIDTH=\"24\"></TD>");
            return;
        }

        builder.Append($"<TD{portAttribute} WIDTH=\"24\" ALIGN=\"CENTER\"><FONT POINT-SIZE=\"9\">{marker}</FONT></TD>");
    }

    private static string FormatColumnDetails(ColumnModel column, DiagramRenderOptions options)
    {
        var parts = new List<string>();
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

    private static void AppendForeignKey(StringBuilder builder, DotIdEncoder encoder, IReadOnlyList<TableModel> tables, ForeignKeyModel foreignKey, DiagramRenderOptions options)
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

        if (foreignKey.SourceColumns.Count == foreignKey.TargetColumns.Count)
        {
            for (var i = 0; i < foreignKey.SourceColumns.Count; i++)
            {
                var sourceColumn = source.Columns.FirstOrDefault(column => string.Equals(column.Name, foreignKey.SourceColumns[i], StringComparison.OrdinalIgnoreCase));
                var targetColumn = target.Columns.FirstOrDefault(column => string.Equals(column.Name, foreignKey.TargetColumns[i], StringComparison.OrdinalIgnoreCase));
                if (sourceColumn is null || targetColumn is null)
                {
                    AppendTableEdge(builder, encoder, source, target, foreignKey.Name, options.Direction);
                    return;
                }

                var label = i == 0 && options.Show.ForeignKeyLabels ? foreignKey.Name : null;
                AppendColumnEdge(builder, encoder, source, sourceColumn, target, targetColumn, label, options.Direction);
            }

            return;
        }

        AppendTableEdge(builder, encoder, source, target, options.Show.ForeignKeyLabels ? foreignKey.Name : null, options.Direction);
    }

    private static TableModel? FindTable(IReadOnlyList<TableModel> tables, TableRef tableRef) =>
        tables.FirstOrDefault(table =>
            string.Equals(table.SchemaName, tableRef.SchemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(table.Name, tableRef.TableName, StringComparison.OrdinalIgnoreCase));

    private static bool IsSelfReferencing(ForeignKeyModel foreignKey) =>
        string.Equals(foreignKey.SourceTable.SchemaName, foreignKey.TargetTable.SchemaName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(foreignKey.SourceTable.TableName, foreignKey.TargetTable.TableName, StringComparison.OrdinalIgnoreCase);

    private static void AppendColumnEdge(StringBuilder builder, DotIdEncoder encoder, TableModel source, ColumnModel sourceColumn, TableModel target, ColumnModel targetColumn, string? label, DiagramDirection direction)
    {
        var sourcePort = sourceColumn.IsForeignKey
            ? GetForeignKeyPortId(encoder.GetColumnPortId(source, sourceColumn))
            : encoder.GetColumnPortId(source, sourceColumn);
        builder.Append($"  \"{encoder.EscapeDotString(encoder.GetTableNodeId(source))}\":\"{encoder.EscapeDotString(sourcePort)}\":{GetSourceCompass(direction)}");
        builder.Append($" -> \"{encoder.EscapeDotString(encoder.GetTableNodeId(target))}\":\"{encoder.EscapeDotString(encoder.GetColumnPortId(target, targetColumn))}\":{GetTargetCompass(direction)}");
        AppendEdgeAttributes(builder, encoder, label);
    }

    private static void AppendTableEdge(StringBuilder builder, DotIdEncoder encoder, TableModel source, TableModel target, string? label, DiagramDirection direction)
    {
        builder.Append($"  \"{encoder.EscapeDotString(encoder.GetTableNodeId(source))}\":{GetSourceCompass(direction)} -> \"{encoder.EscapeDotString(encoder.GetTableNodeId(target))}\":{GetTargetCompass(direction)}");
        AppendEdgeAttributes(builder, encoder, label);
    }

    private static string GetSourceCompass(DiagramDirection direction) =>
        direction switch
        {
            DiagramDirection.LR => "e",
            DiagramDirection.RL => "w",
            DiagramDirection.TB => "s",
            DiagramDirection.BT => "n",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

    private static string GetTargetCompass(DiagramDirection direction) =>
        direction switch
        {
            DiagramDirection.LR => "w",
            DiagramDirection.RL => "e",
            DiagramDirection.TB => "n",
            DiagramDirection.BT => "s",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

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
