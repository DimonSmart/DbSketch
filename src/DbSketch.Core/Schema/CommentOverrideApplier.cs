using DimonSmart.DbSketch.Core.Config;
using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Schema;

public static class CommentOverrideApplier
{
    public static DatabaseModel Apply(DatabaseModel model, CommentOverridesConfig overrides)
    {
        var tableOverrides = BuildTableOverrideMap(overrides);
        if (tableOverrides.Count == 0)
        {
            return model;
        }

        var tables = model.Tables
            .Select(table => ApplyTableOverride(table, tableOverrides))
            .ToList();

        return model with { Tables = tables };
    }

    private static Dictionary<string, TableCommentOverrideConfig> BuildTableOverrideMap(CommentOverridesConfig overrides)
    {
        var tableOverrides = new Dictionary<string, TableCommentOverrideConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var tableOverride in overrides.Tables)
        {
            if (string.IsNullOrWhiteSpace(tableOverride.Schema))
            {
                throw new InvalidOperationException("comments.overrides.tables[].schema is required.");
            }

            if (string.IsNullOrWhiteSpace(tableOverride.Name))
            {
                throw new InvalidOperationException("comments.overrides.tables[].name is required.");
            }

            var key = TableKey(tableOverride.Schema, tableOverride.Name);
            if (!tableOverrides.TryAdd(key, tableOverride))
            {
                throw new InvalidOperationException($"Duplicate comment override for table '{key}'.");
            }
        }

        return tableOverrides;
    }

    private static TableModel ApplyTableOverride(TableModel table, Dictionary<string, TableCommentOverrideConfig> tableOverrides)
    {
        if (!tableOverrides.TryGetValue(TableKey(table.SchemaName, table.Name), out var tableOverride))
        {
            return table;
        }

        var tableComment = NormalizeOverride(tableOverride.Comment) ?? table.Comment;
        var columns = table.Columns
            .Select(column => ApplyColumnOverride(column, tableOverride))
            .ToList();

        return table with
        {
            Comment = tableComment,
            Columns = columns
        };
    }

    private static ColumnModel ApplyColumnOverride(ColumnModel column, TableCommentOverrideConfig tableOverride)
    {
        if (!TryGetColumnOverride(tableOverride, column.Name, out var columnOverride))
        {
            return column;
        }

        return column with { Comment = NormalizeOverride(columnOverride) ?? column.Comment };
    }

    private static bool TryGetColumnOverride(TableCommentOverrideConfig tableOverride, string columnName, out string? columnOverride)
    {
        foreach (var overrideColumn in tableOverride.Columns)
        {
            if (string.Equals(overrideColumn.Key, columnName, StringComparison.OrdinalIgnoreCase))
            {
                columnOverride = overrideColumn.Value;
                return true;
            }
        }

        columnOverride = null;
        return false;
    }

    private static string? NormalizeOverride(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string TableKey(string schemaName, string tableName) => $"{schemaName.Trim()}.{tableName.Trim()}";
}
