# JNPF v5.2 后端架构认知文档（读懂结构的唯一入口）

> **定位**：全员认知基线文档。任何 CR 评审、新人 onboarding、架构讨论之前必须先读本文。
> **效力**：本文描述的是**实际结构**（全部数据经实测扫描），不是教科书模型。
> 生成日期：2026-08-19 · 扫描脚本：`scripts/arch-module-dependency-scan.ps1`（可复跑）· 证据：`.claude/evidence/arch-scan/*.json`

---

## 1. 一句话心智模型

**这不是 ABP vNext 式「按技术垂直分层」的结构，而是「框架内核 + 水平模块化」的洋葱式结构：**

- 垂直分层（Domain/Application/HttpApi/Infrastructure）**没有**作为顶层目录存在；
- 垂直分层**藏在每个业务模块内部**（`.Interfaces` / `.Entitys` / 主工程三段式）；
- 顶层四个目录是**同心圆**，不是**上下层**。

用「洋葱 + 模块切片」模型看就通了；用 ABP 视角看必然困惑（见 §5 对照表）。

---

## 2. 四目录「名实对照表」（最大歧义源，必读）

| 顶层目录 | ABP vNext 中同名概念 | **本仓库实际含义** | 关键证据 |
|---------|---------------------|-------------------|---------|
| `application/` | 应用服务层（AppService/DTO） | ❌ **宿主壳**：仅 2 个可执行入口 `JNPF.API.Entry`（主）+ `JNPF.OA.API.Entry`（OA），负责模块组装/中间件管道/启动配置。相当于 ABP 的 `*.HttpApi.Host` | `application/JNPF.API.Entry/Program.cs`：`Serve.Run` + `WebComponent` |
| `framework/` | （无对应概念） | **vendored Furion 框架源码**（fork 进仓库的内核，非 NuGet 包）：动态 API、DI 扫描、`Oops`、`Serve`、JWT/SqlSugar/Serilog Extras，共 8 工程 | `framework/JNPF/App/App.cs`（静态容器）、csproj 中 Furion 包引用 = 0 |
| `infrastructure/` | 基础设施层（仓储实现/DbContext） | ❌ **跨切面技术件**：仅 5 个扩展工程（EventBus.Outbox / EventBus.RabbitMQ / WebSockets / CollectiveOAuth / Thirdparty）。真正的数据访问基础设施在 `framework/JNPF.Extras.DatabaseAccessor.SqlSugar` | `infrastructure/` 目录清单实测 |
| `modularity/` | （模块= NuGet 包概念） | **全部业务代码**：16 个业务域（system/oauth/workflow/visualdev/inteAssistant/codegen…），ABP 的 Application 层职责由各模块 `*Service.cs` 实现 `IDynamicApiController` 承担 | 155 个 `IDynamicApiController` 实现实测 |

**结论**：目录名是历史遗留，**不做改名**（72 个 csproj + CI 脚本 + 文档，ROI 为负；决策 D5）。语义以本文档为准。

---

## 3. 依赖方向全景图

```mermaid
flowchart TB
    subgraph HOST["application/ 宿主壳（组合根，唯一允许全量引用处）"]
        Entry["JNPF.API.Entry<br/>Serve.Run → 14个JnpfModule拓扑装配<br/>Startup.cs · Modules/*.cs"]
    end
    subgraph BIZ["modularity/ 16个业务模块（垂直切片）"]
        direction LR
        SYS["system/<br/>JNPF.Systems.Interfaces 契约<br/>JNPF.Systems.Entitys 实体+DTO<br/>JNPF.Systems Service=API"]
        OTHERS["oauth/ workflow/ visualdev/<br/>inteAssistant/ codegen/ ..."]
        COMMON["common/<br/>JNPF.Common.Core 共享内核(50类型)<br/>JNPF.Common · JNPF.Common.CodeGen"]
    end
    subgraph TECH["infrastructure/ 跨切面技术件"]
        EB["EventBus.Outbox · RabbitMQ<br/>WebSockets · CollectiveOAuth"]
    end
    subgraph KERNEL["framework/ vendored Furion 内核"]
        FW["JNPF 核心 + JNPF.Extras.*<br/>(SqlSugar/JWT/Serilog/Mapster)"]
    end
    Entry --> BIZ & TECH
    BIZ --> COMMON
    BIZ --> FW
    TECH --> FW
    SYS --".Interfaces契约引用(合规)".-> OTHERS
    SYS -.-x|"实现工程引用(违规存量46处)"| OTHERS
```

**图 3-1 洋葱式依赖方向**（依据 csproj ProjectReference 全量扫描，2026-08-19）

读懂要点：

1. **依赖只能由外向内**：宿主 → 模块 → 共享内核/技术件 → 框架内核。反向即违规。
2. **模块间通信的合规通道**：引用对方的 `.Interfaces` 工程（契约）；跨模块事件走 EventBus。
3. **强制守护目前很窄**：`tests/JNPF.Tests.Architecture/LayeringTests.cs`（ARCH-01）仅硬拦
   「framework/common 不得依赖 InteAssistant」；完整模块依赖矩阵测试为战役 2 候选项（尚未实施）。

---

## 4. 模块内部结构（垂直分层的真正所在）

以 `modularity/system/` 为例（其余模块同构）：

| 工程 | 职责 | 对应 ABP 概念 |
|------|------|--------------|
| `JNPF.Systems.Interfaces` | 对外服务契约（供他模块引用） | Application.Contracts |
| `JNPF.Systems.Entitys` | Entity（SqlSugar 贫血实体）+ DTO + Enum + Mapper | Domain 实体（贫血）+ DTO |
| `JNPF.Systems` | Service（实现 `IDynamicApiController`）= **API + 应用逻辑 + 数据访问三层塌缩** | Application + HttpApi + 部分 Infrastructure 合体 |

**塌缩的代价**：69 个 >500 行文件（Top：`CodeGenFormControlDesignHelper` 3757 行、`RunService` 3734 行、
`UsersService` 2644 行 45 方法）——这是战役 1 的治理对象。
**刻意不补 Domain 层**：贫血实体 + SqlSugar 是低代码场景的既定取舍（决策 D1/D5），强行 DDD 化制造更大混乱。

---

## 5. 与 ABP vNext 逐项对照（为什么「不像」且「不必像」）

| 维度 | ABP vNext | 本仓库 | 判定 |
|------|-----------|--------|------|
| 分层组织 | 垂直：Domain → Application → HttpApi → Infrastructure | 水平：内核 → 技术件 → 模块 → 宿主 | 选型差异，非债务 |
| API 暴露 | 显式 Controller（AppService 自动映射也需约定） | `IDynamicApiController` 动态路由（155 个 Service） | 低代码核心诉求，**锁定保留**（D1） |
| ORM | EF Core + 聚合根/仓储 | SqlSugar + 贫血实体 + `ITenantFilter` 多租户硬门 | 锁定保留（D1） |
| 模块化 | NuGet 包 + `AbpModule` 依赖声明 | 源码工程 + `JnpfModule` 拓扑排序（`AddJnpfModules`） | 机制等价，形态不同 |
| 依赖注入 | Autofac + 约定注册 | vendored Furion 反射扫描 + JNPF001 分析器禁服务定位器 | 业务层已合规 |
| 领域层 | DDD 聚合根/领域服务/规约 | 无（Service 内直连 SqlSugar） | **刻意不补**（D5） |

**认知校准（决策者批复 2026-08-19）**：低代码平台核心诉求是「元数据驱动 + 动态 API + 多租户」，
ABP 的显式 Controller + EF Core + DDD 范式与之存在天然摩擦。**真正要治理的不是「不像 ABP」，
而是任何架构风格下都会腐烂的问题：Service 塌缩、上帝类、DI 生命周期混乱、模块边界失守。**

---

## 6. 模块间依赖实测（2026-08-19 全量扫描）

### 6.1 量化结论

- 跨模块 ProjectReference **67 条**，其中绕过 `.Interfaces` **46 条**，分级如下：

| 严重度 | 类别 | 数量 | 说明 |
|--------|------|-----:|------|
| — | → `common` 内核（设计内） | 27 | 所有模块依赖共享内核属预期 |
| 🔴 高 | **共享内核反向依赖模块**：`JNPF.Common.Core` → 5 个模块的 `.Entitys`（Systems/Message/VisualDev/TaskScheduler/Engine.Entity） | 5 | 依赖方向倒置，内核被业务实体污染 |
| 🔴 高 | **跨模块引用实现/Service 工程**：`Systems→OAuth`、`Systems→VisualDev.Engine`、`CodeGen→VisualDev.Engine`、`WorkFlow→VisualDev.Engine`、`VisualDev→VisualDev.Engine`、`Common.CodeGen→VisualDev` | 6 | 战役 1 拆分时最可能踩到的硬耦合 |
| 🟡 中 | 跨模块引用 `.Entitys` 工程（数据契约而非服务契约） | 8 | `.Entitys` 非 Service 实现，但契约通道应走 `.Interfaces` |

### 6.2 域级依赖矩阵

> I=仅经 `.Interfaces`（合规）· X=存在实现/Entitys 引用 · -=无关
> （`common` 列全 X 为设计内；完整工程级清单见 `.claude/evidence/arch-scan/module-dependency-edges.json`）

| 引用方 ↓ \ 被引用方 → | app | codegen | common | engine | extend | inteAssist | message | oauth | system | tasksched | visualdata | visualdev | workflow | zxdev |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **app** | - | | X | | | | | | X | | | | I | |
| **codegen** | | - | | X | | | | | | | | I | | |
| **common** | | | - | X | | | X | | X | X | | X | | |
| **engine** | | | X | - | | | | | I | | | I | X | |
| **extend** | | | X | | - | | | | I | | | | I | |
| **inteAssistant** | | | X | | | - | I | | I | | | | | |
| **message** | | | X | | | | - | | I | | | | X | |
| **oauth** | | | X | | | | I | - | I | | | | | |
| **report** | | | X | | | | | | | | | | | |
| **subdev** | | | X | | | | | | | | | | | |
| **system** | X | | X | X | | | I | X | - | I | | | I | |
| **taskscheduler** | | | X | | | | | | I | - | | | | |
| **visualdata** | | | X | | | | | | | | - | | | |
| **visualdev** | | | X | X | I | | I | | X | | | - | X | |
| **workflow** | | | X | X | | | I | | X | | | X | - | |
| **zxdev** | | | X | | | | | | | | | | | - |

---

## 7. JNPF.Common.Core 量化（共享内核 or 上帝程序集？）

**结论：不是上帝程序集（仅 50 个公共类型），但存在「方向倒置 + 伪共享」两类治理点。**

- 公共类型 **50 个**；被 **18 个 csproj** 引用（modularity 内 12 个）。
- **真共享内核**（≥6 域使用，应保留）：`IUserManager`(14 域)、`UserManager`(13)、`Mapper`(9)、
  `TenantManager`/`ITenantManager`(6)、`IFileManager`/`FileManager`(6)、`IDataBaseManager`(6)。
- **可下沉候选**（仅 1-2 域使用）**20 个**，其中 **8 个仅被 `application` 宿主使用**
  （如 `PollyRetryHandlerExecutor`、`RequestActionFilter`、`RabbitMQEventSourceStorer` —— 属宿主/技术件职责）；
  另有疑似零引用类型 **18 个**。明细：`.claude/evidence/arch-scan/common-core-type-usage.json`。
- 治理动作归入战役 2（不在战役 1 范围内）：内核反向依赖解除（5 条 🔴）+ 伪共享类型下沉。

---

## 8. 三不做清单（决策 D5，2026-08-19 锁定）

| ❌ 不做 | 理由 |
|---------|------|
| 目录重命名（`application/`→`host/` 等） | 72 csproj + CI 脚本 + 文档连锁成本，ROI 为负；语义用本文档固化 |
| 引入 ABP vNext 或任何新框架 | 与低代码核心诉求（动态 API/多租户/元数据驱动）天然摩擦；D1/D4 已锁定 |
| 战役 1 中补 Domain 层 / DDD 化 | 贫血实体 + SqlSugar 为既定选型，强行 DDD 制造更大混乱 |

## 9. 与重构战役的映射

| 本文档发现的问题 | 治理承载 |
|-----------------|---------|
| Service 三层塌缩 / 上帝类 | **战役 1**（CR-20260819-01 起） |
| 6 条跨模块实现引用硬耦合 | 战役 1 拆分对应模块时的前置排查项 |
| 共享内核反向依赖 + 伪共享类型 + `InternalApp` 静态状态 | **战役 2**（框架去隐式化） |
| 完整模块依赖矩阵无测试守护 | 战役 2 候选：扩展 `LayeringTests`（「模块只能引用对方 `.Interfaces`」），待基线评估后实施 |
| 目录名误导 | **本文档**（不改名） |
