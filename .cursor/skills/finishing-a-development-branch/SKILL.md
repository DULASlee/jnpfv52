---
name: finishing-a-development-branch
description: 实现完成、验证通过后，引导用户选择合并/PR/保留/丢弃分支，并清理 worktree。
scope: JNPF-v52
tech-stack: [dotnet, pnpm]
---

# Finishing a Development Branch — 分支收尾

## 前置

必须先通过 `verification-before-completion`：
- `dotnet build`（backend）
- 相关前端 `pnpm build`（若改了前端）
- 功能点手动验证

**构建或测试失败 → 停止，不提供收尾选项。**

## 流程

### Step 1：确认基线分支

```powershell
git merge-base HEAD main
# 或 master；不确定则询问用户
```

### Step 2：呈现 4 个选项（不多不少）

```
实现已完成。请选择：

1. 本地合并回 <base-branch>
2. Push 并创建 Pull Request
3. 保持分支不动（稍后自行处理）
4. 丢弃此工作

选哪一项？
```

### Step 3：执行

**选项 1 — 本地合并**
```powershell
git checkout <base>
git pull
git merge <feature>
cd backend; dotnet build   # 合并后验证
git branch -d <feature>
```

**选项 2 — 创建 PR**
```powershell
git push -u origin <feature>
gh pr create --title "..." --body "..."
```
worktree **保留**（PR 待审期间）。

**选项 3 — 保持**
报告分支名与 worktree 路径，不清理。

**选项 4 — 丢弃**
要求用户输入 `discard` 确认后：
```powershell
git checkout <base>
git branch -D <feature>
```

### Step 4：Worktree 清理

| 选项 | 清理 worktree |
|------|---------------|
| 1 合并 | ✅ |
| 2 PR | ❌ 保留 |
| 3 保持 | ❌ 保留 |
| 4 丢弃 | ✅ |

```powershell
git worktree remove <path>
```

## 铁律

- ❌ 测试/构建失败时提供合并选项
- ❌ 丢弃工作无确认
- ❌ 未经用户明确要求 force push
- ✅ 选项 4 必须 typed `discard` 确认

## 集成

- **调用方**：`executing-plans`、`subagent-driven-development` 全部任务完成后
- **配对**：`using-git-worktrees` 创建的 worktree 在此清理
