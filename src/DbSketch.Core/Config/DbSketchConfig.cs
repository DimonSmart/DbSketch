namespace DimonSmart.DbSketch.Core.Config;

public sealed class DbSketchConfig
{
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public IncludeExcludeConfig Include { get; init; } = new();
    public IncludeExcludeConfig Exclude { get; init; } = new();
    public OutputConfig Output { get; init; } = new();
    public DiagramConfig Diagram { get; init; } = new();
    public CommentsConfig Comments { get; init; } = new();
}

public sealed class IncludeExcludeConfig
{
    public List<string> Tables { get; init; } = [];
}

public sealed class OutputConfig
{
    public string? Path { get; init; }
    public string Format { get; init; } = "raw";
    public MarkdownOutputConfig Markdown { get; init; } = new();
}

public sealed class MarkdownOutputConfig
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
    public bool Compact { get; init; } = true;
    public DiagramShowConfig Show { get; init; } = new();
    public MermaidConfig Mermaid { get; init; } = new();
}

public sealed class MermaidConfig
{
    public bool EmitDirection { get; init; }
}

public sealed class DiagramShowConfig
{
    public bool SchemaName { get; init; } = true;
    public bool ColumnTypes { get; init; }
    public bool Nullability { get; init; }
    public bool PrimaryKeys { get; init; } = true;
    public bool ForeignKeys { get; init; } = true;
    public bool Comments { get; init; }
}

public sealed class CommentsConfig
{
    public bool Enabled { get; init; }
}
