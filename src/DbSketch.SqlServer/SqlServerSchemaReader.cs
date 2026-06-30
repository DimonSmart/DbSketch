using DbSketch.Core.Model;
using DbSketch.Core.Schema;
using Microsoft.Data.SqlClient;

namespace DbSketch.SqlServer;

public sealed class SqlServerSchemaReader : IDatabaseSchemaReader
{
    public async Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var tables = await ReadTablesAsync(connection, cancellationToken);
        var primaryKeys = await ReadPrimaryKeysAsync(connection, cancellationToken);
        var foreignKeys = await ReadForeignKeysAsync(connection, cancellationToken);
        var foreignKeyColumns = foreignKeys.SelectMany(fk => fk.SourceColumns.Select(column => (fk.SourceTable.FullName, Column: column))).ToHashSet();
        var columns = await ReadColumnsAsync(connection, primaryKeys, foreignKeyColumns, cancellationToken);

        var models = tables
            .Select(table => new TableModel(table.SchemaName, table.TableName, columns.TryGetValue(table, out var tableColumns) ? tableColumns : []))
            .ToArray();

        return new DatabaseModel(options.Provider, databaseName, models, foreignKeys);
    }

    private static async Task<IReadOnlyList<TableRef>> ReadTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select s.name, t.name
            from sys.tables t
            join sys.schemas s on s.schema_id = t.schema_id
            where t.is_ms_shipped = 0
            order by s.name, t.name;
            """;

        var result = new List<TableRef>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableRef(reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    private static async Task<Dictionary<TableRef, IReadOnlyList<ColumnModel>>> ReadColumnsAsync(SqlConnection connection, HashSet<(string Table, string Column)> primaryKeys, HashSet<(string Table, string Column)> foreignKeyColumns, CancellationToken cancellationToken)
    {
        const string sql = """
            select s.name, t.name, c.name,
                   case
                     when ty.name in ('nvarchar', 'nchar') and c.max_length > 0 then concat(ty.name, '(', c.max_length / 2, ')')
                     when ty.name in ('varchar', 'char', 'varbinary', 'binary') and c.max_length > 0 then concat(ty.name, '(', c.max_length, ')')
                     when c.max_length = -1 then concat(ty.name, '(max)')
                     else ty.name
                   end,
                   c.is_nullable
            from sys.tables t
            join sys.schemas s on s.schema_id = t.schema_id
            join sys.columns c on c.object_id = t.object_id
            join sys.types ty on ty.user_type_id = c.user_type_id
            where t.is_ms_shipped = 0
            order by s.name, t.name, c.column_id;
            """;

        var result = new Dictionary<TableRef, List<ColumnModel>>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = new TableRef(reader.GetString(0), reader.GetString(1));
            var fullName = table.FullName;
            var columnName = reader.GetString(2);
            if (!result.TryGetValue(table, out var columns))
            {
                columns = [];
                result[table] = columns;
            }

            columns.Add(new ColumnModel(
                columnName,
                reader.GetString(3),
                reader.GetBoolean(4),
                primaryKeys.Contains((fullName, columnName)),
                foreignKeyColumns.Contains((fullName, columnName))));
        }

        return result.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ColumnModel>)pair.Value);
    }

    private static async Task<HashSet<(string Table, string Column)>> ReadPrimaryKeysAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select s.name, t.name, c.name
            from sys.key_constraints kc
            join sys.tables t on t.object_id = kc.parent_object_id
            join sys.schemas s on s.schema_id = t.schema_id
            join sys.index_columns ic on ic.object_id = t.object_id and ic.index_id = kc.unique_index_id
            join sys.columns c on c.object_id = t.object_id and c.column_id = ic.column_id
            where kc.type = 'PK' and t.is_ms_shipped = 0;
            """;

        var result = new HashSet<(string Table, string Column)>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(($"{reader.GetString(0)}.{reader.GetString(1)}", reader.GetString(2)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<ForeignKeyModel>> ReadForeignKeysAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select fk.name,
                   ss.name, st.name, sc.name,
                   ts.name, tt.name, tc.name,
                   fkc.constraint_column_id
            from sys.foreign_keys fk
            join sys.foreign_key_columns fkc on fkc.constraint_object_id = fk.object_id
            join sys.tables st on st.object_id = fk.parent_object_id
            join sys.schemas ss on ss.schema_id = st.schema_id
            join sys.columns sc on sc.object_id = st.object_id and sc.column_id = fkc.parent_column_id
            join sys.tables tt on tt.object_id = fk.referenced_object_id
            join sys.schemas ts on ts.schema_id = tt.schema_id
            join sys.columns tc on tc.object_id = tt.object_id and tc.column_id = fkc.referenced_column_id
            where st.is_ms_shipped = 0 and tt.is_ms_shipped = 0
            order by fk.name, fkc.constraint_column_id;
            """;

        var rows = new List<FkRow>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new FkRow(reader.GetString(0), new TableRef(reader.GetString(1), reader.GetString(2)), reader.GetString(3), new TableRef(reader.GetString(4), reader.GetString(5)), reader.GetString(6), reader.GetInt32(7)));
        }

        return rows.GroupBy(row => row.Name).Select(ToForeignKey).ToArray();
    }

    private static ForeignKeyModel ToForeignKey(IGrouping<string, FkRow> group)
    {
        var rows = group.OrderBy(row => row.Ordinal).ToArray();
        return new ForeignKeyModel(group.Key, rows[0].SourceTable, rows.Select(row => row.SourceColumn).ToArray(), rows[0].TargetTable, rows.Select(row => row.TargetColumn).ToArray());
    }

    private sealed record FkRow(string Name, TableRef SourceTable, string SourceColumn, TableRef TargetTable, string TargetColumn, int Ordinal);
}
