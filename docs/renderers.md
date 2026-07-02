# Renderers

DbSketch separates the diagram renderer from the output container. A renderer creates DOT or Mermaid text; an output container writes it directly or wraps it in Markdown.

## DOT

The DOT renderer is the best choice for precise technical diagrams.

- Preserves column-to-column foreign key edges.
- Supports table and column comments.
- Works well as source for PNG generation through Graphviz.

GitHub does not render DOT code fences as diagrams. Commit a generated PNG when a diagram needs to be visible on a README or package page.

## Mermaid ER

The Mermaid ER renderer is convenient for GitHub Markdown.

- Renders relationships between entities, not exact column ports.
- Supports column comments when enabled.
- Does not emit table comments because Mermaid ER has no natural table comment syntax.

For Mermaid ER diagrams, DbSketch does not emit `direction LR` by default. Some Markdown renderers display `direction` and `LR` as separate entities. Set `diagram.mermaid.emitDirection: true` only when your Mermaid renderer correctly supports `direction` inside `erDiagram`.

## Markdown Output

When `output.format: markdown`, DbSketch wraps the generated diagram text in a fenced Markdown block.

Use `output.markdown.header` or `defaults.output.markdown.header` to replace the default header.
Use `output.markdown.footer` to append content after the diagram.
Use `output.markdown.header: ""` to generate a Markdown file without a header.

If `output.markdown.fenceLanguage` is omitted, DbSketch uses `mermaid` for the Mermaid renderer and `dot` for the DOT renderer.

For README and NuGet package pages, use a committed PNG image for the main illustration. Mermaid is useful in GitHub Markdown, but NuGet package README does not render Mermaid diagrams.
