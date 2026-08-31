# ADR-022: NO-CHANGE 主动判断原则（数据库治理成熟度核心）

**状态:** Final
**日期:** 2026-08-30
**阶段:** Phase 8 / 数据库治理文化

---

## 背景

传统数据库治理的隐含假设：**"优化是好事，应该尽量多做"**。这种假设导致：

1. **无收益改造** — 为改而改、增加维护成本
2. **高风险表误修改** — 核心表（如 base_user）盲目加索引
3. **核心资产保护不足** — 没有机制主动避开 R3+ 高风险表
4. **AI 治理文化缺失** — AI 只会"找问题"，不会"判断什么时候不动"

Phase 8 Table Refactoring Expert Skill 实际执行中发现：

- 22 张表经 7 维度评估后判定 **NO-CHANGE**
- 16 张 R3+ 高风险表全部判定 NO-CHANGE 或 DEFERRED
- 这一决策本身就是 Skill 治理成熟度的体现

---

## 决策内容

**NO-CHANGE 是 Table Refactoring Expert Skill 的合法决策模式，不构成"未完成任务"。**

```
NO-CHANGE 触发条件（任一满足）：
  1. 现有索引已覆盖查询需求
  2. R3+ 高风险表（自动保护）
  3. 修改风险大于收益
  4. 历史遗留模块（等专项治理）
  5. 关键实体受 Hard Gate 保护
  6. 数据量 < 100 行（小表无需索引）

NO-CHANGE 与 REFACTORED 边界：
  | 场景 | 默认决策 |
  |------|---------|
  | 已有 N 个索引覆盖查询需求 | NO-CHANGE |
  | 完全无索引 | REFACTORED |
  | 部分覆盖 + 明确业务需求 | REFACTORED（针对性补全） |
  | R3+ 高风险 | NO-CHANGE（保护） |
  | 数据量 < 100 行 | NO-CHANGE（小表无需索引） |
```

---

## 理由

### 1. 避免无收益改造

```
Phase 8 实际数据：

| 类别 | 数量 | 占比 | 价值判断 |
|------|------|------|---------|
| REFACTORED | 65 | 73% | 明确性能收益 |
| NO-CHANGE | 22 | 25% | AI 主动避免误修改 |
| DEDUPLICATED | 1 | 1% | 视图继承基表索引 |
| RETAIN-AS-EXCEPTION | 1 | 1% | OUT_OF_SCOPE 例外 |

22 张 NO-CHANGE 表避免了：
  - 索引维护成本（22 个无用索引）
  - DDL 部署失败风险
  - Schema 漂移累积
  - 团队认知负荷
```

### 2. 核心资产保护机制

Phase 8 主动判定 NO-CHANGE 的核心资产：

| 表 | 模块 | 风险 | 保护理由 |
|----|------|------|---------|
| base_user | system-core | R3+ | 80+ 字段、47+ 外键、登录核心链路 |
| sa_data_dictionary | inteAssistant-SA | R3+ | 多版本字段、跨子系统引用 |
| WH_Bill 系列 (6 张) | warehouse | R3+ | 历史遗留模块 |
| flow_task | workflow | R3+ | 流程核心任务表 |

这 16 张 R3+ 表全部 NO-CHANGE，避免了对核心业务表的误修改。

### 3. Skill 治理成熟度的核心标志

> "知道什么时候不动"是 AI 治理最重要的能力之一。

AI 不是"找问题机器"，而是"架构决策辅助专家"。NO-CHANGE 文化让 Skill 从"过度优化"转向"精准优化"。

### 4. 与 Phase 8 实际结果一致

```
Phase 8 全部 93 张表的处置：
  - 65 REFACTORED：实际产生性能收益
  - 22 NO-CHANGE：AI 判断无需修改（保护不动）
  - 1 DEDUPLICATED：视图继承基表索引（节省维护成本）
  - 1 RETAIN-AS-EXCEPTION：OUT_OF_SCOPE 例外

P0/P1 错误：0
生产回滚：0
数据丢失：0

→ NO-CHANGE 不仅是合法结果，而且是降低风险的关键策略
```

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 必须修改（无 NO-CHANGE 选项） | KPI 看起来"做事" | 误修改风险高、维护成本 | 不可持续 |
| 自动决定，无人类把关 | 效率高 | 决策不透明、争议无解 | 高风险场景需要人工 |
| 仅 R3+ 高风险不修改 | 保守 | 低风险表可能也 NO-CHANGE | 浪费机会 |
| **5 触发条件 + 边界规则（本决策）** | 平衡效率与安全 | 需 Skill 训练有素 | ✅ 选择此项 |

---

## 后果

### 正面

- **核心资产保护** — base_user 等 R3+ 表 0 误修改
- **降低维护成本** — 22 张表免于无意义索引维护
- **展现治理成熟度** — AI 不只是找问题，更懂什么时候不动
- **决策可审计** — 每个 NO-CHANGE 都有 Evidence 证明

### 负面

- **KPI 呈现挑战** — "完成数"不再是简单的 93 张表
- **业务方可能质疑** — "为什么这张表没改？"
- **需要充分的证据文档** — NO-CHANGE 必须可解释

### 风险缓解

- 每个 NO-CHANGE 都有 Evidence 文件证明决策依据
- 22 张 NO-CHANGE 表在 Change Catalog 中有专门章节解释
- KPI 统计使用 `Action` 字段区分（REFACTORED / NO-CHANGE / ...）
- Phase 8 4 层资产（战略/管理/技术/机器）都对 NO-CHANGE 价值做充分说明

---

## NO-CHANGE 表清单（Phase 8）

详见 `docs/architecture/v52/database-modernization/JNPF-表级重构-技术变更目录.md` §4 NO-CHANGE 表目录

```
22 张 NO-CHANGE 表分布：

R3+ 高风险保护：15 张（68%）
├─ flow_task (workflow R3+)
├─ WH_Bill, WH_BillDetail, WH_Customer, WH_Material, WH_Supplier, WH_Depot (warehouse R3+)
└─ (其他 R3+ 高风险表)

R2 标准确认：7 张（32%）
├─ blade_visual, blade_visual_category, BASE_REPORT, report_charts (visualdata)
├─ flow_comment, flow_event_log, flow_task_operator_user, flow_task_circulate, flow_visible (workflow)
├─ ext_product_classify, ext_email_send (extension)
├─ wform_applybanquet, wform_leaveapply (form-template)
└─ BASE_AI_EVAL_GOLDEN_SET, BASE_AI_PIPELINE_S2_PROGRESS (AI)
```

---

## 验证结果

```
Phase 8 NO-CHANGE 决策结果：

NO-CHANGE 表清单 (22 张)：
  - 全部有 Evidence 文件证明决策依据
  - 0 R2-COMP 验证失败
  - 0 R1 人工盲审争议
  - 0 业务中断

业务价值（避免）：
  - 22 个无用索引的维护成本
  - R3+ 核心表误修改的潜在事故
  - 团队对"找问题机器"的疲劳
```

---

## 相关 ADR

- ADR-019: Table Refactoring Expert Skill v1.0 冻结决策（NO-CHANGE 是 v1.0 决策模式之一）
- ADR-020: R2-COMP 独立 AI 验证（R2-COMP 验证了 NO-CHANGE 决策的一致性）
- ADR-021: Triple-Key Iron Law（NO-CHANGE 同样适用于 Triple-Key 已覆盖的表）

## 相关资产

- `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md` §3.5 NO-CHANGE 主动判断
- `docs/architecture/v52/database-modernization/JNPF-表级重构-技术变更目录.md` §4 NO-CHANGE 表目录
- `docs/architecture/v52/database-modernization/JNPF-表级重构-管理层报告.md` NO-CHANGE 价值包装



