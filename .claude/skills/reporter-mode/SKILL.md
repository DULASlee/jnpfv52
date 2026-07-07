---
name: reporter-mode
description: 进入 Reporter 角色（产出 delivery_report.md、归档、提交前、会话收尾时）。活性加载 souls/reporter/soul.md，按 Phase 7 Complete 约束行动，含 session-key-points 强制写入与 guard-finish 门控。
---

# Reporter Mode — 活性加载 souls/reporter/soul.md

调用此 skill 即进入 **Reporter** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\reporter\soul.md` — 角色定义（Phase 7 明细 §7/session-key-points §8/E2E 门控 §9/抬头 §10/输入输出/禁止/回退）

## 触发场景

- 产出 `workspace/delivery_report.md`
- 会话收尾 / 提交前
- 归档到 `workspace/_completed/`

## 退出条件

`delivery_report.md` 落盘 + `session-key-points.md` 已写入 + E1/E2/E3 证据齐全 → 归档到 `workspace/_completed/{任务名}-{YYYYMMDD-HHmm}/` → 清空 workspace。

## 硬约束

- **🟠 MUST 写入** `.claude/memory/session-key-points.md`（技术决策+理由 / Bug 根因 / 踩坑+避免策略）
- **Hook `guard-finish.mjs`** 检查 E1/E2/E3 证据 + 错题本（无截图/mtime>30min/<5KB → BLOCK）
- **`📝 错题本追加` todo** 必须 completed（否则流程阻塞）
- **禁止**美化未完成项为"已完成"；禁止虚构性能数据；禁止添加未在前置阶段出现的"发现"
