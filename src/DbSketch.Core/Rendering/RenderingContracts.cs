using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public interface IDiagramRenderer
{
    DiagramRendererCapabilities Capabilities { get; }

    string Render(DatabaseModel model, DiagramRenderOptions options);
}

public sealed record DiagramRendererCapabilities(bool SupportsColumnToColumnRelationships);

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
