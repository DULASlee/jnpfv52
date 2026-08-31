# Main Task ambiguity: 3 candidates pending user choice (2026-08-31)

## 背景

用户指令："继续推进主线任务"。但"主线任务"未明确。Phase 8 状态有内部矛盾。

## 发现的矛盾

- phase-gate-state.md: PHASE 8 CLOSED, 248/274 = 90.5%, Skill v1.0 FROZEN
- Production-Progress-Ledger.md: 实际 40/274 = 14.6% EXECUTED, 53 PREPARED
- MASTER-JNPF 计划: 当前 S0+S1 起步

## 三个候选解释

### A. 继续 Phase 8 表级重构
- 53 PREPARED → EXECUTED
- 用 Skill v1.0-FROZEN（不是 v2.0）
- 需要 Chief Architect 批准

### B. 推进 MASTER-JNPF 主线（Aspire）
- S0 → S1 → 第二圈 → ... → Aspire 注册
- Phase 8 只是其中一个 circle
- S0+S1 当前都是"起步"

### C. 修复 Skill v2.0 缺陷
- DoD-01/02 crash
- DoD-05/06 placeholder
- V6 R1 真实人类审查
- 然后 FROZEN Skill v2.0

## 关键关联

- mem_20260829_534b568991cd4c0e8459: Phase 8 Status Snapshot
- mem_20260830_aee2009f742e41f98c17: Skill v2.0 NOT 彻底实现
- mem_20260830_075582b836fc4940aed3: 18 canonical 文档恢复
- mem_20260830_de1019f1699a45f3abe2: AGENTS.md v4 final-correct
