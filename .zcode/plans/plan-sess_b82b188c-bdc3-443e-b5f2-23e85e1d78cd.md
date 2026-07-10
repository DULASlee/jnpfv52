## 交互式澄清矩阵题 UI 基础设施 — 端到端实施计划

### 背景
用户反馈：AI原生开发中产品经理和需求分析师Skills追问需求时，输出的是纯文本，未动态渲染为多选题和矩阵题。

### 根因分析
后端 DTO 已定义 `QuestionFormat`（SINGLE/MULTI/MATRIX_SINGLE/MATRIX_MULTI）和 `MatrixSubItem[]`，但：
1. **前端无矩阵渲染** — ClarificationCard.vue 仅处理 `type: single/multi/text`
2. **LLM prompt 未指令模型输出矩阵格式** — 4 个 prompt 位置均未提及 `questionFormat` / `matrixSubItems`
3. **后端 answer DTO 缺矩阵行答字段** — `ClarificationAnswer` 无 `MatrixRowAnswers`
4. **后端格式化/校验未处理矩阵行**

---

## 实施计划（7 阶段，22 步骤）

### 阶段 A：TypeScript 类型对齐（`skills.ts`）
- **A1** 新增 `MatrixSubItem` 接口：`{ rowId, rowLabel, selectedOption?, freeText? }`
- **A2** 扩展 `ClarificationQuestion`：增加 `questionFormat?`（SINGLE/MULTI/MATRIX_SINGLE/MATRIX_MULTI）、`matrixSubItems?`、`contextHint?`、`defaultOption?`
- **A3** 扩展 `ClarificationAnswer`：增加 `matrixRowAnswers?: MatrixSubItem[]`

### 阶段 B：后端 DTO 扩展（`ClarificationDtos.cs`）
- **B1** `ClarificationAnswer` 增加 `List<MatrixSubItem>? MatrixRowAnswers`

### 阶段 C：前端矩阵渲染（`ClarificationCard.vue`）
- **C1** 矩阵检测：`isMatrixQuestion(q)` → `questionFormat?.startsWith('MATRIX_')`
- **C2** 矩阵表格模板：手写 `<table class="matrix-table">`（thead: 行标签列 + 选项列；tbody: 每 MatrixSubItem 一行）
- **C3** MATRIX_SINGLE：每格 `<a-radio :checked + @change>`（每行独立单选）
- **C4** MATRIX_MULTI：每格 `<a-checkbox :checked + @change>`（每行独立多选）
- **C5** "其他"列：选中 `freeText=true` 的选项时展示 `<a-input>`
- **C6** 状态管理：`matrixAnswers` 响应式 Map `{ [qId]: { [rowId]: { selectedOption?, freeText? } } }`
- **C7** `buildAnswers()` 改造：矩阵题产出含 `matrixRowAnswers` 的 `ClarificationAnswer`
- **C8** `canSubmit` 改造：矩阵必答题至少一行有选中
- **C9** `contextHint` 渲染：问题文本旁 `<a-tooltip>` ℹ️ 图标
- **C10** `defaultOption` 预选：mount 时初始化 `answersMap` / `matrixAnswers` 的默认值

### 阶段 D：后端校验 + 格式化（`SkillsApiService.cs`）
- **D1** 矩阵行校验：在 `AnswerClarificationAsync` 现有 OptionIds 校验之后，验证 `MatrixRowAnswers[].SelectedOption` ∈ 合法选项集
- **D2** 矩阵行格式化：在 `FormatAnswersAsUserMessage` 中添加矩阵行文本（"逐行作答：\n  - RowLabel：选项（补充：FreeText）"）

### 阶段 E：LLM Prompt 改造（4 个文件，6 个 prompt 位置）
全部 prompt 的输出 schema 增加 `questionFormat`、`matrixSubItems`、`contextHint`、`defaultOption`，并附加矩阵使用规则：
> "如果问题覆盖 2+ 个事件/实体的同一决策维度 → 使用矩阵格式（MATRIX_SINGLE/MATRIX_MULTI）并输出 matrixSubItems 数组。单事件/实体 → SINGLE/MULTI。"

| 步骤 | 文件 | 方法 |
|------|------|------|
| E1 | `RequirementAnalysisOrchestrator.cs` | `BuildRoundPrompt` Round 1（27号PM Skill） |
| E2 | `RequirementAnalysisOrchestrator.cs` | `BuildRoundPrompt` Round 2（联合精化） |
| E3 | `RequirementAnalysisOrchestrator.cs` | `BuildRoundPrompt` Round 3（最终确认） |
| E4 | `RequirementGateService.cs` | `EvaluateMaturity`（需求门控评估） |
| E5 | `ArchitectSkillService.cs` | `GenerateArchitectureClarificationAsync` |
| E6 | `SystemDesignClarificationSkill.cs` | `GenerateSystemDesignClarificationAsync` |

### 阶段 F：Parser 兜底逻辑（`RequirementAnalysisOrchestrator.cs`）
- **F1** 在 `ParseQuestionsFromLlm` 返回后（line 528），新增 `ApplyMatrixFallback`：若问题文本包含 ≥2 个 `compileResult.EventResults` 中的事件名但无 `MatrixSubItems`，则自动合成矩阵行并升级 `QuestionFormat` → `MATRIX_SINGLE`。这是 LLM 不听话时的保险。

### 阶段 G：验证
- **G1** `pnpm type-check` → 0 errors
- **G2** `dotnet build` → 0 errors, 0 warnings

---

### 涉及文件（8 个）
1. `jnpf-web-vue3/src/views/studio/api/studio/skills.ts`
2. `jnpf-web-vue3/src/views/studio/components/clarification/ClarificationCard.vue`
3. `backend/.../Entitys/Dto/Ir/ClarificationDtos.cs`
4. `backend/.../Skills/SkillsApiService.cs`
5. `backend/.../Skills/RequirementAnalysisOrchestrator.cs`
6. `backend/.../Gates/RequirementGateService.cs`
7. `backend/.../Skills/ArchitectSkillService.cs`
8. `backend/.../Skills/SystemDesignClarificationSkill.cs`

### 关键设计决策
| 决策 | 理由 |
|------|------|
| 手写 `<table>` 而非 `<a-table>` | 矩阵小（3-5列×N行），手写更灵活控制 per-cell 绑定 |
| `a-radio` 独立使用（非 `a-radio-group`）| 每行独立单选，`:checked`+`@change` 完全受控 |
| 兜底：文本匹配事件名（`OrdinalIgnoreCase`）| 容错 LLM 大小写不一致 |
| Prompt 保留 `type` 字段 + 新增 `questionFormat` | 向后兼容，新解析器优先读 `questionFormat` |
| `contextHint` 用 `<a-tooltip>` | 不干扰布局，符合现有 ant-design 模式 |
| `defaultOption` 在 mount 时应用 | 用户可覆盖，仅预填默认值 |