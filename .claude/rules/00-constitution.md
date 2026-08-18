# 宪法摘要与规则加载策略

> Cursor：`.cursor/rules/00-constitution.mdc`（**唯一** alwaysApply）  
> 分层目录：`iron-laws/` · `domain/` · `frontend/` · `toolchain/` · `docs/` — 见 `.cursor/rules/README.md`

## ADF 写入锁（L12）

`.claude/workflow-state.json`：`adfPhase` = `null` | `P0`–`P3` | `P4` | `exempt`  
（设 `currentSg` 且未给 phase → 视为 P0）

## 四支柱硬门

`awaitingNodeApproval` / `currentSg` → `pillar-claim-current.json` → `pillar-claim-check.mjs --force`
