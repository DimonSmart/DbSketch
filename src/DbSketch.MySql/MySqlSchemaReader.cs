using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;
using MySqlConnector;

namespace DimonSmart.DbSketch.MySql;

public sealed class MySqlSchemaReader : IDatabaseSchemaReader
{
    public async Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await ReadTablesAsync(connection, cancellationToken);
        var primaryKeys = await ReadPrimaryKeysAsync(connection, cancellationToken);
        var foreignKeys = await ReadForeignKeysAsync(connection, cancellationToken);
        var foreignKeyColumns = foreignKeys.SelectMany(fk => fk.SourceColumns.Select(column => (fk.SourceTable.FullName, Column: column))).ToHashSet();
        var columns = await ReadColumnsAsync(connection, primaryKeys, foreignKeyColumns, cancellationToken);

        return new DatabaseModel(options.Provider, connection.Database, tables.Select(table => new TableModel(table.SchemaName, table.TableName, columns.TryGetValue(table, out var c) ? c : [])).ToArray(), foreignKeys);
    }

    private static async Task<IReadOnlyList<TableRef>> ReadTablesAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select table_schema, table_name
            from information_schema.tables
            where table_type = 'BASE TABLE'
              and table_schema not in ('information_schema', 'mysql', 'performance_schema', 'sys')
            order by table_schema, table_name;
            """;
        var result = new List<TableRef>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableRef(reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    private static async Task<Dictionary<TableRef, IReadOnlyList<ColumnModel>>> ReadColumnsAsync(MySqlConnection connection, HashSet<(string Table, string Column)> primaryKeys, HashSet<(string Table, string Column)> foreignKeyColumns, CancellationToken cancellationToken)
    {
        const string sql = """
            select table_schema, table_name, column_name, column_type, is_nullable = 'YES'
            from information_schema.columns
            where table_schema not in ('information_schema', 'mysql', 'performance_schema', 'sys')
            order by table_schema, table_name, ordinal_position;
            """;
        var result = new Dictionary<TableRef, List<ColumnModel>>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = new TableRef(reader.GetString(0), reader.GetString(1));
            var column = reader.GetString(2);
            if (!result.TryGetValue(table, out var columns))
            {
                columns = [];
                result[table] = columns;
            }

            columns.Add(new ColumnModel(column, reader.GetString(3), reader.GetBoolean(4), primaryKeys.Contains((table.FullName, column)), foreignKeyColumns.Contains((table.FullName, column))));
        }

        return result.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ColumnModel>)pair.Value);
    }

    private static async Task<HashSet<(string Table, string Column)>> ReadPrimaryKeysAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select kcu.table_schema, kcu.table_name, kcu.column_name
            from information_schema.key_column_usage kcu
            join information_schema.table_constraints tc
              on tc.constraint_schema = kcu.constraint_schema
             and tc.constraint_name = kcu.constraint_name
             and tc.table_schema = kcu.table_schema
             and tc.table_name = kcu.table_name
            where tc.constraint_type = 'PRIMARY KEY'
              and kcu.table_schema not in ('information_schema', 'mysql', 'performance_schema', 'sys');
            """;
        var result = new HashSet<(string Table, string Column)>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(($"{reader.GetString(0)}.{reader.GetString(1)}", reader.GetString(2)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<ForeignKeyModel>> ReadForeignKeysAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select kcu.constraint_name,
                   kcu.table_schema, kcu.table_name, kcu.column_name,
                   kcu.referenced_table_schema, kcu.referenced_table_name, kcu.referenced_column_name,
                   kcu.ordinal_position
            from information_schema.key_column_usage kcu
            join information_schema.referential_constraints rc
              on rc.constraint_schema = kcu.constraint_schema
             and rc.constraint_name = kcu.constraint_name
            where kcu.referenced_table_name is not null
              and kcu.table_schema not in ('information_schema', 'mysql', 'performance_schema', 'sys')
              and kcu.referenced_table_schema not in ('information_schema', 'mysql', 'performance_schema', 'sys')
            order by kcu.constraint_name, kcu.ordinal_position;
            """;
        var rows = new List<FkRow>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new FkRow(reader.GetString(0), new TableRef(reader.GetString(1), reader.GetString(2)), reader.GetString(3), new TableRef(reader.GetString(4), reader.GetString(5)), reader.GetString(6), reader.GetInt32(7)));
        }

        return rows.GroupBy(row => row.Name).Select(group =>
        {
            var ordered = group.OrderBy(row => row.Ordinal).ToArray();
            return new ForeignKeyModel(group.Key, ordered[0].SourceTable, ordered.Select(row => row.SourceColumn).ToArray(), ordered[0].TargetTable, ordered.Select(row => row.TargetColumn).ToArray());
        }).ToArray();
    }

    private sealed record FkRow(string Name, TableRef SourceTable, string SourceColumn, TableRef TargetTable, string TargetColumn, int Ordinal);
}
