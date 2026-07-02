namespace DimonSmart.DbSketch.Core.Config;

public sealed record class DbSketchConfig
{
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public DatabaseConfig Database { get; init; } = new();
    public CommentsConfig Comments { get; init; } = new();
    public DefaultsConfig Defaults { get; init; } = new();
    public List<DiagramTargetConfig> Diagrams { get; init; } = [];
}

public sealed class DatabaseConfig
{
    public int? CommandTimeoutSeconds { get; init; }
}

public sealed class IncludeExcludeConfig
{
    public List<string> Tables { get; init; } = [];
}

public sealed class DefaultsConfig
{
    public OutputDefaultsConfig Output { get; init; } = new();
    public DiagramConfig Diagram { get; init; } = new();
}

public sealed class DiagramTargetConfig
{
    public string? Name { get; init; }
    public string? Title { get; init; }
    public IncludeExcludeConfig Include { get; init; } = new();
    public IncludeExcludeConfig Exclude { get; init; } = new();
    public OutputOverrideConfig? Output { get; init; }
    public DiagramOverrideConfig? Diagram { get; init; }
}

public sealed class OutputDefaultsConfig
{
    public string Format { get; init; } = "raw";
    public MarkdownOutputConfig Markdown { get; init; } = new();
}

public sealed class OutputOverrideConfig
{
    public string? Path { get; init; }
    public string? Format { get; init; }
    public MarkdownOutputOverrideConfig? Markdown { get; init; }
}

public sealed class MarkdownOutputConfig
{
    public string? Header { get; init; }
    public string? Footer { get; init; }
    public string? FenceLanguage { get; init; }
}

public sealed class MarkdownOutputOverrideConfig
{
    public string? Header { get; init; }
    public string? Footer { get; init; }
    public string? FenceLanguage { get; init; }
}

public sealed class DiagramConfig
{
    public string? Title { get; init; }
    public string Renderer { get; init; } = "dot";
    public string Direction { get; init; } = "LR";
    public string Style { get; init; } = "classic";
    public bool Compact { get; init; } = true;
    public string? ColumnLayout { get; init; }
    public string? TableHeaderLayout { get; init; }
    public DiagramShowConfig Show { get; init; } = new();
    public MermaidConfig Mermaid { get; init; } = new();
    public DiagramCommentsConfig Comments { get; init; } = new();
    public DotConfig Dot { get; init; } = new();
}

public sealed class DiagramOverrideConfig
{
    public string? Renderer { get; init; }
    public string? Direction { get; init; }
    public string? Style { get; init; }
    public bool? Compact { get; init; }
    public string? ColumnLayout { get; init; }
    public string? TableHeaderLayout { get; init; }
    public DiagramShowOverrideConfig? Show { get; init; }
    public MermaidOverrideConfig? Mermaid { get; init; }
    public DiagramCommentsOverrideConfig? Comments { get; init; }
    public DotOverrideConfig? Dot { get; init; }
}

public sealed class DotConfig
{
    public DotGraphConfig Graph { get; init; } = new();
    public DotNodeConfig Node { get; init; } = new();
    public DotEdgeConfig Edge { get; init; } = new();
    public DotTableConfig Table { get; init; } = new();
}

public sealed class DotGraphConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
    public double? Nodesep { get; init; }
    public double? Ranksep { get; init; }
    public string? BackgroundColor { get; init; }
}

public sealed class DotNodeConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
}

public sealed class DotEdgeConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
    public string? Color { get; init; }
    public double? PenWidth { get; init; }
    public double? ArrowSize { get; init; }
}

public sealed class DotTableConfig
{
    public string? BorderColor { get; init; }
    public string? HeaderBackground { get; init; }
    public int? CellPadding { get; init; }
}

public sealed class DotOverrideConfig
{
    public DotGraphOverrideConfig? Graph { get; init; }
    public DotNodeOverrideConfig? Node { get; init; }
    public DotEdgeOverrideConfig? Edge { get; init; }
    public DotTableOverrideConfig? Table { get; init; }
}

public sealed class DotGraphOverrideConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
    public double? Nodesep { get; init; }
    public double? Ranksep { get; init; }
    public string? BackgroundColor { get; init; }
}

public sealed class DotNodeOverrideConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
}

public sealed class DotEdgeOverrideConfig
{
    public string? FontName { get; init; }
    public int? FontSize { get; init; }
    public string? Color { get; init; }
    public double? PenWidth { get; init; }
    public double? ArrowSize { get; init; }
}

public sealed class DotTableOverrideConfig
{
    public string? BorderColor { get; init; }
    public string? HeaderBackground { get; init; }
    public int? CellPadding { get; init; }
}

public sealed class MermaidConfig
{
    public bool EmitDirection { get; init; }
}

public sealed class MermaidOverrideConfig
{
    public bool? EmitDirection { get; init; }
}

public sealed class DiagramShowConfig
{
    public bool SchemaName { get; init; } = true;
    public bool ColumnTypes { get; init; }
    public bool Nullability { get; init; }
    public bool PrimaryKeys { get; init; } = true;
    public bool ForeignKeys { get; init; } = true;
    public bool ForeignKeyLabels { get; init; } = true;
    public bool SelfReferencingForeignKeys { get; init; } = true;
    public bool TableComments { get; init; }
    public bool ColumnComments { get; init; }
}

public sealed class DiagramShowOverrideConfig
{
    public bool? SchemaName { get; init; }
    public bool? ColumnTypes { get; init; }
    public bool? Nullability { get; init; }
    public bool? PrimaryKeys { get; init; }
    public bool? ForeignKeys { get; init; }
    public bool? ForeignKeyLabels { get; init; }
    public bool? SelfReferencingForeignKeys { get; init; }
    public bool? TableComments { get; init; }
    public bool? ColumnComments { get; init; }
}

public sealed class DiagramCommentsConfig
{
    public int? MaxLength { get; init; }
}

public sealed class DiagramCommentsOverrideConfig
{
    public int? MaxLength { get; init; }
}

public sealed class CommentsConfig
{
    public bool Enabled { get; init; }
    public CommentOverridesConfig Overrides { get; init; } = new();
}

public sealed class CommentOverridesConfig
{
    public List<TableCommentOverrideConfig> Tables { get; init; } = [];
}

public sealed class TableCommentOverrideConfig
{
    public string? Schema { get; init; }
    public string? Name { get; init; }
    public string? Comment { get; init; }
    public Dictionary<string, string?> Columns { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
