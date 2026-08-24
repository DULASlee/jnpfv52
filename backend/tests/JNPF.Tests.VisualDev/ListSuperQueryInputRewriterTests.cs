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

    // ==================== D1.1 S1 特征用例（规格 §2.1 不变量 I1-I10 缺口补齐） ====================
    // 金标准纪律：本批用例在当前实现上必须全绿后才允许拆分；拆分后逐条等价。
    // I8（Q1 恒 false 比较）为死分支（fieldValue 键恒已写入），由本注释+ Rewrite_NullSymbolNumInput 用例注记锁定，不新增无效用例。

    private static string Wrap(string matchLogic, string logic, string itemJson) => $$"""
        {
          "matchLogic": "{{matchLogic}}",
          "conditionList": [
            { "logic": "{{logic}}", "groups": [ {{itemJson}} ] }
          ]
        }
        """;

    private static List<Dictionary<string, object>> Run(string json)
        => ListSuperQueryInputRewriter.Rewrite(json).ToObject<List<Dictionary<string, object>>>();

    [Fact]
    public void D1_I3_SimpleComparisonOperators_MapOneToOne()
    {
        // I1+简单符号直映：>= > <= < <> 各一条款（两组对照首条款带 where、次条款不带）
        var json = Wrap("OR", "AND", """
            {"jnpfKey":"input","field":"f_a","symbol":">=","fieldValue":"1"},
            {"jnpfKey":"input","field":"f_b","symbol":">","fieldValue":"2"},
            {"jnpfKey":"input","field":"f_c","symbol":"<=","fieldValue":"3"},
            {"jnpfKey":"input","field":"f_d","symbol":"<","fieldValue":"4"},
            {"jnpfKey":"input","field":"f_e","symbol":"<>","fieldValue":"5"}
            """);

        var list = Run(json);
        Assert.Equal(5, list.Count);
        Assert.Equal(ConditionalType.GreaterThanOrEqual, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal(ConditionalType.GreaterThan, (ConditionalType)Convert.ToInt32(list[1]["ConditionalType"]));
        Assert.Equal(ConditionalType.LessThanOrEqual, (ConditionalType)Convert.ToInt32(list[2]["ConditionalType"]));
        Assert.Equal(ConditionalType.LessThan, (ConditionalType)Convert.ToInt32(list[3]["ConditionalType"]));
        Assert.Equal(ConditionalType.NoEqual, (ConditionalType)Convert.ToInt32(list[4]["ConditionalType"]));
        // I2：where 键仅首条款；其余仅 whereType（组内逻辑 AND）
        Assert.True(list[0].ContainsKey("where"));
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[0]["where"]));
        Assert.False(list[1].ContainsKey("where"));
        Assert.Equal(WhereType.And, (WhereType)Convert.ToInt32(list[1]["whereType"]));
    }

    [Fact]
    public void D1_I5_EmptyFieldValue_EqualSymbol_EmitsEqual()
    {
        // I5 实测形态：缺省 fieldValue 的 NUMINPUT 被置 null 后，"==" 短路分支不可达（ContainsKey 恒真），实际走 Equal —— 锁定该行为防拆分误判为 EqualNull/IsNullOrEmpty
        var json = Wrap("AND", "AND", """{"jnpfKey":"inputNumber","field":"f_qty","symbol":"=="}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.Equal, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Null(list[0]["fieldValue"]);
    }

    [Fact]
    public void D1_I5_EmptyFieldValue_NotEqualSymbol_EmitsIsNot()
    {
        // I5："<>" + 空值短路 → IsNot（数值控件置 null 形态）
        var json = Wrap("AND", "AND", """{"jnpfKey":"inputNumber","field":"f_qty","symbol":"<>"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Null(list[0]["fieldValue"]);
    }

    [Fact]
    public void D1_I10_UnknownSymbol_EmitsClauseWithoutConditionalType()
    {
        // I10：未命中任何 symbol case → 条款仍入列且无 ConditionalType 键
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"!=","fieldValue":"v"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.False(list[0].ContainsKey("ConditionalType"));
        Assert.Equal("v", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I3_FieldValueWhitespace_StrippedBeforeEmit()
    {
        // I3：\r\n 与空格在条款生成前剥离（值非空路径）
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"==","fieldValue":" a b"}""");

        var list = Run(json);
        Assert.Equal("ab", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I3_MissingFieldValue_TextControlSetsEmptyString()
    {
        // I3：非数值控件缺省值 = 空串（与数值控件 null 对照）
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"<>"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal(string.Empty, list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I4_DateSingleValue_TimestampConvertedAndDatetimeAnnotated()
    {
        // I4：DATE 非 between 单值时间戳转换 + CSharpTypeName=datetime
        var json = Wrap("AND", "AND", """{"jnpfKey":"datePicker","field":"f_date","symbol":"==","fieldValue":"1577836800000"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.Equal, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal("datetime", list[0]["CSharpTypeName"]?.ToString());
        Assert.False(string.IsNullOrEmpty(list[0]["fieldValue"]?.ToString()));
        Assert.DoesNotContain("1577836800000", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I4_TimeControl_FormatsByFormatField()
    {
        // I4：TIME 控件按 format 字段格式化（非 between）；实测特征：I3 剥空格使日期时间粘连不可解析，故夹具用纯时间串（确定性形态，保真锁定格式化行为）
        var json = Wrap("AND", "AND", """{"jnpfKey":"timePicker","field":"f_time","symbol":"==","fieldValue":"13:05:09","format":"HH:mm:ss"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal("13:05:09", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I4_RateAndSlider_AnnotateDecimalType()
    {
        // I4：RATE/SLIDER 标注 CSharpTypeName=decimal
        var json = Wrap("AND", "AND", """
            {"jnpfKey":"rate","field":"f_rate","symbol":">=","fieldValue":"3"},
            {"jnpfKey":"slider","field":"f_slider","symbol":">=","fieldValue":"5"}
            """);

        var list = Run(json);
        Assert.Equal(2, list.Count);
        Assert.Equal("decimal", list[0]["CSharpTypeName"]?.ToString());
        Assert.Equal("decimal", list[1]["CSharpTypeName"]?.ToString());
    }

    [Fact]
    public void D1_I6_CheckboxIn_WrapsEachIdAsJson()
    {
        // I6：CHECKBOX isListValue=true → 每个 id 值 ToJsonString 包裹；非 COMSELECT 无 "] 后缀
        var json = Wrap("AND", "AND", """{"jnpfKey":"checkbox","field":"f_tags","symbol":"in","fieldValue":"[\"x\",\"y\"]"}""");

        var list = Run(json);
        Assert.Equal(2, list.Count);
        Assert.Equal("\"x\"", list[0]["fieldValue"]?.ToString());
        Assert.Equal("\"y\"", list[1]["fieldValue"]?.ToString());
        Assert.Equal(ConditionalType.Like, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_I6_NestedDoubleBracketIds_FlattenedToLastLevel()
    {
        // I6："[[" 嵌套形态取每对末级 id（select 控件：非 isListValue 族、非 COMSELECT → 无包裹无后缀）
        var json = Wrap("AND", "AND", """{"jnpfKey":"select","field":"f_area","symbol":"in","fieldValue":"[[\"p\",\"c1\"],[\"p\",\"c2\"]]"}""");

        var list = Run(json);
        Assert.Equal(2, list.Count);
        Assert.Equal("c1", list[0]["fieldValue"]?.ToString());
        Assert.Equal("c2", list[1]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I9_CurrOrganizeNestedIds_AppendQuoteBracketSuffix()
    {
        // I9（Q2）：CURRORGANIZE 嵌套形态同样追加 "] 后缀（与 COMSELECT 同族）
        var json = Wrap("AND", "AND", """{"jnpfKey":"currOrganize","field":"f_org","symbol":"in","fieldValue":"[[\"p\",\"o1\"]]"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal("o1\"]", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_I6_TreeSelectIn_UsesEqualInsteadOfLike()
    {
        // I6：TREESELECT 特判 — in 用 Equal（非 Like）
        var json = Wrap("AND", "AND", """{"jnpfKey":"treeSelect","field":"f_tree","symbol":"in","fieldValue":"[\"t1\"]"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.Equal, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_I6_TreeSelectNotIn_UsesNoEqualPlusIsNotGuards()
    {
        // I6+Q3：TREESELECT notIn 用 NoEqual，且追加 null/空 IsNot 双条款
        var json = Wrap("AND", "AND", """{"jnpfKey":"treeSelect","field":"f_tree","symbol":"notIn","fieldValue":"[\"t1\"]"}""");

        var list = Run(json);
        Assert.Equal(3, list.Count);
        Assert.Equal(ConditionalType.NoEqual, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[1]["ConditionalType"]));
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[2]["ConditionalType"]));
    }

    [Fact]
    public void D1_I6_WhereTypeSequence_InWithOrGroupLogic()
    {
        // I6 whereType 序列：组内逻辑 OR + in → 首条 Or，后续 Or（规则：i==0 && sub==And 才 And）
        var json = Wrap("AND", "OR", """{"jnpfKey":"select","field":"f_s","symbol":"in","fieldValue":"[\"a\",\"b\",\"c\"]"}""");

        var list = Run(json);
        Assert.Equal(3, list.Count);
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[0]["whereType"]));
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[1]["whereType"]));
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[2]["whereType"]));
    }

    [Fact]
    public void D1_I6_WhereTypeSequence_NotInWithAndGroupLogic()
    {
        // I6 whereType 序列：组内逻辑 AND + notIn → 首条 And，后续 And（规则：i==0 && sub==Or 才 Or）
        var json = Wrap("AND", "AND", """{"jnpfKey":"select","field":"f_s","symbol":"notIn","fieldValue":"[\"a\",\"b\"]"}""");

        var list = Run(json);
        // 2 id 条款 + 2 IsNot 条款（IsNot 条款恒 And）
        Assert.Equal(4, list.Count);
        Assert.Equal(WhereType.And, (WhereType)Convert.ToInt32(list[0]["whereType"]));
        Assert.Equal(WhereType.And, (WhereType)Convert.ToInt32(list[1]["whereType"]));
        Assert.Equal(ConditionalType.NoLike, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_I6_InWithNonListValue_FallsBackToIn()
    {
        // I6：值不含 "[" → 不展开，回退 ConditionalType.In（与 Append I5 回退 Equal 的语义差异，保真锁定）
        var json = Wrap("AND", "AND", """{"jnpfKey":"select","field":"f_s","symbol":"in","fieldValue":"solo"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.In, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal("solo", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_NullSymbol_TextControl_UsesIsNullOrEmpty()
    {
        // "null" 符号：非数值控件 → IsNullOrEmpty（与 NUMINPUT EqualNull 对照）
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"null"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.IsNullOrEmpty, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_NullSymbol_Calculate_UsesEqualNull()
    {
        // "null" 符号：CALCULATE 属数值族 → EqualNull
        var json = Wrap("AND", "AND", """{"jnpfKey":"calculate","field":"f_calc","symbol":"null"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.EqualNull, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_NotNullSymbol_EmptyValue_EmitsIsNotWithNull()
    {
        // notNull：fieldValue 空 → IsNot 且值置 null（非空值保持原值）
        var json = Wrap("AND", "AND", """
            {"jnpfKey":"select","field":"f_s","symbol":"notNull"},
            {"jnpfKey":"select","field":"f_t","symbol":"notNull","fieldValue":"v"}
            """);

        var list = Run(json);
        Assert.Equal(2, list.Count);
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Null(list[0]["fieldValue"]);
        Assert.Equal(ConditionalType.IsNot, (ConditionalType)Convert.ToInt32(list[1]["ConditionalType"]));
        Assert.Equal("v", list[1]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_NotLikeWithBrackets_StripsBrackets()
    {
        // notLike："[" 剥离（与 like 对称）
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"notLike","fieldValue":"[abc]"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.NoLike, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
        Assert.Equal("abc", list[0]["fieldValue"]?.ToString());
    }

    [Fact]
    public void D1_LikeWithEmptyValue_TextControl_FallsToIsNullOrEmpty()
    {
        // like + 空值：非数值控件 → IsNullOrEmpty（三元表达式回退支）
        var json = Wrap("AND", "AND", """{"jnpfKey":"input","field":"f_x","symbol":"like"}""");

        var list = Run(json);
        Assert.Single(list);
        Assert.Equal(ConditionalType.IsNullOrEmpty, (ConditionalType)Convert.ToInt32(list[0]["ConditionalType"]));
    }

    [Fact]
    public void D1_I2_TwoGroups_EachFirstItemCarriesWhereKey()
    {
        // I2：跨组 — 每组首条款各自携带 where 键（顶层 matchLogic）
        var json = """
            {
              "matchLogic": "OR",
              "conditionList": [
                { "logic": "AND", "groups": [ {"jnpfKey":"input","field":"f_a","symbol":"==","fieldValue":"1"} ] },
                { "logic": "AND", "groups": [ {"jnpfKey":"input","field":"f_b","symbol":"==","fieldValue":"2"} ] }
              ]
            }
            """;

        var list = Run(json);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].ContainsKey("where"));
        Assert.True(list[1].ContainsKey("where"));
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[0]["where"]));
        Assert.Equal(WhereType.Or, (WhereType)Convert.ToInt32(list[1]["where"]));
    }
}
