using DimonSmart.DbSketch.Core.Model;
using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Tests;

public sealed class ForeignKeyModelBuilderTests
{
    [Fact]
    public void CompositeForeignKeyIsGroupedIntoOneModel()
    {
        var foreignKeys = ForeignKeyModelBuilder.Build(
        [
            Row("public", "FK_Order_User", "public", "Orders", "TenantId", "public", "Users", "TenantId", 1),
            Row("public", "FK_Order_User", "public", "Orders", "UserId", "public", "Users", "Id", 2)
        ]);

        var foreignKey = Assert.Single(foreignKeys);
        Assert.Equal(["TenantId", "UserId"], foreignKey.SourceColumns);
        Assert.Equal(["TenantId", "Id"], foreignKey.TargetColumns);
    }

    [Fact]
    public void SameNameInDifferentTablesStaysSeparate()
    {
        var foreignKeys = ForeignKeyModelBuilder.Build(
        [
            Row("dbo", "FK_User", "dbo", "Orders", "UserId", "dbo", "Users", "Id", 1),
            Row("dbo", "FK_User", "dbo", "Invoices", "UserId", "dbo", "Users", "Id", 1)
        ]);

        Assert.Equal(2, foreignKeys.Count);
    }

    [Fact]
    public void SameNameInDifferentSchemasStaysSeparate()
    {
        var foreignKeys = ForeignKeyModelBuilder.Build(
        [
            Row("app", "FK_User", "app", "Orders", "UserId", "app", "Users", "Id", 1),
            Row("audit", "FK_User", "audit", "Orders", "UserId", "audit", "Users", "Id", 1)
        ]);

        Assert.Equal(2, foreignKeys.Count);
    }

    [Fact]
    public void ColumnOrderIsPreservedByOrdinal()
    {
        var foreignKey = Assert.Single(ForeignKeyModelBuilder.Build(
        [
            Row("public", "FK_Order_User", "public", "Orders", "UserId", "public", "Users", "Id", 2),
            Row("public", "FK_Order_User", "public", "Orders", "TenantId", "public", "Users", "TenantId", 1)
        ]));

        Assert.Equal(["TenantId", "UserId"], foreignKey.SourceColumns);
    }

    private static ForeignKeyColumnRow Row(
        string constraintSchema,
        string name,
        string sourceSchema,
        string sourceTable,
        string sourceColumn,
        string targetSchema,
        string targetTable,
        string targetColumn,
        int ordinal) =>
        new(
            constraintSchema,
            name,
            new TableRef(sourceSchema, sourceTable),
            sourceColumn,
            new TableRef(targetSchema, targetTable),
            targetColumn,
            ordinal);
}
