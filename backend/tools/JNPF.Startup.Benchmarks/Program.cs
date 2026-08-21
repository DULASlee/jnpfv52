// ═══════════════════════════════════════════════════════════════
// JNPF.Startup.Benchmarks — 战役0 启动性能基准（2026-08-19）
//
// 模式：
//   --mode process  真实进程冷启动测量（外部拉起 JNPF.API.Entry，默认）
//   --mode inproc   in-process 组合测量（DI 注册数/模块加载/扫描成本）
//
// 指标：冷启动耗时、DI 注册总数、首请求延迟、模块加载耗时、
//       程序集扫描成本、Swagger 文档生成（反射）成本。
// 说明：本工程不加入主 sln，不影响 CI 构建。
// ═══════════════════════════════════════════════════════════════

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var mainSw = Stopwatch.StartNew();

// ---------- 参数解析 ----------
string mode = "process";
int rounds = 5;
int port = 5000;
string? entryDirOverride = null;
string config = "Debug";
string environment = "Production"; // 与线上/启动日志一致；Development 会开启 Scope 校验（当前存在存量违规，另立缺陷记录）
string routeFilter = "api/permission/users"; // --mode routes 的路径过滤子串

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--mode" when i + 1 < args.Length: mode = args[++i]; break;
        case "--rounds" when i + 1 < args.Length: rounds = int.Parse(args[++i]); break;
        case "--port" when i + 1 < args.Length: port = int.Parse(args[++i]); break;
        case "--entry-dir" when i + 1 < args.Length: entryDirOverride = args[++i]; break;
        case "--config" when i + 1 < args.Length: config = args[++i]; break;
        case "--environment" when i + 1 < args.Length: environment = args[++i]; break;
        case "--filter" when i + 1 < args.Length: routeFilter = args[++i]; break;
    }
}

// 入口输出目录解析：harness bin → backend → application/JNPF.API.Entry/bin/{config}/net8.0
static string ResolveEntryDir(string? overrideDir, string config)
{
    if (!string.IsNullOrEmpty(overrideDir)) return Path.GetFullPath(overrideDir);
    var backend = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    return Path.Combine(backend, "application", "JNPF.API.Entry", "bin", config, "net8.0");
}

var entryDir = ResolveEntryDir(entryDirOverride, config);
if (!Directory.Exists(entryDir))
{
    Console.Error.WriteLine($"[FATAL] 入口输出目录不存在: {entryDir}（先 dotnet build JNPF.API.Entry）");
    return 1;
}

Console.WriteLine($"JNPF.Startup.Benchmarks mode={mode} rounds={rounds} entryDir={entryDir}");

return mode == "inproc"
    ? RunInProc(entryDir, mainSw)
    : mode == "routes"
        ? RunRoutes(entryDir, routeFilter)
        : RunProcess(entryDir, rounds, port, environment);

// ═══════════════════════════════════════════════════════════════
// 模式一：真实进程冷启动
// ═══════════════════════════════════════════════════════════════
static int RunProcess(string entryDir, int rounds, int port, string environment)
{
    var exe = Path.Combine(entryDir, "JNPF.API.Entry.exe");
    var fallbackExe = Path.Combine(entryDir, "JNPF.API.Entry.dll");
    if (!File.Exists(exe)) exe = null!;

    var results = new List<RoundResult>();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

    for (int r = 1; r <= rounds; r++)
    {
        Console.WriteLine($"--- Round {r}/{rounds} ---");
        var res = new RoundResult();

        var psi = new ProcessStartInfo
        {
            FileName = exe ?? "dotnet",
            WorkingDirectory = entryDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (exe == null) psi.ArgumentList.Add(fallbackExe);
        psi.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = environment;

        string? listeningUrl = null;
        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            var m = Regex.Match(e.Data, @"Now listening on:\s*(http\S+)");
            if (m.Success) listeningUrl = m.Groups[1].Value;
        };
        proc.ErrorDataReceived += (_, e) => { };

        var sw = Stopwatch.StartNew();
        if (!proc.Start())
        {
            Console.Error.WriteLine("[FATAL] 无法启动入口进程");
            return 1;
        }
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // 轮询存活探针，首次 200 的耗时即「冷启动 + 首请求」
        var baseUrl = $"http://127.0.0.1:{port}";
        var coldSw = Stopwatch.StartNew();
        long firstRequestMs = -1;
        bool ready = false;
        while (coldSw.Elapsed < TimeSpan.FromSeconds(180))
        {
            try
            {
                var reqSw = Stopwatch.StartNew();
                using var resp = http.GetAsync($"{baseUrl}/health/live").GetAwaiter().GetResult();
                if ((int)resp.StatusCode == 200)
                {
                    firstRequestMs = reqSw.ElapsedMilliseconds;
                    ready = true;
                    break;
                }
            }
            catch { /* 连接拒绝 = 尚未监听 */ }
            Thread.Sleep(100);
        }

        if (!ready)
        {
            Console.Error.WriteLine($"[WARN] Round {r}: 180s 内未就绪，listeningUrl={listeningUrl}");
            KillTree(proc);
            continue;
        }

        res.ColdStartMs = coldSw.ElapsedMilliseconds;
        res.FirstRequestMs = firstRequestMs;

        // 动态 API 首请求（经路由/鉴权管道，预期 401/600 但证明路由已生成）
        res.DynamicApiFirstMs = TimedGet(http, $"{baseUrl}/api/oauth/CurrentUser");
        // Swagger 文档首次生成（反射成本）
        res.SwaggerFirstMs = TimedGet(http, $"{baseUrl}/swagger/v1/swagger.json");

        // 热请求基线（10 次取均值）
        var warm = new List<long>();
        for (int w = 0; w < 10; w++) warm.Add(TimedGet(http, $"{baseUrl}/health/live"));
        res.WarmAvgMs = (long)warm.Average();

        res.WorkingSetMb = proc.WorkingSet64 / (1024 * 1024);
        res.ListenedUrl = listeningUrl;
        results.Add(res);
        Console.WriteLine($"    cold={res.ColdStartMs}ms firstReq={res.FirstRequestMs}ms dynApi={res.DynamicApiFirstMs}ms swagger={res.SwaggerFirstMs}ms warmAvg={res.WarmAvgMs}ms rss={res.WorkingSetMb}MB url={res.ListenedUrl}");

        KillTree(proc);
        Thread.Sleep(1500); // 等端口释放
    }

    if (results.Count == 0)
    {
        Console.Error.WriteLine("[FATAL] 无有效轮次");
        return 1;
    }

    // 汇总
    Console.WriteLine();
    Console.WriteLine("════════════ 汇总 ════════════");
    Report("冷启动(进程启动→/health/live 200)", results.Select(x => x.ColdStartMs));
    Report("首请求延迟(首次成功 /health/live)", results.Select(x => x.FirstRequestMs));
    Report("动态API首请求(/api/oauth/CurrentUser)", results.Select(x => x.DynamicApiFirstMs));
    Report("Swagger文档首次生成", results.Select(x => x.SwaggerFirstMs));
    Report("热请求均值(/health/live ×10)", results.Select(x => x.WarmAvgMs));
    Report("进程内存RSS(MB)", results.Select(x => x.WorkingSetMb));
    return 0;

    static long TimedGet(HttpClient http, string url)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var resp = http.GetAsync(url).GetAwaiter().GetResult();
            _ = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }
        catch { /* 状态码/异常不影响计时 */ }
        return sw.ElapsedMilliseconds;
    }

    static void KillTree(Process proc)
    {
        try
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
            proc.WaitForExit(10000);
        }
        catch { }
        finally { proc.Dispose(); }
    }

    static void Report(string name, IEnumerable<long> values)
    {
        var list = values.OrderBy(v => v).ToList();
        Console.WriteLine($"{name}: median={list[list.Count / 2]}ms min={list[0]} max={list[^1]} n={list.Count}");
    }
}

// ═══════════════════════════════════════════════════════════════
// 模式二：in-process 服务组合测量（DI 数量 / 模块加载 / 扫描成本）
// 注：harness 以 --mode inproc 运行时，工作目录应设为入口输出目录
//     （配置自 AppContext.BaseDirectory 解析）
// ═══════════════════════════════════════════════════════════════
static int RunInProc(string entryDir, Stopwatch mainSw)
{
    // 1) 先注入配置（必须在触碰任何 JNPF 类型之前：App 静态构造依赖 InternalApp.Configuration）
    InjectConfiguration(entryDir);

    // 2) 程序集扫描成本（App 静态构造：GetAssemblies + EffectiveTypes）
    var scanMs = ForceAppInit(mainSw);
    var assemblyCount = JNPF.App.Assemblies.Count();
    var typeCount = JNPF.App.EffectiveTypes.Count();
    Console.WriteLine($"[扫描] App静态初始化+物化: {scanMs}ms | 程序集={assemblyCount} 类型={typeCount}");

    // 3) 组合服务并分段计时
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = entryDir,
        EnvironmentName = "Development",
    });
    var services = builder.Services;
    services.AddControllers(); // AddDynamicApiControllers 前置依赖（真实链路由 Serve 内部完成）

    int n0 = services.Count;
    var t1 = TimeRun(() => InvokeInternalAddApp(services));
    int n1 = services.Count;
    Console.WriteLine($"[AddApp核心] {t1}ms | 描述符 {n0} → {n1} (+{n1 - n0})  ← 含 AddDependencyInjection 全量反射注册");

    // 逐模块注册（单模块失败不阻断后续，定位失败模块）
    int moduleCount = 0;
    var t2 = TimeRun(() =>
    {
        var moduleTypes = JNPF.App.EffectiveTypes
            .Where(t => typeof(JNPF.Modules.JnpfModule).IsAssignableFrom(t)
                && t.IsClass && !t.IsAbstract && !t.IsGenericType)
            .ToList();
        Console.WriteLine($"[模块] 扫描到 JnpfModule 子类={moduleTypes.Count}");
        moduleCount = moduleTypes.Count;
        foreach (var mt in moduleTypes)
        {
            var before = services.Count;
            var sw = Stopwatch.StartNew();
            try
            {
                var module = (JNPF.Modules.JnpfModule)Activator.CreateInstance(mt)!;
                module.ConfigureServices(services, builder.Configuration);
                Console.WriteLine($"[METRIC] module_ms={mt.Name}:{sw.ElapsedMilliseconds}");
                Console.WriteLine($"    ✓ {mt.Name}: {sw.ElapsedMilliseconds}ms (+{services.Count - before})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[METRIC] module_fail={mt.Name}");
                Console.WriteLine($"    ✗ {mt.Name}: 失败 — {RootMsg(ex)}");
            }
        }
    });
    int n2 = services.Count;
    Console.WriteLine($"[模块注册总计] {t2}ms | 描述符 {n1} → {n2} (+{n2 - n1})");
    Console.WriteLine($"[METRIC] module_registration_total_ms={t2}");
    Console.WriteLine($"[METRIC] module_count={moduleCount}");

    var t3 = TimeRun(() => services.AddInject());
    int n3 = services.Count;
    Console.WriteLine($"[AddInject] {t3}ms | 描述符 {n2} → {n3} (+{n3 - n2})  ← 动态API控制器/Swagger/校验/友好异常");

    // 4) 容器构建成本
    var t4 = TimeRun(() => { using var provider = services.BuildServiceProvider(); });
    Console.WriteLine($"[BuildServiceProvider] {t4}ms | 最终描述符数={services.Count}");

    // ASCII 机器可读指标行（供 PR 门控脚本解析，避免控制台编码问题）
    Console.WriteLine($"[METRIC] descriptor_count={services.Count}");
    Console.WriteLine($"[METRIC] module_count={moduleCount}");
    Console.WriteLine($"[METRIC] app_static_init_ms={scanMs}");

    // 5) 生命周期分布
    Console.WriteLine("[生命周期分布] " + string.Join(" | ",
        services.GroupBy(d => d.Lifetime).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}={g.Count()}")));

    return 0;
}

static string RootMsg(Exception ex)
{
    var innermost = ex;
    while (innermost.InnerException != null) innermost = innermost.InnerException;
    return innermost.Message;
}

// ═══════════════════════════════════════════════════════════
// 模式三：路由契约快照（CR-01 硬门控：拆分前后 ActionDescriptor 逐条比对）
// 原理：完整复现注册链路后从 IActionDescriptorCollectionProvider 枚举
//       真实路由模板，不依赖 Swagger HTTP 端点。
// ═══════════════════════════════════════════════════════════
static int RunRoutes(string entryDir, string filter)
{
    InjectConfiguration(entryDir);
    ForceAppInit(Stopwatch.StartNew());

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = entryDir,
        EnvironmentName = "Development",
    });
    var services = builder.Services;
    services.AddControllers();
    InvokeInternalAddApp(services);

    foreach (var mt in JNPF.App.EffectiveTypes
        .Where(t => typeof(JNPF.Modules.JnpfModule).IsAssignableFrom(t)
            && t.IsClass && !t.IsAbstract && !t.IsGenericType))
    {
        try
        {
            var module = (JNPF.Modules.JnpfModule)Activator.CreateInstance(mt)!;
            module.ConfigureServices(services, builder.Configuration);
        }
        catch { /* 单模块失败不阻断（同 inproc 口径） */ }
    }
    services.AddInject();

    using var provider = services.BuildServiceProvider();
    var adProvider = provider.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>();
    var lines = new List<string>();
    foreach (var ad in adProvider.ActionDescriptors.Items)
    {
        var template = ad.AttributeRouteInfo?.Template;
        if (template == null) continue;
        var cad = ad as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        List<string> verbs = new();
        if (cad?.MethodInfo != null)
        {
            // 反射读 HTTP 动词（方法特性 + ActionConstraint，避免编译期类型解析问题）
            foreach (var attr in cad.MethodInfo.GetCustomAttributes(true))
                CollectHttpMethods(attr, verbs);
        }
        if (verbs.Count == 0 && ad.ActionConstraints != null)
        {
            foreach (var ac in ad.ActionConstraints)
                CollectHttpMethods(ac, verbs);
        }
        var verb = verbs is { Count: > 0 } ? string.Join("|", verbs.Distinct().OrderBy(v => v)) : "ANY";
        var owner = cad != null ? $"{cad.ControllerName}.{cad.ActionName}" : ad.DisplayName ?? "?";
        lines.Add($"[ROUTE] {verb} /{template.ToLowerInvariant()} ({owner})");
    }
    var matched = lines.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).OrderBy(l => l).ToList();
    foreach (var l in matched) Console.WriteLine(l);
    Console.WriteLine($"[METRIC] route_total={lines.Count} route_matched={matched.Count} filter={filter}");
    return 0;
}

static void CollectHttpMethods(object obj, List<string> verbs)
{
    var prop = obj.GetType().GetProperty("HttpMethods");
    if (prop?.GetValue(obj) is System.Collections.IEnumerable methods)
    {
        foreach (var m in methods)
            if (m is string s) verbs.Add(s);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static long ForceAppInit(Stopwatch mainSw)
{
    var sw = Stopwatch.StartNew();
    _ = typeof(JNPF.App);           // 触发静态构造（程序集扫描）
    var dummy = JNPF.App.Assemblies; // 确保物化
    _ = dummy.GetEnumerator();
    return sw.ElapsedMilliseconds;
}

static void InjectConfiguration(string entryDir)
{
    try
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(entryDir)
            .AddJsonFile("appsettings.json", optional: true);
        var configDir = Path.Combine(entryDir, "Configurations");
        if (Directory.Exists(configDir))
        {
            foreach (var f in Directory.GetFiles(configDir, "*.json").OrderBy(f => f))
                configBuilder.AddJsonFile(f, optional: true);
        }
        var configuration = configBuilder.Build();

        // 不能引用 typeof(JNPF.App)（会触发其静态构造），用反射按名找程序集
        var jnpfAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "JNPF")
            ?? Assembly.Load("JNPF");
        var internalApp = jnpfAssembly.GetType("JNPF.InternalApp");
        var field = internalApp?.GetField("Configuration", BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(null, configuration);
            Console.WriteLine($"[配置] InternalApp.Configuration 已注入（{configuration.AsEnumerable().Count()} 个键）");
        }
        else
        {
            Console.WriteLine("[配置][WARN] 未找到 InternalApp.Configuration 字段，模块加载可能失败");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[配置][WARN] 注入失败: {ex.Message}");
    }
}

static long InvokeInternalAddApp(IServiceCollection services)
{
    var method = typeof(Microsoft.Extensions.DependencyInjection.AppServiceCollectionExtensions)
        .GetMethod("AddApp", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("未找到 internal AddApp 方法（vendored 框架结构可能已变化）");

    try
    {
        method.Invoke(null, new object?[] { services, null });
    }
    catch (TargetInvocationException ex)
    {
        Console.WriteLine($"[AddApp][WARN] 内部异常（已捕获，继续）: {RootMsg(ex)}");
    }
    return 0; // 计时由外层 TimeRun 负责
}

static long TimeRun(Action action)
{
    var sw = Stopwatch.StartNew();
    try { action(); }
    catch (Exception ex)
    {
        Console.WriteLine($"    [WARN] 阶段异常（已捕获）: {RootMsg(ex)}");
    }
    return sw.ElapsedMilliseconds;
}

sealed class RoundResult
{
    public long ColdStartMs;
    public long FirstRequestMs;
    public long DynamicApiFirstMs;
    public long SwaggerFirstMs;
    public long WarmAvgMs;
    public long WorkingSetMb;
    public string? ListenedUrl;
}
