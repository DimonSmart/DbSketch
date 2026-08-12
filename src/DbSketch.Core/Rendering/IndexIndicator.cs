using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public enum IndexIndicator { None, Simple, SimpleUnique, Complex }

public static class IndexIndicatorClassifier
{
    public static IndexIndicator Classify(TableModel table, ColumnModel column, IReadOnlyList<IndexModel>? indexes)
    {
        var matches = (indexes ?? []).Where(index => !index.IsPrimaryKeyBacking &&
            string.Equals(index.Table.FullName, table.FullName, StringComparison.OrdinalIgnoreCase) &&
            (index.KeyColumns.Any(key => string.Equals(key.Name, column.Name, StringComparison.OrdinalIgnoreCase)) ||
             (index.IncludedColumns ?? []).Any(name => string.Equals(name, column.Name, StringComparison.OrdinalIgnoreCase)))).ToArray();
        if (matches.Length == 0) return IndexIndicator.None;
        if (matches.Length != 1) return IndexIndicator.Complex;
        var index = matches[0];
        if (index.KeyColumns.Count != 1 || (index.IncludedColumns?.Count ?? 0) != 0 || !string.IsNullOrWhiteSpace(index.Filter) || !string.Equals(index.KeyColumns[0].Name, column.Name, StringComparison.OrdinalIgnoreCase)) return IndexIndicator.Complex;
        return index.IsUnique ? IndexIndicator.SimpleUnique : IndexIndicator.Simple;
    }

    public static string Format(IndexIndicator indicator) => indicator switch { IndexIndicator.Simple => "IDX", IndexIndicator.SimpleUnique => "UQ", IndexIndicator.Complex => "*", _ => "" };
}
