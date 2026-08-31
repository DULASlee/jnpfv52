# L2 — Phase Rules（Phase 级规则）

> **层级：** 针对特定 Phase 或 Section 的临时约束
> 
> **执行层：** L2 约定
> 
> **来源：** 所有 L2 规则的内容存储在对应 Section 文档中

---

## Section 8 Runtime

### L2-01: Runtime.Core 不依赖 Capability

**来源：** Section 8 架构规范

**规则：** Runtime.Core 层不得依赖 Capability Layer

**强制要求：**
- 层级边界严格遵守
- 依赖方向只能是 Capability → Runtime

---

### L2-02: Execution Boundary 不携带 Intelligence

**来源：** Section 8 架构规范

**规则：** Execution Boundary 不得携带 Intelligence 逻辑

**强制要求：**
- Execution 只负责执行
- Intelligence 必须在上层（Capability）

---

### L2-03: Lifecycle Contract Frozen

**来源：** Section 8 架构规范

**规则：** 生命周期契约不可变

**强制要求：**
- Lifecycle 接口不得随意修改
- 修改必须经过架构评审

---

## Section 9 Integration

### L2-10: Capability Layer 冻结

**来源：** Section 9 架构规范

**规则：** Capability Layer 边界约束

**强制要求：**
- Capability 定义必须稳定
- 新增 Capability 必须通过 Gate

---

## AI 原生开发

### L2-20: S0-S2 门控

**来源：** AI原生开发/1-3

**规则：** 需求分析子链铁律

**强制要求：**
- S0 门控 → 拦截/通过
- S1 PM → skeleton.md
- S2 Analyst → requirement-spec.md

---

### L2-21: Phase 契约硬化

**来源：** Section 9 架构规范

**规则：** Contract Hardening

**强制要求：**
- 契约一旦签署不得随意修改
- 修改必须经过 CR 流程

---

## Phase 级规则管理

### 创建新 Phase 规则

1. 在对应 Section 文档中定义规则
2. 在 GOVERNANCE-INDEX.md 中注册
3. 在 phase-state.yaml 中激活

### 规则状态

| 状态 | 说明 |
|------|------|
| ACTIVE | 当前 Phase 生效 |
| DEPRECATED | 即将废弃 |
| ARCHIVED | 已归档 |

---

## 关联文档

- `GOVERNANCE-INDEX.md` — 完整规则映射表
- `08-phase-contracts/` — Phase Contract Registry
