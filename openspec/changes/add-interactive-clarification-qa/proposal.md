# Change: add-interactive-clarification-qa

## Why

需求分析阶段 LLM 识别出歧义点（如"请假时长按自然日还是工作日""调休余额来源"）时，现有实现直接输出 `## 待确认事项` markdown 段落供用户整体确认，**没有提供单选/多选/文本补充的交互式问答**供用户逐条细化需求。用户无法与 LLM 多轮交互来收敛歧义，导致需求分析说明书带病进入下游设计阶段。

## What

引入**交互式澄清问答系统**，覆盖需求分析、架构设计、总体设计三个阶段：

- LLM 产出结构化选择题（单选/多选/文本，每轮 3-5 题，每题 3-5 选项，末项恒为"其他"+ 文本框）
- 用户通过交互卡片作答，关键题（required）硬门控推进流程
- 完整 IR 事件化（`ClarificationRequested` / `ClarificationAnswered`），可审计回放
- 默认 3-7 轮（可配置 `Clarification:MaxRounds`），用户可随时"全部跳过直接分析"

## Scope

| 纳入 | 排除 |
|------|------|
| `RequirementGateService` 三模式 prompt 升级 + `BuildClarificationSet` | `RequirementGateService` 成熟度评分算法本身 |
| `ArchitectSkillService` 两阶段改造（提问 + ToT） | sa-service 9 个 Agent 的 prompt |
| `SystemDesignClarificationSkill`（新建，两阶段） | `SystemDesignSkillService` 本体（保持纯约束引擎） |
| `IrProjectionEngine` 注册 Clarification 投影 | `DesignSkillOrchestrator` 编排顺序（零改动） |
| `SkillsApiService.AnswerClarificationAsync`（关键题硬门控） | 多租户隔离（沿用既有 `ITenantFilter`） |
| 前端 `ClarificationCard.vue` + SSE `clarification_requested` 分支 | 问卷 UI 主题/皮肤定制 |
| IR 事件：`ClarificationRequested` / `ClarificationAnswered` / `SystemDesignClarificationCompleted` | 答案版本化/diff（沿用既有 IR 事件序列） |

## Status

- [x] 草稿创建（2026-07-06，P1+P2+P3 已实现并通过编译/类型/lint 验收）
- [ ] 运行时端到端验证（pipeline 311 全链路）
- [ ] 归档到 `openspec/specs/studio-clarification/`
