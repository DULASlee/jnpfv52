# Studio 交互式澄清问答（ADR-005）

> Cursor 镜像：`.cursor/rules/studio-clarification.mdc`
> 知识库：`openspec/specs/studio-clarification/spec.md` · ADR-005 · `openspec/changes/add-interactive-clarification-qa/`

## 三阶段差异化实现（2026-07-06）

| 阶段 | 提问入口 | 暂停/恢复 | 答案注入 |
|------|---------|-----------|---------|
| 需求分析 | `RequirementGateService` 复用成熟度评估 LLM | sa-gate 对话流 `sse.Complete();return` | 对话历史 → 下一轮 maturity |
| 架构设计 | `ArchitectSkillService` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText → ToT userPrompt |
| 总体设计 | `SystemDesignClarificationSkill` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText → SystemDesignLocked.assumptions |

## IR 事件

| 事件 | fragment 状态 |
|------|--------------|
| `ClarificationRequested` | `IR1_Clarification` → in-progress |
| `ClarificationAnswered` | `IR1_Clarification` → stable |
| `SystemDesignClarificationCompleted` | 留痕（不更新 fragment） |

fragmentId 按 stage 区分：`clarification:{requirement|architecture|system-design}:{projectId}`

## 关键题硬门控

`ClarificationQuestion.Required=true` 必答才推进。`AnswerClarificationAsync` 遍历 required 题，未作答 `throw Oops.Bah`。

## 不变量（BuildClarificationSet 强制）

每题 options ∈ [3,5] · 末项恒为 `{id:"o_other",label:"其他",freeText:true}` · type ∈ {single,multi,text} · required ≤2/轮 · round ∈ [1,7]（`Clarification:MaxRounds` 默认 7）

## 验收

`dotnet build` · `pnpm type-check` · `pnpm lint` · `E2E_PIPELINE_ID=311 pnpm test:api`（待运行时验证）

## 禁止

· 跳过关键题门控推进流程 · 修改 `SystemDesignSkillService` 本体（保持纯约束引擎）· 在 compile 主链插入提问（只在调 LLM 的环节）· LLM 失败不降级（必用 fallback 默认题）

## 关键代码

`Gates/RequirementGateService.cs`（BuildClarificationSet）· `Skills/ArchitectSkillService.cs`（两阶段）· `Skills/SystemDesignClarificationSkill.cs`（新建两阶段）· `Skills/SkillsApiService.cs`（AnswerClarificationAsync）· `Ir/IrProjectionEngine.cs`（UpsertClarificationAsync）· `jnpf-web-vue3/.../clarification/ClarificationCard.vue`
