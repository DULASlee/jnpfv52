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

    // ==================== D1.5 S1 特征用例（规格 §2.5 不变量 + A0 补登，拆分前金标准） ====================
    // 金标准纪律：本批用例在当前实现上全绿后才允许拆分；拆分后逐条等价。
    // 反序列化硬契约：匿名对象属性名（Key/Value/FieldName/FieldValue/ConditionalType）与枚举数值。

    private static Newtonsoft.Json.Linq.JArray J(List<object> list) =>
        Newtonsoft.Json.Linq.JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(list));

    private static int Cond(Newtonsoft.Json.Linq.JToken clause) =>
        (int)clause["Value"]!["ConditionalType"]!;

    private static int Key(Newtonsoft.Json.Linq.JToken clause) => (int)clause["Key"]!;

    [Theory]
    [InlineData(QueryType.Equal, ConditionalType.Equal)]
    [InlineData(QueryType.NotEqual, ConditionalType.NoEqual)]
    [InlineData(QueryType.Included, ConditionalType.Like)]
    [InlineData(QueryType.NotIncluded, ConditionalType.NoLike)]
    [InlineData(QueryType.GreaterThan, ConditionalType.GreaterThan)]
    [InlineData(QueryType.GreaterThanOrEqual, ConditionalType.GreaterThanOrEqual)]
    [InlineData(QueryType.LessThan, ConditionalType.LessThan)]
    [InlineData(QueryType.LessThanOrEqual, ConditionalType.LessThanOrEqual)]
    public void D1_I1_EightDirectMappings_OneClauseEach_NoContinue(QueryType qt, ConditionalType expected)
    {
        // I1：八种直映逐一对齐（规格 §2.5 登记全集），单条款、返回 false、值透传
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, qt, "f_x", "v1", null, "varchar", "and");
        Assert.False(cont);
        Assert.Single(list);
        var clause = J(list)[0];
        Assert.Equal((int)expected, Cond(clause));
        Assert.Equal((int)WhereType.And, Key(clause));
        Assert.Equal("f_x", (string)clause["Value"]!["FieldName"]!);
        Assert.Equal("v1", (string)clause["Value"]!["FieldValue"]!);
    }

    [Fact]
    public void D1_I1_DirectMapping_OrLogic_KeyIsOr()
    {
        // I1 补：logic="or" → Key=WhereType.Or（logicWhere 分派，直映分支共用）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.NotEqual, "f_x", "v", null, "varchar", "or");
        Assert.Equal((int)WhereType.Or, Key(J(list)[0]));
    }

    [Fact]
    public void D1_I2_NotNull_UsesIsNot_NoContinue()
    {
        // I2：NotNull → IsNot（字段恒非空语义），单条款返回 false
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.NotNull, "f_x", null, null, "varchar", "and");
        Assert.False(cont);
        var clause = J(list)[0];
        Assert.Equal((int)ConditionalType.IsNot, Cond(clause));
        Assert.Equal((int)WhereType.And, Key(clause));
    }

    [Fact]
    public void D1_I3_Between_NullList_NoClause_NoContinue()
    {
        // I3：between=null → IsNotEmptyOrNull 为 false，不追加条款、返回 false。
        // 2026-08-24 实测修正：between=空列表时 IsNotEmptyOrNull 仅判 null，
        // between[0] 抛 ArgumentOutOfRangeException —— 既有异常行为，保真不修。
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.Between, "f_d", null, null, "datetime", "and");
        Assert.False(cont);
        Assert.Empty(list);

        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            GetConditionQueryClauseAppender.Append(
                new List<object>(), QueryType.Between, "f_d", null,
                new List<string>(), "datetime", "and"));
    }

    [Fact]
    public void D1_I3_Between_SecondClauseKeyForcedAnd_EvenWithOrLogic()
    {
        // I3：首条款 Key=logicWhere；次条款 Key 恒为 And（even logic="or"）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Between, "f_d", null, new List<string> { "lo", "hi" }, "datetime", "or");
        var arr = J(list);
        Assert.Equal((int)WhereType.Or, Key(arr[0]));
        Assert.Equal((int)ConditionalType.GreaterThanOrEqual, Cond(arr[0]));
        Assert.Equal("lo", (string)arr[0]["Value"]!["FieldValue"]!);
        Assert.Equal((int)WhereType.And, Key(arr[1]));
        Assert.Equal((int)ConditionalType.LessThanOrEqual, Cond(arr[1]));
        Assert.Equal("hi", (string)arr[1]["Value"]!["FieldValue"]!);
    }

    [Theory]
    [InlineData("double")]
    [InlineData("bigint")]
    public void D1_I4_Null_NumericVariants_UseEqualNull(string fieldType)
    {
        // I4：Null 数值三型（double/int/bigint）→ EqualNull（int 已由既有用例锁定）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Null, "f_n", null, null, fieldType, "and");
        Assert.Equal((int)ConditionalType.EqualNull, Cond(J(list)[0]));
    }

    [Fact]
    public void D1_I4_Null_NonNumericTypes_UseIsNullOrEmpty()
    {
        // I4：非数值型（datetime 等）→ IsNullOrEmpty（varchar 已由既有用例锁定）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Null, "f_d", null, null, "datetime", "and");
        Assert.Equal((int)ConditionalType.IsNullOrEmpty, Cond(J(list)[0]));
    }

    [Fact]
    public void D1_I5_In_NestedArray_FlattenedInOrder()
    {
        // I5：嵌套拍平 — Contains('[') 分支仅对字符串编码的内层数组生效：
        // ["a","[\"b\",\"c\"]"]（内层为字符串）→ 拍平为 3 条款，按序 a→b→c。
        // 2026-08-24 实测修正：真实嵌套数组 ["a",["b","c"]] 在 ToObject<List<string>>
        // 即抛 JsonReaderException（无法反序列化为 string 元素）—— 既有行为，保真锁定。
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "[\"a\",\"[\\\"b\\\",\\\"c\\\"]\"]", null, "varchar", "and");
        Assert.True(cont);
        var arr = J(list);
        Assert.Equal(3, arr.Count);
        Assert.Equal(new[] { "a", "b", "c" }, arr.Select(t => (string)t["Value"]!["FieldValue"]!).ToArray());
        Assert.All(arr, t => Assert.Equal((int)ConditionalType.Like, Cond(t)));

        Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
            GetConditionQueryClauseAppender.Append(
                new List<object>(), QueryType.In, "f_org",
                "[\"a\",[\"b\",\"c\"]]", null, "varchar", "and"));
    }

    [Fact]
    public void D1_I5_In_WhereTypeSequence_AndLogic_FirstAndRestOr()
    {
        // I5：In + logic="and" → Key 序列 [And, Or, Or]（首条 And，其余 Or）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "[\"a\",\"b\",\"c\"]", null, "varchar", "and");
        Assert.Equal(
            new[] { (int)WhereType.And, (int)WhereType.Or, (int)WhereType.Or },
            J(list).Select(Key).ToArray());
    }

    [Fact]
    public void D1_I5_NotIn_WhereTypeSequence_OrLogic_FirstOrRestAnd()
    {
        // I5：NotIn + logic="or" → NoLike 条款 Key 序列 [Or, And, And]；
        // NotIn + logic="and" → 恒 [And, ...]（同一用例双断言锁定两分支）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.NotIn, "f_org", "[\"a\",\"b\",\"c\"]", null, "varchar", "or");
        var noLike = J(list).Take(3).ToArray();
        Assert.Equal(
            new[] { (int)WhereType.Or, (int)WhereType.And, (int)WhereType.And },
            noLike.Select(Key).ToArray());

        var list2 = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list2, QueryType.NotIn, "f_org", "[\"a\",\"b\"]", null, "varchar", "and");
        Assert.All(J(list2).Take(2), t => Assert.Equal((int)WhereType.And, Key(t)));
    }

    [Fact]
    public void D1_I6_NotIn_TrailingGuards_NullStringThenEmptyString_IsNot()
    {
        // I6（Q11/"null" 语义保真）：NotIn 尾部追加两条 IsNot 守卫 —
        // 先字符串 "null" 后空串，Key 恒 And，顺序固定（数据权限非空放行契约）
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.NotIn, "f_org", "[\"o1\"]", null, "varchar", "and");
        var arr = J(list);
        Assert.Equal(3, arr.Count);
        Assert.Equal((int)ConditionalType.NoLike, Cond(arr[0]));
        Assert.Equal((int)ConditionalType.IsNot, Cond(arr[1]));
        Assert.Equal("null", (string)arr[1]["Value"]!["FieldValue"]!);
        Assert.Equal((int)WhereType.And, Key(arr[1]));
        Assert.Equal((int)ConditionalType.IsNot, Cond(arr[2]));
        Assert.Equal(string.Empty, (string)arr[2]["Value"]!["FieldValue"]!);
        Assert.Equal((int)WhereType.And, Key(arr[2]));
    }

    [Fact]
    public void D1_I6_RealNullValue_FallsBackToEqual_WithNullFieldValue()
    {
        // I6：真实 null（itemValue==null）→ 不进解析分支，回退 Equal 且 FieldValue 为 null
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", null, null, "varchar", "and");
        Assert.False(cont);
        var clause = J(list)[0];
        Assert.Equal((int)ConditionalType.Equal, Cond(clause));
        Assert.Equal(Newtonsoft.Json.Linq.JTokenType.Null, clause["Value"]!["FieldValue"]!.Type);
    }

    [Fact]
    public void D1_I6_NullLiteralString_FallsBackToEqual_StringPreserved()
    {
        // I6：字符串 "null"（不含 '['）→ 回退 Equal，"null" 字符串原样透传（与真实 null 语义分离）
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "null", null, "varchar", "and");
        Assert.False(cont);
        var clause = J(list)[0];
        Assert.Equal((int)ConditionalType.Equal, Cond(clause));
        Assert.Equal("null", (string)clause["Value"]!["FieldValue"]!);
    }

    [Fact]
    public void D1_I7_Contains_UnmappedQueryType_DefaultPath_NoClause_NoContinue()
    {
        // I7（A0 补登事实）：QueryType.Contains（"模糊"）无 case → default 路径：
        // 不追加条款、返回 false（调用方不 continue，走后续默认处理）
        var list = new List<object>();
        var cont = GetConditionQueryClauseAppender.Append(
            list, QueryType.Contains, "f_x", "v", null, "varchar", "and");
        Assert.False(cont);
        Assert.Empty(list);
    }

    [Fact]
    public void D1_I8_ConditionOrder_DirectThenExpanded_GuaranteesSequence()
    {
        // I8：条件顺序契约 — 先直映条款后 In 展开条款，列表内顺序即最终条件组合顺序
        var list = new List<object>();
        GetConditionQueryClauseAppender.Append(
            list, QueryType.Equal, "f_status", "1", null, "varchar", "and");
        GetConditionQueryClauseAppender.Append(
            list, QueryType.In, "f_org", "[\"a\",\"b\"]", null, "varchar", "and");
        var arr = J(list);
        Assert.Equal(3, arr.Count);
        Assert.Equal("f_status", (string)arr[0]["Value"]!["FieldName"]!);
        Assert.Equal((int)ConditionalType.Equal, Cond(arr[0]));
        Assert.Equal("a", (string)arr[1]["Value"]!["FieldValue"]!);
        Assert.Equal("b", (string)arr[2]["Value"]!["FieldValue"]!);
    }
}
