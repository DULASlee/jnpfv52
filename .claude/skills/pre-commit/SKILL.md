---
name: pre-commit
description: 提交前检查（编译+架构红线+代码质量）。当用户要 git commit/push、或要求提交前验证时触发。
---

# Pre-Commit Check

在 `git commit` 之前执行完整检查，确保不提交未编译/未格式化的代码。

> **与 hooks 的关系：** hooks 在写入文件时触发（guard-write），本 skill 在提交前触发，补充 hooks 的盲区。

## 执行步骤

### Step 1: 检查工作区状态

```bash
git status
git diff --stat
```

如果工作区干净 → 报告"无变更可提交"，结束。

### Step 2: 编译验证

根据变更文件类型选择验证命令：

```bash
# 检查是否有后端变更
git diff --name-only | grep -E "\.cs$|\.csproj$"
```

如果有后端变更：
```bash
cd backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj
```
预期：`Build succeeded. 0 Error(s)`

```bash
# 检查是否有前端变更
git diff --name-only | grep -E "\.vue$|\.ts$|\.tsx$|\.js$"
```

如果有前端变更：
```bash
cd jnpf-web-vue3 && pnpm type-check
# 若改了 workflow/onlineDev/FormGenerator 等 legacy：pnpm type-check:full
```
预期：无输出（0 errors）。禁止 `npx vue-tsc --noEmit`（全量 OOM）。见 `.cursor/rules/frontend-typecheck.mdc`。

**任何编译失败 → 停止，报告错误，不继续提交。**

### Step 3: 架构红线快速检查

对变更的 .cs 文件执行以下检查：

```bash
# R1: 是否手动创建了 Controller？
git diff --name-only | grep -i "controller\.cs$"
```
如果有匹配 → 警告：违反 R1，应使用 IDynamicApiController 自动映射。

```bash
# R4: 新增的 SqlSugar 查询是否包含租户过滤？
git diff | grep -E "ISugarQueryable|Updateable|Deleteable" | grep -v "ITenantFilter"
```
如果有匹配 → 警告：可能违反 R4，检查是否需要 ITenantFilter。

```bash
# R5: 是否修改了禁用模块？
git diff --name-only | grep -E "modularity/oa/|modularity/iot/|modularity/mes/"
```
如果有匹配 → 警告：违反 R5，OA 禁用 / IoT、MES 未创建。

### Step 4: 代码质量快速检查

```bash
# 检查是否有 TODO / TBD / fix later
git diff | grep -iE "^\+.*(TODO|TBD|fix later|应该可以|理论上)"
```
如果有匹配 → 警告：违反工程铁律，未完成的代码不应提交。

```bash
# 检查是否有吞没异常的 try-catch
git diff | grep -A 2 "catch.*Exception" | grep -E "^\+.*\{\s*\}"
```
如果有匹配 → 警告：可能吞没异常。

### Step 5: 输出检查报告

```
## 提交前检查报告

| 检查项 | 结果 | 详情 |
|--------|------|------|
| 编译验证 | PASS/FAIL | [错误数] |
| R1 Controller | PASS/WARN | [详情] |
| R4 租户过滤 | PASS/WARN | [详情] |
| R5 禁用模块 | PASS/WARN | [详情] |
| 代码质量 | PASS/WARN | [TODO/异常处理] |

### 结论
- ✅ 可以提交：所有检查通过
- ⚠️ 建议修复后再提交：有警告但非阻塞
- ❌ 禁止提交：有编译错误或严重违规

### 下一步
- 如果 PASS：git add <文件> && git commit -m "..."
- 如果 FAIL：修复问题后重新运行 pre-commit 检查
```
