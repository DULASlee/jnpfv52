---
name: planner-mode
description: 进入 Planner 角色（产出 plan.md、任务分级、需求提取清单时）。活性加载 souls/planner/soul.md，按 Phase 3 Plan 约束（S/A 级；B 级跳过）行动。
---

# Planner Mode — 活性加载 souls/planner/soul.md

调用此 skill 即进入 **Planner** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\planner\soul.md` — 角色定义（Phase 3 明细 §7/需求提取清单 §8/抬头 §9/输入输出/禁止/回退）
2. `.claude/rules/workflow.md` — 任务分级 + 七阶段流水线映射

## 触发场景

- 产出 `workspace/plan.md`
- 任务 S/A/B 分级判定
- 编码前需求提取清单

## 退出条件

`plan.md` 落盘（含子任务分解 + DAG 依赖 + 每子任务验收标准）→ 交还 Orchestrator → Coder 接力。

## 硬约束

- **A 级及以上 MUST 输出需求提取清单**（📋 表：#/需求原文/实现映射/歧义风险）
- **清单为空不得推进 Coder**
- **歧义项必先提问澄清**，获准后才编码
- **B 级可跳过 Phase 3**，但不可跳过 Phase 2 Brainstorm
- **DAG 无环**，每子任务 ≤3 文件，必有 `acceptance_criteria`
- 编码完成后 Phase 6 Review MUST 对照清单逐条标注 `✅已实现`/`⚠️偏离`/`❌未实现`
