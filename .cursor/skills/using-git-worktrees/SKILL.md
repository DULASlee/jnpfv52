---
name: using-git-worktrees
description: 开始需要与当前工作区隔离的功能开发，或执行施工包前创建独立 git worktree。适用于 JNPF 多模块并行开发。
scope: JNPF-v52
tech-stack: [dotnet, pnpm]
---

# Using Git Worktrees — 隔离工作区

## 适用场景

- 执行 `executing-plans` / `subagent-driven-development` 前需要干净基线
- 当前分支有未提交改动，但需并行开新功能
- 大型重构需与主线隔离验证

## 目录选择（按优先级）

1. 已存在 `.worktrees/` → 使用（优先）
2. 已存在 `worktrees/` → 使用
3. 查 `CLAUDE.md` / `AGENTS.md` 是否有约定
4. 均无 → 询问用户：`.worktrees/`（项目内）或全局目录

## 安全验证（项目内目录必做）

```powershell
git check-ignore -q .worktrees
```

若**未**被 ignore → 立即加入 `.gitignore` 再继续（防止 worktree 内容被误提交）。

## 创建步骤

```powershell
# 1. 确定分支名
$branch = "feature/your-feature"

# 2. 创建 worktree
git worktree add .worktrees/$branch -b $branch
cd .worktrees/$branch

# 3. JNPF 项目初始化
cd backend; dotnet build
cd ..\jnpf-web-vue3; pnpm install

# 4. 验证基线
cd ..\backend; dotnet build
# 前端按需: cd ..\jnpf-web-vue3; pnpm run build
```

## 输出

```
Worktree 就绪：<完整路径>
dotnet build: PASS
可开始实施 <功能名>
```

## 常见错误

| 错误 | 修复 |
|------|------|
| 未验证 .gitignore | 创建前必须 `git check-ignore` |
| 基线构建失败仍继续 | 报告失败，询问是否继续 |
| 硬编码 npm test | JNPF 后端用 `dotnet build`，前端用 `pnpm build` |

## 集成

- **调用方**：`executing-plans`、`subagent-driven-development`（执行前）
- **配对**：`finishing-a-development-branch`（完成后清理 worktree）
