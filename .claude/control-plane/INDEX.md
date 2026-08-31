# Control Plane Index — 详细索引

> **目的：** 作为 Control Plane 所有模块的导航索引

---

## 目录结构

```
.claude/control-plane/
│
├── README.md                    # 入口概览
├── INDEX.md                    # 本文件，详细索引
│
├── 00-governance/              # Governance Foundation ✅
│   ├── MASTER-GOVERNANCE.md    # 主控文件
│   ├── GOVERNANCE-INDEX.md    # 规则映射表
│   ├── L0-LAWS.md            # L0 宪法索引
│   ├── L1-PROJECT-RULES.md   # L1 项目规则索引
│   ├── L2-PHASE-RULES.md    # L2 Phase 规则索引
│   ├── HUMAN-GATE-RULES.md    # Human Gate 规则
│   └── HUMAN-GATE-RULES.yaml   # Human Gate 机器可读
│
├── 01-workflows/              # 🔄 待创建
│   ├── AUTONOMOUS-MULTI-PHASE-ENGINEERING-WORKFLOW.md
│   ├── PHASE-EXECUTION-PROTOCOL.md
│   ├── DESIGN-TO-IMPLEMENTATION.md
│   ├── TDD-WORKFLOW.md
│   ├── REVIEW-REPAIR-WORKFLOW.md
│   └── VERIFICATION-WORKFLOW.md
│
├── 02-rules/                  # 专项规则索引 ✅
│   ├── ARCHITECTURE-RULES.md
│   ├── CONTRACT-RULES.md
│   ├── TESTING-RULES.md
│   ├── API-FREEZE-RULES.md
│   ├── DEPENDENCY-RULES.md
│   └── ANTI-REGRESSION-RULES.md
│
├── 03-skills/                  # 🔄 待创建
│   ├── orchestration/
│   ├── phase-management/
│   ├── contract-governance/
│   ├── architecture-gate/
│   ├── adversarial-review/
│   ├── self-repair/
│   ├── evidence-collection/
│   └── completion-verification/
│
├── 04-templates/              # 🔄 待创建
│   ├── PHASE-CONTRACT.md
│   ├── DESIGN-SPEC.md
│   ├── IMPLEMENTATION-PLAN.md
│   ├── TEST-MATRIX.md
│   ├── ADR.md
│   ├── API-BASELINE.md
│   ├── EVIDENCE-RECORD.md
│   ├── VERIFICATION-REPORT.md
│   └── PHASE-COMPLETION-REPORT.md
│
├── 05-gates/                   # 🔄 待创建
│   ├── GATE-DESIGN.md
│   ├── GATE-CONTRACT.md
│   ├── GATE-IMPLEMENTATION.md
│   ├── GATE-API-FREEZE.md
│   ├── GATE-ARCHITECTURE.md
│   └── GATE-PHASE-CLOSURE.md
│
├── 06-orchestrator/            # 🔄 待创建
│   ├── ORCHESTRATOR-INDEX.md
│   ├── phase-start.md
│   ├── phase-routing.md
│   ├── gate-evaluation.md
│   ├── evidence-collection.md
│   ├── phase-close.md
│   └── phase-state.yaml
│
├── 07-skill-routing/           # 🔄 待创建
│   ├── ROUTING-MATRIX.md
│   ├── ROUTING-CONFIG.yaml
│   └── ROUTING-RULES.md
│
├── 08-phase-contracts/         # 🔄 待创建
│   └── README.md
│
├── 09-evidence/               # 🔄 待创建
│   └── README.md
│
└── 10-dry-run/                # 🔄 待创建
    └── FULL-DRY-RUN.md
```

---

## 状态总结

| 模块 | 状态 | 说明 |
|------|------|------|
| 00-governance | ✅ 完成 | L0/L1/L2 + Human Gate |
| 01-workflows | ✅ 完成 | 6 个工作流 |
| 02-rules | ✅ 完成 | 6 个专项规则索引 |
| 03-skills | ✅ 完成 | 8 个 Engineering Control Skills |
| 04-templates | ✅ 完成 | 9 个可执行模板 |
| 05-gates | ✅ 完成 | Phase Gate 定义 |
| 06-orchestrator | ✅ 完成 | Orchestrator + phase-state.yaml |
| 07-skill-routing | ✅ 完成 | Routing Matrix + Config |
| 08-phase-contracts | ✅ 完成 | Phase Contract Registry |
| 09-evidence | ✅ 完成 | Evidence Chain |
| 10-dry-run | ✅ 完成 | Full Dry Run 方案 |

---

## 关键文件

### Governance (Package A) ✅

| 文件 | 用途 |
|------|------|
| `GOVERNANCE-INDEX.md` | 规则映射表，Single Source of Truth |
| `L0-LAWS.md` | L0 宪法索引 |
| `HUMAN-GATE-RULES.md` | Human Gate H1-H5 定义 |
| `HUMAN-GATE-RULES.yaml` | Human Gate 机器可读版本 |

### Rules (Package A) ✅

| 文件 | 用途 |
|------|------|
| `ARCHITECTURE-RULES.md` | 架构红线 R1-R12 |
| `CONTRACT-RULES.md` | 契约规则 |
| `TESTING-RULES.md` | 测试规则 + TDD 双 Profile |
| `API-FREEZE-RULES.md` | API Freeze 规则 |
| `DEPENDENCY-RULES.md` | 依赖规则 |
| `ANTI-REGRESSION-RULES.md` | 防退化规则 |

---

## 加载顺序

### 1. AGENTS.md (入口)

### 2. Control Plane 加载

```
Control Plane README
    ↓
MASTER-GOVERNANCE.md
    ↓
GOVERNANCE-INDEX.md (规则映射)
    ↓
L0-LAWS.md + L1-PROJECT-RULES.md + L2-PHASE-RULES.md
    ↓
HUMAN-GATE-RULES.yaml (机器可读)
    ↓
Phase-state.yaml (当前 Phase)
    ↓
Applicable Skills + Templates
```

---

## Human Gate H1-H5

| ID | 名称 | 触发 | Action |
|----|------|------|--------|
| H1 | 架构冲突 | 新设计 vs Frozen Architecture | PAUSE |
| H2 | 需求冲突 | 语义冲突无法消解 | PAUSE |
| H3 | Breaking Change | API/DB/Protocol breaking | PAUSE + CR |
| H4 | 跨 Section | Section 8 ↔ Section 9 | PAUSE |
| H5 | 安全/数据风险 | Security/Data/Production | EMERGENCY |

---

## Package Gate

| Package | Gate | 通过标准 |
|---------|------|---------|
| A: Governance | Governance Consistency | 与现有 Rules 无冲突 |
| B: Workflow | Workflow Completeness | Template 可用，TDD Profile 定义 |
| C: Skill & Routing | Skill Routing Integrity | 5 类场景 routing |
| D: Orchestration | Autonomous Loop | State 可读写 |
| E: Integration | Full Simulation | 5 类 Dry Run |

---

## Package Gate

| Package | Gate | 状态 | 验证文件 |
|---------|------|------|---------|
| A: Governance | Governance Consistency | ✅ PASS | E2-GOVERNANCE-VERIFICATION.md |
| B: Workflow | Workflow Completeness | ✅ PASS | E13-FINAL-AUDIT.md |
| C: Skill & Routing | Skill Routing Integrity | ✅ PASS | E3-SKILL-ROUTING-VERIFICATION.md |
| D: Orchestration | Autonomous Loop | ✅ PASS | E4-ORCHESTRATOR-VERIFICATION.md |
| E: Integration | Full Simulation | ✅ PASS | E1-E13 + FINAL-ACCEPTANCE-REPORT.md |

## Package E 验证结果

| 验证项 | Human Gates | Status |
|--------|-------------|--------|
| E1: IDE Integration | 0 | ✅ |
| E2: Governance Runtime | 0 | ✅ |
| E3: Skill Routing | 0 | ✅ |
| E4: Phase State Machine | 0 | ✅ |
| E5: Evidence Chain | 0 | ✅ |
| E6: New Feature Dry Run | 0 | ✅ |
| E7: Runtime Dry Run | 1 (H3 verified) | ✅ |
| E8: Bug Fix Dry Run | 0 | ✅ |
| E9: Breaking API Dry Run | 1 (H3 PAUSE) | ✅ |
| E10: Refactor Dry Run | 1 (H1) | ✅ |
| E11: Adversarial Attack | 0 | ✅ |
| E12: Self-Repair | 0 | ✅ |
| E13: Final Audit | 0 | ✅ |

**总计: 13/13 PASS**

---

## 最终状态

```
AI Engineering Control Plane v1.1

FOUNDATION            ✅
OPERATIONAL           ✅
AUTONOMOUS DRY RUN   ✅
HUMAN GATE CONTROL    ✅
EVIDENCE CHAIN       ✅
FINAL ACCEPTANCE     ✅

STATUS: CLOSED ✅
MODE: FROZEN 🔒
```

---

## Version History

| Version | Date | Status |
|---------|------|--------|
| v1.1 | 2026-08-31 | FROZEN 🔒 |

---

## AI Engineering Control Plane v1.1 实施完成

所有 Package 已完成实施。

### 下一步

- **Dry Run:** 执行 FULL-DRY-RUN.md 中的 5 类任务验证
- **正式启用:** Control Plane 正式接入 AGENTS.md
