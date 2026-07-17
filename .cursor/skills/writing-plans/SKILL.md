---
name: writing-plans
description: Create detailed construction packages (施工包) for non-trivial changes. Use when the task involves multiple files, architectural decisions, or needs review before implementation.
scope: JNPF-v52
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

## 2.5 ADF 三先行（S/A 级强制）

> 模板：`.cursor/templates/adf-architecture.md` · `adf-patterns.md` · `adf-contracts.md`  
> 规则：`.cursor/rules/architecture-design-interface-first.mdc`

### §架构（P1）
- 层边界 / 数据唯一源 / 三元组 / ≥2 方案+failure_boundary / 禁改清单
- 状态：待用户「继续」

### §模式（P2）
- 1–2 主模式 + 映射 SkillHarness/Gate/IR/IDynamicApiController + 为何不用替代
- 状态：待 P1 批准后填写

### §契约（P3）
- 签名/DTO/事件/错误契约（无方法体）
- 状态：待 P2 批准后填写

B 级可写：`ADF 豁免：B级 — …`

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
**S/A：** §架构/§模式/§契约未获用户「继续」前，禁止 coder 写业务实现。

## 铁律

- ✅ 每个阶段必须有明确的验收标准
- ✅ 任务粒度：每个任务应在 30 分钟内可完成
- ✅ S/A 施工包必须含 ADF §架构 / §模式 / §契约
- ❌ 禁止跳过预研直接写方案
- ❌ 禁止没有验收标准的任务
- ❌ 禁止 S/A 跳过 ADF 直接编码