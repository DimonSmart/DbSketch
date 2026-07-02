using DimonSmart.DbSketch.Cli;
using DimonSmart.DbSketch.Tests.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DimonSmart.DbSketch.Tests.Integration;

public sealed class PostgresNorthwindEndToEndTests
{
    [ManualIntegrationFact]
    [Trait("Category", "ManualIntegration")]
    public async Task Generate_WithPostgresNorthwind_WritesDotSchema()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("dbsketch_northwind")
            .WithUsername("dbsketch")
            .WithPassword("dbsketch")
            .Build();

        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();
        await ApplySchemaAsync(connectionString);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "DimonSmart.DbSketch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var outputPath = Path.Combine(tempDirectory, "northwind.dot");
            var configPath = Path.Combine(tempDirectory, "dbsketch.yml");
            await File.WriteAllTextAsync(configPath, CreateConfig(connectionString, outputPath));

            var exitCode = await DbSketchApp.RunAsync(["generate", "--config", configPath], CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var dot = await File.ReadAllTextAsync(outputPath);

            Assert.Contains("digraph DbSketch", dot);
            Assert.Contains("Northwind schema", dot);

            Assert.Contains("northwind.customers", dot);
            Assert.Contains("northwind.orders", dot);
            Assert.Contains("northwind.order_details", dot);
            Assert.Contains("northwind.products", dot);
            Assert.Contains("northwind.categories", dot);
            Assert.Contains("northwind.employees", dot);

            Assert.Contains("col_customer_id\" ALIGN=\"LEFT\">customer_id", dot);
            Assert.Contains("col_order_id\" ALIGN=\"LEFT\">order_id", dot);
            Assert.Contains("col_product_id\" ALIGN=\"LEFT\">product_id", dot);
            Assert.Contains("<FONT POINT-SIZE=\"9\">PK</FONT>", dot);
            Assert.Contains("<FONT POINT-SIZE=\"9\">FK</FONT>", dot);

            Assert.Contains("fk_orders_customers", dot);
            Assert.Contains("fk_orders_employees", dot);
            Assert.Contains("fk_order_details_orders", dot);
            Assert.Contains("fk_order_details_products", dot);
            Assert.Contains("fk_products_categories", dot);

            Assert.Contains("\"table_northwind_orders\":\"col_customer_id_fk\":e -> \"table_northwind_customers\":\"col_customer_id\":w", dot);
            Assert.Contains("\"table_northwind_order_details\":\"col_product_id_fk\":e -> \"table_northwind_products\":\"col_product_id\":w", dot);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests.
            }
        }
    }

    private static async Task ApplySchemaAsync(string connectionString)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Northwind", "postgres-northwind-schema.sql");
        var sql = await File.ReadAllTextAsync(scriptPath);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string CreateConfig(string connectionString, string outputPath) =>
        $$"""
        provider: postgres
        connectionString: {{ToYamlSingleQuoted(connectionString)}}

        diagrams:
          - name: northwind
            title: Northwind schema
            include:
              tables:
                - "northwind.*"
            output:
              path: {{ToYamlSingleQuoted(outputPath)}}
              format: raw
            diagram:
              renderer: dot
              direction: LR
              compact: true
              show:
                schemaName: true
                columnTypes: false
                nullability: false
                primaryKeys: true
                foreignKeys: true
        """;

    private static string ToYamlSingleQuoted(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
