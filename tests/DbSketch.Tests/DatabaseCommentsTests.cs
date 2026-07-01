using DimonSmart.DbSketch.Core.Schema;

namespace DimonSmart.DbSketch.Tests;

public sealed class DatabaseCommentsTests
{
    [Fact]
    public void GetTableComment_ReturnsNullWhenNoCommentExists()
    {
        var comments = new DatabaseComments([], []);

        Assert.Null(comments.GetTableComment("dbo", "Users"));
    }

    [Fact]
    public void GetTableComment_ReturnsCommentByCaseInsensitiveKey()
    {
        var comments = new DatabaseComments([("dbo", "Users", "Application users")], []);

        Assert.Equal("Application users", comments.GetTableComment("DBO", "users"));
    }

    [Fact]
    public void GetColumnComment_ReturnsCommentByCaseInsensitiveKey()
    {
        var comments = new DatabaseComments([], [("dbo", "Users", "Id", "User identifier")]);

        Assert.Equal("User identifier", comments.GetColumnComment("DBO", "users", "ID"));
    }

    [Fact]
    public void GetTableComment_ReturnsTrimmedComment()
    {
        var comments = new DatabaseComments([("dbo", "Users", " User table ")], []);

        Assert.Equal("User table", comments.GetTableComment("dbo", "Users"));
    }

    [Fact]
    public void GetColumnComment_TreatsEmptyAndWhitespaceCommentsAsNull()
    {
        var comments = new DatabaseComments(
            [("dbo", "Users", "")],
            [("dbo", "Users", "Id", "  ")]);

        Assert.Null(comments.GetTableComment("dbo", "Users"));
        Assert.Null(comments.GetColumnComment("dbo", "Users", "Id"));
    }
}
