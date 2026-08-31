# Orchestration Skill

> **类型：** Engineering Control Skill
> 
> **定位：** 专注 orchestration/governance，复用现有 Skills
> 
> **版本：** v1.1

---

## 概述

Orchestration Skill 负责协调整个 Phase 的执行流程，确保遵循 Autonomous Multi-Phase Engineering Workflow。

## 输入

- Task Input（用户需求）
- Phase Contract
- Skill Routing

## 流程

### 1. 加载 Phase Contract

读取当前 Phase Contract：
- Objective
- Scope
- Non-Scope
- Testing Profile
- Human Gates

### 2. Skill Routing

根据任务类型和上下文，加载所需的 Skills

### 3. Phase Orchestration

协调 Phase 的执行：
```
Phase 0: Discovery
Phase 1: Requirement Analysis
Phase 2: Design Specification
...
```

### 4. 状态跟踪

维护 Phase 状态

### 5. Gate 决策

根据 Phase 结果做出 Gate 决策

## 关联文档

- `07-skill-routing/ROUTING-MATRIX.md`
- `06-orchestrator/phase-state.yaml`
- `04-templates/PHASE-CONTRACT.md`
