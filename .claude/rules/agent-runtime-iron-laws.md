# Agent Runtime 项目工作铁律（HIP-01 完整定义）

> **本文件位置：** `.claude/rules/`（项目配置，与 hook / command / skills / MCP 对齐）。
> **本文件性质：** **项目级**工作铁律 — 适用于所有项目相关工作，不仅限于 Agent Runtime。
> **不包含：** 具体工作的纪律（如 Agent 封装 / Runtime 实施的纪律），这些位于 `docs/构建AI软件工程agent闭环体系/` 目录。
> **配套文件：** [`.claude/rules/workflow-iron-law.md`](./workflow-iron-law.md)（WORKFLOW-IRON-01 完整定义）
> **生效日期：** 2026-08-30 · **永久生效**

---

## 本文件范围（明确边界）

本文件**包含 HIP-01 完整定义**（项目工作铁律 — 节奏层）。

| 铁律 | 位置 | 性质 |
|------|------|------|
| **HIP-01** Human Interrupt Policy | **本文件** | 节奏层（防止人类审批过细） |
| **WORKFLOW-IRON-01** Autonomous Engineering Execution Loop | [`.claude/rules/workflow-iron-law.md`](./workflow-iron-law.md) | 执行层（保证工程质量） |
| **IRON-01~12** Implementation Iron Laws | [`docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md`](../../docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md) | 设计层（防止 AI 削弱能力） |

**任何项目相关工作必须同时遵守**这 3 层铁律。

---

## Human Interrupt Policy（HIP-01）— 完整定义

### 核心思想

**默认连续执行 + 阶段性汇报 + 仅 4 类情况主动请求人类决策**。

防止人类审批粒度过细导致 Agent 无法连续工作；同时保证关键治理节点有人类控制。

### 连续工作原则

AI 工程师必须保持连续推进能力。

**标准流程：**

```
理解任务
   ↓
选择 Superpowers
   ↓
执行工作
   ↓
自动评估
   ↓
自动测试
   ↓
自动修复
   ↓
Reviewer 审查
   ↓
阶段汇报
```

**禁止：**

```
❌ 每完成一个小步骤立即请求人工确认
❌ 每发现普通问题立即暂停
❌ 将内部执行过程拆成大量人工审批节点
❌ 用频繁汇报替代工程验证
```

### 4 类必须暂停请求人类决策的情况

**A. 架构级决策冲突**

- 需要修改已冻结 baseline
- 需要改变已 LOCKED Decision
- 出现两个不可兼容的架构方向

**B. 范围边界变化**

- 需要扩大或缩小 Phase Scope
- 需要引入原计划之外的重要组件
- 需要改变阶段目标

**C. 高风险不可逆操作**

- 删除已有设计资产
- 修改公共契约
- 破坏向后兼容性
- 大规模重构目录 / 模块边界

**D. 发现重大风险**

- 当前方案无法满足 Iron Laws
- 会导致 Agent Runtime 能力退化
- 会导致未来无法扩展

除此之外：**AI 必须通过 Superpowers 工作流自主解决**。

### 不暂停的情况

- 文档章节完成
- 普通接口细化
- 非关键措辞调整
- 格式优化
- 内部一致性检查通过
- 已明确决策下的展开设计
- 不影响架构方向的小调整

这些内容统一放入**阶段性工作汇报**。

### 汇报节奏（6 要素）

默认：

```
完成一个完整 Section 或重大里程碑后汇报
```

汇报格式（6 要素）：

```
1. 做了什么（事实）
2. 发现什么（洞察）
3. 判断影响（专业判断）
4. 下一步计划
5. 是否需要决策
6. 核心原则
```

### 与 Phase Exit Gate 的关系

- HIP-01 不替代 Phase Exit Gate（PV-3）
- HIP-01 仅调整**审批节奏**，不改变**审批节点**
- Phase Exit Gate 仍必须遵守（Section 8-12 Exit Gate 6 项）

---

## 配套铁律（不重复定义，仅引用）

| 铁律 | 完整定义位置 |
|------|-------------|
| **WORKFLOW-IRON-01** | [`.claude/rules/workflow-iron-law.md`](./workflow-iron-law.md) |
| **IRON-01~12**（Agent 实施纪律） | [`docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md`](../../docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md) |
| **Git / Hooks / E2E 等** | [`AGENTS.md`](../../AGENTS.md) |

---

> **维护纪律**：HIP-01 修订需同步更新 AGENTS.md + `.cursor/rules/` 镜像 + baseline 决策表。
> **下次更新触发**：HIP-01 新增或修订。
