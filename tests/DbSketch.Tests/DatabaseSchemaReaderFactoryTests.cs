using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Sqlite;

namespace DimonSmart.DbSketch.Tests;

public sealed class DatabaseSchemaReaderFactoryTests
{
    [Fact]
    public void Create_WithSqlite_ReturnsSqliteSchemaReader()
    {
        var reader = new DatabaseSchemaReaderFactory().Create("sqlite");

        Assert.IsType<SqliteSchemaReader>(reader);
    }
}
