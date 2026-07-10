using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 架构设计 Skill（R3 认知模具版）— BudgetGuard ToT N=3 + kg.score-candidate Top-1；禁止 fallback。
///
/// ADR-005 交互式澄清问答（两阶段执行）：
///   阶段一：无 stable 的 IR1_Clarification fragment → 调 LLM 生成架构澄清题
///           → yield ClarificationRequested（暂停 Skill，等用户作答）
///   阶段二：有 stable Clarification → 读 answersText 注入 userPrompt → 跑 ToT → ArchitectureDecisionRecorded
/// </summary>
public sealed class ArchitectSkillService : CognitiveSkill, ITransient
{
    private const int TotBranchCount = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ILogger<ArchitectSkillService> _logger;

    public ArchitectSkillService(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard,
        IPipelineSseChannelHub sseHub,
        EntityDesignRepository entityDesignRepo,
        ILogger<ArchitectSkillService> logger)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
        _sseHub = sseHub;
        _entityDesignRepo = entityDesignRepo;
        _logger = logger;
    }

    public override string SkillId => DesignSkillIds.Architect;
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.RefineSpecification;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.EventSpec },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.ArchitectureDecisionRecorded,
            // ADR-005：阶段一产出的"暂停态"事件，ValidateOutputAsync 会放行
            IrEventTypes.ClarificationRequested,
        },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 EventSpec 未 stable（设计启动另须 Finalize，见 DesignSkillsApi）"));

        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        // 阶段二：1 条 ArchitectureDecisionRecorded（正常产出）
        if (events.Count == 1 && events[0].EventType == IrEventTypes.ArchitectureDecisionRecorded)
            return Task.FromResult(SkillValidationResult.Ok());

        // 阶段一：1 条 ClarificationRequested（暂停态，等用户作答后重跑）
        if (events.Count == 1 && events[0].EventType == IrEventTypes.ClarificationRequested)
            return Task.FromResult(SkillValidationResult.Ok());

        return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 ArchitectureDecisionRecorded 或 1 条 ClarificationRequested"));
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var fragmentId = $"architecture:{context.ProjectId}";

        // ── ADR-005 两阶段：检查是否已有 stable 的架构澄清片段 ──
        var clarificationFragment = context.Snapshot.Find(
            IrFragmentTypes.Clarification, IrStabilityStates.Stable);

        // 只认 architecture stage 的澄清片段（fragmentId 前缀区分）
        var archClarification = clarificationFragment is { } f
            && f.FragmentId.StartsWith($"clarification:{ClarificationStages.Architecture}:", StringComparison.Ordinal)
                ? f
                : null;

        if (archClarification == null)
        {
            // 阶段一：生成架构澄清提问，暂停 Skill
            var clarificationSet = await GenerateArchitectureClarificationAsync(context, ct);
            var clarFragmentId = $"clarification:{ClarificationStages.Architecture}:{context.ProjectId}";

            // 显式推 clarification_requested 事件名（SkillHarness 只推 ir_event/skill_progress，
            // 前端聊天面板靠此事件名渲染问卷卡）
            _sseHub.TryPush(context.PipelineId, "clarification_requested",
                JsonSerializer.Serialize(clarificationSet, JsonOptions));

            _logger.LogInformation(
                "Architect 阶段一：发出澄清提问 round={Round} questions={Count} pipelineId={Id}",
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

        // 阶段二：读用户答案，注入 userPrompt，跑 ToT
        var answersText = ExtractAnswersText(archClarification.Payload);
        var fieldCount = await _entityDesignRepo.CountFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        if (fieldCount == 0)
            throw Oops.Bah("Architect Skill: ai_entity_field 无字段，拒绝架构生成（须先 Round 3 Finalize 投影）");

        var payload = await GenerateArchitectureViaTotAsync(context, fragmentId, answersText, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.ArchitectureDecisionRecorded,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Architecture,
            FragmentVersion = 1,
            Payload = payload,
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
        catch (JsonException) { /* 损坏 payload，降级空串 */ }
        return string.Empty;
    }

    /// <summary>
    /// 阶段一：调 LLM 生成架构澄清选择题（部署/技术栈/集成/非功能）。
    /// 复用 ClarificationSet 契约，不变量由 BuildArchitectureClarificationSet 保证。
    /// </summary>
    private async Task<ClarificationSet> GenerateArchitectureClarificationAsync(
        SkillContext context, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 架构师。根据用户需求与 IR-1 业务事件，识别架构设计前必须澄清的歧义点，
            输出结构化交互问答 JSON（让用户通过选项而非打字来确认）。

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
            - 聚焦架构层歧义：部署方式、单体/微服务、技术栈、数据库、消息队列、缓存、集成方式、非功能需求（并发/数据量）
            - questions 数量 3-5 个
            - 每个 question.options 数量 3-5 个
            - 每个 question.options 末项必须是 {"id":"o_other","label":"其他","freeText":true}
            - type=single（单选）/multi（多选）；本阶段不用 text
            - required=true 的关键题不超过 2 个
            - id 用 q1/q2/...，option id 用 o1/o2/.../o_other
            - 新增字段（P9）：contextHint（为什么问）、defaultOption（默认值 option id）、questionFormat（SINGLE|MULTI|MATRIX_SINGLE|MATRIX_MULTI）
              如果问题是对「多个组件/模块」做同一维度的决策，使用 MATRIX_SINGLE 并输出 matrixSubItems：[{"rowId","rowLabel"}]
            - 只输出 JSON
            """;

        var userPrompt = $"""
            用户需求：
            {context.UserRequirement}

            IR-1 上下文：
            {context.PromptContext.CompressedSummary}

            ai_entity_field（唯一字段源，25 §6）：
            {await LoadEntityFieldContextAsync(context, ct)}

            projectId={context.ProjectId}
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", userPrompt),
            },
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
                _logger.LogWarning("架构澄清提问 LLM 失败，降级默认题：{Error}", response.Error);
                return BuildFallbackArchitectureClarification();
            }

            var json = PmSkillService.ExtractJson(response.Content);
            var draft = JsonSerializer.Deserialize<ClarificationDraft>(json, JsonOptions)
                ?? new ClarificationDraft();
            return BuildArchitectureClarificationSet(draft);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "架构澄清提问异常，降级默认题");
            return BuildFallbackArchitectureClarification();
        }
        finally
        {
            _budgetGuard.ReleaseRun(context.RunId, SkillId);
        }
    }

    /// <summary>把 LLM 草案升级为符合不变量的 ClarificationSet（stage=architecture）。</summary>
    private static ClarificationSet BuildArchitectureClarificationSet(ClarificationDraft draft)
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
                Text = string.IsNullOrWhiteSpace(dq.Text) ? $"架构问题 {questions.Count + 1}" : dq.Text,
                Type = type,
                Required = required,
                Options = options,
            });

            if (questions.Count >= 5) break;
        }

        if (questions.Count == 0)
            return BuildFallbackArchitectureClarification();

        return new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = ClarificationStages.Architecture,
            Round = 1,
            Title = string.IsNullOrWhiteSpace(draft.Title) ? "架构设计澄清" : draft.Title,
            Intro = string.IsNullOrWhiteSpace(draft.Intro)
                ? "以下问题影响架构决策，请逐题确认。每题最后一项为「其他」，可自由补充。"
                : draft.Intro,
            Questions = questions,
            AllowSkipNonCritical = true,
        };
    }

    /// <summary>LLM 降级时的默认架构澄清题（保证流程不卡死）。</summary>
    private static ClarificationSet BuildFallbackArchitectureClarification()
    {
        var other = new ClarificationOption { Id = "o_other", Label = "其他", FreeText = true };
        return new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = ClarificationStages.Architecture,
            Round = 1,
            Title = "架构设计澄清",
            Intro = "请确认以下架构决策要点，以便生成架构方案。每题最后一项为「其他」，可自由补充。",
            Questions = new List<ClarificationQuestion>
            {
                new()
                {
                    Id = "q1",
                    Text = "系统部署方式？",
                    Type = "single",
                    Required = true,
                    Options = new List<ClarificationOption>
                    {
                        new() { Id = "o1", Label = "单体应用（单库单服务）" },
                        new() { Id = "o2", Label = "微服务（按业务域拆分）" },
                        new() { Id = "o3", Label = "模块化单体（单部署多模块）" },
                        other,
                    },
                },
                new()
                {
                    Id = "q2",
                    Text = "数据库选型？",
                    Type = "single",
                    Required = false,
                    Options = new List<ClarificationOption>
                    {
                        new() { Id = "o1", Label = "SQL Server（沿用现有）" },
                        new() { Id = "o2", Label = "MySQL" },
                        new() { Id = "o3", Label = "PostgreSQL" },
                        other,
                    },
                },
                new()
                {
                    Id = "q3",
                    Text = "需要哪些中间件？（多选）",
                    Type = "multi",
                    Required = false,
                    Options = new List<ClarificationOption>
                    {
                        new() { Id = "o1", Label = "消息队列（RabbitMQ/Kafka）" },
                        new() { Id = "o2", Label = "缓存（Redis）" },
                        new() { Id = "o3", Label = "全文搜索（ES）" },
                        other,
                    },
                },
            },
            AllowSkipNonCritical = true,
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
            if (d.FreeText) continue; // 跳过 LLM 自己加的"其他"，下面统一补
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

    private async Task<string> GenerateArchitectureViaTotAsync(
        SkillContext context, string fragmentId, string clarificationAnswersText, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 架构师 Skill。根据 IR-1 业务事件输出架构决策 JSON。
            必须包含：pattern（layered|cqrs）、modules（非空数组）、candidates（2 个架构候选，含 score）、selectedIndex（Top-1）。
            只输出 JSON。
            """;

        // ADR-005：注入用户对架构澄清的作答（人可读汇总），引导 LLM 选型
        var clarificationBlock = string.IsNullOrWhiteSpace(clarificationAnswersText)
            ? string.Empty
            : $"""

                架构澄清（用户已确认）：
                {clarificationAnswersText}
                """;

        var userPrompt = $"""
            用户需求：
            {context.UserRequirement}

            IR-1 上下文：
            {context.PromptContext.CompressedSummary}
            {clarificationBlock}

            ai_entity_field（唯一字段源，25 §6）：
            {await LoadEntityFieldContextAsync(context, ct)}

            请给出一种完整架构决策 JSON。
            projectId={context.ProjectId}
            """;

        var branches = await BudgetGuardTreeSearch.RunAsync(
            _budgetGuard, context, SkillId, systemPrompt, userPrompt,
            TotBranchCount, 0.3, 0.35, "json", ct);

        if (!branches.Any(b => b.IsSuccess))
            throw Oops.Bah($"Architect Skill ToT 全部分支 LLM 失败: {string.Join(" | ", branches.Select(b => b.Error))}");

        var keyword = PmSkillService.ExtractSearchKeyword(context);
        var scored = new List<(string Json, decimal Score, int BranchIndex)>();

        foreach (var branch in branches.Where(b => b.IsSuccess))
        {
            string json;
            try
            {
                json = PmSkillService.ExtractJson(branch.Content);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("modules", out var modules)
                    || modules.ValueKind != JsonValueKind.Array
                    || modules.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Architect ToT 分支 {Branch} 缺少 modules，跳过", branch.BranchIndex);
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Architect ToT 分支 {Branch} JSON 无效，跳过", branch.BranchIndex);
                continue;
            }

            var score = await ScoreCandidateAsync(json, keyword, ct);
            scored.Add((json, score, branch.BranchIndex));
        }

        if (scored.Count == 0)
            throw Oops.Bah("Architect Skill ToT 全部分支产出无效（JSON 或 modules 校验失败）");

        var top = scored.OrderByDescending(s => s.Score).First();
        _logger.LogInformation("Architect ToT Top-1: branch={Branch} score={Score}", top.BranchIndex, top.Score);

        using var topDoc = JsonDocument.Parse(top.Json);
        return NormalizeArchitectureJson(topDoc.RootElement, fragmentId);
    }

    private async Task<decimal> ScoreCandidateAsync(string candidateJson, string keyword, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { candidateJson, keyword }, JsonOptions);
        var result = await Mcp.CallToolAsync("kg.score-candidate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"kg.score-candidate 失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        return doc.RootElement.TryGetProperty("score", out var scoreEl) && scoreEl.TryGetDecimal(out var score)
            ? score
            : 0m;
    }

    private static string NormalizeArchitectureJson(JsonElement root, string fragmentId)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        // 阶段二执行到这里时，架构澄清已 stable（门控已过），架构片段可直接 stable
        dict["stabilityState"] = JsonSerializer.SerializeToElement(IrStabilityStates.Stable);

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    /// <summary>25 §6：从 ai_entity_field 组装字段上下文，禁止仅依赖 IR JSON。</summary>
    private async Task<string> LoadEntityFieldContextAsync(SkillContext context, CancellationToken ct)
    {
        var fields = await _entityDesignRepo.ListFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        if (fields.Count == 0)
            return "（尚无投影字段 — 须 Finalize 后才有完整实体模型）";

        var sb = new StringBuilder();
        foreach (var g in fields.GroupBy(f => f.EntityName))
        {
            sb.AppendLine($"实体 {g.Key} (表 {g.First().TableName}):");
            foreach (var f in g)
                sb.AppendLine($"  - {f.FieldName} ({f.CSharpType}, required={f.IsRequired})");
        }
        return sb.ToString();
    }

    /// <summary>LLM 产出的澄清草案（内部反序列化用）。</summary>
    private sealed class ClarificationDraft
    {
        public string Title { get; set; } = "";
        public string Intro { get; set; } = "";
        public List<ClarificationQuestion> Questions { get; set; } = new();
    }
}
