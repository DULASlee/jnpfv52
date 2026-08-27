using JNPF.Common.Security;
using Xunit;

namespace JNPF.Tests.Common;

public class SqlGuardTests
{
    [Theory]
    [InlineData("users")]
    [InlineData("BASE_USER")]
    [InlineData("f_delete_mark")]
    [InlineData("_private_table")]
    [InlineData("table123")]
    public void ValidateIdentifier_ValidIdentifier_DoesNotThrow(string identifier)
    {
        var ex = Record.Exception(() => SqlGuard.ValidateIdentifier(identifier, "test"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("users; DROP TABLE secrets--")]
    [InlineData("t' OR '1'='1")]
    [InlineData("1 UNION SELECT * FROM passwords--")]
    [InlineData("admin'/*")]
    [InlineData("table name")]
    [InlineData("table-name")]
    [InlineData("table.name")]
    [InlineData("123start")]
    [InlineData("")]
    public void ValidateIdentifier_MaliciousIdentifier_Throws(string identifier)
    {
        var ex = Record.Exception(() => SqlGuard.ValidateIdentifier(identifier, "表名"));
        Assert.NotNull(ex);
    }

    [Fact]
    public void ValidateIdentifier_Null_Throws()
    {
        var ex = Record.Exception(() => SqlGuard.ValidateIdentifier(null!, "表名"));
        Assert.NotNull(ex);
    }
}
