using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Entitys.Dto.Ir;

// ════════════════════════════════════════════════════════════════
// 交互式澄清问答 DTO（ADR-005）
//
// 契约层级：本文件是 LLM 产出 / 前端渲染 / IR 事件 payload 的
//           单一信源。三阶段（需求分析 / 架构设计 / 总体设计）共用。
//
// 关键不变量（IrSchemaValidator / SkillsApiService 强制）：
//   1. 每个 ClarificationQuestion.Options 长度 ∈ [3,5]
//   2. 每个 ClarificationQuestion.Options 末项必须 freeText=true（"其他"+文本框）
//   3. ClarificationQuestion.Type ∈ {single, multi, text}
//   4. ClarificationSet.Round ∈ [1,7]
// ════════════════════════════════════════════════════════════════

/// <summary>一轮澄清提问集合（对应一个 ClarificationRequested 事件的 payload）。</summary>
public record ClarificationSet
{
    /// <summary>提问集合唯一标识（UUID）。</summary>
    public string SetId { get; init; } = string.Empty;

    /// <summary>阶段：requirement | architecture | system-design。</summary>
    public string Stage { get; init; } = ClarificationStages.Requirement;

    /// <summary>第几轮提问（1-7）。</summary>
    public int Round { get; init; } = 1;

    /// <summary>本轮提问标题（如"请假时长计算规则"）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>引导文案，说明为何要确认这些问题。</summary>
    public string Intro { get; init; } = string.Empty;

    /// <summary>本轮 3-5 个问题。</summary>
    public List<ClarificationQuestion> Questions { get; init; } = new();

    /// <summary>是否允许跳过非关键题（required=false 的题）。</summary>
    public bool AllowSkipNonCritical { get; init; } = true;
}

/// <summary>单个澄清问题。</summary>
public record ClarificationQuestion
{
    /// <summary>问题 id（setId 内唯一）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>问题文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>题型：single（单选）| multi（多选）| text（纯文本补充）。</summary>
    public string Type { get; init; } = "single";

    /// <summary>是否关键题（true 时硬门控：必须作答才能推进流程）。</summary>
    public bool Required { get; init; }

    /// <summary>可选项（3-5 个，末项必须 freeText=true）。</summary>
    public List<ClarificationOption> Options { get; init; } = new();

    // ── P9 需求分析子链重构新增字段（26 号 §4）──

    /// <summary>为什么问这个问题（减少用户困惑，提高回答质量）。</summary>
    public string? ContextHint { get; init; }

    /// <summary>合理默认值（option id）。PM 能定的行业惯例自动设为默认值。</summary>
    public string? DefaultOption { get; init; }

    /// <summary>问题格式枚举：SINGLE / MULTI / MATRIX_SINGLE / MATRIX_MULTI。</summary>
    public string QuestionFormat { get; init; } = "SINGLE";

    /// <summary>矩阵子项（矩阵题专用：每行一个事件/实体，独立选择）。</summary>
    public List<MatrixSubItem>? MatrixSubItems { get; init; }
}

/// <summary>矩阵题子项——每行对应一个事件或实体，用户独立选择。</summary>
public record MatrixSubItem
{
    /// <summary>行标识（事件 ID 或实体名）。</summary>
    public string RowId { get; init; } = string.Empty;

    /// <summary>行标签（事件名/实体名，展示给用户）。</summary>
    public string RowLabel { get; init; } = string.Empty;

    /// <summary>用户选择的选项 ID。</summary>
    public string? SelectedOption { get; init; }

    /// <summary>用户在文本框中的补充。</summary>
    public string? FreeText { get; init; }
}

/// <summary>问题选项。</summary>
public record ClarificationOption
{
    /// <summary>选项 id（questionId 内唯一）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>选项展示文本。</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>是否为"其他"项（true 时前端展开文本输入框）。</summary>
    public bool FreeText { get; init; }
}

/// <summary>用户对单个问题的作答。</summary>
public record ClarificationAnswer
{
    /// <summary>对应 ClarificationQuestion.Id。</summary>
    public string QuestionId { get; init; } = string.Empty;

    /// <summary>选中的选项 id 列表（single 取 1 个；multi 取多个；text 题可为空）。</summary>
    public List<string> OptionIds { get; init; } = new();

    /// <summary>"其他"项或 text 题的自由文本补充。</summary>
    public string? FreeText { get; init; }
}

/// <summary>用户提交作答的请求体（POST /api/studio/skills/clarification/{id}/answer）。</summary>
public record AnswerClarificationRequest
{
    /// <summary>对应 ClarificationSet.SetId。</summary>
    public string SetId { get; init; } = string.Empty;

    /// <summary>逐题作答列表。</summary>
    public List<ClarificationAnswer> Answers { get; init; } = new();

    /// <summary>用户主动跳过的非关键题 id 列表（required=true 的题不允许出现在此）。</summary>
    public List<string> SkippedQuestionIds { get; init; } = new();

    /// <summary>是否"全部跳过直接分析"（逃生口，对应 ForceRefine）。</summary>
    public bool SkipAll { get; init; }
}

/// <summary>作答结果返回。</summary>
public record AnswerClarificationResult
{
    public string Status { get; init; } = "answered";
    public string SetId { get; init; } = string.Empty;
    public string FragmentId { get; init; } = string.Empty;

    /// <summary>作答后的 fragment 稳定性状态（全部 required 题已答 → stable）。</summary>
    public string StabilityState { get; init; } = IrStabilityStates.Stable;

    /// <summary>下一轮是否触发新的 maturity 评估。</summary>
    public bool TriggerNextRound { get; init; } = true;

    /// <summary>澄清阶段（requirement | architecture | system-design），前端据此决定后续动作。</summary>
    public string Stage { get; init; } = ClarificationStages.Requirement;

    /// <summary>
    /// 作答后前端应执行的下一步动作：
    ///   "re-evaluate"                   — 重新触发 sa-gate（需求阶段，下一轮 maturity 评估）
    ///   "rerun-architect"               — 重新运行 architect-skill（架构阶段，阶段二 ToT）
    ///   "rerun-system-design-clarification"— 重新运行 system-design-clarification-skill（总体设计阶段，阶段二约束引擎+锁定）
    ///   "continue-requirement-analysis" — 续跑三轮需求分析编排器（requirement-analysis-round1/2/3）
    ///   "none"                          — 无后续
    /// </summary>
    public string NextAction { get; init; } = "re-evaluate";
}
