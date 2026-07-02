using DimonSmart.DbSketch.Cli.Console;
using DimonSmart.DbSketch.Core.Filtering;
using DimonSmart.DbSketch.Core.Rendering;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Cli.Generation;

public sealed record GenerateResult(string OutputPath, bool WroteOutput);

public sealed class DbSketchGenerator(
    IDatabaseSchemaReaderFactory readerFactory,
    IDiagramRendererFactory rendererFactory,
    ISchemaFilter schemaFilter,
    ICommandLineConsole console)
{
    public async Task<IReadOnlyList<GenerateResult>> GenerateAsync(ResolvedGenerateOptions options, CancellationToken cancellationToken)
    {
        var progress = new ProgressReporter(console, options.Quiet, options.NoProgress, options.Verbose);

        progress.Info("DbSketch");
        progress.Info($"Provider: {options.Provider}");
        var reader = readerFactory.Create(options.Provider);
        progress.Verbose("Connection string: provided");
        progress.Verbose($"Provider reader: {reader.GetType().Name}");
        progress.Verbose($"Comments: {(options.ReadComments ? "enabled" : "disabled")}");
        progress.Verbose($"Command timeout: {options.CommandTimeoutSeconds?.ToString() ?? "provider default"}");

        progress.Info("Reading database schema...");
        var model = await reader.ReadAsync(
            new DatabaseReadOptions(options.Provider, options.ConnectionString, options.ReadComments, options.CommandTimeoutSeconds),
            cancellationToken);
        var commented = CommentOverrideApplier.Apply(model, options.CommentOverrides);
        progress.Info($"Diagrams: {options.Diagrams.Count}");

        var results = new List<GenerateResult>(options.Diagrams.Count);
        foreach (var diagram in options.Diagrams)
        {
            var filtered = schemaFilter.Apply(commented, diagram.Filter);
            progress.Verbose($"Included tables: {string.Join(", ", diagram.Filter.IncludeTables)}");
            progress.Verbose($"Excluded tables: {string.Join(", ", diagram.Filter.ExcludeTables)}");
            progress.Info("");
            progress.Info($"Diagram: {diagram.Name}");
            progress.Info($"Tables: {filtered.Tables.Count}");
            progress.Info($"Foreign keys: {filtered.ForeignKeys.Count}");
            if (filtered.Tables.Count == 0)
            {
                progress.Warning($"diagram '{diagram.Name}' has no tables after filtering.");
            }

            var renderer = rendererFactory.Create(diagram.DiagramRenderer);
            EmitCapabilityWarnings(renderer, diagram, progress);

            if (options.DryRun)
            {
                progress.Info("Dry run: output was not written.");
                results.Add(new GenerateResult(diagram.OutputPath, WroteOutput: false));
                continue;
            }

            var diagramText = renderer.Render(filtered, diagram.Diagram);
            var output = diagram.Output.Format == OutputContainerFormat.Markdown
                ? MarkdownDiagramWrapper.Wrap(diagramText, diagram.Output.Markdown ?? throw new InvalidOperationException("Markdown output options are required."))
                : diagramText;

            var fullPath = Path.GetFullPath(diagram.OutputPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            progress.Info($"Writing: {diagram.OutputPath}");
            await File.WriteAllTextAsync(diagram.OutputPath, output, cancellationToken);
            results.Add(new GenerateResult(diagram.OutputPath, WroteOutput: true));
        }

        if (!options.DryRun)
        {
            progress.Info("Done.");
        }

        return results;
    }

    private static void EmitCapabilityWarnings(IDiagramRenderer renderer, ResolvedDiagramTarget diagram, ProgressReporter progress)
    {
        if (!renderer.Capabilities.SupportsColumnToColumnRelationships)
        {
            progress.Warning("Mermaid ER renders relationships between entities, not between specific column ports. Use DOT for column-to-column edges.");
            if (diagram.Diagram.Show.TableComments)
            {
                progress.Warning("Mermaid ER does not support table comments. Table comments will not be emitted.");
            }
        }

        if (!renderer.Capabilities.SupportsCustomTableLayouts &&
            (diagram.Diagram.Layout.ColumnLayout is not null || diagram.Diagram.Layout.TableHeaderLayout is not null))
        {
            progress.Warning("Mermaid ER does not support custom columnLayout/tableHeaderLayout. Layout settings will be ignored. Use DOT for custom table cell layouts.");
        }
    }
}
