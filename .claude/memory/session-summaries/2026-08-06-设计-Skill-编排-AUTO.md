# 设计 Skill 编排（自动草稿）
> Cursor `stop` hook 于 2026-08-06T10:39:22.129Z 自动生成。
> Agent 或用户 SHOULD 补全：问题链、根因、下 Chat 开场词。
## 变更文件（166）
- `.ai-memory/knowledge-graph.json`
- `.claude/.session-init-lock.json`
- `.claude/.skill-load-state.json`
- `.cursor/hooks.json`
- `.cursor/hooks/episodic-session-start.mjs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Ir/IrEventStoreService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Ir/IrProjectionEngine.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Llm/LlmCallPolicy.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Llm/SkillLlmBudgetGuard.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/LlmBudgetApiService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Pipeline/StageConfirmSkillTrigger.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/AnalystSkillService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DesignSkillOrchestrator.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DesignSkillsApiService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/RequirementAnalysisOrchestrator.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/SkillsApiService.cs`
- `backend/tests/JNPF.Tests.Gate/Gates/SemanticFitnessValidatorTests.cs`
- `backend/tests/JNPF.Tests.PhaseB/ExperienceR4Tests.cs`
- `backend/tests/JNPF.Tests.PhaseB/IrPhase2SkillTests.cs`
- `backend/tests/JNPF.Tests.PhaseB/IrPhase4ArchGuardQ2Tests.cs`
- `backend/tests/JNPF.Tests.PhaseB/IrPhase4ArchGuardTests.cs`
- `backend/tests/JNPF.Tests.PhaseB/PmIntentClassificationTests.cs`
- `backend/tests/JNPF.Tests.PhaseB/PmNewPipelineTests.cs`
- `backend/tests/JNPF.Tests.PhaseB/PmSkillR1Tests.cs`
- `backend/tests/JNPF.Tests.PhaseB/RequirementAnalysisOrchestratorTests.cs`
- `jnpf-web-vue3/src/views/studio/api/studio/designSkills.ts`
- `jnpf-web-vue3/src/views/studio/api/studio/skills.ts`
- `jnpf-web-vue3/src/views/studio/components/AiChatPanel.vue`
- `jnpf-web-vue3/src/views/studio/components/ir/IrRequirementSpecConfirmCard.vue`
- `jnpf-web-vue3/src/views/studio/composables/useAnalystSkill.ts`
- `jnpf-web-vue3/src/views/studio/composables/useDesignSkills.ts`
- `jnpf-web-vue3/src/views/studio/composables/useDeveloperSkill.ts`
- … 另有 126 个
## 归档状态
- archiveStatus: **pending**
- mistakeOk: false
- focusOk: false
- registryOk: false
## 机器归档
- [x] `.cursor/CURRENT-FOCUS.md`（hook 自动）
- [x] `docs/progress-registry.yaml` session_log（hook 自动）
- [x] `.claude/memory/mistake-log.md` M0xx 占位（hook 自动）
- [ ] 可选：人工润色根因/修复语义