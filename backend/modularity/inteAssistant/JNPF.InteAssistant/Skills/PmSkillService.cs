using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 产品经理 Skill（R1 认知模具版）— 真 ToT：TreeSearchAsync 多路并行 + kg.score-candidate 裁决 Top-1。
/// LLM/校验全败 → 抛 Oops.Bah，禁止 fallback 假骨架（施工包 21 R1 / 红线 RL-1）。
/// </summary>
public sealed class PmSkillService : CognitiveSkill, ITransient
{
    private const int TotBranchCount = 3;
    /// <summary>与 LlmCallPolicy["pm-skill"].MaxTokensPerCall 对齐；4096 会截断大型 IR-0 JSON。</summary>
    private const int TotMaxTokens = 8192;
    private const int QuestionsPerRound = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<PmSkillService> _logger;
    private readonly IRequirementEvolutionContext? _evolutionContext;

    public PmSkillService(
        ICognitiveSkillToolkit toolkit,
        ILogger<PmSkillService> logger,
        IRequirementEvolutionContext? evolutionContext = null)
        : base(toolkit)
    {
        _logger = logger;
        _evolutionContext = evolutionContext;
    }

    public override string SkillId => "pm-skill";
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Decision;
    public override SkillMission Mission => SkillMission.DefineBoundary;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = Array.Empty<string>(),
        RequiredStability = IrStabilityStates.Draft,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.SkeletonCreated },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (existing != null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架已 stable，请先修订或新建项目"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.SkeletonCreated)
            return Task.FromResult(SkillValidationResult.Fail("PM Skill 必须产出 1 条 SkeletonCreated"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var skeletonId = $"SK-{context.PipelineId}";
        var fragmentId = $"skeleton:{skeletonId}";

        var payload = await GenerateSkeletonViaTotAsync(context, skeletonId, fragmentId, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            FragmentVersion = 1,
            Payload = payload,
        };
    }

    public async Task<ClarificationSet> GenerateClarificationAsync(
        int round,
        string stage,
        string tenantId,
        string projectId,
        long pipelineId,
        SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings,
        string? previousAnswersText,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var (title, intro, systemPrompt) = BuildRoundPrompt(round, compileResult, warnings, previousAnswersText);
        var selectedSlots = SlotInformationGainSelector.SelectTopSlots(compileResult, previousAnswersText, QuestionsPerRound);
        var retrievalText = BuildRoundUserPrompt(round, compileResult, warnings, previousAnswersText)
                            + "\n" + string.Join("\n", selectedSlots.Select(s => $"{s.SlotId}:{s.Title} {s.Description}"));
        var seeds = await RetrieveEvolutionSeedsAsync(tenantId, projectId, pipelineId, retrievalText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var slotsPrompt = selectedSlots.Count > 0
            ? "\n优先覆盖的信息增益槽位：\n" + string.Join("\n", selectedSlots.Select(s => $"- {s.SlotId}: {s.Title} — {s.Description}"))
            : string.Empty;
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt + "\n" + seedPrompt + slotsPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", BuildRoundUserPrompt(round, compileResult, warnings, previousAnswersText)),
            },
            Temperature = 0.3,
            MaxTokens = 2048,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        var response = await ChatWithSchemaRetryAsync(request, "clarification", ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("pm-skill 第 {Round} 轮出题 LLM 失败，降级为空题集: {Error}",
                round, response.Error);
            return BuildEmptyClarificationSet(stage, round, title, intro);
        }

        var questions = ParseQuestionsFromLlm(response.Content, round);
        ApplyMatrixFallback(questions, compileResult);
        EnsureEscapeHatch(questions);
        StampSlotHints(questions, selectedSlots);

        var set = new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = title,
            Intro = intro,
            AllowSkipNonCritical = true,
            Questions = questions,
            TargetSlotIds = selectedSlots.Select(s => s.SlotId).ToList(),
        };

        _logger.LogInformation(
            "pm-skill 第 {Round} 轮出题完成 stage={Stage} tenant={TenantId} project={ProjectId} pipeline={PipelineId} questions={Count} slots={SlotIds} seeds={SeedIds}",
            round, stage, tenantId, projectId, pipelineId, questions.Count,
            string.Join(",", selectedSlots.Select(s => s.SlotId)),
            string.Join(",", seeds.Select(s => s.CaseId)));
        return set;
    }

    public async Task<PmSpecReviewResult> ReviewSpecAsync(
        SkillContext context,
        string requirementSpecMarkdown,
        CancellationToken ct,
        SaNineViewCompileResult? compileResult = null)
    {
        if (string.IsNullOrWhiteSpace(requirementSpecMarkdown))
        {
            return new PmSpecReviewResult
            {
                Score = 0,
                Verdict = "fail",
                Gaps = new List<string> { "需求分析说明书为空，无法评审" },
                GapDetails = new List<PmSpecReviewGap>
                {
                    new() { Source = "graph", Message = "需求分析说明书为空，无法评审" },
                },
            };
        }

        var graphGaps = compileResult != null
            ? RequirementConflictGraph.FindGaps(compileResult)
            : Array.Empty<RequirementGraphGap>();
        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, requirementSpecMarkdown, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var graphChecklist = graphGaps.Count > 0
            ? "\n确定性冲突图已发现 gaps，请合并到输出 gaps：\n"
              + string.Join("\n", graphGaps.Select(g => $"- source=graph; {g.Code}: {g.Message}"))
            : string.Empty;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 JNPF 低代码平台的 PM 需求专家。请审查 02 需求分析说明书是否足以交给开发。
                只输出 JSON：{"score":0-100,"verdict":"pass|fail","gaps":["缺口1"]}。
                评分标准：业务范围清晰、功能流程完整、实体/表可支撑、边界/异常明确、可验收。
                请软性检查是否包含「非目标 / Out of Scope」「失败与补偿」「验收要点」等可交付章节；缺失时写入 gaps，但不要因模板占位本身直接判为失败。
                若存在确定性冲突图 gaps，必须保留；LLM 自己发现的 gaps 也可补充。
                """ + "\n" + seedPrompt + graphChecklist,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                    需求分析说明书：
                    {requirementSpecMarkdown}
                    """),
            },
            Temperature = 0.1,
            MaxTokens = 1024,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        try
        {
            var response = await ChatWithSchemaRetryAsync(request, "review", ct);
            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("pm-skill ReviewSpec LLM 失败: {Error}", response.Error);
                return new PmSpecReviewResult
                {
                    Score = 0,
                    Verdict = "fail",
                    Gaps = new List<string> { response.Error ?? "PM 终评 LLM 返回空" },
                    GapDetails = new List<PmSpecReviewGap>
                    {
                        new() { Source = "llm", Message = response.Error ?? "PM 终评 LLM 返回空" },
                    },
                };
            }

            var parsed = ParseSpecReviewResult(response.Content);
            var merged = MergeReviewGaps(parsed, graphGaps);
            var cappedScore = RequirementConfidencePolicy.ApplyPmScoreCap(merged.Score, compileResult);
            if (cappedScore != merged.Score)
            {
                var capGap = "存在 confidence<0.5 的核心假设，PM 分数封顶 84";
                merged = new PmSpecReviewResult
                {
                    Score = cappedScore,
                    Verdict = "fail",
                    Gaps = merged.Gaps.Concat(new[] { capGap }).Distinct(StringComparer.Ordinal).ToList(),
                    GapDetails = merged.GapDetails.Concat(new[]
                    {
                        new PmSpecReviewGap { Source = "graph", Message = capGap },
                    }).ToList(),
                };
            }

            _logger.LogInformation(
                "pm-skill ReviewSpec 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} score={Score} graphGaps={GraphGapCount} seeds={SeedIds}",
                context.TenantId, context.ProjectId, context.PipelineId, merged.Score, graphGaps.Count,
                string.Join(",", seeds.Select(s => s.CaseId)));
            return merged;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "pm-skill ReviewSpec JSON 解析失败");
            return new PmSpecReviewResult
            {
                Score = 0,
                Verdict = "fail",
                Gaps = new List<string> { "PM 终评 JSON 解析失败" },
                GapDetails = new List<PmSpecReviewGap>
                {
                    new() { Source = "llm", Message = "PM 终评 JSON 解析失败" },
                },
            };
        }
    }

    public async Task<PmAmendProposeResult> AmendProposeAsync(
        SkillContext context,
        string userMessage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw Oops.Bah("补充需求不能为空");

        var seeds = await RetrieveEvolutionSeedsAsync(context.TenantId, context.ProjectId, context.PipelineId, userMessage, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 PM 需求专家。用户在确认 02 前输入了硬需求，请只做理解回显，不改业务骨架。
                输出 JSON：{"features":[],"flows":[],"entitiesOrTables":[],"summaryMarkdown":"","severity":"patch|enhance","patches":[]}。
                features 必须列功能，flows 必须列流程变化，entitiesOrTables 必须列实体或表影响。
                patches 可选；若能确定性表达变更，输出操作对象：
                {"operation":"AddEntity|AddEvent|PatchRule|AddField|PatchSummary|AddStateTransition","target":"","name":"","displayName":"","type":"","description":"","required":false,"references":"","scopeEventId":"","from":"","to":""}
                禁止在 patches 中表达不确定猜测；不确定就留空 patches。
                """ + "\n" + seedPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                    用户补充需求：
                    {userMessage}
                    """),
            },
            Temperature = 0.2,
            MaxTokens = 1536,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        AmendmentUnderstanding understanding;
        var response = await ChatWithSchemaRetryAsync(request, "amend", ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("pm-skill AmendPropose LLM 失败，使用原文回显: {Error}", response.Error);
            understanding = BuildFallbackUnderstanding(userMessage);
        }
        else
        {
            understanding = ParseAmendmentUnderstanding(response.Content, userMessage);
        }

        _logger.LogInformation(
            "pm-skill AmendPropose 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} patches={PatchCount} seeds={SeedIds}",
            context.TenantId, context.ProjectId, context.PipelineId, understanding.Patches.Count,
            string.Join(",", seeds.Select(s => s.CaseId)));

        return new PmAmendProposeResult
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            Understanding = understanding,
        };
    }

    public Task<string> ApplyAmendmentAsync(
        SkillContext context,
        AmendmentUnderstanding understanding,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var delta = new StringBuilder();
        delta.AppendLine("【用户确认的补充需求】");
        if (!string.IsNullOrWhiteSpace(understanding.SummaryMarkdown))
            delta.AppendLine(understanding.SummaryMarkdown.Trim());
        if (understanding.Features.Count > 0)
            delta.AppendLine("功能：" + string.Join("、", understanding.Features));
        if (understanding.Flows.Count > 0)
            delta.AppendLine("流程：" + string.Join("、", understanding.Flows));
        if (understanding.EntitiesOrTables.Count > 0)
            delta.AppendLine("实体/表：" + string.Join("、", understanding.EntitiesOrTables));
        if (understanding.Patches.Count > 0)
        {
            delta.AppendLine("类型化补丁：");
            foreach (var patch in understanding.Patches)
                delta.AppendLine($"- {patch.Operation}: {patch.Target}/{patch.Name}");
        }

        _logger.LogInformation(
            "pm-skill ApplyAmendment 生成 delta tenant={TenantId} project={ProjectId} pipeline={PipelineId} severity={Severity} patches={PatchCount}",
            context.TenantId, context.ProjectId, context.PipelineId, understanding.Severity, understanding.Patches.Count);
        return Task.FromResult(delta.ToString());
    }

    /// <summary>
    /// 三轮澄清作答后：PM 作为完善主体，把用户答案落实为 Typed patches 写回骨架。
    /// 确定性槽位补丁为基线；LLM 失败时仍返回基线，不阻断流程。
    /// </summary>
    public async Task<IReadOnlyList<AmendmentPatch>> RefineSkeletonFromClarificationAsync(
        SkillContext context,
        string skeletonJson,
        string answersText,
        IReadOnlyList<string> filledSlotIds,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string? existingSummary = null;
        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            if (doc.RootElement.TryGetProperty("requirementSummary", out var sum)
                && sum.ValueKind == JsonValueKind.String)
                existingSummary = sum.GetString();
        }
        catch (JsonException) { /* ignore */ }

        var baseline = ClarificationAnswerPatchMapper.BuildPatches(answersText, filledSlotIds, existingSummary);
        if (string.IsNullOrWhiteSpace(answersText) && baseline.Count == 0)
            return Array.Empty<AmendmentPatch>();

        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, answersText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var skeletonBrief = skeletonJson.Length > 3500 ? skeletonJson[..3500] : skeletonJson;
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 JNPF 产品经理。用户已回答需求分析澄清题，请把答案落实进需求骨架（完善，不是再出题）。
                只输出 JSON：{"patches":[...],"summaryMarkdown":""}。
                patches 使用：{"operation":"AddEntity|AddEvent|PatchRule|AddField|PatchSummary|AddStateTransition","target":"","name":"","displayName":"","type":"","description":"","required":false,"references":"","scopeEventId":"","from":"","to":""}
                规则：
                - 根据用户答案补字段、规则、状态流转、事件说明；不确定则不要猜造实体。
                - 必须把用户确认点写入 PatchSummary 或 PatchRule 的 description。
                - 禁止输出与答案无关的大幅重写。
                """ + "\n" + seedPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}
                    已填槽位：{string.Join(",", filledSlotIds)}

                    用户澄清答案：
                    {answersText}

                    当前骨架 JSON（节选）：
                    {skeletonBrief}
                    """),
            },
            Temperature = 0.2,
            MaxTokens = 2048,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        IReadOnlyList<AmendmentPatch> llmPatches = Array.Empty<AmendmentPatch>();
        try
        {
            var response = await ChatWithSchemaRetryAsync(request, "amend", ct);
            if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Content))
            {
                using var doc = JsonDocument.Parse(ExtractJson(response.Content));
                llmPatches = AmendmentPatchApplier.ParsePatches(doc.RootElement);
            }
            else
            {
                _logger.LogWarning("pm-skill RefineSkeleton LLM 失败，仅用确定性补丁: {Error}", response.Error);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "pm-skill RefineSkeleton 解析失败，仅用确定性补丁");
        }

        // LLM 为完善主体：优先保留其补丁，确定性基线补缺口
        var merged = AmendmentPatchApplier.MergePatches(llmPatches, baseline);
        _logger.LogInformation(
            "pm-skill RefineSkeleton 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} llmPatches={Llm} baseline={Base} merged={Merged} seeds={SeedIds}",
            context.TenantId, context.ProjectId, context.PipelineId,
            llmPatches.Count, baseline.Count, merged.Count,
            string.Join(",", seeds.Select(s => s.CaseId)));
        return merged;
    }

    public static PmSpecReviewResult ParseSpecReviewResult(string content)
    {
        var json = ExtractJson(content);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var score = root.TryGetProperty("score", out var scoreEl) && scoreEl.TryGetInt32(out var s)
            ? Math.Clamp(s, 0, 100)
            : 0;
        var verdict = root.TryGetProperty("verdict", out var verdictEl) && verdictEl.ValueKind == JsonValueKind.String
            ? verdictEl.GetString() ?? ""
            : "";
        var gaps = new List<string>();
        if (root.TryGetProperty("gaps", out var gapsEl) && gapsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in gapsEl.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    gaps.Add(item.GetString()!);
        }

        var gapDetails = new List<PmSpecReviewGap>();
        if (root.TryGetProperty("gapDetails", out var detailsEl) && detailsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in detailsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var message = ReadString(item, "message", "gap");
                if (string.IsNullOrWhiteSpace(message))
                    continue;
                gapDetails.Add(new PmSpecReviewGap
                {
                    Source = ReadString(item, "source") ?? "llm",
                    Message = message,
                });
            }
        }
        if (gapDetails.Count == 0)
            gapDetails = gaps.Select(g => new PmSpecReviewGap { Source = "llm", Message = g }).ToList();
        if (gaps.Count == 0)
            gaps = gapDetails.Select(g => g.Message).ToList();

        return new PmSpecReviewResult
        {
            Score = score,
            Verdict = score >= 85 && string.Equals(verdict, "pass", StringComparison.OrdinalIgnoreCase) ? "pass" : "fail",
            Gaps = gaps,
            GapDetails = gapDetails,
        };
    }

    private static (string Title, string Intro, string SystemPrompt) BuildRoundPrompt(
        int round, SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings, string? previousAnswersText)
    {
        return round switch
        {
            1 => ("需求骨架确认",
                  "产品经理已基于行业经验完善了您的需求骨架，请确认以下几个关键决策点。",
                  """
                    你是有 10 年经验的行业产品经理。用户不是专家，你是。
                    任务：基于已生成的需求骨架（businessEvents + entityDrafts），判断最需要用户确认的 3 个模糊点。
                    规则：
                      - 只问真正模糊的、需要业务方决策的点
                      - 行业惯例能定的不要问用户
                      - 每个问题含：问题文本 + 3-5 个选项（末项为"其他"）+ context_hint（为什么问）+ 默认值
                      - 普通题只允许 questionFormat: "MULTI"
                      - 如果问题是「对多个事件做同一维度的决策」，必须使用 "MATRIX_SINGLE" 或 "MATRIX_MULTI"
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON，不要 markdown。
                    """),
            2 => ("深度精化确认",
                  "系统需求分析师已完成深度分析（含 PSpec/DecisionTable 增强），请确认以下边界条件与业务规则。",
                  """
                    你是产品经理 + 系统需求分析师联合体。
                    基于用户上一轮的回答与 SA 深度分析，判断最重要的 3 个需要用户裁决的点。
                    规则：每个问题聚焦一个决策点（边界条件/异常路径/业务规则冲突）。
                    普通题只允许 questionFormat: "MULTI"；矩阵题使用 "MATRIX_SINGLE"/"MATRIX_MULTI"。
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON。
                    """),
            3 => ("最终遗漏检查",
                  "这是最后一轮确认，请核对以下推导假设与遗漏点。全部跳过可直接定稿。",
                  """
                    你是最终审查专家。
                    任务：检查全部三轮分析后，还有遗漏吗？推导的假设项中哪些需要用户确认？
                    规则：最多发现 3 个遗漏/待确认假设；如无遗漏，返回空数组 []。
                    普通题只允许 questionFormat: "MULTI"；矩阵题使用 "MATRIX_SINGLE"/"MATRIX_MULTI"。
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    如确实无遗漏，输出 []。
                    """),
            _ => ($"第 {round} 轮确认", string.Empty, "输出 JSON 数组。普通题只允许 questionFormat: \"MULTI\"。"),
        };
    }

    private static string BuildRoundUserPrompt(
        int round, SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings, string? previousAnswersText)
    {
        var eventSummary = string.Join("\n",
            compileResult.EventResults.Select(e => $"- {e.EventId}: {e.EventName}（{e.Complexity}）"));

        var assumptionsText = compileResult.Assumptions.Count > 0
            ? "\n编译器推导的假设项：\n" + string.Join("\n",
                compileResult.Assumptions.Select(a => $"- [{a.Confidence:P0}] {a.EventId}/{a.SourceStep}: {a.Text}"))
            : "\n（编译器无假设项）";

        var warningsText = warnings.Count > 0
            ? "\n轻量校验警告：\n" + string.Join("\n", warnings.Select(w => $"- {w}"))
            : string.Empty;

        var prevText = !string.IsNullOrWhiteSpace(previousAnswersText)
            ? $"\n用户上一轮回答：\n{previousAnswersText}"
            : string.Empty;

        return $"""
            需求事件清单（共 {compileResult.EventResults.Count} 个）：
            {eventSummary}
            {assumptionsText}{warningsText}{prevText}

            请给出本轮最需用户确认的最多 {QuestionsPerRound} 个问题。
            """;
    }

    private static List<ClarificationQuestion> ParseQuestionsFromLlm(string content, int round)
    {
        var questions = new List<ClarificationQuestion>();
        try
        {
            var json = ExtractJsonArray(content);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return questions;

            var idx = 0;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (idx >= QuestionsPerRound) break;
                var text = el.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(text)) continue;

                var options = new List<ClarificationOption>();
                if (el.TryGetProperty("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var opt in optsEl.EnumerateArray())
                    {
                        var label = opt.ValueKind == JsonValueKind.String
                            ? opt.GetString() ?? string.Empty
                            : opt.TryGetProperty("label", out var l) ? l.GetString() ?? string.Empty : opt.GetRawText();
                        options.Add(new ClarificationOption
                        {
                            Id = $"opt-{options.Count + 1}",
                            Label = label,
                            FreeText = label is "其他" or "其它" or "以上都不是",
                        });
                    }
                }

                var format = "MULTI";
                if (el.TryGetProperty("format", out var fmtEl) && fmtEl.ValueKind == JsonValueKind.String)
                    format = ClarificationQuestion.NormalizeQuestionFormat(fmtEl.GetString());
                else if (el.TryGetProperty("questionFormat", out var qfEl) && qfEl.ValueKind == JsonValueKind.String)
                    format = ClarificationQuestion.NormalizeQuestionFormat(qfEl.GetString());

                List<MatrixSubItem>? matrixItems = null;
                if (el.TryGetProperty("matrixSubItems", out var mxEl) && mxEl.ValueKind == JsonValueKind.Array
                    || el.TryGetProperty("matrix_rows", out mxEl) && mxEl.ValueKind == JsonValueKind.Array)
                {
                    matrixItems = new List<MatrixSubItem>();
                    foreach (var row in mxEl.EnumerateArray())
                    {
                        var rowId = row.TryGetProperty("rowId", out var rid) ? rid.GetString()
                            : row.TryGetProperty("id", out var id2) ? id2.GetString() : null;
                        var rowLabel = row.TryGetProperty("rowLabel", out var rl) ? rl.GetString()
                            : row.TryGetProperty("label", out var lb) ? lb.GetString() : rowId;
                        if (string.IsNullOrWhiteSpace(rowId)) continue;
                        matrixItems.Add(new MatrixSubItem
                        {
                            RowId = rowId!,
                            RowLabel = rowLabel ?? rowId!,
                        });
                    }
                    if (matrixItems.Count > 0 && format is "MULTI")
                        format = "MATRIX_MULTI";
                }

                var qType = format switch
                {
                    "MULTI" or "MATRIX_MULTI" => "multi",
                    "TEXT" => "text",
                    _ => "single",
                };

                questions.Add(new ClarificationQuestion
                {
                    Id = $"r{round}-q{idx + 1}",
                    Text = text,
                    Type = qType,
                    Required = false,
                    Options = options,
                    ContextHint = el.TryGetProperty("contextHint", out var ch) ? ch.GetString()
                        : el.TryGetProperty("context_hint", out var ch2) ? ch2.GetString() : null,
                    DefaultOption = el.TryGetProperty("defaultOption", out var dof)
                        ? (dof.ValueKind == JsonValueKind.String ? dof.GetString() : null)
                        : el.TryGetProperty("default_option", out var dof2)
                            ? (dof2.ValueKind == JsonValueKind.String ? dof2.GetString() : null) : null,
                    QuestionFormat = format,
                    MatrixSubItems = matrixItems,
                });
                idx++;
            }
        }
        catch (JsonException)
        {
            return questions;
        }

        return questions;
    }

    private static void ApplyMatrixFallback(List<ClarificationQuestion> questions, SaNineViewCompileResult compileResult)
    {
        var eventNames = compileResult.EventResults
            .Select(e => e.EventName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eventNames.Count < 2) return;

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            if (q.MatrixSubItems is { Count: > 0 }) continue;
            if (q.QuestionFormat is not "MULTI") continue;

            var matchedEvents = eventNames
                .Where(en => q.Text.Contains(en, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedEvents.Count < 2) continue;

            questions[i] = q with
            {
                MatrixSubItems = matchedEvents.Select((en, j) => new MatrixSubItem
                {
                    RowId = $"evt-{j + 1}",
                    RowLabel = en,
                }).ToList(),
                QuestionFormat = "MATRIX_MULTI",
            };
        }
    }

    private static void EnsureEscapeHatch(List<ClarificationQuestion> questions)
    {
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            if (q.Options.Count == 0) continue;
            if (q.Options.Any(o => o.FreeText || o.Label is "其他" or "其它"))
                continue;

            var options = q.Options.Take(4).ToList();
            options.Add(new ClarificationOption
            {
                Id = "o_other",
                Label = "其他",
                FreeText = true,
            });
            questions[i] = q with { Options = options };
        }
    }

    /// <summary>把优先槽位 id 戳进 ContextHint，作答文本可回传 slotId 供 DetectFilledSlots。</summary>
    private static void StampSlotHints(
        List<ClarificationQuestion> questions,
        IReadOnlyList<RequirementSlot> selectedSlots)
    {
        for (var i = 0; i < questions.Count && i < selectedSlots.Count; i++)
        {
            var slot = selectedSlots[i];
            var q = questions[i];
            var stamp = $"[slot:{slot.SlotId}]";
            var hint = string.IsNullOrWhiteSpace(q.ContextHint)
                ? stamp + " " + slot.Description
                : stamp + " " + q.ContextHint;
            questions[i] = q with { ContextHint = hint.Trim() };
        }
    }

    private static ClarificationSet BuildEmptyClarificationSet(string stage, int round, string title, string intro)
        => new()
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = title,
            Intro = intro + "（自动生成题目暂不可用，可直接跳过）",
            AllowSkipNonCritical = true,
            Questions = new List<ClarificationQuestion>(),
        };

    private static string ExtractJsonArray(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('[');
            var end = trimmed.LastIndexOf(']');
            if (start >= 0 && end > start) return trimmed[start..(end + 1)];
        }
        var s = trimmed.IndexOf('[');
        var e = trimmed.LastIndexOf(']');
        if (s >= 0 && e > s) return trimmed[s..(e + 1)];
        return trimmed;
    }

    private static AmendmentUnderstanding ParseAmendmentUnderstanding(string content, string fallbackText)
    {
        try
        {
            var json = ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new AmendmentUnderstanding
            {
                Features = ReadStringArray(root, "features"),
                Flows = ReadStringArray(root, "flows"),
                EntitiesOrTables = ReadStringArray(root, "entitiesOrTables", "entities_or_tables", "tables"),
                SummaryMarkdown = ReadString(root, "summaryMarkdown", "summary_markdown") ?? fallbackText,
                Severity = NormalizeSeverity(ReadString(root, "severity")),
                Patches = ReadPatches(root),
            };
        }
        catch (JsonException)
        {
            return BuildFallbackUnderstanding(fallbackText);
        }
    }

    private static AmendmentUnderstanding BuildFallbackUnderstanding(string userMessage)
        => new()
        {
            Features = new List<string> { userMessage.Trim() },
            SummaryMarkdown = userMessage.Trim(),
            Severity = "patch",
        };

    private static List<string> ReadStringArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
                continue;
            var result = new List<string>();
            foreach (var item in el.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    result.Add(item.GetString()!);
            return result;
        }

        return new List<string>();
    }

    private static List<AmendmentPatch> ReadPatches(JsonElement root)
    {
        if (!root.TryGetProperty("patches", out var el) || el.ValueKind != JsonValueKind.Array)
            return new List<AmendmentPatch>();

        var patches = new List<AmendmentPatch>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var opText = ReadString(item, "operation", "op", "type");
            if (!Enum.TryParse<AmendmentPatchOperation>(opText, ignoreCase: true, out var operation))
                continue;

            patches.Add(new AmendmentPatch(
                operation,
                ReadString(item, "target", "entity", "eventId") ?? "",
                ReadString(item, "name", "field", "eventName", "ruleId") ?? "",
                ReadString(item, "displayName", "display_name"),
                ReadString(item, "dataType", "fieldType", "type"),
                ReadString(item, "description", "summary"),
                item.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True,
                ReadString(item, "references", "ref"),
                ReadString(item, "scopeEventId", "scope"),
                ReadString(item, "from"),
                ReadString(item, "to")));
        }

        return patches;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        return null;
    }

    private static string NormalizeSeverity(string? severity)
        => string.Equals(severity, "enhance", StringComparison.OrdinalIgnoreCase) ? "enhance" : "patch";

    private async Task<IReadOnlyList<RequirementEvolutionSeed>> RetrieveEvolutionSeedsAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string text,
        CancellationToken ct)
    {
        if (_evolutionContext == null)
            return Array.Empty<RequirementEvolutionSeed>();
        return await _evolutionContext.RetrieveSeedsAsync(tenantId, projectId, pipelineId, text, 3, ct);
    }

    private async Task<ChatCompletionResponse> ChatWithSchemaRetryAsync(
        ChatCompletionRequest request,
        string schemaKind,
        CancellationToken ct)
    {
        var response = await Llm.ChatAsync(request, ct);
        var error = ValidateJsonShape(response.Content, schemaKind);
        if (!response.IsSuccess || error == null)
            return response;

        _logger.LogWarning("pm-skill {SchemaKind} JSON schema 校验失败，重试一次: {Error}", schemaKind, error);
        var retryMessages = request.Messages.ToList();
        retryMessages.Add(new ChatMessage("user", $"上一次 JSON 不符合 schema：{error}。请只输出修正后的 JSON。"));
        var retry = new ChatCompletionRequest
        {
            ProviderCode = request.ProviderCode,
            SystemPrompt = request.SystemPrompt,
            Messages = retryMessages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            TimeoutMs = request.TimeoutMs,
            ResponseFormat = request.ResponseFormat,
        };
        return await Llm.ChatAsync(retry, ct);
    }

    private static string? ValidateJsonShape(string? content, string schemaKind)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var json = schemaKind == "clarification" ? ExtractJsonArray(content) : ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return schemaKind switch
            {
                "clarification" => ValidateClarificationJson(root),
                "amend" => ValidateAmendJson(root),
                "review" => ValidateReviewJson(root),
                _ => null,
            };
        }
        catch (JsonException ex)
        {
            return $"JSON 解析失败: {ex.Message}";
        }
    }

    private static string? ValidateClarificationJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return "出题结果必须是数组";
        foreach (var q in root.EnumerateArray())
        {
            if (q.ValueKind != JsonValueKind.Object)
                return "每个题目必须是对象";
            if (!q.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(text.GetString()))
                return "题目缺少 text";
            if (!q.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() is < 3 or > 5)
                return "题目 options 数量必须为 3-5";
        }

        return null;
    }

    private static string? ValidateAmendJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return "Amend 结果必须是对象";
        if (!root.TryGetProperty("summaryMarkdown", out var summary) || summary.ValueKind != JsonValueKind.String)
            return "Amend 缺少 summaryMarkdown";
        if (root.TryGetProperty("patches", out var patches) && patches.ValueKind != JsonValueKind.Array)
            return "patches 必须是数组";
        return null;
    }

    private static string? ValidateReviewJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return "Review 结果必须是对象";
        if (!root.TryGetProperty("score", out var score) || !score.TryGetInt32(out _))
            return "Review 缺少整数 score";
        if (!root.TryGetProperty("gaps", out var gaps) || gaps.ValueKind != JsonValueKind.Array)
            return "Review gaps 必须是数组";
        return null;
    }

    private static PmSpecReviewResult MergeReviewGaps(
        PmSpecReviewResult parsed,
        IReadOnlyList<RequirementGraphGap> graphGaps)
    {
        var details = parsed.GapDetails.Count > 0
            ? parsed.GapDetails.ToList()
            : parsed.Gaps.Select(g => new PmSpecReviewGap { Source = "llm", Message = g }).ToList();

        foreach (var gap in graphGaps)
        {
            if (details.Any(d => string.Equals(d.Message, gap.Message, StringComparison.Ordinal)))
                continue;
            details.Add(new PmSpecReviewGap { Source = gap.Source, Message = gap.Message });
        }

        var gapTexts = details.Select(d => d.Message)
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new PmSpecReviewResult
        {
            Score = parsed.Score,
            Verdict = graphGaps.Count > 0 && parsed.Score >= 85 ? "fail" : parsed.Verdict,
            Gaps = gapTexts,
            GapDetails = details,
        };
    }

    private async Task<string> GenerateSkeletonViaTotAsync(
        SkillContext context, string skeletonId, string fragmentId, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill。根据用户需求输出 IR-0 骨架 JSON。
            必须包含：businessEvents（6-15项，每项含 eventId/eventName/complexityHint/dependsOn）、
            roleMatrix（角色×事件→操作）、entityDrafts（3-8项）。

            entityDrafts 每项必须含：
            - entityName: 实体名（PascalCase）
            - displayName: 中文显示名
            - tableName: 表名（留空则编译器自动派生）
            - fields[]: 每字段含 name/type/required/primaryKey（布尔，true=主键）
            - relations[]: 实体间关系声明，每项含 fromField/toEntity/toField/relationType(many-to-one|one-to-many|many-to-many)
              或在字段级用 references: "EntityName.FieldName" 声明外键

            字段示例：
            "fields": [
              {"name":"id","type":"BIGINT","required":true,"primaryKey":true},
              {"name":"employeeId","type":"BIGINT","required":true,"references":"Employee.id"},
              {"name":"leaveDays","type":"float","required":true}
            ]

            关系示例：
            "relations": [
              {"fromField":"employeeId","toEntity":"Employee","toField":"id","relationType":"many-to-one"}
            ]

            roleMatrix 示例：
            "roleMatrix": {
              "roles": ["员工","部门主管","HR"],
              "matrix": {"EV-001": {"员工": ["create","read"], "部门主管": ["approve","reject"]}}
            }

            只输出 JSON，不要 markdown。
            """;

        var seedHints = string.Join(", ", context.SeedMatches.Take(5).Select(s => s.EventNamePattern));
        var userPrompt = $"""
            用户需求：
            {RequirementTextHelper.ForPmPrompt(context)}

            参考种子（可复用模式）：
            {(string.IsNullOrWhiteSpace(seedHints) ? "（无）" : seedHints)}

            请给出一种 businessEvents 切分方案的完整 IR-0 骨架。
            skeletonId 使用 {skeletonId}。
            """;

        var tot = await Llm.TreeSearchAsync(new TreeSearchRequest
        {
            // 27 号 §7.2/§7.3：按任务路由 Provider + 超时分级。
            // context.ProviderCode 显式指定时优先（编排器/测试可覆盖）；否则走 AI:ProviderRouting["pm-skill"]。
            ProviderCode = !string.IsNullOrWhiteSpace(context.ProviderCode)
                ? context.ProviderCode
                : Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            BranchCount = TotBranchCount,
            BaseTemperature = 0.3,
            TemperatureStep = 0.35,
            ResponseFormat = "json",
            MaxTokens = TotMaxTokens,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
        }, ct);

        if (!tot.IsSuccess || !tot.Succeeded.Any())
            throw Oops.Bah($"PM Skill ToT 全部分支 LLM 失败: {tot.Error}");

        var keyword = ExtractSearchKeyword(context);
        var scored = new List<(string Json, decimal Score, int BranchIndex, double Temperature)>();

        foreach (var candidate in tot.Succeeded)
        {
            string json;
            try
            {
                json = ExtractJson(candidate.Content);
                // #region agent log
                try
                {
                    var content = candidate.Content ?? string.Empty;
                    var trimmed = content.TrimEnd();
                    var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["sessionId"] = "ead5d0",
                        ["runId"] = "post-fix",
                        ["hypothesisId"] = "A",
                        ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:validate",
                        ["message"] = "tot-branch-before-parse",
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ["data"] = new Dictionary<string, object?>
                        {
                            ["branch"] = candidate.BranchIndex,
                            ["temperature"] = candidate.Temperature,
                            ["tokensIn"] = candidate.TokensIn,
                            ["tokensOut"] = candidate.TokensOut,
                            ["maxTokens"] = TotMaxTokens,
                            ["hitMaxTokens"] = candidate.TokensOut >= TotMaxTokens,
                            ["contentLen"] = content.Length,
                            ["endsWithBrace"] = trimmed.EndsWith('}'),
                            ["jsonExtractLen"] = json.Length,
                            ["head"] = content.Length <= 120 ? content : content[..120],
                            ["tail"] = content.Length <= 160 ? content : content[^160..],
                        },
                    });
                    File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", payload + Environment.NewLine);
                }
                catch { /* debug ingest must not break PM */ }
                // #endregion
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("businessEvents", out var events)
                    || events.ValueKind != JsonValueKind.Array
                    || events.GetArrayLength() == 0)
                {
                    // #region agent log
                    try
                    {
                        var miss = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["sessionId"] = "ead5d0",
                            ["runId"] = "post-fix",
                            ["hypothesisId"] = "E",
                            ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:missingEvents",
                            ["message"] = "tot-branch-missing-businessEvents",
                            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            ["data"] = new Dictionary<string, object?>
                            {
                                ["branch"] = candidate.BranchIndex,
                                ["tokensOut"] = candidate.TokensOut,
                                ["rootProps"] = doc.RootElement.EnumerateObject().Select(p => p.Name).Take(12).ToArray(),
                            },
                        });
                        File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", miss + Environment.NewLine);
                    }
                    catch { }
                    // #endregion
                    _logger.LogWarning(
                        "PM ToT 分支 {Branch}@{Temp} 缺少 businessEvents，跳过",
                        candidate.BranchIndex, candidate.Temperature);
                    continue;
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try
                {
                    var content = candidate.Content ?? string.Empty;
                    var fail = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["sessionId"] = "ead5d0",
                        ["runId"] = "post-fix",
                        ["hypothesisId"] = "A",
                        ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:parseFail",
                        ["message"] = "tot-branch-json-invalid",
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ["data"] = new Dictionary<string, object?>
                        {
                            ["branch"] = candidate.BranchIndex,
                            ["tokensOut"] = candidate.TokensOut,
                            ["maxTokens"] = TotMaxTokens,
                            ["hitMaxTokens"] = candidate.TokensOut >= TotMaxTokens,
                            ["contentLen"] = content.Length,
                            ["exType"] = ex.GetType().Name,
                            ["exMsg"] = ex.Message.Length <= 200 ? ex.Message : ex.Message[..200],
                        },
                    });
                    File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", fail + Environment.NewLine);
                }
                catch { }
                // #endregion
                _logger.LogWarning(ex,
                    "PM ToT 分支 {Branch}@{Temp} JSON 无效，跳过",
                    candidate.BranchIndex, candidate.Temperature);
                continue;
            }

            var score = await ScoreCandidateAsync(json, keyword, ct);
            scored.Add((json, score, candidate.BranchIndex, candidate.Temperature));
            _logger.LogInformation(
                "PM ToT 分支 {Branch}@{Temp} score={Score} tokens={In}/{Out}",
                candidate.BranchIndex, candidate.Temperature, score,
                candidate.TokensIn, candidate.TokensOut);
        }

        if (scored.Count == 0)
            throw Oops.Bah("PM Skill ToT 全部分支产出无效（JSON 或 businessEvents 校验失败）");

        var top = SelectTopCandidate(scored);
        _logger.LogInformation(
            "PM ToT Top-1: branch={Branch} temp={Temp} score={Score}",
            top.BranchIndex, top.Temperature, top.Score);

        using var topDoc = JsonDocument.Parse(top.Json);
        return NormalizeSkeletonJson(topDoc.RootElement, skeletonId, fragmentId);
    }

    private async Task<decimal> ScoreCandidateAsync(string candidateJson, string keyword, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { candidateJson, keyword }, JsonOptions);
        var result = await Mcp.CallToolAsync("kg.score-candidate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"kg.score-candidate 失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        if (doc.RootElement.TryGetProperty("score", out var scoreEl)
            && scoreEl.TryGetDecimal(out var score))
        {
            return score;
        }

        return 0m;
    }

    /// <summary>按 kg.score-candidate 评分选 Top-1；同分取首条。</summary>
    public static (string Json, decimal Score, int BranchIndex, double Temperature) SelectTopCandidate(
        IReadOnlyList<(string Json, decimal Score, int BranchIndex, double Temperature)> scored)
    {
        var best = scored[0];
        for (var i = 1; i < scored.Count; i++)
        {
            if (scored[i].Score > best.Score)
                best = scored[i];
        }

        return best;
    }

    public static string ExtractSearchKeyword(SkillContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.UserRequirement))
        {
            var trimmed = context.UserRequirement.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed[..80];
        }

        return context.SeedMatches.FirstOrDefault()?.EventNamePattern ?? "enterprise";
    }

    private static string NormalizeSkeletonJson(JsonElement root, string skeletonId, string fragmentId)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["skeletonId"] = JsonSerializer.SerializeToElement(skeletonId);
        if (!dict.ContainsKey("version"))
            dict["version"] = JsonSerializer.SerializeToElement(1);

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    public static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed[start..(end + 1)];
        }

        return trimmed;
    }
}
