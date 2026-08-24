using System.Reflection;
using JNPF.Engine.Entity.Model;
using JNPF.Systems.Entitys.Permission;
using JNPF.VisualDev.Engine.Import;
using JNPF.VisualDev.Query;
using JNPF.VisualDev.Transfer;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// 战役 D1 签名契约（规格验收⑤）：拆分重构期间 5 个公开静态入口签名逐字不变，调用方零改动.
/// 每方法销账时追加对应断言；全部完成后本文件为长期守护（防未来误改签名）.
/// </summary>
public class D1SignatureContractTests
{
    [Fact]
    public void Rewrite_Signature_Unchanged()
    {
        var m = typeof(ListSuperQueryInputRewriter).GetMethod(
            "Rewrite", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(m);
        Assert.Equal(typeof(string), m!.ReturnType);
        var p = Assert.Single(m.GetParameters());
        Assert.Equal(typeof(string), p.ParameterType);
        Assert.Equal("superQueryJson", p.Name);
    }

    [Fact]
    public void Bind_Signature_Unchanged()
    {
        var m = typeof(FieldBindDefaultValueHelpers).GetMethod(
            "Bind", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(m);
        Assert.Equal(typeof(void), m!.ReturnType);
        var types = m.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(new[]
        {
            typeof(List<Dictionary<string, object>>).MakeByRefType(),
            typeof(string), typeof(string),
            typeof(List<string>), typeof(List<string>), typeof(List<string>),
            typeof(List<UserRelationEntity>), typeof(string),
        }, types);
    }

    [Fact]
    public void ApplyMapRules_Signature_Unchanged()
    {
        var m = typeof(FlowFormDataMapper).GetMethod(
            "ApplyMapRules", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(m);
        Assert.Equal(typeof(void), m!.ReturnType);
        var types = m.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(new[]
        {
            typeof(Dictionary<string, object>),
            typeof(List<Dictionary<string, string>>),
            typeof(IReadOnlyDictionary<string, FieldsModel>),
            typeof(IReadOnlyDictionary<string, FieldsModel>),
            typeof(string),
        }, types);
    }

    [Fact]
    public void ValidateBatchUnique_Signature_Unchanged()
    {
        var m = typeof(ImportFirstVerifyHelpers).GetMethod(
            "ValidateBatchUnique", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(m);
        Assert.Equal(typeof(void), m!.ReturnType);
        var types = m.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(new[]
        {
            typeof(List<Dictionary<string, object>>),
            typeof(List<FieldsModel>),
            typeof(List<string>),
            typeof(string),
        }, types);
    }
}
