# 架构测试方案设计 — JNPF 平台定制

## 项目基因诊断（基于实际代码探索）

### 一、工程结构

```
backend/
├── framework/           (10 项目) — 平台核心。JNPF + 6 个 Extras.* 插件
├── infrastructure/      (5 项目)  — EventBus/WebSocket/OAuth/第三方集成
├── modularity/          (31 项目) — 13 个业务模块。每模块 .Interfaces + .Entitys + 主项目
│   ├── common/          (3 项目)  — JNPF.Common.Core 是全局中枢
│   ├── system/          (3 项目)  — 系统管理模块
│   ├── workflow/        (3 项目)  — 工作流引擎
│   ├── inteAssistant/   (3 项目)  — 智能助手
│   ├── message/         (3 项目)  — 消息模块
│   ├── app/ codegen/ engine/ extend/ oauth/ subdev/ taskscheduler/ visualdata/ visualdev/ zxdev/
├── application/         (2 项目)  — JNPF.API.Entry (主入口) + OA (已禁用)
├── tests/               (7 项目)  — 测试 + 模块验证 + SqlSugar 验证
└── tools/               (3 项目)  — 自定义 Roslyn Analyzer + 数据库迁移
```

**依赖方向**：`application → modularity → infrastructure → framework`（严格单向，不可逆）

### 二、代码生成 vs 手写

| 维度 | 生成代码 | 手写代码 |
|------|---------|---------|
| 模板引擎 | `JNPF.ViewEngine` + `IViewEngine`（Velocity 语法） | — |
| 模板位置 | `wwwroot/Template/` 下 ~85 个 `.vm` 文件 | — |
| 生成服务 | `JNPF.CodeGen.CodeGenService`（`IDynamicApiController`） | — |
| 命名空间 | [COMPUTED] 与手写代码混在同一命名空间，无 `Generated` 前缀 | — |
| 区分标记 | **无** `[GeneratedCode]` 属性、无 "auto-generated" 文件头 | — |
| 架构红线 | R3：bug 修 `.vm` 模板，禁止直接改输出文件 | R1-R10 全部适用 |

**[FRAME→现实] 关键发现**：生成代码和手写代码在命名空间上**没有物理隔离**。传统架构测试（如 NetArchTest）无法通过命名空间区分两者。需另辟路径。

### 三、模块通信模式

| 机制 | 传输 | 实现 |
|------|------|------|
| 接口抽象 | 编译时 | 每个模块的 `.Interfaces` 项目暴露契约 |
| EventBus | 运行时 | `MessageCenter.PublishAsync()` → Channel（进程内）/ RabbitMQ（跨进程） |
| 直接引用 | 编译时 | JNPF.Systems 引用 Message.Interfaces + WorkFlow.Interfaces |

**模块解耦现状**：[INFERRED, MED] 接口层（`.Interfaces`）已经形成物理隔离——模块 A 引用模块 B 时只引用 `.Interfaces` 和 `.Entitys`，不引用 `.Main`。但 Common.Core 引用了所有 `.Entitys`，形成全局耦合点。

### 四、禁止区域 vs 自由区域

| 区域 | 项目 | 约束级别 |
|------|------|---------|
| 🔴 禁用 | `JNPF.OA.*` | L0 Hook 拦截写入 |
| 🔴 不存在 | `JNPF.IoT.*` / `JNPF.MES.*` | L0 Hook 拦截创建 |
| 🟡 平台核心 | `framework/JNPF` + `JNPF.Extras.*` | 二开禁止修改 |
| 🟢 业务模块 | `modularity/*` | 自由开发 |
| 🟢 入口 | `JNPF.API.Entry` | 配置/注册自由 |

### 五、命名空间约定

模式为 `JNPF.{Module}.{Layer}`：
- 模块主项目：`JNPF.Systems`、`JNPF.Systems.Common`、`JNPF.Systems.Permission`
- 接口层：`JNPF.Systems.Interfaces.Permission`
- 实体层：`JNPF.Systems.Entitys.Dto.Tenant`
- 框架扩展：`JNPF.EventBus`、`JNPF.IPCChannel`
- 基础设施：`JNPF.Extras.EventBus.Outbox`
- 框架扩展伪装：`Microsoft.Extensions.DependencyInjection`（AddSqlSugar 等注册方法）

### 六、技术栈确认

| 维度 | 选型 |
|------|------|
| ORM | SqlSugar 5.1.4（主）+ Dapper 2.0（辅） |
| DI | Microsoft.Extensions.DependencyInjection（内置，无 Autofac） |
| API | `IDynamicApiController`（自动路由生成，禁止手写 Controller） |
| 模块化 | 编译时引用 + EventBus（Channel/RabbitMQ）+ 接口抽象 |
| 动态编译 | 无 Roslyn 运行时编译。代码生成在**设计时**通过 Velocity 模板完成 |
| 已有架构守卫 | 自定义 Roslyn Analyzer（注入所有项目）+ L0 Hook（guard-write.mjs） |

---

## 架构测试方案

### 方案 A：增强已有 Roslyn Analyzer（推荐）

**[KNOWN]** 项目已有 `JNPF.Analyzers` 项目，通过 `Directory.Build.props` 注入所有 `.csproj`。在此基础扩展诊断规则：

| 规则 | 检测内容 | Severity |
|------|---------|----------|
| AR001 | `JNPF.OA.*` 命名空间被引用 → Error | R5 |
| AR002 | 模块直接引用其他模块的 `.Main` 项目 → Error | 模块解耦 |
| AR003 | `IDynamicApiController` 实现类无权限属性 → Error | R8 |
| AR004 | 手写 `Controller` 类（非 `LogController`）→ Error | R1 |
| AR005 | `Common.Core` 新增对模块 `.Main` 的引用 → Warning | 耦合趋势 |
| AR006 | 生成代码目录被手写代码引用 → Warning | R3 |

**优点**：编译时拦截，零 CI 成本，已有基础设施。
**失效边界**：只能检测**当前可编译的代码**。不能检测注解缺失、运行时行为、命名规范。

### 方案 B：NetArchTest + xUnit 架构测试

在 `tests/verifications/` 下新增 `ArchitectureVerification` 项目，用 NetArchTest 库编写断言：

```csharp
[Fact]
public void 模块只能引用Interfaces和Entitys()
{
    Types.InAssembly(typeof(JNPF.Systems.UsersService).Assembly)
        .That().HaveDependencyOn("JNPF.WorkFlow")
        .Should().HaveDependencyOn("JNPF.WorkFlow.Interfaces")
        .Or().HaveDependencyOn("JNPF.WorkFlow.Entitys")
        .GetResult().IsSuccessful.Should().BeTrue();
}
```

**优点**：表达能力极强，可编写复杂的依赖拓扑检查。
**失效边界**：需额外 NuGet 依赖。运行时检查而非编译时。依赖程序集加载。

### 方案 C：不新增。强化现有 L0 Hook + CLAUDE.md 硬约束

**[COMMON, MED]** 不引入新工具，在现有体系内加深：
- guard-write.mjs 已有 L4-L8 八层检查
- CLAUDE.md 已有架构红线表
- code-reviewer 子代理已有 5 维度审查

**优点**：零成本。**失效边界**：纯约定，无编译时强制。会话越长漂移越大。

---

## 推荐

**方案 A（增强 Roslyn Analyzer）为主 + 方案 C（Hook 硬防线）兜底。**

理由：
- Roslyn Analyzer 基础设施已就绪，增量成本最低
- 编译时强制 > 运行时检查 > 纯约定
- Hook 硬防线拦截文件级操作（OA 写入等），Analyzer 拦截代码级违规（依赖方向等）
- 不引入新依赖，不改变现有 CI 流程

---

## 信息缺口（阻塞项）

以下问题当前无法从代码中确定，需人工确认：

| # | 问题 | 影响 |
|---|------|------|
| G1 | 代码生成输出到哪个目录？是否与手写代码在同一 `.csproj` 内？ | 决定 R3 检测策略 |
| G2 | 是否有 `JNPF.Generated` 或类似约定但未在当前代码中体现？ | 决定是否可用命名空间区分生成/手写 |
| G3 | 当前 `JNPF.Analyzers` 有哪些现有规则？避免重复开发 | 决定增量的起点 |
| G4 | 团队最痛的架构违规是哪类？模块耦合？R8 权限遗漏？生成代码被篡改？ | 决定规则优先级 |
