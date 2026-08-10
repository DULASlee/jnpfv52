using JNPF.Common.Const;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization tests for shared import system-auto field mapping (W3 edge).
/// </summary>
public class ImportSystemFieldAssemblerTests
{
    private static ImportSystemFieldContext Ctx(
        string userId = "u1",
        string? orgId = "org1",
        DateTime? now = null)
        => new(userId, orgId, now ?? new DateTime(2026, 8, 7, 12, 30, 0));

    [Theory]
    [InlineData(JnpfKeyConst.BILLRULE, true)]
    [InlineData(JnpfKeyConst.MODIFYUSER, true)]
    [InlineData(JnpfKeyConst.CREATEUSER, true)]
    [InlineData(JnpfKeyConst.MODIFYTIME, true)]
    [InlineData(JnpfKeyConst.CREATETIME, true)]
    [InlineData(JnpfKeyConst.CURRPOSITION, true)]
    [InlineData(JnpfKeyConst.CURRORGANIZE, true)]
    [InlineData(JnpfKeyConst.SELECT, false)]
    public void IsSystemAutoKey_MatchesKnownKeys(string key, bool expected)
        => Assert.Equal(expected, ImportSystemFieldAssembler.IsSystemAutoKey(key));

    [Fact]
    public void TryMapStatic_CreateUserAndTimes()
    {
        var row = new Dictionary<string, object>();
        var ctx = Ctx();
        Assert.True(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.CREATEUSER, "f_cu", row, ctx));
        Assert.True(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.CREATETIME, "f_ct", row, ctx));
        Assert.True(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.MODIFYUSER, "f_mu", row, ctx));
        Assert.True(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.MODIFYTIME, "f_mt", row, ctx));
        Assert.Equal("u1", row["f_cu"]);
        Assert.Equal("2026-08-07 12:30:00", row["f_ct"]);
        Assert.Equal(string.Empty, row["f_mu"]);
        Assert.Equal(string.Empty, row["f_mt"]);
    }

    [Fact]
    public void TryMapStatic_CurrOrganize_NullBecomesEmpty()
    {
        var row = new Dictionary<string, object>();
        Assert.True(ImportSystemFieldAssembler.TryMapStatic(
            JnpfKeyConst.CURRORGANIZE, "f_org", row, Ctx(orgId: null)));
        Assert.Equal(string.Empty, row["f_org"]);

        Assert.True(ImportSystemFieldAssembler.TryMapStatic(
            JnpfKeyConst.CURRORGANIZE, "f_org2", row, Ctx(orgId: "o9")));
        Assert.Equal("o9", row["f_org2"]);
    }

    [Fact]
    public void TryMapStatic_ReturnsFalseForAsyncKeys()
    {
        var row = new Dictionary<string, object>();
        Assert.False(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.BILLRULE, "f_b", row, Ctx()));
        Assert.False(ImportSystemFieldAssembler.TryMapStatic(JnpfKeyConst.CURRPOSITION, "f_p", row, Ctx()));
        Assert.Empty(row);
    }

    [Fact]
    public void MapBillRule_MissingRuleClearsValue()
    {
        var row = new Dictionary<string, object>();
        ImportSystemFieldAssembler.MapBillRule("f_bill", "BN-001", row);
        Assert.Equal("BN-001", row["f_bill"]);

        ImportSystemFieldAssembler.MapBillRule(
            "f_bill2", ImportSystemFieldAssembler.MissingBillRuleMessage, row);
        Assert.Equal(string.Empty, row["f_bill2"]);
    }

    [Fact]
    public void MapCurrPosition_EmptyWhenMissing()
    {
        var row = new Dictionary<string, object>();
        ImportSystemFieldAssembler.MapCurrPosition("f_pos", "p1", row);
        Assert.Equal("p1", row["f_pos"]);
        ImportSystemFieldAssembler.MapCurrPosition("f_pos2", null, row);
        Assert.Equal(string.Empty, row["f_pos2"]);
        ImportSystemFieldAssembler.MapCurrPosition("f_pos3", string.Empty, row);
        Assert.Equal(string.Empty, row["f_pos3"]);
        ImportSystemFieldAssembler.MapCurrPosition("f_pos4", "   ", row);
        Assert.Equal(string.Empty, row["f_pos4"]);
    }
}
