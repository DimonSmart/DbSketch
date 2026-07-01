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
    public async Task<GenerateResult> GenerateAsync(ResolvedGenerateOptions options, CancellationToken cancellationToken)
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
        var filtered = schemaFilter.Apply(commented, options.Filter);
        progress.Verbose($"Included tables: {string.Join(", ", options.Filter.IncludeTables)}");
        progress.Verbose($"Excluded tables: {string.Join(", ", options.Filter.ExcludeTables)}");
        progress.Info($"Tables: {filtered.Tables.Count}");
        progress.Info($"Foreign keys: {filtered.ForeignKeys.Count}");
        if (filtered.Tables.Count == 0)
        {
            progress.Warning("no tables found after filtering.");
        }

        var renderer = rendererFactory.Create(options.DiagramRenderer);
        EmitCapabilityWarnings(renderer, options, progress);

        if (options.DryRun)
        {
            progress.Info("Dry run: output was not written.");
            return new GenerateResult(options.OutputPath, WroteOutput: false);
        }

        var diagramText = renderer.Render(filtered, options.Diagram);
        var output = options.Output.Format == OutputContainerFormat.Markdown
            ? MarkdownDiagramWrapper.Wrap(diagramText, options.Output.Markdown ?? throw new InvalidOperationException("Markdown output options are required."))
            : diagramText;

        if (options.OutputPath == "-")
        {
            await console.Out.WriteAsync(output.AsMemory(), cancellationToken);
            return new GenerateResult(options.OutputPath, WroteOutput: true);
        }

        var fullPath = Path.GetFullPath(options.OutputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        progress.Info($"Writing: {options.OutputPath}");
        await File.WriteAllTextAsync(options.OutputPath, output, cancellationToken);
        progress.Info("Done.");
        return new GenerateResult(options.OutputPath, WroteOutput: true);
    }

    private static void EmitCapabilityWarnings(IDiagramRenderer renderer, ResolvedGenerateOptions options, ProgressReporter progress)
    {
        if (renderer.Capabilities.SupportsColumnToColumnRelationships)
        {
            return;
        }

        progress.Warning("Mermaid ER renders relationships between entities, not between specific column ports. Use DOT for column-to-column edges.");
        if (options.Diagram.Show.TableComments)
        {
            progress.Warning("Mermaid ER does not support table comments. Table comments will not be emitted.");
        }
    }
}
