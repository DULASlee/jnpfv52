# ADR-005：交互式澄清问答系统（三阶段）

| 字段 | 内容 |
|------|------|
| 状态 | **已接受** |
| 日期 | 2026-07-06 |
| 决策者 | 架构师 + AI 原生链施工 |
| 关联 | `add-interactive-clarification-qa` change · `studio-clarification` capability · ADR-004（S2 compile 主链） |

## 背景

需求分析阶段 LLM 识别出歧义点（如"请假时长按自然日还是工作日""调休余额来源""分管副总规则映射"）时，现有 `RequirementGateService` 已在 `MaturityResult.Missing` / `NextQuestion` 字段产出结构化信息，但三模式 prompt 把它们降级成 markdown 文本（`❓` 前缀、`## 待确认事项` 段落）。前端 `AiChatPanel` 用 `marked` 渲染后，用户只能"读 → 整体确认"，**没有逐条作答的单选/多选/文本交互**。

架构设计（`ArchitectSkillService`）与总体设计（`SystemDesignSkillService`）阶段更无任何提问机制 —— 前者直接 ToT 生成架构决策，后者纯约束引擎锁定。

后果：需求歧义带病进入下游设计，返工成本高。

## 决策

### 1. 三阶段差异化实现，统一 IR 事件契约

新增 `ClarificationRequested` / `ClarificationAnswered` 两类 IR 事件（+ P3 的 `SystemDesignClarificationCompleted` 留痕事件），三阶段共用 `ClarificationSet` Schema，但暂停/恢复机制按阶段特点差异化：

| 阶段 | 暂停/恢复 | 理由 |
|------|-----------|------|
| 需求分析 | sa-gate 对话流 `sse.Complete();return` | 已有对话循环，天然支持多轮 |
| 架构设计 | 两阶段 Skill 执行（重跑恢复） | `ThinkAsync` 单次消费，return 即 run 结束 |
| 总体设计 | 两阶段 Skill 执行（重跑恢复） | 同上 |

### 2. 关键题硬门控（required 题必答）

`ClarificationQuestion.Required=true` 的题，用户必须作答才能推进流程。`AnswerClarificationAsync` 遍历 required 题，未作答 `throw Oops.Bah`。这仿 `ConfirmRequirementSpecAsync` 的硬门控范式 —— 用"必须满足"作为推进前置条件。

### 3. 完整 IR 事件化（可审计回放）

提问与作答均落 IR 事件，fragment 状态机：`ClarificationRequested` → in-progress；`ClarificationAnswered` → stable。`IrProjectionEngine.UpsertClarificationAsync` 负责投影。两阶段模式靠 `snapshot.Find(Clarification, Stable)` 判断"已作答"。

### 4. 每题末项恒为"其他"+ 文本框

`BuildClarificationSet` / `BuildOptions` 强制每个 question 的 options 末项为 `{id:"o_other",label:"其他",freeText:true}`。LLM 输出不规范时裁剪 + 补"其他"。前端 `ClarificationCard` 选中"其他"时联动展开文本框。

### 5. 逃生口 + 轮次上限

`ClarificationCard` 底部"全部跳过直接分析"按钮始终可见（对应 ForceRefine 语义）。`Clarification:MaxRounds` 默认 7（可配置），触顶强制 refine，避免无限提问。

## 后果

### 正面

- 用户通过选项而非打字细化需求，降低交互成本
- 关键题硬门控阻止歧义带病进入下游
- IR 事件化支持审计回放，跨阶段复用问答记录
- 三阶段差异化实现，不破坏 compile 主链（提问只在真正调 LLM 的环节插入）
- `SystemDesignSkillService` 本体不动，纯约束引擎确定性不受影响

### 负面

- 设计阶段两阶段 Skill 执行增加一次 run 开销（提问 run + ToT/锁定 run）
- LLM 输出 Question Schema 不稳定时需 fallback 降级（已用默认题兜底）
- 运行时未验证（代码层编译/类型/lint 全过，端到端 pipeline 311 验证待做）
- `SystemDesignClarificationSkill` 需手动触发，未自动接入"三片段 stable 后自动触发"（避免编排器暂停复杂性）

## 相关文件

| 模块 | 路径 |
|------|------|
| 事件常量 | `backend/.../Entitys/Ir/IrEventTypes.cs` |
| DTO 契约 | `backend/.../Entitys/Dto/Ir/ClarificationDtos.cs` |
| 需求 gate | `backend/.../Gates/RequirementGateService.cs` |
| 需求管道 | `backend/.../AIDevelopmentPipelineService.cs` |
| 作答 API | `backend/.../Skills/SkillsApiService.cs` |
| 架构 Skill | `backend/.../Skills/ArchitectSkillService.cs` |
| 总体设计 Skill | `backend/.../Skills/SystemDesignClarificationSkill.cs` |
| 设计 API | `backend/.../Skills/DesignSkillsApiService.cs` |
| 投影引擎 | `backend/.../Ir/IrProjectionEngine.cs` |
| 前端问卷卡 | `jnpf-web-vue3/src/views/studio/components/clarification/ClarificationCard.vue` |
| 前端 composable | `jnpf-web-vue3/src/views/studio/composables/useClarification.ts` |
| 前端 API | `jnpf-web-vue3/src/views/studio/api/studio/skills.ts` · `designSkills.ts` |
| 前端聊天面板 | `jnpf-web-vue3/src/views/studio/components/AiChatPanel.vue` |
| OpenSpec | `openspec/changes/add-interactive-clarification-qa/` · `openspec/specs/studio-clarification/`（待归档） |
| 规则文件 | `.claude/rules/studio-clarification.md` · `.cursor/rules/studio-clarification.mdc` |
