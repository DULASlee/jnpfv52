---
name: writing-plans
description: Create detailed construction packages (施工包) for non-trivial changes. Use when the task involves multiple files, architectural decisions, or needs review before implementation.
---

# Writing Plans — 编写施工包

为非 trivial 的变更编写结构化施工包，确保方案经过审核再执行。

## 适用场景

- 涉及多文件/多模块的改动
- 架构层面的决策（技术选型、接口设计）
- 需要分阶段实施的复杂任务
- 老板/架构师要求审核方案

## 工作流

### 1. 预研（Brainstorming）

先调用 `brainstorming` 技能摸清现状和根因。

### 2. 编写施工包

产出文件位置：`docs/架构迭代/` 或 `docs/architecture/`

施工包结构：

```markdown
# [任务名称] 施工包

## 1. 背景与目标
- 当前状态
- 期望状态

## 2. 影响范围
- 涉及文件清单
- 涉及模块

## 3. 分阶段任务

### 阶段 1：[阶段名]
- [ ] 任务 1.1 — 描述
- [ ] 任务 1.2 — 描述
- 验收标准：...

### 阶段 2：[阶段名]
- [ ] 任务 2.1 — 描述
- 验收标准：...

## 4. 风险与对策
| 风险 | 概率 | 对策 |
|------|------|------|

## 5. 验证计划
- 构建验证：dotnet build
- 功能验证：...
- 回归验证：...
```

### 3. 审核

施工包写完后，标注"待架构师审核"。未经审核不得进入 executing-plans。

## 铁律

- ✅ 每个阶段必须有明确的验收标准
- ✅ 任务粒度：每个任务应在 30 分钟内可完成
- ❌ 禁止跳过预研直接写方案
- ❌ 禁止没有验收标准的任务
