---
name: ecc-harness-setup
description: ECC Memory Vault 启用 + 7 个 ECC 技能三-harness 安装 (opencode/claude/cursor)
metadata:
  type: project
  date: 2026-08-28
---

# ECC Harness Setup — 2026-08-28

## 摘要
本项目启用 **ECC Memory Vault** 作为跨 harness 共享记忆层，并从 `ecc-universal@2.2.0` 技能目录安装 7 个技能到三个活跃 harness（OpenCode / Claude Code / Cursor）。三"可选"技能经评估后判定不装。

## 安装动作（[KNOWN] 高置信）

### 运行时
- `npm install -g ecc-universal@2.2.0` → 全局 `AppData\Roaming\npm`（ecc / ecc-memory / ecc-memory-mcp / ecc-control-pane / ecc-plan-canvas）
- `ecc memory init --scope project` → vault 生成于 `.ecc/memory/project/{contexts,decisions,facts,handoffs,lessons,notes,preferences,runbooks}` + fail-closed `.gitignore`（**不入 git**）
- `ecc memory doctor`：`ok:true`，记忆可读写（已存 v6.0 Sprint 闭合记忆 `mem_20260828_3c5c59cd4fa345169aec`）

### 技能安装（7 × 3 harness = 21 份，全部新目录零覆盖）
直接复制技能目录（注：`ecc-install --skills` 只认注册表白名单，这 7 个不在 register；正确装法=复制到各 harness skills 目录）：

| 技能 | 用途 | 落点 |
|------|------|------|
| production-audit | 生产就绪/合并前审计（本地证据不外发） | 三 harness |
| agent-architecture-audit | 12 层 agent 栈诊断（wrapper 回归/记忆污染/工具纪律） | 三 harness |
| rules-distill | 技能→跨技能原则蒸馏进 rules（含 scan-rules.sh/scan-skills.sh） | 三 harness |
| skill-stocktake | skills/commands 库存质量审计（Quick/Full） | 三 harness |
| skill-scout | 建新技能前查重（本地/市场/GitHub） | 三 harness |
| prompt-optimizer | prompt 优化 + ECC 组件匹配 | 三 harness |
| dotnet-patterns | C#/.NET 惯例（DI/async 等） | 三 harness |

落点：`~/.config/opencode/skills/`、`.claude/skills/`、`.cursor/skills/`（21 份均含 SKILL.md，已验证）。

### 已评估不装（含依据，防未来误翻）
- **token-budget-advisor** ❌：纯启发式无真实 tokenizer（±15%），与 R1 否决的"伪精确"同源；主动打断流每次提问多一层选择框。
- **parallel-execution-optimizer** ❌：纯提示词无执行机制；与仓库已有 `dispatching-parallel-agents` 重复且双源风险。
- **strategic-compact** ⚠️：策略（压缩时机表）可复用，但其 hook 面向 Claude Code transcript/settings，本仓库 OpenCode 运行时下不生效——如需要，以规则而非 hook 的方式转录压缩时机表。

## 决策与影响
| 决策 | 影响 |
|------|------|
| ECC vault 作跨 harness 共享层 | 与 `.claude/memory/` 项目知识库并存，前者 unreviewed context 不入 git，后者随仓库版本 |
| 7 技能三 harness 落地 | .claude/skills 与 .cursor/skills 下新增技能将随 git 共享团队 |
| 三个可选技能不装 | 避免伪精确/重复/运行时错配 |

## 证据索引
- vault：`ecc memory search "ECC"`（project scope）
- 技能源：`npm root -g\ecc-universal\skills\`
- 复验：`ecc memory doctor --scope project`