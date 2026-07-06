using DimonSmart.DbSketch.Cli;
using Microsoft.Data.Sqlite;

namespace DimonSmart.DbSketch.Tests.Integration;

public sealed class SqliteEndToEndTests
{
    [Fact]
    public async Task Generate_WithSqlite_WritesDotSchema()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DimonSmart.DbSketch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var dbPath = Path.Combine(tempDirectory, "app.db");
            var connectionString = $"Data Source={dbPath}";
            await ExecuteSqlAsync(connectionString, """
                create table categories
                (
                    category_id integer primary key,
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

            var outputPath = Path.Combine(tempDirectory, "sqlite.dot");
            var configPath = Path.Combine(tempDirectory, "dbsketch.yml");
            await File.WriteAllTextAsync(configPath, CreateConfig(connectionString, outputPath), cancellationToken);

            var exitCode = await DbSketchApp.RunAsync(["generate", "--config", configPath], cancellationToken);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var dot = await File.ReadAllTextAsync(outputPath, cancellationToken);

            Assert.Contains("digraph DbSketch", dot);
            Assert.Contains("SQLite schema", dot);
            Assert.Contains("main.categories", dot);
            Assert.Contains("main.products", dot);
            Assert.Contains("Product catalog", dot);
            Assert.Contains("Category reference", dot);
            Assert.Contains(">PK</TD>", dot);
            Assert.Contains(">FK</TD>", dot);
            Assert.Contains("fk_products_categories_0", dot);
            Assert.Contains("\"table_main_products\":\"col_category_id_fk\":e -> \"table_main_categories\":\"col_category_id\":w", dot);
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

    private static async Task ExecuteSqlAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateConfig(string connectionString, string outputPath) =>
        $$"""
        provider: sqlite
        connectionString: {{ToYamlSingleQuoted(connectionString)}}

        comments:
          enabled: true
          overrides:
            tables:
              - schema: main
                name: products
                comment: Product catalog
                columns:
                  category_id: Category reference

        diagrams:
          - name: sqlite
            title: SQLite schema
            include:
              tables:
                - "main.*"
            output:
              path: {{ToYamlSingleQuoted(outputPath)}}
              format: raw
            diagram:
              renderer: dot
              direction: LR
              compact: true
              columnLayout: "{name} | {type} | {keys} | {comment}"
              show:
                schemaName: true
                tableComments: true
                columnComments: true
        """;

    private static string ToYamlSingleQuoted(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
