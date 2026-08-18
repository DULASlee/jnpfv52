# studio-clarification — Delta（2026-07-17）

## ADDED: 新 PM 4 步流水线下的澄清续跑

当 `RequirementAnalysisOrchestrator.RunPmPipelineAsync` 为唯一需求分析主链时：

### 出题路径（步骤③ deepen）

- 若 `ClarificationAnswered` 轮次 `< MinPmOptimizationRounds`（默认 2）且步骤③ LLM 返回 `completed`：
  - **MUST** 调用 `GenerateClarificationAsync(..., forceQuestions: true)` 产出结构化题集；
  - **MUST NOT** 递归重跑 `RefineFromAnalysisStreamAsync` 并期望 LLM 自发输出 `===META=== pending_question`。

### 作答后续跑

- 用户经 `POST /api/studio/skills/clarification/{pipelineId}/answer` 提交后，前端 `nextAction=continue-requirement-analysis` 触发 `requirement-analysis/run`。
- 编排器 **MUST** 在以下条件成立时续跑步骤③（而非步骤①）：
  - `IR1_SaNineView` stable；
  - `RequirementRefined` 事件已存在；
  - `clarification:requirement:{projectId}` fragment **stable** 且 payload 含 `answersText`；
  - 无 in-progress 澄清 fragment。
- 续跑时 **MUST**：
  1. `ApplyClarificationAnswersToSkeletonAsync` 写回骨架；
  2. 将 `answersText` 并入 `enhancedText` 再调 `RunStep3RefineAsync`；
  3. SSE 推送 `📋 已收到第 N 轮澄清作答…`。
- 当 `ClarificationAnswered` 轮次 `≥ MinPmOptimizationRounds` 且其余前置满足时 → `RenderSpecAndWaitConfirmAsync`（步骤④）。

### 前端

- `onClarificationAnswered` 在 `continue-requirement-analysis` 时 **MUST** 设 `loading=true` 并 `readSseStream` 直至编排器推送完成或下一轮 `clarification_requested`。

## ADDED: LLM 长等待 heartbeat

步骤①③ token 流与步骤② PSpec 增强期间，每 15s 推送一条 `thinking` SSE，避免折叠区长时间无更新。
