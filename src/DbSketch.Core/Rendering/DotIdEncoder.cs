using System.Text;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public sealed class DotIdEncoder
{
    private readonly Dictionary<string, Dictionary<string, string>> _portIds = new(StringComparer.Ordinal);

    public string GetTableNodeId(TableModel table) => "table_" + Sanitize($"{table.SchemaName}_{table.Name}");

    public string GetColumnPortId(TableModel table, ColumnModel column)
    {
        var tableKey = table.FullName;
        if (!_portIds.TryGetValue(tableKey, out var tablePorts))
        {
            tablePorts = BuildTablePorts(table);
            _portIds[tableKey] = tablePorts;
        }

        return tablePorts[column.Name];
    }

    public string EscapeLabel(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    public string EscapeDotString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static Dictionary<string, string> BuildTablePorts(TableModel table)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in table.Columns)
        {
            var baseId = "col_" + Sanitize(column.Name);
            var id = baseId;
            var suffix = 2;
            while (!used.Add(id))
            {
                id = $"{baseId}_{suffix}";
                suffix++;
            }

            result[column.Name] = id;
        }

        return result;
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(char.IsAsciiLetterOrDigit(c) ? c : '_');
        }

        var sanitized = builder.ToString().Trim('_');
        return sanitized.Length == 0 ? "id" : sanitized;
    }
}
