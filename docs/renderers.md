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

## DOT styling

Diagrams use `classic` style by default. For DOT output, set `style: readable` for a more spacious Graphviz preset:

```yaml
defaults:
  diagram:
    renderer: dot
    style: readable
    columnLayout: "{name:bold} {type:color=#666666}\n{comment:color=#666666,fontSize=9} | {keys}"
    tableHeaderLayout: "{table:bold}\n{comment:color=#666666,fontSize=9}"
```

Supported styles:

- `classic`: legacy-looking output and the default.
- `readable`: neutral sans-serif diagram with calmer edges and padded tables.
- `compact`: denser tables and shorter spacing for large schemas.
- `soft`: light green-tinted headers and softer relationships.
- `blueprint`: blue technical drawing style.
- `contrast`: stronger borders, darker edges, and larger text.

All non-classic styles enable sans-serif fonts, calmer edges, table padding, colored borders, header background color, and left-balanced multiline cells.

Style presets can be overridden with low-level Graphviz options:

```yaml
defaults:
  diagram:
    renderer: dot
    style: readable
    dot:
      graph:
        fontName: Helvetica
        fontSize: 16
        nodesep: 0.55
        ranksep: 0.9
        backgroundColor: "#FFFFFF"
      node:
        fontName: Helvetica
        fontSize: 10
      edge:
        fontName: Helvetica
        fontSize: 9
        color: "#555555"
        penWidth: 1.1
        arrowSize: 0.7
      table:
        borderColor: "#777777"
        headerBackground: "#F1F3F5"
        cellPadding: 4
```

For a visual comparison of the built-in styles, see [DOT style presets](examples/dot-styles.md).

## Mermaid ER

The Mermaid ER renderer is convenient for GitHub Markdown.

- Renders relationships between entities, not exact column ports.
- Can show or hide foreign key names on relationships with `diagram.show.foreignKeyLabels`.
- Can show or hide self-referencing relationships with `diagram.show.selfReferencingForeignKeys`.
- Supports `columnLayout` as a logical projection for type, name, PK/FK markers, nullable markers, and comments.
- Ignores style modifiers, multiline cell structure, and `tableHeaderLayout`.
- Does not emit table comments because Mermaid ER has no natural table comment syntax.

When `columnLayout` contains `{nullable}`, Mermaid ER renders nullable columns as an attribute comment, for example `nvarchar_100 Name "NULL"`. Not-null columns are left unmarked by the default layout. Use `{nullability}` instead to mark every column with either `"NULL"` or `"NOT NULL"`:

```yaml
defaults:
  diagram:
    renderer: mermaid
    columnLayout: "{name} | {type} | {keys} | {nullability}"
```

For Mermaid ER diagrams, DbSketch does not emit `direction LR` by default. Some Markdown renderers display `direction` and `LR` as separate entities. Set `diagram.mermaid.emitDirection: true` only when your Mermaid renderer correctly supports `direction` inside `erDiagram`.

## Markdown Output

When `output.format: markdown`, DbSketch wraps the generated diagram text in a fenced Markdown block.

Use `output.markdown.header` or `defaults.output.markdown.header` to replace the default header.
Use `output.markdown.footer` to append content after the diagram.
Use `output.markdown.header: ""` to generate a Markdown file without a header.

If `output.markdown.fenceLanguage` is omitted, DbSketch uses `mermaid` for the Mermaid renderer and `dot` for the DOT renderer.

Set `output.markdown.showIndexes: true` (or the corresponding value under `defaults`) to add a deterministic `## Indexes` table after the diagram and before the footer. The table includes user indexes, key-column order and direction, included columns, predicates, and comments where the provider exposes them. Primary-key backing indexes are omitted. Raw output is unchanged.

```yaml
defaults:
  output:
    format: markdown
    markdown:
      showIndexes: true
  diagram:
    renderer: dot
    columnLayout: "{name} | {type} | {keys} | {idx}"
```

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

Install Graphviz, then render PNG or SVG from the generated DOT file:

```bash
dot -Tpng docs/db/schema.dot -o docs/db/schema.png
dot -Tsvg docs/db/schema.dot -o docs/db/schema.svg
```
