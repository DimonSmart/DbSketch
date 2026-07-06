using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;
using Microsoft.Data.Sqlite;

namespace DimonSmart.DbSketch.Sqlite;

public sealed class SqliteSchemaReader : IDatabaseSchemaReader
{
    public async Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await ReadTablesAsync(connection, options.CommandTimeoutSeconds, cancellationToken);
        var primaryKeys = await ReadPrimaryKeysAsync(connection, tables, options.CommandTimeoutSeconds, cancellationToken);
        var foreignKeys = await ReadForeignKeysAsync(connection, tables, options.CommandTimeoutSeconds, cancellationToken);
        var foreignKeyColumns = foreignKeys.SelectMany(fk => fk.SourceColumns.Select(column => (fk.SourceTable.FullName, Column: column))).ToHashSet();
        var columns = await ReadColumnsAsync(connection, tables, primaryKeys, foreignKeyColumns, options.CommandTimeoutSeconds, cancellationToken);

        return new DatabaseModel(
            options.Provider,
            GetDatabaseName(connection),
            tables.Select(table => new TableModel(
                table.SchemaName,
                table.TableName,
                columns.TryGetValue(table, out var c) ? c : [],
                Comment: null)).ToArray(),
            foreignKeys);
    }

    private static SqliteConnection CreateConnection(string connectionString)
    {
        try
        {
            return new SqliteConnection(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new DatabaseConnectionException(
                "Invalid sqlite connectionString. SQLite connection strings use keys like Data Source, Mode, Cache, and Foreign Keys.",
                exception);
        }
    }

    private static string GetDatabaseName(SqliteConnection connection)
    {
        if (string.Equals(connection.DataSource, ":memory:", StringComparison.Ordinal))
        {
            return ":memory:";
        }

        if (!string.IsNullOrWhiteSpace(connection.DataSource))
        {
            var fileName = Path.GetFileName(connection.DataSource);
            return string.IsNullOrWhiteSpace(fileName) ? connection.DataSource : fileName;
        }

        return "main";
    }

    private static async Task<IReadOnlyList<TableRef>> ReadTablesAsync(SqliteConnection connection, int? timeout, CancellationToken cancellationToken)
    {
        var schemas = new List<string>();
        await using (var command = CreateCommand("PRAGMA database_list;", connection, timeout))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var schemaName = reader.GetString(1);
                if (!schemaName.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    schemas.Add(schemaName);
                }
            }
        }

        var result = new List<TableRef>();
        foreach (var schemaName in schemas.Order(StringComparer.Ordinal))
        {
            var sql = $"""
                select name
                from {QuoteIdentifier(schemaName)}.sqlite_schema
                where type = 'table'
                  and name not like 'sqlite_%'
                order by name;
                """;

            await using var command = CreateCommand(sql, connection, timeout);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new TableRef(schemaName, reader.GetString(0)));
            }
        }

        return result;
    }

    private static async Task<Dictionary<TableRef, IReadOnlyList<ColumnModel>>> ReadColumnsAsync(
        SqliteConnection connection,
        IReadOnlyList<TableRef> tables,
        HashSet<(string Table, string Column)> primaryKeys,
        HashSet<(string Table, string Column)> foreignKeyColumns,
        int? timeout,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<TableRef, IReadOnlyList<ColumnModel>>();
        foreach (var table in tables)
        {
            const string sql = """
                select name, type, "notnull", pk, hidden
                from pragma_table_xinfo($tableName, $schemaName)
                where hidden in (0, 2, 3)
                order by cid;
                """;

            var columns = new List<ColumnModel>();
            await using var command = CreateCommand(sql, connection, timeout);
            command.Parameters.AddWithValue("$tableName", table.TableName);
            command.Parameters.AddWithValue("$schemaName", table.SchemaName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var columnName = reader.GetString(0);
                var storeType = reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1))
                    ? "column"
                    : reader.GetString(1);

                columns.Add(new ColumnModel(
                    columnName,
                    storeType,
                    reader.GetInt32(2) == 0,
                    primaryKeys.Contains((table.FullName, columnName)),
                    foreignKeyColumns.Contains((table.FullName, columnName)),
                    Comment: null));
            }

            result[table] = columns;
        }

        return result;
    }

    private static async Task<HashSet<(string Table, string Column)>> ReadPrimaryKeysAsync(SqliteConnection connection, IReadOnlyList<TableRef> tables, int? timeout, CancellationToken cancellationToken)
    {
        var result = new HashSet<(string Table, string Column)>();
        foreach (var table in tables)
        {
            foreach (var column in await ReadPrimaryKeyColumnsAsync(connection, table, timeout, cancellationToken))
            {
                result.Add((table.FullName, column));
            }
        }

        return result;
    }

    private static async Task<IReadOnlyList<ForeignKeyModel>> ReadForeignKeysAsync(SqliteConnection connection, IReadOnlyList<TableRef> tables, int? timeout, CancellationToken cancellationToken)
    {
        var rows = new List<ForeignKeyColumnRow>();
        var primaryKeyColumns = new Dictionary<TableRef, IReadOnlyList<string>>();

        foreach (var sourceTable in tables)
        {
            const string sql = """
                select id, seq, "table", "from", "to"
                from pragma_foreign_key_list($tableName, $schemaName)
                order by id, seq;
                """;

            await using var command = CreateCommand(sql, connection, timeout);
            command.Parameters.AddWithValue("$tableName", sourceTable.TableName);
            command.Parameters.AddWithValue("$schemaName", sourceTable.SchemaName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt64(0);
                var seq = reader.GetInt32(1);
                var targetTableName = reader.GetString(2);
                var sourceColumn = reader.GetString(3);
                var targetTable = new TableRef(sourceTable.SchemaName, targetTableName);
                var targetColumn = reader.IsDBNull(4) ? null : reader.GetString(4);

                if (string.IsNullOrWhiteSpace(targetColumn))
                {
                    if (!primaryKeyColumns.TryGetValue(targetTable, out var targetPrimaryKeys))
                    {
                        targetPrimaryKeys = await ReadPrimaryKeyColumnsAsync(connection, targetTable, timeout, cancellationToken);
                        primaryKeyColumns[targetTable] = targetPrimaryKeys;
                    }

                    if (seq < 0 || seq >= targetPrimaryKeys.Count)
                    {
                        continue;
                    }

                    targetColumn = targetPrimaryKeys[seq];
                }

                rows.Add(new ForeignKeyColumnRow(
                    sourceTable.SchemaName,
                    BuildForeignKeyName(sourceTable.TableName, targetTableName, id),
                    sourceTable,
                    sourceColumn,
                    targetTable,
                    targetColumn,
                    seq));
            }
        }

        return ForeignKeyModelBuilder.Build(rows);
    }

    private static async Task<IReadOnlyList<string>> ReadPrimaryKeyColumnsAsync(SqliteConnection connection, TableRef table, int? timeout, CancellationToken cancellationToken)
    {
        const string sql = """
            select name
            from pragma_table_xinfo($tableName, $schemaName)
            where pk > 0
            order by pk;
            """;

        var result = new List<string>();
        await using var command = CreateCommand(sql, connection, timeout);
        command.Parameters.AddWithValue("$tableName", table.TableName);
        command.Parameters.AddWithValue("$schemaName", table.SchemaName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static string QuoteIdentifier(string value) =>
        "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string BuildForeignKeyName(string sourceTable, string targetTable, long id) =>
        $"fk_{sourceTable}_{targetTable}_{id}";

    private static SqliteCommand CreateCommand(string sql, SqliteConnection connection, int? timeout)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (timeout is { } value)
        {
            command.CommandTimeout = value;
        }

        return command;
    }
}
