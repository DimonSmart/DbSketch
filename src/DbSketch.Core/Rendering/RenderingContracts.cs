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

public enum DiagramStyle
{
    Classic,
    Readable,
    Compact,
    Soft,
    Blueprint,
    Contrast
}

public sealed record DiagramRenderOptions(
    string Title,
    DiagramDirection Direction,
    DiagramStyle Style,
    bool Compact,
    DiagramLayoutOptions Layout,
    DiagramShowOptions Show,
    MermaidRenderOptions Mermaid,
    DiagramCommentRenderOptions Comments,
    GraphvizDotRenderOptions Dot);

public sealed record DiagramLayoutOptions(
    string? ColumnLayout,
    string? TableHeaderLayout);

public sealed record MermaidRenderOptions(bool EmitDirection);

public sealed record DiagramCommentRenderOptions(int? MaxLength);

public sealed record GraphvizDotRenderOptions(
    GraphvizDotGraphRenderOptions Graph,
    GraphvizDotNodeRenderOptions Node,
    GraphvizDotEdgeRenderOptions Edge,
    GraphvizDotTableRenderOptions Table);

public sealed record GraphvizDotGraphRenderOptions(
    string? FontName,
    int? FontSize,
    double? Nodesep,
    double? Ranksep,
    string? BackgroundColor);

public sealed record GraphvizDotNodeRenderOptions(
    string? FontName,
    int? FontSize);

public sealed record GraphvizDotEdgeRenderOptions(
    string? FontName,
    int? FontSize,
    string? Color,
    double? PenWidth,
    double? ArrowSize);

public sealed record GraphvizDotTableRenderOptions(
    string? BorderColor,
    string? HeaderBackground,
    int? CellPadding);

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
