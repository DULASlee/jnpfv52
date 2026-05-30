---
name: dispatching-parallel-agents
description: 面对 2+ 个互不依赖、可并行处理的独立任务时使用。通过 Cursor Task 工具派发子 Agent 并行执行。
scope: JNPF-v52
---

# Dispatching Parallel Agents — 并行子 Agent 派发

## 适用场景

- 多个测试文件/子系统各自独立失败
- 后端 API 与前端页面改动互不阻塞
- 多个模块 Bug 根因无关，可并行调查

## 不适用

- 失败可能同源（修一个可能修全部）
- 需要共享上下文才能理解问题
- 多个 Agent 会改同一文件

## 工作流

### 1. 划分独立域

按「互不影响」分组，例如：
- Agent A：后端 `modularity/system/` 登录 Service
- Agent B：前端 `jnpf-web-vue3/src/views/basic/login/`
- Agent C：Highcharts 图表 demo 页面

### 2. 构造聚焦 prompt

每个 Agent 必须包含：
- **范围**：具体文件/模块
- **目标**：要达成什么（测试通过 / Bug 修复）
- **约束**：不改哪些文件
- **返回**：根因摘要 + 改动清单

### 3. 并行派发（Cursor Task 工具）

同一条消息中发起多个 `Task` 调用，例如：

```
Task(subagent_type=generalPurpose, prompt="修复 backend/... LoginService 的 xxx 问题...")
Task(subagent_type=explore, prompt="追踪 jnpf-web-vue3 登录页 token 存储链路...")
```

### 4. 汇总集成

- 阅读各 Agent 返回摘要
- 检查是否改动同一文件（冲突）
- 运行 `dotnet build` + 相关前端验证
- 调用 `verification-before-completion`

## Prompt 模板

```markdown
## 任务
修复 [具体文件/模块] 的 [具体问题]

## 上下文
[错误信息、复现步骤、相关表/API]

## 约束
- 仅改 [范围]
- 不改 [排除范围]
- 遵循 AGENTS.md 编码规范

## 返回
1. 根因（附文件路径+行号）
2. 改动文件列表
3. 验证命令及结果
```

## 铁律

- ❌ 「修复所有测试」— 范围过大
- ✅ 一 Agent 一独立问题域
- ✅ prompt 自包含，不假设 Agent 继承会话历史
