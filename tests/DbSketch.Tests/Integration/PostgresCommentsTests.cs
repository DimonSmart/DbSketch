using DimonSmart.DbSketch.Core.Schema;
using DimonSmart.DbSketch.Postgres;
using DimonSmart.DbSketch.Tests.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DimonSmart.DbSketch.Tests.Integration;

public sealed class PostgresCommentsTests
{
    [ManualIntegrationFact]
    [Trait("Category", "ManualIntegration")]
    public async Task ReadAsync_WhenReadCommentsIsTrue_ReadsTableAndColumnComments()
    {
        await using var postgres = await StartPostgresAsync();
        var connectionString = postgres.GetConnectionString();
        await CreateCommentedSchemaAsync(connectionString);

        var model = await new PostgresSchemaReader().ReadAsync(
            new DatabaseReadOptions("postgres", connectionString, ReadComments: true),
            CancellationToken.None);

        var table = model.Tables.Single(table => table.SchemaName == "public" && table.Name == "users");
        Assert.Equal("Application users", table.Comment);

        var id = table.Columns.Single(column => column.Name == "id");
        Assert.Equal("User identifier", id.Comment);
    }

    [ManualIntegrationFact]
    [Trait("Category", "ManualIntegration")]
    public async Task ReadAsync_WhenReadCommentsIsFalse_LeavesCommentsNull()
    {
        await using var postgres = await StartPostgresAsync();
        var connectionString = postgres.GetConnectionString();
        await CreateCommentedSchemaAsync(connectionString);

        var model = await new PostgresSchemaReader().ReadAsync(
            new DatabaseReadOptions("postgres", connectionString, ReadComments: false),
            CancellationToken.None);

        var table = model.Tables.Single(table => table.SchemaName == "public" && table.Name == "users");
        Assert.Null(table.Comment);

        var id = table.Columns.Single(column => column.Name == "id");
        Assert.Null(id.Comment);
    }

    private static async Task<PostgreSqlContainer> StartPostgresAsync()
    {
        var postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("dbsketch_comments")
            .WithUsername("dbsketch")
            .WithPassword("dbsketch")
            .Build();

        await postgres.StartAsync();
        return postgres;
    }

    private static async Task CreateCommentedSchemaAsync(string connectionString)
    {
        const string sql = """
            create table public.users (
                id integer primary key,
                name text
            );

            comment on table public.users is 'Application users';
            comment on column public.users.id is 'User identifier';
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
