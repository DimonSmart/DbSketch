using DimonSmart.DbSketch.Core.Schema;
using DimonSmart.DbSketch.SqlServer;

namespace DimonSmart.DbSketch.Tests;

public sealed class DatabaseConnectionTests
{
    [Fact]
    public async Task SqlServerReaderRejectsPostgresConnectionStringWithProviderHint()
    {
        var reader = new SqlServerSchemaReader();

        var exception = await Assert.ThrowsAsync<DatabaseConnectionException>(() =>
            reader.ReadAsync(new DatabaseReadOptions("sqlserver", "Host=localhost;Database=app"), CancellationToken.None));

        Assert.Contains("Invalid sqlserver connectionString", exception.Message);
        Assert.Contains("provider: postgres", exception.Message);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }
}
