using System.Text.RegularExpressions;
using DbSketch.Core.Model;
using DbSketch.Core.Schema;

namespace DbSketch.Core.Filtering;

public sealed class WildcardSchemaFilter : ISchemaFilter
{
    public DatabaseModel Apply(DatabaseModel model, SchemaFilterOptions options)
    {
        var included = model.Tables
            .Where(table => IsIncluded(table.FullName, options.IncludeTables))
            .Where(table => !MatchesAny(table.FullName, options.ExcludeTables))
            .ToArray();

        var tableNames = included
            .Select(table => table.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var foreignKeys = model.ForeignKeys
            .Where(fk => tableNames.Contains(fk.SourceTable.FullName) && tableNames.Contains(fk.TargetTable.FullName))
            .ToArray();

        return model with { Tables = included, ForeignKeys = foreignKeys };
    }

    private static bool IsIncluded(string value, IReadOnlyList<string> patterns) =>
        patterns.Count == 0 || MatchesAny(value, patterns);

    private static bool MatchesAny(string value, IReadOnlyList<string> patterns) =>
        patterns.Any(pattern => WildcardRegex(pattern).IsMatch(value));

    private static Regex WildcardRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal);
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
