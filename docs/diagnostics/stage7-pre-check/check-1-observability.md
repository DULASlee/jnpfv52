# Check 1: Existing Observability Infrastructure

## 1.1 Performance Monitoring Tools

| Tool | File Path | Version/Details | Notes |
|---|---|---|---|
| MiniProfiler.AspNetCore.Mvc | `backend/framework/JNPF/JNPF.csproj` (line 29) | 4.3.8 | Active; controlled via `App.Settings.InjectMiniProfiler` bool flag. Registered in `SpecificationDocumentServiceCollectionExtensions.AddMiniProfiler()`. Used in UoW, validation, exception filters, SQL logging. |
| Microsoft.ApplicationInsights | — | Not found | — |
| SkyWalking / ElasticApm / Prometheus / Grafana | — | Not found as integrations. `Prometheus` appears only in a comment in `Services/LogHealthCheckService.cs` | No actual Prometheus client library or metrics export endpoint exists. |
| Serilog Enrichers | `Infrastructure/SerilogBootstrap.cs:32`, `SerilogHostingExtensions.cs:38,72` | `.Enrich.FromLogContext()` only | TraceIdMiddleware pushes `TraceId`, `UserId`, `TenantId` into `LogContext` per-request. |

## 1.2 TraceId / Activity Usage

| Usage Pattern | File | Line | Description |
|---|---|---|---|
| Custom TraceIdMiddleware (X-Trace-Id) | `Infrastructure/TraceIdMiddleware.cs` | 10-45 | Priority: request header X-Trace-Id > `Activity.Current?.Id` > `Guid.NewGuid()`. Sets `HttpContext.Items["TraceId"]` and `TraceContext.TraceId` (AsyncLocal). |
| TraceContext (AsyncLocal) | `Infrastructure/TraceIdMiddleware.cs` | 51-59 | Static `AsyncLocal<string>` for cross-thread propagation. |
| Middleware registration | `Modules/AuthenticationModule.cs` | 82 | `app.UseMiddleware<Infrastructure.TraceIdMiddleware>();` |
| Activity.Current?.Id fallback | `Extensions/SqlSugarConfigureExtensions.cs` | 174 | `TraceId = Activity.Current?.Id` in SQL AOP callback. |
| App.GetTraceId() | `framework/JNPF/App/App.cs` | 284-286 | `return Activity.Current?.Id ?? HttpContext?.TraceIdentifier;` |
| TraceId in log output | `framework/JNPF/Logging/LoggerFormatter.cs` | 58-59 | JSON log formatter writes `"traceId": logMsg.TraceId`. |
| TraceId in SysLogEntity | `modularity/system/.../SysLogEntity.cs` | 137 | `public string TraceId { get; set; }` |
| TraceId in DiffLog | `framework/.../DiffLog/DiffLogData.cs` | 24 | `public string TraceId { get; set; }` |
| TraceId in RequestActionFilter | `modularity/common/.../RequestActionFilter.cs` | 82 | Reads from `httpContext.Items["TraceId"]`. |
| TraceId in LogExceptionHandler | `modularity/common/.../LogExceptionHandler.cs` | 42 | Reads from `httpContext?.Items["TraceId"]`. |
| TraceId in OAuthService | `modularity/oauth/.../OAuthService.cs` | 1402 | Reads from `App.HttpContext?.Items["TraceId"]`. |

### Conflict Risk with OpenTelemetry

**Low risk.** The existing TraceIdMiddleware already accounts for `Activity.Current?.Id` as a fallback source (priority #2), which is exactly what OpenTelemetry sets. When OTel auto-instrumentation is added:
- ASP.NET Core instrumentation will create an `Activity` (span) automatically at request start.
- The middleware's priority #1 (incoming `X-Trace-Id` header) would override with the caller's ID — correct distributed tracing behavior.
- If no incoming header exists, the middleware will use `Activity.Current?.Id` (the OTel-generated span ID), also correct.

**Recommendation:** No conflict remediation needed. For full OTLP export, augment the middleware to propagate OTel `Baggage` and set `Activity.Current?.AddTag("traceId", traceId)`.

## 1.3 Logging Configuration

- **Output format:** JSON (via `JsonFormatter(renderMessage: true)`) for file sinks; Console uses text template.
- **Enrichers:** `FromLogContext()` only (no custom enrichers).
- **Sinks:**
  - File (JSON, error level, 30-day retention, 50MB per file)
  - File (JSON, warning level, 14-day retention, 50MB per file)
  - Console (text, `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`)
  - Seq (conditional, disabled by default via `Logging:Seq:Enabled: false`)
- **Special config:**
  - `LoggingLevelSwitch` (starts at Information, dynamically adjustable)
  - `SelfLog.Enable(Console.Error)` for sink failure diagnostics
  - MinLevel overrides: Microsoft=Warning, System=Warning, SqlSugar=Warning
  - Non-Entry projects use fallback text format via `UseSerilogDefault()`
  - TraceId, UserId, TenantId pushed to LogContext per-request
  - Seq server URL: `http://localhost:5341` (config placeholder)

### OTLP Integration Point

**Yes, `Serilog.Sinks.OpenTelemetry` can bridge logs to OTLP.** Current logging is well-positioned:
- Serilog is the primary logging provider (v8.0.3).
- `LogContext` push properties already flow per-request.
- `LoggingLevelSwitch` pattern works with any sink.
- Extend `SerilogBootstrap.Configure()` with `WriteTo.OpenTelemetry()` as a conditional sink.

## Summary for Stage 7.1

1. **MiniProfiler** is the only existing APM-like tool; dev-oriented, not suitable for production. Zero OTLP output.
2. **No production APM** (AppInsights, SkyWalking, Prometheus) is integrated.
3. **TraceId infrastructure is solid:** TraceIdMiddleware + TraceContext + Serilog LogContext. Already uses `Activity.Current?.Id` as fallback (OTel-compatible).
4. **Recommendation:** Add OpenTelemetry SDK with ASP.NET Core + HttpClient instrumentation. Add `Serilog.Sinks.OpenTelemetry` for log-to-OTLP bridging. Retain existing TraceIdMiddleware (complements OTel). Disable MiniProfiler in production (already done via config).
