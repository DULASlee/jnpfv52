using System.Reflection;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.User.Conditions;
using JNPF.Common.Enums;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// P0-B S1 特征金标准（路径 B：GetConditionAsync/GetDataConditionAsync 行为契约锁定，非重构）。
/// 链路事实：唯一外部消费者 OrderService.GetList（GetConditionAsync&lt;OrderListOutput&gt;(menu.Id, "F_ID", true, "a.")）；
/// 产出以 List&lt;IConditionalModel&gt; 进 SqlSugar .Where(authorizeWhere)。
/// 金标准纪律：本批用例在当前实现上全绿后才算锁定；不修改任何实现。
/// </summary>
public class UserManagerPathBDataPermissionTests
{
    private static Newtonsoft.Json.Linq.JArray J(List<object> list) =>
        Newtonsoft.Json.Linq.JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(list));

    private static int Key(Newtonsoft.Json.Linq.JToken c) => (int)c["Key"]!;

    private static int Cond(Newtonsoft.Json.Linq.JToken c) => (int)c["Value"]!["ConditionalType"]!;

    private static ConditionStrategyContext Ctx(
        string field, QueryType method, ConditionalType type, string logic, bool isCurrentRole, string[] ids) =>
        new()
        {
            ItemField = field,
            ItemMethod = method,
            ConditionalType = type,
            Logic = logic,
            IsCurrentRole = isCurrentRole,
            Ids = ids,
        };

    // ==================== A. AppendIds 分支矩阵 ====================

    [Fact]
    public void A1_EmptyIds_AppendsNothing()
    {
        var list = new List<object>();
        var ctx = Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", true, Array.Empty<string>());
        ConditionClauseAppender.AppendIds(list, ctx);
        Assert.Empty(list);
    }

    [Fact]
    public void A2_SingleId_And_CurrentRole_KeyIsOr()
    {
        // Q-PB1 怪异保真：and + isCurrentRole=true 时首条 Key 取 Or（而非 And）
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", true, new[] { "o1" }));
        Assert.Equal((int)WhereType.Or, Key(J(list)[0]));
    }

    [Fact]
    public void A3_SingleId_And_NotCurrentRole_KeyIsAnd()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", false, new[] { "o1" }));
        Assert.Equal((int)WhereType.And, Key(J(list)[0]));
    }

    [Fact]
    public void A4_SingleId_Or_KeyIsOr()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "or", false, new[] { "o1" }));
        Assert.Equal((int)WhereType.Or, Key(J(list)[0]));
    }

    [Fact]
    public void A5_MultiId_And_CurrentRole_Equal_AllOr()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", true, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.Or, (int)WhereType.Or, (int)WhereType.Or }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A6_MultiId_And_NotCurrentRole_Equal_FirstAndRestOr()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", false, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.And, (int)WhereType.Or, (int)WhereType.Or }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A7_MultiId_NotEqual_And_CurrentRole_FirstOrRestAnd()
    {
        // i>0 且 NotEqual/NotIncluded → isCurrentRole 分支（首条后 false → And）
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.NotEqual, ConditionalType.NoLike, "and", true, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.Or, (int)WhereType.And, (int)WhereType.And }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A8_MultiId_NotEqual_Or_FirstOrRestAnd()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.NotEqual, ConditionalType.NoLike, "or", false, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.Or, (int)WhereType.And, (int)WhereType.And }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A9_MultiId_NotIncluded_And_NotCurrentRole_AllAnd()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.NotIncluded, ConditionalType.NoLike, "and", false, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.And, (int)WhereType.And, (int)WhereType.And }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A10_MultiId_Included_Or_AllOr()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("f_org", QueryType.Included, ConditionalType.Like, "or", true, new[] { "a", "b", "c" }));
        Assert.Equal(new[] { (int)WhereType.Or, (int)WhereType.Or, (int)WhereType.Or }, J(list).Select(Key).ToArray());
    }

    [Fact]
    public void A11_IsCurrentRole_FlipsAfterFirst()
    {
        var list = new List<object>();
        var ctx = Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", true, new[] { "a", "b" });
        ConditionClauseAppender.AppendIds(list, ctx);
        Assert.False(ctx.IsCurrentRole);
    }

    [Fact]
    public void A12_FieldValueType_Transparent()
    {
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("a.F_ID", QueryType.Equal, ConditionalType.Like, "and", false, new[] { "u1" }));
        var c = J(list)[0];
        Assert.Equal("a.F_ID", (string)c["Value"]!["FieldName"]!);
        Assert.Equal("u1", (string)c["Value"]!["FieldValue"]!);
        Assert.Equal((int)ConditionalType.Like, Cond(c));
    }

    // ==================== B. Registry 6 token 全查 ====================

    [Theory]
    [InlineData(ConditionStrategyRegistry.UserId)]
    [InlineData(ConditionStrategyRegistry.UserAndSubordinates)]
    [InlineData(ConditionStrategyRegistry.OrganizeId)]
    [InlineData(ConditionStrategyRegistry.OrganizationAndSub)]
    [InlineData(ConditionStrategyRegistry.BranchManageOrganize)]
    [InlineData(ConditionStrategyRegistry.BranchManageOrganizeAndSub)]
    public void B_Tokens_RegistryResolves(string token)
    {
        Assert.True(ConditionStrategyRegistry.TryGet(token, out var strategy));
        Assert.Equal(token, strategy!.ItemType);
        var list = new List<object>();
        strategy.Append(list, Ctx("f_org", QueryType.Equal, ConditionalType.Like, "and", false, new[] { "x" }));
        Assert.Single(list);
    }

    // ==================== C. 短路层补 ====================

    [Fact]
    public void C1_DenyAll_PrimaryKeyAsInt_IntConvert()
    {
        var models = DataPermissionShortCircuits.DenyAll("f_id", primaryKeyAsInt: true);
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        Assert.IsType<int>(cm.FieldValueConvertFunc!("0"));
    }

    // ==================== D. GetConditionalModel（private 反射） ====================

    private static ConditionalModel InvokeGetConditionalModel(QueryType qt, string field, string value, string dataType = "string")
    {
        // 绕过构造函数（App 静态初始化在单测环境不可用）：GetConditionalModel 为纯函数，不依赖实例状态
        var um = (UserManager)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UserManager));
        var m = typeof(UserManager).GetMethod("GetConditionalModel", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (ConditionalModel)m.Invoke(um, new object[] { qt, field, value, dataType })!;
    }

    [Fact]
    public void D1_Contains_MapsLike()
    {
        var cm = InvokeGetConditionalModel(QueryType.Contains, "f_name", "abc");
        Assert.Equal(ConditionalType.Like, cm.ConditionalType);
        Assert.Equal("abc", cm.FieldValue);
        Assert.Null(cm.FieldValueConvertFunc);
    }

    [Fact]
    public void D2_Equal_Int32_IntConvert()
    {
        var cm = InvokeGetConditionalModel(QueryType.Equal, "f_n", "5", "Int32");
        Assert.Equal(ConditionalType.Equal, cm.ConditionalType);
        Assert.Equal(5, cm.FieldValueConvertFunc!("5"));
    }

    [Fact]
    public void D3_Equal_Double_DoubleConvert()
    {
        var cm = InvokeGetConditionalModel(QueryType.Equal, "f_n", "5.5", "Double");
        Assert.Equal(ConditionalType.Equal, cm.ConditionalType);
        Assert.Equal(5.5d, cm.FieldValueConvertFunc!("5.5"));
    }

    [Fact]
    public void D4_Equal_String_NoConvert()
    {
        var cm = InvokeGetConditionalModel(QueryType.Equal, "f_n", "x");
        Assert.Equal(ConditionalType.Equal, cm.ConditionalType);
        Assert.Null(cm.FieldValueConvertFunc);
    }

    [Fact]
    public void D5_NotEqual_Int32_NoEqualInt()
    {
        var cm = InvokeGetConditionalModel(QueryType.NotEqual, "f_n", "5", "Int32");
        Assert.Equal(ConditionalType.NoEqual, cm.ConditionalType);
        Assert.Equal(5, cm.FieldValueConvertFunc!("5"));
    }

    [Fact]
    public void D6_GreaterThan_MapsGreaterThan()
    {
        var cm = InvokeGetConditionalModel(QueryType.GreaterThan, "f_n", "1");
        Assert.Equal(ConditionalType.GreaterThan, cm.ConditionalType);
    }

    [Fact]
    public void D7_Between_Unmapped_EmptyModel()
    {
        // E-PB3 怪异保真：QueryType.Between 在 GetConditionalModel 无 case → 返回空 ConditionalModel
        // （FieldName/FieldValue 为 null，ConditionalType 为枚举默认）—— 既有行为，登记不修
        var cm = InvokeGetConditionalModel(QueryType.Between, "f_d", "a");
        Assert.Null(cm.FieldName);
        Assert.Null(cm.FieldValue);
    }

    [Theory]
    [InlineData(QueryType.In, ConditionalType.In)]
    [InlineData(QueryType.Included, ConditionalType.Like)]
    [InlineData(QueryType.NotIn, ConditionalType.NotIn)]
    [InlineData(QueryType.NotIncluded, ConditionalType.NoLike)]
    public void D8_InFamily_Mappings(QueryType qt, ConditionalType expected)
    {
        var cm = InvokeGetConditionalModel(qt, "f_x", "v");
        Assert.Equal(expected, cm.ConditionalType);
    }

    // ==================== E. ReplaceOp（private 反射） ====================

    private static string InvokeReplaceOp(string op)
    {
        var um = (UserManager)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UserManager));
        var m = typeof(UserManager).GetMethod("ReplaceOp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (string)m.Invoke(um, new object[] { op })!;
    }

    [Theory]
    [InlineData("==", "Equal")]
    [InlineData("between", "Between")]
    [InlineData(">", "GreaterThan")]
    [InlineData("<", "LessThan")]
    [InlineData("<>", "NotEqual")]
    [InlineData(">=", "GreaterThanOrEqual")]
    [InlineData("<=", "LessThanOrEqual")]
    [InlineData("like", "Included")]
    [InlineData("notLike", "NotIncluded")]
    public void E1_SymbolMapping(string op, string expected) => Assert.Equal(expected, InvokeReplaceOp(op));

    [Fact]
    public void E2_UnknownSymbol_Passthrough() => Assert.Equal("customOp", InvokeReplaceOp("customOp"));

    // ==================== R. 调用路由/消费契约 ====================

    [Fact]
    public void R1_InterfaceSignatures_Unchanged()
    {
        var iface = typeof(IUserManager);
        var m1 = iface.GetMethod("GetConditionAsync")!;
        Assert.Equal(typeof(Task<List<IConditionalModel>>), m1.ReturnType);
        Assert.Equal(
            new[] { typeof(string), typeof(string), typeof(bool), typeof(string) },
            m1.GetParameters().Select(p => p.ParameterType).ToArray());

        var m2 = iface.GetMethod("GetDataConditionAsync")!;
        Assert.Equal(typeof(Task<List<IConditionalModel>>), m2.ReturnType);
        Assert.Equal(
            new[] { typeof(string), typeof(string), typeof(bool) },
            m2.GetParameters().Select(p => p.ParameterType).ToArray());
    }

    [Fact]
    public void R2_TableNumberPrefix_OrderServiceShape()
    {
        // OrderService 实际调用参数：GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.")
        // → itemField = tableNumber + primaryKey = "a." + "F_ID" = "a.F_ID"（GetConditionAsync L550 形态）
        var list = new List<object>();
        ConditionClauseAppender.AppendIds(list, Ctx("a.F_ID", QueryType.Equal, ConditionalType.Like, "and", false, new[] { "u1" }));
        Assert.Equal("a.F_ID", (string)J(list)[0]["Value"]!["FieldName"]!);
    }

    [Fact]
    public void R3_ShortCircuit_ToSqlWhere_SnapshotStable()
    {
        var models = DataPermissionShortCircuits.AllowAll("a.F_ID");
        var where = DataPermissionWhereSnapshot.ToSqlWhere(models);
        // SqlSugar 将 "a.F_ID" 渲染为 [a].[F_ID]（表别名.列名形态）—— 消费契约锁定
        Assert.Contains("a].[F_ID", where, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(where, DataPermissionWhereSnapshot.ToSqlWhere(models));
    }
}
