# JNPF V5.2 全项目关键模式扫描报告

> 扫描日期：2026-06-07
> 扫描工具：ripgrep (rg)
> 扫描范围：`backend/**/*.cs`

---

## 1. App.GetService / App.GetRequiredService (Service Locator)

**总计：38 处，26 个文件**

| 文件 | 行号 | 服务类型 | 所属项目 |
|---|---|---|---|
| `framework/JNPF/TaskQueue/TaskQueued.cs` | 20, 33, 47, 61 | `ITaskQueue` | JNPF |
| `framework/JNPF/Localization/L.cs` | 22, 27, 36, 46 | `IStringLocalizerFactory` | JNPF |
| `framework/JNPF/RemoteRequest/Internal/HttpRequestPartMethods.cs` | 452, 542, 854 | `JsonSerializerProvider` | JNPF |
| `framework/JNPF/InstantMessaging/IM.cs` | 19, 33 | `IHubContext<THub>` | JNPF |
| `framework/JNPF/DistributedIDGenerator/IDGen.cs` | 17, 27 | `IDistributedIDGenerator` | JNPF |
| `framework/JNPF/EventBus/MessageCenter.cs` | 117, 126 | `IEventPublisher` | JNPF |
| `application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs` | 132-133 | `IUserManager` | API.Entry |
| `framework/JNPF/FriendlyException/Oops.cs` | 251 | — | JNPF |
| `framework/JNPF/Schedule/Schedular.cs` | 69 | — | JNPF |
| `framework/JNPF/Logging/Log.cs` | 26 | `ILogger<T>` | JNPF |
| `framework/JNPF/Logging/Internal/StringLoggingPartMethods.cs` | 122 | — | JNPF |
| `framework/JNPF/VirtualFileServer/FS.cs` | 44 | — | JNPF |
| `framework/JNPF/ViewEngine/Internal/ViewEnginePartMethods.cs` | 77 | — | JNPF |
| `framework/JNPF/JsonSerialization/JSON.cs` | 17 | — | JNPF |
| `framework/JNPF/DataValidation/Validators/DataValidator.cs` | 250 | — | JNPF |
| `framework/JNPF/Options/ConnectionStringsOptions.cs` | 64 | — | JNPF |
| `framework/JNPF/App/Extensions/IConfigurationExtenstions.cs` | 19 | — | JNPF |
| `framework/JNPF/App/Native.cs` | 62 | — | JNPF |
| `framework/JNPF/RemoteRequest/Http.cs` | 17 | — | JNPF |
| `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Options/ConnectionStringsOptions.cs` | 75 | — | SqlSugar |
| `framework/JNPF.Xunit/XunitExtensions/...cs` | 85 | — | Xunit |
| `modularity/common/JNPF.Common.Security/JsonHelper.cs` | 18 | — | Common |
| `modularity/common/JNPF.Common.CodeGen/DataParsing/ControlParsing.cs` | 262 | — | CodeGen |
| `modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs` | 1879 | — | VisualDev |
| `modularity/system/JNPF.Systems/Permission/RoleService.cs` | 886 | — | Systems |
| `application/JNPF.API.Entry/Handlers/JwtHandler.cs` | 74 | `ISysMenuService` (注释中) | API.Entry |

---

## 2. App.GetConfig / App.GetOptions / App.GetOptionsMonitor (Config Access)

**总计：60 处，40 个文件**

关键文件（按出现次数排序）：

| 文件 | 次数 | 配置路径 | 所属项目 |
|---|---|---|---|
| `modularity/oauth/JNPF.OAuth/OAuthService.cs` | 4 | `OAuth`, `JNPF_App` | OAuth |
| `modularity/common/JNPF.Common.Core/Manager/Files/FileManager.cs` | 3 | `JNPF_App` | Common |
| `modularity/common/JNPF.Common/Configuration/KeyVariable.cs` | 3 | `Tenant`, `JNPF_App`, `Oss` (静态初始化) | Common |
| `framework/JNPF/Localization/L.cs` | 3 | `LocalizationSettingsOptions` | JNPF |
| `modularity/extend/JNPF.Extend/DocumentPreview.cs` | 3 | `JNPF_App` | Extend |
| `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Repositories/SqlSugarDbContextProvider.cs` | 2 | `ConnectionStrings`, `Tenant` | SqlSugar |
| `framework/JNPF/UnifyResult/Providers/RESTfulResultProvider.cs` | 2 | — | JNPF |
| `framework/JNPF/FriendlyException/Oops.cs` | 2 | — | JNPF |
| `framework/JNPF/Logging/Internal/Penetrates.cs` | 2 | — | JNPF |
| `application/JNPF.API.Entry/Startup.cs` | 2 | — | API.Entry |
| `modularity/system/JNPF.Systems/System/DataInterfaceService.cs` | 2 | (静态初始化) | Systems |
| 另外 26 个文件各 1 处 | | | |

**风险标记：** `KeyVariable.cs` 和 `DataInterfaceService.cs` 在静态字段初始化器中调用 `App.GetConfig`，类型加载时执行，时序风险高。

---

## 3. App.HttpContext / App.User (HTTP Context Access)

**总计：68 处，26 个文件**

| 文件 | 次数 | 用途 | 所属项目 |
|---|---|---|---|
| `modularity/system/JNPF.Systems/System/DataInterfaceService.cs` | 9 | 请求头、Host、IP | Systems |
| `modularity/oauth/JNPF.OAuth/OAuthService.cs` | 9 | UserAgent、Headers、Trace | OAuth |
| `modularity/common/JNPF.Common/Security/NetHelper.cs` | 6 | IP、URL 构建 | Common |
| `modularity/common/JNPF.Common.Contracts/TenantCLDSEntityBase.cs` | 4 | `App.User?.FindFirst(ClaimConst.CLAINMUSERID)` | Common |
| `modularity/common/JNPF.Common.Contracts/SystemCLDSEntityBase.cs` | 4 | 同上 | Common |
| `modularity/common/JNPF.Common.Contracts/FwCLDEntityBase.cs` | 4 | 同上 | Common |
| `modularity/common/JNPF.Common.Contracts/CLDEntityBase.cs` | 4 | 同上 | Common |
| `framework/JNPF/Localization/L.cs` | 3 | — | JNPF |
| `modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs` | 2 | 构造函数中 `App.HttpContext` | Common |
| 另外 17 个文件 | | | |

**风险标记：** Entity 基类（`CLDEntityBase` 等）在数据生命周期方法中直接访问 `App.User`，领域实体与 HTTP 上下文紧耦合。

---

## 4. serviceProvider.CreateScope() (Manual Scope Creation)

**总计：4 处，4 个文件**

| 文件 | 行号 | 是否 using | 所属项目 |
|---|---|---|---|
| `framework/JNPF/Schedule/HostedServices/ScheduleHostedService.cs` | 213 | **否（泄漏风险）** | JNPF |
| `modularity/taskscheduler/JNPF.TaskScheduler/Listener/ScheduleJob.cs` | 38 | 是 | TaskScheduler |
| `modularity/taskscheduler/JNPF.TaskScheduler/Listener/OnlineUserJob.cs` | 25 | 是 | TaskScheduler |
| `modularity/system/JNPF.Systems/System/ScheduleService.cs` | 923 | 是 | Systems |

**风险标记：** `ScheduleHostedService.cs:213` 未使用 `using`，scope 可能泄漏。

---

## 5. Aop.DataExecuting = (赋值，非 +=)

**总计：4 处，3 个文件**

| 文件 | 行号 | 上下文 | 所属项目 |
|---|---|---|---|
| `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Repositories/SqlSugarDbContextProvider.cs` | 233 | 在 `ApplyDataExecutingFilter` 方法内，合并了 TenantId + ZxSystemId | SqlSugar |
| `modularity/common/JNPF.Common.Core/Manager/Tenant/TenantManager.cs` | 85 | 租户管理器内赋值 | Common |
| `modularity/common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs` | 170 | 数据库管理器内赋值 | Common |
| `modularity/common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs` | 202 | 同上，第二次赋值（覆盖前一次） | Common |

**关键发现：** 全部使用 `=` 赋值（覆盖模式），无 `+=` 追加。`DataBaseManager.cs` 有两次赋值，后者覆盖前者。这是 Task 0.6 的核心验证目标。

---

## 6. QueryFilter.Clear / QueryFilter.AddTableFilter

**总计：12 处，3 个文件**

| 文件 | 行号 | 操作 | 所属项目 |
|---|---|---|---|
| `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Repositories/SqlSugarDbContextProvider.cs` | 145 | `Clear()` | SqlSugar |
| 同上 | 146 | `AddTableFilter<ITenantFilter>` | SqlSugar |
| 同上 | 205 | `AddTableFilter<IZxSystemFilter>` | SqlSugar |
| 同上 | 206 | `Clear<IZxSystemFilter>` | SqlSugar |
| 同上 | 207 | `AddTableFilter<IZxSystemFilter>` | SqlSugar |
| `modularity/common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs` | 168-169 | Clear + Add | Common |
| 同上 | 198-200 | Clear + Add（含 workaround 注释） | Common |
| `modularity/common/JNPF.Common.Core/Manager/Tenant/TenantManager.cs` | 83-84 | Clear + Add | Common |

**注意：** `DataBaseManager.cs:199` 有 workaround："first removal throws exception, so add one first"。

---

## 7. CopyNew()

**总计：23 处，13 个文件**

| 文件 | 次数 | 所属项目 |
|---|---|---|
| `modularity/common/JNPF.Common.Core/Job/DbJobPersistence.cs` | 4 | Common |
| `modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantRun.cs` | 3 | InteAssistant |
| `modularity/common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs` | 3 | Common |
| `modularity/common/JNPF.Common.Core/EventBus/UserEventSubscriber.cs` | 2 | Common |
| `modularity/common/JNPF.Common.Core/EventBus/IntegreateEventSubscriber.cs` | 2 | Common |
| `modularity/system/JNPF.Systems/System/DataInterfaceService.cs` | 2 | Systems |
| `application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs` | 1 | API.Entry |
| `modularity/common/JNPF.Common.Core/EventBus/LogEventSubscriber.cs` | 1 | Common |
| `modularity/inteAssistant/JNPF.InteAssistant.Engine/IntegrateTiming.cs` | 1 | InteAssistant |
| `modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantWayEventSubscriber.cs` | 1 | InteAssistant |
| `modularity/inteAssistant/JNPF.InteAssistant.Engine/ExecutionQueue.cs` | 1 | InteAssistant |
| `modularity/inteAssistant/JNPF.InteAssistant/WebHookService.cs` | 1 | InteAssistant |
| `modularity/taskscheduler/JNPF.TaskScheduler/Listener/SpareTimeDemo.cs` | 1 | TaskScheduler |

**模式：** CopyNew 主要用于 EventBus 订阅者和后台任务中获取独立 SqlSugar 客户端。

---

## 8. [EventSubscribe] (Event Bus Subscribers)

**总计：8 个订阅，4 个文件**

| 文件 | 行号 | 事件名称 | 所属项目 |
|---|---|---|---|
| `EventBus/UserEventSubscriber.cs` | 43 | `User:UpdateUserLogin` | Common |
| `EventBus/UserEventSubscriber.cs` | 58 | `User:Maxkey_Identity` | Common |
| `EventBus/LogEventSubscriber.cs` | 41 | `Log:CreateReLog` | Common |
| `EventBus/LogEventSubscriber.cs` | 42 | `Log:CreateExLog` | Common |
| `EventBus/LogEventSubscriber.cs` | 43 | `Log:CreateVisLog` | Common |
| `EventBus/LogEventSubscriber.cs` | 44 | `Log:CreateOpLog` | Common |
| `EventBus/IntegreateEventSubscriber.cs` | 78 | `Inte:CreateInte` | Common |
| `InteAssistant.Engine/InteAssistantWayEventSubscriber.cs` | 71 | `Inte:ExecutiveInte` | InteAssistant |

---

## 9. [AppStartup] / AppStartup Subclasses

**总计：1 处**

| 文件 | 行号 | 类名 | Order | 所属项目 |
|---|---|---|---|---|
| `application/JNPF.API.Entry/Startup.cs` | 28 | `Startup : AppStartup` | — | API.Entry |

---

## 10. ITransient / IScoped / ISingleton (DI Lifetime Interfaces)

**总计：5 处，5 个文件**（仅 ITransient 和 ISingleton，无 IScoped）

| 文件 | 行号 | 接口 | 所属项目 |
|---|---|---|---|
| `modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs` | 30 | ITransient | VisualDev |
| `modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs` | 36 | ITransient | CodeGen |
| `modularity/common/JNPF.Common.CodeGen/DataParsing/ControlParsing.cs` | 19 | ITransient | CodeGen |
| `modularity/common/JNPF.Common.Core/Job/DynamicJobCompiler.cs` | 9 | ISingleton | Common |
| `modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantRun.cs` | 29 | ITransient | InteAssistant |

---

## 11. async void

**总计：3 处，1 个文件**

| 文件 | 行号 | 方法签名 | 所属项目 |
|---|---|---|---|
| `modularity/common/JNPF.Common.Core/Job/DbJobPersistence.cs` | 141 | `public async void OnChanged(PersistenceContext context)` | Common |
| 同上 | 182 | `public async void OnTriggerChanged(PersistenceTriggerContext context)` | Common |
| 同上 | 223 | `public async void OnExecutionRecord(TriggerTimeline timeline)` | Common |

**说明：** 接口强制签名（定时任务持久化框架接口），非自主选择。`Program.cs:15` 有注释说明。

---

## 统计汇总

| 模式 | 总数 | 涉及项目数 |
|---|---|---|
| App.GetService 调用 | 38 | 26 |
| App.GetConfig 调用 | 60 | 40 |
| App.HttpContext 引用 | 68 | 26 |
| CreateScope 调用 | 4 | 4 |
| Aop.DataExecuting = (赋值) | 4 | 3 |
| QueryFilter 使用 | 12 | 3 |
| CopyNew 调用 | 23 | 13 |
| EventSubscribe 处理器 | 8 | 4 |
| AppStartup 子类 | 1 | 1 |
| 生命周期接口实现 | 5 | 5 |
| async void 方法 | 3 | 1 |
| **合计** | **226** | — |

---

## 生产环境部署

- 部署模式：**待确认**（需联系运维）
- 数据库：**待确认**（SQL Server / MySQL / PostgreSQL）
- Redis：**待确认**（单机 / 集群）

---

## 关键风险摘要

1. **JwtHandler 权限校验被注释**（P0）— 所有已认证用户可访问所有端点
2. **DataExecuting 全部使用 `=` 赋值** — 后设置的回调会覆盖前一个
3. **Entity 基类直接访问 App.User** — 领域实体与 HTTP 上下文紧耦合
4. **ScheduleHostedService CreateScope 未 using** — 潜在 scope 泄漏
5. **静态字段初始化器调用 App.GetConfig** — 类型加载时序风险
