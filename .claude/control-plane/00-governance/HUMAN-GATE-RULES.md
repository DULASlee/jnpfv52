# Human Gate Rules — 人工介入规则

> **目的：** 定义何时必须暂停并请求人工决策
> 
> **关键原则：** 普通实现问题必须进入 Self-Repair，不触发 Human Gate

---

## Human Gate 定义

### H1 — 架构方向冲突 (Architecture Conflict)

**触发条件：**

| 条件 | 说明 |
|------|------|
| `architecture_conflict` | 新设计与 Frozen Architecture 冲突 |
| `layer_boundary_dispute` | 跨 Layer 职责无法确定 |
| `section_boundary_dispute` | Section 之间职责边界无法确定 |

**Action:** `PAUSE`

**Resolution:** 架构评审（Architecture Council）

**示例：**
```
新设计要求 Runtime.Core 依赖 Capability Layer
    ↓
架构冲突
    ↓
H1 触发
```

---

### H2 — 需求语义冲突 (Requirement Conflict)

**触发条件：**

| 条件 | 说明 |
|------|------|
| `requirement_conflict` | 无法从现有需求、Specification、ADR 中消解的语义冲突 |
| `spec_contradiction` | 两个 Specification 之间存在无法调和的矛盾 |
| `user_story_inconsistency` | 用户故事之间存在语义不一致 |

**Action:** `PAUSE`

**Resolution:** 需求澄清（Requirement Clarification）

**注意：** `frozen_contract_violation` 属于 H3，不属于 H2。

**示例：**
```
需求 A 要求"实时同步"
需求 B 要求"最终一致性"
    ↓
无法消解的语义冲突
    ↓
H2 触发
```

---

### H3 — Breaking Change (Contract / Breaking Change)

**触发条件：**

| 条件 | 说明 |
|------|------|
| `public_api_breaking_change` | Public API Breaking Change |
| `database_contract_breaking_change` | Database Contract Breaking Change |
| `protocol_breaking_change` | Protocol Breaking Change |
| `frozen_contract_violation` | Frozen Contract 修改 |
| `breaking_schema_change` | Schema 破坏性变更 |

**Action:** `PAUSE + CHANGE_REQUEST`

**Resolution:** Change Request 审批

**示例：**
```
修改已冻结的 API 接口签名
    ↓
Frozen Contract Violation
    ↓
H3 触发
    ↓
必须提交 Change Request
```

---

### H4 — 跨 Section 架构决策 (Cross-Section Architecture Decision)

**触发条件：**

| 条件 | 说明 |
|------|------|
| `section_8_section_9_boundary` | Section 8 ↔ Section 9 边界争议 |
| `runtime_capability_boundary` | Runtime ↔ Capability 边界争议 |
| `core_intelligence_boundary` | Core ↔ Intelligence 边界争议 |

**Action:** `PAUSE`

**Resolution:** 架构评审（Architecture Council）

**示例：**
```
新需求要求将 Capability 注入 Runtime.Core
    ↓
Runtime ↔ Capability 边界争议
    ↓
H4 触发
```

---

### H5 — 安全 / 数据 / 生产风险 (Security / Data / Production Risk)

**触发条件：**

| 条件 | 说明 |
|------|------|
| `security_boundary_breach` | Security Boundary 突破 |
| `data_loss_risk` | Data Loss 风险 |
| `destructive_migration` | 破坏性 Migration |
| `production_behavior_change` | 生产行为变更 |
| `compatibility_risk` | 重大兼容性风险 |

**Action:** `EMERGENCY_PAUSE`

**Resolution:** 立即升级（Immediate Escalation）

**示例：**
```
Migration 脚本可能删除生产数据
    ↓
Data Loss 风险
    ↓
H5 触发
    ↓
立即暂停所有操作
```

---

## Human Gate 决策矩阵

| Gate | Action | Resolution | 典型场景 |
|------|--------|------------|---------|
| H1 | PAUSE | 架构评审 | Runtime ↔ Capability 冲突 |
| H2 | PAUSE | 需求澄清 | 需求 A vs 需求 B 语义冲突 |
| H3 | PAUSE + CR | Change Request | 冻结 API 修改 |
| H4 | PAUSE | 架构评审 | Section 8 ↔ Section 9 边界 |
| H5 | EMERGENCY | 立即升级 | 数据丢失风险 |

---

## 非 Human Gate 场景

**以下情况不是 Human Gate，必须进入 Self-Repair：**

| 场景 | 正确处理 |
|------|---------|
| 编译错误 | Self-Repair |
| 测试失败 | Self-Repair |
| 依赖缺失 | Self-Repair |
| API 调整 | Self-Repair（如果是 breaking 则触发 H3）|
| 内部重构 | Self-Repair |
| 普通设计优化 | Self-Repair |
| 测试补充 | Self-Repair |
| 文档补充 | Self-Repair |
| Code Review 发现问题 | Self-Repair |
| Adversarial Review 发现问题 | Self-Repair |

---

## 冲突处理

当多个 Gate 同时触发时：

1. **H5 优先** — 任何 H5 触发立即暂停所有其他操作
2. **H3 其次** — Breaking Change 需要 Change Request
3. **H1/H4 并行** — 架构决策可合并评审
4. **H2 最后** — 需求冲突通常可以协商解决

---

## 维护规则

1. Gate 条件列表是穷尽的，新增条件必须添加到对应 Gate
2. Gate 条件之间不得重叠（重叠部分归入更高优先级 Gate）
3. H3 与 H2 的区别：`frozen_contract_violation` 属于 H3

---

## 关联文档

- `HUMAN-GATE-RULES.yaml` — 机器可读版本
- `GOVERNANCE-INDEX.md` — 完整规则映射
