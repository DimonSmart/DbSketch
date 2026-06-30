using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Descriptions;

public interface IDescriptionProvider
{
    Task<TableDescription?> GetTableDescriptionAsync(TableModel table, CancellationToken cancellationToken);

    Task<ColumnDescription?> GetColumnDescriptionAsync(TableModel table, ColumnModel column, CancellationToken cancellationToken);
}

public sealed record TableDescription(string Text);

public sealed record ColumnDescription(string Text);

public sealed class NullDescriptionProvider : IDescriptionProvider
{
    public Task<TableDescription?> GetTableDescriptionAsync(TableModel table, CancellationToken cancellationToken) =>
        Task.FromResult<TableDescription?>(null);

    public Task<ColumnDescription?> GetColumnDescriptionAsync(TableModel table, ColumnModel column, CancellationToken cancellationToken) =>
        Task.FromResult<ColumnDescription?>(null);
}
