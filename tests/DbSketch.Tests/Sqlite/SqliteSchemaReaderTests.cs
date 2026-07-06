using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;
using DimonSmart.DbSketch.Sqlite;
using Microsoft.Data.Sqlite;

namespace DimonSmart.DbSketch.Tests.Sqlite;

public sealed class SqliteSchemaReaderTests
{
    [Fact]
    public async Task ReadAsync_ReadsTablesColumnsPrimaryKeysAndForeignKeys()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbPath = CreateTempDatabasePath();
        try
        {
            var connectionString = $"Data Source={dbPath}";
            await ExecuteSqlAsync(connectionString, """
                create table categories
                (
                    category_id integer primary key autoincrement,
                    category_name text not null
                );

                create table products
                (
                    product_id integer primary key,
                    product_name text not null,
                    category_id integer not null,
                    price numeric,
                    foreign key (category_id) references categories(category_id)
                );
                """,
                cancellationToken);

            var model = await new SqliteSchemaReader().ReadAsync(
                new DatabaseReadOptions("sqlite", connectionString, ReadComments: true),
                cancellationToken);

            Assert.Equal("sqlite", model.Provider);
            Assert.Equal(Path.GetFileName(dbPath), model.DatabaseName);
            Assert.DoesNotContain(model.Tables, table => table.Name == "sqlite_sequence");

            var categories = AssertTable(model, "main", "categories");
            var products = AssertTable(model, "main", "products");

            Assert.True(AssertColumn(categories, "category_id").IsPrimaryKey);
            Assert.False(AssertColumn(categories, "category_name").IsNullable);

            var categoryId = AssertColumn(products, "category_id");
            Assert.True(categoryId.IsForeignKey);
            Assert.Equal("numeric", AssertColumn(products, "price").StoreType);

            var foreignKey = Assert.Single(model.ForeignKeys);
            Assert.Equal("fk_products_categories_0", foreignKey.Name);
            Assert.Equal(new TableRef("main", "products"), foreignKey.SourceTable);
            Assert.Equal(["category_id"], foreignKey.SourceColumns);
            Assert.Equal(new TableRef("main", "categories"), foreignKey.TargetTable);
            Assert.Equal(["category_id"], foreignKey.TargetColumns);
        }
        finally
        {
            DeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task ReadAsync_GroupsCompositeForeignKeyColumnsByIdAndSeq()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbPath = CreateTempDatabasePath();
        try
        {
            var connectionString = $"Data Source={dbPath}";
            await ExecuteSqlAsync(connectionString, """
                create table parent
                (
                    a integer not null,
                    b integer not null,
                    primary key (a, b)
                );

                create table child
                (
                    a integer not null,
                    b integer not null,
                    foreign key (a, b) references parent(a, b)
                );
                """,
                cancellationToken);

            var model = await new SqliteSchemaReader().ReadAsync(
                new DatabaseReadOptions("sqlite", connectionString),
                cancellationToken);

            var foreignKey = Assert.Single(model.ForeignKeys);
            Assert.Equal(["a", "b"], foreignKey.SourceColumns);
            Assert.Equal(["a", "b"], foreignKey.TargetColumns);
        }
        finally
        {
            DeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task ReadAsync_ResolvesImplicitForeignKeyTargetPrimaryKeyColumns()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbPath = CreateTempDatabasePath();
        try
        {
            var connectionString = $"Data Source={dbPath}";
            await ExecuteSqlAsync(connectionString, """
                create table parent
                (
                    a integer not null,
                    b integer not null,
                    primary key (a, b)
                );

                create table child
                (
                    a integer not null,
                    b integer not null,
                    foreign key (a, b) references parent
                );
                """,
                cancellationToken);

            var model = await new SqliteSchemaReader().ReadAsync(
                new DatabaseReadOptions("sqlite", connectionString),
                cancellationToken);

            var foreignKey = Assert.Single(model.ForeignKeys);
            Assert.Equal(["a", "b"], foreignKey.TargetColumns);
        }
        finally
        {
            DeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task ReadAsync_WithReadOnlyMissingFile_FailsInsteadOfCreatingEmptyDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbPath = CreateTempDatabasePath();
        var connectionString = $"Data Source={dbPath};Mode=ReadOnly";

        await Assert.ThrowsAsync<SqliteException>(() =>
            new SqliteSchemaReader().ReadAsync(
                new DatabaseReadOptions("sqlite", connectionString),
                cancellationToken));

        Assert.False(File.Exists(dbPath));
    }

    [Fact]
    public async Task ReadOpenConnectionAsync_ReadsAttachedDatabasesAndAllowsSchemaFiltering()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var mainDbPath = CreateTempDatabasePath();
        var auxDbPath = CreateTempDatabasePath();

        try
        {
            await using var connection = new SqliteConnection($"Data Source={mainDbPath}");
            await connection.OpenAsync(cancellationToken);
            await ExecuteSqlAsync(connection, $"""
                attach database {ToSqlStringLiteral(auxDbPath)} as aux;

                create table main.local_table
                (
                    id integer primary key
                );

                create table aux.external_table
                (
                    id integer primary key,
                    name text not null
                );
                """,
                cancellationToken);

            var model = await SqliteSchemaReader.ReadOpenConnectionAsync(
                connection,
                new DatabaseReadOptions("sqlite", connection.ConnectionString),
                cancellationToken);

            AssertTable(model, "main", "local_table");
            AssertTable(model, "aux", "external_table");

            var filtered = new WildcardSchemaFilter().Apply(model, new SchemaFilterOptions(["aux.*"], []));
            var table = Assert.Single(filtered.Tables);
            Assert.Equal("aux.external_table", table.FullName);
        }
        finally
        {
            DeleteDatabase(mainDbPath);
            DeleteDatabase(auxDbPath);
        }
    }

    private static TableModel AssertTable(DatabaseModel model, string schemaName, string tableName) =>
        Assert.Single(model.Tables, table => table.SchemaName == schemaName && table.Name == tableName);

    private static ColumnModel AssertColumn(TableModel table, string columnName) =>
        Assert.Single(table.Columns, column => column.Name == columnName);

    private static string CreateTempDatabasePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DimonSmart.DbSketch.Tests");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{Guid.NewGuid():N}.db");
    }

    private static async Task ExecuteSqlAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await ExecuteSqlAsync(connection, sql, cancellationToken);
    }

    private static async Task ExecuteSqlAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ToSqlStringLiteral(string value) =>
        "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static void DeleteDatabase(string dbPath)
    {
        try
        {
            File.Delete(dbPath);
        }
        catch
        {
            // Ignore cleanup errors in tests.
        }
    }
}
