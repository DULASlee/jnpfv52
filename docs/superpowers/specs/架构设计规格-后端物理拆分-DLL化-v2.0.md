# 架构设计规格 — 后端物理拆分与包化（DLL/NuGet）v2.0

> **文档性质：** v1.0 的评审升级版。v1.0（`架构设计规格-后端物理拆分-DLL化.md`）覆盖面止于 framework/JNPF 的 9 个零依赖区；v2.0 依据 **2026-08-24 全后端 73 工程引用边实测扫描**，将分析面扩至全后端，给出完整的发包能力判定与「框架源码退出日常编译图」的施工主线。
> **证据基线：** 本文档所有依赖论断均来自当日 `csproj` 全量扫描（边清单见 §3.3），非名义分层推断。差异对照见 §7。

---

## 0. 全局锚定卡（一页看懂）

| 项 | 内容 |
|---|---|
| **要解决的问题** | 框架/基础设施/公共类源码与业务源码同图编译：每次 `dotnet build` 重建全部 73 工程（含 37 区巨无霸 JNPF），AI 改框架代码面对整包无物理边界 |
| **终态** | 单仓 + 本地 NuGet 源：**14+3 个工程打成版本化包（3.5.0）**，业务侧仅消费包；框架源码退出 `zx_lowcode_netcore.sln` 日常编译图 |
| **切包线** | `JNPF.Common`（业务真地基）划入包域；`JNPF.Common.Core` 及以上留源码域（判据见 §2.2） |
| **价值兑现点** | P2 全量切换完成后：日常构建不再编译任何框架源码，构建时间下降（目标值 S0 实测基线后登记） |
| **主线** | S0 核验基线 → P1 B1/B2 拆分（管线试炼）→ P2 内核上包+全量切换 → P3 按收益扩展 → P4 解环路线图（纯分析） |
| **零改动承诺** | 框架源码零行为变更（纯移动+csproj 引用切换）；业务 `.cs` 文件零改动；仅 **6 个业务侧 csproj** 需要切包编辑（§5 P2） |

---

## 1. 总体结构分析（全局观）

### 1.1 后端工程全景（73 工程 × 5 物理层；zx sln 现有 83 个 Project 条目）

| 物理层 | 目录 | 工程数 | 角色 |
|---|---|---|---|
| 框架层 | `framework/` | **8**（JNPF + 7 Extras） | 平台内核与官方扩展 |
| 基础设施层 | `infrastructure/` | **5**（EventBus.Outbox/RabbitMQ、Thirdparty、CollectiveOAuth、WebSockets） | 横切基础设施 |
| 业务公共层 | `modularity/common/` | **3**（JNPF.Common 133cs / Common.Core 50cs / Common.CodeGen 2cs） | 全部业务工程共享的底座 |
| 业务域层 | `modularity/{15 域}/` | **37**（Entitys/Interfaces/服务 三段式 × 15 域） | 高频业务变更区 |
| 宿主/验证 | `application/` + `tools/` + `tests/` | **20**（2 宿主 + 5 工具 + 13 测试） | 组装与验证 |

### 1.2 真实依赖骨架（实测，颠覆 v1.0 认知的三处偏离）

**名义分层**是 `framework → infrastructure → modularity → application` 单向塔。**实测边数据**（§3.3）揭示三处结构性偏离，它们决定了 v2.0 的一切判定：

```
偏离一【业务层不直连内核】——43 个业务工程中，直引 JNPF 内核的只有
       infrastructure/JNPF.Extras.EventBus.Outbox 一个。业务栈的真正入口是
       modularity/common/JNPF.Common → 3 个框架 Extras（SqlSugar/Mapster/JwtBearer），
       内核经 Extras 传递触达。=> 切包的钥匙在 JNPF.Common，不在内核本身。

偏离二【基础设施反向依赖业务层】——WebSockets / Thirdparty / CollectiveOAuth
       三个 infrastructure 工程 ProjectReference → JNPF.Common（业务公共层）。
       => 这三个工程在 JNPF.Common 包化之前不可独立发包。

偏离三【公共层内部反向耦合领域】——JNPF.Common.Core 依赖 4 个域的 Entitys +
       Engine.Entity；JNPF.Common.CodeGen 依赖 JNPF.VisualDev 域服务。
       => Common.Core/CodeGen 不是干净的底座，必须留在源码域（业务侧）。
```

### 1.3 分层依赖全景图（DAG，自底向上）

```
T0  框架内核        JNPF(37区,内部三组成环)
                     ▲            ▲
T1  框架Extras      CodeAnalysis ─┘(内核唯一上行)   SqlSugar→内核   Xunit→内核
                    JwtBearer / Dapper / Serilog / Mapster = 零工程引用孤立叶
                     ▲    ▲
T2  业务真地基      JNPF.Common ──→ Mapster + SqlSugar + JwtBearer
                     ▲  ▲  ▲
T2' 基础设施        WebSockets / Thirdparty / CollectiveOAuth → JNPF.Common ⚠反向
                    EventBus.Outbox → JNPF内核 + SqlSugar    EventBus.RabbitMQ = 孤立叶
                     ▲
T3  领域实体        16 域 Entitys → JNPF.Common（ Systems.Entitys 额外直引 Thirdparty/CollectiveOAuth）
                     ▲
T4  领域接口        Interfaces → 本域 Entitys + 跨域 Entitys（Systems/WorkFlow/VisualDev 交叉）
                     ▲
T5  公共核心        JNPF.Common.Core → 4域Entitys + Engine.Entity + WebSockets + RabbitMQ + SqlSugar + JwtBearer ⚠跨层
                     ▲
T6  领域服务        16 域服务 → Interfaces + Common.Core（跨域互引经 Interfaces 解环）
T6' 代码生成        JNPF.Common.CodeGen → JNPF.VisualDev ⚠反向    SubDev/ZxDev → Common.CodeGen
                     ▲
T7  宿主            JNPF.API.Entry → 14 域服务 + EventBus.Outbox；OA.Entry → API.Entry（禁用）
T8  验证            8 测试工程（直引内核/Common.Core/域工程）+ Benchmarks→API.Entry + Analyzers.Tests
```

### 1.4 切包线（本战役的核心架构决策）

```
═════════════════ ✂ 切包线 ═════════════════
包域（14 工程 → NuGet 3.5.0，退出日常编译图）
    JNPF 内核 + 7 框架 Extras
    + JNPF.Common（业务真地基）
    + 5 基础设施 Extras
    （P1 拆出的 3 个 B1/B2 DLL 也属包域，合计 17 包）
──────────────────────────────────────────
源码域（59 工程，留在 zx sln 日常编译图）
    JNPF.Common.Core / JNPF.Common.CodeGen
    + 37 业务域工程 + 2 宿主 + 测试/工具
════════════════════════════════════════════
```

**切包线划在 T2 与 T3 之间**：线以下的依赖闭包完整（不触及任何领域工程）、变更频率低（框架/地基稳定）、消费面广（被全部上游共享）。线以上三者皆反。

### 1.5 编译图前后对比（P2 完成时）

| | 切换前 | 切换后 |
|---|---|---|
| `dotnet build zx.sln` 构建工程数 | 73（sln 83 条目，含巨无霸） | 59，**零框架工程**（sln 摘除 17 个包域工程） |
| 框架源码变更的生效方式 | 改源码即生效 | `framework/JNPF.sln` 开发 → `JNPF_PACK=true` 重打包 → restore（低频操作） |
| 框架开发态载体 | 同一 sln | `framework/JNPF.sln` 独立解决方案（已存在，S0 核验内容） |

---

## 2. 发包能力矩阵（一锤定音）

### 2.1 判定规则（四判据，全部满足才判 ✅）

| # | 判据 | 说明 |
|---|---|---|
| C1 | **依赖闭包干净** | 自身及全部传递依赖都在包域内，不触及领域工程/宿主 |
| C2 | **变更频率低** | 稳定基础设施，非高频业务迭代区 |
| C3 | **消费面广** | 被 ≥2 个上游工程共享，包化收益为正 |
| C4 | **身份唯一** | 打包后不与现存包/源工程形成双身份冲突（切换原子性保障） |

### 2.2 总矩阵

| 工程 | 层 | 波次 | 判定 | C1 依赖闭包 | 理由一句话 |
|---|---|---|---|---|---|
| **JNPF 内核** | T0 | W1 | ✅ 发包 | 干净（仅 CodeAnalysis） | `JNPF.3.4.7.nupkg` 先例实锤；内部三组成环**不阻碍单程序集打包**，只阻碍继续细分 |
| JNPF.Extras.Authentication.JwtBearer | T1 | W1 | ✅ 发包 | 零工程引用孤立叶 | 已有 3.4.7 成品包 |
| JNPF.Extras.ObjectMapper.Mapster | T1 | W1 | ✅ 发包 | 孤立叶 | 已有成品包 |
| JNPF.Extras.DatabaseAccessor.Dapper | T1 | W1 | ✅ 发包 | 孤立叶 | 零消费引用，顺手入列 |
| JNPF.Extras.Logging.Serilog | T1 | W1 | ✅ 发包 | 孤立叶 | 同上 |
| JNPF.Extras.DependencyModel.CodeAnalysis | T1 | W1 | ✅ 发包 | 孤立叶（被内核依赖） | 已有成品包；内核包的依赖项 |
| JNPF.Extras.DatabaseAccessor.SqlSugar | T1 | W2 | ✅ 发包 | → 内核（W1 已包） | 已有成品包 |
| JNPF.Xunit | T1 | W2 | ✅ 发包 | → 内核 | 测试支撑包，随 W2 |
| JNPF.Extras.EventBus.RabbitMQ | T2' | W1 | ✅ 发包 | 零工程引用孤立叶 | 随手包，无依赖风险 |
| JNPF.Extras.EventBus.Outbox | T2' | W2 | ✅ 发包 | → 内核 + SqlSugar（均 W2 前） | 横切事件外发箱 |
| **JNPF.Common** | T2 | W2 | ✅ 发包 | → 3 框架 Extras（W1 已包） | **业务真地基**，133cs；包化后基础设施反向依赖随之闭合 |
| JNPF.Extras.WebSockets | T2' | W3 | ✅ 发包 | → JNPF.Common（W2 已包）+ JwtBearer | 反向依赖随 W2 解锁 |
| JNPF.Extras.Thirdparty | T2' | W3 | ✅ 发包 | → JNPF.Common | 同上 |
| JNPF.Extras.CollectiveOAuth | T2' | W3 | ✅ 发包 | → JNPF.Common | 同上 |
| *P1 产出的 3 个 B1/B2 DLL* | T0.5 | W2 | ✅ 发包 | 零依赖（v1.0 已证） | Cryptography/Utils/Abstractions，内核包的前置依赖 |
| **JNPF.Common.Core** | T5 | — | ❌ **不发包** | ✗ 依赖 4 域 Entitys + Engine.Entity | 公共层里的"领域耦合体"，包化会把领域实体拖进公共包；留源码域 |
| **JNPF.Common.CodeGen** | T6' | — | ❌ **不发包** | ✗ 依赖 JNPF.VisualDev 域服务 | 同上，且仅 2 个 .cs |
| **37 个业务域工程** | T3-T6 | — | ❌ **不发包** | ✗ 跨域互引 + C2 不满足 | 高频变更，包化只增发版摩擦（v1.0 决策沿用） |
| **API.Entry / OA.Entry** | T7 | — | ❌ **不发包** | 宿主 | 组装点，永不为包 |
| **测试/工具 10 工程** | T8 | — | ❌ **不发包** | 消费者 | 验证设施（JNPF.Xunit 除外，它本身是 W2 包） |

**汇总：17 个包（14 现存工程 + 3 个 P1 新 DLL）｜59 个工程永留源码域。**

### 2.3 发波次依赖链

```
W1（无前置）: JNPF内核, JwtBearer, Mapster, Dapper, Serilog, CodeAnalysis, RabbitMQ
W2（依赖 W1）: SqlSugar, Xunit, EventBus.Outbox, JNPF.Common, B1/B2 三 DLL
W3（依赖 W2）: WebSockets, Thirdparty, CollectiveOAuth
```

### 2.4 版本策略

- 包域统一 **3.5.0**（自 3.4.7 升），后续包域内变更统一升版；
- `backend/Directory.Build.props` 的 3.6.0 是**业务侧版本域**，维持独立——两版本域已事实上并存（3.4.7 vs 3.6.0），强行统一徒增耦合；
- 本地源：`nuget.config` 追加 `framework/nupkgs`（v1.0 D5 决策沿用，写法 S0 核验）。

---

## 3. 依赖结构详图

### 3.1 包域内部依赖（打包后的包间依赖关系）

```
JNPF@3.5.0 ──→ CodeAnalysis@3.5.0
SqlSugar@3.5.0 ──→ JNPF@3.5.0 ──→ B1.Cryptography / B1.Utils / B2.Abstractions（P1 后）
Xunit@3.5.0 ──→ JNPF@3.5.0
EventBus.Outbox@3.5.0 ──→ JNPF + SqlSugar
JNPF.Common@3.5.0 ──→ SqlSugar + Mapster + JwtBearer
WebSockets / Thirdparty / CollectiveOAuth@3.5.0 ──→ JNPF.Common
RabbitMQ / Dapper / Serilog ──→ （零包域依赖）
```

**无环验证口径（终审必查）：** 上述包间图为 DAG；JNPF 内核的**程序集内部**三组成环（FriendlyException↔UnifyResult、DataValidation↔DynamicApiController、App↔ConfigurableOptions）不影响包间图，留待 P4。

### 3.2 源码域 → 包域的消费边（P2 切换面，完整清单）

| 消费者（源码域） | 现引工程 | 切换后 |
|---|---|---|
| JNPF.Common.Core | SqlSugar, JwtBearer, WebSockets, RabbitMQ | → 4 个 PackageReference |
| JNPF.Systems.Entitys | Thirdparty, CollectiveOAuth | → 2 个 |
| JNPF.Systems | CollectiveOAuth | → 1 个 |
| JNPF.Message | WebSockets | → 1 个 |
| JNPF.API.Entry | EventBus.Outbox | → 1 个 |
| JNPF.Common（自身入包域） | SqlSugar, Mapster, JwtBearer | → 3 个（成为包内依赖声明） |
| 测试工程 ×7（Architecture/Gate/PhaseB/Phase6/Stage5/ADR012/ModuleVerification） | JNPF / SqlSugar / Outbox | → 按所引工程切换 |

**业务侧 csproj 编辑量：6 个业务工程 + 7 个测试工程。** 业务工程间彼此仍走 ProjectReference，不受影响——这是实测数据带来的重大利好（v1.0 设想的"43 工程全切"实为 6 个）。

### 3.3 实测边数据（证据存根，2026-08-24 扫描）

全部 73 工程 ProjectReference 边已扫描归档；关键边摘录：

- `JNPF → CodeAnalysis`（内核唯一上行，8 个框架工程中其余 6 Extras 零引用）
- `SqlSugar → JNPF`、`Xunit → JNPF`
- `JNPF.Common → {Mapster, SqlSugar, JwtBearer}`
- `WebSockets → {JNPF.Common, JwtBearer}`；`Thirdparty → JNPF.Common`；`CollectiveOAuth → JNPF.Common`
- `EventBus.Outbox → {JNPF, SqlSugar}`
- `Common.Core → {VisualDev/TaskScheduler/Systems/Message Entitys, Engine.Entity, WebSockets, RabbitMQ, SqlSugar, JwtBearer}`
- `Common.CodeGen → JNPF.VisualDev`
- `API.Entry → 14 域服务 + EventBus.Outbox`
- 业务侧 `PackageReference Include="JNPF`：**零命中**（只产不销实锤）

---

## 4. 每项详细分析

### 4.1 JNPF 内核（T0）——包域基石

- **现状：** 单 csproj 承载 37 功能区；程序集内部三组成环（2026-08-23 实测）。
- **判定：** ✅ W1 整体发包。成环只阻碍"继续拆分"，不阻碍"整体成包"——`JNPF.3.4.7.nupkg` 已存在即为先例。
- **P1 前置：** 先按 v1.0 拆出 B1（Cryptography/Utils）+ B2（Abstractions）3 个零依赖 DLL——双重目的：①小包试炼打包/消费管线；②AI 上下文物理瘦身（改加密工具不再面对 37 区整包）。
- **P4 远期：** 三组成环各出一份解法分析（成本/失效条件），供未来拆细 JNPF.Core 立项。**本战役不施工解环。**
- **风险：** 打包后内核 API 面即公共契约——v1.0 的 Public API 清单冻结（五件套之 4）从"纪律"升格为"包版本语义"。

### 4.2 框架 Extras（T1，7 工程）

- **孤立叶四件套**（JwtBearer/Mapster/Dapper/Serilog）：零工程引用，W1 直接打包，零风险。
- **CodeAnalysis：** 被内核依赖（内核唯一上行边），W1 打包后成为 JNPF 包的 nuspec 依赖。
- **SqlSugar：** 下行依赖内核，W2（内核包就绪后）打包；已有 3.4.7 成品。
- **Xunit：** 同 W2；仅测试工程消费。

### 4.3 JNPF.Common（T2）——被 v1.0 漏掉的真正地基

- **现状：** 133 个 .cs，被 16 域 Entitys + 3 个 infrastructure 工程引用，是**事实上**的框架级组件，却物理躺在 `modularity/common/`。
- **判定：** ✅ W2 发包。这是 v2.0 相对 v1.0 最重要的范围修正：不包化 JNPF.Common，切包线就划不下去（领域 Entitys 的引用无处安放）。
- **施工要点：** 包化时其 3 个 Extras 引用转为包依赖；**目录不迁移**（避免与"modularity 归属"议题耦合，纯 csproj 操作）。
- **归属议题（P3 裁决）：** 它在 modularity 目录下却承担框架职责——是否物理迁至 framework/ 目录，属"可选项，按团队口味"，不阻塞主线。

### 4.4 基础设施 Extras（T2'，5 工程）

- **RabbitMQ：** 零依赖孤立叶，W1。
- **EventBus.Outbox：** 依赖内核+SqlSugar，W2；API.Entry 是其唯一业务消费者。
- **WebSockets / Thirdparty / CollectiveOAuth：** ⚠ 反向依赖 JNPF.Common（偏离二），W3（JNPF.Common 包就绪后）打包。包化后反向依赖转为包依赖，**结构性倒挂随之在包图层面合法化**（JNPF.Common 是包域成员，不再是"框架依赖业务"）。
- **SQL/行为零变更**：仅引用形态变化。

### 4.5 不包域详析（为何留下）

| 工程 | 不包的核心理由 | 远期出路 |
|---|---|---|
| JNPF.Common.Core | 依赖 4 域 Entitys + Engine.Entity——包化 = 把领域实体拖进"公共包"，违反 C1 | 若领域实体契约稳定后可议；或重构掉对 Entitys 的依赖再议（远期专项） |
| JNPF.Common.CodeGen | 依赖 VisualDev 域服务；仅 2 个 .cs，包化收益为负 | 维持现状 |
| 37 业务域工程 | 高频变更（C2 ✗）；已是独立 csproj，物理边界已存在（v1.0 决策沿用） | 无需求则永不为包 |
| 宿主/测试/工具 | 消费者身份 | — |

---

## 5. 分阶段主线（施工排期）

### S0 核验与基线（开工前必须全绿）

继承 v1.0 S0 全部任务（A-1/A-2/A-3 疑点溯源、预扫描、基线采集、nuget.config 写法核验），**v2.0 新增：**

- S0-5：`framework/JNPF.sln` 内容核验（框架开发态解决方案是否可用，缺工程则补齐）；
- S0-6：**混合引用尖刺测试（spike）**——两个临时工程模拟「同一身份经 ProjectReference 与 PackageReference 双路到达」，实测 NuGet 行为（NU1107/身份冲突与否），结论写入本规格 §6 风险表，决定 P2 切换的原子化粒度；
- S0-7：构建时间基线 ×3 取中位落盘（`dotnet build zx.sln` 与 `dotnet build framework/JNPF.sln` 各测）。

### P1 B1/B2 拆分（v1.0 原案，~10h）

Task 1.1 Cryptography / Task 1.2 Utils / Task 2.1 Abstractions，五件套证据门禁，契约台账 `C-SPLIT-{区}@v1`。**产出 3 DLL 即 W2 波次包成员。**

### P2 内核上包 + 全量切换（价值兑现点）

1. W1 打包 7 包（JNPF 内核 + JwtBearer/Mapster/Dapper/Serilog/CodeAnalysis/RabbitMQ）→ W2 打包 7 项（SqlSugar/Xunit/EventBus.Outbox/JNPF.Common + P1 三 DLL）→ W3 打包 3 包（WebSockets/Thirdparty/CollectiveOAuth）；
2. 按 §3.2 清单原子切换消费边（业务 6 工程 + 测试 7 工程，单提交完成，S0-6 结论定粒度）；
3. 框架 14+3 工程从 `zx_lowcode_netcore.sln` 摘除，日常编译图瘦身至 51 工程；
4. 验收：构建时间对比基线落盘 + 路由快照零 diff + `jnpf-api.mjs CurrentUser` 200 + `E2E_PIPELINE_ID=311 pnpm test:api` 绿 + 全量 `dotnet test` 绿。

### P3 按收益扩展（可选段）

- JNPF.Common 目录归属裁决（迁 framework/ 与否）；
- v1.0 B3 四包（Caching/RemoteRequest/WebAssets/BackgroundJobs）按实测收益逐个裁决——P2 后内核源码已退出编译图，B3 的剩余价值仅剩 AI 上下文粒度，优先级自然下调。

### P4 解环路线图（纯分析，不施工）

三组成环各出：解法方案 ≥2 个、代码级成本估算、失效条件。产物为三份分析文档，供远期立项。

---

## 6. 横切关注点

### 6.1 证据门禁（五件套沿用 + v2.0 扩展）

P1 沿用 v1.0 五件套；**P2 追加第六件：切换前后行为等价三证**——路由快照零 diff + CurrentUser 冒烟 + test:api 绿（包引用与源码引用产出必须行为一致）。

### 6.2 回滚轴

| 阶段 | 回滚方式 |
|---|---|
| P1 | 批次级 `git revert`（v1.0 沿用） |
| P2 切换 | 单提交 `git revert` 恢复全部 ProjectReference（切换面小，原子可逆） |
| P2 运行期异常 | nuget.config 移除本地源 + revert = 完整退回源码消费态 |

### 6.3 多轨隔离（与 RunService 战役）

沿用 v1.0 §3.3 全部规则；**v2.0 新增碰撞面申报：** P2 将编辑 Systems.Entitys/Systems/Message 等业务 csproj——与 RunService 战役文件面（visualdev/engine、Program.cs、App.json）存在邻接，切换单提交须与战役路由快照窗口错开，csproj 编辑不触碰战役文件。

### 6.4 风险表

| 风险 | 概率 | 影响 | 缓解 |
|---|---|---|---|
| 混合引用双身份冲突（包+源同身份） | 中 | 高 | S0-6 尖刺测试先行定论；P2 原子切换设计兜底 |
| GlobalUsings/隐式依赖致 P1 编译失败 | 中 | 低 | v1.0 预扫描 + 仅增 using 豁免（沿用） |
| 打包遗漏非 .cs 资产（嵌入资源/本地化） | 中 | 中 | S0 Task 0.2 资产清点扩展至包域 14 工程 |
| 框架开发态重打包遗忘导致"改了不生效" | 中 | 低 | ADR 已知代价；P2 验收含"框架变更→重打包→生效"演练用例 |
| 与 RunService 战役窗口冲突 | 中 | 中 | §6.3 错窗规则 |
| 版本漂移（包 3.5.0 与业务 3.6.0 混淆） | 低 | 低 | §2.4 双版本域文档化 + S0 登记基线 |

### 6.5 SLO（业务口径）

- P2 完成：`dotnet build zx.sln` 时间较 S0 基线下降（目标值以 S0-7 实测登记，不预编数字）；"AI 修改框架代码"可见上下文 = 单工程（P1 兑现）+ 包消费视角（P2 兑现）。
- P4 产出：三份解环分析，无施工承诺。

---

## 7. 与 v1.0 的差异对照（评审升级记录）

| 维度 | v1.0 | v2.0 | 升级依据 |
|---|---|---|---|
| 分析范围 | framework/JNPF 的 9 零依赖区 | 全后端 73 工程实测 | 2026-08-24 全量边扫描 |
| 切包认知 | 拆 3 DLL + 试点消费 | 17 包 + 切包线 + 全量切换 | 发现"业务真地基是 JNPF.Common"（偏离一） |
| 切换面估算 | 未涉（试点 1 工程） | 实测 6 业务 + 7 测试 csproj | §3.2 边数据 |
| JNPF.Common | 未覆盖 | ✅ W2 包（关键新增） | 偏离一/二推导 |
| 基础设施反向依赖 | 未发现 | W3 波次处理 | 偏离二实锤 |
| 内核解环 | 排除（远期） | P4 路线图（分析产物） | 沿用 + 结构化 |
| 其余（纯移动纪律/五件套/多轨隔离/本地源） | — | 全部沿用 | v1.0 决策仍成立 |
