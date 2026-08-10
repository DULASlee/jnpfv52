using JNPF.Common.Security;
using JNPF.VisualDev.Query;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: RunService.GetSuperQueryInput rewrite (VisualDev list path).
/// Do NOT merge with SuperQueryHelper (typed CodeGen path).
/// </summary>
public class ListSuperQueryInputRewriterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rewrite_EmptyInput_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, ListSuperQueryInputRewriter.Rewrite(input));
    }

    [Fact]
    public void Rewrite_TextEqual_MapsConditionalTypeAndWhere()
    {
        // Key order matters: FirstOrDefault=matchLogic, LastOrDefault=conditionList
        var json = """
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "input",
                      "field": "f_name",
                      "symbol": "==",
                      "fieldValue": "alice"
                    }
                  ]
                }
              ]
            }
            """;

        var result = ListSuperQueryInputRewriter.Rewrite(json);
        var list = result.ToObject<List<Dictionary<string, object>>>();
        Assert.Single(list);
        Assert.Equal(WhereType.And, (WhereType)Convert.ToInt32(list[0]["whereType"]));
        Assert.Equal(WhereType.And, (WhereType)Convert.ToInt32(list[0]["where"]));
        Assert.Equal(ConditionalType.Equal, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal("f_name", list[0]["field"]?.ToString());
        Assert.Equal("alice", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void Rewrite_LikeWithBrackets_StripsBrackets()
    {
        var json = """
            {
              "matchLogic": "OR",
              "conditionList": [
                {
                  "logic": "OR",
                  "groups": [
                    {
                      "jnpfKey": "input",
                      "field": "f_tag",
                      "symbol": "like",
                      "fieldValue": "[hello]"
                    }
                  ]
                }
              ]
            }
            """;

        var list = ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();
        Assert.Single(list);
        Assert.Equal(ConditionalType.Like, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal("hello", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void Rewrite_DateBetween_EmitsTwoBoundsWithDatetimeType()
    {
        // 2020-01-01 / 2020-01-02 UTC ms — local ToString after TimeStampToDateTime
        var startMs = "1577836800000";
        var endMs = "1577923200000";
        var json = $$"""
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "datePicker",
                      "field": "f_date",
                      "symbol": "between",
                      "fieldValue": ["{{startMs}}", "{{endMs}}"]
                    }
                  ]
                }
              ]
            }
            """;

        var list = ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(2, list.Count);
        Assert.Equal(ConditionalType.GreaterThanOrEqual, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal(ConditionalType.LessThanOrEqual, (ConditionalType)Convert.ToInt32(list[1]["ConditionalType"]));
        Assert.Equal("datetime", list[0]["CSharpTypeName"]?.ToString());
        Assert.Equal("datetime", list[1]["CSharpTypeName"]?.ToString());
        Assert.False(string.IsNullOrEmpty(list[0]["fieldValue"]?.ToString()));
        Assert.False(string.IsNullOrEmpty(list[1]["fieldValue"]?.ToString()));
    }

    [Fact]
    public void Rewrite_NullSymbolNumInput_UsesEqualNull()
    {
        // symbol "null" (not "=="): live EqualNull path for numeric keys.
        // Note: "==" + missing fieldValue is NOT EqualNull — else-branch inserts null
        // then legacy `ContainsKey(...).Equals("[]")` never fires.
        var json = """
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "inputNumber",
                      "field": "f_qty",
                      "symbol": "null"
                    }
                  ]
                }
              ]
            }
            """;

        var list = ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();
        Assert.Single(list);
        Assert.Equal(ConditionalType.EqualNull, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void Rewrite_EmptyConditionList_ReturnsEmptyArrayJson()
    {
        var json = """{"matchLogic":"AND","conditionList":[]}""";
        Assert.Equal("[]", ListSuperQueryInputRewriter.Rewrite(json));
    }

    [Fact]
    public void Rewrite_ComSelectIn_AppendsQuoteBracketSuffix()
    {
        var json = """
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "organizeSelect",
                      "field": "f_org",
                      "symbol": "in",
                      "fieldValue": "[\"org1\",\"org2\"]"
                    }
                  ]
                }
              ]
            }
            """;

        var list = ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(2, list.Count);
        Assert.Equal("org1\"]", list[0]["fieldValue"]?.ToString());
        Assert.Equal("org2\"]", list[1]["fieldValue"]?.ToString());
        Assert.Equal(ConditionalType.Like, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void Rewrite_NotInWithArray_AddsNullAndEmptyIsNotGuards()
    {
        var json = """
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "select",
                      "field": "f_status",
                      "symbol": "notIn",
                      "fieldValue": "[\"a\",\"b\"]"
                    }
                  ]
                }
              ]
            }
            """;

        var list = ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();
        // 2 values + null IsNot + empty IsNot
        Assert.Equal(4, list.Count);
        Assert.Contains(list, q =>
            q["ConditionalType"] != null
            && (ConditionalType)Convert.ToInt32(q["ConditionalType"]) == ConditionalType.IsNot
            && q["fieldValue"] == null);
        Assert.Contains(list, q =>
            q["ConditionalType"] != null
            && (ConditionalType)Convert.ToInt32(q["ConditionalType"]) == ConditionalType.IsNot
            && string.Empty.Equals(q["fieldValue"]?.ToString()));
    }
}
