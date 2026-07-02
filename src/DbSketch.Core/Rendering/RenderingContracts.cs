using DimonSmart.DbSketch.Core.Model;

namespace DimonSmart.DbSketch.Core.Rendering;

public interface IDiagramRenderer
{
    DiagramRendererCapabilities Capabilities { get; }

    string Render(DatabaseModel model, DiagramRenderOptions options);
}

public sealed record DiagramRendererCapabilities(
    bool SupportsColumnToColumnRelationships,
    bool SupportsCustomTableLayouts);

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
    DiagramLayoutOptions Layout,
    DiagramShowOptions Show,
    MermaidRenderOptions Mermaid,
    DiagramCommentRenderOptions Comments);

public sealed record DiagramLayoutOptions(
    string? ColumnLayout,
    string? TableHeaderLayout);

public sealed record MermaidRenderOptions(bool EmitDirection);

public sealed record DiagramCommentRenderOptions(int? MaxLength);

public sealed record DiagramShowOptions(
    bool SchemaName,
    bool ColumnTypes,
    bool Nullability,
    bool PrimaryKeys,
    bool ForeignKeys,
    bool ForeignKeyLabels,
    bool SelfReferencingForeignKeys,
    bool TableComments,
    bool ColumnComments);
