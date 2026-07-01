using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public interface IDiagramRenderer
{
    DiagramRendererCapabilities Capabilities { get; }

    string Render(DatabaseModel model, DiagramRenderOptions options);
}

public sealed record DiagramRendererCapabilities(bool SupportsColumnToColumnRelationships);

public enum DiagramDirection
{
    TB,
    BT,
    LR,
    RL
}

public sealed record DiagramRenderOptions(
    string Title,
    DiagramDirection Direction,
    bool Compact,
    DiagramShowOptions Show,
    MermaidRenderOptions Mermaid);

public sealed record MermaidRenderOptions(bool EmitDirection);

public sealed record DiagramShowOptions(
    bool SchemaName,
    bool ColumnTypes,
    bool Nullability,
    bool PrimaryKeys,
    bool ForeignKeys);
