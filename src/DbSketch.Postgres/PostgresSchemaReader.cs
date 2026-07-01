using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;
using Npgsql;

namespace DimonSmart.DbSketch.Postgres;

public sealed class PostgresSchemaReader : IDatabaseSchemaReader
{
    public async Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await ReadTablesAsync(connection, cancellationToken);
        var primaryKeys = await ReadPrimaryKeysAsync(connection, cancellationToken);
        var foreignKeys = await ReadForeignKeysAsync(connection, cancellationToken);
        var foreignKeyColumns = foreignKeys.SelectMany(fk => fk.SourceColumns.Select(column => (fk.SourceTable.FullName, Column: column))).ToHashSet();
        var comments = options.ReadComments ? await ReadCommentsAsync(connection, cancellationToken) : DatabaseComments.Empty;
        var columns = await ReadColumnsAsync(connection, primaryKeys, foreignKeyColumns, comments, cancellationToken);

        return new DatabaseModel(
            options.Provider,
            connection.Database,
            tables.Select(table => new TableModel(
                table.SchemaName,
                table.TableName,
                columns.TryGetValue(table, out var c) ? c : [],
                comments.GetTableComment(table.SchemaName, table.TableName))).ToArray(),
            foreignKeys);
    }

    private static async Task<IReadOnlyList<TableRef>> ReadTablesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select table_schema, table_name
            from information_schema.tables
            where table_type = 'BASE TABLE'
              and table_schema not in ('pg_catalog', 'information_schema')
            order by table_schema, table_name;
            """;
        var result = new List<TableRef>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableRef(reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    private static async Task<Dictionary<TableRef, IReadOnlyList<ColumnModel>>> ReadColumnsAsync(NpgsqlConnection connection, HashSet<(string Table, string Column)> primaryKeys, HashSet<(string Table, string Column)> foreignKeyColumns, DatabaseComments comments, CancellationToken cancellationToken)
    {
        const string sql = """
            select table_schema, table_name, column_name,
                   coalesce(udt_name, data_type),
                   is_nullable = 'YES'
            from information_schema.columns
            where table_schema not in ('pg_catalog', 'information_schema')
            order by table_schema, table_name, ordinal_position;
            """;
        var result = new Dictionary<TableRef, List<ColumnModel>>();
        await using var command = new NpgsqlCommand(sql, connection);
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

            columns.Add(new ColumnModel(
                column,
                reader.GetString(3),
                reader.GetBoolean(4),
                primaryKeys.Contains((table.FullName, column)),
                foreignKeyColumns.Contains((table.FullName, column)),
                comments.GetColumnComment(table.SchemaName, table.TableName, column)));
        }

        return result.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ColumnModel>)pair.Value);
    }

    private static async Task<DatabaseComments> ReadCommentsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string tableSql = """
            select
                ns.nspname as schema_name,
                cls.relname as table_name,
                obj_description(cls.oid, 'pg_class') as comment
            from pg_class cls
            join pg_namespace ns on ns.oid = cls.relnamespace
            where cls.relkind in ('r', 'p')
              and ns.nspname not in ('pg_catalog', 'information_schema');
            """;

        const string columnSql = """
            select
                ns.nspname as schema_name,
                cls.relname as table_name,
                att.attname as column_name,
                col_description(cls.oid, att.attnum) as comment
            from pg_class cls
            join pg_namespace ns on ns.oid = cls.relnamespace
            join pg_attribute att on att.attrelid = cls.oid
            where cls.relkind in ('r', 'p')
              and att.attnum > 0
              and not att.attisdropped
              and ns.nspname not in ('pg_catalog', 'information_schema');
            """;

        var tableComments = new List<(string SchemaName, string TableName, string? Comment)>();
        await using (var command = new NpgsqlCommand(tableSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                tableComments.Add((reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
            }
        }

        var columnComments = new List<(string SchemaName, string TableName, string ColumnName, string? Comment)>();
        await using (var command = new NpgsqlCommand(columnSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                columnComments.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
            }
        }

        return new DatabaseComments(tableComments, columnComments);
    }

    private static async Task<HashSet<(string Table, string Column)>> ReadPrimaryKeysAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select ns.nspname, cls.relname, att.attname
            from pg_constraint con
            join pg_class cls on cls.oid = con.conrelid
            join pg_namespace ns on ns.oid = cls.relnamespace
            join unnest(con.conkey) with ordinality as cols(attnum, ord) on true
            join pg_attribute att on att.attrelid = cls.oid and att.attnum = cols.attnum
            where con.contype = 'p'
              and ns.nspname not in ('pg_catalog', 'information_schema');
            """;
        var result = new HashSet<(string Table, string Column)>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(($"{reader.GetString(0)}.{reader.GetString(1)}", reader.GetString(2)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<ForeignKeyModel>> ReadForeignKeysAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select con.conname,
                   src_ns.nspname, src_cls.relname, src_att.attname,
                   tgt_ns.nspname, tgt_cls.relname, tgt_att.attname,
                   src_cols.ord
            from pg_constraint con
            join pg_class src_cls on src_cls.oid = con.conrelid
            join pg_namespace src_ns on src_ns.oid = src_cls.relnamespace
            join pg_class tgt_cls on tgt_cls.oid = con.confrelid
            join pg_namespace tgt_ns on tgt_ns.oid = tgt_cls.relnamespace
            join unnest(con.conkey) with ordinality as src_cols(attnum, ord) on true
            join unnest(con.confkey) with ordinality as tgt_cols(attnum, ord) on tgt_cols.ord = src_cols.ord
            join pg_attribute src_att on src_att.attrelid = src_cls.oid and src_att.attnum = src_cols.attnum
            join pg_attribute tgt_att on tgt_att.attrelid = tgt_cls.oid and tgt_att.attnum = tgt_cols.attnum
            where con.contype = 'f'
              and src_ns.nspname not in ('pg_catalog', 'information_schema')
              and tgt_ns.nspname not in ('pg_catalog', 'information_schema')
            order by con.conname, src_cols.ord;
            """;
        var rows = new List<FkRow>();
        await using var command = new NpgsqlCommand(sql, connection);
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
