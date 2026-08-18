# Tasks: add-interactive-clarification-qa

## 1. P1 — 需求分析阶段交互提问

- [x] `IrEventTypes.cs` 新增 `ClarificationRequested` / `ClarificationAnswered` + `IR1_Clarification` fragment + `ClarificationStages`
- [x] `ClarificationDtos.cs`（新建）Question/Answer/Set/Request/Result DTO
- [x] `RequirementGateService` `MaturityResult.Clarifications` 扩展 + `EvaluateMaturity` prompt 升级 + `BuildClarificationSet`（含不变量校验 + fallback）
- [x] `AIDevelopmentPipelineService` 插入提问决策（mode∈{explore,confirm} 投事件暂停）+ `Clarification:MaxRounds` 配置
- [x] `SkillsApiService.AnswerClarificationAsync` 关键题硬门控 + 答案存对话历史
- [x] 前端 `ClarificationCard.vue` + `useClarification.ts` + `skills.ts` API + `AiChatPanel.vue` SSE 分支

## 2. P2 — 架构设计阶段交互提问

- [x] `IrProjectionEngine.UpsertClarificationAsync`（Requested→in-progress / Answered→stable）
- [x] `ArchitectSkillService` 两阶段改造：ThinkAsync 检查 stable Clarification + ValidateOutput 放宽 + Outputs 声明 + 答案注入 userPrompt
- [x] ArchitectSkillService 注入 `IPipelineSseChannelHub` 推 `clarification_requested`
- [x] `AnswerClarificationAsync` 扩展 architecture stage（`nextAction=rerun-architect`）
- [x] 前端 `onClarificationAnswered` 适配（`runArchitectSkill` + readSseStream）

## 3. P3 — 总体设计阶段交互提问

- [x] `IrEventTypes.SystemDesignClarificationCompleted` + `DesignSkillIds.SystemDesignClarification`
- [x] `SystemDesignClarificationSkill`（新建，两阶段：提问 + 阶段二约束引擎 + SystemDesignLocked assumptions 留痕）
- [x] `IrProjectionEngine` 注册 `SystemDesignClarificationCompleted`（留痕 null）
- [x] `AnswerClarificationAsync` 扩展 system-design stage（`nextAction=rerun-system-design-clarification`）
- [x] `DesignSkillsApiService` 新增 `system-design-clarification/{id}/run` 端点
- [x] 前端 `designSkills.ts` + `onClarificationAnswered` 适配

## 4. P4 — 规范层 + 端到端文档

- [x] openspec change 提案（proposal.md / tasks.md / design.md）
- [x] change spec 草稿 `specs/studio-clarification/spec.md`
- [x] `openspec/adr/ADR-005-interactive-clarification-qa.md`
- [x] `.claude/rules/studio-clarification.md` + `.cursor/rules/studio-clarification.mdc`
- [x] `CLAUDE.md` + `AGENTS.md` 追加 ADR-005 段
- [ ] 跑 `update-openspec-index.mjs` 更新 `specs/README.md` 索引
- [ ] 运行时端到端验证（pipeline 311）
- [ ] 归档（`/opsx:archive`）
