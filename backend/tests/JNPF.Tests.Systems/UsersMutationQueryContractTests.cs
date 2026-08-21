using System.Reflection;
using JNPF.Systems;
using Xunit;

namespace JNPF.Tests.Systems;

/// <summary>
/// CR-20260819-01 阶段 2/3 安全网：用户写端点（6）与查询端点（18）的路由契约特征测试。
/// 按契约（路由模板 + HTTP 动词 + 方法签名形态）断言，不绑定宿主类型——
/// 拆分前宿主为 UsersService，拆分后写归 UsersMutationService、查归 UsersQueryService，测试均须通过。
/// 配套硬门控：harness --mode routes 快照逐条比对（.claude/evidence/cr-20260819-01/）。
/// </summary>
public class UsersMutationQueryContractTests
{
    /// <summary>阶段 2：写端点 方法名 → 期望的 HTTP 动词特性名 + 路由模板。</summary>
    private static readonly Dictionary<string, (string VerbAttr, string Template)> MutationEndpoints = new()
    {
        ["Create"] = ("HttpPostAttribute", ""),
        ["Delete"] = ("HttpDeleteAttribute", "{id}"),
        ["Update"] = ("HttpPutAttribute", "{id}"),
        ["UpdateState"] = ("HttpPutAttribute", "{id}/Actions/State"),
        ["ResetPassword"] = ("HttpPostAttribute", "{id}/Actions/ResetPassword"),
        ["Unlock"] = ("HttpPutAttribute", "{id}/Actions/Unlock"),
    };

    /// <summary>阶段 3：查询端点 方法名 → 期望的 HTTP 动词特性名 + 路由模板。</summary>
    private static readonly Dictionary<string, (string VerbAttr, string Template)> QueryEndpoints = new()
    {
        ["GetList"] = ("HttpGetAttribute", ""),
        ["GetUserAllList"] = ("HttpGetAttribute", "All"),
        ["GetUsersByRoleId"] = ("HttpGetAttribute", "getUsersByRoleId"),
        ["GetUsersByRoleOrgId"] = ("HttpGetAttribute", "GetUsersByRoleOrgId"),
        ["GetImUserList"] = ("HttpGetAttribute", "ImUser"),
        ["GetSelector"] = ("HttpGetAttribute", "Selector"),
        ["GetInfo"] = ("HttpGetAttribute", "{id}"),
        ["GetOrganizeMember"] = ("HttpGetAttribute", "getOrganization"),
        ["GetWorkByUser"] = ("HttpGetAttribute", "getWorkByUser"),
        ["GetUsersByPositionId"] = ("HttpGetAttribute", "GetUsersByPositionId"),
        ["GetDefaultCurrentValueUserId"] = ("HttpPostAttribute", "getDefaultCurrentValueUserId"),
        ["GetUserList"] = ("HttpPostAttribute", "GetUserList"),
        ["GetOrganizeMemberList"] = ("HttpPostAttribute", "ImUser/Selector/{organizeId}"),
        ["GetListByAuthorize"] = ("HttpPostAttribute", "GetListByAuthorize/{organizeId}"),
        ["GetSubordinate"] = ("HttpPostAttribute", "getSubordinates"),
        ["UserCondition"] = ("HttpPostAttribute", "UserCondition"),
        ["GetSelectedList"] = ("HttpPostAttribute", "GetSelectedList"),
        ["GetSelectedUserList"] = ("HttpPostAttribute", "GetSelectedUserList"),
    };

    private static bool IsHttpVerbAttr(object a) =>
        a.GetType().Name is "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or "HttpDeleteAttribute";

    /// <summary>
    /// 在 JNPF.Systems 程序集中按名定位承载该端点的动态 API 方法（拆分后宿主变化不影响定位）。
    /// 收窄条件：类级 Route 模板 + ApiDescriptionSettings Name=="Users"（[controller] 占位符解析值）——
    /// permission 域内 Roles/Organizes 等类共享同一 Route 模板且存在同名 CRUD 方法，仅按模板收窄会误匹配。
    /// 用 GetMethods 过滤而非 GetMethod：GetOrganizeMemberList 存在 [NonAction] 重载，GetMethod 会抛 AmbiguousMatchException。
    /// </summary>
    private static MethodInfo ResolveAction(string methodName)
    {
        var candidates = typeof(UsersService).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.GetCustomAttributes().Any(a => a.GetType().Name == "RouteAttribute"
                    && (string?)a.GetType().GetProperty("Template")!.GetValue(a) == "api/permission/[controller]")
                && t.GetCustomAttributes().Any(a => a.GetType().Name == "ApiDescriptionSettingsAttribute"
                    && (string?)a.GetType().GetProperty("Name")!.GetValue(a) == "Users"))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.Name == methodName && m.GetCustomAttributes().Any(IsHttpVerbAttr))
            .ToList();

        Assert.True(candidates.Count == 1,
            $"方法 {methodName} 应恰好存在于 1 个 [Route(\"api/permission/[controller]\")] 动态 API 类上，实际 {candidates.Count} 处（重复或丢失均为路由契约破坏）");
        return candidates[0];
    }

    private static void AssertEndpointContract(string methodName, string verbAttrTypeName, string template)
    {
        var method = ResolveAction(methodName);

        var verbAttr = method.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == verbAttrTypeName);
        Assert.True(verbAttr != null, $"{methodName} 缺少 [{verbAttrTypeName}]（HTTP 动词契约破坏）");

        var routeTemplate = (string?)verbAttr!.GetType().GetProperty("Template")!.GetValue(verbAttr);
        Assert.Equal(template, routeTemplate);

        // 签名形态契约：末位参数为 CancellationToken（带默认值），拆分不得增删参数
        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, $"{methodName} 参数数量异常（至少包含 CancellationToken）");
        Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
        Assert.True(parameters[^1].HasDefaultValue, $"{methodName} 的 CancellationToken 必须保留默认值");
    }

    [Theory]
    [MemberData(nameof(MutationData))]
    public void MutationEndpoint_KeepsVerbAndRouteTemplate(string methodName, string verbAttrTypeName, string template)
        => AssertEndpointContract(methodName, verbAttrTypeName, template);

    [Theory]
    [MemberData(nameof(QueryData))]
    public void QueryEndpoint_KeepsVerbAndRouteTemplate(string methodName, string verbAttrTypeName, string template)
        => AssertEndpointContract(methodName, verbAttrTypeName, template);

    [Fact]
    public void Create_KeepsAllowAnonymous()
    {
        // Create 支持外部自主注册（代码注释明示），[AllowAnonymous] 是权限契约的一部分，剥离不得丢失
        var method = ResolveAction("Create");
        Assert.True(
            method.GetCustomAttributes().Any(a => a.GetType().Name == "AllowAnonymousAttribute"),
            "Create 丢失 [AllowAnonymous]（外部自主注册入口将要求登录，权限契约破坏）");
    }

    public static IEnumerable<object[]> MutationData =>
        MutationEndpoints.Select(kv => new object[] { kv.Key, kv.Value.VerbAttr, kv.Value.Template });

    public static IEnumerable<object[]> QueryData =>
        QueryEndpoints.Select(kv => new object[] { kv.Key, kv.Value.VerbAttr, kv.Value.Template });
}
