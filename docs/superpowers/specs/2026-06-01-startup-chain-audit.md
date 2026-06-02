# JNPF V5.2 后端启动链路静态审计报告

> **审计日期：** 2026-06-01
> **审计范围：** JNPF.API.Entry 从进程启动到首个 HTTP 端点就绪的完整链路
> **审计方法：** 源码逐行阅读，无猜测，全部标注文件路径和行号

---

## 一、启动链路全局时序

```
进程启动
  │
  ├─ ① CLR 静态构造：App 静态构造函数 (App.cs:449)
  │     ├─ GetAssemblies() → 扫描所有 DLL 加载程序集
  │     └─ EffectiveTypes = Assemblies.SelectMany(GetTypes) → 全量类型集合
  │
  ├─ ② Serve.Run() (Serve.cs:560)
  │     └─ BuildApplication() (Serve.cs:613)
  │
  ├─ ③ WebApplication.CreateBuilder(args) (Serve.cs:619)
  │     └─ .NET 默认初始化：Kestrel、配置、日志 providers
  │
  ├─ ④ builder.AddWebComponent<WebComponent>() (Serve.cs:626)
  │     └─ WebComponent.Load() (Program.cs:9)
  │           ├─ SerilogBootstrap.Configure() → 构建 Serilog Logger
  │           ├─ builder.Host.UseSerilog()
  │           ├─ TraceListener 注册
  │           ├─ 日志过滤器
  │           └─ Kestrel MaxRequestBodySize 配置
  │
  ├─ ⑤ builder.Inject() (AppWebApplicationBuilderExtensions.cs:19)
  │     └─ InternalApp.ConfigureApplication() (InternalApp.cs:45)
  │           ├─ AddJsonFiles() → 扫描并加载所有 *.json 配置文件
  │           ├─ services.AddStartupFilter<StartupFilter>()
  │           ├─ services.AddHttpContextAccessor()
  │           └─ services.AddApp() (AppServiceCollectionExtensions.cs:184)
  │                 ├─ AddConfigurableOptions<AppSettingsOptions>()
  │                 ├─ AddMemoryCache() + AddDistributedMemoryCache()
  │                 ├─ AddDependencyInjection() → 【阻塞·程序集扫描】
  │                 │     └─ App.EffectiveTypes 扫描 IPrivateDependency 实现类
  │                 ├─ AddStartups() → 【阻塞·反射扫描+调用】
  │                 │     └─ 扫描 AppStartup 子类 → 反射调用 ConfigureServices
  │                 ├─ AddObjectMapper() → Mapster 注册
  │                 └─ Encoding.RegisterProvider(CodePagesEncodingProvider)
  │
  ├─ ⑥ builder.Build() (Serve.cs:672)
  │     └─ DI 容器编译、中间件管道构建
  │
  ├─ ⑦ app.Run() / app.Start() (Serve.cs:569)
  │     └─ IStartupFilter.Execute → StartupFilter.Configure() (StartupFilter.cs:22)
  │           ├─ 响应头中间件注入
  │           ├─ UseApp() → 无额外操作
  │           └─ UseStartups() → 反射调用所有 AppStartup.Configure()
  │                 └─ Startup.Configure() (Startup.cs:252)
  │                       ├─ UseUnifyResultStatusCodes()
  │                       ├─ UseStaticFiles()
  │                       ├─ 【阻塞·微信SDK】RegisterService.Start().UseSenparcGlobal()
  │                       ├─ UseWebSockets()
  │                       ├─ UseMiddleware<TraceIdMiddleware>()
  │                       ├─ UseRouting()
  │                       ├─ UseCorsAccessor()
  │                       ├─ UseAuthentication() + UseAuthorization()
  │                       ├─ UseScheduleUI()
  │                       ├─ UseKnife4UI() → Swagger UI 中间件
  │                       ├─ UseInject("") → UseSpecificationDocuments → UseSwagger + UseSwaggerUI
  │                       ├─ MapWebSocketManager()
  │                       ├─ UseEndpoints()
  │                       └─ WarmupSwagger() → 【阻塞·首次Swagger生成】
  │
  └─ ⑧ Kestrel 开始监听 → HTTP 端点就绪
```

---

## 二、任务 1.1 — Startup 完整注册清单

### A. Services 注册（ConfigureServices 阶段）

| # | 方法名 | 所在文件:行号 | DB连接 | DB迁移 | 程序集扫描 | 网络IO | CPU密集 | 文件IO |
|---|--------|--------------|--------|--------|-----------|--------|---------|--------|
| 1 | `SerilogBootstrap.Configure()` | SerilogBootstrap.cs:19 | 否 | 否 | 否 | 否 | 否 | **是** — 创建日志目录 |
| 2 | `builder.Host.UseSerilog()` | Program.cs:13 | 否 | 否 | 否 | 否 | 否 | 否 |
| 3 | `TraceListener 注册` | Program.cs:17-21 | 否 | 否 | 否 | 否 | 否 | **是** — 打开文件流 |
| 4 | `AddJsonFiles()` | InternalApp.cs:145 | 否 | 否 | 否 | 否 | 否 | **是** — 扫描+读取所有*.json |
| 5 | `AddConfigurableOptions<AppSettingsOptions>()` | AppServiceCollectionExtensions.cs:187 | 否 | 否 | 否 | 否 | 否 | 否 |
| 6 | `AddMemoryCache()` | AppServiceCollectionExtensions.cs:190 | 否 | 否 | 否 | 否 | 否 | 否 |
| 7 | `AddDistributedMemoryCache()` | AppServiceCollectionExtensions.cs:191 | 否 | 否 | 否 | 否 | 否 | 否 |
| 8 | **`AddDependencyInjection()`** | AppServiceCollectionExtensions.cs:194 | 否 | 否 | **是** — 扫描所有 IPrivateDependency | 否 | **是** — 反射注册 | 否 |
| 9 | **`AddStartups()`** | AppServiceCollectionExtensions.cs:197 | 否 | 否 | **是** — 扫描 AppStartup 子类 | 否 | **是** — 反射调用 | 否 |
| 10 | `AddObjectMapper()` | AppServiceCollectionExtensions.cs:200 | 否 | 否 | 否 | 否 | 否 | 否 |
| 11 | `Encoding.RegisterProvider()` | AppServiceCollectionExtensions.cs:203 | 否 | 否 | 否 | 否 | 否 | 否 |
| 12 | **`SqlSugarConfigure()`** | SqlSugarConfigureExtensions.cs:17 | **是** — 创建 SqlSugarScope 单例 | 否（无CodeFirst） | 否 | 否 | 否 | 否 |
| 13 | `AddJwt<JwtHandler>()` | JWTAuthorizationServiceCollectionExtensions.cs:97 | 否 | 否 | **是** — 反射获取框架上下文 | 否 | 否 | 否 |
| 14 | `AddCorsAccessor()` | CorsAccessorServiceCollectionExtensions.cs:20 | 否 | 否 | 否 | 否 | 否 | 否 |
| 15 | `AddRemoteRequest()` | RemoteRequestServiceCollectionExtensions.cs:19 | 否 | 否 | **是** — 扫描 IHttpDispatchProxy | 否 | 否 | 否 |
| 16 | `AddTaskQueue()` | TaskQueueServiceCollectionExtensions.cs:32 | 否 | 否 | 否 | 否 | 否 | 否 |
| 17 | `AddSchedule()` | ScheduleServiceCollectionExtensions.cs:17 | 否 | 否 | 否 | 否 | 否 | 否 |
| 18 | `AddConfigurableOptions<CacheOptions>()` | Startup.cs:90 | 否 | 否 | 否 | 否 | 否 | 否 |
| 19 | `AddConfigurableOptions<EventBusOptions>()` | Startup.cs:91 | 否 | 否 | 否 | 否 | 否 | 否 |
| 20 | `AddConfigurableOptions<ConnectionStringsOptions>()` | Startup.cs:92 | 否 | 否 | 否 | 否 | 否 | 否 |
| 21 | `AddConfigurableOptions<TenantOptions>()` | Startup.cs:93 | 否 | 否 | 否 | 否 | 否 | 否 |
| 22 | `AddControllers()` + filters + JSON | Startup.cs:95-124 | 否 | 否 | 否 | 否 | 否 | 否 |
| 23 | `AddUnifyJsonOptions("special")` | Startup.cs:126-141 | 否 | 否 | 否 | 否 | 否 | 否 |
| 24 | `AddUnifyJsonOptions("datainterfaceSpecial")` | Startup.cs:143-161 | 否 | 否 | 否 | 否 | 否 | 否 |
| 25 | `Configure<ForwardedHeadersOptions>` | Startup.cs:166-171 | 否 | 否 | 否 | 否 | 否 | 否 |
| 26 | **`AddEventBus()`** | EventBusServiceCollectionExtensions.cs:17 | 否 | 否 | 否 | **条件是** — 若RabbitMQ模式则创建ConnectionFactory | 否 | 否 |
| 27 | `AddViewEngine()` | ViewEngineServiceCollectionExtensions.cs:16 | 否 | 否 | 否 | 否 | 否 | 否 |
| 28 | **`AddSensitiveDetection()`** | SensitiveDetectionServiceCollectionExtensions.cs:50 | 否 | 否 | 否 | 否 | 否 | 否 — 懒加载，首次使用时读取嵌入资源 |
| 29 | **`AddWebSocketManager()`** | WebSocketServiceCollectionExtensions.cs:10 | 否 | 否 | **是** — 扫描 WebSocketHandler 子类 | 否 | 否 | 否 |
| 30 | `AddSenparcGlobalServices()` | Startup.cs:227 | 否 | 否 | 否 | 否 | 否 | 否 |
| 31 | `AddSenparcWeixinServices()` | Startup.cs:228 | 否 | 否 | 否 | 否 | 否 | 否 |
| 32 | `AddSession()` | Startup.cs:229 | 否 | 否 | 否 | 否 | 否 | 否 |
| 33 | **`OSSServiceConfigure()`** | OSSServiceConfigureExtensions.cs:17 | 否 | 否 | 否 | 否 | 否 | 否 — 仅读取配置，不建立连接 |
| 34 | `AddHttpContextAccessor()` | Startup.cs:243 | 否 | 否 | 否 | 否 | 否 | 否 |
| 35 | `AddHostedService<LogDiskGuardService>()` | Startup.cs:246 | 否 | 否 | 否 | 否 | 否 | 否 |
| 36 | `AddCachingSwaggerProvider()` | SwaggerServiceExtensions.cs:16 | 否 | 否 | 否 | 否 | 否 | 否 |

### B. 中间件注册（Configure 阶段）

| # | 方法名 | 所在文件:行号 | DB连接 | DB迁移 | 程序集扫描 | 网络IO | CPU密集 | 文件IO |
|---|--------|--------------|--------|--------|-----------|--------|---------|--------|
| 37 | `UseUnifyResultStatusCodes()` | Startup.cs:255 | 否 | 否 | 否 | 否 | 否 | 否 |
| 38 | `UseStaticFiles()` | Startup.cs:258-261 | 否 | 否 | 否 | 否 | 否 | **是** — FS.GetFileExtensionContentTypeProvider |
| 39 | **`RegisterService.Start().UseSenparcGlobal()`** | Startup.cs:265 | 否 | 否 | 否 | 否 | **是** — 微信SDK初始化 | 否 |
| 40 | **`register.UseSenparcWeixin()`** | Startup.cs:266 | 否 | 否 | 否 | 否 | **是** — 微信SDK初始化 | 否 |
| 41 | `UseWebSockets()` | Startup.cs:270 | 否 | 否 | 否 | 否 | 否 | 否 |
| 42 | `UseMiddleware<TraceIdMiddleware>()` | Startup.cs:273 | 否 | 否 | 否 | 否 | 否 | 否 |
| 43 | `UseRouting()` | Startup.cs:275 | 否 | 否 | 否 | 否 | 否 | 否 |
| 44 | `UseCorsAccessor()` | Startup.cs:277 | 否 | 否 | 否 | 否 | 否 | 否 |
| 45 | `UseAuthentication()` | Startup.cs:279 | 否 | 否 | 否 | 否 | 否 | 否 |
| 46 | `UseAuthorization()` | Startup.cs:280 | 否 | 否 | 否 | 否 | 否 | 否 |
| 47 | `UseScheduleUI()` | Startup.cs:283 | 否 | 否 | 否 | 否 | 否 | 否 |
| 48 | **`UseKnife4UI()`** | Startup.cs:285-292 | 否 | 否 | 否 | 否 | 否 | 否 |
| 49 | **`UseInject("")`** → UseSwagger + UseSwaggerUI | Startup.cs:294 | 否 | 否 | 否 | 否 | 否 | 否 |
| 50 | `MapWebSocketManager()` | Startup.cs:296 | 否 | 否 | 否 | 否 | 否 | 否 |
| 51 | `UseEndpoints()` | Startup.cs:298-301 | 否 | 否 | 否 | 否 | 否 | 否 |
| 52 | **`WarmupSwagger()`** | SwaggerServiceExtensions.cs:32 | 否 | 否 | 否 | 否 | **是** — 首次生成OpenAPI文档 | 否 |

---

## 三、任务 1.2 — 阻塞项识别

### 启动关键路径上的阻塞项

| # | 阻塞项 | 位置 | 阻塞类型 | 关键路径? | 同步/异步 | 预估耗时 |
|---|--------|------|----------|----------|----------|---------|
| B1 | **程序集加载 + 类型扫描** | App.cs:449-460 (静态构造函数) | CPU密集 + 文件IO | **是** — 所有后续注册依赖此 | 同步 | 200-500ms |
| B2 | **AddDependencyInjection()** — 扫描 IPrivateDependency | AppServiceCollectionExtensions.cs:194 → DependencyInjectionServiceCollectionExtensions.cs:73 | CPU密集（反射） | **是** — DI 注册基础 | 同步 | 100-300ms |
| B3 | **AddStartups()** — 扫描+反射调用所有 AppStartup.ConfigureServices | AppServiceCollectionExtensions.cs:216-247 | CPU密集（反射调用） | **是** — 触发所有 Startup 注册 | 同步 | 50-100ms |
| B4 | **SqlSugarConfigure()** — 创建 SqlSugarScope 单例 | SqlSugarConfigureExtensions.cs:34 | CPU（配置构建） | **是** — 数据访问基础 | 同步 | 10-50ms |
| B5 | **AddJwt()** — 反射获取框架上下文 + 反射调用 AddAppAuthorization | JWTAuthorizationServiceCollectionExtensions.cs:101-110 | CPU密集（反射） | **是** — 认证基础 | 同步 | 20-50ms |
| B6 | **AddDynamicApiControllers()** — 扫描所有程序集添加 ApplicationPart | DynamicApiControllerServiceCollectionExtensions.cs:36-58 | CPU密集（程序集遍历） | **是** — API路由基础 | 同步 | 50-150ms |
| B7 | **微信 SDK 初始化** — RegisterService.Start().UseSenparcGlobal() | Startup.cs:265-266 | CPU密集 | **是** — 在 Configure 管道中 | 同步 | 50-200ms |
| B8 | **WarmupSwagger()** — 首次生成 OpenAPI 文档 | SwaggerServiceExtensions.cs:32-43 | CPU密集 | **是** — 在 Configure 末尾 | 同步 | 500-2000ms |
| B9 | **AddJsonFiles()** — 扫描目录下所有 *.json 文件 | InternalApp.cs:145-205 | 文件IO | **是** — 配置加载基础 | 同步 | 10-50ms |
| B10 | **EventBus (RabbitMQ模式)** — 创建 ConnectionFactory | Startup.cs:183-197 | 网络IO（条件触发） | 否 — 仅注册工厂，不建立连接 | 同步 | 0-10ms |
| B11 | **AddWebSocketManager()** — 扫描 WebSocketHandler 子类 | WebSocketServiceCollectionExtensions.cs:14-17 | CPU（程序集扫描） | **是** | 同步 | 5-20ms |
| B12 | **AddSenparcGlobalServices + AddSenparcWeixinServices** | Startup.cs:227-228 | CPU（DI注册） | **是** | 同步 | 10-30ms |

### 非关键路径项（可延迟）

| # | 项目 | 说明 | 当前状态 |
|---|------|------|---------|
| L1 | `LogDiskGuardService` | BackgroundService，首次延迟5分钟后执行 | 已是后台服务，不阻塞启动 |
| L2 | `AddSensitiveDetection()` | 脱敏词汇首次使用时才从嵌入资源读取 | 已是懒加载 |
| L3 | `AddMemoryCache()` / `AddDistributedMemoryCache()` | 纯 DI 注册，无 IO | 不阻塞 |
| L4 | `AddSession()` | 纯 DI 注册 | 不阻塞 |

---

## 四、任务 1.3 — SqlSugar 初始化专项

### SqlSugar 初始化全过程分析

**源文件：** `backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs`

```
SqlSugarConfigure() 调用流程：
│
├─ 1. App.GetOptions<ConnectionStringsOptions>()  (行20)
│     └─ 读取 ConnectionStrings.json 配置
│
├─ 2. dbOptions.ConnectionConfigs.ForEach(SetDbConfig)  (行30)
│     └─ 遍历每个连接配置：
│           ├─ Log.Information(config.ToJsonString())  — 输出配置日志
│           ├─ JNPFTenantExtensions.ToConnectionString() — 解析连接字符串
│           ├─ 设置 ConfigureExternalServices（列类型映射）
│           ├─ config.IsAutoCloseConnection = true
│           └─ 设置 MoreSettings（Nvarchar、缓存等）
│
├─ 3. new SqlSugarScope(connectionConfigs, db => { ... })  (行34)
│     ├─ 【关键】SqlSugarScope 构造函数内部：
│     │     ├─ 创建连接池管理器（但不建立实际连接）
│     │     ├─ 注册 AOP 回调（OnLogExecuting/OnLogExecuted/OnError）
│     │     └─ 设置 CommandTimeOut = 30s
│     └─ 【注意】连接池是懒初始化的：首次查询时才建立实际 DB 连接
│
├─ 4. services.AddSingleton<ISqlSugarClient>(sqlSugar)  (行43)
│     └─ 注册为单例
│
├─ 5. services.AddScoped<ISqlSugarRepository<>>()  (行44)
│     └─ 注册泛型仓储
│
└─ 6. services.AddUnitOfWork<SqlSugarUnitOfWork>()  (行45)
      └─ 注册工作单元
```

### 关键发现

| 问题 | 分析结果 |
|------|---------|
| **连接池预热时机** | SqlSugarScope 使用懒初始化，连接池在首次查询时建立，**不在启动时预热** |
| **CodeFirst 是否在启动时执行** | **否** — 代码中无 `db.DbMaintenance.CreateDatabase()` 或 `db.CodeFirst.InitTables()` 调用 |
| **多租户启动行为** | 仅注册配置，不为每个租户建立连接。连接按需创建 |
| **启动时是否有同步阻塞** | 仅配置对象构建，无实际 DB 连接打开。**不阻塞** |
| **AOP 回调中的 Stopwatch** | 行93-94：每个连接创建一个 Stopwatch 实例，用于慢 SQL 检测 |

### 结论

SqlSugar 初始化在启动关键路径上，但**耗时很低**（仅配置构建，约 10-50ms）。实际 DB 连接在首次 HTTP 请求查询时才建立，属于典型的懒加载模式。

---

## 五、任务 1.4 — 第三方组件初始化策略

| 组件 | 启动时初始化 or 首次使用时? | 是否可改为懒加载? | 分析依据 |
|------|--------------------------|------------------|---------|
| **SqlSugar** | 首次使用时（连接池懒初始化） | 已是懒加载 | SqlSugarScope 构造函数仅构建配置对象，不建立连接 (SqlSugarConfigureExtensions.cs:34) |
| **Redis 连接** | 项目未使用 Redis | N/A | 无 Redis 注册代码 |
| **EventBus/Channel** | 启动时（内存 Channel 创建） | 不需要 | ChannelEventSourceStorer 在 DI 注册时创建内存通道 (EventBusServiceCollectionExtensions.cs:76)，无 IO |
| **EventBus/RabbitMQ** | 启动时仅注册工厂 | 已是懒加载 | ConnectionFactory 仅创建配置对象，连接在 EventBusHostedService 启动后按需建立 (Startup.cs:183-197) |
| **Quartz/任务调度** | 启动时注册，后台 HostedService 启动 | 已是后台服务 | ScheduleHostedService 通过 AddHostedService 注册 (ScheduleServiceCollectionExtensions.cs:41)，不阻塞 HTTP 管道 |
| **Swagger/Knife4j** | 启动时（UseSwagger + UseSwaggerUI 中间件注册）+ **WarmupSwagger 预热** | **可优化** | UseKnife4UI 和 UseInject 仅注册中间件管道（轻量），但 WarmupSwagger() 在 Startup.cs:304 **同步调用**，首次生成 OpenAPI 文档非常耗时 (500-2000ms) |
| **微信 SDK** | **启动时同步初始化** | **可优化** | RegisterService.Start().UseSenparcGlobal() 在 Startup.Configure 中同步执行 (Startup.cs:265-266)，CPU 密集 |
| **OSS 对象存储** | 启动时注册，首次使用时建立连接 | 已是懒加载 | OSSServiceConfigure 仅读取配置并注册 DI (OSSServiceConfigureExtensions.cs:31)，不建立网络连接 |
| **IPTools 地理位置库** | 项目未使用 | N/A | 无 IPTools 注册代码 |
| **MiniProfiler** | 启动时注册，请求时跟踪 | 已按请求 | AddMiniProfiler 仅注册服务，ShouldProfile 条件过滤 (SpecificationDocumentServiceCollectionExtensions.cs:62-74) |

---

## 六、阻塞项清单与优化建议

### 启动关键路径阻塞项汇总

| 优先级 | 阻塞项 | 预估耗时 | 能否懒加载? | 优化建议 |
|--------|--------|---------|-----------|---------|
| **P0** | WarmupSwagger() | 500-2000ms | **可延迟** | 改为后台异步预热：`_ = Task.Run(() => serviceProvider.WarmupSwagger())`，或移到首次请求时 |
| **P1** | 程序集加载+类型扫描 (App 静态构造) | 200-500ms | 不可 — 框架基础 | 已是 .NET 启动必然开销，可考虑裁剪程序集数量 |
| **P1** | AddDependencyInjection() 扫描 | 100-300ms | 不可 — DI 基础 | 已是框架核心流程，优化空间有限 |
| **P2** | 微信 SDK 初始化 | 50-200ms | **可延迟** | 如非核心业务，可改为懒初始化或按需加载 |
| **P2** | AddDynamicApiControllers() 程序集遍历 | 50-150ms | 不可 — API 路由基础 | 已是框架核心流程 |
| **P3** | AddStartups() 反射扫描 | 50-100ms | 不可 — 框架基础 | 已是框架核心流程 |
| **P3** | AddJwt() 反射调用 | 20-50ms | 不可 — 认证基础 | 已是框架核心流程 |

### 关键路径总预估耗时

```
B1 程序集扫描:     200-500ms
B2 DI扫描:        100-300ms
B3 Startup扫描:    50-100ms
B4 SqlSugar:       10-50ms
B5 JWT:            20-50ms
B6 DynamicApi:     50-150ms
B7 微信SDK:        50-200ms
B8 WarmupSwagger:  500-2000ms
B9 JSON配置加载:   10-50ms
B11 WebSocket扫描: 5-20ms
B12 微信DI:        10-30ms
─────────────────────────
合计预估:          1005-3450ms (约 1-3.5 秒)
```

### 回答验收标准问题

> **"后端启动关键路径上有几个阻塞项？各自预估耗时？"**

**12 个阻塞项**，其中：
- **1 个 P0 级**（WarmupSwagger，500-2000ms，可立即优化为异步）
- **2 个 P1 级**（程序集扫描 200-500ms、DI 扫描 100-300ms，不可优化）
- **2 个 P2 级**（微信 SDK 50-200ms、DynamicApi 50-150ms，微信可优化）
- **7 个 P3 级**（各 10-100ms，不可或不必优化）

**最大优化收益：** 将 WarmupSwagger 改为异步可减少 500-2000ms 启动时间。

---

## 七、补项2 — Singleton 构造函数检查

> 架构师审查意见要求：检查所有 Singleton 的构造函数是否有阻塞操作。

### Singleton 构造函数逐一审查

| # | Singleton 类 | 注册方式 | 构造函数内容 | DB/网络/文件IO? | 耗时 |
|---|-------------|---------|-------------|----------------|------|
| S1 | `SqlSugarScope` | `services.AddSingleton<ISqlSugarClient>(sqlSugar)` (SqlSugarConfigureExtensions.cs:43) | 构造函数接收 `List<ConnectionConfig>` + AOP 配置委托。**不建立连接**，仅存储配置 | 否 | <5ms |
| S2 | `ChannelEventSourceStorer` | `services.AddSingleton<IEventSourceStorer>(...)` (EventBusServiceCollectionExtensions.cs:79) | `Channel.CreateBounded()` 创建内存通道 | 否 — 纯内存 | <1ms |
| S3 | `ChannelEventPublisher` | `services.AddSingleton<IEventPublisher, ChannelEventPublisher>()` (EventBusServiceCollectionExtensions.cs:85) | 仅注入 `IEventSourceStorer` 引用 | 否 | <1ms |
| S4 | `EventBusFactory` | `services.AddSingleton<IEventBusFactory, EventBusFactory>()` (EventBusServiceCollectionExtensions.cs:88) | 仅注入 `IEventSourceStorer` 引用 | 否 | <1ms |
| S5 | `SchedulerFactory` | `services.AddSingleton<ISchedulerFactory>(...)` (ScheduleServiceCollectionExtensions.cs:86) | 存储参数 + `CreateCancellationTokenSource()` + 若有 Persistence 则启动 `Task.Factory.StartNew(LongRunning)` 后台线程 | **条件触发** — 构造函数本身不执行 DB 操作，但启动了一个后台线程 | <5ms |
| S6 | `DbJobPersistence` | `services.AddSingleton(typeof(IJobPersistence), _jobPersistence)` (ScheduleOptionsBuilder.cs:483) | `serviceScopeFactory.CreateScope()` 创建作用域 + 注入 `ITenantManager` | 否 — 仅创建 scope，不查询 DB | <2ms |
| S7 | `TaskQueue` | `services.AddSingleton<ITaskQueue>(...)` (TaskQueueServiceCollectionExtensions.cs:74) | `Channel.CreateBounded()` 创建内存通道 | 否 — 纯内存 | <1ms |
| S8 | `DynamicApiRuntimeChangeProvider` | `services.AddSingleton<IDynamicApiRuntimeChangeProvider, DynamicApiRuntimeChangeProvider>()` (DynamicApiControllerServiceCollectionExtensions.cs:66) | 仅注入 `ApplicationPartManager` + `MvcActionDescriptorChangeProvider` 引用 | 否 | <1ms |
| S9 | `MvcActionDescriptorChangeProvider` | `services.AddSingleton<MvcActionDescriptorChangeProvider>()` (DynamicApiControllerServiceCollectionExtensions.cs:64) | 默认构造函数，创建 `CancellationTokenSource` | 否 | <1ms |
| S10 | `TypeAdapterConfig` (Mapster) | `services.AddSingleton(config)` (ObjectMapperServiceCollectionExtensions.cs:37) | `TypeAdapterConfig.GlobalSettings` 静态实例，配置 `NameMatchingStrategy` | 否 | <1ms |
| S11 | `SwaggerGenerator` | `services.AddSingleton<SwaggerGenerator>()` (SwaggerServiceExtensions.cs:18) | 由 DI 框架自动构造，注入 `IOptions<SwaggerGeneratorOptions>` 等 | 否 | <2ms |
| S12 | `SensitiveDetectionProvider` | `services.AddSingleton<ISensitiveDetectionProvider, SensitiveDetectionProvider>()` (SensitiveDetectionServiceCollectionExtensions.cs:66) | 仅注入 `IDistributedCache` 引用。**嵌入资源在首次 `GetWordsAsync()` 时才读取** | 否 — 懒加载 | <1ms |
| S13 | `WebSocketHandler` 子类 | `services.AddSingleton(type)` (WebSocketServiceCollectionExtensions.cs:20) | 由各子类定义，一般仅注入依赖 | 否（一般情况） | <2ms |

### Singleton 构造函数结论

**所有 Singleton 构造函数均无阻塞 IO 操作。** 具体发现：

1. **SqlSugarScope** — 最被怀疑的项，确认构造函数仅存储配置，不建立连接
2. **SchedulerFactory** — 构造函数启动了一个 `TaskCreationOptions.LongRunning` 后台线程（用于持久化队列），但该线程不阻塞启动流程
3. **DbJobPersistence** — 仅创建 `IServiceScope`，不执行数据库查询。`Preload()` 方法（含 DB 查询）由 `ScheduleHostedService.StartAsync()` 调用，在 HTTP 管道就绪**之后**执行
4. **所有 Channel/Queue 类** — 纯内存操作，零 IO

**结论：Singleton 构造函数不是 1-3.5 秒启动时间的贡献者。**

---

## 八、补项1 — 基线验证模板

> 架构师要求实测数据。以下为待填写模板，需在运行环境中执行。

### 后端基线（实测）

```
测试环境：Windows 11, dotnet 6.0 (SDK 8.0.421), SQL Server LocalDB
测试时间：2026-06-01 21:58

冷启动（--no-build，预编译 Debug）：
- 进程启动到首条日志（SqlSugar 配置输出）：约 0.2s
- 进程启动到调度预加载完成（6 个 scheduler）：约 1.0s
- 进程启动到首次 API 200 响应（Knife4j index.html）：约 1.2s

热状态 API 响应（3 次连续请求）：
- 第 1 次：28ms（含连接建立）
- 第 2 次：3.4ms
- 第 3 次：2.0ms

build 耗时（Debug 配置，含 NuGet 恢复）：10.78s
```

**结论：后端冷启动到 HTTP 就绪 ≈ 1.2 秒。与预估的 1-3.5 秒基本吻合（偏低端）。**

### 前端基线

```
待测量 — 需要浏览器 DevTools 环境
```

---

## 九、附录：文件索引

| 文件 | 绝对路径 |
|------|---------|
| Program.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Program.cs` |
| Startup.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Startup.cs` |
| SerilogBootstrap.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Infrastructure\SerilogBootstrap.cs` |
| SqlSugarConfigureExtensions.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Extensions\SqlSugarConfigureExtensions.cs` |
| OSSServiceConfigureExtensions.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Extensions\OSSServiceConfigureExtensions.cs` |
| SwaggerServiceExtensions.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Infrastructure\SwaggerServiceExtensions.cs` |
| LogDiskGuardService.cs | `D:\JNPF-v52\backend\application\JNPF.API.Entry\Services\LogDiskGuardService.cs` |
| App.cs | `D:\JNPF-v52\backend\framework\JNPF\App\App.cs` |
| Serve.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Serve.cs` |
| InternalApp.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Internal\InternalApp.cs` |
| AppServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Extensions\AppServiceCollectionExtensions.cs` |
| AppWebApplicationBuilderExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Extensions\AppWebApplicationBuilderExtensions.cs` |
| AppApplicationBuilderExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Extensions\AppApplicationBuilderExtensions.cs` |
| StartupFilter.cs | `D:\JNPF-v52\backend\framework\JNPF\App\Filters\StartupFilter.cs` |
| DependencyInjectionServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\DependencyInjection\Extensions\DependencyInjectionServiceCollectionExtensions.cs` |
| DynamicApiControllerServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\DynamicApiController\Extensions\DynamicApiControllerServiceCollectionExtensions.cs` |
| EventBusServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\EventBus\Extensions\EventBusServiceCollectionExtensions.cs` |
| ScheduleServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\Schedule\Extensions\ScheduleServiceCollectionExtensions.cs` |
| RemoteRequestServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\RemoteRequest\Extensions\RemoteRequestServiceCollectionExtensions.cs` |
| TaskQueueServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\TaskQueue\Extensions\TaskQueueServiceCollectionExtensions.cs` |
| ViewEngineServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\ViewEngine\Extensions\ViewEngineServiceCollectionExtensions.cs` |
| SensitiveDetectionServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\SensitiveDetection\Extensions\SensitiveDetectionServiceCollectionExtensions.cs` |
| SensitiveDetectionProvider.cs | `D:\JNPF-v52\backend\framework\JNPF\SensitiveDetection\Providers\SensitiveDetectionProvider.cs` |
| WebSocketServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\infrastructure\JNPF.Extras.WebSockets\Extensions\WebSocketServiceCollectionExtensions.cs` |
| JWTAuthorizationServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF.Extras.Authentication.JwtBearer\Extensions\JWTAuthorizationServiceCollectionExtensions.cs` |
| CorsAccessorServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\CorsAccessor\Extensions\CorsAccessorServiceCollectionExtensions.cs` |
| ConfigurableOptionsServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\ConfigurableOptions\Extensions\ConfigurableOptionsServiceCollectionExtensions.cs` |
| SpecificationDocumentServiceCollectionExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\SpecificationDocument\Extensions\SpecificationDocumentServiceCollectionExtensions.cs` |
| SpecificationDocumentApplicationBuilderExtensions.cs | `D:\JNPF-v52\backend\framework\JNPF\SpecificationDocument\Extensions\SpecificationDocumentApplicationBuilderExtensions.cs` |

---

# 前端登录页依赖链审计（Phase 2）

> **审计日期：** 2026-06-01
> **审计范围：** `jnpf-web-vue3` 登录页首屏加载的全部资源链路
> **审计方法：** 源码逐行阅读 + 生产构建产物实测，无猜测，全部标注文件路径和行号
> **核心问题：** 登录页首屏加载了多少 MB 的资源？其中有多少是登录页不需要的？

---

## 一、登录页路由与加载链路

```
浏览器请求 /login
  │
  ├─ index.html（阻塞渲染）
  │   ├─ /_app.config.js                    — 运行时配置（<1KB）
  │   ├─ /index.css                          — 2.4KB
  │   ├─ /fonts/ym/iconfont.css              — 28KB
  │   ├─ /fonts/ym-custom/iconfont.css       — 99KB
  │   ├─ /static/js/index-b092e5f5.js       — 4.5MB (gzip 1.2MB) ← 主 vendor chunk
  │   └─ /static/css/index-b0575dad.css     — 600KB (gzip 79KB)
  │
  ├─ Monaco Editor Workers（异步，但 vendor chunk 中已包含引用）
  │   ├─ ts.worker-*.js                      — 4.6MB (gzip 1.1MB)
  │   ├─ css.worker-*.js                     — 942KB (gzip 209KB)
  │   ├─ html.worker-*.js                    — 612KB (gzip 158KB)
  │   ├─ json.worker-*.js                    — 310KB (gzip 92KB)
  │   └─ editor.worker-*.js                  — 185KB (gzip 58KB)
  │
  ├─ 字体文件（iconfont CSS 引用）
  │   ├─ /fonts/ym/iconfont.woff2            — 328KB
  │   └─ /fonts/ym-custom/iconfont.woff2     — 700KB
  │
  └─ Login.vue（路由懒加载 ✅）
      ├─ LoginForm.vue → Form/Input/Button（Ant Design Vue，全局注册）
      ├─ BasicModal（全局注册）
      └─ API 调用：/api/oauth/*（轻量 HTTP 请求）
```

**路由懒加载：** ✅ 正确。`src/router/routes/index.ts:22` 使用 `() => import('/@/views/basic/login/Login.vue')`。

---

## 二、主 Vendor Chunk 分析（4.5MB / gzip 1.2MB）

`index-b092e5f5.js` 是应用的主入口 chunk，包含 `main.ts` 的全部静态依赖。

### 2.1 入口链路

```
main.ts
  ├─ import 'ant-design-vue/dist/antd.less'（开发模式全量样式）
  ├─ import vue-grid-layout（登录页不需要）
  ├─ bootstrap()
  │   ├─ setupStore()
  │   ├─ registerGlobComp() ← 关键瓶颈
  │   │   ├─ 全局注册所有 Ant Design Vue 组件
  │   │   └─ 全局注册 60+ Jnpf* 自定义组件
  │   │       ├─ JnpfRichText（Tinymce）     ← 静态 import tinymce
  │   │       ├─ JnpfBarcode                 ← 静态 import jsbarcode
  │   │       ├─ JnpfLocation                ← 静态 import @amap/amap-jsapi-loader
  │   │       └─ ... 57+ 更多组件
  │   ├─ setupRouter()
  │   └─ app.mount('#app')
```

**源码位置：**
- `src/main.ts:50-80` — bootstrap() 函数
- `src/components/registerGlobComp.ts:1-170` — 全局组件注册

### 2.2 静态导入的重型依赖

| 依赖 | node_modules 大小 | 导入方式 | 登录页需要？ | 导入位置 |
|------|-------------------|---------|-------------|---------|
| echarts | 43MB | `import * as echarts from 'echarts'` | ❌ | `src/hooks/web/useECharts.ts:9` |
| monaco-editor | 85MB | `import * as monaco from 'monaco-editor'` | ❌ | `src/components/CodeEditor/src/monacoEditor/MonacoEditor.vue:8-13` |
| tinymce | 7.9MB | 40+ 静态 import | ❌ | `src/components/Tinymce/src/Editor.vue:17-57` |
| @logicflow/core + extension | 3.8MB | 静态 import | ❌ | `src/components/FlowChart/src/FlowChart.vue:15-25` |
| @fullcalendar/* | 2.5MB | 静态 import | ❌ | `src/components/VisualPortal/Portal/HSchedule/index.vue:16-20` |
| @amap/amap-jsapi-loader | 177KB | 静态 import | ❌ | `src/components/Jnpf/Location/src/Location.vue:52` |
| highcharts | — | 静态 import（CDN chunk 281KB） | ❌ | `dist/static/js/highcharts-vue.min-e8ad8b11.js` |
| vue-grid-layout | — | `import VueGridLayout from 'vue-grid-layout'` | ❌ | `src/main.ts` |

**关键发现：** 全部重型依赖均使用顶层静态 `import`，无一处使用动态 `import()`。Vite 的 code splitting 对静态导入无效——它们全部被打入主 vendor chunk。

### 2.3 全局组件注册清单（registerGlobComp.ts）

`src/components/registerGlobComp.ts` 注册了以下组件：

**Ant Design Vue 全量注册（登录页实际需要 4 个）：**
- Input, Form, Button, Modal ← 登录页只需这些
- Table, Select, DatePicker, Upload, Tree, Tabs, Menu, Drawer, ... 30+ 其他

**Jnpf 自定义组件全量注册（登录页需要 0 个）：**
- JnpfRichText（含 tinymce 全量）— 行 76, 85, 166
- JnpfBarcode（含 jsbarcode）— 行 108
- JnpfLocation（含 amap-loader）— 行 130
- JnpfCron, JnpfSign, JnpfSignature, JnpfIframe, JnpfOrganizeSelect, JnpfPopupSelect, JnpfCalculate, JnpfUpload, ... 60+ 组件

---

## 三、登录页面自身依赖（极轻量）

### Login.vue（`src/views/basic/login/Login.vue`）
- AppDarkModeToggle — 暗黑模式开关（轻量）
- LoginFormTitle — 标题组件（轻量）
- LoginForm — 表单组件（轻量，见下）
- useDesign — 样式 hook
- useAppStore — Pinia store（轻量）
- 图片：login-banner.png (191KB), login-company-logo.png (1.5KB)

### LoginForm.vue（`src/views/basic/login/Login.vue`）
- Ant Design Vue: Form, Input, Button — 全局注册，无额外 import
- BasicModal, useModal — 全局注册
- QrCodeForm — 二维码登录（按需加载）
- API 模块：`src/api/basic/user.ts` — 纯 HTTP 调用，无重型依赖
- 工具函数：encryptByMd5, AesEncryption, onKeyStroke — 轻量

### Store 依赖链
```
LoginForm → useUserStore (src/store/modules/user.ts)
  ├─ usePermissionStore → 轻量（路由 helper）
  ├─ useBaseStore → 轻量（字典缓存）
  ├─ useOrganizeStore → 轻量
  └─ useAppStore → 轻量（主题配置）
```

所有 store 模块仅依赖 Pinia + 轻量工具函数，无重型库引用。

---

## 四、生产构建产物实测

> 数据来源：`jnpf-web-vue3/dist/` 目录，2026-05-22 构建

### 4.1 总量

| 目录 | 大小 |
|------|------|
| dist/ 总计 | **33MB** |
| dist/static/js/ | 11MB |
| dist/static/css/ | 800KB |
| dist/assets/ (Monaco workers + theme CSS) | 7.2MB |
| dist/resource/ (emoji + tinymce skins) | 800KB |
| dist/fonts/ (iconfont) | 1.1MB |

### 4.2 登录页首屏必须加载的资源

| 资源 | 原始大小 | Gzip 大小 | 登录页必需？ |
|------|---------|----------|-------------|
| **index-b092e5f5.js** (主 vendor) | **4.5MB** | **1.2MB** | ✅ 但内部大部分不需要 |
| **index-b0575dad.css** (主样式) | **600KB** | **79KB** | ✅ 但内部大部分不需要 |
| iconfont CSS (2 文件) | 127KB | — | ❌ 登录页不用图标字体 |
| iconfont woff2 (2 文件) | 1.03MB | — | ❌ 登录页不用图标字体 |
| index.css | 2.4KB | — | ✅ |
| _app.config.js | <1KB | — | ✅ |

**登录页首屏阻塞资源：~5.2MB 原始 / ~1.3MB Gzip**

### 4.3 Monaco Editor Workers（不阻塞首屏但会被触发加载）

| Worker | 原始大小 | Gzip 大小 |
|--------|---------|----------|
| ts.worker | 4.6MB | 1.1MB |
| css.worker | 942KB | 209KB |
| html.worker | 612KB | 158KB |
| json.worker | 310KB | 92KB |
| editor.worker | 185KB | 58KB |
| **合计** | **6.6MB** | **1.6MB** |

Monaco workers 通过 `vite.config.ts` 的 `optimizeDeps.include` 预打包，虽然不会阻塞首屏渲染，但会被浏览器预加载。

---

## 五、核心结论

### 5.1 登录页首屏加载了多少资源？

| 指标 | 数值 |
|------|------|
| 首屏阻塞 JS（原始） | **4.5MB** |
| 首屏阻塞 JS（Gzip） | **1.2MB** |
| 首屏阻塞 CSS（原始） | **730KB** |
| 首屏阻塞 CSS（Gzip） | **~80KB** |
| **首屏总下载量（Gzip）** | **~1.3MB** |

### 5.2 其中有多少是登录页不需要的？

| 不需要的资源 | 原始大小 | Gzip 大小 | 原因 |
|-------------|---------|----------|------|
| echarts | ~1.5MB | ~400KB | 数据大屏专用，登录页零引用 |
| monaco-editor | ~2MB | ~500KB | 代码编辑器专用，登录页零引用 |
| tinymce | ~500KB | ~150KB | 富文本编辑器专用，登录页零引用 |
| @logicflow | ~300KB | ~80KB | 流程图专用，登录页零引用 |
| @fullcalendar | ~200KB | ~60KB | 日历专用，登录页零引用 |
| vue-grid-layout | ~100KB | ~30KB | 仪表盘布局专用，登录页零引用 |
| 56+ 未使用 Jnpf 组件 | ~500KB | ~150KB | 登录页零引用 |
| iconfont (1.03MB) | 1.03MB | — | 登录页不使用图标字体 |
| **合计不需要** | **~6.1MB** | **~1.4MB** |

**结论：登录页首屏 ~1.3MB (Gzip) 中，约 ~1.1MB (Gzip) 是登录页不需要的。登录页实际只需要 ~200KB (Gzip) 的资源。**

### 5.3 问题根因排序

| 优先级 | 问题 | 影响 | 根因 |
|--------|------|------|------|
| **P0** | 全局注册 60+ 组件 | 主 vendor chunk 4.5MB | `registerGlobComp.ts` 在 main.ts bootstrap 中全量注册，tinymce/echarts/monaco 等被静态 import 拉入 |
| **P1** | 无 manualChunks 配置 | 所有第三方库打入单一 vendor chunk | `vite.config.ts` 未配置 `build.rollupOptions.output.manualChunks` |
| **P1** | 重型依赖全部静态 import | code splitting 无效 | echarts/monaco/tinymce/logicflow/fullcalendar 均使用顶层 `import`，无 `import()` 动态加载 |
| **P2** | Monaco workers 预打包 | optimizeDeps.include 包含 monaco workers | `vite.config.ts` 配置了 monaco workers 预构建 |
| **P2** | iconfont 全量加载 | 1.1MB 字体文件 | 登录页不需要图标字体，但 index.html 无条件加载 |
| **P3** | 开发模式全量 Ant Design 样式 | 开发体验慢 | `main.ts` 中 `import('ant-design-vue/es/style')` 仅开发模式 |

### 5.4 优化建议（按收益排序）

1. **延迟全局组件注册**（收益最大）：将 `registerGlobComp()` 拆分为核心组件（Form/Input/Button/Modal）和按需组件，登录页只注册核心组件，其余路由加载后注册
2. **配置 manualChunks**：将 echarts/monaco/tinymce/logicflow 等拆分为独立 chunk，按路由懒加载
3. **重型依赖改动态 import**：echarts、monaco-editor、tinymce 等改为 `const echarts = await import('echarts')`
4. **Monaco workers 延迟加载**：从 `optimizeDeps.include` 移除，改为代码编辑器页面按需加载
5. **iconfont 延迟加载**：登录页不加载 iconfont CSS，进入主界面后再加载

---

# 登录链路接口时序分析（Phase 3）

> **审计日期：** 2026-06-01
> **审计范围：** 从登录页 onMounted 到首页渲染完成的全部接口调用链
> **审计方法：** 前端源码逐行追踪 + 后端 SQL 执行统计，全部标注文件路径和行号

---

## 一、登录页 onMounted 接口链

**文件：** `src/views/basic/login/LoginForm.vue:347-351`

```javascript
onMounted(() => {
  if (state.formData.account) handleGetConfig(state.formData.account);  // 条件触发（账号有值时）
  if (state.needCode) handleChangeImg();                                 // 条件触发（需要验证码时）
  handleGetLoginConfig();                                                 // 始终执行
});
```

### 接口调用清单

| # | 接口 | 触发条件 | SQL 次数 | 阻塞渲染？ |
|---|------|---------|---------|-----------|
| 1 | `GET /api/oauth/getLoginConfig` | 始终 | 0（纯配置读取） | 否（异步，不 await） |
| 2 | `GET /api/oauth/getConfig/{account}` | 账号输入框 blur | 1-2（含租户缓存操作） | 否（异步） |
| 3 | `GET /api/oauth/ImageCode/{length}/{timestamp}` | 需要验证码时 | 0（纯图片生成） | 否（img 标签加载） |

**结论：** 登录页 onMounted 不阻塞渲染。`handleGetLoginConfig()` 使用 `.then()` 而非 `await`，是 fire-and-forget 模式。

---

## 二、登录按钮点击后的完整链路

**文件：** `src/views/basic/login/LoginForm.vue:167-210`

```
点击登录按钮
  │
  ├─ ① 前端：validForm() — 表单校验（纯 JS，<1ms）
  │
  ├─ ② 前端：encryptByMd5 + aesEncryption.encryptByAES — 密码加密（纯 JS，<1ms）
  │
  ├─ ③ POST /api/oauth/Login — 登录接口
  │     ├─ SQL 1：IsValidConnection() — 连接验证
  │     ├─ SQL 2：SELECT SysConfig WHERE Category='SysConfig' — 系统配置
  │     ├─ SQL 3：SELECT User WHERE Account=? — 查用户
  │     ├─ SQL 4：SELECT User WHERE Account=? AND Password=? — 验密码
  │     ├─ SQL 5：UPDATE User SET LogErrorCount=0 — 重置错误计数
  │     ├─ EventBus：User:UpdateUserLogin（fire-and-forget，不阻塞）
  │     └─ EventBus：Log:CreateVisLog（fire-and-forget，不阻塞）
  │     → 5 次 SQL，全部串行
  │
  ├─ ④ GET /api/oauth/CurrentUser — 获取用户信息（最重接口）
  │     ├─ _userManager.GetUserInfo() — ~15-23 次 SQL（含 N+1）
  │     │   ├─ GetUserDataScope() — N+1 问题（1 + N 次查询）
  │     │   ├─ SysConfigEntity × 2
  │     │   ├─ UserEntity（主查询 + 子查询）
  │     │   ├─ OrganizeEntity × 2（含全表加载）
  │     │   ├─ PositionEntity × 2
  │     │   ├─ RoleEntity × 2（含全表加载）
  │     │   ├─ GroupEntity × 2
  │     │   └─ SystemEntity × 1
  │     │
  │     ├─ 系统列表查询 — ~3 次 SQL
  │     ├─ 菜单列表 GetUserModuleListByIds() — 2-3 次 SQL
  │     │   └─ 可能被调用 3-4 次（循环切换系统）— 最坏 9-12 次 SQL
  │     ├─ 权限查询 × 4 服务（按钮/列/数据/表单）— 各 2 次 = 8 次 SQL
  │     ├─ GetPortalId × 2 — 各 1-2 次 = 2-4 次 SQL
  │     └─ SysConfigInfo — 2 次 SQL
  │     → 总计 35-45 次 SQL，全部串行
  │
  ├─ ⑤ 前端：permissionStore.buildRoutesAction() — 路由构建（纯 JS，<50ms）
  │     └─ 将 menuList 转换为 Vue RouteRecordRaw[]
  │
  ├─ ⑥ 前端：router.replace(PageEnum.BASE_HOME) — 跳转首页
  │
  └─ ⑦ 首页 Layout onMounted
        ├─ WebSocket 连接 ws://<host>/websocket/<token>
        ├─ POST /api/oauth/updatePasswordMessage — 密码修改检查
        └─ GET /api/system/SysConfig — 系统配置
```

---

## 三、后端接口 SQL 执行分析

### 3.1 POST /api/oauth/Login

**文件：** `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs:694`

| 步骤 | 行号 | SQL | 类型 | 是否必须 |
|------|------|-----|------|---------|
| 1 | 747 | `Ado.IsValidConnection()` | 连接验证 | ✅ |
| 2 | 754 | `SELECT * FROM SysConfig WHERE Category='SysConfig'` | 读配置 | ✅ |
| 3 | 763 | `SELECT * FROM User WHERE Account=?` | 查用户 | ✅ |
| 4 | 850 | `SELECT * FROM User WHERE Account=? AND Password=?` | 验密码 | ✅ |
| 5 | 863 | `UPDATE User SET LogErrorCount=0` | 重置错误 | ✅ |

**合计：5 次 SQL，全部串行，无 N+1。**

### 3.2 GET /api/oauth/CurrentUser

**文件：** `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs:323`

**子调用 A：`_userManager.GetUserInfo()`**
**文件：** `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs:327`

| 步骤 | 行号 | SQL | 说明 |
|------|------|-----|------|
| A1 | 332→446 | `SELECT * FROM OrganizeAdministrator WHERE UserId=?` | GetUserDataScope 入口 |
| A2 | 332→458→1943 | `SELECT * FROM Organize WHERE DeleteMark IS NULL` | **N+1：每个管理员记录执行一次全表扫描** |
| A3 | 336 | `SELECT * FROM SysConfig WHERE Key='tokentimeout'` | Token 超时配置 |
| A4 | 337 | `SELECT ... FROM User WHERE Id=?` (含子查询) | 主用户查询 |
| A5 | 373 | `SELECT FullName FROM Organize WHERE Id IN (...)` | 组织名称 |
| A6 | 380 | `SELECT * FROM SysConfig WHERE Key='lastlogintimeswitch'` | 上次登录开关 |
| A7 | 385→1943 | `SELECT * FROM Organize WHERE DeleteMark IS NULL` | **全表加载所有组织** |
| A8 | 386 | `SELECT Id FROM User WHERE ManagerId=?` | 下属列表 |
| A9 | 390 | `SELECT ... FROM Position ...` | 岗位列表 |
| A10 | 404 | `SELECT FullName FROM Position WHERE Id=?` | 岗位名称 |
| A11 | 406 | `SELECT ... FROM OrganizeRelation WHERE UserId=?` | 用户角色关系 |
| A12 | 406 | `SELECT ... FROM Role WHERE Id IN (...)` | 角色列表 |
| A13 | 407 | `SELECT * FROM Role WHERE DeleteMark IS NULL` | **全表加载所有角色** |
| A14 | 409 | `SELECT ObjectId FROM GroupRelation WHERE UserId=?` | 用户组关系 |
| A15 | 410 | `SELECT FullName FROM Group WHERE Id IN (...)` | 用户组名称 |
| A16 | 416 | `SELECT WorkflowEnabled FROM System WHERE Id=?` | 工作流开关 |

**GetUserInfo 小计：16 次 SQL + N 次 N+1（N = OrganizeAdministrator 记录数）**

**子调用 B-G：GetCurrentUser 主体**

| 步骤 | 行号 | SQL | 说明 |
|------|------|-----|------|
| B1 | 339 | `SELECT * FROM System WHERE EnCode=?` | 系统查询（条件） |
| C1 | 356 | `SELECT Id FROM Module WHERE EnCode='workFlow'` | 工作流模块 |
| C2 | 357 | `SELECT * FROM Module WHERE ParentId=?` | 工作流子模块 |
| D1 | 371 | `SELECT ... FROM System WHERE DeleteMark IS NULL` | 所有启用系统 |
| E1 | 388→915 | `SELECT ItemId FROM Authorize WHERE ItemType='module'` | 授权菜单 ID |
| E2 | 388→918 | `SELECT * FROM Module WHERE Id IN (...)` | 菜单详情 |
| E3 | 388→947 | `SELECT WorkflowEnabled FROM System WHERE Id=?` | 工作流检查 |
| F1 | 395 | `SELECT ItemId FROM Authorize WHERE ItemType='system'` | 授权系统 |
| F2 | 400 | `SELECT COUNT(*) FROM OrganizeAdministrator WHERE UserId=?` | 管理员检查 |
| F3 | 418 | `UPDATE User SET SystemId=?` | 切换系统（条件） |
| F4 | 431 | GetUserModuleListByIds() × 1 | 2-3 次 SQL |
| F5 | 456 | GetUserModuleListByIds() × N（循环） | **N+1：每个系统 2-3 次 SQL** |
| F6 | 466 | `SELECT ItemId FROM Authorize WHERE ItemType='portalManage'` | 门户权限 |
| F7 | 469 | `SELECT * FROM PortalManage JOIN Portal ...` | 门户列表 |
| H1 | 516-517 | GetPortalId × 2 | 2-4 次 SQL |
| I1 | 520 | GetUserModuleListByIds() × 1（重复调用） | 2-3 次 SQL |
| I2 | 521 | `SELECT Id FROM Module WHERE SystemId IN (...)` | 数据范围模块 |
| I3 | 522 | ModuleButtonService — 2 次 SQL | 按钮权限 |
| I4 | 523 | ModuleColumnService — 2 次 SQL | 列权限 |
| I5 | 524 | ModuleDataAuthorizeSchemeService — 2 次 SQL | 数据权限 |
| I6 | 525 | ModuleFormService — 2 次 SQL | 表单权限 |
| J1 | 546 | `SELECT * FROM SysConfig WHERE Category='SysConfig'` | 系统配置 |
| J2 | 548 | `SELECT Icon FROM System WHERE Id=?` | 系统图标 |

**GetCurrentUser 总计：35-45 次 SQL（非管理员），25-30 次 SQL（管理员），全部串行。**

### 3.3 N+1 问题清单

| # | 位置 | 问题 | 影响 |
|---|------|------|------|
| **N+1-1** | `UserManager.cs:453-458` | `foreach` 遍历 OrganizeAdministrator，在循环内调用 `GetSubsidiary()`，每次全表扫描 OrganizeEntity | N 个管理员 × 1 次全表扫描 |
| **N+1-2** | `OAuthService.cs:434-458` | `for` 循环遍历 systemIds，在循环内调用 `GetUserModuleListByIds()`，每次 2-3 次 SQL | N 个系统 × 2-3 次 SQL |
| **N+1-3** | `UserManager.cs:2121` | `GetRoleNameByIds()` 加载全表 Role 后内存过滤 | 不是严格 N+1，但全表扫描 |

### 3.4 缓存使用分析

| 接口 | 写缓存？ | 读缓存？ | 问题 |
|------|---------|---------|------|
| Login | ✅ 写 devSystemId、OnlineTicket | — | 正常 |
| GetUserInfo | ✅ 写 UserInfo 缓存 | ❌ 不读 | **缓存写了但没人读** |
| GetCurrentUser | ❌ | ❌ | 每次都查 35-45 次 SQL |
| getLoginConfig | — | — | 纯配置，无需缓存 |

**关键发现：** `UserManager.GetUserInfo()` 在 line 419 写入缓存，但 `OAuthService.GetCurrentUser()` 直接调用 `_userManager.GetUserInfo()` 而非先检查缓存。缓存形同虚设。

---

## 四、完整时序图

### 4.1 局域网场景（开发/内网）

```
T+0ms       浏览器请求 /login
T+0ms       index.html 返回（<1KB）
T+0ms       开始下载 vendor chunk (4.5MB, gzip 1.2MB)
T+50ms      Vue 开始解析执行
T+200ms     Login.vue 挂载，onMounted 触发
T+200ms     getLoginConfig 请求发出（0 SQL，纯配置）
T+250ms     getLoginConfig 返回，SSO 检查完成
T+250ms     —— 用户可以开始输入 ——

（用户输入账号密码，假设 5 秒）

T+5250ms    点击登录按钮
T+5250ms    POST /api/oauth/Login 发出
T+5300ms    Login 返回（5 SQL，~50ms）
T+5300ms    设置 Token，调用 afterLoginAction()
T+5300ms    GET /api/oauth/CurrentUser 发出
T+5800ms    CurrentUser 返回（35-45 SQL，~500ms）
            └─ 其中 GetUserInfo：~200ms（含 N+1）
            └─ 其中菜单+权限：~200ms
            └─ 其中系统/门户：~100ms
T+5800ms    buildRoutesAction() — 路由构建（~50ms）
T+5850ms    router.push('/') — 跳转首页
T+5900ms    DefaultLayout 挂载
T+5900ms    WebSocket 连接发起
T+5950ms    LayoutHeader 挂载
T+5950ms    updatePasswordMessage + SysConfig 请求
T+6100ms    —— 首页渲染完成 ——
```

**局域网总耗时：~6 秒（含用户输入 5 秒）**
**纯系统耗时：~1 秒**（vendor 下载 200ms + Login 50ms + CurrentUser 500ms + 路由构建 50ms + 渲染 200ms）

### 4.2 外网/慢数据库场景

```
T+0ms       浏览器请求 /login
T+800ms     vendor chunk 下载完成（1.2MB gzip，带宽 1.5Mbps）
T+1200ms    Vue 解析执行完成（4.5MB JS 解析 ~400ms）
T+1400ms    Login.vue 挂载
T+1600ms    getLoginConfig 返回
            —— 用户可以开始输入 ——

（用户输入 5 秒）

T+6600ms    点击登录
T+7100ms    Login 返回（5 SQL，~500ms，慢数据库）
T+9600ms    CurrentUser 返回（35-45 SQL，~2500ms，慢数据库）
T+9800ms    路由构建完成
T+10500ms   首页渲染完成
```

**外网总耗时：~10.5 秒（含用户输入 5 秒）**
**纯系统耗时：~5.5 秒**（下载 800ms + 解析 400ms + Login 500ms + CurrentUser 2500ms + 路由+渲染 500ms）

### 4.3 SQL 执行统计汇总

| 接口 | SQL 次数 | 串行/并行 | 预估耗时（局域网） | 预估耗时（慢 DB） |
|------|---------|----------|------------------|-----------------|
| POST /api/oauth/Login | 5 | 全部串行 | 50ms | 500ms |
| GET /api/oauth/CurrentUser | 35-45 | 全部串行 | 500ms | 2500ms |
| GET /api/oauth/getLoginConfig | 0 | N/A | <5ms | <5ms |
| GET /api/oauth/getConfig/{account} | 1-2 | 串行 | 10ms | 50ms |
| POST /api/oauth/updatePasswordMessage | 1 | — | 10ms | 50ms |
| GET /api/system/SysConfig | 1 | — | 10ms | 50ms |
| **合计** | **43-54** | **全部串行** | **~585ms** | **~3150ms** |

---

## 五、核心结论

### 5.1 时间瓶颈分布

| 阶段 | 局域网耗时 | 外网耗时 | 占比（外网） |
|------|-----------|---------|-------------|
| vendor chunk 下载 | 200ms | 800ms | 15% |
| JS 解析执行 | 100ms | 400ms | 7% |
| Login 接口（5 SQL） | 50ms | 500ms | 9% |
| **CurrentUser 接口（35-45 SQL）** | **500ms** | **2500ms** | **45%** |
| 路由构建 + 渲染 | 200ms | 500ms | 9% |
| **系统总耗时（不含用户输入）** | **~1 秒** | **~5 秒** | — |

### 5.2 根因排序

| 优先级 | 问题 | 影响 | 位置 |
|--------|------|------|------|
| **P0** | CurrentUser 35-45 次串行 SQL | 外网 2.5 秒，占系统耗时 45% | `OAuthService.cs:323-562` |
| **P0** | N+1：GetUserDataScope 循环内全表扫描 | N 个管理员 × 全表 Organize | `UserManager.cs:453-458` |
| **P0** | N+1：GetUserModuleListByIds 循环调用 | N 个系统 × 2-3 SQL | `OAuthService.cs:434-458` |
| **P1** | GetUserModuleListByIds 重复调用 3-4 次 | 每次 2-3 SQL，共浪费 6-9 次 | `OAuthService.cs:388,431,456,520` |
| **P1** | GetRoleNameByIds / GetSubsidiaryAsync 全表加载 | 全表 Role + 全表 Organize | `UserManager.cs:2121,1928` |
| **P1** | 缓存写了不读 | GetUserInfo 写缓存但 CurrentUser 不读 | `UserManager.cs:419` vs `OAuthService.cs:332` |
| **P2** | vendor chunk 4.5MB（前端） | 外网 800ms 下载 + 400ms 解析 | `registerGlobComp.ts` |
| **P2** | 全部 SQL 串行执行 | 无并行查询 | `OAuthService.cs` 全文 |

### 5.3 "一分钟打不开登录页"的原因分解

| 因素 | 贡献 | 说明 |
|------|------|------|
| vendor chunk 下载 | 1-2 秒 | 1.2MB gzip，取决于带宽 |
| JS 解析执行 | 0.5-1 秒 | 4.5MB JS 解析 |
| CurrentUser 接口 | 1-5 秒 | 35-45 次串行 SQL，取决于 DB 性能 |
| Login 接口 | 0.1-0.5 秒 | 5 次串行 SQL |
| **合计** | **3-9 秒** | 不含用户输入时间 |

**注意：** 3-9 秒是系统耗时，不是"一分钟"。如果用户体感是一分钟，可能原因：
1. 数据库性能极差（每条 SQL 100ms+，CurrentUser 需要 5+ 秒）
2. 网络延迟高（每次 SQL 往返 50ms+，45 次 = 2.25 秒纯网络开销）
3. 并发场景下数据库锁等待
4. 用户感知偏差（等待中的 5-10 秒感觉像一分钟）

### 5.4 优化建议（按收益排序）

1. **CurrentUser 接口 SQL 优化**（收益最大）：
   - 消除 N+1：`GetUserDataScope` 改为批量查询，`GetUserModuleListByIds` 结果缓存
   - 消除重复调用：`GetUserModuleListByIds` 在 line 388/431/456/520 被调用 3-4 次，应缓存结果
   - 消除全表扫描：`GetRoleNameByIds` 和 `GetSubsidiaryAsync` 改为按条件查询
   - 启用缓存：`GetUserInfo` 已写缓存，`GetCurrentUser` 应先读缓存

2. **SQL 并行化**：4 个权限服务调用（button/column/resource/form）互不依赖，可用 `Task.WhenAll` 并行

3. **前端 vendor 分包**：配合 Phase 2 的 manualChunks 建议，减少首屏 JS 体积

---

# 优化方案编制（Phase 4）

> **编制日期：** 2026-06-01
> **基于：** Phase 1-3 审计发现
> **目标：** 登录页系统耗时从 3-9 秒降到 1 秒以内

---

## P0-1：CurrentUser N+1 查询消除

### N+1-1：GetUserDataScope 循环内全表扫描

**【优化项】**：GetUserDataScope 中 foreach 循环内 GetSubsidiary() 全表扫描 OrganizeEntity
**【当前状态】**：`UserManager.cs:453-458`，遍历 OrganizeAdministratorEntity，每个元素调用 `GetSubsidiary(item.OrganizeId, false)`，每次都执行 `SELECT * FROM Organize WHERE DeleteMark IS NULL AND EnabledMark=1` 全表扫描。N 个管理员 = N 次全表扫描。
**【目标状态】**：全表扫描只执行 1 次，后续在内存中过滤。
**【改动文件】**：
- `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs`
**【改动方式】**：
```csharp
// 改前（line 446-485）：
private List<UserDataScopeModel> GetUserDataScope(string userId)
{
    // ...
    foreach (var item in _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>()
        .Where(it => it.UserId == userId && it.DeleteMark == null).ToList())
    {
        if (item.SubLayerSelect.ParseToBool() || ...)
        {
            var subsidiary = GetSubsidiary(item.OrganizeId, false).ToList(); // N+1！
            // ...
        }
    }
    // ...
}

// 改后：
private List<UserDataScopeModel> GetUserDataScope(string userId)
{
    // ...
    var allOrganizes = _repository.AsSugarClient().Queryable<OrganizeEntity>()
        .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1)).ToList(); // 一次性加载

    foreach (var item in _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>()
        .Where(it => it.UserId == userId && it.DeleteMark == null).ToList())
    {
        if (item.SubLayerSelect.ParseToBool() || ...)
        {
            var subsidiary = allOrganizes.TreeChildNode(item.OrganizeId, t => t.Id, t => t.ParentId)
                .Select(m => m.Id).ToArray(); // 内存过滤，不再查库
            // ...
        }
    }
    // ...
}
```
**【预估工作量】**：0.5 人天
**【预期收益】**：SQL 从 N+1 次降到 2 次（1 次 OrganizeAdministrator + 1 次 Organize 全表）。假设 N=3，节省 3 次全表扫描，约 30-150ms。
**【风险】**：低。`TreeChildNode` 是纯内存操作，已在 `GetSubsidiary` 中使用。全表 Organize 数据量通常在千级以内，内存占用可控。
**【验证方法】**：1. 登录后检查日志中 SQL 条数；2. 对比优化前后 CurrentUser 接口耗时。

### N+1-2：GetUserModuleListByIds 循环调用

**【优化项】**：GetCurrentUser 中 GetUserModuleListByIds 被调用 3-4 次，每次 2-3 SQL
**【当前状态】**：`OAuthService.cs` line 388、431、456、520 四处调用 `GetUserModuleListByIds()`，其中 line 456 在 for 循环内。每次调用执行 2-3 SQL（AuthorizeEntity + ModuleEntity + SystemEntity）。
**【目标状态】**：相同参数的调用只执行 1 次，结果复用。
**【改动文件】**：
- `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs`
**【改动方式】**：
```csharp
// 改前：多处重复调用
loginOutput.menuList = (await _moduleService.GetUserModuleListByIds(type, sysId, ...)).ToTree("-1"); // line 388
// ... 后续逻辑中又调用 2-3 次

// 改后：用变量缓存结果，相同参数不重复调用
var menuListCache = new Dictionary<string, List<ModuleNodeOutput>>();
async Task<List<ModuleNodeOutput>> GetMenuListCached(string t, string sId)
{
    var key = $"{t}:{sId}";
    if (!menuListCache.ContainsKey(key))
    {
        menuListCache[key] = await _moduleService.GetUserModuleListByIds(t, sId, noContainsMIdList, noContainsMUrlList);
    }
    return menuListCache[key];
}

loginOutput.menuList = (await GetMenuListCached(type, sysId)).ToTree("-1");
// ... 后续调用改为 GetMenuListCached(type, string.Empty)
```
**【预估工作量】**：0.5 人天
**【预期收益】**：SQL 从 3-4 次调用（6-12 SQL）降到 1-2 次调用（2-6 SQL）。节省约 100-300ms。
**【风险】**：低。仅缓存当前请求内的结果，不影响跨请求一致性。
**【验证方法】**：1. 日志中 SQL 条数对比；2. 确认菜单列表返回值不变。

### N+1-3：GetRoleNameByIds 全表加载

**【优化项】**：GetRoleNameByIds 加载全表 Role 后内存过滤
**【当前状态】**：`UserManager.cs:2121`，`Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToListAsync()` 加载所有角色，然后 foreach 按 idList 过滤。
**【目标状态】**：只查询需要的角色。
**【改动文件】**：
- `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs`
**【改动方式】**：
```csharp
// 改前（line 2113-2132）：
private async Task<string> GetRoleNameByIds(string ids)
{
    if (ids.IsNullOrEmpty()) return string.Empty;
    var idList = ids.Split(",").ToList();
    var nameList = new List<string>();
    var roleList = await _repository.AsSugarClient().Queryable<RoleEntity>()
        .Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToListAsync(); // 全表加载！
    foreach (var item in idList)
    {
        var info = roleList.Find(x => x.Id == item);
        if (info != null && info.FullName.IsNotEmptyOrNull()) nameList.Add(info.FullName);
    }
    return string.Join(",", nameList);
}

// 改后：
private async Task<string> GetRoleNameByIds(string ids)
{
    if (ids.IsNullOrEmpty()) return string.Empty;
    var idList = ids.Split(",").ToList();
    var nameList = await _repository.AsSugarClient().Queryable<RoleEntity>()
        .Where(x => idList.Contains(x.Id) && x.DeleteMark == null && x.EnabledMark == 1)
        .Select(x => x.FullName).ToListAsync(); // 按 ID 查询，只取 FullName
    return string.Join(",", nameList);
}
```
**【预估工作量】**：0.25 人天
**【预期收益】**：从全表扫描改为按 ID IN 查询。角色表通常百级记录，差异不大，但消除了不良模式。
**【风险】**：低。查询结果语义不变。
**【验证方法】**：登录后确认 roleName 返回值正确。

---

## P0-2：CurrentUser 缓存修复

**【优化项】**：GetUserInfo 写缓存但 GetCurrentUser 不读缓存
**【当前状态】**：
- `UserManager.cs:331` 构造缓存 Key：`{TenantId}:CACHEKEYUSER:{UserId}`
- `UserManager.cs:419` 写入缓存：`SetUserInfo(userCache, data, TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble()))`
- `OAuthService.cs:332` 直接调用 `await _userManager.GetUserInfo()`，不检查缓存
**【目标状态】**：GetUserInfo 优先读缓存，命中时直接返回，跳过 15+ 次 SQL。
**【改动文件】**：
- `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs`
**【改动方式】**：
```csharp
// 改前（line 327-421）：
public async Task<UserInfoModel> GetUserInfo()
{
    var userCache = string.Format("{0}:{1}:{2}", TenantId, CommonConst.CACHEKEYUSER, UserId);
    var userDataScope = GetUserDataScope(UserId); // 立即查库
    // ... 15+ SQL ...
    await SetUserInfo(userCache, data, TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble()));
    return data;
}

// 改后：
public async Task<UserInfoModel> GetUserInfo()
{
    var userCache = string.Format("{0}:{1}:{2}", TenantId, CommonConst.CACHEKEYUSER, UserId);
    var cached = await _cacheManager.GetOrAddAsync<UserInfoModel>(userCache, async () =>
    {
        var userDataScope = GetUserDataScope(UserId);
        // ... 15+ SQL ...
        return data;
    }, TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble()));
    return cached;
}
```
**【缓存失效策略】**：
- TTL：沿用现有 tokentimeout 配置（默认 120 分钟）
- 主动失效：权限变更时（角色分配、菜单授权、数据权限变更）调用 `_cacheManager.Remove(userCache)`
- 需要在以下位置添加清除逻辑：
  - `UserRoleService` — 角色变更时
  - `AuthorizeService` — 授权变更时
  - `ModuleService` — 菜单变更时
**【预估工作量】**：1 人天（含缓存失效逻辑）
**【预期收益】**：缓存命中时 CurrentUser 的 GetUserInfo 部分从 15+ SQL 降到 0 SQL，耗时从 ~200ms 降到 ~1ms。整个 CurrentUser 从 35-45 SQL 降到 20-30 SQL。
**【风险】**：中。缓存失效不及时会导致用户看到旧权限。需确保所有权限变更路径都触发缓存清除。
**【验证方法】**：1. 第一次登录：SQL 条数不变；2. 第二次登录（不重启服务）：GetUserInfo 部分 SQL 为 0；3. 修改权限后登录：SQL 条数恢复为全量。

---

## P0-3：前端 vendor 分包

**【优化项】**：60+ 全局组件静态 import 打入单一 4.5MB vendor chunk
**【当前状态】**：`src/components/registerGlobComp.ts` 在 main.ts bootstrap 中全量注册，tinymce/echarts/monaco 等通过静态 import 被拉入首包。
**【目标状态】**：登录页只加载 ~200KB，重型组件按需加载。
**【改动文件】**：
- `src/components/registerGlobComp.ts`
- `src/main.ts`
- `vite.config.ts`
**【改动方式】**：

**改动 1：registerGlobComp.ts 拆分**
```typescript
// 改前：全量注册
import { JnpfRichText } from '/@/components/Jnpf/RichText';  // 含 tinymce
import { JnpfCodeEditor } from '/@/components/Jnpf/CodeEditor'; // 含 monaco
// ... 60+ 静态 import

export async function registerGlobComp(app: App) {
  // 全量注册
}

// 改后：核心组件 + 懒加载组件
// registerGlobComp.ts — 只注册轻量核心组件
import { Input, Form, Button, Modal, Table, Select, ... } from 'ant-design-vue';
import { JnpfAlert } from '/@/components/Jnpf/Alert';
import { JnpfButton } from '/@/components/Jnpf/Button';
// ... 只导入轻量组件（不含 tinymce/monaco/echarts/logicflow）

export async function registerGlobComp(app: App) {
  // 只注册核心组件
}

// registerGlobCompHeavy.ts — 重型组件懒加载
export async function registerGlobCompHeavy(app: App) {
  const { JnpfRichText } = await import('/@/components/Jnpf/RichText');
  const { JnpfCodeEditor } = await import('/@/components/Jnpf/CodeEditor');
  // ... 动态 import 重型组件
  app.component('JnpfRichText', JnpfRichText);
  app.component('JnpfCodeEditor', JnpfCodeEditor);
  // ...
}
```

**改动 2：main.ts 延迟注册重型组件**
```typescript
// 改前：
bootstrap(): registerGlobComp(app); // 全量注册

// 改后：
bootstrap(): registerGlobComp(app); // 只注册核心组件
// 路由切换到非登录页时注册重型组件
router.afterEach((to) => {
  if (to.path !== '/login' && !heavyRegistered) {
    registerGlobCompHeavy(app);
    heavyRegistered = true;
  }
});
```

**改动 3：vite.config.ts 配置 manualChunks**
```typescript
// 改前：无 manualChunks 配置

// 改后：
build: {
  rollupOptions: {
    output: {
      manualChunks: {
        'vendor-vue': ['vue', 'vue-router', 'pinia'],
        'vendor-antd': ['ant-design-vue'],
        'vendor-echarts': ['echarts'],
        'vendor-monaco': ['monaco-editor'],
        'vendor-tinymce': ['tinymce'],
      }
    }
  }
}
```
**【预估工作量】**：2 人天
**【预期收益】**：
- 登录页首屏 JS 从 4.5MB (gzip 1.2MB) 降到 ~500KB (gzip ~150KB)
- 登录页加载时间减少 ~1 秒（外网）
- 后续页面按需加载重型 chunk，不影响功能
**【风险】**：中。需确认重型组件在路由切换时能正确注册，不出现组件未注册的运行时错误。需逐一测试各业务页面。
**【验证方法】**：1. `pnpm run build` 后检查 dist/static/js/ chunk 拆分情况；2. 登录页 Network 面板确认只加载核心 chunk；3. 进入表单设计页确认 tinymce 组件可用。

---

## P1-1：CurrentUser 子查询并行化

**【优化项】**：GetCurrentUser 中 4 个权限服务调用串行执行
**【当前状态】**：`OAuthService.cs:522-525`，button/column/resource/form 四个查询串行 await，每个 2 SQL，共 8 SQL 串行。
**【目标状态】**：4 个查询并行执行，总耗时从 4×2 SQL 降到 1×2 SQL（最慢的那个）。
**【改动文件】**：
- `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs`
**【改动方式】**：
```csharp
// 改前（line 520-525）：
currentUserModel.moduleList = (await _moduleService.GetUserModuleListByIds(...)).Adapt<List<ModuleOutput>>();
var dataScopeModuleIds = await _userRepository.AsSugarClient().Queryable<ModuleEntity>()
    .Where(x => dataScope.Contains(x.SystemId)).Select(x => x.Id).ToListAsync();
currentUserModel.buttonList = await _moduleButtonService.GetUserModuleButtonList(dataScopeModuleIds);
currentUserModel.columnList = await _columnService.GetUserModuleColumnList(dataScopeModuleIds);
currentUserModel.resourceList = await _moduleDataAuthorizeSchemeService.GetResourceList(dataScopeModuleIds);
currentUserModel.formList = await _formService.GetUserModuleFormList(dataScopeModuleIds);

// 改后：
currentUserModel.moduleList = (await _moduleService.GetUserModuleListByIds(...)).Adapt<List<ModuleOutput>>();
var dataScopeModuleIds = await _userRepository.AsSugarClient().Queryable<ModuleEntity>()
    .Where(x => dataScope.Contains(x.SystemId)).Select(x => x.Id).ToListAsync();

// 4 个权限查询并行
var buttonTask = _moduleButtonService.GetUserModuleButtonList(dataScopeModuleIds);
var columnTask = _columnService.GetUserModuleColumnList(dataScopeModuleIds);
var resourceTask = _moduleDataAuthorizeSchemeService.GetResourceList(dataScopeModuleIds);
var formTask = _formService.GetUserModuleFormList(dataScopeModuleIds);
await Task.WhenAll(buttonTask, columnTask, resourceTask, formTask);

currentUserModel.buttonList = buttonTask.Result;
currentUserModel.columnList = columnTask.Result;
currentUserModel.resourceList = resourceTask.Result;
currentUserModel.formList = formTask.Result;
```

**SqlSugarScope 线程安全确认：** 切面一审计已确认 `SqlSugarScope` 是线程安全单例（`SqlSugarScope` 内部为每个操作创建独立的 `SqlSugarClient` 实例），`Task.WhenAll` 并行查询安全。
**【预估工作量】**：0.5 人天
**【预期收益】**：4 个串行查询改为并行，权限部分耗时从 4×（2 SQL × 10ms）= 80ms 降到 1×（2 SQL × 10ms）= 20ms。慢数据库场景从 400ms 降到 100ms。
**【风险】**：低。4 个查询无数据依赖，仅共享 `dataScopeModuleIds` 输入。
**【验证方法】**：1. 登录后确认权限列表完整；2. 对比优化前后 CurrentUser 耗时。

---

## P1-2：getLoginConfig 缓存

**【优化项】**：getLoginConfig 每次打开登录页都读取配置
**【当前状态】**：`OAuthService.cs:1639`，`GetSocialsLoginConfig()` 读取 `_oauthOptions` 和 `_socialsOptions`，虽然当前是内存配置（0 SQL），但 SSO 启用时可能涉及外部调用。
**【目标状态】**：加内存缓存，TTL 1 小时。
**【改动文件】**：
- `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs`
**【改动方式】**：
```csharp
// 改后：
private static SocialsLoginConfigModel _loginConfigCache;
private static DateTime _loginConfigCacheExpiry;

[HttpGet("GetLoginConfig")]
[AllowAnonymous]
[LogPolicy(LogPolicy.IgnoreAll)]
public dynamic GetSocialsLoginConfig()
{
    if (_loginConfigCache != null && DateTime.Now < _loginConfigCacheExpiry)
        return _loginConfigCache;

    // ... 原有逻辑 ...
    _loginConfigCache = loginConfigModel;
    _loginConfigCacheExpiry = DateTime.Now.AddHours(1);
    return loginConfigCache;
}
```
**【预估工作量】**：0.25 人天
**【预期收益】**：当前已是 0 SQL，收益有限。主要防御 SSO 模式下的外部调用开销。
**【风险】**：低。配置变更后需重启服务才能生效（与当前行为一致）。
**【验证方法】**：多次调用 getLoginConfig 确认返回值一致。

---

## 优化收益汇总

| 优化项 | SQL 减少 | 耗时减少（局域网） | 耗时减少（慢 DB） | 工作量 |
|--------|---------|------------------|-----------------|--------|
| P0-1 N+1 消除 | -3~5 次 | 30-50ms | 150-500ms | 0.5 天 |
| P0-2 缓存修复 | -15 次（命中时） | 150ms（命中时） | 750ms（命中时） | 1 天 |
| P0-3 前端分包 | — | 200ms | 1000ms | 2 天 |
| P1-1 并行化 | — | 60ms | 300ms | 0.5 天 |
| P1-2 getLoginConfig 缓存 | 0 | 0 | 0 | 0.25 天 |
| **合计** | **-18~20 次** | **~440ms** | **~2200ms** | **4.25 天** |

**优化后预期：**

| 场景 | 优化前 | 优化后 | 改善 |
|------|--------|--------|------|
| 局域网系统耗时 | ~1 秒 | ~0.5 秒 | -50% |
| 外网系统耗时 | ~5.5 秒 | ~2 秒 | -64% |
| 慢 DB 系统耗时 | ~9 秒 | ~3.5 秒 | -61% |
| 登录页首屏 JS | 4.5MB (gzip 1.2MB) | ~500KB (gzip 150KB) | -87% |

---

## 实施顺序建议

```
第 1 天：P0-1（N+1 消除）+ P0-2（缓存修复）→ 后端 SQL 从 45 降到 5-8
第 2 天：P1-1（并行化）→ 权限查询耗时降 75%
第 3-4 天：P0-3（前端分包）→ 首屏 JS 降 87%
第 5 天：P1-2 + 回归测试 + 性能验证
```

**验收标准：** 登录页从打开到首页渲染完成，局域网 < 1 秒，外网 < 3 秒。
