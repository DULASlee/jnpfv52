using System.Reflection;
using JNPF.Systems;
using Xunit;

namespace JNPF.Tests.Systems;

/// <summary>
/// CR-20260819-01 安全网：用户导入导出六端点的路由契约特征测试。
/// 按契约（路由模板 + HTTP 动词 + 方法签名形态）断言，不绑定宿主类型——
/// 拆分前宿主为 UsersService，拆分后为 UsersImportExportService，测试均须通过。
/// 配套硬门控：harness --mode routes 快照逐条比对（.claude/evidence/cr-20260819-01/）。
/// </summary>
public class UsersImportExportContractTests
{
    /// <summary>方法名 → 期望的 HTTP 动词特性名 + 路由模板。</summary>
    private static readonly Dictionary<string, (string VerbAttr, string Template)> Expected = new()
    {
        ["ExportData"] = ("HttpGetAttribute", "ExportData"),
        ["TemplateDownload"] = ("HttpGetAttribute", "TemplateDownload"),
        ["Uploader"] = ("HttpPostAttribute", "Uploader"),
        ["ImportPreview"] = ("HttpGetAttribute", "ImportPreview"),
        ["ExportExceptionData"] = ("HttpPostAttribute", "ExportExceptionData"),
        ["ImportData"] = ("HttpPostAttribute", "ImportData"),
    };

    private static bool IsHttpVerbAttr(object a) =>
        a.GetType().Name is "HttpGetAttribute" or "HttpPostAttribute";

    /// <summary>在 JNPF.Systems 程序集中按名定位承载该端点的动态 API 方法（拆分后宿主变化不影响定位）。</summary>
    private static MethodInfo ResolveAction(string methodName)
    {
        var candidates = typeof(UsersService).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.GetCustomAttributes().Any(a => a.GetType().Name == "RouteAttribute"
                    && (string?)a.GetType().GetProperty("Template")!.GetValue(a) == "api/permission/[controller]"))
            .Select(t => t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m != null && m.GetCustomAttributes().Any(IsHttpVerbAttr))
            .Cast<MethodInfo>()
            .ToList();

        Assert.True(candidates.Count == 1,
            $"方法 {methodName} 应恰好存在于 1 个 [Route(\"api/permission/[controller]\")] 动态 API 类上，实际 {candidates.Count} 处（重复或丢失均为路由契约破坏）");
        return candidates[0];
    }

    [Theory]
    [MemberData(nameof(ExpectedData))]
    public void ImportExportEndpoint_KeepsVerbAndRouteTemplate(string methodName, string verbAttrTypeName, string template)
    {
        var method = ResolveAction(methodName);

        var verbAttr = method.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == verbAttrTypeName);
        Assert.True(verbAttr != null, $"{methodName} 缺少 [{verbAttrTypeName}]（HTTP 动词契约破坏）");

        var routeTemplate = (string?)verbAttr!.GetType().GetProperty("Template")!.GetValue(verbAttr);
        Assert.Equal(template, routeTemplate);

        // 签名形态契约：末位参数为 CancellationToken（带默认值），拆分不得增删参数
        // （TemplateDownload 仅有 CancellationToken 一个参数，属现状契约）
        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, $"{methodName} 参数数量异常（至少包含 CancellationToken）");
        Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
        Assert.True(parameters[^1].HasDefaultValue, $"{methodName} 的 CancellationToken 必须保留默认值");
    }

    [Fact]
    public void ImportDataAndExportExceptionData_KeepUnitOfWork()
    {
        foreach (var name in new[] { "ImportData", "ExportExceptionData" })
        {
            var method = ResolveAction(name);
            Assert.True(
                method.GetCustomAttributes().Any(a => a.GetType().Name == "UnitOfWorkAttribute"),
                $"{name} 丢失 [UnitOfWork]（导入/错误报告必须在事务内执行）");
        }
    }

    [Fact]
    public void HostClass_KeepsControllerRouteTemplateAndTagName()
    {
        var host = ResolveAction("ImportData").DeclaringType!;

        var route = host.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == "RouteAttribute");
        Assert.NotNull(route);
        Assert.Equal("api/permission/[controller]",
            (string?)route!.GetType().GetProperty("Template")!.GetValue(route));

        // ApiDescriptionSettings Name="Users" 决定 [controller] 占位符的解析值，改名即路由契约破坏
        var desc = host.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == "ApiDescriptionSettingsAttribute");
        Assert.NotNull(desc);
        Assert.Equal("Users", (string?)desc!.GetType().GetProperty("Name")!.GetValue(desc));
    }

    public static IEnumerable<object[]> ExpectedData =>
        Expected.Select(kv => new object[] { kv.Key, kv.Value.VerbAttr, kv.Value.Template });
}
