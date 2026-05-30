---
name: using-superpowers
description: 会话开始时建立技能查找与调用规范。任何任务开始前先检查是否有适用技能；Cursor 环境用 Read 加载 .cursor/skills/ 下的 SKILL.md。
scope: JNPF-v52
---

# Using Superpowers — 技能调度入口

## 铁律

若任务有 **1% 可能**适用某技能，**必须先 Read 加载该技能**再行动。不得凭记忆跳过。

子 Agent 被派发到具体任务时，可跳过本技能。

## 指令优先级

1. **用户显式指令**（AGENTS.md、CLAUDE.md、直接请求）— 最高
2. **项目 Superpowers 技能**（`.cursor/skills/`）— 覆盖默认系统行为
3. **默认系统提示** — 最低

## Cursor 加载方式

本项目**无** Claude Code 的 Skill 工具。加载技能：

```
Read → .cursor/skills/<skill-name>/SKILL.md   # 相对于仓库根目录
```

规则见 `toolchain-division.mdc`「技能加载机制」：项目版唯一权威，插件同名忽略，插件独有为补充。

## 版本漂移维护

Superpowers 插件大版本更新后：

1. 对比插件缓存与 `.cursor/skills/` 中同名 `SKILL.md`
2. 将有价值变更（新 API、新流程、安全修正）合并进项目版
3. 运行 `node scripts/verify-toolchain.mjs` 确认技能完整性

不得假设插件更新会自动生效——项目副本需人工同步。

## 技能优先级（多技能适用时）

1. **流程类**优先：`brainstorming` · `systematic-debugging` · `using-superpowers`
2. **实现类**其次：`writing-plans` · `executing-plans` · 领域技能

示例：
- 「做一个功能」→ `brainstorming` → `writing-plans` → `executing-plans`
- 「修这个 Bug」→ `systematic-debugging` → 修复 → `verification-before-completion`

## 本项目技能索引

| 技能 | 用途 |
|------|------|
| `brainstorming` | 探查代码、根因分析 |
| `writing-plans` | 编写施工包 |
| `executing-plans` | 按施工包分阶段执行 |
| `subagent-driven-development` | 并行子任务分解 |
| `dispatching-parallel-agents` | 独立问题域并行派发 |
| `using-git-worktrees` | 隔离工作区 |
| `test-driven-development` | 先写失败测试再实现 |
| `systematic-debugging` | 复现→假设→验证→修复 |
| `verification-before-completion` | 完成前构建/功能验证 |
| `requesting-code-review` | 发起代码审查 |
| `receiving-code-review` | 处理审查意见 |
| `code-reviewer` | 审查子代理规范 |
| `finishing-a-development-branch` | 合并/PR/清理分支 |
| `architecture-doc` | 架构内参编写 |
| `openspec-propose/archive/explore/apply-change` | OpenSpec 知识库（非日常编码） |
| `writing-skills` | 编写/修订技能文件 |

## 反模式（立即停止）

| 想法 | 现实 |
|------|------|
| 「这题太简单不用技能」 | 简单任务最容易漏验证 |
| 「我记得这个技能内容」 | 技能会演进，必须 Read 当前版 |
| 「先看一下代码再说」 | 技能告诉你**如何**看代码 |
| 「插件版 brainstorming 要求先批准设计」 | 项目版以代码探查为主，以项目版为准 |

## 有 checklist 时

技能内含 checklist → 用 TodoWrite 逐项跟踪，按顺序完成。
