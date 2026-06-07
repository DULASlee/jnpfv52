# Check 6: Existing Analyzer and Code Quality Tools

## 6.1 Existing Analyzer References

| Analyzer | Version | Reference Type | Scope |
|---|---|---|---|
| Roslynator.Analyzers | 4.3.0 | Directory.Build.props (CI_BUILD only) | All backend (CI_BUILD=true) |
| StyleCop.Analyzers | 1.2.0-beta.406 | Directory.Build.props (CI_BUILD only) | All backend (CI_BUILD=true) |
| StyleCop.Analyzers | 1.1.118 | JNPF.Common.csproj (always) | `modularity/common/JNPF.Common/JNPF.Common.csproj:44` |
| Microsoft NETAnalyzers | latest-recommended | Directory.Build.props | All backend (always enabled) |
| SonarAnalyzer.CSharp | — | Not found | — |
| ErrorProne.NET | — | Not found | — |

**Additional quality infrastructure:**
- `backend/dotnet.ruleset` — ~80+ Roslynator/StyleCop rules suppressed (Silent/None), CA* quality rules at Warning.
- `backend/stylecop.json` — systemUsingDirectivesFirst=true, usingDirectivesPlacement=outsideNamespace.
- `backend/Directory.Build.props` — `TreatWarningsAsErrors=false`, `CodeAnalysisTreatWarningsAsErrors=false`, nullable warnings suppressed locally (CS8600-CS8625), StyleCop/Roslynator disabled locally.

## 6.2 .editorconfig

- **Files found:**
  - `D:/JNPF-v52/.editorconfig` — root, frontend config (163 lines)
  - `D:/JNPF-v52/backend/.editorconfig` — backend root (302 lines)
  - `D:/JNPF-v52/backend/framework/.editorconfig` — legacy Furion (284 lines)
- **Total rules:** ~140+ across all files
- **DiagnosticSuppress:** Absent — no `dotnet_diagnostic.*` severity suppression for JNPF-specific rules.
- **generated_code markers:** Absent.
- **Notable:** `indent_style=space`, `indent_size=4` (C#), 2 (frontend); Allman braces; `csharp_prefer_braces=false:suggestion`; `csharp_style_var_elsewhere=true:suggestion`.

## 6.3 Forbidden Pattern Inventory

### JNPF001 (App.GetService / App.GetRequiredService) — 37 occurrences

**App.GetService<T> (29 occurrences):**

| File | Line | Service Type |
|---|---|---|
| `application/.../SqlSugarConfigureExtensions.cs` | 155 | IHttpContextAccessor |
| `application/.../SqlSugarConfigureExtensions.cs` | 173 | IUserManager |
| `application/.../SqlSugarConfigureExtensions.cs` | 214 | IHttpContextAccessor |
| `application/.../SqlSugarConfigureExtensions.cs` | 226 | IHttpContextAccessor |
| `application/.../JwtHandler.cs` | 73 | IUserManager |
| `application/.../JwtHandler.cs` | 115 | ICacheManager |
| `application/.../JwtHandler.cs` | 124 | IUserManager |
| `framework/JNPF/VirtualFileServer/FS.cs` | 44 | Func<...> |
| `framework/JNPF/JsonSerialization/JSON.cs` | 17 | IJsonSerializerProvider |
| `framework/.../SqlSugarDbContextProvider.cs` | 68 | IHttpContextAccessor |
| `framework/.../SqlSugarDbContextProvider.cs` | 118 | ICacheManager |
| `framework/JNPF/ViewEngine/.../ViewEnginePartMethods.cs` | 77 | IViewEngine |
| `framework/.../ConnectionStringsOptions.cs` (JNPF) | 75 | IHttpContextAccessor |
| `framework/.../ConnectionStringsOptions.cs` (SqlSugar) | 64 | IHttpContextAccessor |
| `framework/JNPF/InstantMessaging/IM.cs` | 19 | IHubContext<THub> |
| `framework/JNPF/InstantMessaging/IM.cs` | 33 | IHubContext<THub, T> |
| `framework/JNPF/FriendlyException/Oops.cs` | 251 | IErrorCodeTypeProvider |
| `framework/JNPF/RemoteRequest/.../HttpRequestPartMethods.cs` | 542 | IHttpClientFactory |
| `framework/JNPF/RemoteRequest/Http.cs` | 17 | THttpDispatchProxy |
| `framework/JNPF/DistributedIDGenerator/IDGen.cs` | 17 | IDistributedIDGenerator |
| `framework/JNPF/Localization/L.cs` | 22 | IStringLocalizerFactory |
| `framework/JNPF/Localization/L.cs` | 27 | IHtmlLocalizerFactory |
| `framework/JNPF/Localization/L.cs` | 36 | IStringLocalizer<T> |
| `framework/JNPF/Localization/L.cs` | 46 | IHtmlLocalizer<T> |
| `framework/JNPF/DataValidation/.../DataValidator.cs` | 250 | IValidationMessageTypeProvider |
| `modularity/engine/.../FormDataParsing.cs` | 1879 | IRunService |
| `framework/JNPF/App/.../IConfigurationExtenstions.cs` | 19 | IConfiguration |
| `modularity/common/.../ControlParsing.cs` | 262 | IRunService |
| `modularity/system/.../RoleService.cs` | 886 | OnlineUserService |

**App.GetRequiredService<T> (8 occurrences):**

| File | Line | Service Type |
|---|---|---|
| `framework/JNPF/TaskQueue/TaskQueued.cs` | 20,33,47,61 | ITaskQueue |
| `framework/JNPF/EventBus/MessageCenter.cs` | 117 | IEventPublisher |
| `framework/JNPF/EventBus/MessageCenter.cs` | 126 | IEventBusFactory |
| `framework/JNPF/Schedule/Schedular.cs` | 69 | ISchedulerFactory |
| `framework/JNPF/Logging/Log.cs` | 26 | ILogger<T> |

### JNPF002 (Aop.DataExecuting = assignment) — 10 occurrences (4 production + 6 test)

| File | Line | Note |
|---|---|---|
| `application/.../SqlSugarConfigureExtensions.cs` | 197 | PROD — ADR-002 "=" override pattern |
| `modularity/common/.../TenantManager.cs` | 85 | PROD |
| `modularity/common/.../DataBaseManager.cs` | 170 | PROD |
| `modularity/common/.../DataBaseManager.cs` | 202 | PROD |
| `tests/.../SqlSugarVerification/Program.cs` | 100,105,126,154,168,196 | TEST |

**Note:** All 4 production occurrences follow ADR-002 intentionally. No `+=` patterns found. Analyzer should flag as Warning, not Error.

### JNPF003 (CreateScope) — 24 occurrences (23 production + 1 test)

Key production files:
- `framework/JNPF/App/App.cs:110`, `framework/JNPF/App/Native.cs:51`
- `framework/JNPF/DependencyInjection/Scoped.cs:77`
- `framework/JNPF/Logging/.../DatabaseLoggerProvider.cs:145`
- `framework/JNPF/Schedule/.../ScheduleHostedService.cs:213`
- `infrastructure/.../EventOutboxDispatcher.cs:83`
- `modularity/common/.../IntegreateEventSubscriber.cs:66`
- `modularity/common/.../DbJobPersistence.cs:27`
- `modularity/taskscheduler/.../SpareTimeDemo.cs:37`
- `modularity/taskscheduler/.../ScheduleJob.cs:38`
- `modularity/taskscheduler/.../OnlineUserJob.cs:25`
- `modularity/inteAssistant/.../IntegrateTiming.cs:56`
- `modularity/inteAssistant/.../InteAssistantWayEventSubscriber.cs:59`
- `modularity/inteAssistant/.../InteAssistantRun.cs:54`
- `modularity/inteAssistant/.../InteAssistantProgramStartupJob.cs:22`
- `modularity/inteAssistant/.../ExecutionQueue.cs:58`
- `modularity/inteAssistant/.../WebHookService.cs:180`
- `modularity/oauth/.../OAuthService.cs:1445`
- `modularity/workflow/.../FlowTaskManager.cs:2371`
- `modularity/visualdev/.../RunService.cs:137`
- `modularity/system/.../DataInterfaceService.cs:1853`
- `modularity/system/.../ScheduleService.cs:923,954`
- `tests/.../XunitTestCollectionRunnerWithAssemblyFixture.cs:76` (TEST)

### JNPF006 (async void) — 3 occurrences

| File | Line | Method Name |
|---|---|---|
| `modularity/common/.../DbJobPersistence.cs` | 141 | `OnChanged(PersistenceContext)` |
| `modularity/common/.../DbJobPersistence.cs` | 182 | `OnTriggerChanged(PersistenceTriggerContext)` |
| `modularity/common/.../DbJobPersistence.cs` | 223 | `OnExecutionRecord(TriggerTimeline)` |

**Note:** All 3 are Quartz.NET `IJobPersistenceListener` interface implementations (interface defines void return). Suppress with `#pragma warning disable JNPF006`, not refactor.

## Suppression Strategy

| Rule | Severity | Framework (`framework/**`) | Application Code |
|---|---|---|---|
| JNPF001 | Warning | Suggestion (`.editorconfig`) | Warning — migrate to constructor injection |
| JNPF002 | Warning | `#pragma` suppress (ADR-002 pattern) | Warning |
| JNPF003 | Warning | Suggestion (`.editorconfig`) | Warning |
| JNPF006 | Error | N/A — only in modularity | `#pragma` suppress (Quartz interface constraint) |

## Summary for Stage 7.6

1. **Analyzers exist but are CI-gated:** Roslynator 4.3.0 + StyleCop 1.2.0-beta.406 run only when `CI_BUILD=true`. NETAnalyzers always enabled at `latest-recommended`.
2. **No custom JNPF analyzers exist.** `dotnet.ruleset` suppresses ~80+ Roslynator/StyleCop rules.
3. **Forbidden pattern inventory:**
   - JNPF001: 37 occurrences across 19 files — largest surface area. Most in framework internals.
   - JNPF002: 4 production occurrences, all ADR-002 intentional. Zero `+=` patterns.
   - JNPF003: 24 occurrences, many in background job/event scopes where `CreateScope()` is correct.
   - JNPF006: 3 occurrences, all Quartz.NET interface implementations.
4. **Recommendation:** Create `JNPF.Analyzers` project with 4 analyzers. Use `.editorconfig` per-directory severity overrides for framework suppression. Enable StyleCop/Roslynator unconditionally. Gradually migrate JNPF001 in application code.
