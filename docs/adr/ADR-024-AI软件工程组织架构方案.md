# ADR-024: AI 软件工程组织架构方案 v1.0（组织级 AI Agent 家族）

**状态:** Proposed
**日期:** 2026-08-30
**阶段:** Phase 8 后 / AI 工程组织建设 Phase A
**作者:** AI 工程师 + 首席架构师（基于 `8、AI软件工程组织架构方案v1.0.md`）

---

## 背景

经过 Phase 8 数据库治理（JNPF 后端 274 张生产表的表级智能重构），JNPF 团队积累了两个关键事实：

1. **生产验证可行性** — Table Refactoring Expert Skill v1.0 在 17 批次 / 0 事故 / R1+R2-COMP 双验证下，证明 AI 可以在数据库治理领域承担资深专家职责（详见 [ADR-019]）。

2. **现有 AI 工程基础设施碎片化** — 本仓库已存在的三类 AI 资产长期独立演化，缺乏统一抽象：
   - 8 个**阶段角色灵魂** (`.claude/souls/{architect,coder,debugger,orchestrator,planner,reporter,reviewer,tester}/soul.md`)
   - 2 个**专家型 Skill** (`.claude/skills/{table-refactor-expert,generic-class-refactor-expert}/`)
   - 3 个**任务型 Sub-agent** (`.claude/agents/{jnpf-debugger,jnpf-tester,session-summary-agent}.md`)

   三类资产的边界、组合方式、演进路径**无统一规范**，导致：
   - 「Agent」一词被混用（用户问「Agent 在哪」时无确定答案）
   - 难以判断一个能力应包装为 Soul / Skill / Sub-agent
   - Phase 8 之外的工程能力（需求分析、领域设计、界面设计、Aspire 迁移）缺乏建设路径
   - 已有的同系列方案（`docs/架构迭代/8、AI编程范式的再次进化/` 1-7 篇）各自提出不同形态，未整合

同时，企业级软件复杂度持续提升，传统工程面临需求理解、业务建模、技术债治理、Aspire 微服务化等多重挑战（详见背景 §2.1）。单纯代码生成 AI 无法解决，需要**组织级 AI 工程体系**。

---

## 决策内容

**采用「AI 软件工程组织」三层形态映射模型，统一所有 AI 工程资产的边界、组合方式与演进路径。**

### 决策 1：三形态映射（核心）

AI 软件工程组织中的「Agent」对应本仓库工程实现时，**必须**严格区分三种形态：

| 组织抽象 | 工程实现 | 启动者 | 调用接口 |
|---|---|---|---|
| **业务型 Agent** | `.claude/souls/{role}/soul.md` | Orchestrator 调度 | 阶段输入 + 阶段交接物 |
| **专家型 Agent** | `.claude/skills/{skill}/SKILL.md` + `references/` | 任意 Agent / 人工调用 | Skill description 触发 |
| **任务型 Agent** | `.claude/agents/{name}.md` (YAML frontmatter) | Main Claude dispatch | Task tool |

**强制约束**：
- 任何能力只能归属一种形态，**禁止跨形态复用身份**
- Soul 不直接写代码；Skill 不调度其他 Skill；Sub-agent 不修改 Soul
- 同名 Agent 必须三形态对齐（如 `Table Refactoring Expert` 在三个形态都有一致身份）

### 决策 2：六层 Agent 标准模型

所有 Agent 必须符合六层结构（即使部分层为空）：

```
Identity → Capability → Knowledge → Workflow → Evidence → Quality Gate
```

各形态层对应（详见 `8、AI软件工程组织架构方案v1.0.md §5.2`）：

| 层 | Soul 实现 | Skill 实现 | Sub-agent 实现 |
|---|---|---|---|
| Identity | `soul.md:1-50` | SKILL.md description | YAML `name:` |
| Capability | soul workflow 段 | Execution Protocol 段 | YAML `tools:` |
| Knowledge | `_shared/*.md` | `references/` | `skills:` 引用 |
| Workflow | soul 阶段流转 | Manual §3 | main Claude 调度 |
| Evidence | `assertion-discipline.md` | Master Spec §11.1 | 上游 Skill 决定 |
| Quality Gate | soul DoD | Master Spec §13.2 | YAML description |

### 决策 3：治理框架共享，专业能力分离

所有 Agent 必须接入共享治理：

- **Evidence 五标签**：`[KNOWN]` / `[COMPUTED]` / `[INFERRED]` / `[GUESS]` / `[DESIGN]`（源自 Master Spec §11.1，已冻结）
- **双防线质量门**：L0 Hooks（已实施）+ L1 Reviewer（已实施）
- **Hard Gate 矩阵**：Master Spec §10.3 10 条 + 6 条禁区规则
- **Closed Gate 5 条件 + 6 记录**：Manual §11

专业能力按维度分离（11 个 Agent，已识别状态见下表）：

| Agent | 形态 | 状态 |
|---|---|---|
| Table Refactoring | Skill v1.0 | `[PRODUCTION-VALIDATED]` — Phase 8 / 93 表 / 0 事故 |
| Class Refactoring | Skill v6.0 | `[PILOT-VALIDATED]` — 9 维度已建立 |
| Test | Soul + Sub-agent | `[PRODUCTION-READY]` |
| Debug | Soul + Sub-agent | `[PRODUCTION-READY]` |
| Code Review | Soul | `[PRODUCTION-READY]` |
| Report | Soul | `[PRODUCTION-READY]` |
| Plan | Soul | `[PRODUCTION-READY]` |
| Architecture | Soul | `[PRODUCTION-READY]` |
| Orchestrator | Soul | `[PRODUCTION-READY]` |
| Requirement Analysis | — | `[PLANNED]` |
| Domain Design | 部分 (R12 Triple-Key 已冻结) | `[PARTIAL]` |
| API / Service / Aspire Refactoring | — | `[PLANNED]` |
| Deployment | — | `[PLANNED]` |

### 决策 4：6 阶段 Agent 生命周期

每个 Agent 必须经历（且每阶段有明确 DoD）：

```
Prototype → Skill Validation → Production Validation
   → Agent Packaging → Enterprise Usage → Continuous Evolution
```

**当前最大缺口**：Table Refactoring Expert 已完成前 3 阶段，**Agent Packaging 未开始**（`.claude/agents/table-refactoring-expert.md` 未创建）。

### 决策 5：演进路线（Phase A → D）

- **Phase A（1-2 周）**：决策「用户级 Agent」运行时；统一三形态规范；定义包装模板
- **Phase B（1-2 月）**：包装 Table/Class Refactoring Expert Agent；冻结 API/Service/Aspire Master Spec
- **Phase C（2-3 月）**：完成 API/Service/Aspire Agent 包装；建立 Agent Registry
- **Phase D（6 月+）**：业务型 Agent 全部 Agent 化；流水线自动驱动

### 决策 6：v1.0-draft → v1.0-final 升级条件

本方案升级为 Final 必须满足 5 项硬门槛（详见 `8、AI软件工程组织架构方案v1.0.md 附录 D`）：

1. [ ] Phase A 运行时决策已批准
2. [ ] 表级重构 Expert Agent 包装完成
3. [ ] Skill `references/` 缺口补齐
4. [ ] 类级重构 Production Validation 完成
5. [ ] API/Service/Aspire 三 Agent 的 Master Spec 至少 1 个已冻结

---

## 理由

### 1. Phase 8 已证明「专家型 Agent」生产可行性

Table Refactoring Expert Skill v1.0 的 Phase 8 验证（详见 [ADR-019]）：

```
R1 人工治理：5/5 PASS
R2-COMP 独立 AI 验证：10/10 EXACT/EQUIV 一致
Safety Gates：4/4 PASS（HG FN=0, P0/P1=0, Scope=0, Closure=0）
生产事故：0
Schema 漂移自动检测：16+ 处
93 张表治理 / 190 个索引 / 17 Batch
```

该验证**不只是 Skill 自身的胜利**，而是「AI 可在特定专业领域承担资深专家职责」的范式证据。这是组织级方案的**唯一成熟锚点**。

### 2. 现有碎片化已产生实际成本

- 用户问「Class Refactoring Expert Agent 在哪」→ 无 Agent，只有 Skill；预期与现实不一致
- Phase 8 完成后想做「Class Refactoring Expert 同样生产验证」→ 无统一路径
- Aspire 微服务化讨论（`docs/architecture/MASTER-JNPF后端重构到Aspire微服务架构方案.md`）需要多个 Refactoring Expert 协同 → 无统一调度规范

### 3. 同系列方案已积累组织级思考

`docs/架构迭代/8、AI编程范式的再次进化/` 系列 1-7 篇（已存在 ~230KB）：

- 1-3 篇：Fugu 化方案（被自身分析否决过度设计）
- 4-7 篇：Kimi 涅槃重构 V3.0（外部状态机 + 内部专家 + 双防线）

新方案**不替换**该系列，而是其**上层抽象**：

- 接受 V3.0 的「文件级交接 + 双防线质量门」原则
- 拒绝 V3.0 的「Python 外部状态机」物理隔离（不符合 Claude Code 约束）
- 拒绝 Fugu 化方案的「RL 训练 + 动态角色分配」（单体环境不可行）

### 4. 三形态映射解决「Agent 是什么」的认知冲突

历史上本仓库对「Agent」有三种不同用法：

| 用法 | 实际含义 | 风险 |
|---|---|---|
| 「Phase 1-7 的 Agent」 | soul 阶段角色 | 与「独立运行的 Agent」混淆 |
| 「Table Refactoring Expert Agent」 | Skill 技能 | 用户期望 main Claude 可 dispatch 的实体不存在 |
| 「JNPF Debugger」 | YAML sub-agent | 用户期望开箱即用，但触发需 main Claude 显式 |

三形态映射**一次性消除三义性**，未来新增能力可按规则归类。

### 5. 治理框架共享避免重复造轮子

Phase 8 已沉淀的 6 项治理资产：

| 资产 | 位置 |
|---|---|
| Evidence 五标签 | `Master Spec §11.1` |
| Hard Gate 矩阵 | `Master Spec §10.3` |
| Closed Gate | `Manual §11` |
| Schema 漂移检测 | `Master Spec §15` / ADR-023 |
| Triple-Key Iron Law | ADR-021 |
| NO-CHANGE 主动判断 | ADR-022 |

新 Agent **必须**接入这套治理，不允许自行定义第二套 evidence taxonomy、hard gate、closed gate。

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 不建组织级抽象，各自演化 | 灵活 | 「Agent」三义性永久存在；Aspire 阶段必踩坑 | 不可持续 |
| 采用 V3.0 KIMI 涅槃的 Python 外部状态机 | 物理隔离会话；JSON Schema 交接 | 与 Claude Code 内置调度冲突；增加运维成本 | 工程不可行（详见同系列 1 篇的反 Fugu 分析） |
| 采用 Fugu 化（RL 训练 + 动态角色） | 概念最前沿 | 单体 Claude Code 无训练信号；RL 不可行 | 已被同系列 1 篇否决 |
| **本方案（三形态映射 + 共享治理 + 渐进式封装）** | 兼容现有 Claude Code；三层清晰；渐进式 | 需新建若干 sub-agent 与 references；Phase A 需决策运行时 | ✅ 选择此项 |

---

## 后果

### 正面

1. **认知统一** — 「Agent」三义性消除；新增能力可按规则归类
2. **资产复用** — Phase 8 治理框架直接服务 11 个 Agent，不重造
3. **演进路径清晰** — 6 阶段生命周期 + Phase A-D 路线图可执行
4. **业务优先** — 共享治理保留「业务第一」原则（每个 Agent 都有 Domain Mapping）
5. **风险可控** — v1.0-draft → v1.0-final 5 项硬门槛明确，避免范围蔓延

### 负面

1. **新增文件** — 至少需要 1 个 sub-agent 包装（`table-refactoring-expert.md`）+ 13 个 Skill references
2. **形态选择争议** — 部分能力（如 Orchestrator）形态归属需讨论
3. **运行时锁定风险** — Phase A 决策后，迁移到另一种运行时成本较高
4. **过度抽象** — 组织级概念若脱离工程实现，易变成空话（已被同系列 1 篇警示）

### 风险缓解

| 风险 | 缓解措施 |
|---|---|
| 过度抽象 | 11 个 Agent 每个必须有资产矩阵行；空命名 Agent 禁止入库 |
| 运行时锁定 | Phase A 决策前不开始用户级 Agent 包装 |
| 形态混淆 | 持续以 §3.2 / §5.2 为准；新文件入库前审查 |
| 跨系列割裂 | 附录 A 持续交叉引用同系列 1-7 篇 |
| 范围蔓延 | v1.0-final 5 项硬门槛明确阻止 |

---

## 验证结果

### 内部一致性验证（已通过）

- ✅ 文档 §12.1 资产盘点矩阵：11 个 Agent × 6 阶段生命周期，每格有 `[状态]` 标签
- ✅ 文档 §12.2 三个关键缺口：每个缺口都有「现状 / 必须产物 / 模板 / 优先级」
- ✅ 文档 §12.3 Skill `references/` 缺口：13 个文件清单，每个有命名规范
- ✅ 文档附录 A：与同系列 1-7 篇的映射矩阵，无遗漏
- ✅ 文档附录 D：v1.0-final 升级 5 项硬门槛，全部可验证

### 锚定现有资产（已核验）

| 已存在 | 文档锚定 |
|---|---|
| `.claude/souls/architect/soul.md` | §3.2 业务型 Agent 实例 |
| `.claude/skills/table-refactor-expert/SKILL.md` | §6.2 表级重构专家本体 |
| `.claude/agents/jnpf-debugger.md` | §5.2 任务型 Agent 模板 |
| `.claude/_shared/assertion-discipline.md` | §11.1 Evidence 五标签 |
| `docs/universal/Phase-7-Final-Report.md` | §6.2 状态表 Phase 7 证据 |
| `docs/universal/Phase-8/Phase-8-最终关闭报告.md` | §6.2 状态表 Phase 8 证据 |

### 实施里程碑（计划中，待 Phase A 启动）

| 里程碑 | 验证标准 | 状态 |
|---|---|---|
| Phase A 完成 | 运行时已选；包装模板发布 | ⏳ PENDING |
| Table Refactoring Agent 包装 | `.claude/agents/table-refactoring-expert.md` 创建；dispatch 测试通过 | ⏳ PENDING |
| Skill references 补齐 | 13 个文件全部存在；引用路径全部可达 | ⏳ PENDING |
| Class Refactoring Production Validation | ≥10 真实类 KPI；0 严重错误 | ⏳ PENDING |
| v1.0-final 升级 | 附录 D 5 项硬门槛全部 ✅ | ⏳ PENDING |

---

## 与本仓库其他 AI 资产的关系

| 资产 | 关系 |
|---|---|
| `.claude/rules/*` 编码铁律 | **保留** — 是所有 Agent 的运行时输入 |
| L0-L11 Hooks | **保留** — 是所有 Agent 的强制守卫 |
| `.claude/souls/*` 8 个阶段角色 | **保留** — 是业务型 Agent 的工程实现 |
| `.claude/skills/*` 专家技能 | **保留** — 是专家型 Agent 的工程实现 |
| `.claude/agents/*` 任务单元 | **扩展** — 是任务型 Agent 的工程实现（待补充） |
| `docs/AI原生开发/1、多用户多任务并行/阶段A.md` | **保留** — 业务流水线约束输入 |

**本方案不引入新规则、新 Hooks、新铁律**（除非必要）。它是把现有抽象为组织级概念。

---

## 相关 ADR

- **ADR-019**: Table Refactoring Expert Skill v1.0 冻结决策（[本组织方案的首个成熟锚点]）
- **ADR-020**: R2-COMP 独立 AI 验证机制（[组织方案的治理验证范式]）
- **ADR-021**: Triple-Key Iron Law（[组织方案的 Evidence 落地例]）
- **ADR-022**: NO-CHANGE 主动判断原则（[组织方案的 Quality Gate 例]）
- **ADR-023**: Schema 漂移检测执行前强制规则（[组织方案的 Workflow 例]）

## 相关资产

- **主文档**：`docs/架构迭代/8、AI编程范式的再次进化/8、AI软件工程组织架构方案v1.0.md`（30.5 KB，v1.0-draft）
- **所属系列**：`docs/架构迭代/8、AI编程范式的再次进化/`（共 8 篇）
- **关联 Phase 8 资产**：`docs/universal/Phase-8/Phase-8-最终关闭报告.md`（93 表 / 0 事故）
- **Skill 工程化**：`docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md`
- **模板原型**：`.claude/agents/jnpf-debugger.md`（任务型 Agent 模板）

---

## 状态转换计划

```
[Proposed - 当前]
    ↓ Phase A 决策完成 + Table/Class Agent 包装完成
[Accepted]
    ↓ v1.0-final 5 项硬门槛全部达成
[Final]
```

**禁止跳过 Accepted 直接升 Final**。Accepted 期间可新增/修改 Agent 包装，但不引入新的治理框架或形态。

