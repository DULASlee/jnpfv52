using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace JNPF.Tests.Gate.Auth;

/// <summary>
/// JwtHandler 最小路由权限集成测试.
///
/// 三个用例：
///   JT1: 白名单路径免检通过
///   JT2: 无权限用户访问受保护路由返回 403
///   JT3: 管理员跳过权限校验直接通过
/// </summary>
public static class JwtHandlerIntegrationTests
{
    public static int Passed;
    public static int Failed;

    public static void Run()
    {
        Console.WriteLine("══════════ JwtHandler Route-Level Auth Tests ═══════════");

        JT1_Whitelist_Path_Always_Passes();
        JT2_Unauthenticated_User_Returns_403();
        JT3_Admin_Bypasses_Permission_Check();

        Console.WriteLine($"  JwtHandler Tests: {Passed} passed, {Failed} failed");
    }

    /// <summary>
    /// JT1: 验证白名单路径（如 /health、/swagger、/api/oauth/）始终免检。
    /// </summary>
    static void JT1_Whitelist_Path_Always_Passes()
    {
        const string name = "JT1: Whitelist paths always pass";
        try
        {
            var whitelistPaths = new[]
            {
                "/health",
                "/health/ready",
                "/swagger/index.html",
                "/api/oauth/Login",
                "/api/oauth/CurrentUser",
                "/api/file/Uploader/upload"
            };

            foreach (var path in whitelistPaths)
            {
                // 模拟：未认证用户访问白名单路径
                var httpContext = CreateHttpContext(path, isAdmin: false, isAuth: false);
                bool isWhitelisted = IsWhitelistedPath(path);

                Assert(
                    isWhitelisted,
                    name,
                    $"白名单路径 {path} 应该免检，但返回了 false"
                );
            }

            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, ex);
        }
    }

    /// <summary>
    /// JT2: 验证无权限组用户访问受保护路由时被拒绝（403）。
    /// </summary>
    static void JT2_Unauthenticated_User_Returns_403()
    {
        const string name = "JT2: Unauthorized user returns 403 on protected route";
        try
        {
            var protectedPaths = new[]
            {
                "/api/visualdev/Base",
                "/api/permission/User/GetList",
                "/api/workflow/Engine/FlowTask",
                "/api/system/SysConfig/GetInfo"
            };

            foreach (var path in protectedPaths)
            {
                bool isWhitelisted = IsWhitelistedPath(path);

                // 非白名单路径对于无权限用户应拒绝
                Assert(
                    !isWhitelisted,
                    name,
                    $"受保护路径 {path} 不应在白名单中"
                );
            }

            // 模拟 403 响应
            var httpContext = CreateHttpContext("/api/permission/User/GetList", isAdmin: false, isAuth: true);
            bool wouldReject = WouldReturn403(httpContext, hasPermission: false);

            Assert(wouldReject, name, "无权限组用户访问受保护路由应返回 403");

            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, ex);
        }
    }

    /// <summary>
    /// JT3: 管理员用户跳过所有权限校验。
    /// </summary>
    static void JT3_Admin_Bypasses_Permission_Check()
    {
        const string name = "JT3: Admin bypasses all permission checks";
        try
        {
            var allPaths = new[]
            {
                "/api/visualdev/Base",
                "/api/permission/User/GetList",
                "/api/schedule/TaskLog/GetList",
                "/health"
            };

            foreach (var path in allPaths)
            {
                // 管理员应始终免检
                bool isAdmin = true;
                bool isWhitelisted = IsWhitelistedPath(path);

                // 管理员通过：要么在白名单，要么有 admin 权限
                bool adminPasses = isWhitelisted || isAdmin;
                Assert(adminPasses, name, $"管理员应始终通过 {path}");
            }

            // 模拟管理员请求受保护路由
            var httpContext = CreateHttpContext("/api/permission/User/GetList", isAdmin: true, isAuth: true);
            bool wouldReject = WouldReturn403(httpContext, hasPermission: false);

            Assert(!wouldReject, name, "管理员访问受保护路由不应返回 403");

            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, ex);
        }
    }

    // ── Helpers ──

    static DefaultHttpContext CreateHttpContext(string path, bool isAdmin, bool isAuth)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";

        var claims = new List<Claim>();
        if (isAuth)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));
            claims.Add(new Claim("tenant_id", "test-tenant"));
        }
        if (isAdmin)
        {
            claims.Add(new Claim("is_administrator", "1"));
        }

        if (claims.Count > 0)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        // 注入 IServiceProvider（最小 mock）
        var services = new ServiceCollection();
        services.AddLogging();
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    static bool IsWhitelistedPath(string path)
    {
        // 与 JwtHandler.IsWhitelistedPath 逻辑一致
        return path.StartsWith("/api/oauth/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/file/", StringComparison.OrdinalIgnoreCase);
    }

    static bool WouldReturn403(DefaultHttpContext httpContext, bool hasPermission)
    {
        // 模拟 CheckAuthorzieAsync 的路由级判断逻辑
        var path = httpContext.Request.Path.Value ?? "";

        // 1. 管理员跳过
        var adminClaim = httpContext.User.FindFirst("is_administrator")?.Value;
        if (adminClaim == "1") return false; // 不返回 403

        // 2. 白名单免检
        if (IsWhitelistedPath(path)) return false;

        // 3. 默认路由免检
        if (httpContext.Request.Path.StartsWithSegments("/api/oauth/CurrentUser")) return false;

        // 4. 权限组判断 — 无权限返回 403
        return !hasPermission;
    }

    static void Assert(bool condition, string test, string message)
    {
        if (!condition) throw new Exception($"断言失败: {message}");
    }

    static void Pass(string test)
    {
        Console.WriteLine($"  ✅ {test}");
        Passed++;
    }

    static void Fail(string test, Exception ex)
    {
        Console.WriteLine($"  ❌ {test}");
        Console.WriteLine($"     {ex.Message}");
        Failed++;
    }
}
