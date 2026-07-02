using DimonSmart.DbSketch.Cli;

namespace DimonSmart.DbSketch.Tests;

public sealed class SampleConfigTests
{
    [Fact]
    public void SampleConfigs_LoadWithYamlSensitiveConnectionStrings()
    {
        var repoRoot = FindRepoRoot();

        const string sqlServerConnection = "Server=localhost;Database=DbSketch Sample;User Id=sa;Password=p: a # b;TrustServerCertificate=True";
        const string postgresConnection = "Host=localhost;Port=5432;Database=dbsketch sample;Username=dbsketch;Password=p: a # b";
        const string mysqlConnection = "Server=localhost;Database=dbsketch sample;User=dbsketch;Password=p: a # b";

        Environment.SetEnvironmentVariable("DBSKETCH_SQLSERVER_CONNECTION", sqlServerConnection);
        Environment.SetEnvironmentVariable("DBSKETCH_POSTGRES_CONNECTION", postgresConnection);
        Environment.SetEnvironmentVariable("DBSKETCH_MYSQL_CONNECTION", mysqlConnection);

        try
        {
            var sqlServerConfig = ConfigLoader.Load(Path.Combine(repoRoot, "samples", "sqlserver", "dbsketch.yml"));
            var postgresConfig = ConfigLoader.Load(Path.Combine(repoRoot, "samples", "postgres", "dbsketch.yml"));
            var mysqlConfig = ConfigLoader.Load(Path.Combine(repoRoot, "samples", "mysql", "dbsketch.yml"));

            Assert.Equal(sqlServerConnection, sqlServerConfig.ConnectionString);
            Assert.Equal(postgresConnection, postgresConfig.ConnectionString);
            Assert.Equal(mysqlConnection, mysqlConfig.ConnectionString);

            AssertSampleDiagrams(sqlServerConfig.Diagrams);
            AssertSampleDiagrams(postgresConfig.Diagrams);
            AssertSampleDiagrams(mysqlConfig.Diagrams);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DBSKETCH_SQLSERVER_CONNECTION", null);
            Environment.SetEnvironmentVariable("DBSKETCH_POSTGRES_CONNECTION", null);
            Environment.SetEnvironmentVariable("DBSKETCH_MYSQL_CONNECTION", null);
        }
    }

    private static void AssertSampleDiagrams(IReadOnlyCollection<Core.Config.DiagramTargetConfig> diagrams)
    {
        Assert.NotEmpty(diagrams);
        Assert.All(diagrams, diagram => Assert.False(string.IsNullOrWhiteSpace(diagram.Name)));
        Assert.All(diagrams, diagram => Assert.False(string.IsNullOrWhiteSpace(diagram.Output?.Path)));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DbSketch.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
