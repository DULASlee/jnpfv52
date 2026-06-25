---
name: "trace-bug"
description: "Runs 4-stage structured debugging (reproduce → hypothesize → instrument → fix). Invoke when user reports a bug, test failure, unexpected behavior, error, or asks to debug an issue."
---

# Trace Bug — Structured Debugging

启动四阶段调试流程，强制收集运行时证据后再修复。

> **调试流程详细规则：** 见 `.claude/rules/debugging.md`
> 本 skill 启动流程并输出调试声明，不重复规则内容。

## 执行步骤

### Step 1: 输出调试协议声明

```
🔍 Debugging Protocol 启动

- 当前问题：[等待用户描述或从上下文推断]
- 调试策略：四阶段流程（复现 → 假设 → 插桩 → 修复）
- 铁律：无证据，不修复。无验证，不声称完成。
```

### Step 2: 收集问题上下文

向用户询问（如果未提供）：
1. **症状**：发生了什么？预期 vs 实际？
2. **复现步骤**：如何稳定触发？
3. **错误信息**：完整的异常堆栈/日志？
4. **影响范围**：哪些功能受影响？
5. **最近变更**：问题出现前改了什么？（`git log --oneline -5`、`git diff`）

### Step 3: 阶段 1 — 复现

目标：稳定复现问题。

- 如果用户已提供复现步骤 → 验证步骤有效性
- 如果未提供 → 通过代码分析推断触发条件，构造复现路径
- **必须得到**：一个可重复触发的具体操作序列

如果无法复现 → 报告"无法稳定复现"，询问用户是否能提供更多上下文。

### Step 4: 阶段 2 — 假设

基于复现结果，列出所有可能的根因假设：

```
## 根因假设清单

| # | 假设 | 依据 | 验证方式 | 优先级 |
|---|------|------|---------|--------|
| 1 | [假设 A] | [代码线索] | [插桩/日志] | 高/中/低 |
| 2 | [假设 B] | [代码线索] | [插桩/日志] | 高/中/低 |
```

按优先级排序，先验证最可能的假设。

### Step 5: 阶段 3 — 插桩验证

对优先级最高的假设，设计最小化验证方案：

- **日志插桩**：在关键路径加 `Log.Information(...)`，记录变量值
- **断点验证**：如果支持调试器，设置断点
- **单元测试**：针对假设编写失败测试
- **临时输出**：`Console.WriteLine` 或 `Debug.WriteLine`

运行复现步骤，收集证据。

**关键：** 只验证，不修复。收集到证据后再进入下一阶段。

### Step 6: 阶段 4 — 修复

基于证据确认根因后：

1. 设计最小化修复方案（不过度重构）
2. 实施修复
3. 运行验证：
   - `cd backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj`
   - 复现原始症状 → 确认消失
   - 检查副作用：相关功能是否正常

### Step 7: JNPF 专项检查

修复后，对照 `.claude/rules/debugging.md` 的 "JNPF 专项检查清单" 逐项确认：
- [ ] ITenantFilter 在子查询中是否生效？
- [ ] Mapster Adapt() 是否覆盖了审计字段？
- [ ] Oops.Bah vs Oops.Oh 使用是否正确？
- [ ] 异步方法是否有 Async 后缀？（不应有）
- [ ] .vm 生成页面是否被修改？（不应修改）

### Step 8: 输出调试报告

```
## 调试报告

### 问题
- 症状：[描述]
- 根因：[确认的根因]

### 证据
- [插桩日志/测试结果]

### 修复
- 文件：[修改的文件]
- 方案：[修复方案]
- 验证：[复现步骤已通过 + 编译通过]

### JNPF 专项检查
- [全部通过]

### 剩余风险
- [如有]
```

将重要 bug 的根因和修复方案追加到 `.claude/memory/lessons-learned.md`。
