# RunService 引擎化重构（战役 1 · S0-S5）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 4157 行上帝类 `RunService` 按 A+C 方案绞杀者式拆分为编译层/数据访问抽象/三个执行引擎 + 缩壳门面，路由契约零差异、引擎层零 SqlSugar 绑定。

**Architecture:** 纯移动 + 依赖倒置。`RunSqlCompiler`（Singleton 纯函数）← `IRuntimeDataStore`（provider 中立抽象，SqlSugar 实现为唯一绑定点）← `RunDataEngine`/`RunListQueryService`/`RunDataViewService`（Transient）← `RunService` 缩壳门面（Transient，维持 IRunService 注册）。每阶段路由快照零 diff + 节点审批。

**Tech Stack:** .NET 8 / SqlSugar / xUnit / JNPF.Startup.Benchmarks harness（`--mode routes`）/ JNPF009 Roslyn 分析器

**设计事实源：** `docs/superpowers/specs/2026-08-20-runservice-engine-refactor-design.md`（下称 spec）
**总纲：** `docs/architecture/runservice-refactor-master-plan.md` v2.1 · **DI 约束表：** `docs/architecture/runservice-refactor-di-constraints.md`
**关联 CR：** `.claude/change-requests/CR-20260820-01.md`

---

## 全局铁律（每个 Task 都生效）

1. **绞杀者纯移动**：S1-S4 禁改方法体逻辑；唯一允许的改写是 S2 Queryable→SqlQueryable（风险7流程）与 S5 门面委托替换。
2. **路由快照硬门控**：每个 Task 收尾必须跑快照比对，零 diff 才能 commit。
3. **JNPF009**：`backend/tools/JNPF.Analyzers/complexity-baseline.json` 只随迁（file/symbol 路径），值不上调不新增。
4. **节点审批**：每阶段（S0-S5）收尾暂停，提交「业务实现+质量自检+功能证据+验收对照」，未经用户审批不进下一阶段。
5. **异常**：引擎内统一 `Oops.Bah()`（业务）/`Oops.Oh()`（系统），不引入新异常层级。
6. **DI 生命周期**：按 DI 约束表——Compiler `ISingleton`；DataStore/三引擎/门面 `ITransient`；引擎构造禁注 SqlSugar 类型与 Scoped 服务。

## 文件结构（拆分决策锁定点）

```
backend/modularity/visualdev/JNPF.VisualDev/
├── RunService.cs                    （4157 行 → S5 后 <400 行门面）
├── Runtime/                         （新增目录，引擎层）
│   ├── RunSqlCompiler.cs            （S1：编译层 7 方法，~1600 行，ISingleton）
│   ├── IRuntimeDataStore.cs         （S2：provider 中立抽象）
│   ├── RuntimeDbLink.cs             （S2：DbLinkEntity 的中立 DTO）
│   ├── SqlSugarRuntimeDataStore.cs  （S2：唯一 SqlSugar 绑定点，ITransient+IDisposable）
│   ├── RunDataEngine.cs             （S3：CRUD/流程表单 20 方法，~1500 行，ITransient）
│   ├── RunListQueryService.cs       （S4：列表编排 5 方法，~800 行，ITransient）
│   └── RunDataViewService.cs        （S4：数据视图 4 方法，~400 行，ITransient）
backend/tests/JNPF.Tests.VisualDev/
├── RunServiceContractTests.cs       （S0：IRunService 反射契约）
├── VisualDevRouteOwnerTests.cs      （S0：三委托方路由归属契约）
├── RunSqlCompilerTests.cs           （S1：编译层特征单测）
└── （已有 23 个 Helpers 测试——纯移动不得破坏，每阶段全绿）
backend/tests/JNPF.Tests.Architecture/
└── RunEngineSqlSugarBoundaryTests.cs（S2：引擎层零 SqlSugar 引用硬门控）
```

## 复用工具与命令速查

| 用途 | 命令 | 工作目录 |
|------|------|---------|
| 路由快照 | `dotnet run --project backend/tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug` | `backend/` |
| 快照比对 | `Compare-Object (Get-Content base.txt) (Get-Content now.txt)` | 仓库根（PowerShell） |
| VisualDev 测试 | `dotnet test backend/tests/JNPF.Tests.VisualDev` | 仓库根 |
| 架构测试 | `dotnet test backend/tests/JNPF.Tests.Architecture --filter FullyQualifiedName~Architecture` | 仓库根 |
| 全量构建 | `dotnet build`（Debug）/ `dotnet build -c Release` | `backend/` |
| JNPF009 门控 | `dotnet build /p:CI_BUILD=true`（0 新增违规） | `backend/` |
| API 快测 | `E2E_PIPELINE_ID=311 pnpm test:api` | 仓库根 |
| 活体冒烟 | `node scripts/lib/jnpf-auth.mjs --json` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` | 仓库根 |
| 启停 dev 栈 | `powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1`（停：`-CleanupOnly`） | 仓库根 |

证据目录：`.claude/evidence/cr-20260820-01/`（每阶段快照/测试输出以 `s{N}-*.txt` 命名落盘）。

---

## S0 安全网

### Task 1: api/visualdev 路由快照基线 + IRunService 契约测试

**Files:**
- Create: `backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs`
- Create: `backend/tests/JNPF.Tests.VisualDev/VisualDevRouteOwnerTests.cs`
- Evidence: `.claude/evidence/cr-20260820-01/s0-routes-visualdev-baseline.txt`

- [ ] **Step 1: 落盘路由快照基线**

```powershell
cd d:\JNPF-v52\backend
dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug *> ..\.claude\evidence\cr-20260820-01\s0-routes-visualdev-baseline.txt
```

Expected: 输出含大量 `[ROUTE] ...` 行 + 末行 `[METRIC] route_total=N route_matched=M filter=api/visualdev`（M>0；若 M=0 停手排查过滤串）。
同时落盘一份 api/permission/users 过滤基线（防误伤，CR-01 已有基线可对照路径 `.claude/evidence/cr-20260819-01/`）。

- [ ] **Step 2: 写 IRunService 反射契约测试（先写即成立——现状守护）**

`backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs`（模式复用 CR-01 `UsersImportExportContractTests`：反射 + 属性名字符串匹配，不引 MVC 类型）：

```csharp
using System.Reflection;
using JNPF.VisualDev.Interfaces;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// IRunService 契约守护（CR-20260820-01 S0）：17 成员签名零变更；
/// S5 瘦身后改为断言 WorkFlow 消费 7 方法签名不变（见 Task 10）。
/// </summary>
public class RunServiceContractTests
{
    private static readonly string[] ExpectedMembers =
    {
        // 开工第一步：用 typeof(IRunService).GetMethods() 打印当前全部方法名回填此数组，
        // 断言 Count==17 与 spec §2 实证一致；回填后本数组即为契约基线
    };

    [Fact]
    public void IRunService_MemberCount_IsSeventeen()
    {
        var methods = typeof(IRunService).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(17, methods.Length);
    }

    [Fact]
    public void IRunService_MemberSignatures_AreFrozen()
    {
        var actual = typeof(IRunService).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})")
            .OrderBy(s => s)
            .ToList();
        // 开工回填：将 actual 打印结果固化为 ExpectedSignatures 数组后启用下行断言
        // Assert.Equal(ExpectedSignatures.OrderBy(s => s), actual);
        Assert.NotEmpty(actual); // 回填前的占位断言——回填后删除本行
    }

    [Theory]
    [InlineData(nameof(IRunService.SaveFlowFormData))]
    [InlineData(nameof(IRunService.GetFlowFormDataDetails))]
    [InlineData(nameof(IRunService.SaveDataToDataByFId))]
    [InlineData(nameof(IRunService.GetDbLink))]
    [InlineData(nameof(IRunService.GetVisualDevModelDataConfig))]
    [InlineData(nameof(IRunService.GetCreateSqlByTemplate))]
    [InlineData(nameof(IRunService.GetUpdateSqlByTemplate))]
    public void WorkFlowConsumed_SevenMethods_Exist(string methodName)
    {
        var method = typeof(IRunService).GetMethod(methodName);
        Assert.True(method != null, $"WorkFlow 消费方法 {methodName} 丢失");
    }
}
```

注意：7 个 `nameof` 若与接口实际成员名不符（spec 消费面来自调用点实证），开工时先核对 `IRunService.cs` 逐一对齐，**以接口为准修正 nameof 而非改接口**。

- [ ] **Step 3: 写三委托方路由归属契约测试**

`VisualDevRouteOwnerTests.cs`：断言 `VisualDevModelDataService`（ApiDescriptionSettings Name=OnlineDev）、`VisualDevService`（Name=Base）、`VisualdevShortLinkService`（Name=ShortLink）三类存在且 `[Route]` 模板不变：

```csharp
using System.Reflection;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// RunService 三委托方路由归属契约（S0）：API 暴露面由委托方间接承载，
/// 类名/Name/Route 模板任一变更即路由契约破坏。
/// </summary>
public class VisualDevRouteOwnerTests
{
    public static IEnumerable<object[]> Owners()
    {
        // 开工回填：核对三类实际 Route 模板与 Name 后固化（从类特性反射读取一次即为基线）
        yield return new object[] { "VisualDevModelDataService", "OnlineDev" };
        yield return new object[] { "VisualDevService", "Base" };
        yield return new object[] { "VisualdevShortLinkService", "ShortLink" };
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void DelegateOwner_KeepsNameAndRoute(string typeName, string expectedName)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetTypes().FirstOrDefault(t => t.Name == typeName))
            .FirstOrDefault(t => t != null);
        Assert.True(type != null, $"委托方 {typeName} 丢失");

        var desc = type!.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == "ApiDescriptionSettingsAttribute");
        Assert.NotNull(desc);
        Assert.Equal(expectedName, (string?)desc!.GetType().GetProperty("Name")!.GetValue(desc));

        var route = type.GetCustomAttributes()
            .SingleOrDefault(a => a.GetType().Name == "RouteAttribute");
        Assert.NotNull(route); // 模板值开工时反射读取后固化为第三列断言
    }
}
```

- [ ] **Step 4: 跑绿 + 落盘证据**

```powershell
dotnet test backend/tests/JNPF.Tests.VisualDev --filter "FullyQualifiedName~RunServiceContractTests|FullyQualifiedName~VisualDevRouteOwnerTests"
```

Expected: 全绿（签名守护测试对现状必然绿；不绿说明回填有误）。

- [ ] **Step 5: Commit**

```powershell
git add backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs backend/tests/JNPF.Tests.VisualDev/VisualDevRouteOwnerTests.cs .claude/evidence/cr-20260820-01/s0-routes-visualdev-baseline.txt
git commit -m "test(visualdev): S0 安全网 — api/visualdev 路由快照基线 + IRunService/委托方契约测试 [CR-20260820-01]"
```

- [ ] **Step 6: S0 节点审批** — 提交「快照基线行数/METRIC + 契约测试绿数 + 证据路径」，等待用户批准进 S1。

---

## S1 编译层

### Task 2: RunSqlCompiler 骨架 + 7 方法纯移动 + JNPF009 随迁

**Files:**
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunSqlCompiler.cs`
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`（移出 7 方法 + 注入 Compiler）
- Modify: `backend/tools/JNPF.Analyzers/complexity-baseline.json`（8 条中属 Compiler 的条目随迁）

- [ ] **Step 1: 建骨架**

```csharp
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// 运行态 SQL 编译层（A+C 战役 1 · S1）：可视化查询条件/模型 JSON → SQL/IConditionalModel。
/// 纯函数、零 DB 依赖、零可变状态（DI 约束表：Singleton；禁注入任何 DB/请求上下文类型）。
/// 自 RunService 绞杀者式纯移动，方法体零改写。
/// </summary>
public class RunSqlCompiler : ISingleton
{
    // 移入方法：GetListQuerySql(源行2302)、GetInfoQuerySql(2907)、GetQueryJson(2967)、
    // GetSuperQueryJson(3517)、GetSuperQueryInput(2189)、
    // GetIConditionalModelListByTableName(2896)、GetVisualDevModelDataConfig(2016)
}
```

- [ ] **Step 2: 纯移动 7 方法 + 其私有辅助**

移动清单（spec §3.1，行号为移动前基线）：`GetListQuerySql`、`GetInfoQuerySql`、`GetQueryJson`、`GetSuperQueryJson`、`GetSuperQueryInput`、`GetIConditionalModelListByTableName`、`GetVisualDevModelDataConfig`，以及**仅被上述方法调用的私有辅助**（移动前用 IDE Find References 逐一确认调用面；被其他未迁移方法共享的辅助留在 RunService，Compiler 经参数传入结果）。

**DB 依赖处置**（唯一允许的签名调整）：若某方法体内有 `_visualDevRepository`/`_sqlSugarClient` 调用，把该 DB 调用**留在 RunService 侧**取数后经参数传入 Compiler（编译层零 DB 是硬约束）；改动仅限参数化重排，SQL 拼装逻辑逐字不动。

RunService 侧同步：构造注入 `RunSqlCompiler _compiler`，原方法体改为一行委托 `return _compiler.GetListQuerySql(...)`（或直接删除方法、调用点改 `_compiler.X`——模块内调用点选后者，`IRunService` 成员必须保留委托转发）。

- [ ] **Step 3: JNPF009 基线随迁**

`complexity-baseline.json` 中 `RunService.GetListQuerySql`(CC140)/`GetSuperQueryInput`/`GetQueryJson` 等属 Compiler 的条目：`file` 改为 `Runtime/RunSqlCompiler.cs`、symbol 前缀改 `RunSqlCompiler.`，**值不变**。

```powershell
dotnet build backend /p:CI_BUILD=true   # Expected: 0 新增 JNPF009 违规
```

- [ ] **Step 4: 构建 + 全量测试 + 快照比对**

```powershell
dotnet build backend
dotnet test backend/tests/JNPF.Tests.VisualDev
cd backend; dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug *> ..\.claude\evidence\cr-20260820-01\s1-routes.txt
Compare-Object (Get-Content .claude\evidence\cr-20260820-01\s0-routes-visualdev-baseline.txt) (Get-Content .claude\evidence\cr-20260820-01\s1-routes.txt)
```

Expected: 测试全绿（含既有 23 个 Helpers 测试）；Compare-Object 零输出。

- [ ] **Step 5: Commit**

```powershell
git add backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunSqlCompiler.cs backend/modularity/visualdev/JNPF.VisualDev/RunService.cs backend/tools/JNPF.Analyzers/complexity-baseline.json
git commit -m "refactor(visualdev): S1 RunSqlCompiler 编译层纯移动（7方法+JNPF009随迁）[CR-20260820-01]"
```

### Task 3: RunSqlCompiler 特征单测（重构后首个可单测面）

**Files:**
- Test: `backend/tests/JNPF.Tests.VisualDev/RunSqlCompilerTests.cs`

- [ ] **Step 1: 特征捕获** — 对 7 个编译方法各构造 2-3 组代表性输入（覆盖 spec §8 门禁：JOIN/过滤/子查询/分页/超级查询）。特征值不得手写猜测：先写临时输出代码跑现状 `RunSqlCompiler` 得到真实输出，固化为期望值：

```csharp
using JNPF.VisualDev.Runtime;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class RunSqlCompilerTests
{
    private readonly RunSqlCompiler _compiler = new();

    [Fact]
    public void GetSuperQueryInput_Characterization_IsStable()
    {
        // 输入按方法实际签名构造（开工时读 RunSqlCompiler 签名）
        var result = _compiler.GetSuperQueryInput(/* 特征输入 */);
        Assert.Equal(/* 特征捕获的期望输出 */, result);
    }

    [Fact]
    public void GetListQuerySql_Characterization_IsStable()
    {
        var sql = _compiler.GetListQuerySql(/* 特征输入：分页+过滤 */);
        Assert.Equal(/* 特征捕获的 SQL 文本 */, sql);
    }

    // 其余 5 方法同模式：每方法 ≥2 用例（正常路径 + 空条件/边界路径）
}
```

- [ ] **Step 2: 跑绿** — `dotnet test backend/tests/JNPF.Tests.VisualDev --filter FullyQualifiedName~RunSqlCompilerTests`，Expected: 全绿。
- [ ] **Step 3: Commit** — `git commit -m "test(visualdev): S1 RunSqlCompiler 特征单测（编译路径守护）[CR-20260820-01]"`
- [ ] **Step 4: S1 节点审批** — 提交：移动方法清单+行号对照、快照零 diff 证据、单测数、CI_BUILD 0 违规。批准后进入 S2。

---

## S2 数据访问抽象

### Task 4: IRuntimeDataStore 契约 + SqlSugar 实现 + 架构测试硬门控

**Files:**
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/IRuntimeDataStore.cs`
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RuntimeDbLink.cs`
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/SqlSugarRuntimeDataStore.cs`
- Test: `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs`

- [ ] **Step 1: 先写架构测试（失败优先——此时引擎类尚不存在 SqlSugar 引用，测试应绿；它守护的是后续 Task 5 的收敛）**

```csharp
using System.Reflection;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// 引擎层 SqlSugar 边界硬门控（spec §8：S2 起）：
/// Runtime 目录引擎类禁止直接引用 SqlSugar 类型；
/// 唯一绑定点 SqlSugarRuntimeDataStore 豁免。
/// </summary>
public class RunEngineSqlSugarBoundaryTests
{
    private static readonly string[] EngineTypeNames =
    {
        "RunSqlCompiler", "RunDataEngine", "RunListQueryService",
        "RunDataViewService", "RunService",
    };

    [Fact]
    public void EngineTypes_DoNotReferenceSqlSugar()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "JNPF.VisualDev");

        foreach (var name in EngineTypeNames)
        {
            var type = assembly.GetType($"JNPF.VisualDev.Runtime.{name}")
                ?? (name == "RunService" ? assembly.GetType("JNPF.VisualDev.RunService") : null);
            Assert.True(type != null, $"引擎类 {name} 缺失");

            var violations = CollectSqlSugarReferences(type!);
            Assert.True(violations.Count == 0,
                $"{name} 直接引用 SqlSugar 类型：{string.Join("; ", violations)}（唯一绑定点应为 SqlSugarRuntimeDataStore）");
        }
    }

    private static List<string> CollectSqlSugarReferences(Type type)
    {
        var result = new List<string>();
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            if (field.FieldType.FullName?.StartsWith("SqlSugar") == true)
                result.Add($"field {field.Name}:{field.FieldType.Name}");
        foreach (var ctor in type.GetConstructors())
            foreach (var p in ctor.GetParameters())
                if (p.ParameterType.FullName?.StartsWith("SqlSugar") == true)
                    result.Add($"ctor param {p.Name}:{p.ParameterType.Name}");
        foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (m.ReturnType.FullName?.StartsWith("SqlSugar") == true)
                result.Add($"method {m.Name} returns {m.ReturnType.Name}");
            foreach (var p in m.GetParameters())
                if (p.ParameterType.FullName?.StartsWith("SqlSugar") == true)
                    result.Add($"method {m.Name} param {p.Name}:{p.ParameterType.Name}");
        }
        return result;
    }
}
```

注意：S2 完成时 `RunService` 门面尚在缩壳前，仍持 `_sqlSugarClient`——Task 4 阶段先把 `"RunService"` 从 `EngineTypeNames` 注释豁免（加 TODO-S5 标注），Task 10 门面缩壳后恢复并断言。RunSqlCompiler（Task 2 已零 DB）此刻必须绿。

```powershell
dotnet test backend/tests/JNPF.Tests.Architecture --filter FullyQualifiedName~RunEngineSqlSugarBoundaryTests
```
Expected: PASS（RunSqlCompiler 已零 SqlSugar）。

- [ ] **Step 2: 写 IRuntimeDataStore + RuntimeDbLink**

`IRuntimeDataStore.cs` 逐字采用 spec §4 契约：

```csharp
using System.Data;
using System.Data.Common;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// provider 中立运行时数据访问抽象（A+C 战役 1 · S2）：
/// 自 RunService 49+8 处真实调用面提取，只含现有能力（YAGNI）。
/// PostgreSQL 经 Dialect + ISqlDialectAdapter（backlog 2.8）扩展；时序库走独立 ITimeSeriesStore（backlog 2.9）。
/// </summary>
public interface IRuntimeDataStore
{
    /// <summary>数据库方言标识：sqlserver / mysql / postgresql / ...</summary>
    string Dialect { get; }

    Task<object?> ExecuteScalarAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<int> ExecuteCommandAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<List<Dictionary<string, object>>> SqlQueryAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<DataTable> GetDataTableAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<bool> AnyAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task RunInTransactionAsync(Func<Task> action, string? tenantId = null);

    /// <summary>外部数据源链接路由（承接 GetDbLink + _sqlSugarClient 连接切换）</summary>
    RuntimeDbLink? ResolveDbLink(string linkId, string? tenantId = null);
}

/// <summary>DbLinkEntity 的 provider 中立投影（引擎层不见 SqlSugar/JNPF 实体具体类型）.</summary>
public record RuntimeDbLink(string Id, string DbType, string ConnectionString);
```

- [ ] **Step 3: 写 SqlSugarRuntimeDataStore（承接原 RunService 的 _sqlSugarClient 状态与 Dispose）**

```csharp
using JNPF.DependencyInjection;
using SqlSugar;
using System.Data;
using System.Data.Common;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// IRuntimeDataStore 的 SqlSugar 实现——A+C 唯一 provider 绑定点（S2）。
/// 承接原 RunService._sqlSugarClient 状态与 Dispose 语义（DI 约束表：Transient）。
/// 租户切换（AsTenant）与外部数据源连接切换收敛于本层。
/// </summary>
public class SqlSugarRuntimeDataStore : IRuntimeDataStore, ITransient, IDisposable
{
    private readonly SqlSugarScope _client; // 从 RunService 构造函数原样移植初始化逻辑

    public string Dialect => _client.CurrentConnectionConfig.DbType.ToString().ToLowerInvariant();

    // 构造：把 RunService 中 _sqlSugarClient 的获取/初始化代码原样搬入（含 DataBaseManager 交互），逻辑零改写
    // 各方法实现：把 RunService 中对应 _sqlSugarClient/_visualDevRepository.AsSugarClient()
    // 调用点的执行语义（Ado 执行/查询/事务/AsTenant 切换）收敛至此——实现体从原调用点剪切粘贴，禁改写

    public void Dispose() /* 原 RunService.Dispose 语义原样搬入 */ { }
}
```

- [ ] **Step 4: 构建 + 测试 + Commit**

```powershell
dotnet build backend
dotnet test backend/tests/JNPF.Tests.Architecture --filter FullyQualifiedName~RunEngineSqlSugarBoundaryTests
git add backend/modularity/visualdev/JNPF.VisualDev/Runtime/IRuntimeDataStore.cs backend/modularity/visualdev/JNPF.VisualDev/Runtime/RuntimeDbLink.cs backend/modularity/visualdev/JNPF.VisualDev/Runtime/SqlSugarRuntimeDataStore.cs backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs
git commit -m "feat(visualdev): S2 IRuntimeDataStore 抽象 + SqlSugar 实现 + 架构边界测试 [CR-20260820-01]"
```

### Task 5: 49+8 处调用收敛 + Queryable→SqlQueryable 等价比对（风险7）

**Files:**
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/*.cs`（调用面收敛）

- [ ] **Step 1: 建收敛台账** — 逐处登记 49 处 `_visualDevRepository.AsSugarClient()` + 8 处 `_sqlSugarClient`，每行：行号/调用类别（Queryable27/Utilities12/SqlQueryable7/CurrentConnectionConfig3/AsTenant4）/收敛去向。台账落盘 `.claude/evidence/cr-20260820-01/s2-convergence-ledger.md`。

- [ ] **Step 2: 分类收敛**
  - **SQL 执行/查询类**（Ado 语义）→ 改经 `IRuntimeDataStore` 对应方法；
  - **AsTenant×4** → 收敛至 `SqlSugarRuntimeDataStore` 内部；
  - **Utilities×12** → provider 相关部分下沉 DataStore，纯工具逻辑上移引擎类；
  - **CurrentConnectionConfig×3** → 经 `Dialect`/`ResolveDbLink` 替代；
  - **Queryable LINQ×27（行为敏感，风险7）**：涉运行时业务表的改 `SqlQueryable`（编译 SQL）；元数据实体（VisualDevEntity 等平台表）保留原仓储用法。

- [ ] **Step 3: 每处 Queryable 改写的 SQL 等价比对** — 改写前 `ToSql()` 抓取原 LINQ 链 SQL，改写后抓取 SqlQueryable SQL，逐处比对（允许参数占位符格式差异，语义必须等价）；比对记录追加进台账。**无法等价改写处保留原用法并在台账登记豁免理由**（spec 风险7）。

- [ ] **Step 4: 架构测试转严** — 此时 RunService 应已不持 SqlSugar 字段（状态已迁 DataStore）；恢复 Task 4 Step 1 注释掉的 `"RunService"` 断言，跑：

```powershell
dotnet test backend/tests/JNPF.Tests.Architecture --filter FullyQualifiedName~RunEngineSqlSugarBoundaryTests
```
Expected: PASS（若 RunService 残留 SqlSugar 引用，继续收敛直至绿）。

- [ ] **Step 5: 活体冒烟（含外部数据源链路）**

```powershell
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1    # 后台起栈
node scripts/lib/jnpf-auth.mjs --json
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser                  # Expected: 200
```
外部数据源链路实测：用已配置外部数据链接的 OnlineDev 功能端点发起一次列表查询（`jnpf-api.mjs` 调对应 visualdev 端点，Expected 200 且数据结构与改前一致）；无可用外部源时以台账登记说明并用默认库 SqlQueryable 路径冒烟替代。完成后 `start-dev.ps1 -CleanupOnly`。

- [ ] **Step 6: 快照零 diff + Commit**

```powershell
cd backend; dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug *> ..\.claude\evidence\cr-20260820-01\s2-routes.txt
Compare-Object (Get-Content .claude\evidence\cr-20260820-01\s0-routes-visualdev-baseline.txt) (Get-Content .claude\evidence\cr-20260820-01\s2-routes.txt)   # Expected: 零输出
git add -A; git commit -m "refactor(visualdev): S2 数据访问收敛 — 49+8 处经 IRuntimeDataStore，Queryable 等价比对 [CR-20260820-01]"
```

- [ ] **Step 7: S2 节点审批** — 提交：收敛台账（49+8 全勾）、豁免登记、架构测试绿、等价比对记录、冒烟证据。批准后进入 S3。

---

## S3 执行层

### Task 6: RunDataEngine 拆分（20 方法纯移动）

**Files:**
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunDataEngine.cs`
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`
- Modify: `backend/tools/JNPF.Analyzers/complexity-baseline.json`

- [ ] **Step 1: 建骨架**

```csharp
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// 运行态执行层（A+C 战役 1 · S3）：CRUD + 唯一校验 + 乐观锁 + 流程表单数据。
/// 自 RunService 绞杀者式纯移动（DI 约束表：Transient；注入 RunSqlCompiler + IRuntimeDataStore，禁 SqlSugar 类型）。
/// </summary>
public class RunDataEngine : ITransient
{
    private readonly RunSqlCompiler _compiler;
    private readonly IRuntimeDataStore _dataStore;

    public RunDataEngine(RunSqlCompiler compiler, IRuntimeDataStore dataStore)
    {
        _compiler = compiler;
        _dataStore = dataStore;
    }
}
```

- [ ] **Step 2: 纯移动 20 方法 + 专属私有辅助**（spec §3.1 清单）：Create(615)、CreateHaveTableSql(670)、GetCreateSqlByTemplate(677)、GenerateFeilds(1748)、FieldBindDefaultValue(1995)、UniqueVerify(2201)、Update(878)、BatchUpdate(937)、UpdateHaveTableSql(1026)、GetUpdateSqlByTemplate(1032)、DelHaveTableInfo(1495)、DelInteAssistant(1593)、BatchDelHaveTableData(1637)、DeleteRootFlowTasks(1727)、GetAllowDeleteFlowTaskList(2178)、SaveFlowFormData(1250)、GetFlowFormDataDetails(1316)、SaveDataToDataByFId(1362)、OptimisticLocking(3808)、DataTransferVerify(3864)。

移动纪律：方法体逐字不动；DB 调用已在 S2 收敛为 `_dataStore.*`；共享辅助的归属用 Find References 裁定（被多引擎共享的辅助提升为 internal static 工具类 `Runtime/RuntimeSharedHelpers.cs`，登记于台账）。`IRunService` 成员（SaveFlowFormData 等）在 RunService 保留一行委托。

- [ ] **Step 3: JNPF009 随迁** — `SaveDataToDataByFId`(CC90)/`GenerateFeilds`(CC81)/`FieldBindDefaultValue`(CC82)/`DataTransferVerify`(CC74) 条目 file/symbol 改指 RunDataEngine，值不变。`dotnet build backend /p:CI_BUILD=true` Expected 0 新增。

- [ ] **Step 4: 验证四件套**

```powershell
dotnet build backend
dotnet test backend/tests/JNPF.Tests.VisualDev
dotnet test backend/tests/JNPF.Tests.Architecture --filter FullyQualifiedName~RunEngineSqlSugarBoundaryTests
cd backend; dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug *> ..\.claude\evidence\cr-20260820-01\s3-routes.txt
Compare-Object (Get-Content .claude\evidence\cr-20260820-01\s0-routes-visualdev-baseline.txt) (Get-Content .claude\evidence\cr-20260820-01\s3-routes.txt)
```
Expected: 全绿 + 零 diff。

- [ ] **Step 5: CRUD 全链路冒烟** — 起栈后用 OnlineDev 功能端点走一次「建表单数据→查详情→更新→删除」链路（`jnpf-api.mjs` POST/GET/PUT/DELETE，逐条 Expected 200），证据落盘 `s3-crud-smoke.txt`。
- [ ] **Step 6: Commit** — `git commit -m "refactor(visualdev): S3 RunDataEngine 执行层纯移动（20方法+JNPF009随迁）[CR-20260820-01]"`
- [ ] **Step 7: S3 节点审批** — 批准后进入 S4。

---

## S4 列表/视图层

### Task 7: RunListQueryService 拆分（5 方法）

**Files:**
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunListQueryService.cs`
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs` · `complexity-baseline.json`

- [ ] **Step 1: 骨架 + 纯移动**

```csharp
public class RunListQueryService : ITransient
{
    private readonly RunSqlCompiler _compiler;
    private readonly IRuntimeDataStore _dataStore;
    // 移入：GetListResult(168,CC85)、GetRelationFormList(312)、GetHaveTableInfo(418)、
    // GetHaveTableInfoDetails(509)、GetListChildTable(3577) + 专属私有辅助
}
```

- [ ] **Step 2: JNPF009 随迁**（`GetListResult` 条目；CC85 值不变）→ `dotnet build backend /p:CI_BUILD=true` 0 新增。
- [ ] **Step 3: 验证四件套**（同 Task 6 Step 4，快照落盘 `s4a-routes.txt`，零 diff）。
- [ ] **Step 4: 列表分页冒烟** — OnlineDev 列表端点：首页/翻页/条件过滤各一次（Expected 200 + 数据形态与 S3 后一致），落盘 `s4a-list-smoke.txt`。
- [ ] **Step 5: Commit** — `git commit -m "refactor(visualdev): S4a RunListQueryService 列表层纯移动 [CR-20260820-01]"`

### Task 8: RunDataViewService 拆分（4 方法）

**Files:**
- Create: `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunDataViewService.cs`
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`

- [ ] **Step 1: 骨架 + 纯移动**

```csharp
public class RunDataViewService : ITransient
{
    private readonly RunSqlCompiler _compiler;
    private readonly IRuntimeDataStore _dataStore;
    // 移入：GetDataViewResults(3873)、GetDataViewQuery(4038)、AddDataViewId(4015)、GetPageToDataTable(3998)
}
```

- [ ] **Step 2: 验证四件套**（快照 `s4b-routes.txt` 零 diff）。
- [ ] **Step 3: 数据视图冒烟** — 数据视图端点查询一次（Expected 200），落盘 `s4b-view-smoke.txt`。
- [ ] **Step 4: Commit** — `git commit -m "refactor(visualdev): S4b RunDataViewService 视图层纯移动 [CR-20260820-01]"`
- [ ] **Step 5: S4 节点审批** — 批准后进入 S5。

---

## S5 收尾

### Task 9: Common.CodeGen 注入点切换 CR（先行审批）

**Files:**
- Create: `.claude/change-requests/CR-{当日日期}-runservice-codegen-injection.md`
- Modify: `backend/modularity/common/Common.CodeGen/.../ExportImportDataHelper.cs`（CR 批准后）

- [ ] **Step 1: 起草 CR** — 内容：切换点（ExportImportDataHelper 构造注入的 RunService 具体类）、目标注入类型（按实际消费方法选择：仅用列表能力→`RunListQueryService`；混合→门面 `RunService` 保留至下一轮）、回滚方式（git revert 单 commit）。
- [ ] **Step 2: 提交用户审批** — 批准后将 `workflow-state.json` 标 `cr-approved`（L10a 放行），未批不动代码。
- [ ] **Step 3: 批准后切换** — 改注入类型 + 调用点方法名对齐（消费方法若已迁引擎，按引擎类 public 方法直调）；验证四件套 + 快照零 diff（`s5a-routes.txt`）。
- [ ] **Step 4: Commit** — `git commit -m "refactor(codegen): S5a ExportImportDataHelper 注入点切换（CR 已批）[CR-20260820-01]"`

### Task 10: 门面缩壳 + IRunService 17→7 + 模块内注入点切换

**Files:**
- Modify: `backend/modularity/visualdev/JNPF.VisualDev.Interfaces/IRunService.cs`（17→7）
- Modify: `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`（缩壳 <400 行）
- Modify: 模块内 4 注入点（`VisualDevService`/`VisualDevModelDataService`/`VisualdevShortLinkService`/`VisualdevModelAppService`）
- Modify: `backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs`（契约改严）
- Modify: `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs`（恢复 RunService 断言，若 Task 5 未恢复）

- [ ] **Step 1: 前置核验 WorkFlow 消费面** — grep WorkFlow 模块对 `IRunService` 的全部调用（`FlowTaskManager`/`FlowFormService`/`FlowTaskOtherUtil`），逐一确认落在 7 方法内；发现第 8 个即停手上报（spec 消费面是 S0 实证，此处复核）。
- [ ] **Step 2: IRunService 瘦身** — 删除非 7 方法成员；被删成员的实现方法在对应引擎类上已是 public（S1-S4 保证），WorkFlow 编译即验证无遗漏。
- [ ] **Step 3: 门面缩壳** — RunService 只保留：IRunService 7 方法委托 + `GetDbLink` 等基础设施方法（下沉 DataStore 的改委托）+ 构造函数注入四引擎。目标 <400 行；超了就检查是否有未迁方法残留。
- [ ] **Step 4: 4 处模块内注入点切换** — 按各委托方实际消费面改注入类型（消费单一引擎的直注引擎类；混合消费保留门面注入）。逐点切换逐点构建。
- [ ] **Step 5: 契约测试改严** — `RunServiceContractTests`：`IRunService_MemberCount_IsSeventeen` 改为断言 **7**；启用 S0 注释的签名冻结断言（基线为瘦身后 7 方法签名）；`WorkFlowConsumed_SevenMethods_Exist` 保持绿。
- [ ] **Step 6: 架构测试全严** — `EngineTypeNames` 含 `"RunService"`，断言门面也零 SqlSugar 引用（状态已全在 DataStore）。
- [ ] **Step 7: 全回归链（spec §8 总表逐项）**

```powershell
dotnet build backend                                   # Debug 0 错误
dotnet build backend -c Release                        # Release 0 错误
dotnet build backend /p:CI_BUILD=true                  # JNPF009 0 新增
dotnet test backend/tests/JNPF.Tests.VisualDev         # 全绿
dotnet test backend/tests/JNPF.Tests.Architecture      # 全绿
cd backend; dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev" --config Debug *> ..\.claude\evidence\cr-20260820-01\s5-routes.txt
Compare-Object (Get-Content .claude\evidence\cr-20260820-01\s0-routes-visualdev-baseline.txt) (Get-Content .claude\evidence\cr-20260820-01\s5-routes.txt)   # 零输出
E2E_PIPELINE_ID=311 pnpm test:api                      # 全绿（仓库根）
```
活体冒烟：起栈 → 登录 → CurrentUser 200 → OnlineDev 列表/表单/数据视图三类端点 200 → 落盘 `s5-live-smoke.txt` → `-CleanupOnly`。

- [ ] **Step 8: Commit** — `git add -A; git commit -m "refactor(visualdev): S5 门面缩壳 + IRunService 17→7 + 注入点全切换 [CR-20260820-01]"`
- [ ] **Step 9: S5 终审** — 提交完整验收对照（spec §8 六门禁逐项证据）+ 度量终值（行数：RunService<400、各引擎规模 vs 估算）+ CR-20260820-01 勾选归档。

---

## Self-Review 记录（计划作者自查）

1. **Spec 覆盖**：§1 目标1-5→Task2/4/6/10；§3 组件→文件结构锁定；§4 契约→Task4 Step2 逐字；§6 S0-S5→Task1-10 一一对应；§7 风险1→Task5 Step5、风险2/7→Task5 Step3、风险3→各随迁步、风险4→Task9、风险5→Task1/3、风险6→每 Task 独立 commit；§8 六门禁→Task10 Step7 逐项。战役 0 前置已闭环（不在本计划）。✅
2. **占位符扫描**：契约数组/特征值标注为「开工回填/特征捕获」并给出回填方法与前置断言——这是现状守护测试的正确形态（基线值只能由执行者从运行中的代码捕获，计划内手写即为伪造），非 TBD。✅
3. **类型一致性**：`IRuntimeDataStore`/`RuntimeDbLink` 签名在 Task4/5/6/7/8 一致；引擎构造参数 `(RunSqlCompiler, IRuntimeDataStore)` 全计划统一；生命周期标记与 DI 约束表一致（Compiler ISingleton、其余 ITransient）。✅
