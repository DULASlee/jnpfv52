# JNPF 后端全层审计修复开发计划

> 基于 2026-07-11 五层架构审计报告（P0×7 / P1×18 / P2×12）
> 修复原则：**功能正确性优先 → 性能热点 → 现代化改造 → 代码清理**
> 执行策略：**两批次分步推进 — 第一批低风险纯优化（不动业务逻辑）+ 第二批高风险需额外保护**
> 状态：🟢 执行中

---

## 零、执行策略（两批次分步推进）

### 批次划分原则

| 批次 | 标准 | 特点 |
|------|------|------|
| **第一批** | 纯 Bug 修复 / 行为等价的性能优化 / 纯增量功能 | 不动业务逻辑，输入输出完全等价，低风险 |
| **第二批** | 改变调用链 / 改变执行时序 / 改变 API 契约 | 需先写测试锁定当前行为，高风险 |

### 第一批：低风险纯优化（立即执行）— 约 7 天

```
B1   WebSocket Singleton           │ 纯 Bug（Transient→Singleton 本身就是设计意图）
B4.1 SuperQueryHelper 参数化        │ SQL 注入修复（string.Format→SqlParameter）
B4.2 表名/字段名白名单              │ SQL 注入防御（加正则校验层）
B7   ConcurrentBag→ConcurrentQueue  │ 资源泄漏修复（shutdown Dispose 可靠性）
R1   ChangeType 委托缓存            │ 行为等价（快照对比验证）
R2   TenantId PropertyInfo 缓存     │ 行为等价（缓存同一个 PropertyInfo）
R3   JWTEncryption 反射缓存         │ 行为等价（静态 ctor 一次性缓存）
R4   LoggingMonitor 特性缓存        │ 行为等价（IsDefined 结果不变）
R5   MemoryCache 显式键索引         │ 行为等价（替代反射读私有字段）
S1   LoggingMonitor StringBuilder   │ 行为等价（输出字符串一致）
S2   SensitiveDetection Span 优化   │ 行为等价（输出一致）
S3   DynamicApiController Regex 编译│ 行为等价（正则不变）
D4   租户缓存改分键                 │ 行为等价（缓存值不变）
I4   WebSocket ArrayPool 缓冲池     │ 行为等价（字节内容不变）
API1 JwtHandler 配置注入            │ 行为等价（配置值同源）
API2 响应压缩                       │ 纯增量（添加 gzip/brotli，客户端解压后内容不变）
P2-1 CacheManager 删空 Select      │ 纯优化（删除无操作行）
P2-2 InternalApp HashSet           │ 行为等价（O(n)→O(1) 无副作用）
P2-3 EventBus 事件处理器字典化      │ 行为等价（SortedList→Dictionary 不变序）
P2-4 ChannelEventPublisher try-catch│ 纯增量（异常不静默丢失）
P2-5 EventOutbox 并行处理          │ 行为等价（Task.WhenAll 结果集一致）
P2-6 EventOutbox COUNT 优化        │ 行为等价（DB 聚合替代客户端 COUNT）
P2-7 Entity Column Length          │ 纯增量（加属性不影响读写，注意不截断现网数据）
P2-8 SqlSugar AOP 缓存            │ 行为等价（Type.IsAssignableFrom 缓存）
P2-9 FounderGuardMiddleware Channel│ 行为等价（日志写入不丢）
P2-10 DbJobPersistence async void  │ 纯 Bug（async void→async Task + try-catch）
P2-11 DapperRepository OpenAsync   │ 行为等价（同步→异步 Open）
P2-12 BgTaskShutdownService await  │ 行为等价（.Wait→await + CancellationToken）
N1   GeneratedRegex                │ 行为等价（编译期验证正则）
N2   FrozenSet                     │ 行为等价（不可变集合同值查找）
N3   ValueTask 同步缓存路径         │ 行为等价（ValueTask.FromResult 语义一致）
N4   ArrayPool 批量应用             │ 行为等价（池化字节，内容不变）
N5   Channels 引入                 │ 行为等价（已在 P2-9 覆盖）
N6   Mapster 配置去重               │ 纯优化（移除被覆盖的无效配置）
```

### 第二批：高风险需额外保护（第一批完成后执行）— 约 11 天

```
B2   V8 加锁                       │ 需压测确认吞吐 / 无死锁
B5   FormDataParsing .Result→await  │ 需集成测试锁定当前行为 / 审查 Scoped.Create 生命周期
B6   DataExecuting 竞态修复         │ 需 xUnit 多租户并发测试 FIRST
D1   TenantService N+1→批量        │ 需快照测试锁定 GetMenuTree 输出
D2   ModuleService N+1→批量        │ 需理解 type=0/type=1 完整语义 + xUnit
SA1  FormDataParsing 全链 async    │ → 合入 B5
SA2  ControlParsing .Result→await  │ 需代码生成集成测试
SA3  FlowTaskUserUtil .Result→await│ 需审批流集成测试
SR1  Newtonsoft→System.Text.Json   │ 需 staging 验证 + 前端回归（API 契约兼容性）
I1   OAuth HttpClient async        │ 需第三方登录集成测试
I2   Thirdparty SDK async          │ 需各渠道（DingTalk/WeChat/Email/SMS）集成测试
I3   WebSocket 广播并行化           │ 需性能测试（确认不丢消息）
API3 UnsafeRelaxedJsonEscaping     │ 需确认使用范围（内部/外部）
```

### 门禁规则

- **第一批**：每完成 5 项 → `dotnet build` + `pnpm test:api` → 快照对比 → 用户审批
- **第二批**：每项开始前先写 xUnit/集成测试锁定当前行为 → 编码 → 验证 → 用户审批
- **严禁**跳过测试直接改第二批代码

---

## 目录
2. [阶段一：P0 致命缺陷修复（8项）](#二阶段一p0-致命缺陷修复)
3. [阶段二：P1 反射热点修复（5项）](#三阶段二p1-反射热点修复)
4. [阶段三：P1 字符串分配优化（3项）](#四阶段三p1-字符串分配优化)
5. [阶段四：P1 数据访问优化（N+1 & 线程安全，4项）](#五阶段四p1-数据访问优化)
6. [阶段五：P1 Sync-over-Async 消除（3项）](#六阶段五p1-sync-over-async-消除)
7. [阶段六：P1 序列化栈迁移 & 基础设施（8项）](#七阶段六p1-序列化栈迁移--基础设施)
8. [阶段七：P2 中影响优化（12项）](#八阶段七p2-中影响优化)
9. [阶段八：现代 .NET 机制批量应用](#九阶段八现代-net-机制批量应用)
10. [修复依赖 & 时间估算](#十修复依赖--时间估算)
11. [每阶段验收标准](#十一每阶段验收标准)

---

## 一、概述 & 修复策略

### 1.1 分层推进

| 阶段 | 优先级 | 任务数 | 风险等级 | 预计工时 |
|------|--------|--------|----------|----------|
| 一 | P0 | 8 | 🔴 高（改线上行为） | 3.5 天 |
| 二 | P1 反射 | 5 | 🟡 中 | 2 天 |
| 三 | P1 字符串 | 3 | 🟢 低 | 1 天 |
| 四 | P1 数据访问 | 4 | 🔴 高（SQL 变更） | 2 天 |
| 五 | P1 Sync-over-Async | 3 | 🔴 高（调用链改 async） | 2.5 天 |
| 六 | P1 序列化+基础设施 | 8 | 🟡 中 | 3 天 |
| 七 | P2 | 12 | 🟢 低 | 3 天 |
| 八 | 现代 .NET 机制 | 6 | 🟢 低 | 1.5 天 |

**总计：约 18 个工作日**

### 1.2 每次修改必做

```
1. 阅读目标文件上下文（前后 30 行）
2. 执行修改
3. dotnet build backend/ （Release + CI_BUILD）
4. E2E_PIPELINE_ID=311 pnpm test:api （快断言）
5. 失败则回滚+重试（≤3 轮）
```

### 1.3 关键约束

- **禁止改 Controller**：所有 API 通过 Service 实现 `IDynamicApiController`
- **禁止改 `.vm` 生成输出**：codegen 模板修复只改 `.vm` 源
- **每阶段完成后暂停**：提交 "业务实现 + 质量自检 + 功能证据 + 验收对照"，经审批后进入下一阶段
- **沉默 ≠ 审批**：用户未回复前不得继续

---

## 二、阶段一：P0 致命缺陷修复

### B1：WebSocket 连接管理器改为 Singleton

**文件**：`backend/infrastructure/JNPF.Extras.WebSockets/Extensions/WebSocketServiceCollectionExtensions.cs:12`

**现状**：
```csharp
services.AddTransient<WebSocketConnectionManager>();
```

**问题**：`WebSocketConnectionManager` 持有 `ConcurrentDictionary` 连接池，每次 DI 解析创建新实例导致连接分散在多个孤立池中，广播/群组消息断裂。

**修复**：
```csharp
services.AddSingleton<WebSocketConnectionManager>();
```

**影响范围**：WebSocket 连接管理全部逻辑
**风险**：极低 — `WebSocketConnectionManager` 本身已设计为线程安全的连接池，Singleton 是其正确语义
**验证**：
1. `dotnet build backend/`
2. `pnpm test:api`（WebSocket 相关测试）
3. 检查 WebSocket 广播逻辑：同时连接两个客户端，确认广播可达

**预计工时**：0.5h

---

### B2：静态 V8JsEngine 线程安全化

**文件**：`backend/infrastructure/JNPF.Extras.Thirdparty/JSEngine/JsEngineUtil.cs:10`

**现状**：
```csharp
private static V8JsEngine engine = new V8JsEngine();
```

**问题**：V8 引擎非线程安全，并发调用损坏内部状态。

**修复方案**：引入 `SemaphoreSlim` 保护所有访问

```csharp
private static readonly SemaphoreSlim _engineLock = new SemaphoreSlim(1, 1);

public static object CallFunction(string jsContent, params object[] args)
{
    _engineLock.Wait();
    try
    {
        engine.Execute(jsContent);
        return engine.CallFunction("result", args);
    }
    catch (Exception e)
    {
        throw new Exception("不支持的JS数据");
    }
    finally
    {
        _engineLock.Release();
    }
}
```

**同样修复** `AggreFunction` 方法。

**可选增强**（不在此阶段）：改为 `ThreadLocal<V8JsEngine>` 或按 key 对象池化，但 SemaphoreSlim 是最小改动且正确。

**影响范围**：`JsEngineUtil` 全部方法
**风险**：低 — 加锁降低并发吞吐，但 JSEngine 调用频率低（仅聚合公式/规则引擎）
**验证**：
1. `dotnet build backend/`
2. 并发调用 JsEngineUtil：`Task.WhenAll(10 并发)`
3. 验证无异常、无结果串扰

**预计工时**：1h

---

### B3：MemoryCache.RemoveAll 导致缓存永久失效

**文件**：`backend/infrastructure/JNPF.Extras.CollectiveOAuth/Utils/ConfigurationManager.cs`

**现状**：`RefreshConfiguration()` 方法（行 210-224）调用 `FileListeners.Pop().Value.Dispose()` 逐个释放 `FileSystemWatcher`，无直接 `MemoryCache.RemoveAll`。

**重新审计确认**：经代码复查，B3 告警可能误报。`ConfigurationManager` 内部不直接使用 `MemoryCache`。原报告指出 `ConfigurationManager.cs:47` 调用 `MemoryCache.Dispose()`，但第 47 行实际是 `_configUrlPostfix` 字段声明。

**修正**：移除 B3（或降级为复查 `MemoryCache` 使用处）

**改为审计**：全局搜索 `.RemoveAll()` / `.Dispose()` 对 MemoryCache 的调用

```bash
git grep "RemoveAll\|\.Dispose()" -- "*.cs" | grep -i cache
```

**验证**：`dotnet build backend/`

**预计工时**：0.5h

---

### B4：SQL 注入修复（3 处）

#### B4.1 SuperQueryHelper 用户输入直拼 SQL

**文件**：`backend/modularity/common/JNPF.Common/Security/SuperQueryHelper.cs:316`

**现状**：
```csharp
var sql = string.Format("SELECT F_OBJECTID OBJECTID,F_OBJECTTYPE OBJECTTYPE FROM BASE_USERRELATION WHERE F_USERID='{0}'", 
    fieldValue.ToString().Replace("--user", string.Empty));
```

**修复**：参数化查询
```csharp
var sql = "SELECT F_OBJECTID OBJECTID,F_OBJECTTYPE OBJECTTYPE FROM BASE_USERRELATION WHERE F_USERID=@userId";
var res = db.Ado.GetDataTable(sql, new SugarParameter("@userId", fieldValue.ToString().Replace("--user", string.Empty)));
```

**影响范围**：`SuperQueryHelper.GetUserRelation` 方法
**风险**：低 — 原代码已做 `Replace("--user", "")` 但不足以防御 SQL 注入
**验证**：
1. `dotnet build backend/`
2. 输入测试：`admin' OR '1'='1` → 确认不返回全表
3. `pnpm test:api`

#### B4.2 RunService 表名/字段名直拼（3 行）

**文件**：`backend/modularity/visualdev/JNPF.VisualDev/RunService.cs:3235,3315,3392`

**修复方案**：添加白名单校验 — 表名/字段名必须匹配 `^[a-zA-Z_][a-zA-Z0-9_]*$`，否则 `throw Oops.Bah("非法表名")`

```csharp
private static readonly Regex SafeIdentifierPattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

private static string ValidateIdentifier(string identifier, string context)
{
    if (string.IsNullOrWhiteSpace(identifier) || !SafeIdentifierPattern.IsMatch(identifier))
        throw Oops.Bah($"{context}不合法: {identifier}");
    return identifier;
}
```

在各拼接点调用：`ValidateIdentifier(tableName, "表名")`、`ValidateIdentifier(fieldName, "字段名")`

**影响范围**：`RunService` 动态 SQL 构建
**风险**：中 — 需确认合法标识符正则未拒绝正常业务表名（如中文表名场景）
**验证**：
1. `dotnet build backend/`
2. 测试合法表名/字段名：`BASE_USER`、`F_ACCOUNT`、`中文表名`
3. 测试非法输入：`; DROP TABLE BASE_USER; --`
4. `pnpm test:api`

#### B4.3 ConfigController 表名直拼

**文件**：`backend/modularity/zxdev/JNPF.ZxDev/ConfigController.cs:286`

**修复**：同 B4.2 白名单校验

**验证**：同上

**预计工时**：2h（三项合计）

---

### B5：热路径 Sync-over-Async 消除（3 处）

#### B5.1 FormDataParsing.GetRelationFormList .Result（2 处）

**文件**：`backend/modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs:1880,2008,2145`

**现状**：
```csharp
// 行 1880
var res = _runService.GetRelationFormList(relationFormModel, listQueryInput)
    .WaitAsync(TimeSpan.FromMinutes(2)).Result;

// 行 2008
var res = _dataInterfaceService.GetResponseByType(...).Result;

// 行 2145
var res = _dataInterfaceService.GetResponseByType(...).Result;
```

**修复方案**：逐层改 async（调用的外层方法改为 async，传播 await 链）

**步骤**：
1. 找到包含 `.Result` 的父方法签名，添加 `async Task` / `async Task<T>`
2. 将 `.Result` 改为 `await`
3. 递归传播到所有调用方
4. 顶层由 DynamicApiController 自动处理 async

**影响范围**：`FormDataParsing` 整个调用链（预估 3-5 层调用方需改签名）
**风险**：高 — 大规模 async 传播可能改变执行时序；需逐层审查 `Scoped.Create` 生命周期
**验证**：
1. `dotnet build backend/`（0 error）
2. 表单渲染集成测试
3. `pnpm test:api`

**预计工时**：6h

#### B5.2 ControlParsing.GetRelationFormList .Result

**文件**：`backend/modularity/common/JNPF.Common.CodeGen/DataParsing/ControlParsing.cs:263`

**修复**：同 B5.1 模式

**影响范围**：`ControlParsing` 调用链
**风险**：中
**验证**：
1. `dotnet build backend/`
2. 代码生成集成测试

**预计工时**：3h

#### B5.3 FlowTaskUserUtil .Result 在 LINQ Where

**文件**：`backend/modularity/workflow/JNPF.WorkFlow/Manager/FlowTaskUserUtil.cs:156`

**修复方案**：将 LINQ Where 改为 `foreach + await` 或使用 `System.Linq.Async` 第三方库

```csharp
// 现状（推测）
var list = source.Where(x => SomeAsyncMethod(x).Result).ToList();

// 修复
var results = new List<T>();
foreach (var x in source)
{
    if (await SomeAsyncMethod(x))
        results.Add(x);
}
```

**预计工时**：1.5h

---

### B6：DataBaseManager DataExecuting 竞态修复

**文件**：`backend/modularity/common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs:168-189`

**现状**：
```csharp
_sqlSugarClient.Aop.DataExecuting = (oldValue, entityInfo) =>
{
    // 写 TenantId
};
```

**问题**：`DataExecuting` 在 `SqlSugarScope` 上是单一委托引用，多租户并发切换时互相覆盖 → 跨租户数据写错。

**修复方案**：在 AOP 回调内部基于 `entityInfo.EntityValue` 动态判断租户，而非依赖回调注册时的闭包变量

```csharp
// 只在初始化时注册一次（非租户切换时）
_sqlSugarClient.Aop.DataExecuting = (oldValue, entityInfo) =>
{
    var entity = entityInfo.EntityValue;
    if (entity is ITenantFilter tenantEntity && string.IsNullOrEmpty(tenantEntity.TenantId))
    {
        // 从当前上下文获取正确的 TenantId
        var currentTenantId = GetCurrentTenantId(); // 线程安全的上下文读取
        entityInfo.SetValue(currentTenantId);
    }
};
```

**关键**：`GetCurrentTenantId()` 必须从请求上下文（`IHttpContextAccessor` / `AsyncLocal`）读取，不能依赖闭包。

**影响范围**：`DataBaseManager`、`TenantManager` 全部 DataExecuting 注册点
**风险**：高 — 改动租户隔离机制，必须充分测试多租户并发场景
**验证**：
1. `dotnet build backend/`
2. 并发切换租户 + 并发插入 → 确认 TenantId 无跨租户污染
3. `pnpm test:api`

**预计工时**：4h

---

### B7：ConcurrentBag → ConcurrentQueue（IDisposable 追踪）

**文件**：`backend/framework/JNPF/App/App.cs:88`

**现状**（推测）：
```csharp
private static ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();
```

**修复**：
```csharp
private static ConcurrentQueue<IDisposable> _disposables = new ConcurrentQueue<IDisposable>();
```

**说明**：`ConcurrentBag` 使用 work-stealing 导致单线程 Dispose 可能看不到其他线程添加的 disposable 对象。`ConcurrentQueue` 保证 FIFO 顺序清理。

**影响范围**：`App` 类 disposable 注册 / 清理
**风险**：极低
**验证**：
1. `dotnet build backend/`
2. 确认注册的 disposable 在 shutdown 时全部清理

**预计工时**：0.5h

---

> **阶段一审查点**：完成 B1-B7 后暂停，提交 build + test:api 结果，等待用户审批。

---

## 三、阶段二：P1 反射热点修复

### R1：ObjectExtensions.ChangeType 缓存属性访问器

**文件**：`backend/framework/JNPF/App/Extensions/ObjectExtensions.cs:370-392`

**现状**：
```csharp
var constructor = type.GetConstructor(Type.EmptyTypes);
var o = constructor.Invoke(null);
var propertys = type.GetProperties();
var oldType = obj.GetType();
foreach (var property in propertys)
{
    var p = oldType.GetProperty(property.Name);
    if (property.CanWrite && p != null && p.CanRead)
    {
        property.SetValue(o, ChangeType(p.GetValue(obj, null), property.PropertyType), null);
    }
}
```

**问题**：每次类型转换都 `Type.GetProperties()` + `PropertyInfo.GetValue/SetValue`

**修复方案**：使用编译后的委托缓存

```csharp
private static readonly ConcurrentDictionary<(Type Source, Type Target), object> _converterCache = new();

private static Func<object, object> GetOrCreateConverter(Type sourceType, Type targetType)
{
    var key = (sourceType, targetType);
    if (_converterCache.TryGetValue(key, out var cached))
        return (Func<object, object>)cached;

    var sourceParam = Expression.Parameter(typeof(object), "source");
    var sourceCast = Expression.Convert(sourceParam, sourceType);
    var targetVar = Expression.Variable(targetType, "target");
    var newTarget = Expression.New(targetType);
    var assignTarget = Expression.Assign(targetVar, newTarget);

    var expressions = new List<Expression> { assignTarget };
    foreach (var targetProp in targetType.GetProperties()
        .Where(p => p.CanWrite))
    {
        var sourceProp = sourceType.GetProperty(targetProp.Name);
        if (sourceProp != null && sourceProp.CanRead)
        {
            var sourceValue = Expression.Property(sourceCast, sourceProp);
            var convertedValue = Expression.Convert(sourceValue, targetProp.PropertyType);
            expressions.Add(Expression.Assign(
                Expression.Property(targetVar, targetProp), convertedValue));
        }
    }
    expressions.Add(targetVar);

    var block = Expression.Block(new[] { targetVar }, expressions);
    var lambda = Expression.Lambda<Func<object, object>>(block, sourceParam);
    var compiled = lambda.Compile();
    _converterCache.TryAdd(key, compiled);
    return compiled;
}
```

**影响范围**：所有走 `ChangeType` 兜底分支的类型转换
**风险**：低 — 纯优化，输入输出语义不变
**验证**：
1. `dotnet build backend/`
2. 单元测试：`ChangeType` 对常用 DTO 转换 → 对比修复前后结果完全一致
3. `pnpm test:api`

**预计工时**：3h

---

### R2：SqlSugarRepository 缓存 TenantId 属性信息

**文件**：`backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Repositories/SqlSugarRepository.cs:214-235`

**现状**：每次 `HasTenantId` / `SetTenantId` 调用 `typeof(TEntity).GetProperty("TenantId")`

**修复**：
```csharp
private static readonly ConcurrentDictionary<Type, PropertyInfo> _tenantIdPropCache = new();

private static PropertyInfo GetTenantIdProperty(Type entityType)
{
    return _tenantIdPropCache.GetOrAdd(entityType, t =>
        t.GetProperty("TenantId", BindingFlags.Public | BindingFlags.Instance));
}

// 可选进一步增强：缓存编译后的 getter/setter
private static readonly ConcurrentDictionary<Type, Func<TEntity, string>> _tenantIdGetterCache = new();
private static readonly ConcurrentDictionary<Type, Action<TEntity, string>> _tenantIdSetterCache = new();
```

**影响范围**：每次 Insert/Update DB 操作
**风险**：极低
**验证**：
1. `dotnet build backend/`
2. `pnpm test:api`（确认 Insert/Update 行为不变）

**预计工时**：1.5h

---

### R3：JWTEncryption 缓存 App 类型引用

**文件**：`backend/framework/JNPF.Extras.Authentication.JwtBearer/JWTEncryption.cs:492-508`

**现状**：每次 JWT 操作通过 `AssemblyLoadContext.Default.LoadFromAssemblyName` + 反射找到 `App` 类型 → `GetMethod("GetOptions").MakeGenericMethod.Invoke`

**修复方案**：静态构造函数缓存

```csharp
private static readonly Type _appType;
private static readonly MethodInfo _getOptionsMethod;

static JWTEncryption()
{
    // 一次性反射，缓存结果
    _appType = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "JNPF")
        ?.GetType("JNPF.App");
    
    if (_appType != null)
    {
        _getOptionsMethod = _appType.GetMethod("GetOptions", Type.EmptyTypes);
    }
}
```

**更优方案**：注入 `IOptions<JWTSettingsOptions>` 替代全部反射调用（需改 JWTEncryption 为非静态类，注册为 DI）

**影响范围**：每次 JWT 验证/刷新
**风险**：低
**验证**：
1. `dotnet build backend/`
2. JWT 登录 + Token 验证
3. `pnpm test:api`

**预计工时**：2h（含 DI 改造）

---

### R4：LoggingMonitorAttribute 缓存特性查询

**文件**：`backend/framework/JNPF/Logging/Implantations/Monitors/LoggingMonitorAttribute.cs:741-893`

**现状**：每个请求重复调用 `IsDefined(typeof(SuppressMonitorAttribute))`、`IsDefined(typeof(LoggingMonitorAttribute))` 多次

**修复**：
```csharp
private static readonly ConcurrentDictionary<(MethodInfo, Type), bool> _attributeCache = new();

private static bool HasAttributeCached(MethodInfo method, Type attributeType, bool inherit)
{
    var key = (method, attributeType);
    return _attributeCache.GetOrAdd(key, _ => method.IsDefined(attributeType, inherit));
}
```

**影响范围**：每个被监控的 API 请求
**风险**：极低
**验证**：
1. `dotnet build backend/`
2. `pnpm test:api`

**预计工时**：1h

---

### R5：MemoryCache.GetAllKeys 反射替换

**文件**：`backend/framework/JNPF/Cache/MemoryCache.cs:199-213`

**现状**：反射访问 `MemoryCache._entries` 私有字段 → `.NET 升级时可能断裂`

**修复方案**：显式维护键索引
```csharp
private readonly ConcurrentDictionary<string, byte> _keyIndex = new();

public bool Set(string key, object value, ...)
{
    _memoryCache.Set(key, value, ...);
    _keyIndex.TryAdd(key, 0);
    return true;
}

public void Remove(string key)
{
    _memoryCache.Remove(key);
    _keyIndex.TryRemove(key, out _);
}

public List<string> GetAllKeys()
{
    return _keyIndex.Keys.ToList();
}
```

**影响范围**：所有缓存操作
**风险**：低 — 键索引与 MemoryCache 可能不同步（如缓存过期自动清理）
**缓解**：周期性 `_keyIndex.Keys.Where(k => !_memoryCache.TryGetValue(k, out _))` 清理

**预计工时**：2h

---

> **阶段二审查点**：完成 R1-R5 后暂停，提交 build + test:api 结果，等待审批。

---

## 四、阶段三：P1 字符串分配优化

### S1：LoggingMonitorAttribute 减少字符串分配

**文件**：`backend/framework/JNPF/Logging/Implantations/Monitors/LoggingMonitorAttribute.cs:963-1019`

**现状**：构建日志消息时先 `List<string>` 收集片段再 `string.Join` → 两次分配

**修复**：
```csharp
// 修复前
var messages = new List<string>();
messages.Add($"xxx: {value1}");
messages.Add($"yyy: {value2}");
var log = string.Join(Environment.NewLine, messages);

// 修复后
var sb = new StringBuilder();
sb.Append("xxx: ").Append(value1).AppendLine();
sb.Append("yyy: ").Append(value2).AppendLine();
var log = sb.ToString();
```

**额外优化**：先判断 `logger.IsEnabled(logLevel)` 再构建消息字符串

**预计工时**：1h

---

### S2：SensitiveDetectionProvider 消除内循环 ToString()

**文件**：`backend/framework/JNPF/SensitiveDetection/Providers/SensitiveDetectionProvider.cs:128-167`

**现状**：
```csharp
while (tempStringBuilder.ToString().IndexOf(sensitiveWord) > -1) // 每轮 ToString()
{
    findIndex = tempStringBuilder.ToString().IndexOf(sensitiveWord); // 又一次 ToString()
    ...
    tempStringBuilder.Remove(0, findIndex + sensitiveWord.Length);
}
```

**修复**：将 `ToString()` 提取到循环外，或在 Remove 后维护索引而非重新创建字符串
```csharp
var text = tempStringBuilder.ToString(); // 一次性物化
while ((findIndex = text.IndexOf(sensitiveWord, currentOffset)) > -1)
{
    ...
    currentOffset = findIndex + sensitiveWord.Length;
}
```

**进一步优化（可选）**：`ReadOnlySpan<char>` + `MemoryExtensions.IndexOf` 零分配搜索

**预计工时**：1.5h

---

### S3：DynamicApiController 路由正则编译

**文件**：`backend/framework/JNPF/DynamicApiController/Conventions/DynamicApiControllerApplicationModelConvention.cs`

**现状**：`Regex.IsMatch` 在循环中重复调用

**修复**：
```csharp
// 将 Regex 提取为静态只读
private static readonly Regex RoutePattern = new Regex(@"...", RegexOptions.Compiled);

// 使用处
if (RoutePattern.IsMatch(url)) { ... }
```

**影响范围**：应用启动时一次性路由构建（非热路径），但 `.NET 8` 下 `[GeneratedRegex]` 有额外编译期验证价值

**进一步**：.NET 8+ 使用 `[GeneratedRegex]` 实现编译期验证和零启动分配
```csharp
#if NET8_0_OR_GREATER
[GeneratedRegex(@"^api/[a-z]+/[a-z]+$", RegexOptions.Compiled)]
private static partial Regex RoutePatternRegex();
#endif
```

**预计工时**：0.5h

---

> **阶段三审查点**：完成 S1-S3 后暂停，提交 build + test:api 结果，等待审批。

---

## 五、阶段四：P1 数据访问优化

### D1：TenantService.GetMenuTree N+1 消除

**文件**：`backend/modularity/system/JNPF.Systems/Common/TenantService.cs:95-110`

**现状**：
```csharp
foreach (var item in systemList)
{
    var sysModuleList = await _sqlSugarClient.Queryable<ModuleEntity>()
        .Where(it => it.SystemId.Equals(item.id) ...)
        .ToListAsync(); // 每个 system 一次 DB 查询
}
```

**修复**：先收集所有 SystemId，一次查询
```csharp
var systemIds = systemList.Select(s => s.id).ToList();
var allModules = await _sqlSugarClient.Queryable<ModuleEntity>()
    .Where(it => systemIds.Contains(it.SystemId) 
        && it.EnabledMark == 1 && it.DeleteMark == null 
        && !enCodeList.Contains(it.EnCode))
    .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
    .ToListAsync();

foreach (var item in systemList)
{
    var sysModuleList = allModules.Where(m => m.SystemId.Equals(item.id)).ToList();
    // ... 后续处理不变
}
```

**影响范围**：租户菜单树构建
**风险**：低 — 只改变查询方式，结果集一致
**验证**：
1. `dotnet build backend/`
2. 对比修复前后菜单树输出（快照测试）
3. `pnpm test:api`

**预计工时**：1.5h

---

### D2：ModuleService N+1 重复检查消除

**文件**：`backend/modularity/system/JNPF.Systems/System/ModuleService.cs:1092-1303`

**现状**：foreach 内对每个按钮/列/表单执行 2 次 `AnyAsync`（code + name 检查）+ `Storageable(item)` 逐项持久化

**修复**：
```csharp
// 1. 预查询：收集所有待检查的 EnCode/FullName，一次查询
var allEnCodes = data.buttonEntityList.Select(b => b.EnCode).ToList();
var allFullNames = data.buttonEntityList.Select(b => b.FullName).ToList();
var existingButtons = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
    .Where(it => it.ModuleId == data.id && it.DeleteMark == null
        && (allEnCodes.Contains(it.EnCode) || allFullNames.Contains(it.FullName)))
    .ToListAsync();

var existingEnCodes = new HashSet<string>(existingButtons.Select(b => b.EnCode));
var existingFullNames = new HashSet<string>(existingButtons.Select(b => b.FullName));

// 2. 收集所有非重复项，一次 Storageable
var newButtons = data.buttonEntityList
    .Where(item => !existingEnCodes.Contains(item.EnCode) && !existingFullNames.Contains(item.FullName))
    .ToList();
if (newButtons.Any())
{
    var storage = _repository.AsSugarClient().Storageable(newButtons).Saveable().ToStorage();
    await storage.AsInsertable.ExecuteCommandAsync();
    await storage.AsUpdateable.ExecuteCommandAsync();
}
```

**同样优化** `columnEntityList`（行 1133+）

**影响范围**：模块导入功能
**风险**：中 — 需确保重复检查逻辑完全一致（原逻辑区分 type=0 覆盖和 type=1 追加）
**验证**：
1. `dotnet build backend/`
2. 模块导入测试：含重复按钮/列的导入
3. `pnpm test:api`

**预计工时**：3h

---

### D3：TenantManager / DataBaseManager 竞态修复（同 B6）

**说明**：已在阶段一 B6 覆盖，此处不重复。

---

### D4：SqlSugarDbContextProvider 租户缓存优化

**文件**：`backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/Repositories/SqlSugarDbContextProvider.cs:116-128`

**现状**：全量反序列化租户缓存列表 + `.FirstOrDefault` O(n) 查找

**修复**：按 TenantId 分键缓存
```csharp
// 修复前
var tenants = JsonConvert.DeserializeObject<List<TenantCache>>(cache);
var tenant = tenants.FirstOrDefault(t => t.TenantId == tenantId);

// 修复后
var key = $"jnpf:global:tenant:{tenantId}";
var tenantJson = _cache.Get<string>(key);
if (tenantJson == null)
{
    // fallback: 全量加载后重建分键缓存
    var tenants = JsonConvert.DeserializeObject<List<TenantCache>>(_cache.Get<string>("jnpf:global:tenants"));
    foreach (var t in tenants)
        _cache.Set($"jnpf:global:tenant:{t.TenantId}", JsonConvert.SerializeObject(t));
    tenant = tenants.FirstOrDefault(t => t.TenantId == tenantId);
}
else
{
    tenant = JsonConvert.DeserializeObject<TenantCache>(tenantJson);
}
```

**预计工时**：2h

---

> **阶段四审查点**：完成 D1-D4 后暂停，提交 build + test:api 结果，等待审批。

---

## 六、阶段五：P1 Sync-over-Async 消除

### SA1：FormDataParsing 全链 async 化

**文件**：`backend/modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs:1880,2008,2145`

**修复策略**：从叶子节点（`.Result` 所在方法）向上传播 async

**步骤**：
1. 定位 3 处 `.Result` 的父方法名（`GetFormData` / `GetRelationData` 等）
2. 将父方法签名改为 `async Task<...>`
3. 将 `.Result` 替换为 `await`
4. 向上查找所有调用方，递归改 async
5. 对于 `Scoped.Create` 回调内的 `.Result`（行 1880）— 改 `Scoped.Create` 为 async 重载或改为 `await using var scope = ...`

**关键风险点**：
- `Scoped.Create` 回调中同步等待 → 需确认 `Scoped.Create` 是否支持 async 回调
- `foreach` 内 `.Result` → 改为 `foreach + await` 或使用 `Task.WhenAll` 批量
- `_cacheManager.Get(redisName)` 在 `.Result` 之后立即使用 → 确保缓存写入在 await 后仍然有效

**预计工时**：6h

---

### SA2：ControlParsing 同步阻塞消除

**文件**：`backend/modularity/common/JNPF.Common.CodeGen/DataParsing/ControlParsing.cs:263`

**修复**：同 SA1 模式，向上传播 async

**预计工时**：3h

---

### SA3：FlowTaskUserUtil LINQ 内 .Result 消除

**文件**：`backend/modularity/workflow/JNPF.WorkFlow/Manager/FlowTaskUserUtil.cs:156`

**修复**：
```csharp
// 修复前（推测）
var results = source.Where(x => SomeAsyncMethod(x).Result).ToList();

// 修复后
var results = new List<T>();
foreach (var x in source)
{
    if (await SomeAsyncMethod(x))
        results.Add(x);
}
```

**替代方案**（性能更好）：并行检查
```csharp
var checks = await Task.WhenAll(source.Select(async x => new { Item = x, Ok = await SomeAsyncMethod(x) }));
var results = checks.Where(c => c.Ok).Select(c => c.Item).ToList();
```

**预计工时**：1.5h

---

> **阶段五审查点**：完成 SA1-SA3 后暂停，提交 build + test:api 结果，等待审批。

---

## 七、阶段六：P1 序列化栈迁移 & 基础设施

### SR1：移除双序列化器 → System.Text.Json 单栈

**文件**：`backend/application/JNPF.API.Entry/Modules/JsonSettingsModule.cs:17-31`

**现状**：
```csharp
services.AddControllers()
    .AddNewtonsoftJson(options => { ... })
    .AddJsonOptions(options => { ... });
```

**修复**：移除 `.AddNewtonsoftJson()`，仅保留 `.AddJsonOptions()`

**前置依赖**：
- 所有使用 `[JsonProperty]` 特性的代码 → 改为 `[JsonPropertyName]`
- 所有使用 `JObject`/`JArray` 的 Newtonsoft 代码 → 改为 `JsonDocument`/`JsonNode`
- `JsonConvert.SerializeObject/DeserializeObject` → `JsonSerializer.Serialize/Deserialize`

**受影响文件（预估 30+ 文件）**：

| 文件 | 当前使用 | 迁移目标 |
|------|---------|---------|
| `infrastructure/.../CollectiveOAuth/Utils/HttpUtils.cs` | `JObject`、`JsonConvert` | `JsonDocument`、`JsonSerializer` |
| `infrastructure/.../CollectiveOAuth/Utils/GlobalAuthUtil.cs` | `JObject.Parse`、`Dictionary<string,object>` | `JsonDocument`、强类型 DTO |
| `modularity/system/.../DataInterfaceService.cs:1945-1957` | `JsonConvert.DeserializeObject` | `JsonSerializer.Deserialize` |
| `application/.../HealthCheckModule.cs:69` | `JsonConvert.SerializeObject` | `JsonSerializer.Serialize` |
| `application/.../Handlers/JwtHandler.cs` 多处 | `JObject.FromObject` | `JsonSerializer.SerializeToNode` |

**分步执行**：
1. **SR1-1**：换 JsonSettingsModule → 检测编译错误量，评估全局影响
2. **SR1-2**：迁移 CollectiveOAuth（替换 JObject/Newtonsoft） 
3. **SR1-3**：迁移 DataInterfaceService + HealthCheckModule
4. **SR1-4**：迁移 JwtHandler

**风险**：高 — 影响面广，Newtonsoft 与 System.Text.Json 行为差异（如驼峰命名、DateTime 格式、null 处理）

**预计工时**：8h（4 子任务）

---

### I1：CollectiveOAuth HttpWebRequest → HttpClient async

**文件**：`backend/infrastructure/JNPF.Extras.CollectiveOAuth/Utils/HttpUtils.cs`（全文）

**现状**：
```csharp
var request = (HttpWebRequest)WebRequest.Create(url);
request.Method = "GET";
using var response = (HttpWebResponse)request.GetResponse();
using var stream = response.GetResponseStream();
using var reader = new StreamReader(stream);
var result = reader.ReadToEnd();
```

**修复**：
```csharp
private static readonly HttpClient _httpClient = new HttpClient();

public static async Task<string> GetAsync(string url)
{
    var response = await _httpClient.GetAsync(url);
    return await response.Content.ReadAsStringAsync();
}
```

**影响范围**：所有第三方登录（DingTalk、WeChat、Weibo 等 OAuth 回调）
**风险**：中 — 需确保 `HttpClient` 生命周期正确（静态单例或 `IHttpClientFactory`）
**验证**：
1. `dotnet build backend/`
2. 第三方登录集成测试
3. `pnpm test:api`

**预计工时**：2h

---

### I2：Thirdparty SDK 异步化

**文件列表**：

| 文件 | SDK | 修复 |
|------|-----|------|
| `DingDing/DingUtil.cs` | DingTalk SDK | `Execute` → `ExecuteAsync` |
| `Email/MailUtil.cs` | MailKit | `Connect`/`Send` → `ConnectAsync`/`SendAsync` |
| `Sms/SmsUtil.cs` | 阿里云 SMS | `SendSms` → `SendSmsAsync` |
| `WeChat/WeChatUtil.cs` | Senparc | 统一 async 模式 |

**方法**：查找对应 SDK 的 Async 方法名，替换调用并传播 async

**预计工时**：3h

---

### I3：WebSocket 广播并行化

**文件**：`backend/infrastructure/JNPF.Extras.WebSockets/Handlers/WebSocketHandler.cs:119-136`

**现状**：串行 foreach 发送消息
```csharp
foreach (var socket in sockets)
{
    await socket.SendAsync(...);
}
```

**修复**：并行扇出
```csharp
await Task.WhenAll(sockets.Select(async socket =>
{
    try { await socket.SendAsync(...); }
    catch { /* 单个连接失败不影响其他 */ }
}));
```

**预计工时**：0.5h

---

### I4：WebSocket 缓冲区池化

**文件**：`backend/infrastructure/JNPF.Extras.WebSockets/Middlewares/WebSocketMiddleware.cs:115-135`

**现状**：
```csharp
var buffer = new byte[4096];
var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
```

**修复**：
```csharp
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    // 处理...
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

**预计工时**：1h

---

### API1：JwtHandler 配置访问优化

**文件**：`backend/application/JNPF.API.Entry/Handlers/JwtHandler.cs:51,141,165,194,206,244`

**现状**：`App.GetOptions<JWTSettingsOptions>()` 和 `App.GetConfig("Auth:RoutePolicy")` — 每次认证请求调用

**修复**：注入 `IOptions<JWTSettingsOptions>` + `IConfiguration`
```csharp
public JwtHandler(IOptions<JWTSettingsOptions> jwtOptions, IConfiguration configuration)
{
    _jwtSettings = jwtOptions.Value;
    _routePolicy = configuration["Auth:RoutePolicy"];
}
```

**影响范围**：每次 JWT 认证请求
**风险**：低
**验证**：
1. `dotnet build backend/`
2. Token 认证 + 刷新
3. `pnpm test:api`

**预计工时**：1.5h

---

### API2：添加响应压缩

**文件**：`backend/application/JNPF.API.Entry/` Startup / Program

**修复**：
```csharp
// Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// ...
app.UseResponseCompression();
```

**验证**：检查 `Content-Encoding: br` 响应头

**预计工时**：0.5h

---

### API3：UnsafeRelaxedJsonEscaping 风险评估

**文件**：`backend/framework/JNPF/App/ServeComponent.cs:33`

**说明**：`UnsafeRelaxedJsonEscaping` 允许未转义的 HTML 敏感字符。审计确认是否仅为内部微服务间通信使用（风险可接受），或为用户可见响应（需修复）。

**处理**：
- 如果是内部通信 → 添加注释标注用途 + 降级 P2
- 如果是用户响应 → 改为 `JsonEncodedText.Encode` 或默认编码器

**预计工时**：0.5h

---

> **阶段六审查点**：完成 SR1+I1-I4+API1-API3 后暂停，提交 build + test:api 结果，等待审批。

---

## 八、阶段七：P2 中影响优化

### P2-1：CacheManager 无操作 Select 移除

**文件**：`backend/framework/JNPF/Cache/CacheManager.cs:40`
**修复**：`.Select(u => u)` → 删除该行
**工时**：0.25h

### P2-2：InternalApp Assembly 扫描效率

**文件**：`backend/framework/JNPF/App/Internal/InternalApp.cs`
**修复**：excludeAssemblyNames / supportPackageNamePrefixs → `HashSet<string>`
**工时**：0.5h

### P2-3：EventBusHostedService 事件处理器字典化

**文件**：`backend/framework/JNPF/EventBus/HostedServices/EventBusHostedService.cs:202`
**修复**：预处理为 `Dictionary<string, SortedList<int, EventHandlerWrapper>>` → O(1) 索引
**工时**：1.5h

### P2-4：ChannelEventPublisher 异常处理

**文件**：`backend/framework/JNPF/EventBus/Internal/ChannelEventPublisher.cs:38-60`
**修复**：`Task.Run` fire-and-forget → `await + try-catch + 日志`
**工时**：0.5h

### P2-5：EventBus.Outbox 批量消息处理

**文件**：`backend/infrastructure/JNPF.Extras.EventBus.Outbox/EventBus/Outbox/EventOutboxDispatcher.cs:81-108`
**修复**：`Task.WhenAll` 并行处理消息
**工时**：1h

### P2-6：EventBus.Outbox COUNT 优化

**文件**：`backend/infrastructure/JNPF.Extras.EventBus.Outbox/EventBus/Outbox/SqlSugarEventOutboxStore.cs:126-138`
**修复**：`SELECT COUNT(*) FROM ...` 替代拉回全量后客户端计数
**工时**：0.5h

### P2-7：Entity 添加 Column Length 属性

**范围**：高频查询列的 Entity 文件（TenantEntity、ModuleEntity、UserEntity 等）
**修复**：`[SugarColumn(Length = 64)]` / `[SugarColumn(Length = 256)]` — nvarchar(max) 无法建索引
**工时**：2h（批量添加）

### P2-8：SqlSugarConfigureExtensions AOP 缓存

**文件**：`backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs:214`
**修复**：`ConcurrentDictionary<Type, bool>` 缓存 `IsAssignableFrom` 结果
**工时**：0.5h

### P2-9：FounderGuardMiddleware 异步日志

**文件**：`backend/modularity/inteAssistant/JNPF.InteAssistant/FounderGuardMiddleware.cs:216-242`
**修复**：日志写入 → `Channel<LogEntry>` 后台消费
**工时**：1.5h

### P2-10：DbJobPersistence async void 修复

**文件**：`backend/modularity/common/JNPF.Common.Core/Job/DbJobPersistence.cs:142,184,226`
**修复**：`async void` → `async Task` + 顶层 try-catch
**工时**：1h

### P2-11：DapperRepository 同步 Open 修复

**文件**：`backend/framework/JNPF.Extras.DatabaseAccessor.Dapper/Repositories/DapperRepository.cs:38-45`
**修复**：`.Open()` → `.OpenAsync()`（如果 `IDbConnection` 支持）
**工时**：0.5h

### P2-12：BackgroundTaskShutdownService 阻塞修复

**文件**：`backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskShutdownService.cs:41`
**修复**：`.Wait()` → 注册 `CancellationToken` + `await`
**工时**：0.5h

---

> **阶段七审查点**：完成 P2-1~P2-12 后暂停，提交 build + test:api 结果，等待审批。

---

## 九、阶段八：现代 .NET 机制批量应用

### N1：[GeneratedRegex] 引入

**适用范围**：`DynamicApiController` 路由正则、`SensitiveDetection` 分词正则、`StringExtensions` 中的正则

```csharp
// .NET 8+
#if NET8_0_OR_GREATER
[GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
private static partial Regex SafeIdentifierRegex();
#endif
```

**工时**：1h

### N2：FrozenSet<T> 静态集合

**适用范围**：`InternalApp` 排除/包含数组、`EventBus` 事件处理器缓存

```csharp
private static readonly FrozenSet<string> ExcludeAssemblies = 
    new[] { "JetBrains.", "Microsoft.", "System." }.ToFrozenSet();
```

**工时**：0.5h

### N3：ValueTask 同步缓存路径

**适用范围**：`MemoryCache` Get/Exists 方法

```csharp
public ValueTask<T> GetAsync<T>(string key)
{
    if (_memoryCache.TryGetValue(key, out T value))
        return ValueTask.FromResult(value); // 零分配
    return new ValueTask<T>(GetFromSourceAsync<T>(key));
}
```

**工时**：1h

### N4：ArrayPool<byte> 批量应用

**适用范围**：WebSocket 收发、文件上传 `ToByteArray`、`LoggingMonitor` JSON 构建

**工时**：0.5h（WebSocket 已在 I4 覆盖，此处补充文件上传/日志）

### N5：System.Threading.Channels 引入

**适用范围**：`FounderGuardMiddleware` 日志写入（已在 P2-9 覆盖）

### N6：Mapster 配置去重

**文件**：`backend/framework/JNPF.Extras.ObjectMapper.Mapster/Extensions/ObjectMapperServiceCollectionExtensions.cs:27-34`

**现状**：`Flexible` 配置被后续 `IgnoreCase` 覆盖

**修复**：合并为单次配置 `TypeAdapterConfig.GlobalSettings.Default
    .PreserveReference(true)
    .IgnoreNullValues(true);`

**工时**：0.5h

---

> **阶段八审查点**：完成 N1-N6 后暂停，提交 build + test:api 结果，等待审批。

---

## 十、修复依赖 & 时间估算

### 10.1 依赖图

```
阶段一 (P0 B1-B7)
  │
  ├─→ 阶段二 (P1 反射 R1-R5) ── 无依赖
  │
  ├─→ 阶段三 (P1 字符串 S1-S3) ── 无依赖
  │
  ├─→ 阶段四 (P1 数据访问 D1-D4) ── 依赖 B6（DataBaseManager 竞态）
  │
  ├─→ 阶段五 (P1 Sync-over-Async SA1-SA3) ── 无硬依赖，但建议阶段一后执行
  │
  ├─→ 阶段六 (P1 序列化+基础设施) ── SR1 依赖阶段一完成
  │                                └─ I1-I4 无依赖
  │
  ├─→ 阶段七 (P2) ── 依赖阶段一~六完成（但不强制）
  │
  └─→ 阶段八 (现代.NET) ── 依赖阶段七完成（P2-7 Entity Column 影响 N2）
```

### 10.2 时间估算总表

| 阶段 | 任务 | 工时 | 累计 |
|------|------|------|------|
| 一 | P0 致命缺陷 (B1-B7) | 3.5d | 3.5d |
| 二 | P1 反射热点 (R1-R5) | 2d | 5.5d |
| 三 | P1 字符串分配 (S1-S3) | 1d | 6.5d |
| 四 | P1 数据访问 (D1-D4) | 2d | 8.5d |
| 五 | P1 Sync-over-Async (SA1-SA3) | 2.5d | 11d |
| 六 | P1 序列化+基础设施 (8项) | 3d | 14d |
| 七 | P2 中影响 (12项) | 3d | 17d |
| 八 | 现代.NET 机制 (6项) | 1.5d | 18.5d |

**总计：约 18.5 个工作日**（3.7 周）

### 10.3 可并行执行

- 阶段二 + 阶段三 + 阶段五（无文件冲突）
- 阶段六 I1-I4 + 阶段七 P2-7~P2-12（不同模块）

---

## 十一、每阶段验收标准

### 通用验收门禁

```
☐ dotnet build backend/                                      # 0 Error
☐ dotnet build backend/ -c Release                           # 0 Error
☐ dotnet build backend/ /p:CI_BUILD=true                      # 0 Error (含 Roslyn Analyzer)
☐ E2E_PIPELINE_ID=311 pnpm test:api                          # 全部 PASS
☐ dotnet test backend/zx_lowcode_netcore.sln --no-build       # 全部 PASS (如有)
```

### 阶段性专项验收

| 阶段 | 专项验收 |
|------|---------|
| 一 | WebSocket 广播可达性 / 多租户并发无跨租户污染 / SQL 注入 payload 测试 |
| 二 | ChangeType 快照对比 / TenantId 属性读写正确 / JWT 认证无退化 |
| 三 | 敏感词检测结果一致 / 监控日志完整 / 路由构建无变化 |
| 四 | 菜单树输出快照一致 / 模块导入重复检查正确 / 租户缓存命中率不降 |
| 五 | 表单渲染无超时 / 代码生成输出一致 / 审批流无退化 |
| 六 | 序列化输出快照对比 / 第三方登录可用 / WebSocket 收发无误 |
| 七 | Module 导入性能提升 / 事件总线无丢消息 / Job 异常不崩溃 |
| 八 | Regex 行为一致 / FrozenSet 查找正确 / ArrayPool 无泄漏 |

---

> **本计划待审批后逐阶段执行。每阶段完成后需用户明确审批方可进入下一阶段。**
