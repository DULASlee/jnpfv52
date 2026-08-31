# Governance Index — 规则映射表

> **目的：** 将现有 29 个 Rules 映射到 L0/L1/L2 三级治理体系，同时保持 Single Source of Truth。
> 
> **原则：** 永不复制 Rule 内容，只建立索引和分类。

---

## 一、L0 — Immutable Engineering Laws（宪法级）

L0 规则是绝对不可违反的工程铁律。

| ID | 规则名称 | 来源 | 分类 | 说明 |
|----|---------|------|------|------|
| L0-01 | Frozen Contract 保护 | `business-first-iron-law.md` | execution | 不得破坏已冻结的 Public Contract |
| L0-02 | 功能完整性 | `implementation-integrity-iron-law.md` | integrity | 不得删除核心功能以换取实现便利 |
| L0-03 | Agent Runtime 保护 | `workflow-iron-law.md` | execution | 不得将 Agent Runtime 退化为 Workflow |
| L0-04 | Capability Boundary | `triple-key-iron-law.md` | architecture | 不得将 Capability / Intelligence 倒灌到 Kernel |
| L0-05 | 测试诚信 | `implementation-integrity-iron-law.md` | integrity | 不得为通过测试修改测试掩盖缺陷 |
| L0-06 | Breaking Change 控制 | `architecture-redlines.md` | contract | Breaking Change 必须经 Human Gate 审批 |
| L0-07 | Evidence-Driven | `workflow-iron-law.md` | evidence | 验证证据优先于"看起来正确" |
| L0-08 | 自主闭环 | `workflow-iron-law.md` | execution | 不得跳过 Implementation → Test → Review → Repair → Verification |
| L0-09 | 三元组完整性 | `triple-key-iron-law.md` | architecture | 所有数据实体必须携带 tenantId/projectId/pipelineId |
| L0-10 | 多租户隔离 | `architecture-redlines.md` R4 | security | 新 SqlSugar 查询必须确保租户过滤生效 |
| L0-11 | SQL 注入防御 | `architecture-redlines.md` R7 | security | 动态 SQL 必须参数化 |
| L0-12 | 前端内存安全 | `architecture-redlines.md` R6 | safety | setTimeout/EventSource 必须遵循 6 条铁律 |
| L0-13 | API 权限声明 | `architecture-redlines.md` R8 | security | 每个 IDynamicApiController 必须声明权限属性 |

---

## 二、L1 — Project Rules（项目级规则）

L1 规则是项目特有的编码规范和最佳实践。

### 2.1 架构设计

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-01 | ADF 三先行 | `architecture-design-interface-first.md` | 架构 → 设计模式 → 接口契约 → 实现 |
| L1-02 | 架构红线 | `architecture-redlines.md` | R1-R12 架构约束清单 |
| L1-03 | 断言纪律 | `assertion-discipline.md` | Tag claims with [KNOWN]/[COMPUTED]/[INFERRED]/[GUESS] |

### 2.2 工作流程

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-10 | WORKFLOW-IRON-01 | `workflow-iron-law.md` | 自主工程闭环 4 环节强制执行 |
| L1-11 | HIP-01 | `agent-runtime-iron-laws.md` | Human Interrupt Policy |
| L1-12 | 工作汇报规范 | `ai-work-report-iron-law.md` | 六维结构化汇报 |
| L1-13 | 节点审批门禁 | `implementation-integrity-iron-law.md` | 每个功能节点必须暂停等待审批 |

### 2.3 需求分析

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-20 | 需求分析铁律 | `req-analysis-iron-law.md` | 阶段 A-B-C 为唯一施工依据 |
| L1-21 | 交互式澄清 | `studio-clarification.md` | 结构化选择题让用户细化需求 |

### 2.4 测试与验证

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-30 | 测试工具链 | `testing-toolchain.md` | E2E 分层工具链 |
| L1-31 | 测试纪律 | `testing.md` | Phase 1 验证测试标准 |
| L1-32 | Review 工作流 | `review-workflow.md` | 代码审查流程 |

### 2.5 调试与修复

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-40 | 系统调试 | `debugging.md` | 系统化调试流程 |
| L1-41 | Reviewer 纪律 | `reviewer-discipline.md` | 独立审查员行为规范 |

### 2.6 Studio 特定

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L1-50 | S2 Compile 主链 | `studio-s2-compile.md` | compile 模式 vs agent 模式边界 |
| L1-51 | Eval Pipeline | `studio-eval-pipeline.md` | 四层评估管线 |
| L1-52 | 阶段验收测试 | `fullchain-sprint-iron-law.md` | F1-F4 全链条冲刺铁律 |

---

## 三、L2 — Phase Rules（Phase 级规则）

L2 规则是针对特定 Phase 或 Section 的临时约束。

### 3.1 Section 8 Runtime

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L2-01 | Runtime.Core 不依赖 Capability | Section 8 架构 | 层级边界约束 |
| L2-02 | Execution Boundary 不携带 Intelligence | Section 8 架构 | 职责边界约束 |
| L2-03 | Lifecycle Contract Frozen | Section 8 架构 | 生命周期契约不可变 |

### 3.2 Section 9 Integration

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L2-10 | Capability Layer 冻结 | Section 9 架构 | 能力层边界约束 |

### 3.3 AI 原生开发

| ID | 规则名称 | 来源 | 说明 |
|----|---------|------|------|
| L2-20 | S0-S2 门控 | AI原生开发/1-3 | 需求分析子链铁律 |
| L2-21 | Phase 契约硬化 | Section 9 架构 | Contract Hardening |

---

## 四、专项规则索引

### 4.1 前端规则

| 来源 | 说明 |
|------|------|
| `jnpf-frontend-rules.md` | JNPF 前端编码规范 |
| `frontend-memory-leak.md` | 内存泄漏防护 |
| `low-code-principles.md` | 低代码设计原则 |

### 4.2 安全规则

| 来源 | 说明 |
|------|------|
| `sql-safety.md` | SQL 安全规范 |
| `architecture-redlines.md` R4/R7/R8 | 安全红线 |

### 4.3 代码质量

| 来源 | 说明 |
|------|------|
| `engineering-laws.md` | 工程铁律 |
| `jnpf-expert-traps.md` | 专家陷阱清单 |
| `mcp-code-search.md` | 代码搜索规范 |
| `needle-search.md` | 针式搜索铁律 |

---

## 五、冲突处理规则

当多个规则冲突时：

1. **优先级：** L0 > L1 > L2
2. **Frozen 优先：** Frozen Contract 优先于 Open Contract
3. **Human Gate 优先：** Human Gate 触发时暂停
4. **业务优先：** Business First Iron Law 凌驾于纯技术决策

---

## 六、维护规则

1. **新增规则** → 追加到本文件对应分类
2. **修改规则** → 修改源文件，本文件自动更新引用
3. **删除规则** → 从本文件移除，本文件永不存储规则内容
4. **L0 变更** → 需要 Human Gate 审批

---

## 七、规则数量统计

| 分类 | 数量 |
|------|------|
| L0 (宪法级) | 13 |
| L1 (项目级) | 32 |
| L2 (Phase 级) | 6 |
| **总计** | **51** |

> 注：部分规则跨越多个分类，以主要分类为准。

---

## 八、验证清单

- [x] 所有 29 个现有 Rules 已映射
- [ ] 无重复定义（内容在源文件，不在本文件）
- [ ] L0/L1/L2 分类无冲突
- [ ] Human Gate 触发条件无歧义
- [ ] 冲突处理规则明确

---

## 九、关联文档

- `L0-LAWS.md` — L0 法则索引
- `L1-PROJECT-RULES.md` — L1 项目规则索引
- `L2-PHASE-RULES.md` — L2 Phase 规则索引
- `HUMAN-GATE-RULES.md` — Human Gate 规则
- `MASTER-GOVERNANCE.md` — 主控文件
