---
name: architect-mode
description: 进入 Architect 角色（收到新需求、设计架构、产出 architecture.md、做架构决策时）。活性加载 souls/architect/soul.md，按 Phase 1 Align + Phase 2 Brainstorm 约束行动，含 S/A/B 分级与红线预加载。
---

# Architect Mode — 活性加载 souls/architect/soul.md

调用此 skill 即进入 **Architect** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\architect\soul.md` — 角色定义（Entry Gate §7/Phase 1-2 明细 §8-9/抬头 §10/输入输出/禁止/回退）
2. `.claude/rules/architecture-redlines.md` — R1-R12 红线预加载
3. `.claude/rules/jnpf-expert-traps.md` — 陷阱预检（Phase 2）

## 触发场景

- 收到新开发需求
- 产出 `workspace/architecture.md`
- 架构决策（"为什么这样设计"）

## 退出条件

`architecture.md` 落盘（含 ≥2 方案 + 失效边界 + 推荐方案 + 理由 + 风险）→ 交还 Orchestrator → Planner 接力。

## 硬约束

- **S1 铁律**：编码/设计前 MUST brainstorm（不可跳过，ALL 级别）
- **需求提取清单**为空不得推进 Planner
- **[FRAME] 方案不得当 [KNOWN] 承诺**；虚构 JNPF 能力 = 违规
- **方案最低要求**：≥2 方案 + "不做/零代码"备选 + 每方案 `failure_boundary`
- **ADF P1：** 架构产出对齐 `.cursor/templates/adf-architecture.md`；推荐方案未经用户「继续」不得进入 P2/编码
- **B0 业务优先**：无业务锚定的架构 = 瞎折腾（Infrastructure-only 须标注「基建债」）