---
name: requesting-code-review
description: Request a code review after completing a significant piece of work. Use after implementing a feature, fixing a bug, or completing a construction package phase.
scope: JNPF-v52
---

# Requesting Code Review — 请求代码审查

完成一段重要代码后，提交给 code-reviewer 子代理审查。

## 适用场景

- 完成一个施工包阶段
- 实现新功能
- 修复复杂 Bug
- 重构核心代码

## 工作流

### 1. 确认前置条件

- [ ] 代码已提交（或至少在文件系统中）
- [ ] 构建通过（`dotnet build` / `pnpm build`）
- [ ] 基础验证通过（`verification-before-completion`）

### 2. 确定审查范围

明确要审查什么：

```
审查范围：
- 改动文件：...
- 改动类型：新功能 / Bug修复 / 重构
- 关注重点：安全性 / 性能 / 边界条件
```

### 3. 启动 code-reviewer 子代理

使用 Agent 工具启动 `code-reviewer` 子代理，传入：
- 改动的文件列表
- 改动说明
- 关注重点

### 4. 处理审查意见

对审查意见分类处理：

| 类型 | 处理方式 |
|------|----------|
| 🔴 必须修 | 立即修复 |
| 🟡 建议修 | 评估后决定 |
| 🟢 可选 | 记录，后续改进 |

### 5. 修复后重新审查

修复 review 意见后，如有必要再次审查。

## 铁律

- ❌ 构建不通过不提审
- ❌ 不提审没有验证过的代码
- ✅ 审查意见必须逐条回复
