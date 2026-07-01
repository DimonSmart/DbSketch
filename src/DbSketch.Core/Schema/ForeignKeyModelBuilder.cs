using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Schema;

public sealed record ForeignKeyColumnRow(
    string? ConstraintSchema,
    string Name,
    TableRef SourceTable,
    string SourceColumn,
    TableRef TargetTable,
    string TargetColumn,
    int Ordinal);

public static class ForeignKeyModelBuilder
{
    public static IReadOnlyList<ForeignKeyModel> Build(IEnumerable<ForeignKeyColumnRow> rows) =>
        rows.GroupBy(row => new
        {
            ConstraintSchema = row.ConstraintSchema ?? string.Empty,
            row.SourceTable.SchemaName,
            row.SourceTable.TableName,
            row.Name
        })
        .Select(group =>
        {
            var ordered = group.OrderBy(row => row.Ordinal).ToArray();
            return new ForeignKeyModel(
                ordered[0].Name,
                ordered[0].SourceTable,
                ordered.Select(row => row.SourceColumn).ToArray(),
                ordered[0].TargetTable,
                ordered.Select(row => row.TargetColumn).ToArray());
        })
        .ToArray();
}
