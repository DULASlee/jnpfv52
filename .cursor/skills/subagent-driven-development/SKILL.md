---
name: subagent-driven-development
description: Decompose large tasks into parallel sub-tasks executed by specialized subagents. Use when the task is large enough to benefit from parallel execution across multiple agents.
---

# Subagent-Driven Development — 子代理并行开发

将大任务拆分为可并行的子任务，用多个子代理同时执行，提升效率。

## 适用场景

- 任务涉及多个独立模块
- 前后端需要同时改动
- 多个文件需要类似的操作
- 代码审查 + 修复需要并行

## 工作流

### 1. 任务分解

将一个大的施工包阶段拆分为独立的子任务：

```
主任务：系统启动 + 登录验证

子任务 1：[后端] 检查 SQL Server + 启动 API
子任务 2：[前端] 启动 PC 前端
子任务 3：[前端] 启动大屏前端
子任务 4：[验证] admin 登录三系统
```

判断标准：子任务之间**无依赖**，可以并行执行。

### 2. 分配子代理

对每个子任务启动一个子代理：

```
Agent 1 (Browser): 打开 PC 前端页面，验证登录
Agent 2 (Browser): 打开大屏页面，验证登录
```

对于纯代码任务，用 Agent 工具的默认代理类型。

### 3. 汇总结果

所有子代理完成后，汇总结果：
- 成功的任务
- 失败的任务（附错误信息）
- 需要人工处理的问题

### 4. 串行依赖处理

有依赖关系的任务必须串行执行：
```
任务 A（编译后端）→ 任务 B（启动后端）→ 任务 C（测试 API）
```

## 何时不用子代理

- 任务很小（< 3 步）
- 任务之间有强依赖
- 需要人工判断的复杂决策

## 铁律

- ✅ 子任务必须无依赖才能并行
- ✅ 每个子代理必须有明确的输入和输出
- ❌ 禁止用子代理做需要人工判断的决策
