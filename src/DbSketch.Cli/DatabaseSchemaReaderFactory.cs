using DimonSmart.DbSketch.Core.Schema;
using DimonSmart.DbSketch.MySql;
using DimonSmart.DbSketch.Postgres;
using DimonSmart.DbSketch.SqlServer;

namespace DimonSmart.DbSketch.Cli;

public interface IDatabaseSchemaReaderFactory
{
    IDatabaseSchemaReader Create(string provider);
}

public sealed class DatabaseSchemaReaderFactory : IDatabaseSchemaReaderFactory
{
    public IDatabaseSchemaReader Create(string provider) => provider switch
    {
        "sqlserver" => new SqlServerSchemaReader(),
        "postgres" => new PostgresSchemaReader(),
        "mysql" => new MySqlSchemaReader(),
        _ => throw new CliException($"Unknown provider '{provider}'.")
    };
}
