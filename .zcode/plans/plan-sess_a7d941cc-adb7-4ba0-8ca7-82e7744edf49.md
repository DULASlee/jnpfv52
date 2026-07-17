# ADF Plan：修复三轮澄清→需求分析文档数据流三缺口

## 概述

三轮追踪需求细化（澄清 Q&A）存在三个数据流断点，导致用户作答无法完整修订进最终需求分析文档。本计划按 ADF 流程（架构→设计→接口→实现）逐个修复，每缺口完成一个节点后等用户审批再推进下一缺口。

---

## Gap 1（最高优先级）：第 N 轮澄清答案未修改需求骨架（Round 2+ 答案被丢弃）

### P1 — Architecture

| 维度 | 方案 |
|------|------|
| **层边界** | 修复位于 `RequirementAnalysisOrchestrator`（编排层），不触及 PM Skill / Analyst Skill / IR EventStore |
| **唯一源** | 骨架修改唯一源 = `PmSkillService.RefineSkeletonFromClarificationAsync`（已存在，含确定性基线 + LLM 补丁）。修复仅补全该方法的调用时机，不引入第二修改路径 |
| **方案对比** | **A（采纳）：** 在 `RunRoundAsync` 的前置条件检查之后（line ~498）插入对前一已稳定轮次的 `ApplyClarificationAnswersToSkeletonAsync` 调用，随后刷新 snapshot。最小侵入，复用已有方法。<br>**B（否决）：** 修改 `DetermineCurrentRound` 使其不跳过 Stable 轮次 → 破坏幂等语义、改变编排器核心行为，风险过高 |
| **失败边界** | `ApplyClarificationAnswersToSkeletonAsync` 内部已有 LLM 失败降级（仅基线补丁），不阻断流程。snapshot 刷新失败走既有异常传递 |
| **不改的部分** | `DetermineCurrentRound` 保持「跳过已 Stable 轮次」语义；`ApplyClarificationAnswersToSkeletonAsync` 方法体不变；PM LLM prompt 不变 |

### P2 — Design

- **SkillHarness 模式：** 编排器内部直接调用 `ApplyClarificationAnswersToSkeletonAsync`（已有 private 方法，非 Skill 调用链）
- **Gate 模式：** 不新增 Gate。既有 Round 3 的 PM 终评门控（CR-20260712-01 修复后）保持不变
- **IR 模式：** 不新增 IR 事件。骨架修改通过 `SkeletonCreated`/`SkeletonUpdated`（已有事件）覆盖保存，由 `ApplyClarificationAnswersToSkeletonAsync` 内部写入

### P3 — Interface

**方法签名（不变）：**

```csharp
// 已有方法，签名不变
private async Task ApplyClarificationAnswersToSkeletonAsync(
    Guid pipelineId, string tenantId, string projectId,
    RequirementAnalysisSnapshot snapshot,
    ClarificationEvent prevClar, int round, CancellationToken ct)
```

**插入位置（`RunRoundAsync`，line ~492-498，前置条件检查之后）：**

```csharp
// 伪代码 —— 插入点
// 当前行 492-498: 检查上一轮是否有 ClarificationAnswered（前置条件）
if (prevClar != null)
{
    // 【新增 GAP1】先写入骨架（复用已有方法）
    await ApplyClarificationAnswersToSkeletonAsync(
        pipelineId, tenantId, projectId, snapshot, prevClar, round - 1, ct);
    // 刷新 snapshot（骨架已变更）
    snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
}
// 继续原有逻辑：用 answersText 生成本轮问题
```

**无新 DTO / 无新 IR 事件类型 / 无新接口方法。**

### P4 — Implementation

| 步骤 | 操作 | 验证 |
|------|------|------|
| 1 | 在 `RequirementAnalysisOrchestrator.cs` `RunRoundAsync` 方法中，前置条件检查之后插入上述代码块 | `dotnet build` 0 错误 |
| 2 | 审视插入后的上下文，确保 snapshot 刷新后下游代码使用新 snapshot | 人工逐行审视 |
| 3 | 运行 xUnit 测试 | `dotnet test --filter "RequirementAnalysisOrchestrator"` 全绿 |
| 4 | **用户审批 → 进入 Gap 2** | |

---

## Gap 2（次高优先级）：`FinalizeAsync` 缺少 LLM 对澄清答案 vs 编译规范的完整性审查

### P1 — Architecture

| 维度 | 方案 |
|------|------|
| **层边界** | 修复位于 `AnalystSkillService.FinalizeAsync`（分析师层），新增私有方法 `ReviewClarificationAnswersAgainstSpecAsync` |
| **唯一源** | 编译规范（`PreAnalysisModel`）为审查对照源；澄清答案（多轮汇总）为审查输入；审查结果仅作追加内容（附录 F），不修改编译规范本体 |
| **方案对比** | **A（采纳）：** 在 `FinalizeAsync` 渲染文档前（line ~607-610 之间）插入 LLM 审查调用。使用直接 `Llm.ChatAsync`（匹配 `EnrichSkeletonViaSemanticAnalysisAsync` line 418 既有模式），非 `ISkillLlmBudgetGuard`。<br>**B（否决）：** 注入 `ISkillLlmBudgetGuard` 改造 `FinalizeAsync` → `AnalystSkillService` 构造函数需变更，影响面大且与既有 `EnrichSkeletonViaSemanticAnalysisAsync` 不一致 |
| **失败边界** | LLM 审查失败 → WARNING 日志 + 跳过（文档不含审查结果，但附录 E 仍含答案原文）。不阻断 Finalize |
| **不改的部分** | `FinalizeAsync` 确定性管线不变；`LoadRequirementClarificationAppendicesAsync` 不变（Gap 3 单独修）；PM 终评门控不变 |

### P2 — Design

- **Skill 模式：** `AnalystSkillService` 已是 `CognitiveSkill` 子类，直接使用 `Llm.ChatAsync`（继承自 `CognitiveSkill`），匹配 line 418 既有模式。不使用 Budget Guard（一致性优先于成本控制）
- **IR 模式：** 可选新增 `ClarificationSpecReviewCompleted` IR 事件，用于留存审查结果审计追踪。不强依赖 IR 写入——失败不影响 Finalize
- **Render 模式：** 审查结果通过新增附录 F 或追加段落渲染进需求文档 Markdown

### P3 — Interface

**新增 Record 类型：**

```csharp
internal sealed record ClarificationSpecReviewResult
{
    public bool Executed { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<ClarificationMissedItem> MissedItems { get; init; } = [];
    public string ReviewMarkdown { get; init; } = string.Empty;
}

internal sealed record ClarificationMissedItem
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Suggestion { get; init; } = string.Empty;
}
```

**新增私有方法签名：**

```csharp
// AnalystSkillService.cs
private async Task<ClarificationSpecReviewResult> ReviewClarificationAnswersAgainstSpecAsync(
    PreAnalysisModel model,
    string fullDocumentMarkdown,
    IReadOnlyList<ClarificationAnswerAppendix> answers,
    SkillContext context,
    CancellationToken ct)
```

**调用位置（`FinalizeAsync`，line ~607-610 之间）：**

```csharp
var reviewResult = await ReviewClarificationAnswersAgainstSpecAsync(
    model, fullDoc, clarificationAnswers, skillContext, ct);
if (reviewResult.Executed && !string.IsNullOrWhiteSpace(reviewResult.ReviewMarkdown))
    fullDoc = InjectReviewAppendix(fullDoc, reviewResult);
```

### P4 — Implementation

| 步骤 | 操作 | 验证 |
|------|------|------|
| 1 | 新增 `ClarificationSpecReviewResult` / `ClarificationMissedItem` record | `dotnet build` |
| 2 | 实现 `ReviewClarificationAnswersAgainstSpecAsync`（LLM prompt：对照片段 vs 澄清答案输出遗漏/不一致项） | `dotnet build` |
| 3 | 在 `FinalizeAsync` 插入审查调用 + 文档注入 | `dotnet build` |
| 4 | xUnit | `dotnet test --filter "AnalystSkillService|RequirementAnalysisOrchestrator"` |
| 5 | **用户审批 → 进入 Gap 3** | |

---

## Gap 3（最低优先级）：附录 E 缺少跨轮次语义去重

### P1 — Architecture

| 维度 | 方案 |
|------|------|
| **方案对比** | **A（采纳）：** 确定性文本去重（不调 LLM）。Round N+1 answersText 包含 Round N → Round N 标注「已涵盖于第 N+1 轮」并折叠。<br>**B（否决）：** LLM 语义去重 → 成本高、引入不确定性，与 Gap 3 重要性不匹配 |
| **失败边界** | 去重失败 → 退化为全量渲染（当前行为） |

### P2 — Design

- **数据结构：** `ClarificationAnswerAppendix` 增 `ResolvedByLaterRound: int?` 字段
- **Render 模式：** `RenderAppendixE` 检测该字段，渲染为折叠标注

### P3 — Interface

**修改 Record：**
```csharp
internal sealed record ClarificationAnswerAppendix
{
    // ... 现有字段 Stage/Round/AnswersText
    public int? ResolvedByLaterRound { get; init; } // 【新增】null=未被后续涵盖
}
```

**新增方法：**
```csharp
private static List<ClarificationAnswerAppendix> DeduplicateAcrossRounds(
    IReadOnlyList<ClarificationAnswerAppendix> answers)
// 逻辑：对于 j>i，若 answers[i] 文本含于 answers[j] 文本 80%+，标 ResolvedByLaterRound=j
```

### P4 — Implementation

| 步骤 | 操作 | 验证 |
|------|------|------|
| 1 | `ClarificationAnswerAppendix` 增字段 | `dotnet build` |
| 2 | 实现 `DeduplicateAcrossRounds` + 接入 `LoadRequirementClarificationAppendicesAsync` | `dotnet build` |
| 3 | `RenderAppendixE` 折叠标注逻辑 | `dotnet build` |
| 4 | xUnit 全量回归 | `dotnet test --filter "AnalystSkillService|RequirementDocumentRenderer"` |
| 5 | **用户审批** | |

---

## 实施顺序与门控

| 序号 | 缺口 | 预计改动量 | 门控 |
|------|------|-----------|------|
| **1** | Gap 1 — Round N 答案写入骨架 | ~5 行插入，0 新类型 | `dotnet build` + xUnit 绿 → **用户审批** |
| **2** | Gap 2 — LLM 审查澄清 vs 规范 | ~100 行新方法 + ~10 行插入 + 2 record | `dotnet build` + xUnit 绿 → **用户审批** |
| **3** | Gap 3 — 附录 E 跨轮去重 | ~40 行新方法 + ~5 行调用 + 1 字段 | `dotnet build` + xUnit 绿 → **用户审批** |

每缺口完成后 **必须暂停等用户「继续/通过」** 才能进入下一缺口。

---

## 不改的部分（防止越权）

- 不改 `DetermineCurrentRound` 核心语义
- 不改 `PmSkillService` 的评分/Refine 逻辑
- 不改 `ArchitectSkillService` / `SystemDesignClarificationSkill`（已正确使用 answersText）
- 不改 CR-20260712-01 PM 终评门控修复
- 不改 `ISkillLlmBudgetGuard` 注入 AnalystSkillService
- 不改 `FinalizeAsync` 确定性管线顺序