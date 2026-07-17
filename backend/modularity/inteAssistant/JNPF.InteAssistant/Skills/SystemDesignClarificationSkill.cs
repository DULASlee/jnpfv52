using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 总体设计澄清 Skill（ADR-005 P3）— 两阶段：
///
/// 阶段一（提问）：无 stable 的 IR1_Clarification(system-design) fragment
///   → 调 LLM 生成总体设计澄清题（部署架构/集成方式/非功能/技术栈）
///   → yield ClarificationRequested（暂停 Skill，等用户作答）
///
/// 阶段二（约束引擎 + 锁定）：有 stable Clarification
///   → 读 answersText → 调约束引擎 → 产出 SystemDesignLocked（payload 含 assumptions 留痕）
///   → critical 违规则产出 ConstraintViolationReported 并拒绝锁定
///
/// 设计意图：SystemDesign 本体（SystemDesignSkillService）保持纯约束引擎不动，
/// 本 Skill 在"有澄清需求"时自包含完成提问 + 锁定；SystemDesignSkillService 作为"无澄清路径"保留。
/// </summary>
public sealed class SystemDesignClarificationSkill : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IConstraintEngineService _constraintEngine;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ILogger<SystemDesignClarificationSkill> _logger;

    public SystemDesignClarificationSkill(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard,
        IConstraintEngineService constraintEngine,
        IPipelineSseChannelHub sseHub,
        EntityDesignRepository entityDesignRepo,
        ILogger<SystemDesignClarificationSkill> logger)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
        _constraintEngine = constraintEngine;
        _sseHub = sseHub;
        _entityDesignRepo = entityDesignRepo;
        _logger = logger;
    }

    public override string SkillId => DesignSkillIds.SystemDesignClarification;
    public override string Version => "1.0.0-clarification";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.RefineSpecification;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        // 与 SystemDesignSkillService 一致：三片段 stable 才能进入总体设计
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.DDL,
            IrFragmentTypes.FormPageIR,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.ClarificationRequested,
            IrEventTypes.SystemDesignLocked,
            IrEventTypes.ConstraintViolationReported,
            IrEventTypes.SystemDesignClarificationCompleted,
        },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        // 复用 SystemDesignSkillService 的输入校验
        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.SystemDesign, IrStabilityStates.Locked) != null)
            return Task.FromResult(SkillValidationResult.Fail("SystemDesign 已 locked"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var fragmentId = $"systemDesign:{context.ProjectId}";

        // ── ADR-005 两阶段：检查是否已有 stable 的总体设计澄清片段 ──
        var clarificationFragment = context.Snapshot.Find(
            IrFragmentTypes.Clarification, IrStabilityStates.Stable);

        var sysClarification = clarificationFragment is { } f
            && f.FragmentId.StartsWith($"clarification:{ClarificationStages.SystemDesign}:", StringComparison.Ordinal)
                ? f
                : null;

        if (sysClarification == null)
        {
            // 阶段一：生成总体设计澄清提问，暂停 Skill
            var clarificationSet = await GenerateSystemDesignClarificationAsync(context, ct);
            var clarFragmentId = $"clarification:{ClarificationStages.SystemDesign}:{context.ProjectId}";

            _sseHub.TryPush(context.PipelineId, "clarification_requested",
                JsonSerializer.Serialize(clarificationSet, JsonOptions));

            _logger.LogInformation(
                "SystemDesignClarification 阶段一：发出澄清提问 round={Round} questions={Count} pipelineId={Id}",
                clarificationSet.Round, clarificationSet.Questions.Count, context.PipelineId);

            yield return new AppendIrEventRequest
            {
                EventType = IrEventTypes.ClarificationRequested,
                FragmentId = clarFragmentId,
                FragmentType = IrFragmentTypes.Clarification,
                FragmentVersion = clarificationSet.Round,
                Payload = JsonSerializer.Serialize(clarificationSet, JsonOptions),
                SkillId = SkillId,
            };
            yield break; // 暂停：等用户作答后重跑（阶段二）
        }

        // 阶段二：读用户答案，调约束引擎，产出 SystemDesignLocked（含 assumptions 留痕）
        var answersText = ExtractAnswersText(sysClarification.Payload);

        // 25 §6：锁定前校验 ai_entity_field 投影存在（字段唯一源）— 与 SystemDesignSkillService 一致
        var fieldCount = await _entityDesignRepo.CountFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        if (fieldCount == 0)
            throw Oops.Bah("SystemDesignClarification Skill: ai_entity_field 无字段，拒绝锁定（须先 Round 3 Finalize 投影）");

        // 先投 SystemDesignClarificationCompleted（携带 answersText，留痕 + 作为阶段二信号）
        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SystemDesignClarificationCompleted,
            FragmentId = $"sysDesignClarification:{context.ProjectId}",
            FragmentType = IrFragmentTypes.SystemDesign,
            FragmentVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                projectId = context.ProjectId,
                answersText = string.IsNullOrWhiteSpace(answersText) ? "（用户未补充）" : answersText,
                completedAt = DateTime.UtcNow.ToString("O"),
            }, JsonOptions),
            SkillId = SkillId,
        };

        var check = _constraintEngine.Evaluate(context.Snapshot);
        _logger.LogInformation(
            "SystemDesignClarification 阶段二：约束校验 project={ProjectId} critical={Critical} warning={Warning}",
            context.ProjectId, check.CriticalCount, check.WarningCount);

        if (check.Violations.Count > 0)
            yield return BuildViolationEvent(context.ProjectId, check);

        if (check.CriticalCount > 0)
            throw Oops.Bah($"存在 {check.CriticalCount} 条 critical 约束违规，SystemDesignLocked 已拒绝");

        var arch = context.Snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)!;
        var ddl = context.Snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)!;
        var ui = context.Snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)!;

        // P9-S1：从上游 IR 确定性派生状态机/工作流/菜单（零 LLM，纯编译派生）
        // 与 SystemDesignSkillService 一致，确保澄清路径产出完整结构化设计
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.Skeleton);
        var (stateMachines, workflowNodes, menus) = SystemDesignSkillService.DeriveStructuredDesign(skeleton, ui);

        var payload = JsonSerializer.Serialize(new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            lockedAt = DateTime.UtcNow.ToString("O"),
            references = new
            {
                architectureFragmentId = arch.FragmentId,
                ddlFragmentId = ddl.FragmentId,
                formPageFragmentId = ui.FragmentId,
            },
            consistencyChecks = new object[]
            {
                new { check = "fragments-present", passed = true },
                new { check = "constraint-engine", passed = true, warningCount = check.WarningCount },
                new { check = "clarification-answered", passed = true },
            },
            // P9-S1 新增：结构化设计内容（编译器据此生成工作流 JSON + 菜单注册）
            stateMachines,
            workflowNodes,
            menus,
            // ADR-005 P3：用户对总体设计的澄清作答留痕（约束引擎不读 prompt，但答案写入 payload 供审计回放）
            assumptions = string.IsNullOrWhiteSpace(answersText) ? null : answersText,
            stabilityState = IrStabilityStates.Locked,
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SystemDesignLocked,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.SystemDesign,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };
    }

    /// <summary>从 ClarificationAnswered payload 提取人可读的答案汇总文本。</summary>
    private static string ExtractAnswersText(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("answersText", out var el)
                && el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? string.Empty;
        }
        catch (JsonException) { /* 损坏 payload，返回空串（仅文本展示用） */ }
        return string.Empty;
    }

    /// <summary>
    /// 阶段一：调 LLM 生成总体设计澄清选择题（部署架构/集成/非功能/技术栈）。
    /// </summary>
    private async Task<ClarificationSet> GenerateSystemDesignClarificationAsync(
        SkillContext context, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 总体设计师。架构/DDL/UI 三片段已稳定，在锁定 SystemDesign 前，
            识别总体设计层必须澄清的歧义点，输出结构化交互问答 JSON（让用户通过选项确认）。

            必须输出 JSON：
            {
              "title": "本轮标题",
              "intro": "为何要问这些",
              "questions": [
                {
                  "id": "q1",
                  "text": "问题文本",
                  "type": "single",
                  "questionFormat": "SINGLE",
                  "contextHint": "为什么问这个问题",
                  "defaultOption": "o1",
                  "required": true,
                  "options": [
                    {"id":"o1","label":"选项A"},
                    {"id":"o2","label":"选项B"},
                    {"id":"o_other","label":"其他","freeText":true}
                  ]
                }
              ]
            }

            规则：
            - 聚焦总体设计层歧义：集成方式（API/消息/文件）、非功能需求（并发/响应/数据量/可用性）、部署拓扑（单机/集群/容灾）、安全策略、数据迁移
            - questions 数量 3-5 个
            - 每个 question.options 数量 3-5 个
            - 每个 question.options 末项必须是 {"id":"o_other","label":"其他","freeText":true}
            - type=single（单选）/multi（多选）；不用 text
            - required=true 的关键题不超过 2 个
            - id 用 q1/q2/...，option id 用 o1/o2/.../o_other
            - 新增字段（P9）：contextHint（为什么问）、defaultOption（默认值 option id）、questionFormat（SINGLE|MULTI|MATRIX_SINGLE|MATRIX_MULTI）
              如果问题是对「多个集成点/模块」做同一维度的决策，使用 MATRIX_SINGLE 并输出 matrixSubItems：[{"rowId","rowLabel"}]
            - 只输出 JSON
            """;

        var userPrompt = $"""
            用户需求：
            {context.UserRequirement}

            IR-2 上下文（架构/DDL/UI 已稳定）：
            {context.PromptContext.CompressedSummary}

            projectId={context.ProjectId}
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            MaxTokens = 1500,
            Temperature = 0.3,
            ResponseFormat = "json",
            MaxRetries = 1,
            TimeoutMs = 30000,
        };

        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);
        try
        {
            var response = await _budgetGuard.ExecuteAsync(slot, request, ct);
            if (!response.IsSuccess)
            {
                // 硬错误：LLM 失败即抛，禁止兜底默认题
                throw Oops.Bah($"总体设计澄清问答 LLM 失败/零题: {response.Error ?? "(无错误详情)"} pipeline={context.PipelineId} tenantId={context.TenantId}");
            }

            var json = PmSkillService.ExtractJson(response.Content);
            ClarificationDraft? draft = null;
            try
            {
                draft = JsonSerializer.Deserialize<ClarificationDraft>(json, JsonOptions);
            }
            catch (Exception jex)
            {
                throw Oops.Bah($"总体设计澄清问答 LLM 失败/零题: JSON 解析失败 {jex.Message} pipeline={context.PipelineId} tenantId={context.TenantId}");
            }

            var set = BuildSystemDesignClarificationSet(draft ?? new ClarificationDraft());
            if (set.Questions.Count == 0)
            {
                // 硬错误：LLM 成功但零题也视为失败
                throw Oops.Bah($"总体设计澄清问答 LLM 失败/零题: LLM 未产出任何有效问题 pipeline={context.PipelineId} tenantId={context.TenantId}");
            }
            return set;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            // 硬错误：异常即抛（FriendlyException 透传，其余包装为业务错误）
            if (ex is JNPF.FriendlyException.AppFriendlyException) throw;
            _logger.LogError(ex, "总体设计澄清提问异常 pipeline={PipelineId}", context.PipelineId);
            throw Oops.Bah($"总体设计澄清问答 LLM 失败/零题: {ex.Message} pipeline={context.PipelineId} tenantId={context.TenantId}");
        }
        finally
        {
            _budgetGuard.ReleaseRun(context.RunId, SkillId);
        }
    }

    private static ClarificationSet BuildSystemDesignClarificationSet(ClarificationDraft draft)
    {
        var questions = new List<ClarificationQuestion>();
        var requiredCount = 0;

        foreach (var dq in (draft.Questions ?? new()).Take(5))
        {
            var type = NormalizeType(dq.Type);
            var required = dq.Required && requiredCount < 2;
            if (required) requiredCount++;

            var options = BuildOptions(type, dq.Options);
            if (options.Count < 2) continue;

            var qId = EnsureId(dq.Id, "q", questions.Count);
            questions.Add(new ClarificationQuestion
            {
                Id = qId,
                Text = string.IsNullOrWhiteSpace(dq.Text) ? $"总体设计问题 {questions.Count + 1}" : dq.Text,
                Type = type,
                Required = required,
                Options = options,
            });

            if (questions.Count >= 5) break;
        }

        if (questions.Count == 0)
            throw Oops.Bah("总体设计澄清问答 LLM 失败/零题: LLM 草案未产出任何有效问题");

        return new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = ClarificationStages.SystemDesign,
            Round = 1,
            Title = string.IsNullOrWhiteSpace(draft.Title) ? "总体设计澄清" : draft.Title,
            Intro = string.IsNullOrWhiteSpace(draft.Intro)
                ? "以下问题影响总体设计锁定，请逐题确认。每题最后一项为「其他」，可自由补充。"
                : draft.Intro,
            Questions = questions,
            AllowSkipNonCritical = true,
        };
    }

    private static AppendIrEventRequest BuildViolationEvent(string projectId, ConstraintCheckResult check)
    {
        var payload = JsonSerializer.Serialize(new
        {
            checkedAt = DateTime.UtcNow.ToString("O"),
            criticalCount = check.CriticalCount,
            warningCount = check.WarningCount,
            violations = check.Violations.Select(v => new
            {
                v.RuleId,
                v.Severity,
                v.Message,
                v.FragmentType,
                v.FragmentId,
            }),
        }, JsonOptions);

        return new AppendIrEventRequest
        {
            EventType = IrEventTypes.ConstraintViolationReported,
            FragmentId = $"constraints:{projectId}",
            FragmentType = "IR2_ConstraintReport",
            FragmentVersion = 1,
            Payload = payload,
            SkillId = DesignSkillIds.SystemDesignClarification,
        };
    }

    private static string NormalizeType(string? raw)
        => (raw ?? "").Trim().ToLowerInvariant() switch
        {
            "multi" or "multiple" or "checkbox" => "multi",
            "text" or "textarea" or "freeform" => "text",
            _ => "single",
        };

    private static List<ClarificationOption> BuildOptions(string type, List<ClarificationOption>? draft)
    {
        if (type == "text")
            return new List<ClarificationOption>
            {
                new() { Id = "o_other", Label = "其他", FreeText = true },
            };

        var opts = new List<ClarificationOption>();
        for (var i = 0; i < (draft?.Count ?? 0) && opts.Count < 4; i++)
        {
            var d = draft![i];
            if (string.IsNullOrWhiteSpace(d.Label)) continue;
            if (d.FreeText) continue;
            var oid = EnsureId(d.Id, "o", opts.Count);
            opts.Add(new ClarificationOption { Id = oid, Label = d.Label!.Trim(), FreeText = false });
        }

        if (opts.Count < 2)
            return new List<ClarificationOption>();

        opts.Add(new ClarificationOption { Id = "o_other", Label = "其他", FreeText = true });
        return opts;
    }

    private static string EnsureId(string? raw, string prefix, int index)
    {
        var id = string.IsNullOrWhiteSpace(raw) ? "" : System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(id) ? $"{prefix}{index + 1}" : id;
    }

    /// <summary>LLM 产出的澄清草案（内部反序列化用）。</summary>
    private sealed class ClarificationDraft
    {
        public string Title { get; set; } = "";
        public string Intro { get; set; } = "";
        public List<ClarificationQuestion> Questions { get; set; } = new();
    }
}
