# Database Comments

DbSketch can read database-native table and column comments and include them in supported renderers.

Enable comment reading:

```yaml
comments:
  enabled: true
```

Provider support:

- SQL Server: `MS_Description` extended properties.
- PostgreSQL: `COMMENT ON TABLE` and `COMMENT ON COLUMN`.
- MySQL: `TABLE_COMMENT` and `COLUMN_COMMENT` from `information_schema`.

## Overrides

YAML overrides can replace or add table and column comments. Overrides are applied even when database comment reading is disabled.

```yaml
comments:
  enabled: true
  overrides:
    tables:
      - schema: dbo
        name: Users
        comment: Application users
        columns:
          Id: Internal user identifier
          Email: Login email
```

## Rendering

```yaml
defaults:
  diagram:
    show:
      tableComments: true
      columnComments: true
    comments:
      maxLength: 80
```

DOT supports table and column comments.
Mermaid ER supports column comments.
Mermaid ER does not emit table comments because Mermaid ER has no natural table comment syntax.

`diagram.comments.maxLength` limits rendered comments after inline whitespace normalization. It is optional; by default comments are not truncated.
