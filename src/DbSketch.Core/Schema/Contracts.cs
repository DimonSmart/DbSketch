using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Schema;

public interface IDatabaseSchemaReader
{
    Task<DatabaseModel> ReadAsync(DatabaseReadOptions options, CancellationToken cancellationToken);
}

public sealed record DatabaseReadOptions(string Provider, string ConnectionString, bool ReadComments = false);

public interface ISchemaFilter
{
    DatabaseModel Apply(DatabaseModel model, SchemaFilterOptions options);
}

public sealed record SchemaFilterOptions(
    IReadOnlyList<string> IncludeTables,
    IReadOnlyList<string> ExcludeTables);
