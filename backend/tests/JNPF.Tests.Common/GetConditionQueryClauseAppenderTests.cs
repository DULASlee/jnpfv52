using JNPF.Common.Core.Manager.User.Conditions;
using JNPF.Common.Enums;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// Characterization: GetCondition QueryType → clause append (W1 deep-split).
/// </summary>
public class GetConditionQueryClauseAppenderTests
{
    [Fact]
    public void Equal_AndLogic_UsesAndKey()
    {
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.Equal, "f_name", "alice", null, "varchar", "and");
        Assert.False(cont);
        Assert.Single(list);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains("alice", json);
        Assert.Contains(((int)WhereType.And).ToString(), json);
        Assert.Contains(((int)ConditionalType.Equal).ToString(), json);
    }

    [Fact]
    public void Between_ReturnsContinue_AndTwoBounds()
    {
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.Between, "f_date", null, new List<string> { "a", "b" }, "datetime", "or");
        Assert.True(cont);
        Assert.Equal(2, list.Count);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains("\"a\"", json);
        Assert.Contains("\"b\"", json);
    }

    [Fact]
    public void Null_NumericType_UsesEqualNull()
    {
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Null, "f_n", null, null, "int", "and");
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains(((int)ConditionalType.EqualNull).ToString(), json);
    }

    [Fact]
    public void Null_TextType_UsesIsNullOrEmpty()
    {
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Null, "f_t", null, null, "varchar", "and");
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains(((int)ConditionalType.IsNullOrEmpty).ToString(), json);
    }

    [Fact]
    public void In_JsonArray_ExpandsLikeOrChain_AndContinues()
    {
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "[\"o1\",\"o2\"]", null, "varchar", "and");
        Assert.True(cont);
        Assert.Equal(2, list.Count);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains("o1", json);
        Assert.Contains("o2", json);
        Assert.Contains(((int)ConditionalType.Like).ToString(), json);
    }

    [Fact]
    public void NotIn_JsonArray_AddsNullAndEmptyGuards()
    {
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.NotIn, "f_org", "[\"o1\"]", null, "varchar", "and");
        Assert.True(cont);
        Assert.True(list.Count >= 3);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains("null", json);
        Assert.Contains(((int)ConditionalType.NoLike).ToString(), json);
    }

    [Fact]
    public void AllowAll_PrimaryKeyAsInt_ConvertFuncReturnsInt()
    {
        var models = DataPermissionShortCircuits.AllowAll("f_id", primaryKeyAsInt: true);
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        Assert.Equal(ConditionalType.NoEqual, cm.ConditionalType);
        Assert.NotNull(cm.FieldValueConvertFunc);
        var converted = cm.FieldValueConvertFunc!("0");
        Assert.IsType<int>(converted);
        Assert.Equal(0, converted);
    }

    [Fact]
    public void DenyAll_PrimaryKeyAsInt_EqualZero()
    {
        var models = DataPermissionShortCircuits.DenyAll("f_id", primaryKeyAsInt: true);
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        Assert.Equal(ConditionalType.Equal, cm.ConditionalType);
        Assert.Equal(0, cm.FieldValueConvertFunc!("0"));
    }

    [Fact]
    public void AllowAll_Default_ConvertFuncReturnsString()
    {
        var models = DataPermissionShortCircuits.AllowAll("f_id");
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        var converted = cm.FieldValueConvertFunc!("0");
        Assert.IsType<string>(converted);
        Assert.Equal("0", converted);
    }

    [Fact]
    public void In_NonJson_FallsBackToEqual_NoContinue()
    {
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "plain", null, "varchar", "and");
        Assert.False(cont);
        Assert.Single(list);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        Assert.Contains(((int)ConditionalType.Equal).ToString(), json);
        Assert.Contains("plain", json);
    }
}
