namespace DimonSmart.DbSketch.Core.Schema;

public sealed class DatabaseComments
{
    private readonly Dictionary<string, string> _tableComments;
    private readonly Dictionary<string, string> _columnComments;

    public static DatabaseComments Empty { get; } = new([], []);

    public DatabaseComments(
        IEnumerable<(string SchemaName, string TableName, string? Comment)> tableComments,
        IEnumerable<(string SchemaName, string TableName, string ColumnName, string? Comment)> columnComments)
    {
        _tableComments = tableComments
            .Select(item => (Key: TableKey(item.SchemaName, item.TableName), Comment: NormalizeComment(item.Comment)))
            .Where(item => item.Comment is not null)
            .ToDictionary(item => item.Key, item => item.Comment!, StringComparer.OrdinalIgnoreCase);

        _columnComments = columnComments
            .Select(item => (Key: ColumnKey(item.SchemaName, item.TableName, item.ColumnName), Comment: NormalizeComment(item.Comment)))
            .Where(item => item.Comment is not null)
            .ToDictionary(item => item.Key, item => item.Comment!, StringComparer.OrdinalIgnoreCase);
    }

    public string? GetTableComment(string schemaName, string tableName) =>
        _tableComments.TryGetValue(TableKey(schemaName, tableName), out var comment) ? comment : null;

    public string? GetColumnComment(string schemaName, string tableName, string columnName) =>
        _columnComments.TryGetValue(ColumnKey(schemaName, tableName, columnName), out var comment) ? comment : null;

    public static string? NormalizeComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string TableKey(string schemaName, string tableName) => $"{schemaName}.{tableName}";

    private static string ColumnKey(string schemaName, string tableName, string columnName) => $"{schemaName}.{tableName}.{columnName}";
}
