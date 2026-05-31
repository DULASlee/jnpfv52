# Logging System Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current FileLogging + Console.WriteLine logging with Serilog structured logging, fix tenant isolation bugs in EventBus subscribers, add TraceId full-chain tracing, and introduce LogPolicy attribute for fine-grained log control.

**Architecture:** Middleware-based TraceId injection + Serilog file sinks with JSON format + EventBus subscriber tenant isolation fix + LogPolicy attribute replacing IgnoreLog. Frontend gets new technical log pages for error/slow-request/trace viewing.

**Tech Stack:** Serilog.AspNetCore 9.0.0 (already installed), SqlSugar, ASP.NET Core Middleware, Vue 3 + Ant Design Vue

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs` | Generate/inject TraceId per request |
| `backend/framework/JNPF/Logging/Attributes/LogPolicyAttribute.cs` | LogPolicy enum + attribute replacing IgnoreLog |
| `backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs` | Serilog configuration |
| `backend/application/JNPF.API.Entry/Infrastructure/LogDiskGuardService.cs` | Disk space monitoring for log directory |
| `backend/modularity/system/JNPF.Systems/System/TechnicalLogService.cs` | API for querying Serilog file logs |
| `jnpf-web-vue3/src/views/extend/errorLog/index.vue` | Error log page |
| `jnpf-web-vue3/src/views/extend/slowRequestLog/index.vue` | Slow request log page |
| `jnpf-web-vue3/src/views/extend/traceLog/index.vue` | TraceId full-chain page |

### Modified Files
| File | Changes |
|------|---------|
| `backend/application/JNPF.API.Entry/Startup.cs` | Register TraceIdMiddleware, replace AddFileLogging with Serilog |
| `backend/application/JNPF.API.Entry/Program.cs` | Add Serilog bootstrap call |
| `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SysLogEntity.cs` | Add F_TRACE_ID, F_TENANT_ID columns |
| `backend/modularity/common/JNPF.Common.Core/EventBus/LogEventSubscriber.cs` | Fix static SqlSugarScope tenant bug |
| `backend/modularity/common/JNPF.Common.Core/EventBus/UserEventSubscriber.cs` | Fix static SqlSugarScope tenant bug |
| `backend/modularity/common/JNPF.Common.Core/EventBus/IntegreateEventSubscriber.cs` | Fix static ISqlSugarClient tenant bug |
| `backend/modularity/common/JNPF.Common.Core/Filter/LogExceptionHandler.cs` | Add TraceId, bypass LogPolicy for exceptions |
| `backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs` | Add LogPolicy, TraceId, summary serialization |
| `backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs` | Replace Console.WriteLine with Serilog |
| `backend/framework/JNPF/Logging/Attributes/IgnoreLogAttribute.cs` | Keep for backward compat, mark obsolete |
| 14 Service files with `[IgnoreLog]` | Replace with `[LogPolicy]` |
| `jnpf-web-vue3/src/views/extend/operationLog/index.vue` | Add TraceId column |
| `backend/application/JNPF.API.Entry/Configurations/appsettings.json` | Add Serilog config section |

---

## Task 1: TraceIdMiddleware

**Files:**
- Create: `backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs`
- Modify: `backend/application/JNPF.API.Entry/Startup.cs:266-316` (Configure method)

- [ ] **Step 1: Create TraceIdMiddleware**

```csharp
// backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs
using System.Diagnostics;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// TraceId middleware - generates unique trace ID per request, injects into response header and context.
/// </summary>
public class TraceIdMiddleware
{
    private const string TraceIdHeader = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Priority: request header (distributed) > Activity.Current > new GUID
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
                      ?? Activity.Current?.Id
                      ?? Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        TraceContext.TraceId = traceId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        // Inject into Serilog LogContext for automatic enrichment
        using (Serilog.LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Async context TraceId propagation (works across Task.Run/ThreadPool).
/// </summary>
public static class TraceContext
{
    private static readonly AsyncLocal<string> _traceId = new();

    public static string TraceId
    {
        get => _traceId.Value ?? "unknown";
        set => _traceId.Value = value;
    }
}
```

- [ ] **Step 2: Register middleware in Startup.cs**

In `Startup.cs` `Configure` method, add **before** `app.UseRouting()` (line 286):

```csharp
// TraceId - must be first middleware
app.UseMiddleware<TraceIdMiddleware>();
```

- [ ] **Step 3: Verify**

Start the project, send any request, check response headers for `X-Trace-Id`.

- [ ] **Step 4: Commit**

```bash
git add backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs backend/application/JNPF.API.Entry/Startup.cs
git commit -m "feat(logging): add TraceIdMiddleware for request tracing"
```

---

## Task 2: DDL Changes - SysLogEntity

**Files:**
- Modify: `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SysLogEntity.cs`

- [ ] **Step 1: Add TraceId and TenantId to SysLogEntity**

Add after the `LoginType` property (line 131):

```csharp
/// <summary>
/// TraceId for full-chain tracing.
/// </summary>
[SugarColumn(ColumnName = "F_TRACE_ID", Length = 64, IsNullable = true)]
public string TraceId { get; set; }

/// <summary>
/// Tenant ID for multi-tenant isolation.
/// </summary>
[SugarColumn(ColumnName = "F_TENANT_ID", Length = 64, IsNullable = true)]
public string TenantId { get; set; }
```

- [ ] **Step 2: Create SQL migration script**

Create file `backend/web/logging_migration.sql`:

```sql
-- Logging System Overhaul: Add TraceId and TenantId to BASE_SYS_LOG
-- Date: 2026-05-31

ALTER TABLE BASE_SYS_LOG ADD F_TRACE_ID NVARCHAR(64) NULL;
ALTER TABLE BASE_SYS_LOG ADD F_TENANT_ID NVARCHAR(64) NULL;

-- Index for TraceId queries
CREATE NONCLUSTERED INDEX IX_SYS_LOG_TRACE_ID ON BASE_SYS_LOG(F_TRACE_ID);

-- Index for tenant + time range queries
CREATE NONCLUSTERED INDEX IX_SYS_LOG_TENANT_TIME ON BASE_SYS_LOG(F_TENANT_ID, F_CREATOR_TIME);
```

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SysLogEntity.cs backend/web/logging_migration.sql
git commit -m "feat(logging): add TraceId and TenantId columns to SysLogEntity"
```

---

## Task 3: LogPolicy Attribute

**Files:**
- Create: `backend/framework/JNPF/Logging/Attributes/LogPolicyAttribute.cs`
- Modify: `backend/framework/JNPF/Logging/Attributes/IgnoreLogAttribute.cs` (mark obsolete)

- [ ] **Step 1: Create LogPolicyAttribute**

```csharp
// backend/framework/JNPF/Logging/Attributes/LogPolicyAttribute.cs
namespace JNPF.Logging.Attributes;

/// <summary>
/// Log recording policy, replaces [IgnoreLog] and default behavior.
/// </summary>
[Flags]
public enum LogPolicy
{
    /// <summary>Record request params, response, operator, elapsed (default).</summary>
    Full = 0,

    /// <summary>Don't record request params (password/token interfaces).</summary>
    IgnoreRequest = 1,

    /// <summary>Don't record response (large data interfaces).</summary>
    IgnoreResponse = 2,

    /// <summary>Neither request params nor response (only operator, time, URL, result code).</summary>
    Minimal = IgnoreRequest | IgnoreResponse,

    /// <summary>Don't record at all (health check, heartbeat).</summary>
    IgnoreAll = 4,

    /// <summary>Force record even under high load (financial/permission ops).</summary>
    Force = 8
}

/// <summary>
/// Mark on Service methods to control operation log recording policy.
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogPolicyAttribute : Attribute
{
    public LogPolicy Policy { get; }

    public LogPolicyAttribute(LogPolicy policy = LogPolicy.Full)
    {
        Policy = policy;
    }
}
```

- [ ] **Step 2: Mark IgnoreLogAttribute as obsolete**

Replace the content of `backend/framework/JNPF/Logging/Attributes/IgnoreLogAttribute.cs`:

```csharp
namespace JNPF.Logging.Attributes;

/// <summary>
/// Ignore log - DEPRECATED, use [LogPolicy(LogPolicy.IgnoreAll)] instead.
/// </summary>
[Obsolete("Use [LogPolicy(LogPolicy.IgnoreAll)] or [LogPolicy(LogPolicy.Minimal)] instead.")]
[SuppressSniffer, AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class IgnoreLogAttribute : Attribute
{
}
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/framework/JNPF/Logging/Attributes/LogPolicyAttribute.cs backend/framework/JNPF/Logging/Attributes/IgnoreLogAttribute.cs
git commit -m "feat(logging): add LogPolicy attribute, deprecate IgnoreLog"
```

---

## Task 4: Serilog Integration

**Files:**
- Create: `backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs`
- Modify: `backend/application/JNPF.API.Entry/Program.cs`
- Modify: `backend/application/JNPF.API.Entry/Startup.cs:243-256` (remove AddFileLogging)

- [ ] **Step 1: Create SerilogBootstrap**

```csharp
// backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Serilog configuration bootstrap.
/// </summary>
public static class SerilogBootstrap
{
    public static void Configure(IConfiguration cfg)
    {
        var logDir = cfg["Logging:File:LogDir"] ?? "logs";

        // JsonFormatter for default (human-readable field names)
        // CompactJsonFormatter for Seq integration
        var fileFormatter = cfg["Logging:Seq:Enabled"] == "true"
            ? new Serilog.Formatting.Compact.CompactJsonFormatter() as ITextFormatter
            : new JsonFormatter(renderMessage: true);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("SqlSugar", LogEventLevel.Warning)
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()

            // Error logs
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "error-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Warning logs (includes slow SQL)
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "warning-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Console (for dev)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")

            // Seq optional
            .WriteTo.Conditional(
                _ => cfg["Logging:Seq:Enabled"] == "true",
                sink => sink.Seq(cfg["Logging:Seq:ServerUrl"] ?? "http://localhost:5341"))

            .CreateLogger();
    }
}
```

- [ ] **Step 2: Modify Program.cs**

Replace the `Load` method in `backend/application/JNPF.API.Entry/Program.cs`:

```csharp
public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
{
    // Configure Serilog
    JNPF.API.Entry.Infrastructure.SerilogBootstrap.Configure(builder.Configuration);
    builder.Host.UseSerilog();

    // Log filter
    builder.Logging.AddFilter((provider, category, logLevel) =>
    {
        return !new[] { "Microsoft.Hosting", "Microsoft.AspNetCore" }.Any(u => category.StartsWith(u))
            && logLevel >= LogLevel.Information;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 52428800;
    });
}
```

- [ ] **Step 3: Remove AddFileLogging from Startup.cs**

Remove lines 243-256 from `Startup.cs`:

```csharp
// DELETE THIS BLOCK:
// 日志写入文件-消息、警告、错误
Array.ForEach(new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error }, logLevel =>
{
    services.AddFileLogging(options =>
    {
        ...
    });
});
```

- [ ] **Step 4: Add Serilog config to appsettings.json**

Add to `backend/application/JNPF.API.Entry/Configurations/appsettings.json`:

```json
{
  "Logging": {
    "File": {
      "LogDir": "logs",
      "RetainedFileCountLimit": 30,
      "FileSizeLimitBytes": 52428800,
      "RollingInterval": "Day"
    },
    "Seq": {
      "Enabled": "false",
      "ServerUrl": "http://localhost:5341"
    }
  }
}
```

- [ ] **Step 5: Verify build and run**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

Start the project, verify log files appear in `logs/` directory.

- [ ] **Step 6: Commit**

```bash
git add backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs backend/application/JNPF.API.Entry/Program.cs backend/application/JNPF.API.Entry/Startup.cs backend/application/JNPF.API.Entry/Configurations/appsettings.json
git commit -m "feat(logging): integrate Serilog, replace AddFileLogging"
```

---

## Task 5: Fix LogEventSubscriber Tenant Bug (P0)

**Files:**
- Modify: `backend/modularity/common/JNPF.Common.Core/EventBus/LogEventSubscriber.cs`

- [ ] **Step 1: Read current implementation**

Read the current file to understand the exact static SqlSugarScope pattern.

- [ ] **Step 2: Fix the tenant isolation bug**

Replace the entire file content:

```csharp
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.Logging.Attributes;
using SqlSugar;
using Microsoft.Extensions.Logging;

namespace JNPF.EventHandler;

/// <summary>
/// Log event subscriber - FIXED: no longer uses static SqlSugarScope.
/// Each call creates an independent SqlSugarClient with correct tenant connection.
/// </summary>
public class LogEventSubscriber : IEventSubscriber, ISingleton
{
    private readonly ITenantManager _tenantManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<LogEventSubscriber> _logger;
    private readonly ISqlSugarClient _sqlSugarClient;

    public LogEventSubscriber(
        ISqlSugarClient sqlSugarClient,
        IUserManager userManager,
        ITenantManager tenantManager,
        ILogger<LogEventSubscriber> logger)
    {
        _sqlSugarClient = sqlSugarClient;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _logger = logger;
    }

    [EventSubscribe("Log:CreateReLog")]
    [EventSubscribe("Log:CreateExLog")]
    [EventSubscribe("Log:CreateVisLog")]
    [EventSubscribe("Log:CreateOpLog")]
    public async Task CreateLog(EventHandlerExecutingContext context)
    {
        var log = (LogEventSource)context.Source;

        try
        {
            if (log.TenantId.IsNotEmptyOrNull())
            {
                await _tenantManager.ChangTenant(_sqlSugarClient, log.TenantId);
            }

            await _sqlSugarClient.CopyNew().Insertable(log.Entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // Log write failure must not affect business, but must be recorded
            _logger.LogError(ex,
                "Operation log write failed, TraceId={TraceId}, TenantId={TenantId}",
                log.Entity.TraceId, log.TenantId);
        }
    }
}
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/modularity/common/JNPF.Common.Core/EventBus/LogEventSubscriber.cs
git commit -m "fix(logging): fix tenant isolation bug in LogEventSubscriber"
```

---

## Task 6: Fix UserEventSubscriber Tenant Bug

**Files:**
- Modify: `backend/modularity/common/JNPF.Common.Core/EventBus/UserEventSubscriber.cs`

- [ ] **Step 1: Apply same fix pattern as Task 5**

Replace the `static SqlSugarScope? _sqlSugarClient` field (line 23) and constructor (lines 30-35):

Change:
```csharp
private static SqlSugarScope? _sqlSugarClient;
```

To:
```csharp
private readonly ISqlSugarClient _sqlSugarClient;
```

Update constructor to use the injected instance directly (remove the static cast).

- [ ] **Step 2: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/common/JNPF.Common.Core/EventBus/UserEventSubscriber.cs
git commit -m "fix(logging): fix tenant isolation bug in UserEventSubscriber"
```

---

## Task 7: Fix IntegreateEventSubscriber Tenant Bug

**Files:**
- Modify: `backend/modularity/common/JNPF.Common.Core/EventBus/IntegreateEventSubscriber.cs`

- [ ] **Step 1: Apply same fix pattern**

Change `private static ISqlSugarClient? _sqlSugarClient` (line 34) to instance field:

```csharp
private ISqlSugarClient _sqlSugarClient;
```

Remove the reassignment `_sqlSugarClient = _sqlSugarClient.CopyNew()` (line 91) — use a local variable instead:

```csharp
var db = _sqlSugarClient.CopyNew();
```

Then use `db` instead of `_sqlSugarClient` for all query/insert operations in the method.

- [ ] **Step 2: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/common/JNPF.Common.Core/EventBus/IntegreateEventSubscriber.cs
git commit -m "fix(logging): fix tenant isolation bug in IntegreateEventSubscriber"
```

---

## Task 8: LogExceptionHandler Enhancement

**Files:**
- Modify: `backend/modularity/common/JNPF.Common.Core/Filter/LogExceptionHandler.cs`

- [ ] **Step 1: Add TraceId and bypass LogPolicy for exceptions**

Replace the `OnExceptionAsync` method:

```csharp
public async Task OnExceptionAsync(ExceptionContext context)
{
    var userContext = App.User;
    var httpContext = context.HttpContext;
    var httpRequest = httpContext?.Request;
    UserAgent userAgent = new UserAgent(httpContext);

    // ★ Core: Exception logs are NOT controlled by [IgnoreLog]/[LogPolicy]
    // Any exception must be recorded regardless of attribute settings
    var traceId = httpContext?.Items["TraceId"]?.ToString() ?? TraceContext.TraceId ?? App.GetTraceId();
    var userId = userContext?.FindFirstValue(ClaimConst.CLAINMUSERID);
    var userName = userContext?.FindFirstValue(ClaimConst.CLAINMREALNAME);
    var userAccount = userContext?.FindFirstValue(ClaimConst.CLAINMACCOUNT);
    var tenantId = userContext?.FindFirstValue(ClaimConst.TENANTID);

    var ipAddress = NetHelper.Ip;
    var ipAddressName = await NetHelper.GetLocation(ipAddress);

    await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateExLog", tenantId, new SysLogEntity
    {
        Id = SnowflakeIdHelper.NextId(),
        UserId = userId,
        UserName = string.Format("{0}/{1}", userName, userAccount),
        Type = 4,
        IPAddress = ipAddress,
        IPAddressName = ipAddressName,
        RequestURL = httpRequest.Path,
        RequestMethod = httpRequest.Method,
        Json = context.Exception.Message + "\n" + context.Exception.StackTrace,
        PlatForm = userAgent.OS.ToString(),
        Browser = userAgent.userAgent.ToString(),
        CreatorTime = DateTime.Now,
        TraceId = traceId,
        TenantId = tenantId
    }));
}
```

- [ ] **Step 2: Add using for TraceContext**

Add at top of file:

```csharp
using JNPF.API.Entry.Infrastructure;
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/modularity/common/JNPF.Common.Core/Filter/LogExceptionHandler.cs
git commit -m "feat(logging): add TraceId to exception logs, bypass LogPolicy"
```

---

## Task 9: RequestActionFilter Enhancement

**Files:**
- Modify: `backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs`

- [ ] **Step 1: Add LogPolicy, TraceId, and summary serialization**

Replace the entire `OnActionExecutionAsync` method:

```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
    var userContext = App.User;
    var httpContext = context.HttpContext;
    var httpRequest = httpContext.Request;
    UserAgent userAgent = new UserAgent(httpContext);

    // Read LogPolicy from action metadata
    var policy = LogPolicy.Full;
    var policyAttr = context.ActionDescriptor.EndpointMetadata
        .OfType<LogPolicyAttribute>().FirstOrDefault();
    if (policyAttr != null)
    {
        policy = policyAttr.Policy;
    }

    // Check IgnoreAll
    if (policy.HasFlag(LogPolicy.IgnoreAll))
    {
        await next();
        return;
    }

    // Also support legacy [IgnoreLog] during transition
    if (context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(IgnoreLogAttribute)))
    {
        await next();
        return;
    }

    Stopwatch sw = new Stopwatch();
    sw.Start();
    var actionContext = await next();
    sw.Stop();

    var traceId = httpContext.Items["TraceId"]?.ToString() ?? TraceContext.TraceId ?? App.GetTraceId();
    var userId = userContext?.FindFirstValue(ClaimConst.CLAINMUSERID);
    var userName = userContext?.FindFirstValue(ClaimConst.CLAINMREALNAME);
    var userAccount = userContext?.FindFirstValue(ClaimConst.CLAINMACCOUNT);
    var tenantId = userContext?.FindFirstValue(ClaimConst.TENANTID);

    var ipAddress = NetHelper.Ip;
    var ipAddressName = await NetHelper.GetLocation(ipAddress);

    // Summary mode serialization (avoid large object serialization)
    string args = null;
    if (!policy.HasFlag(LogPolicy.IgnoreRequest))
    {
        args = SerializeAsSummary(context.ActionArguments);
    }

    string resultJson = null;
    if (!policy.HasFlag(LogPolicy.IgnoreResponse))
    {
        var result = (actionContext.Result as JsonResult)?.Value;
        resultJson = result?.ToJsonString();
        if (resultJson != null && resultJson.Length > 500)
        {
            resultJson = resultJson[..500] + $"...(truncated, total {resultJson.Length} chars)";
        }
    }

    try
    {
        await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateReLog", tenantId, new SysLogEntity
        {
            Id = SnowflakeIdHelper.NextId(),
            UserId = userId,
            UserName = string.Format("{0}/{1}", userName, userAccount),
            Type = 5,
            IPAddress = ipAddress,
            IPAddressName = ipAddressName,
            RequestURL = httpRequest.Path,
            RequestDuration = (int)sw.ElapsedMilliseconds,
            RequestMethod = httpRequest.Method,
            PlatForm = userAgent.OS.ToString(),
            Browser = userAgent.userAgent.ToString(),
            CreatorTime = DateTime.Now,
            RequestParam = args,
            RequestTarget = context.ActionDescriptor.DisplayName,
            Json = resultJson,
            TraceId = traceId,
            TenantId = tenantId
        }));

        if (context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(OperateLogAttribute)))
        {
            var module = context.ActionDescriptor.EndpointMetadata
                .Where(x => x.GetType() == typeof(OperateLogAttribute))
                .FirstOrDefault() as OperateLogAttribute;

            await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateOpLog", tenantId, new SysLogEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                UserId = userId,
                UserName = string.Format("{0}/{1}", userName, userAccount),
                Type = 3,
                IPAddress = ipAddress,
                IPAddressName = ipAddressName,
                RequestURL = httpRequest.Path,
                RequestDuration = (int)sw.ElapsedMilliseconds,
                RequestMethod = httpRequest.Method,
                PlatForm = userAgent.OS.ToString(),
                Browser = userAgent.userAgent.ToString(),
                CreatorTime = DateTime.Now,
                ModuleName = module.ModuleName,
                RequestParam = args,
                RequestTarget = context.ActionDescriptor.DisplayName,
                Json = resultJson,
                TraceId = traceId,
                TenantId = tenantId
            }));
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, ex.Message);
    }
}

private string SerializeAsSummary(IDictionary<string, object> args)
{
    if (args == null || args.Count == 0) return null;

    var summaries = args.Select(kv =>
    {
        var value = kv.Value;
        if (value == null) return $"{kv.Key}=null";

        var json = value.ToJsonString();
        if (json.Length > 200)
        {
            return $"{kv.Key}={json[..200]}...(truncated, total {json.Length} chars)";
        }
        return $"{kv.Key}={json}";
    });

    return "{" + string.Join(", ", summaries) + "}";
}
```

- [ ] **Step 2: Add using statements**

Add at top of file:

```csharp
using JNPF.API.Entry.Infrastructure;
using JNPF.Logging.Attributes;
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs
git commit -m "feat(logging): add LogPolicy, TraceId, summary serialization to RequestActionFilter"
```

---

## Task 10: Replace Console.WriteLine in SqlSugar AOP

**Files:**
- Modify: `backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs:86-116`

- [ ] **Step 1: Replace Console.WriteLine with Serilog**

Replace the `SetDbAop` method:

```csharp
public static void SetDbAop(SqlSugarScopeProvider db)
{
    var config = db.CurrentConnectionConfig;

    db.Ado.CommandTimeOut = 30;

    db.Aop.OnLogExecuting = (sql, pars) =>
    {
        // Keep MiniProfiler integration
        App.PrintToMiniProfiler("SqlSugar", "Info", sql + "\r\n" + db.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
    };

    db.Aop.OnLogExecuted = (sql, pars) =>
    {
        var elapsed = db.Aop.DiffTime.TotalMilliseconds;

        if (elapsed > 1000) // Slow query threshold: 1 second
        {
            Serilog.Log.ForContext("TraceId", TraceContext.TraceId)
               .ForContext("Sql", sql)
               .ForContext("Elapsed", elapsed)
               .Warning("Slow SQL ({Elapsed}ms): {Sql}", elapsed, sql);
        }

        // Dev: log all SQL at Debug level
        Serilog.Log.ForContext("TraceId", TraceContext.TraceId)
           .Debug("SQL ({Elapsed}ms): {Sql}", elapsed, sql);
    };

    db.Aop.OnError = ex =>
    {
        if (ex.Parametres == null) return;
        var pars = db.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));

        Serilog.Log.ForContext("TraceId", TraceContext.TraceId)
           .ForContext("Sql", ex.Sql)
           .Error(ex, "SQL Error: {Sql}", ex.Sql);

        App.PrintToMiniProfiler("SqlSugar", "Error", $"{ex.Message}{Environment.NewLine}{ex.Sql}{pars}{Environment.NewLine}");
    };
}
```

- [ ] **Step 2: Add using**

Add at top of file:

```csharp
using JNPF.API.Entry.Infrastructure;
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs
git commit -m "feat(logging): replace Console.WriteLine with Serilog in SqlSugar AOP"
```

---

## Task 11: [IgnoreLog] to [LogPolicy] Bulk Replacement

**Files:**
- 14 Service files with 54 `[IgnoreLog]` occurrences

- [ ] **Step 1: Replace all [IgnoreLog] with [LogPolicy]**

For each file, apply the replacement rule:
- `[IgnoreLog]` on password/token interfaces → `[LogPolicy(LogPolicy.Minimal)]`
- `[IgnoreLog]` on health check/polling → `[LogPolicy(LogPolicy.IgnoreAll)]`
- `[IgnoreLog]` on large data returns → `[LogPolicy(LogPolicy.IgnoreResponse)]`

Files to modify:
1. `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs` (14 occurrences)
2. `backend/modularity/visualdev/JNPF.VisualDev/VisualdevShortLinkService.cs` (7)
3. `backend/modularity/system/JNPF.Systems/Common/TenantService.cs` (7)
4. `backend/modularity/system/JNPF.Systems/Common/FileService.cs` (5)
5. `backend/modularity/system/JNPF.Systems/System/LocationService.cs` (5)
6. `backend/modularity/system/JNPF.Systems/System/DataInterfaceService.cs` (4)
7. `backend/modularity/inteAssistant/JNPF.InteAssistant/WebHookService.cs` (3)
8. `backend/modularity/message/JNPF.Message/Service/WechatOpenService.cs` (2)
9. `backend/modularity/system/JNPF.Systems/Permission/SocialsUserService.cs` (2)
10. `backend/modularity/message/JNPF.Message/Service/WechatMiniProgramService.cs` (1)
11. `backend/modularity/message/JNPF.Message/Service/ShortLinkService.cs` (1)
12. `backend/modularity/extend/JNPF.Extend/DocumentPreview.cs` (1)
13. `backend/modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs` (1)
14. `backend/modularity/system/JNPF.Systems/System/SysLogService.cs` (1)

- [ ] **Step 2: Verify no [IgnoreLog] remains**

```bash
grep -rn "\[IgnoreLog\]" backend/ --include="*.cs" | grep -v "Obsolete"
```

Expected: 0 results.

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/
git commit -m "refactor(logging): replace 54 [IgnoreLog] with [LogPolicy] across 14 files"
```

---

## Task 12: LogDiskGuardService

**Files:**
- Create: `backend/application/JNPF.API.Entry/Infrastructure/LogDiskGuardService.cs`
- Modify: `backend/application/JNPF.API.Entry/Startup.cs` (register hosted service)

- [ ] **Step 1: Create LogDiskGuardService**

```csharp
// backend/application/JNPF.API.Entry/Infrastructure/LogDiskGuardService.cs
namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// Background service monitoring log directory disk space.
/// </summary>
public class LogDiskGuardService : BackgroundService
{
    private readonly ILogger<LogDiskGuardService> _logger;
    private readonly IConfiguration _cfg;

    private const long WarningThresholdBytes = 5L * 1024 * 1024 * 1024;   // 5GB
    private const long CriticalThresholdBytes = 1L * 1024 * 1024 * 1024;  // 1GB

    public static bool IsDiskCritical { get; private set; }

    public LogDiskGuardService(ILogger<LogDiskGuardService> logger, IConfiguration cfg)
    {
        _logger = logger;
        _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
                var fullPath = Path.GetFullPath(logDir);
                var driveInfo = new DriveInfo(Path.GetPathRoot(fullPath));
                var freeBytes = driveInfo.AvailableFreeSpace;

                if (freeBytes < CriticalThresholdBytes)
                {
                    IsDiskCritical = true;
                    _logger.LogCritical(
                        "LOG_DISK_CRITICAL | Free space {FreeMB}MB | Log writing paused | Clean disk immediately",
                        freeBytes / 1024 / 1024);
                }
                else if (freeBytes < WarningThresholdBytes)
                {
                    IsDiskCritical = false;
                    _logger.LogWarning(
                        "LOG_DISK_WARNING | Free space {FreeMB}MB | Please clean log files",
                        freeBytes / 1024 / 1024);
                }
                else
                {
                    IsDiskCritical = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk space check exception");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

- [ ] **Step 2: Register in Startup.cs**

Add in `ConfigureServices` method:

```csharp
services.AddHostedService<LogDiskGuardService>();
```

- [ ] **Step 3: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add backend/application/JNPF.API.Entry/Infrastructure/LogDiskGuardService.cs backend/application/JNPF.API.Entry/Startup.cs
git commit -m "feat(logging): add LogDiskGuardService for disk space monitoring"
```

---

## Task 13: TechnicalLogService Backend API

**Files:**
- Create: `backend/modularity/system/JNPF.Systems/System/TechnicalLogService.cs`

- [ ] **Step 1: Create TechnicalLogService**

This service reads Serilog JSON log files and exposes APIs for the frontend. Follow the pattern from `SysLogService.cs` (IDynamicApiController).

```csharp
// backend/modularity/system/JNPF.Systems/System/TechnicalLogService.cs
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Logging.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JNPF.Systems;

/// <summary>
/// Technical log query service - reads Serilog JSON files.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "TechnicalLog", Order = 212)]
[Route("api/system/[controller]")]
public class TechnicalLogService : IDynamicApiController, ITransient
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<TechnicalLogService> _logger;

    public TechnicalLogService(IConfiguration cfg, ILogger<TechnicalLogService> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    [IgnoreLog]
    [HttpGet("errors")]
    public async Task<PagedResult<TechLogEntry>> GetErrorsAsync(
        [FromQuery] DateTime? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string level = "Error")
    {
        var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
        var dateStr = date?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd");
        var filePath = Path.Combine(logDir, $"error-{dateStr}.json");

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(logDir, $"warning-{dateStr}.json");
            if (!File.Exists(filePath))
                return new PagedResult<TechLogEntry> { Items = new List<TechLogEntry>(), Total = 0 };
        }

        var entries = await ReadLogFileAsync(filePath, level: level);
        var total = entries.Count;
        var paged = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<TechLogEntry> { Items = paged, Total = total };
    }

    [IgnoreLog]
    [HttpGet("trace/{traceId}")]
    public async Task<TraceAggregateResult> GetTraceAsync(string traceId)
    {
        var result = new TraceAggregateResult { TraceId = traceId };

        var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
        var today = DateTime.Now.ToString("yyyyMMdd");
        var errorFile = Path.Combine(logDir, $"error-{today}.json");
        var warningFile = Path.Combine(logDir, $"warning-{today}.json");

        var fileLogs = new List<TechLogEntry>();
        if (File.Exists(errorFile))
            fileLogs.AddRange(await ReadLogFileAsync(errorFile, traceId: traceId));
        if (File.Exists(warningFile))
            fileLogs.AddRange(await ReadLogFileAsync(warningFile, traceId: traceId));

        result.FileLogs = fileLogs.OrderBy(x => x.Timestamp).ToList();
        return result;
    }

    [IgnoreLog]
    [HttpGet("slow-requests")]
    public async Task<List<TechLogEntry>> GetSlowRequestsAsync(
        [FromQuery] DateTime? date, [FromQuery] int threshold = 1000)
    {
        var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
        var dateStr = date?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd");
        var filePath = Path.Combine(logDir, $"warning-{dateStr}.json");

        if (!File.Exists(filePath))
            return new List<TechLogEntry>();

        var entries = await ReadLogFileAsync(filePath);
        return entries.Where(e => e.Message != null && e.Message.Contains("Slow SQL")).ToList();
    }

    private async Task<List<TechLogEntry>> ReadLogFileAsync(
        string filePath, string level = null, string traceId = null)
    {
        var entries = new List<TechLogEntry>();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize<TechLogEntry>(line);
                if (entry == null) continue;

                if (!string.IsNullOrEmpty(level) && !string.Equals(entry.Level, level, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(traceId) && entry.TraceId != traceId)
                    continue;

                entries.Add(entry);
            }
            catch (JsonException)
            {
                // Last line may be incomplete (being written), skip
                _logger.LogDebug("Skipped malformed log line in {File}", filePath);
                continue;
            }
        }

        return entries;
    }
}

public class TechLogEntry
{
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("Level")]
    public string Level { get; set; }

    [JsonPropertyName("MessageTemplate")]
    public string MessageTemplate { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; }

    [JsonPropertyName("Exception")]
    public string Exception { get; set; }

    [JsonPropertyName("TraceId")]
    public string TraceId { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int Total { get; set; }
}

public class TraceAggregateResult
{
    public string TraceId { get; set; }
    public List<TechLogEntry> FileLogs { get; set; } = new();
}
```

- [ ] **Step 2: Verify build**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/system/JNPF.Systems/System/TechnicalLogService.cs
git commit -m "feat(logging): add TechnicalLogService for Serilog file log queries"
```

---

## Task 14: Frontend - Add TraceId Column to Operation Log

**Files:**
- Modify: `jnpf-web-vue3/src/views/extend/operationLog/index.vue`

- [ ] **Step 1: Add TraceId column to table**

Add after the existing columns (around line 52):

```typescript
{ title: 'TraceId', dataIndex: 'traceId', width: 200, ellipsis: true },
```

- [ ] **Step 2: Add API for technical log queries**

Create API file: `jnpf-web-vue3/src/api/system/technicalLog.ts`

```typescript
import { defHttp } from '/@/utils/http/axios';

enum Api {
  Errors = '/api/system/technicalLog/errors',
  Trace = '/api/system/technicalLog/trace',
  SlowRequests = '/api/system/technicalLog/slow-requests',
}

export const getTechnicalLogErrors = (params) => defHttp.get({ url: Api.Errors, params });
export const getTraceLog = (traceId: string) => defHttp.get({ url: `${Api.Trace}/${traceId}` });
export const getSlowRequests = (params) => defHttp.get({ url: Api.SlowRequests, params });
```

- [ ] **Step 3: Verify frontend builds**

```bash
cd d:\JNPF-v52\jnpf-web-vue3 && pnpm run build
```

- [ ] **Step 4: Commit**

```bash
git add jnpf-web-vue3/src/views/extend/operationLog/index.vue jnpf-web-vue3/src/api/system/technicalLog.ts
git commit -m "feat(logging): add TraceId column to operation log, add technical log API"
```

---

## Task 15: Frontend - Error Log Page

**Files:**
- Create: `jnpf-web-vue3/src/views/extend/errorLog/index.vue`

- [ ] **Step 1: Create error log page**

Follow the pattern from `jnpf-web-vue3/src/views/extend/operationLog/index.vue` using BasicTable + jnpf-content-wrapper.

```vue
<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'level'">
              <a-tag :color="record.level === 'Error' ? 'error' : 'warning'">
                {{ record.level }}
              </a-tag>
            </template>
            <template v-if="column.key === 'traceId'">
              <a @click="handleViewTrace(record.traceId)">{{ record.traceId }}</a>
            </template>
          </template>
        </BasicTable>
      </div>
    </div>
  </div>
</template>
<script lang="ts" setup>
  import { BasicTable, useTable, BasicColumn } from '/@/components/Table';
  import { getTechnicalLogErrors } from '/@/api/system/technicalLog';
  import { useI18n } from '/@/hooks/web/useI18n';
  import dayjs from 'dayjs';

  defineOptions({ name: 'extend-errorLog' });

  const { t } = useI18n();

  const columns: BasicColumn[] = [
    { title: 'Time', dataIndex: 'timestamp', width: 180, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: 'Level', dataIndex: 'level', width: 100 },
    { title: 'Message', dataIndex: 'message', ellipsis: true },
    { title: 'TraceId', dataIndex: 'traceId', width: 200, ellipsis: true },
  ];

  const [registerTable] = useTable({
    api: getTechnicalLogErrors,
    columns,
    useSearchForm: true,
    formConfig: {
      schemas: [
        { field: 'date', label: 'Date', component: 'DatePicker', componentProps: { valueFormat: 'YYYY-MM-DD' } },
        { field: 'level', label: 'Level', component: 'Select', componentProps: { options: [{ label: 'Error', value: 'Error' }, { label: 'Warning', value: 'Warning' }] } },
      ],
    },
    pagination: true,
    pageSize: 50,
  });

  function handleViewTrace(traceId: string) {
    // Navigate to trace page - implement with router
  }
</script>
```

- [ ] **Step 2: Register route**

The route is dynamically injected from backend menu config, so no frontend route file needs editing. Ensure the menu entry points to `extend/errorLog`.

- [ ] **Step 3: Commit**

```bash
git add jnpf-web-vue3/src/views/extend/errorLog/index.vue
git commit -m "feat(logging): add error log page"
```

---

## Task 16: Verification

- [ ] **Step 1: Build backend**

```bash
cd d:\JNPF-v52\backend && dotnet build
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Build frontend**

```bash
cd d:\JNPF-v52\jnpf-web-vue3 && pnpm run build
```

Expected: Build succeeds.

- [ ] **Step 3: Run migration SQL**

Execute `backend/web/logging_migration.sql` against the development database.

- [ ] **Step 4: Start and test**

```bash
cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
```

Test checklist:
- [ ] T1: Any API response contains `X-Trace-Id` header
- [ ] T2: `logs/` directory contains `error-*.json` and `warning-*.json` files
- [ ] T3: Log entries contain `TraceId` field
- [ ] T4: Slow SQL (>1s) appears in `warning-*.json`
- [ ] T5: Build `backend/` has 0 `[IgnoreLog]` references (except the obsolete attribute definition)

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "chore(logging): logging system overhaul complete"
```
