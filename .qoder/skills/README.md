# Qoder 项目级 Skills 入口

> **本文件性质**：Qoder IDE 项目级 Skills 目录入口。**Qoder Skills 必须放在 `.qoder/skills/{skill-name}/SKILL.md` 格式**（Qoder 官方约定）。
>
> **本目录为空的原因**：项目已有 3 套等价的 skills（`.agents/skills/` 14 个 + `.cursor/skills/` 26 个 + `.trae/skills/` 7 个），为避免重复维护，Qoder 通过系统级 `Skill tool` 调用同名 skill，**项目级同名 skill 内容通过 Read 工具读取 `SKILL.md`**。
>
> **如需新增 Qoder 专属 Skill**：在此目录创建 `{skill-name}/SKILL.md`，Qoder 自动识别。

---

## 1. 现有项目 Skills 总览

### 1.1 `.agents/skills/`（14 个 superpowers 标准 skills）

| Skill | 用途 |
|-------|------|
| `using-superpowers` | 必须最先调用 — 介绍如何使用 skills |
| `brainstorming` | 创造性工作前的需求澄清 |
| `writing-plans` | 多步骤任务的实施计划 |
| `executing-plans` | 按计划执行（带 review 检查点）|
| `subagent-driven-development` | 并行独立任务调度 |
| `dispatching-parallel-agents` | 2+ 独立任务并行 |
| `test-driven-development` | TDD 流程 |
| `systematic-debugging` | Bug 诊断根因 |
| `verification-before-completion` | 完成前验证 |
| `writing-skills` | 创建新 skills |
| `using-git-worktrees` | 隔离工作空间 |
| `requesting-code-review` | 完成时请求评审 |
| `receiving-code-review` | 接收评审反馈 |
| `finishing-a-development-branch` | 完成时合并/PR/清理 |

### 1.2 `.cursor/skills/`（26 个，含项目专属 skills）

**通用 superpowers 子集**（与 `.agents/skills/` 重叠）：
- `using-superpowers` / `brainstorming` / `writing-plans` / `executing-plans` / `subagent-driven-development` / `dispatching-parallel-agents` / `test-driven-development` / `systematic-debugging` / `verification-before-completion` / `writing-skills` / `using-git-worktrees` / `requesting-code-review` / `receiving-code-review` / `finishing-a-development-branch`

**项目专属 skills（推荐读取 SKILL.md 了解）**：
- `agent-architecture-audit` — Agent 架构审计
- `architecture-doc` — 架构文档撰写
- `code-reviewer` — 代码审查
- **`dotnet-patterns` ⭐** — .NET 项目模式（必读）
- **`jnpf-api-cli` ⭐** — JNPF API CLI 工具（必读）
- `openspec-apply-change` / `openspec-archive-change` / `openspec-explore` / `openspec-propose` — OpenSpec 规范变更
- `production-audit` — 生产审计
- `prompt-optimizer` — Prompt 优化
- `rules-distill` — 规则蒸馏
- `skill-scout` — Skill 发现
- `skill-stocktake` — Skill 盘点

### 1.3 `.trae/skills/`（7 个 Trae 专属）

| Skill | 用途 |
|-------|------|
| `full-review` | 完整审查 |
| `learn` | 学习辅助 |
| `pre-commit` | 提交前检查 |
| `security-review` | 安全审查 |
| `spec` | 规范管理 |
| `start-dev` | 启动开发 |
| `trace-bug` | 追踪 Bug |

---

## 2. Qoder 调用 Skills 的方式

### 2.1 通过系统级 Skill tool（推荐）

```text
Skill(brainstorming)         → 创造性工作前必调
Skill(writing-plans)         → 多步骤任务必调
Skill(test-driven-development) → 写代码前必调
Skill(verification-before-completion) → 完成前必调
Skill(systematic-debugging)  → 遇到 Bug 必调
Skill(subagent-driven-development) → 并行任务必调
```

### 2.2 通过 Read 读取项目级 SKILL.md

```text
Read(.cursor/skills/dotnet-patterns/SKILL.md)   → .NET 模式约束
Read(.cursor/skills/jnpf-api-cli/SKILL.md)      → JNPF API 调用方式
Read(.cursor/skills/production-audit/SKILL.md)  → 生产审计清单
```

---

## 3. 何时新增 Qoder 专属 Skill

在以下情况下，应在 `.qoder/skills/{skill-name}/SKILL.md` 创建 Qoder 专属 skill：

1. **Qoder 工作流特殊约束** — 例如 Qoder Quest / Canvas / Vercel Deploy 的特定流程
2. **Qoder 用户级 plugin 集成** — 例如 `better-harness` / `design-review` 的调用约定
3. **Qoder MCP 高级用法** — 例如 `genui` 渲染、playwright 自动化模式

---

> **维护纪律**：本 README 是入口导航，不复制 SKILL.md 内容。Skill 内容变更以源目录为准。Qoder 与 `.agents/skills` 共享 superpowers 标准实现时，**勿复制粘贴** — 通过 Read 按需加载。