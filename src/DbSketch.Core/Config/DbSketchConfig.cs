namespace DimonSmart.DbSketch.Core.Config;

public sealed class DbSketchConfig
{
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public IncludeExcludeConfig Include { get; init; } = new();
    public IncludeExcludeConfig Exclude { get; init; } = new();
    public OutputConfig Output { get; init; } = new();
    public DiagramConfig Diagram { get; init; } = new();
    public DescriptionsConfig Descriptions { get; init; } = new();
}

public sealed class IncludeExcludeConfig
{
    public List<string> Tables { get; init; } = [];
}

public sealed class OutputConfig
{
    public string? Path { get; init; }
    public string Format { get; init; } = "dot";
}

public sealed class DiagramConfig
{
    public string? Title { get; init; }
    public string Rankdir { get; init; } = "LR";
    public bool Compact { get; init; } = true;
    public DiagramShowConfig Show { get; init; } = new();
}

public sealed class DiagramShowConfig
{
    public bool SchemaName { get; init; } = true;
    public bool ColumnTypes { get; init; }
    public bool Nullability { get; init; }
    public bool PrimaryKeys { get; init; } = true;
    public bool ForeignKeys { get; init; } = true;
}

public sealed class DescriptionsConfig
{
    public bool Enabled { get; init; }
}
