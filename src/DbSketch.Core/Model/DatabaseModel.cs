namespace DimonSmart.DbSketch.Core.Model;

public sealed record DatabaseModel(
    string Provider,
    string? DatabaseName,
    IReadOnlyList<TableModel> Tables,
    IReadOnlyList<ForeignKeyModel> ForeignKeys,
    IReadOnlyList<IndexModel>? Indexes = null);

public sealed record TableModel(
    string SchemaName,
    string Name,
    IReadOnlyList<ColumnModel> Columns,
    string? Comment = null)
{
    public string FullName => $"{SchemaName}.{Name}";
}

public sealed record ColumnModel(
    string Name,
    string StoreType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsForeignKey,
    string? Comment = null);

public sealed record ForeignKeyModel(
    string Name,
    TableRef SourceTable,
    IReadOnlyList<string> SourceColumns,
    TableRef TargetTable,
    IReadOnlyList<string> TargetColumns);

public sealed record TableRef(string SchemaName, string TableName)
{
    public string FullName => $"{SchemaName}.{TableName}";
}

public enum IndexSortDirection { Unspecified, Asc, Desc }

public sealed record IndexKeyColumn(string Name, IndexSortDirection Direction = IndexSortDirection.Unspecified);

public sealed record IndexModel(
    TableRef Table,
    string Name,
    bool IsUnique,
    IReadOnlyList<IndexKeyColumn> KeyColumns,
    IReadOnlyList<string>? IncludedColumns = null,
    string? Filter = null,
    string? Comment = null,
    bool IsPrimaryKeyBacking = false);
