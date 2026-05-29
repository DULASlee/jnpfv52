---
name: systematic-debugging
description: Systematic debugging with reproduce→hypothesize→verify→fix→confirm workflow. Use when encountering bugs, errors, or unexpected behavior that needs investigation.
---

# Systematic Debugging — 系统化调试

不要随机尝试修复。按以下流程系统化排查。

## 工作流

### 1. 复现（Reproduce）

```bash
# 记录复现步骤
1. 操作：...
2. 输入：...
3. 结果：...
4. 期望：...
```

**必须**把完整报错信息贴出来，不要截断。

### 2. 假设（Hypothesize）

基于源码阅读，列出可能的原因（按可能性排序）：

```
假设 A：[原因] — 可能性：高/中/低
假设 B：[原因] — 可能性：高/中/低
假设 C：[原因] — 可能性：高/中/低
```

**不要猜。** 每个假设必须基于源码证据。

### 3. 验证假设（Verify）

对最可能的假设，设计验证方法：

| 假设 | 验证方法 | 通过条件 |
|------|----------|----------|
| A | 搜索源码 / 加日志 / 断点 | ... |
| B | 检查配置 / 数据库查询 | ... |

按可能性从高到低验证，直到找到根因。

### 4. 修复（Fix）

- 只用搜索工具读源码，不猜测
- 最小改动原则：只改必要的代码
- 记录改了什么、为什么

### 5. 确认修复（Confirm）

- 重新复现步骤，确认问题消失
- 运行相关测试/验证
- 检查是否引入新问题

## 工具使用

| 排查需求 | 工具 |
|----------|------|
| 理解代码逻辑 | `search_codebase` |
| 追踪调用链 | `lsp` → `findReferences` / `incomingCalls` |
| 查配置 | `read_file` → Configurations/*.json |
| 查数据库 | `sqlcmd` |
| 看日志 | `read_file` → logs/*.log |
| 测试修复 | `run_in_terminal` → dotnet build/run |

## 铁律

- ❌ 禁止盲目尝试多个修复
- ❌ 禁止跳过复现步骤
- ✅ 每个假设必须有源码支撑
- ✅ 完整报错必须贴出来
