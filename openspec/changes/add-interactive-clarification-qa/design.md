# Design: add-interactive-clarification-qa

## 方案

三阶段差异化实现，统一 IR 事件契约（`ClarificationRequested` / `ClarificationAnswered`）：

| 阶段 | 提问产生 | 暂停/恢复机制 | 答案注入 |
|------|---------|--------------|---------|
| 需求分析 | `RequirementGateService` 复用成熟度评估 LLM | sa-gate 对话循环（`sse.Complete(); return`） | 答案存为对话历史，下一轮 maturity 评估读取 |
| 架构设计 | `ArchitectSkillService` 阶段一调 BudgetGuard LLM | 两阶段 Skill 执行（ThinkAsync yield 后 return，重跑恢复） | answersText 注入 ToT 的 userPrompt |
| 总体设计 | `SystemDesignClarificationSkill` 阶段一调 BudgetGuard LLM | 两阶段 Skill 执行（同上） | answersText 写入 SystemDesignLocked payload 的 assumptions 字段（约束引擎不读 prompt，留痕） |

**核心约束**：需求阶段走 sa-gate 对话流（单流暂停）；设计阶段走 Skill 两阶段执行（因 `ThinkAsync` 是单次消费 `IAsyncEnumerable`，return 即 run 结束，必须重跑恢复）。

**projection 补全**：`IrProjectionEngine` 原本不认识 Clarification 事件（落入 `_ => null`），P2 补 `UpsertClarificationAsync`（Requested→in-progress / Answered→stable）—— 这是两阶段模式成立的前提（第二次 run 靠 `snapshot.Find(Clarification, Stable)` 判断已作答）。

**fragmentId 按 stage 区分**：`clarification:requirement:{projectId}` / `clarification:architecture:{projectId}` / `clarification:system-design:{projectId}`，同 `IrFragmentTypes.Clarification` 类型靠前缀区分。

## 误诊排除

- **不是** sa-service 9 个 Agent prompt 改造 —— compile 主链（默认）不调 LLM，提问插入在真正调 LLM 的环节（gate / Architect / SystemDesignClarification）
- **不是** `DesignSkillOrchestrator` 编排顺序改动 —— SystemDesignClarificationSkill 由前端作答后单独触发，不进入并行/串行流
- **不是** `SystemDesignSkillService` 改造 —— 它保持纯约束引擎，ClarificationSkill 自包含完成阶段二

## 关键文件

**后端：**
- `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Ir/ClarificationDtos.cs`（新建）
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/RequirementGateService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/SkillsApiService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/ArchitectSkillService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/SystemDesignClarificationSkill.cs`（新建）
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DesignSkillsApiService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Ir/IrProjectionEngine.cs`

**前端：**
- `jnpf-web-vue3/src/views/studio/api/studio/skills.ts`
- `jnpf-web-vue3/src/views/studio/api/studio/designSkills.ts`
- `jnpf-web-vue3/src/views/studio/components/clarification/ClarificationCard.vue`（新建）
- `jnpf-web-vue3/src/views/studio/composables/useClarification.ts`（新建）
- `jnpf-web-vue3/src/views/studio/components/AiChatPanel.vue`
