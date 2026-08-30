# Phase 8 — JNPF Table Refactoring Master Execution Plan

> **唯一执行总计划**：Phase 8 全生命周期的唯一权威执行文档。
> **取代**：任何碎片化 T1/T2/T3 任务驱动推进。
> **生效条件**：用户审批通过（Phase 8 Master Plan Approval）。
> **生效后**：除非触发 Phase Gate / Batch Gate / Hard Gate，AI 工程师不得碎片化停顿等待逐任务审批。

**版本**：v1.0
**日期**：2026-08-30
**阶段**：Phase 8 OPEN / P8-0 READY
**上游**：Phase 0–7 FROZEN
**下游**：P8-0 → P8-A → P8-B → P8-C → P8-D → P8-E → JNPF Phase 1 Table Refactoring CLOSED

---

## 0. Master Principles（不变原则）

### 0.1 生产层级（Hierarchy）

```
Phase
  ↓
Batch
  ↓
Table Unit
```

- **Finding 是 Table Unit 内部工作对象，不是项目进度单位**
- **Task 仅用于内部执行步骤，不是审批单位**
- 人类介入点：Phase Gate / Batch Gate / Hard Gate（见 §14）

### 0.2 Table Unit 固定状态机

```
DISCOVERED
  ↓
ASSESSED
  ↓
DESIGNED
  ↓
READY
  ↓
REFACTORED / NO-CHANGE
  ↓
VERIFIED
  ↓
CLOSED
```

**No-change 是正式生产结果**，不是"失败"。

### 0.3 阶段固定闭环

```
PLAN
  ↓
EXECUTE
  ↓
VERIFY
  ↓
TEST
  ↓
EVIDENCE
  ↓
ACCEPT
  ↓
CLOSED
```

- 未达到 Exit Criteria：阶段不得关闭
- 达到 Exit Criteria：阶段必须立即关闭，不得继续无限探索

### 0.4 严禁事项（永远生效）

| # | 严禁 | 后果 |
|---|---|---|
| 1 | 无限 Discovery | 违反 Phase 8 节奏 |
| 2 | 无限 Audit | 违反 §0.3 |
| 3 | 逐 Finding 审批 | 违反 §0.1 |
| 4 | 逐 Task 停顿 | 违反 Master Plan |
| 5 | 先分析 289 表再统一执行 | 违反 P8-C 流程 |
| 6 | 为产生 diff 而重构 | 违反 §0.2（NO-CHANGE 合法） |
| 7 | 为 KPI 降低证据标准 | 违反 Evidence Sufficiency |
| 8 | 单个 JNPF finding 污染 Universal Core | 违反 Problem Routing |
| 9 | 单个问题阻塞整个 Phase | 违反 §13 Escalation |
| 10 | 未达 Exit Criteria 关闭阶段 | 违反 §0.3 |
| 11 | 达到 Exit Criteria 继续探索 | 违反 §0.3 |

---

## 1. Phase 8 全生命周期概览

```
P8-0  Production Calibration
     ↓
P8-A  Shadow Production (5 tables, no DB writes)
     ↓
P8-A Safety Gate
     ↓
P8-B  Controlled Production (2-3 batches × 3-8 tables)
     ↓
P8-B Stability Gate
     ↓
P8-C  Autonomous Batch Production
     ↓
P8-D  Scale Production (batch groups by domain/risk)
     ↓
P8-E  Final Table Baseline Closure
     ↓
289/289
     ↓
JNPF Phase 1 Table Refactoring CLOSED
```

---

## 2. P8-0 — Production Calibration

### 2.1 Objective

建立 Phase 8 生产基础设施，验证所有 registry / graph / KPI / routing mechanism 可用。**不得修改任何业务表**。

### 2.2 Scope

**包含**：
- Table inventory 全量建立
- Dependency graph 构建
- Table Unit Registry 建立
- Batch Registry 建立
- KPI tracking mechanism 建立
- Problem Routing Log 建立
- Orchestration dry-run

**不包含**：
- 任何业务表评估
- 任何 DDL / DML 执行
- Skill 输出验证

### 2.3 Input

- JNPF DB schema（所有表 + 视图 + 存储过程）
- JNPF.Repository 层 Entity 定义
- JNPF Modularity 模块清单（app / codegen / inteAssistant / system / visualdata / visualdev / workflow 等）
- Phase 7 冻结产物（Universal Skill / JNPF Extension / Foundry Profile）

### 2.4 Development Steps

1. **Inventory Extraction** — 从 DB schema 提取所有表（含列、类型、约束）
2. **Module Mapping** — 将每张表映射到所属模块
3. **Dependency Discovery** — 通过 FK 关系构建 dependency graph
4. **Registry Creation** — 建立 `Table Unit Registry`（289 张表清单 + 状态）
5. **Batch Registry Creation** — 建立 `Batch Registry`（批次分组容器）
6. **KPI Mechanism Setup** — 部署 KPI 采集模板与文件结构
7. **Routing Log Setup** — 部署 Problem Routing Log 模板
8. **Dry-run Execution** — 模拟一个 Table Unit 的完整生命周期（DISCOVERED → CLOSED），但不对实际表执行任何操作
9. **Mechanism Validation** — 验证所有 mechanism 在 dry-run 中工作正常

### 2.5 Workflow

```
P8-0 Kickoff
  ↓
Inventory build (parallel)
  ↓
Dependency graph build
  ↓
Registry commit
  ↓
Mechanism deploy
  ↓
Dry-run
  ↓
Mechanism validation report
  ↓
P8-0 Exit Gate check
  ↓
P8-0 CLOSED → P8-A OPEN
```

### 2.6 Verification

| 验证项 | 通过条件 |
|---|---|
| Inventory complete | 289 张表全部登记（含已完成的 Pilot 表） |
| Dependency graph complete | 所有 FK 关系表达为有向图，无悬挂边 |
| Registry writeable | Table Unit Registry / Batch Registry 可读写 |
| KPI mechanism functional | 模板可填充，可计算累计值 |
| Routing log functional | 6 类问题可路由，可记录 |
| Dry-run successful | 模拟 Table Unit 通过完整状态机 |

### 2.7 Testing

- **Level 1**：每个 mechanism 单独可调用
- **Level 2**：dry-run 完整状态机无错误
- **Level 3**：N/A（P8-0 不涉及业务表）

### 2.8 Evidence

- `docs/universal/Phase-8/p8-0/table-inventory.md`（289 张表清单）
- `docs/universal/Phase-8/p8-0/dependency-graph.md`（FK 关系图）
- `docs/universal/Phase-8/p8-0/registry-snapshot.md`（registry 当前状态）
- `docs/universal/Phase-8/p8-0/dry-run-report.md`（模拟执行记录）

### 2.9 Acceptance

P8-0 仅在所有 mechanism 验证通过后接受。

### 2.10 Exit Criteria

```
[ ] Inventory usable           — 289 张表可查
[ ] Dependency graph usable    — FK 关系可查
[ ] Batch mechanism usable     — 可创建/查询/关闭 Batch
[ ] KPI mechanism usable       — 可填充/计算/导出
[ ] Routing mechanism usable   — 可分类/路由/记录
[ ] Dry-run successful         — 状态机无错误
```

### 2.11 Max Scope

**绝对不扩大**：不评估任何业务表的 R 等级；不输出任何 Finding；不修改任何 DB 对象。

### 2.12 Escalation

- Inventory 不全 → 检查 DB connection / schema 提取脚本
- Dependency graph 错误 → 检查 FK 提取完整性
- Registry 不可用 → 检查文件权限 / 格式
- Dry-run 失败 → 检查状态机实现

### 2.13 KPI

P8-0 不采集生产 KPI（无业务表评估）。仅采集建设指标：
- Inventory completion: 100%
- Graph edge count: ___
- Registry entries: 289
- Dry-run status: PASS/FAIL

### 2.14 Exit Gate

**P8-0 → P8-A 切换条件**：所有 Exit Criteria 勾选完成。

---

## 3. P8-A — Shadow Production

### 3.1 Objective

通过 5 个真实 Table Unit 的双轨评估（AI Track A + Human Independent Track B），建立 Safety Gate 真实基线。

### 3.2 Scope

**包含**：
- 5 个 Table Unit 的 Skill 评估
- AI Track A 产出
- Human Independent Track B 产出
- AI/Human Comparison
- Safety Gate 判定
- Productivity baseline 建立

**不包含**：
- 任何 DB 写操作
- 修改 Skill / Extension / Profile
- 扩大 Shadow 范围

### 3.3 Input

- P8-0 输出的 Inventory / Registry
- Universal Skill v1.0
- JNPF Extension v1.0
- Foundry Target Profile v1.0
- Table Selection 矩阵评估后选定的 5 张表

### 3.4 Development Steps

1. **Table Selection** — 从候选池按 Selection Matrix 选 5 张表（R0/R1 + R2 + R3+ 自然分布）
2. **AI Track A Execution** — Skill 对 5 张表分别执行完整评估
3. **Human Independent Review Setup** — 准备 blind review 流程（评审员不见 Track A）
4. **Track B Execution** — 人类评审员对同一 5 张表独立产出 Track B
5. **Comparison Execution** — AI/Human 逐项对比
6. **Divergence Analysis** — 分类每项 divergence 到 6 类问题路由
7. **Safety Gate Check** — 4 项硬指标计算
8. **Productivity Baseline Establish** — Median / P90 / Human hours / Tables per AI-hour 采集
9. **Shadow Gate Decision** — PASS / FAIL 判定

### 3.5 Workflow

```
Table 01-05 (5 tables)
  ↓ (per table)
AI Track A
  ↓
Human Independent Track B (blind)
  ↓
Comparison
  ↓
Divergence Log
  ↓
5 tables complete
  ↓
Safety Gate check
  ↓
Productivity Baseline record
  ↓
Shadow Gate Decision
  ↓
P8-A CLOSED → P8-B OPEN
```

### 3.6 Verification

| 验证项 | 通过条件 |
|---|---|
| 5 tables evaluated | AI Track A 全部完成 |
| 5 tables reviewed | Human Track B 全部完成（blind） |
| Comparison complete | 每张表 finding / risk / HG / closure 4 维对比完成 |
| Safety Gate check | 4 项硬指标计算 |

### 3.7 Testing

- **Level 1**：每张表 targeted verification（schema / query / index / FK / lifecycle / tenant 6 维证据）
- **Level 2**：N/A（P8-A 不涉及 batch regression）
- **Level 3**：N/A（P8-A 不涉及 phase acceptance）

### 3.8 Evidence

- `docs/universal/Phase-8/p8-a/shadow/table-01-{name}/track-a.md`
- `docs/universal/Phase-8/p8-a/shadow/table-01-{name}/track-b.md`
- `docs/universal/Phase-8/p8-a/shadow/table-01-{name}/comparison.md`
- `docs/universal/Phase-8/p8-a/shadow/{table-01..05}/divergence-log.md`
- `docs/universal/Phase-8/p8-a/shadow-gate-result.md`
- `docs/universal/Phase-8/p8-a/productivity-baseline.md`

### 3.9 Acceptance

Shadow Gate PASS 4 项硬指标全部 = 0。

### 3.10 Exit Criteria

```
Safety Gate:
[ ] Hard Gate FN                     = 0
[ ] P0/P1 decision error             = 0
[ ] Universal Core contamination     = 0
[ ] TABLE CLOSED decision error      = 0

Productivity Baseline:
[ ] Median Table Completion Time: recorded
[ ] P90 Table Completion Time: recorded
[ ] Tables / AI-hour: recorded
[ ] Human Review Time / table: recorded
```

### 3.11 Max Scope

**绝对不扩大**：仅 5 张表；不写库；不修改任何业务 schema。

### 3.12 Escalation

- Safety Gate FAIL → 暂停 Shadow → divergence 分类 → 修正后重新 Shadow
- Critical divergence → 立即暂停该 Table Unit → 路由修正

### 3.13 KPI

| 类别 | 指标 |
|---|---|
| Safety | HG FN / P0-P1 error / Core contamination / TABLE CLOSED error |
| Quality | FP / FN count |
| Productivity | Median / P90 / Tables per AI-hour |
| Human | Review hours per table |
| Risk Misclassification | Count (non-blocking) |

### 3.14 Exit Gate

**P8-A → P8-B 切换条件**：Safety Gate 4 项 = 0 + Productivity baseline 已记录。

---

## 4. P8-B — Controlled Production

### 4.1 Objective

通过 2-3 个受控 Batch 验证 Skill 在真实执行模式下的稳定性，建立 Stability Gate。

### 4.2 Scope

**包含**：
- 2-3 个 Batch（每 Batch 3-8 Table Units）
- R0/R1/R2 自动执行
- R3+ 强制 Human Gate
- 完整 Before → Change → Verify → Regression → Closure 流程
- Human spot-check

**不包含**：
- 修改 Skill / Extension / Profile
- 单 Batch 超过 8 表

### 4.3 Input

- P8-A 产出的 Safety Gate 通过记录
- P8-A Productivity Baseline
- P8-0 Registry 中待处理 Table Units
- Skill / Extension / Profile 冻结产物

### 4.4 Development Steps

1. **Batch 01 Selection** — 从 Registry 选 3-8 Table Units（强关联优先同批）
2. **Batch 01 Plan** — 制定 Batch Plan（顺序、依赖、风险分布）
3. **Batch 01 Execution** — 按 Batch Plan 执行
4. **Batch 01 Verification** — schema / integrity / migration / query / behavior / rollback 6 维验证
5. **Batch 01 Closure** — 每张表 → Batch Closure → Batch Verification Record
6. **Batch 02-03 Repeat** — 同样流程
7. **Stability Assessment** — 比较 Batch 01 vs Batch 02 趋势
8. **Stability Gate Decision** — PASS / FAIL

### 4.5 Workflow

```
Batch 01 Plan
  ↓
Table Units (3-8)
  ↓ (per table)
ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED
  ↓
Batch Verification
  ↓
Batch Closure
  ↓
Batch 02 Plan
  ↓
...
  ↓
Stability Assessment
  ↓
Stability Gate Decision
  ↓
P8-B CLOSED → P8-C OPEN
```

### 4.6 Verification

| 维度 | 验证内容 |
|---|---|
| schema | DDL 正确执行 |
| integrity | FK / 约束正确 |
| migration | 数据迁移无损 |
| query | 查询路径等价 |
| application behavior | 业务行为不变 |
| rollback/recovery | 可回滚（仅对 R3+） |

### 4.7 Testing

- **Level 1**：每张表 targeted verification
- **Level 2**：Batch regression（Batch 内 + Batch 间对比）
- **Level 3**：N/A（P8-B 不涉及 phase acceptance）

### 4.8 Evidence

- `docs/universal/Phase-8/p8-b/batch-01/batch-plan.md`
- `docs/universal/Phase-8/p8-b/batch-01/table-{n}/evidence.md`
- `docs/universal/Phase-8/p8-b/batch-01/batch-verification-record.md`
- `docs/universal/Phase-8/p8-b/batch-01/batch-closure.md`
- `docs/universal/Phase-8/p8-b/batch-{02,03}/...`
- `docs/universal/Phase-8/p8-b/stability-gate-result.md`

### 4.9 Acceptance

Stability Gate PASS（safety / quality / efficiency 三维稳定）。

### 4.10 Exit Criteria

```
[ ] Batch 01 closed and verified
[ ] Batch 02 closed and verified
[ ] (Optional) Batch 03 closed and verified

Stability Gate:
[ ] HG FN: 0 in both Batch 01 & 02
[ ] P0/P1 error: 0 in both Batch 01 & 02
[ ] Core contamination: 0 in both Batch 01 & 02
[ ] Rework Rate: not increasing Batch 01 → Batch 02
[ ] Human Gate Rate: not increasing Batch 01 → Batch 02
[ ] Median time: not increasing Batch 01 → Batch 02
[ ] Tables / AI-hour: not decreasing Batch 01 → Batch 02
```

### 4.11 Max Scope

- 单 Batch：3-8 Table Units
- 总 Batch 数：2-3
- 总 Table Units：≤ 24

### 4.12 Escalation

- Batch Verification FAIL → 暂停后续 Table Units → 修复 → 重跑该 Table Unit
- Stability Gate FAIL → 留在 P8-B / 调查 divergence

### 4.13 KPI

| 类别 | 指标 |
|---|---|
| Safety | HG FN / P0-P1 error / Core contamination |
| Quality | FP / FN / Rework |
| Productivity | Median / P90 / Tables per AI-hour |
| Human | Human Gate Rate / Review hours |
| Progress | Batch throughput |

### 4.14 Exit Gate

**P8-B → P8-C 切换条件**：Stability Gate 全部通过。

---

## 5. P8-C — Autonomous Batch Production

### 5.1 Objective

进入真正的批量生产模式：3-8 Table Units/Batch，按 dependency + business coherence 组织。

### 5.2 Scope

**包含**：
- 多 Batch 并行推进（默认串行）
- R0/R1/R2 自动执行
- R3+ Human Gate（保留）
- Batch verification / closure 流程

**不包含**：
- 修改 Skill / Extension / Profile
- 单 Batch > 8 表
- 先全量 289 表分析再执行

### 5.3 Input

- P8-B Stability Gate 通过记录
- P8-0 Registry
- P8-B KPI 数据
- Batch Registry（已建立的批次容器）

### 5.4 Development Steps

1. **Next Batch Selection** — 从 Registry 按 dependency + business coherence 选下一 Batch
2. **Dependency Check** — 验证 Batch 内 Table Unit 之间依赖关系清晰
3. **Batch Plan** — 制定 Batch Plan
4. **Execute Table Units** — 按 Batch Plan 串行/并行执行
5. **Table Closure** — 每张表 → CLOSED
6. **Batch Verify** — Batch Verification Record
7. **Batch Accept** — Batch Acceptance Record
8. **Registry Update** — Registry 中该 Batch 的 Table Unit 状态更新
9. **Next Batch** — 进入下一 Batch

### 5.5 Workflow

```
Select Next Batch
  ↓
Dependency Check
  ↓
Batch Plan
  ↓
Execute Table Units (parallel within batch where safe)
  ↓
Table Closure (per table)
  ↓
Batch Verify
  ↓
Batch Accept
  ↓
Registry Update
  ↓
KPI Update
  ↓
Next Batch
  ↓
... (continue until P8-D trigger)
```

### 5.6 Verification

- **Per Table**：Level 1 6 维验证
- **Per Batch**：Batch Verification Record
- **Cross Batch**：相邻 Batch regression 检查

### 5.7 Testing

- **Level 1**：每张表 targeted
- **Level 2**：Batch regression
- **Level 3**：仅当触发 Phase Gate 或 Core evolution 时

### 5.8 Evidence

- `docs/universal/Phase-8/p8-c/batch-{nn}/batch-plan.md`
- `docs/universal/Phase-8/p8-c/batch-{nn}/table-{n}/evidence.md`
- `docs/universal/Phase-8/p8-c/batch-{nn}/batch-verification-record.md`
- `docs/universal/Phase-8/p8-c/batch-{nn}/batch-acceptance-record.md`
- `docs/universal/Phase-8/p8-c/kpi/cumulative-kpi.md`（滚动累计）

### 5.9 Acceptance

每 Batch 完成后由 Batch Gate 自动判定（除非触发 Safety 异常）。

### 5.10 Exit Criteria

**P8-C → P8-D 切换条件**：
```
[ ] 累计完成 ≥ 30 Table Units
[ ] Stability Gate 在连续 3 个 Batch 内维持 PASS
[ ] Median Table Completion Time 在 productivity baseline ± 20% 范围内
[ ] Rework Rate ≤ 10%
[ ] Human Gate Rate ≤ 20%（P8-B baseline 对比）
```

### 5.11 Max Scope

- 单 Batch：3-8 Table Units
- P8-C 总 Table Units：30-80（视稳定性而定）

### 5.12 Escalation

- Batch 内 Safety KPI FAIL → 暂停 Batch → 调查 divergence → 修复 → 继续
- Stability Gate FAIL → 暂停 P8-D 升级 → 留在 P8-C

### 5.13 KPI

完整 5 类指标全部采集（Safety / Quality / Productivity / Human / Progress）。

### 5.14 Exit Gate

**P8-C → P8-D 切换条件**：所有 Exit Criteria 勾选完成 + 人工 Phase Gate 确认。

---

## 6. P8-D — Scale Production

### 6.1 Objective

进入规模化生产阶段：通过 Batch Groups（按 domain / risk 组织）持续推进，直至 289 Table Units 接近完成。

### 6.2 Scope

**包含**：
- Batch Groups 组织（按 business domain / dependency cluster / risk level）
- 多个 Batch Group 并行执行
- 全量 KPI 持续采集
- 持续 Stability / Safety monitoring

**不包含**：
- 修改 Skill / Extension / Profile
- 跨 Batch Group 无计划合并

### 6.3 Input

- P8-C Stability Gate 通过记录
- 完整 Registry（剩余 Table Units）
- 累计 KPI

### 6.4 Development Steps

1. **Batch Group Formation** — 将剩余 Table Units 按 domain / dependency / risk 分组
2. **Group Prioritization** — 高耦合/高风险 group 优先
3. **Group Execution** — 每个 Batch Group 内部按 P8-C 流程执行
4. **Cross-group Monitoring** — Safety / Rework / Human Gate 跨 group 监控
5. **Continuous Calibration** — 基于 KPI 调整 Batch 规模 / 节奏
6. **Progress Tracking** — 滚动累计 Tables Closed / Tables Remaining

### 6.5 Workflow

```
Remaining Registry
  ↓
Batch Group Formation
  ↓
Group 01 Execute (P8-C flow)
  ↓
Group 02 Execute (P8-C flow)
  ↓
... (parallel or serial per dependency)
  ↓
Continuous KPI update
  ↓
Stability monitoring
  ↓
≥ 95% Tables Closed
  ↓
P8-D CLOSED → P8-E OPEN
```

### 6.6 Verification

- **Per Table**：Level 1
- **Per Batch**：Level 2
- **Per Group**：跨 Batch 累计 regression
- **Per Phase**：当触发 Phase Gate 时 Level 3

### 6.7 Testing

- **Level 1**：每张表 targeted
- **Level 2**：Batch regression
- **Level 3**：仅 Phase Gate 触发时

### 6.8 Evidence

- `docs/universal/Phase-8/p8-d/group-{nn}/batch-{mm}/...`
- `docs/universal/Phase-8/p8-d/cumulative-kpi.md`
- `docs/universal/Phase-8/p8-d/progress-tracker.md`

### 6.9 Acceptance

P8-D 不需要单独 Acceptance。验收通过 P8-E Final Closure 完成。

### 6.10 Exit Criteria

```
[ ] 累计完成 ≥ 95% Table Units（≥ 274/289）
[ ] Safety KPI 在 P8-D 全程维持 PASS
[ ] Rework Rate ≤ 10%
[ ] Human Gate Rate ≤ 20%
[ ] Remaining Table Units 均处于 ASSESSED / DESIGNED / READY / DEFERRED 状态
[ ] 无 UNKNOWN 状态
```

### 6.11 Max Scope

- Batch Group：5-15 Table Units
- P8-D 总 Table Units：约 200（视 P8-C 完成数）

### 6.12 Escalation

- Rework Rate > 10% → Pause scaling → Identify systemic issue → Local correction → Rerun affected Table Units → Resume
- Safety KPI FAIL → 立即暂停 → 路由修正

### 6.13 KPI

完整 5 类 + Progress（Tables Closed / Remaining）。

### 6.14 Exit Gate

**P8-D → P8-E 切换条件**：所有 Exit Criteria 勾选完成。

---

## 7. P8-E — Final Table Baseline Closure

### 7.1 Objective

完成 289/289 Table Units 的最终状态确认，建立 JNPF Table-Level Refactoring Baseline v1.0。

### 7.2 Scope

**包含**：
- 完成剩余 Table Units
- 处理所有 DEFERRED 项（re-decision 或 accept）
- 最终每张表的状态确认（CLOSED / ACCEPTED-AS-IS / DEFERRED with explicit reason）
- Baseline 文档发布

**不包含**：
- 重新评估已完成 Table Units
- 改变已 CLOSED 状态

### 7.3 Input

- P8-D 输出（≥ 95% 完成）
- Registry 中所有未完成 / DEFERRED Table Units
- 累计 KPI

### 7.4 Development Steps

1. **Remaining Tables Execution** — 完成 P8-D 剩余 Table Units
2. **DEFERRED Resolution** — 每个 DEFERRED 重新决策：CLOSE / ACCEPT-AS-IS / DEFER-WITH-REASON
3. **Final State Verification** — 验证 289/289 全部有明确状态
4. **UNKNOWN Elimination** — 确保无 UNKNOWN 状态
5. **Baseline Document Compilation** — 汇编 `JNPF-Table-Level-Refactoring-Baseline-v1.0`
6. **Final Phase Acceptance** — Phase 8 整体验收

### 7.5 Workflow

```
Remaining Tables
  ↓
Execute + Closure
  ↓
DEFERRED Resolution
  ↓
289/289 State Verification
  ↓
Baseline Document
  ↓
Phase 8 Final Acceptance
  ↓
P8-E CLOSED
  ↓
JNPF Phase 1 Table Refactoring CLOSED
```

### 7.6 Verification

- **Per Table**：最终 state 确认
- **Per Batch**：N/A（无新 Batch）
- **Per Phase**：Level 3 acceptance

### 7.7 Testing

- **Level 1**：每张表最终验证
- **Level 2**：N/A
- **Level 3**：Phase acceptance 全量

### 7.8 Evidence

- `docs/universal/Phase-8/p8-e/final-table-state.md`（289 张表最终状态）
- `docs/universal/Phase-8/p8-e/deferred-resolutions.md`
- `docs/universal/Phase-8/p8-e/baseline-v1.0.md`
- `docs/universal/Phase-8/p8-e/phase-acceptance-record.md`

### 7.9 Acceptance

Phase 8 整体 Acceptance：289/289 + Baseline + Phase Acceptance Record。

### 7.10 Exit Criteria

```
[ ] 289/289 Table Units have explicit final state
[ ] 0 UNKNOWN states
[ ] 0 in-progress states
[ ] Baseline document published
[ ] Phase Acceptance Record signed off
[ ] All Batch Evidence archived
[ ] Cumulative KPI archived
```

### 7.11 Max Scope

仅处理剩余 Table Units（通常 < 15）+ DEFERRED resolution。

### 7.12 Escalation

- 仍有 UNKNOWN → 路由到 Human Decision（不得自动判 CLOSED）
- DEFERRED 无理由 → 补 reason 或转为 CLOSED

### 7.13 KPI

- Final Tables Closed: 289/289
- Final Rework Rate: cumulative %
- Final Human Gate Rate: cumulative %
- Final Safety KPI: PASS/FAIL

### 7.14 Exit Gate

**P8-E → JNPF Phase 1 Table Refactoring CLOSED 切换条件**：所有 Exit Criteria 勾选完成。

---

## 8. 测试分层（Cross-Phase）

```
Level 1: Table targeted verification
    - 每张表 6 维证据（schema / query / index / FK / lifecycle / tenant）
    - 每张表必做

Level 2: Batch regression
    - Batch 内 + Batch 间对比
    - 每个 Batch 必做

Level 3: Phase acceptance
    - 仅当发生以下情况时扩大：
      * shared infrastructure change
      * Core evolution
      * P0/P1 incident
      * unexplained regression
    - 不允许每个 Table Unit 都跑 Level 3
```

**绝对禁止**：每张表都跑全项目测试。

---

## 9. Evidence 分层（Cross-Phase）

```
Table Unit:
    - Table Evidence Ledger (per table)

Batch:
    - Batch Evidence / Verification Record (per batch)

Phase:
    - Phase Acceptance Record (per phase at exit)
```

**Evidence Sufficiency 原则**：达到足够决策 → 停止取证。

---

## 10. Problem Routing（Cross-Phase）

```
JNPF-specific
    → JNPF Extension

Skill execution issue
    → Skill Evolution (Level A/B/C)

Universal rule issue
    → Master Spec Evolution

BBB capability gap
    → BBB Product Backlog

Business ambiguity
    → Human Decision

Provider/database constraint
    → Target/Provider Profile
```

**硬规则**：
- 单个 Table finding 不得自动暂停整个 Phase
- JNPF-specific 不得直接修改 Universal Core
- Business ambiguity 不得 AI 自行决定

---

## 11. Rework Budget（Cross-Phase）

```
Batch 默认 Rework Rate ≤ 10%

超过 10%:
    pause scaling
    → identify systemic issue
    → local correction
    → rerun affected Table Units
    → resume
```

**绝对禁止**：无限返工。

---

## 12. 生产 KPI（Cross-Phase）

### 12.1 Safety（所有阶段采集）

```
Hard Gate FN                    Target: 0
P0/P1 decision error            Target: 0
Universal Core contamination    Target: 0
TABLE CLOSED decision error     Target: 0
```

### 12.2 Quality（所有阶段采集）

```
False Positive Rate
False Negative Rate
Rework Rate
```

### 12.3 Productivity（所有阶段采集）

```
Median Table Completion Time
P90 Table Completion Time
Tables / AI-hour
```

### 12.4 Human Work（所有阶段采集）

```
Human Gate Rate
Human Review Time / table
```

### 12.5 Progress（所有阶段采集）

```
Tables Closed
Tables Remaining
Batch throughput
```

### 12.6 核心指标

> **High-quality Table Units Closed / AI-hour**

不是 Finding 数量、修改行数、Index 数量、DDL 数量。

---

## 13. Escalation 原则（Cross-Phase）

- **Safety KPI 异常**：立即暂停相关 Table Unit / Batch，按 Problem Routing 分类
- **Single finding 不阻塞**：单个 Table Unit 的 finding 路由到对应通道，不暂停整个 Phase
- **Hard Gate 仅在 P0/P1 风险判定时触发**：普通 R1/R2 不触发人工 Gate
- **Phase Gate 仅在阶段切换时触发**：阶段内不重复触发

---

## 14. 人类介入点（唯一三种）

### 14.1 Phase Gate

阶段是否进入下一阶段。

**触发点**：每个 Phase Exit Gate。

**形式**：用户/项目负责人审批。

### 14.2 Batch Gate

批次是否合格。

**触发点**：每个 Batch Acceptance。

**形式**：Batch Acceptance Record 签字（AI 自签 + 必要时人工签）。

### 14.3 Hard Gate

业务/数据/架构高风险决策。

**触发点**：
- HG#1（tenant isolation breach）
- HG#2（data integrity violation）
- HG#3（schema change requiring migration）
- HG#4（cross-module dependency）
- HG#5（business ambiguity）

**形式**：Human Decision。

### 14.4 其他默认

其他普通任务由 AI 工程师按 Master Execution Plan 自主连续推进。**不碎片化停顿等待逐任务审批**。

---

## 15. Phase 8 Final Roadmap

```
P8-0  Calibration
     ↓
P8-A  Shadow
     ↓
P8-A Safety Gate
     ↓
P8-B  Controlled
     ↓
P8-B Stability Gate
     ↓
P8-C  Autonomous Batch
     ↓
P8-D  Scale
     ↓
P8-E  Final Closure
     ↓
289/289
     ↓
JNPF Phase 1 Table Refactoring CLOSED
```

---

## 16. Phase 8 核心原则（一句话）

> **不以"证明 Skill 完美"为目标，以"在可接受风险内持续稳定地产出已验证的 Table Units"为目标。**

---

## 17. 当前状态

```
Phase 0–7                            ✅ CLOSED
Phase 8                              🟢 OPEN
P8-A Shadow Preparation              ✅ PREPARED
Phase 8 Master Execution Plan        📋 AWAITING APPROVAL
P8-0 Calibration                     ⏸ NOT STARTED
P8-A Shadow Execution                ⏸ NOT STARTED
P8-B Controlled                      ⏸ NOT REACHED
P8-C Autonomous                      ⏸ NOT REACHED
P8-D Scale                           ⏸ NOT REACHED
P8-E Final Closure                   ⏸ NOT REACHED
```

---

## 18. Approval

### Phase 8 Master Plan Approval（待用户批准）

批准后：
- P8-0 Calibration 启动
- 除非触发 Phase Gate / Batch Gate / Hard Gate，AI 工程师按本 Master Plan 自主连续推进
- 不再采用 T1/T2/T3 任务驱动方式
- 不再逐 Finding 审批

最终目标：

> **在既定质量门槛下，以可预测的 Batch throughput 完成 289 张 JNPF 表级重构。**
