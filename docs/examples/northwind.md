# Northwind DbSketch Example

This example shows a small Northwind-like PostgreSQL schema with customers, employees, categories, products, orders, and order details.

## Compact DOT layout

```yaml
tableHeaderLayout: "{fullName}"
columnLayout: "{name}"
show:
  foreignKeyLabels: false
```

![Compact DbSketch generated Northwind database schema](../assets/northwind-schema-compact.png)

## Full DOT layout

```yaml
tableHeaderLayout: "{fullName} | {comment}"
columnLayout: "{name} | {type} | {comment} | {keys}"
show:
  foreignKeyLabels: true
  tableComments: true
  columnComments: true
```

![Full DbSketch generated Northwind database schema](../assets/northwind-schema-full.png)

The sample is generated from the test fixture at:

```text
tests/DbSketch.Tests/TestData/Northwind/postgres-northwind-schema.sql
```

Example files:

- [Northwind DbSketch config](northwind.dbsketch.yml)
- [Generated compact DOT](northwind.compact.dot)
- [Generated full DOT](northwind.full.dot)
- [Generated DOT](northwind.dot)
- [Generated README DOT](northwind.readme.dot)
- [Generated Mermaid ER](northwind.mmd)
- [Generated Markdown](northwind.generated.md)

The PNG image is generated from DbSketch DOT output with Graphviz.
