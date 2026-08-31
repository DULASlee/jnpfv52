# AI Engineering Control Plane v1.1

> JNPF v5.2 AI 工程师工程治理基础设施
> 
> **生效日期：** 2026-08-31 · **永久生效**

---

## 核心组件

| 目录 | 职责 | 状态 |
|------|------|------|
| [00-governance](./00-governance/) | L0/L1/L2 三级铁律 + Human Gate | ✅ |
| [01-workflows](./01-workflows/) | Autonomous Multi-Phase Engineering Workflow | ✅ |
| [02-rules](./02-rules/) | 专项规则索引 | ✅ |
| [03-skills](./03-skills/) | Engineering Control Skills | ✅ |
| [04-templates](./04-templates/) | 可执行模板 | ✅ |
| [05-gates](./05-gates/) | Phase Gate 协议 | ✅ |
| [06-orchestrator](./06-orchestrator/) | Phase 编排器 | ✅ |
| [07-skill-routing](./07-skill-routing/) | 技能路由矩阵 | ✅ |
| [08-phase-contracts](./08-phase-contracts/) | Phase Contract Registry | ✅ |
| [09-evidence](./09-evidence/) | Evidence Chain | ✅ |
| [10-dry-run](./10-dry-run/) | 验证报告 | ✅ 13/13 PASS |

---

## 执行链

```
User Request
    ↓
AGENTS.md (入口)
    ↓
Control Plane README
    ↓
Governance (L0/L1/L2 + Human Gate)
    ↓
Skill Routing (自动加载)
    ↓
Phase Orchestrator
    ↓
Autonomous Engineering Loop
    ↓
Verification / Gate
    ↓
Continue / Human Gate
```

---

## 加载顺序

1. `AGENTS.md` (入口)
2. `control-plane/README.md` (本文件)
3. `control-plane/INDEX.md` (详细索引)
4. `control-plane/00-governance/MASTER-GOVERNANCE.md` (主控)
5. `control-plane/00-governance/GOVERNANCE-INDEX.md` (规则映射索引)
6. `control-plane/00-governance/L0-LAWS.md` (L0 宪法索引)
7. `control-plane/06-orchestrator/phase-state.yaml` (当前 Phase 状态)
8. 相关 Skills + Templates + Gates

---

## 关键设计原则

### 1. Single Source of Truth

现有 Rules 永不复制，只建索引。

```
Existing Rule (.claude/rules/*.md)
    ↓
Governance Index
    ↓
Classification (L0/L1/L2)
    ↓
Routing
```

### 2. 双 Profile TDD

由 Phase Contract 决定：

```yaml
testingProfile: STRICT-TDD    # RED → GREEN → REFACTOR
testingProfile: CONTRACT-FIRST-TDD  # Contract → Test Matrix → Implementation → Verification
```

### 3. Engineering Control Skills

专注 orchestration/governance，复用现有 Skills。

### 4. Machine-readable State

Orchestrator + Phase State YAML 驱动执行。

### 5. Evidence Chain 一等公民

```
Requirement → Design → Implementation → Test → Verification → Evidence → Gate
```

---

## Human Gate (H1-H5)

| ID | 名称 | 触发条件 | Action |
|----|------|---------|--------|
| H1 | 架构冲突 | 新设计与 Frozen Architecture 冲突；跨 Layer 职责无法确定 | PAUSE |
| H2 | 需求冲突 | 无法从现有需求、Specification、ADR 中消解的语义冲突 | PAUSE |
| H3 | Breaking Change | Public API / Database / Protocol breaking change；Frozen Contract 修改 | PAUSE + CR |
| H4 | 跨 Section 决策 | Section 8 ↔ Section 9 边界争议 | PAUSE |
| H5 | 安全/数据风险 | Security Boundary / Data Loss / Production Behavior | EMERGENCY_PAUSE |

---

## 持续维护

Control Plane 本身也必须遵守自己的 Autonomous Multi-Phase Engineering Workflow。

任何后续修改：
```
Control Plane v1.1
→ Phase Contract
→ Design
→ Implementation
→ Review
→ Adversarial Review
→ Verification
→ Gate
→ v1.2
```

---

## 关联文档

- `AGENTS.md` — 主入口
- `.claude/rules/*.md` — 现有 Rules (Single Source of Truth)
- `.agents/skills/*/SKILL.md` — 现有 Superpowers Skills
- `docs/构建AI软件工程agent闭环体系/UEEA-Agent-Runtime-Engineering-Rules.md` — 完整定义
