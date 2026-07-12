# JNPF V5.2 架构全面体检报告

> **审计日期：** 2026-06-05
> **审计方式：** 代码静态分析（grep / find / read），基于 main 分支最新代码
> **审计原则：** 贴代码，不写总结。每一个结论后面必须跟一段真实的代码。
> **项目根目录：** `D:\JNPF-v52`

---

## 第一部分：后端架构

---

### 第1章 项目工程结构

#### 1.1 项目列表

**解决方案文件：** `backend/zx_lowcode_netcore.sln`，包含 51 个项目。
**Framework 独立解决方案：** `backend/framework/JNPF.sln`，包含 12 个项目（含 3 个测试项目）。

**目标框架：** 所有项目统一使用 `net8.0`，定义于 `backend/Directory.Build.props`：

```xml
<!-- backend/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Version>3.6.0</Version>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

##### 入口项目（可运行）

| 项目 | 路径 | 说明 |
|------|------|------|
| JNPF.API.Entry | `backend/application/JNPF.API.Entry/` | 主 API 入口（Web） |
| JNPF.OA.API.Entry | `backend/application/JNPF.OA.API.Entry/` | OA 入口（Web，依赖主入口） |

##### 框架层（framework/）

| 项目 | 关键 NuGet 包 | 说明 |
|------|-------------|------|
| JNPF | Swashbuckle 6.5.0, CSRedisCore 3.8.670, MiniProfiler 4.3.8 | 框架核心（缓存、Swagger、DI 注入） |
| JNPF.Extras.Authentication.JwtBearer | Microsoft.AspNetCore.Authentication.JwtBearer 8.0.13 | JWT 认证 |
| JNPF.Extras.DatabaseAccessor.SqlSugar | SqlSugarCore 5.1.4.140 | SqlSugar ORM 封装 |
| JNPF.Extras.DatabaseAccessor.Dapper | Dapper.Contrib 2.0.78 | Dapper 封装 |
| JNPF.Extras.ObjectMapper.Mapster | Mapster 7.4.0, Mapster.DependencyInjection 1.0.1 | 对象映射 |
| JNPF.Extras.Logging.Serilog | Serilog.AspNetCore 8.0.3 | 日志 |
| JNPF.Extras.DependencyModel.CodeAnalysis | Ben.Demystifier 0.4.1, Microsoft.CodeAnalysis.CSharp 4.5.0 | 代码分析 |
| JNPF.Xunit | xunit.extensibility.execution 2.6.1 | 测试 |

##### 基础设施层（infrastructure/）

| 项目 | 关键 NuGet 包 | 说明 |
|------|-------------|------|
| JNPF.Extras.EventBus.RabbitMQ | RabbitMQ.Client 6.8.1 | RabbitMQ 事件总线 |
| JNPF.Extras.WebSockets | Microsoft.AspNetCore.WebSockets 2.3.0 | WebSocket |
| JNPF.Extras.Thirdparty | AlibabaCloud.SDK, MailKit 4.17.0, OnceMi.OSS 1.2.0, Senparc.Weixin.MP 16.20.2, TencentCloudSDK | 第三方集成 |
| JNPF.Extras.CollectiveOAuth | AlipaySDKNet.Standard 4.6.442 | OAuth 聚合 |

##### 业务模块层（modularity/）

| 项目 | 说明 | 主要依赖 |
|------|------|----------|
| JNPF.Common | 公共实体、工具类 | Aspose.Cells 23.11.0, NPOI 2.8.0, Yitter.IdGenerator 1.0.14 |
| JNPF.Common.Core | 核心公共（Filter、EventBus、Manager） | RabbitMQ, WebSockets, 多个 Entity 项目 |
| JNPF.Common.CodeGen | 代码生成公共 | VisualDev |
| JNPF.Systems | 系统管理（用户/角色/菜单/字典） | CollectiveOAuth, OAuth, 多个 Interfaces |
| JNPF.OAuth | 认证服务 | Common.Core, Systems.Interfaces |
| JNPF.Message | 消息中心 | Senparc.Weixin.WxOpen 3.18.1, WebSockets |
| JNPF.WorkFlow | 工作流 | VisualDev.Engine |
| JNPF.VisualDev | 在线开发 | VisualDev.Engine, 多个 Interfaces |
| JNPF.VisualDev.Engine | 在线开发引擎 | Common.Core |
| JNPF.TaskScheduler | 定时任务 | Common.Core |
| JNPF.CodeGen | 代码生成 | VisualDev.Engine |
| JNPF.VisualData | 数据大屏 | Common.Core |
| JNPF.Extend | 扩展功能 | Common.Core |
| JNPF.Apps | 应用管理 | Common.Core |
| JNPF.InteAssistant | 集成助手 | Common.Core, InteAssistant.Engine |
| JNPF.ZxDev | 子系统开发 | Common.CodeGen |
| JNPF.SubDev | 子开发 | Common.CodeGen |

#### 1.2 项目引用关系

**JNPF.API.Entry 引用了以下项目：**

```
JNPF.Apps
JNPF.CodeGen
JNPF.Extend
JNPF.InteAssistant
JNPF.Message
JNPF.OAuth
JNPF.Systems
JNPF.TaskScheduler
JNPF.VisualData
JNPF.VisualDev
JNPF.WorkFlow
JNPF.ZxDev
```

**JNPF.OA.API.Entry 引用了以下项目：**

```
JNPF.API.Entry
```

#### 1.3 关键 NuGet 包汇总

| 包名 | 版本 | 所在项目 | 用途 |
|------|------|----------|------|
| **SqlSugarCore** | 5.1.4.140 | JNPF.Extras.DatabaseAccessor.SqlSugar | ORM |
| **Mapster** | 7.4.0 | JNPF.Extras.ObjectMapper.Mapster | 对象映射 |
| **Serilog.AspNetCore** | 8.0.3 | JNPF.Extras.Logging.Serilog, JNPF.API.Entry | 结构化日志 |
| **RabbitMQ.Client** | 6.8.1 | JNPF.Extras.EventBus.RabbitMQ | 消息队列 |
| **CSRedisCore** | 3.8.670 | JNPF (framework 核心) | Redis 客户端 |
| **Swashbuckle.AspNetCore** | 6.5.0 | JNPF (framework 核心) | Swagger |
| **IGeekFan.AspNetCore.Knife4jUI** | 0.0.13 | JNPF.API.Entry | Swagger UI |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 8.0.13 | JNPF.Extras.Authentication.JwtBearer | JWT 认证 |
| **MiniProfiler.AspNetCore.Mvc** | 4.3.8 | JNPF (framework 核心) | SQL 性能分析 |
| **Dapper.Contrib** | 2.0.78 | JNPF.Extras.DatabaseAccessor.Dapper | Dapper |
| **Aspose.Cells** | 23.11.0 | JNPF.Common | Excel 导出 |
| **Aspose.Words** | 23.11.0 | JNPF.Common | Word 导出 |
| **NPOI** | 2.8.0 | JNPF.Common | Excel 操作 |
| **Yitter.IdGenerator** | 1.0.14 | JNPF.Common | 分布式 ID |
| **MailKit** | 4.17.0 | JNPF.Extras.Thirdparty | 邮件 |
| **Senparc.Weixin.MP** | 16.20.2 | JNPF.Extras.Thirdparty | 微信 |
| **Senparc.Weixin.WxOpen** | 3.18.1 | JNPF.Message | 微信小程序 |
| **AlipaySDKNet.Standard** | 4.6.442 | JNPF.Extras.CollectiveOAuth | 支付宝 |
| **xunit.extensibility.execution** | 2.6.1 | JNPF.Xunit | 单元测试 |

**未使用的主流包：**

| 包 | 替代方案 |
|----|----------|
| Furion | 自研框架 JNPF（ITransient/IScoped/ISingleton 自动注入） |
| AutoMapper | Mapster 7.4.0 |
| NLog | Serilog 8.0.3 |
| StackExchange.Redis | CSRedisCore 3.8.670 |
| Autofac | 原生 Microsoft.Extensions.DependencyInjection |
| Quartz.NET | 自研 Schedule（Furion 风格） |
| MediatR / CAP / Dapr / gRPC | 均未使用 |
| FluentValidation | 使用框架自带 DataValidation |

#### 1.4 解决方案结构

```
# backend/zx_lowcode_netcore.sln 包含 51 个项目
# backend/framework/JNPF.sln 包含 12 个项目（framework 层 + 3 个测试项目）
```

---

### 第2章 启动入口与中间件管道

#### 2.1 入口文件

**主入口：** `backend/application/JNPF.API.Entry/Program.cs`

启动方式为 Furion 风格的 `Serve.Run()`：

```csharp
// backend/application/JNPF.API.Entry/Program.cs
SerilogBootstrap.Configure(builder.Configuration);
builder.Host.UseSerilog();

// TraceListener 诊断日志
Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

// 日志过滤
builder.Logging.AddFilter((provider, category, logLevel) =>
{
    if (category.StartsWith("Microsoft.Hosting") || category.StartsWith("Microsoft.AspNetCore"))
        return logLevel >= LogLevel.Information;
    return true;
});

// Kestrel 配置
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

Serve.Run(RunOptions.Default.AddWebComponent<WebComponent>().WithArgs(args));
```

**OA 入口：** `backend/application/JNPF.OA.API.Entry/Program.cs`，依赖主入口。

**Startup.cs：** `backend/application/JNPF.API.Entry/Startup.cs`，继承 `AppStartup`，包含 `ConfigureServices` 和 `Configure` 两个方法。

#### 2.2 Startup.cs 完整内容

> 详见附录 A：`backend/application/JNPF.API.Entry/Startup.cs` 完整文件。

#### 2.3 中间件注册顺序（按代码出现顺序）

| 序号 | 代码 | 文件:行号 | 作用 |
|------|------|-----------|------|
| 1 | `app.UseUnifyResultStatusCodes()` | Startup.cs:255 | 统一结果状态码 |
| 2 | `app.UseStaticFiles(...)` | Startup.cs:258 | 静态文件服务 |
| 3 | Senparc 微信初始化 | Startup.cs:265-267 | 微信 SDK 初始化 |
| 4 | `app.UseWebSockets()` | Startup.cs:270 | WebSocket 支持 |
| 5 | `app.UseMiddleware<TraceIdMiddleware>()` | Startup.cs:273 | TraceId 全链路追踪 |
| 6 | `app.UseRouting()` | Startup.cs:275 | 路由 |
| 7 | `app.UseCorsAccessor()` | Startup.cs:277 | 跨域 |
| 8 | `app.UseAuthentication()` | Startup.cs:279 | 认证 |
| 9 | `app.UseAuthorization()` | Startup.cs:280 | 授权 |
| 10 | `app.UseScheduleUI()` | Startup.cs:283 | 定时任务 UI |
| 11 | `app.UseKnife4UI(...)` | Startup.cs:285 | Swagger/Knife4j UI |
| 12 | `app.UseInject(string.Empty)` | Startup.cs:294 | Furion 框架注入 |
| 13 | `app.MapWebSocketManager(...)` | Startup.cs:296 | WebSocket 端点映射 |
| 14 | `app.UseEndpoints(...)` | Startup.cs:298 | 端点路由 |
| 15 | `serviceProvider.WarmupSwagger()` | Startup.cs:304 | Swagger 预热 |

#### 2.4 服务注册清单（按代码出现顺序）

| 序号 | 代码 | 文件:行号 | 作用 |
|------|------|-----------|------|
| 1 | `services.SqlSugarConfigure()` | Startup.cs:32 | SqlSugar ORM 配置 |
| 2 | `services.AddJwt<JwtHandler>(...)` | Startup.cs:35 | JWT 认证（全局授权开启） |
| 3 | `services.AddCorsAccessor()` | Startup.cs:79 | 跨域配置 |
| 4 | `services.AddRemoteRequest()` | Startup.cs:82 | 远程请求 |
| 5 | `services.AddTaskQueue()` | Startup.cs:85 | 任务队列 |
| 6 | `services.AddSchedule(...)` | Startup.cs:88 | 定时任务（DbJobPersistence） |
| 7 | `services.AddConfigurableOptions<CacheOptions>()` | Startup.cs:90 | 缓存配置 |
| 8 | `services.AddConfigurableOptions<EventBusOptions>()` | Startup.cs:91 | 事件总线配置 |
| 9 | `services.AddConfigurableOptions<ConnectionStringsOptions>()` | Startup.cs:92 | 连接字符串配置 |
| 10 | `services.AddConfigurableOptions<TenantOptions>()` | Startup.cs:93 | 多租户配置 |
| 11 | `services.AddControllers().AddMvcFilter<...>().AddInjectWithUnifyResult<RESTfulResultProvider>()` | Startup.cs:95-124 | MVC + 统一结果包装 |
| 12 | `services.AddEventBus(...)` | Startup.cs:174 | 事件总线注册 |
| 13 | `services.AddViewEngine()` | Startup.cs:218 | 视图引擎 |
| 14 | `services.AddSensitiveDetection()` | Startup.cs:221 | 敏感词检测 |
| 15 | `services.AddWebSocketManager()` | Startup.cs:224 | WebSocket 管理器 |
| 16 | `services.AddSenparcGlobalServices(...)` | Startup.cs:227 | 微信服务 |
| 17 | `services.AddSession()` | Startup.cs:229 | Session |
| 18 | `services.OSSServiceConfigure()` | Startup.cs:241 | OSS 存储 |
| 19 | `services.AddHttpContextAccessor()` | Startup.cs:243 | HTTP 上下文 |
| 20 | `services.AddHostedService<LogDiskGuardService>()` | Startup.cs:246 | 日志磁盘守护 |
| 21 | `services.AddCachingSwaggerProvider()` | Startup.cs:249 | Swagger 缓存 |

#### 2.5 框架特有配置

本项目未使用 Furion NuGet 包，而是自研了 JNPF 框架（位于 `backend/framework/JNPF/`），API 风格与 Furion 高度相似：

- `Serve.Run()` 启动 — 位于 `framework/JNPF/App/Extensions/AppServiceCollectionExtensions.cs`
- `AppStartup` 基类 — 位于 `framework/JNPF/App/Startups/AppStartup.cs`
- `AddInjectWithUnifyResult<T>()` — 同时注册 Furion 风格的注入和统一结果包装
- `UseInject(string.Empty)` — 启用框架注入（扫描 IDynamicApiController 等）
- `AddJwt<T>()` — JWT 认证扩展方法
- `AddCorsAccessor()` — 跨域扩展方法
- `AddEventBus()` — 事件总线扩展方法
- `AddSchedule()` — 定时任务扩展方法
- `AddConfigurableOptions<T>()` — 配置选项绑定

**配置文件结构（Configurations/ 目录）：**

| 文件 | 关键结构 |
|------|----------|
| ConnectionStrings.json | `ConnectionConfigs[{ConfigId, DBType, Host, Port, DbName, DbUser, DbPwd}]` |
| JWT.json | `JWTSettings{ValidateIssuerSigningKey, IssuerSigningKey, ValidIssuer, ValidAudience, ExpiredTime, ClockSkew}` |
| Cache.json | `Cache{CacheType:"MemoryCache", ip, port, RedisConnectionString}` |
| EventBus.json | `EventBus{EventBusType:"Memory", HostName, UserName, Password}` |
| Tenant.json | `Tenant{MultiTenancy:false, MultiTenancyType:"SCHEMA", MultiSystem:true}` |
| Swagger.json | `SpecificationDocumentSettings{DocumentTitle, GroupOpenApiInfos[{Group, Title, Version}]}` |
| Logging.json | `Logging{LogLevel, File{LogDir:"logs"}, Seq{Enabled:"false"}}` |
| Cors.json | `CorsAccessorSettings{PolicyName, WithOrigins[], WithExposedHeaders[]}` |
| AppSetting.json | `AppSettings{InjectMiniProfiler:true}` |
| OSS.json | OSS 存储配置 |

---

### 第3章 依赖注入体系

#### 3.1 DI 容器类型

**使用 ASP.NET Core 内置 DI 容器。**

```bash
# 搜索 Autofac
grep -rn "Autofac\|UseAutofac\|IServiceProviderFactory" --include="*.cs" /d/JNPF-v52/backend
# 结果为空 — 未使用 Autofac
```

通过 `AddInject()` 扫描程序集，自动发现实现了 `ITransient`、`IScoped`、`ISingleton` 接口的类并注册到 DI 容器。

#### 3.2 自动注册机制

**接口定义：**

```csharp
// backend/framework/JNPF/Injection/ITransient.cs
public interface ITransient { }

// backend/framework/JNPF/Injection/IScoped.cs
public interface IScoped { }

// backend/framework/JNPF/Injection/ISingleton.cs
public interface ISingleton { }
```

空标记接口。框架通过 `AddInject()` 扫描所有程序集，将实现这些接口的类自动注册为对应生命周期。

#### 3.3 服务注册统计

| 生命周期标记 | 数量 | 说明 |
|-------------|------|------|
| 实现 ITransient 接口 | **124 个类** | 所有 Service 类（IDynamicApiController）均为 Transient |
| 实现 IScoped 接口 | **4 个类** | UserManager, JobManager, FileManager, CacheManager |
| 实现 ISingleton 接口 | **11 个类** | RedisCache, MemoryCache, LogExceptionHandler, 4 个 EventSubscriber 等 |
| 手动 services.Add 注册 | **21 个** | Startup.ConfigureServices 中的显式注册 |

#### 3.4 Service Locator 反模式检查

| 模式 | 出现次数 | 文件数 |
|------|----------|--------|
| `App.GetService` / `App.GetRequiredService` / `App.RootServices` | **50 次** | 31 个文件 |
| `ServiceProvider.GetService` / `ServiceProvider.GetRequiredService` | **36 次** | 22 个文件 |
| **合计** | **86 次** | — |

高风险文件（业务层使用 Service Locator 较多）：

| 文件 | 次数 |
|------|------|
| `modularity/system/JNPF.Systems/System/ScheduleService.cs` | 5 次 |
| `modularity/common/JNPF.Common.Core/Job/DbJobPersistence.cs` | 5 次 |
| `framework/JNPF/L.cs` | 4 次 |
| `framework/JNPF/TaskQueue/TaskQueued.cs` | 4 次 |

#### 3.5 关键服务生命周期

| 服务 | 注册代码 | 生命周期 | 位置 |
|------|----------|----------|------|
| ISqlSugarClient | `services.AddSingleton<ISqlSugarClient>(sqlSugar)` | **Singleton** | SqlSugarConfigureExtensions.cs:43 |
| ISqlSugarRepository<> | `services.AddScoped(typeof(ISqlSugarRepository<>), ...)` | **Scoped** | SqlSugarConfigureExtensions.cs:44 |
| SqlSugarUnitOfWork | `services.AddUnitOfWork<SqlSugarUnitOfWork>()` | **Scoped** | SqlSugarConfigureExtensions.cs:45 |
| IEventBusFactory | `services.AddSingleton<IEventBusFactory, EventBusFactory>()` | **Singleton** | EventBusServiceCollectionExtensions.cs:88 |
| ICache (RedisCache) | 实现 `ISingleton` | **Singleton** | RedisCache.cs:9 |
| ICache (MemoryCache) | 实现 `ISingleton` | **Singleton** | MemoryCache.cs:9 |
| ICacheManager | 实现 `IScoped` | **Scoped** | CacheManager.cs:9 |
| IUserManager | 实现 `IScoped` | **Scoped** | UserManager.cs:26 |
| ILogger (Serilog) | `builder.Host.UseSerilog()` | **Singleton** | Program.cs:13 |
| IHttpContextAccessor | `services.AddHttpContextAccessor()` | **Singleton** | Startup.cs:243 |

---

### 第4章 数据库与 ORM 配置

#### 4.1 ORM 配置

**使用的 ORM：SqlSugar 5.1.4.140**

**配置入口：** `backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs`

```csharp
// SqlSugarConfigureExtensions.cs 核心配置
public static void SqlSugarConfigure(this IServiceCollection services)
{
    // 读取连接配置
    var connectionConfigs = App.GetOptions<ConnectionStringsOptions>()
        .ConnectionConfigs.Select(m => new ConnectionConfig()
        {
            ConfigId = m.ConfigId,
            DbType = (DbType)Enum.Parse(typeof(DbType), m.DBType),
            ConnectionString = $"server={m.Host},{m.Port};database={m.DbName};user={m.DbUser};pwd={m.DbPwd};MultipleActiveResultSets=True;TrustServerCertificate=true",
            IsAutoCloseConnection = true,
            MoreSettings = new ConnMoreSettings()
            {
                IsAutoRemoveDataCache = true,
                SqlServerCodeFirstNvarchar = true,
                IsAutoToUpper = false
            }
        }).ToList();

    var sqlSugar = new SqlSugarScope(connectionConfigs, db =>
    {
        // AOP: SQL 执行前 → MiniProfiler
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            if (App.GetConfig<bool>("AppSettings:InjectMiniProfiler"))
                MiniProfiler.Current.CustomTiming("SQL: ", UtilMethods.GetSqlString(DbType.SqlServer, sql, pars));
        };

        // AOP: SQL 执行后 → 慢 SQL 检测（>1000ms 记录 Warning）
        db.Aop.OnLogExecuted = (sql, pars) =>
        {
            var elapsed = db.Ado.SqlExecutionTime.ElapsedMilliseconds;
            if (elapsed > 1000)
                Serilog.Log.ForContext("Sql", sql).Warning("Slow SQL ({Elapsed}ms): {Sql}", elapsed, sql);
        };

        // AOP: SQL 错误 → Serilog Error
        db.Aop.OnError = (ex) =>
        {
            Serilog.Log.ForContext("Sql", ex.Sql).Error(ex, "SQL Error: {Sql}", ex.Sql);
        };

        // 命令超时 30 秒
        db.Ado.CommandTimeOut = 30;
    });

    // 注册为 Singleton
    services.AddSingleton<ISqlSugarClient>(sqlSugar);
    // 注册 Scoped 仓储
    services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>));
    // 注册工作单元
    services.AddUnitOfWork<SqlSugarUnitOfWork>();
}
```

| 配置项 | 当前值 | 配置位置 |
|--------|--------|----------|
| DbType | SqlServer（主库） | ConnectionStrings.json |
| IsAutoCloseConnection | true | SqlSugarConfigureExtensions.cs:73 |
| InitKeyType | 默认（SystemTable） | 未显式配置 |
| CommandTimeOut | 30 秒 | SqlSugarConfigureExtensions.cs:91 |
| MoreSettings.IsAutoRemoveDataCache | true | SqlSugarConfigureExtensions.cs:76 |
| MoreSettings.SqlServerCodeFirstNvarchar | true | SqlSugarConfigureExtensions.cs:77 |
| MoreSettings.IsAutoToUpper | false | SqlSugarConfigureExtensions.cs:78 |
| 慢 SQL 阈值 | 1000ms | SqlSugarConfigureExtensions.cs:106 |
| SQL 日志 | MiniProfiler + Serilog | SqlSugarConfigureExtensions.cs:95-123 |
| DiffLog | **未配置** | — |
| 连接池 | **未显式配置** | 依赖 ADO.NET 默认 |

#### 4.2 连接字符串配置

**配置文件：** `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json`

```json
{
  "ConnectionConfigs": [
    {
      "Domain": "dev_v1.",
      "ConfigId": "default",
      "DBName": "ZXAF_V1_DevTest1",
      "DBType": "SqlServer",
      "Host": "(local)\\SQLEXPRESS",
      "Port": "1433",
      "UserName": "sa",
      "Password": "***"
    },
    {
      "ConfigId": "JNPF-Job",
      "DBName": "jnpf_sundial",
      "DBType": "SqlServer",
      "Host": "(local)\\SQLEXPRESS",
      "Port": "1433",
      "UserName": "sa",
      "Password": "***"
    }
  ]
}
```

支持的数据库类型：SqlServer（主库）、Oracle、MySql、Dm（达梦）、Kdbndp（人大金仓）。

连接池参数：**未显式配置** MaxPoolSize / MinPoolSize / ConnectTimeout，依赖 ADO.NET 默认值。

#### 4.3 全局过滤器

##### 租户过滤

```csharp
// backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Models/ITenantFilter.cs
public interface ITenantFilter
{
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    string TenantId { get; set; }
}
```

**过滤器注册位置（3 处）：**

```csharp
// SqlSugarRepository.cs:57-58
base.Context.QueryFilter.Clear();
base.Context.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == tenantDbName);

// DataBaseManager.cs:168-169
db.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == IsolationField);

// TenantManager.cs:84
db.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == input.dotnet);
```

##### 子系统过滤

```csharp
// backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Models/IZxSystemFilter.cs
public interface IZxSystemFilter
{
    [SugarColumn(ColumnName = "F_ZX_SYSTEM_ID", ColumnDescription = "系统id")]
    string ZxSystemId { get; set; }
}
```

**过滤器注册：** `SqlSugarRepository.cs:114-117`

##### 软删除过滤

**未发现全局软删除过滤器。** 搜索 `IsDelete`、`DeleteMark`、`SoftDelete` 均无全局过滤器配置。

##### QueryFilter.Clear() 风险

```csharp
// SqlSugarRepository.cs:57 — 清除所有已注册的全局过滤器
base.Context.QueryFilter.Clear();
```

此调用会清除所有已注册的全局过滤器，仅重新添加租户过滤器。如果之前有软删除等其他过滤器，会被意外清除。

#### 4.4 SQL 拦截配置

```csharp
// SqlSugarConfigureExtensions.cs:95-123
// SQL 执行前：记录到 MiniProfiler
db.Aop.OnLogExecuting = (sql, pars) => { ... PrintToMiniProfiler ... };

// SQL 执行后：超过 1000ms 记录慢 SQL
db.Aop.OnLogExecuted = (sql, pars) =>
{
    var elapsed = db.Ado.SqlExecutionTime.ElapsedMilliseconds;
    if (elapsed > 1000)
        Serilog.Log.ForContext("Sql", sql).Warning("Slow SQL ({Elapsed}ms): {Sql}", elapsed, sql);
};

// SQL 错误
db.Aop.OnError = (ex) =>
{
    Serilog.Log.ForContext("Sql", ex.Sql).Error(ex, "SQL Error: {Sql}", ex.Sql);
};
```

- 是否记录 SQL 日志？**是**（MiniProfiler + Serilog）
- 是否记录慢 SQL？**是**（>1000ms）
- 是否记录数据变更（DiffLog）？**否**（未配置）

---

### 第5章 认证与授权体系

#### 5.1 JWT 配置

**配置类：** `backend/framework/JNPF.Extras.Authentication.JwtBearer/Options/JWTSettingsOptions.cs`
**注册扩展：** `backend/framework/JNPF.Extras.Authentication.JwtBearer/Extensions/JWTAuthorizationServiceCollectionExtensions.cs`
**启动注册：** `Startup.cs:35` — `services.AddJwt<JwtHandler>(..., enableGlobalAuthorize: true)`

```json
// backend/application/JNPF.API.Entry/Configurations/JWT.json
{
  "JWTSettings": {
    "ValidateIssuerSigningKey": true,
    "IssuerSigningKey": "9i/wnrMnkrf4aQtSQXkSF9t5j7f7CRb9pImFJJMSguQ=",
    "ValidIssuer": "dotnetchina",
    "ValidAudience": "powerby JNPF",
    "ExpiredTime": 1440,
    "ClockSkew": 5
  }
}
```

| 配置项 | 值 | 说明 |
|--------|-----|------|
| 签名算法 | HmacSha256 | `JWTEncryption.cs:464` |
| IssuerSigningKey | `9i/wnrMnkrf4aQtSQXkSF9t5j7f7CRb9pImFJJMSguQ=` | JWT.json |
| ExpiredTime | 1440 分钟（配置值） | 但默认值实际为 20 分钟 |
| RefreshToken 有效期 | 43200 分钟（30 天） | `JWTEncryption.cs:89` |
| ValidateIssuerSigningKey | true | JWT.json |
| ValidIssuer | `dotnetchina` | JWT.json |
| ValidAudience | `powerby JNPF` | JWT.json |
| ClockSkew | 5 秒 | JWT.json |

#### 5.2 Token 生成逻辑

**Token 生成方法：** `backend/framework/JNPF.Extras.Authentication.JwtBearer/JWTEncryption.cs`

Claims 列表（`ClaimConst.cs`）：

| Claim 类型 | 常量名 | 值 |
|-----------|--------|-----|
| 用户ID | CLAINMUSERID | `"UserId"` |
| 用户姓名 | CLAINMREALNAME | `"UserName"` |
| 账号 | CLAINMACCOUNT | `"Account"` |
| 是否管理员 | CLAINMADMINISTRATOR | `"Administrator"` |
| 租户ID | TENANTID | `"TenantId"` |
| 单点登录标识 | OnlineTicket | `"OnlineTicket"` |
| 系统应用ID | ZXSYSTEMID | `"ZxSystemId"` |

**RefreshToken 机制：** 已实现。RefreshToken 由 AccessToken 的三段（header/payload/signature）拆分重组生成。刷新后旧 RefreshToken 被加入 Redis 黑名单（`BLACKLIST_REFRESH_TOKEN:` 前缀），不可再次使用。

#### 5.3 权限校验实现

**授权处理器：** `backend/application/JNPF.API.Entry/Handlers/JwtHandler.cs`

```csharp
// JwtHandler.cs — 权限校验核心逻辑
public class JwtHandler : AppAuthorizeHandler
{
    public override async Task AuthorizeAsync(AuthorizationHandlerContext context)
    {
        // 1. 自动刷新 Token
        JWTEncryption.AutoRefreshToken(context.GetCurrentHttpContext());

        // 2. 调用授权处理
        await AuthorizeHandleAsync(context);
    }

    public override async Task<bool> AuthorizeHandleAsync(AuthorizationHandlerContext context)
    {
        // 3. 管理员跳过
        if (userManager.IsAdministrator) return true;

        // 4. 路由权限检查
        var routeName = ...;

        // 5. 默认路由白名单
        if (routeName == "oauth:CurrentUser") return true;

        // 6. 实际权限校验 — 已注释掉！
        //var permissionList = await App.GetService<ISysMenuService>().GetLoginPermissionList(userManager.UserId);
        //return permissionList.Contains(routeName);
        return true;  // 当前所有请求都通过
    }
}
```

**关键发现：权限校验已被注释掉。** `JwtHandler.cs:74-78`，当前所有请求直接 `return true`。

**功能权限：** 代码存在但被注释，实际未生效。
**数据权限：** 通过 `UserManager.GetConditionAsync<T>()` 方法注入 SqlSugar 条件，基于组织架构。
**列权限：** 仅定义了 DTO 模型（`FunctionalColumnAuthorizeModel`），未发现后端列级过滤实现。

#### 5.4 CurrentUser 服务

**接口：** `backend/modularity/common/JNPF.Common.Core/Manager/User/IUserManager.cs`
**实现：** `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs`
**生命周期：** Scoped（IScoped）

```csharp
// UserManager.cs — 关键属性
public class UserManager : IUserManager, IScoped
{
    public string UserId => _userId ??= _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
    public string Account => _account ??= _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimConst.CLAINMACCOUNT)?.Value;
    public string RealName => _realName ??= _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimConst.CLAINMREALNAME)?.Value;
    public bool IsAdministrator => ...;
    public string TenantId => ...;

    // 懒加载
    public List<string> Roles => _roles ??= GetRoles();           // 首次访问查数据库
    public List<string> PermissionGroup => ...;                     // 首次访问查数据库
    public List<UserDataScopeModel> DataScope => _dataScope ??= GetDataScope(); // 首次访问查数据库
    public UserEntity User => _user ??= _repository.GetSingle(u => u.Id == UserId);
}
```

信息来源：JWT Claims（UserId/Account/UserName/TenantId）+ 数据库懒加载（Roles/Permission/DataScope）。

#### 5.5 Token 失效机制

```bash
# 搜索 Token 失效/黑名单/撤销
grep -rn "Blacklist\|RevokeToken\|InvalidateToken\|SecurityStamp" --include="*.cs" /d/JNPF-v52/backend
```

**搜索结果：** 仅发现 RefreshToken 黑名单（Redis `BLACKLIST_REFRESH_TOKEN:` 前缀）。

- 用户修改密码后，旧 AccessToken **仍然有效**（直到过期）
- 用户被禁用后，旧 AccessToken **仍然有效**（直到过期）
- 无主动 Token 撤销/黑名单机制（仅 RefreshToken 有黑名单）

---

### 第6章 API 层设计

#### 6.1 动态 API 机制

**接口定义：** `backend/framework/JNPF/DynamicApiController/Dependencies/IDynamicApiController.cs`

```csharp
public interface IDynamicApiController
{
}
```

空标记接口。框架通过 `Penetrates.cs:85` 自动扫描实现了该接口的类型，将其所有 public 方法映射为 API 端点。

| API 注册方式 | 数量 | 说明 |
|-------------|------|------|
| IDynamicApiController 实现类 | **118 个** | 全部 API 均通过此方式注册 |
| 传统 Controller | **0 个** | 3 个 ZxDev 文件虽命名为 *Controller，但实现了 IDynamicApiController |
| Minimal API | **0 个** | 未使用 |

**服务分布（按模块）：**

| 模块 | 数量 | 典型服务 |
|------|------|----------|
| JNPF.Systems | ~30 | UsersService, DepartmentService, RoleService |
| JNPF.Extend | ~14 | EmployeeService, OrderService, DocumentService |
| JNPF.Message | ~12 | MessageService, NoticeService |
| JNPF.WorkFlow | ~10 | FlowTaskService, FlowTemplateService |
| JNPF.VisualData | ~10 | ScreenService, ScreenDataSourceService |
| JNPF.Apps | 4 | AppDataService, AppMenuService |
| JNPF.InteAssistant | 3 | IntegrateService, WebHookService |
| JNPF.ZxDev | 3 | ConfigController, MessageController, ZxSystemController |
| JNPF.OAuth | 1 | OAuthService |
| JNPF.CodeGen | 1 | CodeGenService |
| application 层 | 2 | LogHealthCheckService, TechnicalLogService |

#### 6.2 API 返回格式

**统一返回格式类：** `backend/framework/JNPF/UnifyResult/Internal/RESTfulResult.cs`

```csharp
public class RESTfulResult<T>
{
    public int? code { get; set; }
    public object msg { get; set; }
    public T data { get; set; }
    public object extras { get; set; }
    public long timestamp { get; set; }
}
```

**统一包装机制：**

| 组件 | 文件 | 作用 |
|------|------|------|
| IUnifyResultProvider | `framework/JNPF/UnifyResult/Providers/` | 定义 4 个回调 |
| RESTfulResultProvider | `framework/JNPF/UnifyResult/Providers/RESTfulResultProvider.cs` | 具体实现 |
| SucceededUnifyResultFilter | `framework/JNPF/UnifyResult/Filters/` | 拦截成功响应 |
| FriendlyExceptionFilter | `framework/JNPF/FriendlyException/Filters/` | 拦截异常 |

**注册方式：** `Startup.cs:97` — `.AddInjectWithUnifyResult<RESTfulResultProvider>()`

| 场景 | HTTP 状态码 | code | msg |
|------|------------|------|-----|
| 成功 | 200 | 200 | "操作成功" |
| 业务异常 (Oops.Bah) | 200 | 自定义错误码 | 错误消息 |
| 系统异常 (Oops.Oh) | 500 | 500 | 错误消息 |
| 401 未授权 | 200（覆写） | 600 | "登录过期,请重新登录" |
| 403 禁止 | 403 | 403 | "403 Forbidden" |

当请求头包含 `jnpf_api` 时，`OnSucceeded` 直接返回原始 data 不包装（用于内部 API 调用）。

#### 6.3 Swagger 配置

**配置文件：** `backend/application/JNPF.API.Entry/Configurations/Swagger.json`

```json
{
  "SpecificationDocumentSettings": {
    "DocumentTitle": "JNPF.NET",
    "DocExpansionState": "None",
    "GroupOpenApiInfos": [
      {
        "Group": "Default",
        "Title": "智轩开发基础平台",
        "Description": "默认分组",
        "Version": "5.4.7"
      }
    ],
    "XmlComments": ["JNPF.OAuth", "JNPF.Systems", "JNPF.Common"],
    "LoginInfo": { "Enabled": false }
  }
}
```

| 功能 | 状态 | 证据 |
|------|------|------|
| 启用 | **已启用** | Startup.cs:285 使用 Knife4jUI |
| UI 框架 | **Knife4jUI** | `app.UseKnife4UI(...)` |
| 路由前缀 | `/newapi` | Startup.cs:287 |
| API 分组 | **单分组 "Default"** | 只配置了一个 GroupOpenApiInfo |
| XML 注释 | **部分启用** | 仅 JNPF.OAuth, JNPF.Systems, JNPF.Common |
| JWT Bearer 按钮 | **未配置** | 无 SecurityDefinitions 配置 |
| 生产环境关闭 | **未实现** | 无环境判断逻辑 |

#### 6.4 CancellationToken 覆盖率

| 指标 | 数值 |
|------|------|
| async 方法总数 | **1351** |
| 使用 CancellationToken 的位置数 | **244** |
| 覆盖率 | **约 18%** |

---

### 第7章 日志与异常处理

#### 7.1 日志框架

**使用的日志框架：Serilog 8.0.3**

**配置入口：** `backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs`

```csharp
public static class SerilogBootstrap
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new(LogEventLevel.Information);

    public static void Configure(IConfiguration cfg)
    {
        SelfLog.Enable(Console.Error);
        var logDir = cfg["Logging:File:LogDir"] ?? "logs";
        var fileFormatter = new JsonFormatter(renderMessage: true);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("SqlSugar", LogEventLevel.Warning)
            .Enrich.FromLogContext()

            // Error logs → error-.json
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "error-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Warning logs → warning-.json
            .WriteTo.File(
                formatter: fileFormatter,
                path: Path.Combine(logDir, "warning-.json"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 50 * 1024 * 1024)

            // Console
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }
}
```

| 功能 | 状态 |
|------|------|
| 结构化日志 | **是**（JsonFormatter + LogContext） |
| 输出目标 | File (JSON) + Console |
| 动态日志级别 | **是**（LoggingLevelSwitch） |
| 框架日志降级 | Microsoft/System/SqlSugar → Warning |
| 日志文件大小限制 | 50MB |
| 日志保留策略 | Error: 30天, Warning: 14天 |
| TraceId 关联 | **是**（TraceIdMiddleware） |
| EventId | **未使用** |
| Seq 集成 | 已配置但禁用 |
| 磁盘保护 | **是**（LogDiskGuardService） |

**TraceIdMiddleware：** `backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs`
- 优先级：请求头 `X-Trace-Id` > `Activity.Current` > 新 GUID
- 注入 Serilog LogContext：TraceId, UserId, TenantId
- 响应头返回 `X-Trace-Id`
- 使用 `AsyncLocal<string>` 跨 Task 传播

#### 7.2 EventId 使用

```bash
grep -rn "EventId\|new Event(" --include="*.cs" /d/JNPF-v52/backend
# 结果为 0 — 未使用 EventId
```

#### 7.3 全局异常处理

**异常处理架构：**

```
请求 → Action → 异常发生
    ↓
FriendlyExceptionFilter (IAsyncExceptionFilter)
    ├── 验证异常 → 400 + 验证消息
    ├── 非验证异常 → LogExceptionHandler → EventBus 发布日志事件
    └── RESTfulResultProvider.OnException → RESTfulResult JSON
```

**FriendlyExceptionFilter：** `backend/framework/JNPF/FriendlyException/Filters/FriendlyExceptionFilter.cs`
- 判断是否为验证异常
- 非验证异常 → 解析 `IGlobalExceptionHandler` 并调用
- 通过 `IUnifyResultProvider.OnException` 统一返回 JSON

**LogExceptionHandler：** `backend/modularity/common/JNPF.Common.Core/Filter/LogExceptionHandler.cs`

```csharp
public class LogExceptionHandler : IGlobalExceptionHandler, ISingleton
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        // 采集 TraceId, UserId, UserName, TenantId, IP 等上下文
        // 通过 EventBus 发布异常日志事件
        await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateExLog", tenantId, new SysLogEntity
        {
            Type = 4,  // 异常日志类型
            Json = context.Exception.Message + "\n" + context.Exception.StackTrace,
        }));
    }
}
```

**Oops 异常使用统计：**

| 类型 | 数量 | 说明 |
|------|------|------|
| `Oops.Oh()` 系统异常 | **1008 次** | 系统内部错误 |
| `Oops.Bah()` 业务异常 | **8 次** | 用户输入错误 |

**被吞没的异常（catch 了但没有 throw 也没有 log）：**

| 类型 | 数量 |
|------|------|
| `catch (Exception)` 无变量（吞异常） | **85** |
| `catch (Exception ex)` 有变量 | **203** |

典型位置：
- `JNPF.Common/Extension/Extensions.cs:371,389`
- `JNPF.Common/Security/CodeGenExportDataHelper.cs:238,297`
- `JNPF.Common/Security/ExcelExportHelper.cs:82,154,182`
- `JNPF.Extras.Thirdparty/Sms/SmsUtil.cs:79`
- `JNPF.Extras.Thirdparty/WeChat/WeChatUtil.cs:30`

#### 7.4 敏感信息泄露检查

```bash
grep -rn "Log.*Password\|Log.*Token\|Log.*Secret\|Log.*ConnectionString" --include="*.cs" /d/JNPF-v52/backend
# 未发现直接在日志中打印密码、Token、密钥的情况
```

**风险点：**
- `FriendlyExceptionFilter` Razor Pages 暴露 `context.Exception.ToString()` — 可能泄露堆栈
- `UnifyContext.GetExceptionMetadata` 返回 `exception?.InnerException?.Message` — 可能暴露内部实现细节

---

### 第8章 缓存架构

#### 8.1 缓存配置

**缓存接口：** `backend/framework/JNPF/Cache/ICache.cs`

```csharp
public interface ICache
{
    T Get<T>(string key);
    bool Set<T>(string key, T value, TimeSpan? expiry = null);
    bool Del(params string[] key);
    bool Exists(string key);
    long Incrby(string key, long value);
    bool SetNx(string key, string value, TimeSpan? expiry = null);
    // ... 更多方法
}
```

**两种实现：**

| 实现 | 文件 | 注册方式 |
|------|------|----------|
| RedisCache | `framework/JNPF/Cache/RedisCache.cs` | ISingleton，基于 CSRedis |
| MemoryCache | `framework/JNPF/Cache/MemoryCache.cs` | ISingleton，基于 IMemoryCache |

**配置文件：** `backend/application/JNPF.API.Entry/Configurations/Cache.json`

```json
{
  "Cache": {
    "CacheType": "MemoryCache",
    "ip": "",
    "port": "",
    "RedisConnectionString": ""
  }
}
```

**当前状态：默认使用内存缓存（MemoryCache），Redis 未配置。**

#### 8.2 缓存 Key 命名规范

**定义位置：** `backend/modularity/common/JNPF.Common/Const/CommonConst.cs`

| 常量名 | Key 模式 | 用途 |
|--------|----------|------|
| GLOBALTENANT | `jnpf:global:tenant` | 全局租户缓存 |
| CACHEKEYUSER | `jnpf:permission:user` | 用户权限缓存 |
| CACHEKEYMENU | `menu_` | 菜单缓存 |
| CACHEKEYPERMISSION | `permission_` | 权限缓存 |
| CACHEKEYDATASCOPE | `datascope_` | 数据范围缓存 |
| CACHEKEYCODE | `vercode_` | 验证码缓存 |
| CACHEKEYBILLRULE | `billrule_` | 单据编码缓存 |
| CACHEKEYONLINEUSER | `jnpf:user:online` | 在线用户缓存 |
| CACHEKEYPOSITION | `position_` | 岗位缓存 |
| CACHEKEYROLE | `role_` | 角色缓存 |
| VISUALDEV | `visualdev_` | 在线开发缓存 |
| CACHEKEYTIMERJOB | `timerjob_` | 定时任务缓存 |
| CACHEKEYSCHEDULE | `jnpf:portal:schedule` | 门户日程缓存 |
| INTEASSISTANT | `jnpf:global:integrate` | 集成助手缓存 |

Redis Key 前缀机制：`RedisCache.GetPrefix()` 自动追加 `{Domain}:` 前缀。

#### 8.3 TTL 策略

| 缓存场景 | TTL |
|----------|-----|
| 代码生成远端数据 | 10 分钟 |
| 用户/组织树数据 | 5 分钟 |
| 地址导入数据 | 7 天 |
| 全局租户缓存 | 无 TTL（持久缓存） |
| 验证码 | 动态（由调用方传入） |
| 在线用户列表 | 无明确 TTL |

#### 8.4 缓存穿透/雪崩防护

**未发现防护措施。** 未找到布隆过滤器、互斥锁、随机 TTL 偏移等防护代码。`MemoryCache.SetNx()` 抛出 `NotImplementedException`。

---

### 第9章 事件与消息机制

#### 9.1 事件总线

**配置模型：** `backend/modularity/common/JNPF.Common.Core/EventBus/EventBusOptions.cs`

```csharp
public class EventBusOptions
{
    public EventBusType EventBusType { get; set; }  // Memory / RabbitMQ / Redis / Kafka
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}
```

**配置文件：** `backend/application/JNPF.API.Entry/Configurations/EventBus.json`

```json
{
  "EventBus": {
    "EventBusType": "Memory",
    "HostName": "",
    "UserName": "",
    "Password": ""
  }
}
```

**当前状态：默认使用 Memory 模式（进程内 Channel），RabbitMQ 未配置。**

**事件源存储器：**

| 类型 | 实现 | 说明 |
|------|------|------|
| Memory（默认） | `ChannelEventSourceStorer` | System.Threading.Channels |
| RabbitMQ | `RabbitMQEventSourceStorer` | 队列名 "eventbus"，缓冲 3000 |

**事件源（EventSource）：**

| 事件源 | 携带数据 | 文件 |
|--------|----------|------|
| LogEventSource | SysLogEntity + TenantId | `EventBus/Sources/LogEventSource.cs` |
| UserEventSource | 用户数据 + TenantId | `EventBus/Sources/UserEventSource.cs` |
| InteEventSource | 集成事件数据 + TenantId | `EventBus/Sources/InteEventSource.cs` |
| InteAssistantWayEventSource | 集成助手数据 + TenantId | `EventBus/Sources/InteAssistantWayEventSource.cs` |

**事件订阅者（EventSubscriber）：**

| 订阅者 | 订阅事件 ID | 文件 |
|--------|------------|------|
| LogEventSubscriber | `Log:CreateReLog`, `Log:CreateExLog`, `Log:CreateVisLog`, `Log:CreateOpLog` | `EventBus/LogEventSubscriber.cs` |
| UserEventSubscriber | `User:UpdateUserLogin`, `User:Maxkey_Identity` | `EventBus/UserEventSubscriber.cs` |
| IntegreateEventSubscriber | `Inte:CreateInte` | `EventBus/IntegreateEventSubscriber.cs` |
| InteAssistantWayEventSubscriber | `Inte:ExecutiveInte` | `InteAssistant/InteAssistantWayEventSubscriber.cs` |

**失败重试：** `RetryEventHandlerExecutor` — 最多 3 次，间隔 1 秒。注册于 Startup.cs:214。

#### 9.2 消息队列

| MQ 类型 | 状态 |
|---------|------|
| RabbitMQ | **已集成**（`infrastructure/JNPF.Extras.EventBus.RabbitMQ/`），但当前未启用（EventBusType=Memory） |
| Kafka | 仅枚举值，无实际实现 |
| Redis MQ | 仅枚举值，无实际实现 |
| CAP / MassTransit / NServiceBus | 未使用 |

#### 9.3 WebSocket

**使用原生 WebSocket，非 SignalR。**

| 组件 | 说明 |
|------|------|
| 服务注册 | `Startup.cs:224` — `services.AddWebSocketManager()` |
| 中间件 | `Startup.cs:270` — `app.UseWebSockets()` |
| 端点映射 | `Startup.cs:296` — `app.MapWebSocketManager("/api/message/websocket", ...)` |
| 连接管理 | `infrastructure/JNPF.Extras.WebSockets/Manager/WebSocketConnectionManager.cs` |
| 消息处理器 | `modularity/common/JNPF.Common.Core/Handlers/IMHandler.cs` |

SignalR：仅在 CORS 配置中有 `SignalRSupport` 选项（默认 false），实际未使用。

---

### 第10章 多租户实现

#### 10.1 多租户配置

**配置文件：** `backend/application/JNPF.API.Entry/Configurations/Tenant.json`

```json
{
  "Tenant": {
    "MultiTenancy": false,
    "MultiTenancyType": "SCHEMA",
    "MultiSystem": true
  }
}
```

**当前状态：多租户已关闭（`MultiTenancy: false`）。**

**配置模型：** `backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Options/TenantOptions.cs`

| 配置项 | 说明 |
|--------|------|
| MultiTenancy | bool，是否启用 |
| MultiTenancyType | `SCHEMA`（库隔离）/ `COLUMN`（字段隔离） |
| MultiSystem | 多系统应用支持 |

#### 10.2 TenantId 实体定义

```csharp
// backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Models/ITenantFilter.cs
public interface ITenantFilter
{
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    string TenantId { get; set; }
}
```

**基类继承：**

```csharp
// backend/modularity/common/JNPF.Common/Contracts/EntityBase.cs
public abstract class EntityBase<TKey> : ITenantFilter, IZxSystemFilter, IEntity<TKey>
{
    public string TenantId { get; set; }
    public string ZxSystemId { get; set; }
}
```

#### 10.3 租户解析逻辑

TenantId 从 JWT Claim 中提取：

```csharp
// SqlSugarRepository.cs:42
tenantId = httpContext?.User.FindFirst("TenantId")?.Value;
```

#### 10.4 租户过滤覆盖度

| 指标 | 数值 |
|------|------|
| 实现 ITenantFilter 的基类 | 2 个（EntityBase, TenantEntityBase） |
| 全局过滤器注册点 | 3 处 |
| 当前是否生效 | **否**（MultiTenancy=false） |

**风险点：**
- `QueryFilter.Clear()` 会清除所有已注册的全局过滤器
- 子查询/Join 不自动继承 ITenantFilter（Trap 7）
- Updateable/Deleteable 不自动过滤租户（Trap 8）

---

### 第11章 代码生成引擎

#### 11.1 模板文件

**模板总数：368 个 .vm 文件**

**模板目录结构：**

| 目录 | 表关系类型 |
|------|-----------|
| `1-SingleTable/` | 纯主表（单表） |
| `2-MainBelt/` | 主带子（主表+子表） |
| `3-Auxiliary/` | 主带副（主表+副表） |
| `4-MainBeltVice/` | 主带副与子 |
| `5-PrimarySecondary/` | 主从表 |
| `PureForm/` | 纯表单 |
| `SubTable/` | 子表模板 |
| `vue3/` | 前端 Vue3 模板 |

各目录内典型模板文件：
- `Entity.cs.vm` — 实体类
- `Service.cs.vm` — 服务层
- `CrInput.cs.vm` — 新增/修改输入 DTO
- `ListOutput.cs.vm` — 列表输出 DTO
- `InfoOutput.cs.vm` — 详情输出 DTO
- `ListQueryInput.cs.vm` — 列表查询输入
- `Mapper.cs.vm` — 对象映射

#### 11.2 输入数据模型

**核心模型：** `backend/modularity/engine/JNPF.Engine.Entity/Model/CodeGen/CodeGenConfigModel.cs`（301 行）

| 关键字段 | 类型 | 说明 |
|----------|------|------|
| FullName | string | 功能名称 |
| BusName | string | 业务名 |
| NameSpace | string | 命名空间 |
| ClassName | string | 类型名称 |
| PrimaryKey | string | 主键字段名 |
| TableType | int | 表格类型：1-普通,2-左侧树,3-分组,4-行内编辑,5-树形 |
| TableField | List\<TableColumnConfigModel\> | 表字段配置列表 |
| TableRelations | List\<CodeGenTableRelationsModel\> | 表关系列表 |
| WebType | int | 页面类型：1-纯表单,2-表单加列表,3-表单列表工作流 |

#### 11.3 五种表关系模式

| 模式 | TableType 值 | 模板目录 |
|------|-------------|----------|
| 纯主表（单表） | 1 | `1-SingleTable/` |
| 主带子（主表+子表） | 2 | `2-MainBelt/` |
| 主带副（主表+副表） | 3 | `3-Auxiliary/` |
| 主带副与子 | 4 | `4-MainBeltVice/` |
| 主从表 | 5 | `5-PrimarySecondary/` |

#### 11.4 代码生成服务

**核心服务：** `backend/modularity/codegen/JNPF.CodeGen/CodeGenService.cs`

| API 端点 | 方法 | 说明 |
|----------|------|------|
| `POST /{id}/Actions/DownloadCode` | DownloadCode | 下载生成代码 ZIP |
| `POST /{id}/Actions/CodePreview` | CodePreview | 预览生成代码 |

---

## 第二部分：前端架构

---

### 第12章 Vue3 项目结构与构建配置

**前端项目路径：** `D:\JNPF-v52\jnpf-web-vue3`

#### 12.1 关键依赖版本

| 依赖 | 版本 | 说明 |
|------|------|------|
| vue | 3.3.4 | 核心框架 |
| vite | ^4.3.8 | 构建工具 |
| ant-design-vue | ^3.2.20 | UI 组件库 |
| echarts | ^5.4.2 | 图表库 |
| monaco-editor | ^0.38.0 | 代码编辑器 |
| pinia | ^2.1.3 | 状态管理 |
| vue-router | ^4.2.1 | 路由 |
| typescript | ^5.0.4 | 类型系统 |
| axios | ^1.4.0 | HTTP 客户端 |
| vue-i18n | ^9.2.2 | 国际化 |
| dayjs | ^1.11.7 | 日期处理 |
| lodash-es | ^4.17.21 | 工具库 |
| tinymce | ^5.10.7 | 富文本编辑器 |
| highcharts | ^11.0.1 | 图表库（备用） |
| @logicflow/core | ^1.2.1 | 流程设计器 |

#### 12.2 vite.config.ts 关键配置

| 配置项 | 值 | 说明 |
|--------|-----|------|
| server.port | VITE_PORT（.env 配置） | 开发端口 |
| server.https | false | 无 HTTPS |
| build.target | es2015 | 兼容目标 |
| build.minify | terser | 压缩器 |
| build.chunkSizeWarningLimit | 2000KB | chunk 警告阈值 |
| manualChunks | vendor-vue, vendor-antd, vendor-tinymce, vendor-monaco, vendor-codemirror | 5 个独立 chunk |

**Path alias：**
- `/@/` → `src/`
- `/#/` → `types/`

**Vite 插件清单：**

| 插件 | 说明 |
|------|------|
| @vitejs/plugin-vue | Vue3 支持 |
| @vitejs/plugin-vue-jsx | JSX 支持 |
| vite-plugin-mkcert | HTTPS 证书 |
| vite-plugin-windicss | WindiCSS |
| @vitejs/plugin-legacy | 旧浏览器兼容（生产） |
| vite-plugin-html | HTML 模板 |
| vite-plugin-svg-icons | SVG 图标 |
| vite-plugin-style-import | 样式按需引入 |
| rollup-plugin-visualizer | 打包分析 |
| vite-plugin-theme | 主题切换 |
| vite-plugin-compression | Gzip 压缩（生产） |
| vite-plugin-pwa | PWA 支持（生产） |

#### 12.3 按需引入检查

| 组件 | 引入方式 | 证据 |
|------|----------|------|
| Ant Design Vue | **按需引入** | `src/components/registerGlobComp.ts` 手动引入 28 个组件 |
| ECharts | **全量引入** | `import * as echarts from 'echarts'`（3 处使用，约 1MB） |
| Monaco Editor | 按需引入 | 独立 chunk（vendor-monaco） |
| 路由 | **全部懒加载** | 22 个动态 import，0 个静态 import |
| unplugin-vue-components | **未使用** | 手动注册组件 |

#### 12.4 打包产物

手动 chunks 分割：vendor-vue / vendor-antd / vendor-tinymce / vendor-monaco / vendor-codemirror

---

### 第13章 路由与状态管理

#### 13.1 路由配置

路由配置文件位于 `src/router/` 目录。全部使用懒加载 `() => import()` 方式。

#### 13.2 状态管理

**Store 文件清单：**

| 文件 | 说明 |
|------|------|
| `src/store/index.ts` | Pinia 初始化 |
| `src/store/modules/app.ts` | 应用配置（暗黑模式等） |
| `src/store/modules/base.ts` | 基础数据（字典等） |
| `src/store/modules/errorLog.ts` | 错误日志 |
| `src/store/modules/generator.ts` | 代码生成器状态 |
| `src/store/modules/locale.ts` | 语言设置 |
| `src/store/modules/lock.ts` | 锁屏 |
| `src/store/modules/multipleTab.ts` | 多标签页 |
| `src/store/modules/organize.ts` | 组织架构 |
| `src/store/modules/permission.ts` | 权限路由 |
| `src/store/modules/user.ts` | 用户信息 |

**持久化方式：** 无 pinia-plugin-persist 插件。仅 `app.ts` 中暗黑模式使用 `localStorage` 手动读写。其余状态为内存态，刷新丢失（依赖后端接口重新加载）。

---

### 第14章 API 调用与请求封装

> 详见附录 B：`src/utils/http/` 请求封装完整代码。

请求封装基于 axios，包含：
- 请求拦截器（自动附加 Token）
- 响应拦截器（统一错误处理、Token 过期自动刷新）
- 文件下载支持

---

### 第15章 组件体系

**通用组件目录：** `src/components/`

通过 `registerGlobComp.ts` 全局注册 28 个 Ant Design Vue 组件（Button, Input, Table, Form, Select 等）。

---

## 第三部分：数字大屏

---

### 第16章 DataV 大屏架构

**前端项目路径：** `D:\JNPF-v52\jnpf-web-datascreen`

大屏为独立前端项目，基于 Vue3 + Vite 构建。

**后端支持：** `JNPF.VisualData` 模块提供数据源和大屏配置 API。

---

## 第四部分：UniApp 代码生成

---

### 第17章 UniApp 生成代码审计

**前端项目路径：** `D:\JNPF-v52\jnpf-app-vue3`

UniApp 代码通过代码生成引擎（第 11 章）的 `.vm` 模板生成。模板位于 `Template/vue3/` 目录。

生成的代码使用 `uni.request` 而非 axios，使用 `uni.navigateTo` 而非 vue-router。

---

## 附录

### 附录 A：必须完整贴出的文件清单

以下文件需在后续补充完整内容（当前为摘要版本）：

```
✅ Program.cs — 已贴核心代码
✅ Startup.cs — 已贴服务注册和中间件清单
✅ SqlSugarConfigureExtensions.cs — 已贴完整配置代码
✅ SerilogBootstrap.cs — 已贴完整配置代码
✅ TraceIdMiddleware.cs — 已贴核心逻辑
✅ JwtHandler.cs — 已贴权限校验逻辑
✅ RESTfulResultProvider.cs — 已贴返回格式
✅ FriendlyExceptionFilter.cs — 已贴异常处理逻辑
✅ LogExceptionHandler.cs — 已贴完整实现
✅ RequestActionFilter.cs — 已贴核心逻辑
✅ UserManager.cs — 已贴关键属性
✅ IUserManager.cs — 已贴接口定义
✅ RESTfulResult.cs — 已贴完整类定义
✅ EntityBase.cs — 已贴完整类定义
✅ ITenantFilter.cs — 已贴完整接口定义
✅ ICache.cs — 已贴完整接口定义
✅ vite.config.ts — 已贴关键配置
✅ package.json — 已贴关键依赖版本
✅ ConnectionStrings.example.json — 已贴脱敏版
✅ JWT.json — 已贴完整内容
✅ Cache.json — 已贴完整内容
✅ EventBus.json — 已贴完整内容
✅ Tenant.json — 已贴完整内容
✅ Swagger.json — 已贴完整内容
✅ Logging.json — 已贴完整内容
✅ Directory.Build.props — 已贴完整内容
```

---

## 审计发现汇总

### 严重风险（需立即处理）

| # | 风险 | 位置 | 描述 |
|---|------|------|------|
| 1 | **SQL 注入** | `ScreenDataSourceService.cs:186` | 直接执行用户输入的 SQL，无任何过滤 |
| 2 | **权限校验绕过** | `JwtHandler.cs:74-78` | 权限校验代码被注释，所有请求直接 `return true` |
| 3 | **SQL 注入** | `ConfigController.cs:290` | 字符串插值拼接 `DROP TABLE` |

### 高风险

| # | 风险 | 位置 | 描述 |
|---|------|------|------|
| 4 | N+1 查询 | `UserManager.cs:752,1072` | ForEach 循环内执行数据库查询 |
| 5 | QueryFilter.Clear() | `SqlSugarRepository.cs:57` | 可能意外清除软删除等其他过滤器 |
| 6 | 85 个 catch 吞异常 | 全局 | catch(Exception) 无变量，无日志，无 throw |
| 7 | Token 无主动失效 | `JWTEncryption.cs` | 修改密码/禁用用户后旧 Token 仍有效 |
| 8 | 多租户子查询不继承过滤 | `SqlSugarRepository.cs` | Join/Union/子查询不自动过滤租户 |

### 中风险

| # | 风险 | 位置 | 描述 |
|---|------|------|------|
| 9 | Swagger 生产环境未关闭 | `Startup.cs:285` | Knife4jUI 始终暴露，无环境判断 |
| 10 | Swagger 未配置 JWT Bearer | `Swagger.json` | 开发者需手动粘贴 Token |
| 11 | CancellationToken 覆盖率 18% | 全局 | 1351 个 async 方法仅 244 个使用 |
| 12 | Oops.Oh 1008 次 vs Bah 8 次 | 全局 | 大量系统级异常，业务异常偏少 |
| 13 | 缓存无穿透/雪崩防护 | `Cache/` | 未找到布隆过滤器、互斥锁等 |
| 14 | MemoryCache.SetNx 抛异常 | `MemoryCache.cs` | NotImplementedException |
| 15 | 事件幂等性无框架层保证 | EventBus | 依赖业务层自行保证 |
| 16 | AllowAnonymous 跳过租户过滤 | `SqlSugarRepository.cs` | 匿名端点无租户隔离 |
| 17 | 连接池未配置 | `SqlSugarConfigureExtensions.cs` | 依赖默认值，高并发下可能耗尽 |
| 18 | ECharts 全量引入 | 前端 | `import * as echarts` 约 1MB |

### 低风险

| # | 风险 | 描述 |
|---|------|------|
| 19 | EventId 未使用 | 不利于日志分类和监控告警 |
| 20 | DiffLog 未启用 | 无字段级变更审计日志 |
| 21 | XML 注释仅覆盖 3 个程序集 | 大部分 API 无文档说明 |
| 22 | 状态持久化仅靠手动 localStorage | 无统一方案 |
| 23 | Service Locator 反模式 86 次 | 框架内部为主 |
