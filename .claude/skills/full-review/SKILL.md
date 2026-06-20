---
name: full-review
description: 执行三阶段代码审查（test-runner → code-reviewer → 修复循环）。当用户要求代码审查、合并前、重大变更后、或提到审查代码质量时触发。
---

# Full Code Review

执行完整的 Review Workflow：验证 → 审查 → 修复循环。

> **子代理 Prompt 模板、审查维度、循环终止条件：** 见 `.claude/rules/review-workflow.md`
> 本 skill 只定义执行流程编排，不重复 Prompt 内容。

## 执行步骤

### Step 1: 收集变更上下文

运行以下命令收集本次变更信息（同时覆盖已提交和未提交的变更）：

```bash
# 已提交的最近一次变更
git diff --name-only HEAD~1 HEAD
git diff --stat HEAD~1 HEAD

# 未提交的工作区变更
git diff --name-only
git diff --stat
```

合并两个列表，去重，得到本次审查的完整文件清单。

### Step 2: 快速跳过判断

如果变更文件**全部**是以下类型，直接跳到 Step 6 输出"无需代码审查"报告：
- `*.md`（文档）
- `docs/**`（文档目录）
- `LICENSE`、`.gitignore`、`.editorconfig`

如果有任何 `.cs`、`.vue`、`.ts`、`.js`、`.json`（配置除外）文件变更 → 继续 Step 3。

### Step 3: 阶段 4 — test-runner

使用 Agent tool 启动 test-runner 子代理，**Prompt 模板见 `.claude/rules/review-workflow.md` 的 "test-runner Prompt" 章节**。

将以下信息注入 Prompt 的占位符：
- `[文件列表]` ← Step 1 收集的变更文件
- `[diff stat]` ← Step 1 收集的 diff stat

如果 test-runner 返回 FAIL → 立即修复编译错误 → 重新运行 test-runner
如果 test-runner 返回 PASS → 继续 Step 4

### Step 4: 阶段 5 — code-reviewer

使用 Agent tool 启动 code-reviewer 子代理，**Prompt 模板见 `.claude/rules/review-workflow.md` 的 "code-reviewer Prompt" 章节**。

将以下信息注入 Prompt 的占位符：
- `[文件列表]` ← Step 1 收集的变更文件
- `[diff 内容]` ← 读取每个变更文件的 diff

### Step 5: 阶段 6 — 处理反馈

如果 code-reviewer 返回 FAIL：
1. 立即修复所有严重问题
2. 重新运行 test-runner（Step 3）
3. 重新运行 code-reviewer（Step 4）
4. 最多循环 3 次

3 轮后仍有 FAIL → 停止，报告给用户请求介入

如果 code-reviewer 返回 PASS → 继续 Step 6

### Step 6: 输出最终报告

汇总以下信息输出给用户：

```
## 审查报告

### 变更摘要
- [一句话描述本次变更目的]

### 文件变更
- [文件列表 + 行数变化]

### 验证结果
| 阶段 | 结果 | 说明 |
|------|------|------|
| test-runner | PASS/FAIL/SKIP | [说明] |
| code-reviewer | PASS/FAIL/SKIP | [说明] |

### 修复记录
- [如有修复，列出每轮修复的问题和方案]

### 剩余风险
- [如有，列出潜在但未修复的问题]

### 建议
- [下一步行动建议]
```

如果是重要架构变更，将报告追加到 `.claude/memory/decisions.md`。
