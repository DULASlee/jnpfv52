# Capability Evolution Matrix — v4 → v5 → v6 能力基线

> 基于 Git 历史 + SKILL.md + references + 规格/校准文档。不补写仓库中不存在的能力。
> 状态标签：`IMPLEMENTED` / `PARTIAL` / `PROPOSED` / `NOT_FOUND`

| # | Capability | v4.0 状态 | v5.0 变化 | v6.0 Target | 仓库实际状态 |
|---|------------|-----------|-----------|-------------|--------------|
| 1 | **Class structural modeling** | `IMPLEMENTED` — 16 维排查含职责/依赖/边界分析 | 无变化 | 继承 | v4 完整，v5/v6 未改 |
| 2 | **10-dimension expert audit (D1-D10)** | `IMPLEMENTED` — Lifetime/Memory/Async/Concurrency/Exception/Performance/Type/Extensibility/Observability/Architecture | 无变化（P2/P3/P4 挂既有维） | 继承 + D11 跨类 | v4 完整，v5 增 P2/P3/P4 后处理，v6 加 D11 |
| 3 | **Finding identification** | `IMPLEMENTED` — 16 维 Finding，Finding≠Fix，三安全阀 | 无变化 | 继承 | v4 完整，v5/v6 未改 |
| 4 | **Risk classification** | `IMPLEMENTED` — C/H/M/L + JNPF N1-N4 Critical | P4 增数据源追溯调整严重度 | 继承 | v4 完整，v5 增 P4 调整，v6 未改 |
| 5 | **GO / STOP / NEED EVIDENCE** | `IMPLEMENTED` (M1 校准) — 三态门控显式化 | 无变化 | 继承 + 跨类 Finding 接入 | v4 完整，v5/v6 未改决策门本身 |
| 6 | **Semantic Budget** | `IMPLEMENTED` (M2 校准) — 最小语义变更预算 | 无变化 | 继承 | v4 完整，v5/v6 未改 |
| 7 | **Evidence chain** | `IMPLEMENTED` — P0 五维取证 + Evidence→Finding→Risk→Decision | 无变化 | 继承 + 跨类证据 | v4 完整，v5/v6 未改单类证据链 |
| 8 | **Ownership analysis** | `IMPLEMENTED` — 资源生命周期四问 Create→consume→end→who owns→dispose | 无变化 | 继承 + 跨类 ownership | v4 完整（单类），v6 目标扩展到跨类 |
| 9 | **Call graph analysis** | `NOT_FOUND` — v4 仅 P0.1 提 "callers"，无系统 call graph | 无变化 | `PROPOSED` — Level 2 Roslyn call-graph | v4/v5 无，v6 仅 Level 0 人工描述 |
| 10 | **DI relationship analysis** | `PARTIAL` — P0.1 提 "DI lifetime"，无系统 DI graph | 无变化 | `PROPOSED` — Level 2 DI-registration | v4 有单类 DI 检查，v6 目标扩展到跨类 |
| 11 | **Cross-method lifecycle** | `PARTIAL` — 单类内方法间 ownership 追踪 | 无变化 | 继承 | v4 单类内部分覆盖，v6 目标扩展到跨类 |
| 12 | **Cross-class Finding** | `NOT_FOUND` — v4 整条协议单类视野 | 无变化 | `PROPOSED` — D11 跨类 Finding | v4/v5 无，v6 D11 规则已写但仅 Level 0 |
| 13 | **Cross-layer context** | `NOT_FOUND` | 无变化 | `PROPOSED` — 跨层边界分析 | v4/v5 无，v6 仅概念 |
| 14 | **Context expansion** | `NOT_FOUND` — v4 无显式上下文扩展机制 | 无变化 | `PROPOSED` — Evidence Expansion ≠ Scope Expansion | v4/v5 无，v6 需设计最小必要上下文原则 |
| 15 | **Runtime evidence boundary** | `IMPLEMENTED` — P0.2 运行时取证 + Deferred/Env-Blocked 诚实记录 | 无变化 | 继承 | v4 完整，v5/v6 未改 |
| 16 | **Verification / Replay** | `IMPLEMENTED` — characterization/Benchmark/Arch test + Decision Replay | 无变化 | 继承 | v4 完整（六问校准），v5/v6 未改 |
| 17 | **Convergence** | `IMPLEMENTED` (M3 校准) — Class-level 收敛停止规则 | 无变化 | 继承 | v4 完整，v5/v6 未改 |
| 18 | **Golden / Evaluation Corpus** | `IMPLEMENTED` — 4 Golden Examples (Exception/Resource×2/Transaction) | 无新增 Golden | 继承 + 跨类 Golden | v4 完整，v5/v6 未增 |

## 关键观察

### v4.0 已具备的核心能力（1-8, 15-18）
- 完整的单类诊断与决策系统
- P0 五维取证先行
- 16 维 Finding 排查
- GO/STOP/NEED 三门 + Semantic Budget + Convergence
- 4 Golden Examples 跨技术性质
- 六问校准 + Decision Replay

### v4.0 结构性缺失（9-14）
- **无系统 Call Graph**（仅 P0.1 提 "callers"）
- **无系统 DI Graph**（仅单类 DI lifetime 检查）
- **无跨类 Finding 机制**（整条协议单类视野）
- **无显式 Context Expansion**（无最小必要上下文原则）

### v5.0 实际变化
- **0 个真正新能力域**
- P2/P3/P4 = 后处理查表 + 字段规范（C 规则细化 + D 文档）
- 挂在既有维度（D3-D10）之上，不改决策骨架
- 验证声称（F1 0.90→0.95）仓库无评测工件

### v6.0-alpha 当前状态
- **D11 跨类分析** = 规则已写（3 检查项 + 输出字段），但仅 Level 0 人工
- **Level 2 自动取证** = `NOT_FOUND`（reference 自述"第二期工具开发"）
- **跨类 Finding 接入决策门** = `PROPOSED`（SKILL 只加"若上下文可用则分析"）

### v6.0 真正要解决的核心问题
**把 Skill 从"单类人工判断"升级为"解决方案级自动取证决策"**，其完整形态 = Level 2 自动化 call-graph/DI-graph（Roslyn）取代 Level 0 人工喂链。

这才是 v6.0 的真实目标（架构级），不是"再加一维"。

## 状态汇总

| 状态 | 计数 | 能力项 |
|------|------|--------|
| `IMPLEMENTED` | 11 | 1,2,3,4,5,6,7,8,15,16,17,18 |
| `PARTIAL` | 2 | 10,11 |
| `NOT_FOUND` | 4 | 9,12,13,14 |
| `PROPOSED` (v6 target) | 4 | 9,12,13,14 |

> v4/v5 已具备 11 项完整能力 + 2 项部分能力；v6 要解决的是 4 项结构性缺失（Call Graph / DI Graph / Cross-class Finding / Context Expansion）。
