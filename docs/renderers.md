# Renderers

DbSketch separates the diagram renderer from the output container. A renderer creates DOT or Mermaid text; an output container writes it directly or wraps it in Markdown.

## DOT

The DOT renderer is the best choice for precise technical diagrams.

- Preserves column-to-column foreign key edges.
- Can show or hide foreign key names on edges with `diagram.show.foreignKeyLabels`.
- Can show or hide self-referencing foreign key edges with `diagram.show.selfReferencingForeignKeys`.
- Supports full `columnLayout` table cells, including styled tokens and multiline cells.
- Supports `tableHeaderLayout`.
- Supports table comments through the default header or `tableHeaderLayout`, and column comments through `{comment}` in `columnLayout`.
- Supports `diagram.style` presets: `classic`, `readable`, `compact`, `soft`, `blueprint`, and `contrast`.
- Works well as source for PNG or SVG generation through Graphviz.

GitHub does not render DOT code fences as diagrams. Commit a generated PNG when a diagram needs to be visible on a README or package page.

## Mermaid ER

The Mermaid ER renderer is convenient for GitHub Markdown.

- Renders relationships between entities, not exact column ports.
- Can show or hide foreign key names on relationships with `diagram.show.foreignKeyLabels`.
- Can show or hide self-referencing relationships with `diagram.show.selfReferencingForeignKeys`.
- Supports `columnLayout` as a logical projection for type, name, PK/FK markers, and comments.
- Ignores style modifiers, multiline cell structure, and `tableHeaderLayout`.
- Does not emit table comments because Mermaid ER has no natural table comment syntax.

For Mermaid ER diagrams, DbSketch does not emit `direction LR` by default. Some Markdown renderers display `direction` and `LR` as separate entities. Set `diagram.mermaid.emitDirection: true` only when your Mermaid renderer correctly supports `direction` inside `erDiagram`.

## Markdown Output

When `output.format: markdown`, DbSketch wraps the generated diagram text in a fenced Markdown block.

Use `output.markdown.header` or `defaults.output.markdown.header` to replace the default header.
Use `output.markdown.footer` to append content after the diagram.
Use `output.markdown.header: ""` to generate a Markdown file without a header.

If `output.markdown.fenceLanguage` is omitted, DbSketch uses `mermaid` for the Mermaid renderer and `dot` for the DOT renderer.

For README and NuGet package pages, use a committed PNG image for the main illustration. Mermaid is useful in GitHub Markdown, but NuGet package README does not render Mermaid diagrams.

## PNG or SVG rendering

DbSketch does not require image generation. Use the generated text output directly when your documentation platform can render DOT, Mermaid, or Markdown-like diagram formats.

Static images are useful for places that cannot render diagram source directly, such as package pages, PDFs, presentations, or documentation sites without Mermaid or Graphviz support.

To render DOT output as an image, first generate a raw `.dot` file:

```yaml
diagrams:
  - name: main-dot
    title: Database schema
    diagram:
      renderer: dot
    output:
      format: raw
      path: docs/db/schema.dot
```

Install Graphviz:

```bash
# Windows
winget install graphviz

# macOS
brew install graphviz

# Ubuntu / Debian
sudo apt install graphviz
```

Then render PNG or SVG from the generated DOT file:

```bash
dot -Tpng docs/db/schema.dot -o docs/db/schema.png
dot -Tsvg docs/db/schema.dot -o docs/db/schema.svg
```
