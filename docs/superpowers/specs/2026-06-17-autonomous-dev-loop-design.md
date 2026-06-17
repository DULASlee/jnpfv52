# 自主开发循环设计文档

**日期：** 2026-06-17
**状态：** 已批准
**作者：** Claude + 首席架构师

---

## 1. 概述

### 1.1 目标

实现 AI 大模型自主循环开发，自动完成首席架构师安排的工作任务，每次开发任务完成后自动执行 full-review 代码审查。

### 1.2 核心流程

```
监听任务 → 执行开发 → 代码审查 → 自动修复 → 提交归档 → 循环继续
```

---

## 2. 设计决策

### 2.1 执行模式

**统一自动执行** — 所有任务类型都由 AI 自主处理，不做区分。

### 2.2 检测频率

**每 15 分钟** — 平衡频率和资源消耗。

### 2.3 审查失败处理

**自动修复** — AI 自动修复问题，修复后重新审查，最多重试 3 次。

### 2.4 代码提交

**仅本地提交** — 完成后自动 `git commit`，等待人工确认后再 `git push`。

---

## 3. 任务文件格式

### 3.1 文件位置

```
.claude/tasks/
├── pending/          # 待处理任务
│   └── S6-1-browser-e2e-verification.md
└── completed/        # 已完成任务
    └── S6-1-browser-e2e-verification.md
```

### 3.2 文件格式

```yaml
---
title: 任务标题
priority: critical/high/medium/low
assignee: chief-architect
created: 2026-06-17
sprint: 6
task_id: S6-1
estimated_hours: 2
---

## 任务描述

详细描述任务内容...

## 验收标准

- [ ] 标准 1
- [ ] 标准 2

## 相关文件

- path/to/file1
- path/to/file2
```

---

## 4. 循环逻辑

### 4.1 CronCreate 配置

```javascript
CronCreate:
  cron: "*/15 * * * *"  // 每 15 分钟执行
  prompt: |
    自主开发循环启动：
    1. 读取 .claude/tasks/pending/ 目录
    2. 如果没有任务，输出 "等待新任务" 并结束
    3. 如果有任务，读取第一个任务文件
    4. 解析任务标题、描述、验收标准
    5. 执行开发流程（brainstorming → writing-plans → executing）
    6. 执行 full-review（test-runner + code-reviewer + security-scanner）
    7. 如果审查发现问题，自动修复，最多重试 3 次
    8. 审查通过后，git add -A && git commit
    9. 移动任务文件到 .claude/tasks/completed/
    10. 输出完成报告
  recurring: true  // 循环执行
```

### 4.2 执行流程

```
每 15 分钟执行一次：

1. 扫描 .claude/tasks/pending/ 目录
2. 如果没有任务 → 输出 "等待新任务"，结束
3. 读取第一个任务文件（按文件名排序）
4. 解析任务元数据和内容
5. 执行任务：
   a. 调用 superpowers:brainstorming 设计方案
   b. 调用 superpowers:writing-plans 编写计划
   c. 调用 superpowers:executing-plans 执行开发
   d. 运行构建验证（dotnet build / vue-tsc --noEmit）
6. 执行 full-review：
   a. test-runner 子代理
   b. code-reviewer 子代理
   c. security-scanner 子代理
7. 如果审查发现问题：
   a. 自动修复
   b. 重新审查
   c. 最多重试 3 次
   d. 3 次仍失败 → 标记为 "需要人工介入"
8. 审查通过后：
   a. git add -A
   b. git commit -m "feat: {任务标题}"
   c. 移动任务文件到 .claude/tasks/completed/
9. 输出完成报告
10. 继续下一个任务（如果有）
```

---

## 5. 输出格式

### 5.1 循环状态报告

```
🔄 自主循环 [2026-06-17 10:00]
├── 任务: Sprint 6-1 浏览器端到端验证
├── 状态: 执行中
├── 进度: 3/15 验证项完成
└── 预计剩余: 30 分钟
```

### 5.2 完成报告

```
✅ 任务完成 [2026-06-17 10:30]
├── 任务: Sprint 6-1 浏览器端到端验证
├── 审查: PASS (0 重试)
├── 提交: abc1234
└── 下一个任务: S6-2 C档页面补齐
```

### 5.3 失败报告

```
❌ 任务失败 [2026-06-17 10:30]
├── 任务: Sprint 6-1 浏览器端到端验证
├── 审查: FAIL (3/3 重试)
├── 问题: 无法自动修复
└── 状态: 需要人工介入
```

---

## 6. 技术实现

### 6.1 目录结构

```
.claude/
├── tasks/
│   ├── pending/          # 待处理任务
│   └── completed/        # 已完成任务
├── workflows/
│   └── auto-dev-loop.md  # 循环工作流定义
└── rules/
    └── workflow.md        # 工作流程规则
```

### 6.2 依赖的技能

| 技能 | 用途 |
|---|---|
| `superpowers:brainstorming` | 设计方案 |
| `superpowers:writing-plans` | 编写计划 |
| `superpowers:executing-plans` | 执行开发 |
| `superpowers:verification-before-completion` | 完成前验证 |
| `/full-review` | 代码审查 |

### 6.3 依赖的子代理

| 子代理 | 用途 |
|---|---|
| `test-runner` | 构建验证 |
| `code-reviewer` | 代码审查 |
| `security-scanner` | 安全扫描 |

---

## 7. 限制和约束

### 7.1 会话生命周期

- CronCreate 任务在会话结束后停止
- 需要重新启动会话才能恢复循环

### 7.2 7 天自动过期

- 循环任务最多运行 7 天
- 7 天后需要重新创建

### 7.3 Token 消耗

- 持续循环会消耗较多 token
- 建议在任务较多时启用

### 7.4 人工介入

- 3 次自动修复失败后需要人工介入
- 验证类任务需要人工操作

---

## 8. 启动命令

```bash
# 启动自主循环
CronCreate:
  cron: "*/15 * * * *"
  prompt: "自主开发循环启动..."
  recurring: true

# 停止循环
CronDelete:
  id: {job_id}

# 检查状态
CronList
```

---

## 9. 验收标准

- [ ] 任务队列目录已创建
- [ ] 任务文件格式正确
- [ ] CronCreate 配置正确
- [ ] 循环能自动检测任务
- [ ] 能自动执行开发流程
- [ ] 能自动执行 full-review
- [ ] 审查失败能自动修复（最多 3 次）
- [ ] 完成后能自动提交
- [ ] 任务文件能移动到 completed 目录
- [ ] 输出报告格式正确
