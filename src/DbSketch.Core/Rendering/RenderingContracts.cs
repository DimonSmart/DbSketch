using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public interface IDiagramRenderer
{
    string Render(DatabaseModel model, DiagramRenderOptions options);
}

public sealed record DiagramRenderOptions(
    string Title,
    string Rankdir,
    bool Compact,
    DiagramShowOptions Show);

public sealed record DiagramShowOptions(
    bool SchemaName,
    bool ColumnTypes,
    bool Nullability,
    bool PrimaryKeys,
    bool ForeignKeys);
