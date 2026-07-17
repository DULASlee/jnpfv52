using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 分析师 Skill — S2 compile 模式走 SaNineViewCompiler；agent 模式走 sa-service run-async（回归对比）。
/// </summary>
public sealed class AnalystSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISaOrchestratorAdapter _saAdapter;
    private readonly ISaNineViewCompiler _compiler;
    private readonly SaPipelineOptions _pipelineOptions;
    private readonly IAnalysisCompletedCompletenessGate _completenessGate;
    private readonly IIrEventStoreService _eventStore;
    private readonly IExperienceRecorder _experience;
    private readonly ILogger<AnalystSkillService> _logger;

    // ── Round 3 工程接线依赖 ─────────────────────────────────────────
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ISystemDesignLockedCompletenessGate _consistencyGate;
    private readonly ISaMaterializer _materializer;
    private readonly ILightStructureValidator _lightValidator;
    private readonly ISqlSugarClient _db;
    // 28 号 §3/§5/§6/§7：四组件（2026-07-10 接线 P0-2，原为孤儿代码）
    private readonly IDddProjection _dddProjection;
    private readonly IConsistencyChecker _consistencyChecker;
    private readonly IQualityScoreCalculator _qualityScoreCalculator;
    private readonly IRequirementDocumentRenderer _documentRenderer;
    private readonly IPipelineDeliverableService _deliverables;

    public AnalystSkillService(
        ICognitiveSkillToolkit toolkit,
        ISaOrchestratorAdapter saAdapter,
        ISaNineViewCompiler compiler,
        IOptions<SaPipelineOptions> pipelineOptions,
        IAnalysisCompletedCompletenessGate completenessGate,
        IIrEventStoreService eventStore,
        IExperienceRecorder experience,
        ILogger<AnalystSkillService> logger,
        EntityDesignRepository entityDesignRepo,
        ISystemDesignLockedCompletenessGate consistencyGate,
        ISaMaterializer materializer,
        ILightStructureValidator lightValidator,
        ISqlSugarClient db,
        IDddProjection dddProjection,
        IConsistencyChecker consistencyChecker,
        IQualityScoreCalculator qualityScoreCalculator,
        IRequirementDocumentRenderer documentRenderer,
        IPipelineDeliverableService deliverables)
        : base(toolkit)
    {
        _saAdapter = saAdapter;
        _compiler = compiler;
        _pipelineOptions = pipelineOptions.Value;
        _completenessGate = completenessGate;
        _eventStore = eventStore;
        _experience = experience;
        _logger = logger;
        _entityDesignRepo = entityDesignRepo;
        _consistencyGate = consistencyGate;
        _materializer = materializer;
        _lightValidator = lightValidator;
        _db = db;
        _dddProjection = dddProjection;
        _consistencyChecker = consistencyChecker;
        _qualityScoreCalculator = qualityScoreCalculator;
        _documentRenderer = documentRenderer;
        _deliverables = deliverables;
    }

    public override string SkillId => "analyst-skill";
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.RefineSpecification;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.Skeleton },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.SaStepCompleted,
            IrEventTypes.EventSpecConfirmed,
            IrEventTypes.AnalysisCompleted,
        },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架未 stable，请先确认骨架"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (!events.Any(e => e.EventType == IrEventTypes.AnalysisCompleted))
            return Task.FromResult(SkillValidationResult.Fail("缺少 AnalysisCompleted 事件"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)!;
        var skeletonPayload = skeleton.Payload;

        // Round 2：在 Compile 之前做受控语义分析并写回骨架（Analyst 重新成为「分析」主体）
        if (context.EnableSemanticAnalysis
            && !context.EnableFinalization
            && _pipelineOptions.IsCompileMode)
        {
            await foreach (var evt in EnrichSkeletonViaSemanticAnalysisAsync(context, skeleton, ct))
                yield return evt;

            // 刷新：若已写回 SkeletonCreated，后续 Compile 必须用新骨架
            var refreshed = await BuildSnapshotAsync(
                context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
            var updated = refreshed.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
            if (updated != null)
                skeletonPayload = updated.Payload;
        }

        var businessEvents = ParseBusinessEvents(skeletonPayload);
        if (businessEvents.Count == 0)
            throw Oops.Bah("IR-0 无 businessEvents，无法启动分析师 Skill");

        // 断点续跑：读取 Snapshot 中已完成的事件，跳过无需重跑的
        var alreadyConfirmed = context.Snapshot.Fragments
            .Where(f => f.FragmentType == IrFragmentTypes.EventSpec
                     && f.StabilityState == IrStabilityStates.Stable
                     && !string.IsNullOrEmpty(f.FragmentId))
            .Select(f => f.FragmentId!.Replace("eventspec:", ""))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pendingEvents = businessEvents
            .Where(e => !alreadyConfirmed.Contains(e.EventId))
            .ToList();

        _logger.LogInformation(
            "AnalystSkill: 共 {Total} 个事件，{Pending} 个待分析，{Done} 个已完成（断点续跑） semantic={Sem}",
            businessEvents.Count, pendingEvents.Count, alreadyConfirmed.Count, context.EnableSemanticAnalysis);

        SaNineViewCompileResult? compileResult = null;

        if (pendingEvents.Count > 0)
        {
            SaProjectResult saResult;

            if (_pipelineOptions.IsCompileMode)
            {
                var pipelineTitle = await LoadPipelineTitleAsync(context.PipelineId, ct);
                var requirementText = await ResolveRequirementTextAsync(context, ct);
                compileResult = _compiler.CompileFromSkeletonJson(
                    skeletonPayload,
                    requirementText,
                    pipelineTitle);

                _logger.LogInformation(
                    "SaNineViewCompiler 完成：{EventCount} 事件，{Duration}ms hash={Hash}",
                    compileResult.EventResults.Count, compileResult.CompileDurationMs, compileResult.BundleHash);

                yield return new AppendIrEventRequest
                {
                    EventType = IrEventTypes.SaNineViewCompiled,
                    Payload = JsonSerializer.Serialize(new
                    {
                        tenantId = context.TenantId,
                        projectId = context.ProjectId,
                        pipelineId = context.PipelineId,
                        bundleHash = compileResult.BundleHash,
                        compileMs = compileResult.CompileDurationMs,
                        eventCount = compileResult.EventResults.Count,
                        bundle = new
                        {
                            projectSteps = compileResult.ProjectSteps,
                            eventResults = compileResult.EventResults,
                            compileDurationMs = compileResult.CompileDurationMs,
                            bundleHash = compileResult.BundleHash,
                        },
                    }, JsonOptions),
                    SkillId = SkillId,
                };

                saResult = compileResult.ToProjectResult();
            }
            else
            {
                saResult = await _saAdapter.RunProjectAsync(
                    context.TenantId, context.ProjectId, context.PipelineId,
                    context.UserRequirement ?? skeleton.Payload,
                    businessEvents.Select(e => new SaSkeletonEventInput(e.EventId, e.EventName, e.ComplexityHint)).ToList(),
                    context.RunId, ct);

                _logger.LogInformation(
                    "SA agent 完成：{EventCount} 个事件，耗时 {Duration}ms",
                    saResult.EventResults.Count, saResult.TotalDurationMs);
            }

            foreach (var eventResult in saResult.EventResults)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var (stepName, stepOutput) in eventResult.Steps)
                {
                    yield return new AppendIrEventRequest
                    {
                        EventType = IrEventTypes.SaStepCompleted,
                        FragmentId = $"eventspec:{eventResult.EventId}",
                        FragmentType = IrFragmentTypes.EventSpec,
                        Payload = JsonSerializer.Serialize(new
                        {
                            eventId = eventResult.EventId,
                            step = stepName,
                            output = stepOutput,
                            source = _pipelineOptions.IsCompileMode ? "SaNineViewCompiler" : "sa-service",
                        }, JsonOptions),
                        SkillId = SkillId,
                        SaStepName = stepName,
                    };
                }

                if (string.IsNullOrEmpty(eventResult.Error))
                {
                    var meta = pendingEvents.FirstOrDefault(e => e.EventId == eventResult.EventId)
                            ?? new BusinessEventMeta(eventResult.EventId, eventResult.EventName, eventResult.Complexity);

                    var steps = eventResult.Steps.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value,
                        StringComparer.Ordinal);

                    var eventSpecPayload = EventSpecAssembler.BuildPayloadJson(eventResult.EventId, meta, steps);
                    await ValidateEventSpecViaIoiAsync(eventSpecPayload, ct);

                    yield return new AppendIrEventRequest
                    {
                        EventType = IrEventTypes.EventSpecConfirmed,
                        FragmentId = $"eventspec:{eventResult.EventId}",
                        FragmentType = IrFragmentTypes.EventSpec,
                        Payload = eventSpecPayload,
                    };
                }
                else
                {
                    _logger.LogWarning("事件 {EventId} SA 失败（{Error}），跳过 EventSpecConfirmed",
                        eventResult.EventId, eventResult.Error);
                }
            }

            if (_pipelineOptions.IsCompileMode)
            {
                await RecordS2DualReviewAsync(context, compileResult!, businessEvents.Count, ct);
            }
        }

        // Round 2 语义分析后：即使事件已全部 confirmed（pending=0），仍须 Compile 以消费新骨架
        if (compileResult == null
            && context.EnableSemanticAnalysis
            && !context.EnableFinalization
            && _pipelineOptions.IsCompileMode)
        {
            var pipelineTitle = await LoadPipelineTitleAsync(context.PipelineId, ct);
            compileResult = _compiler.CompileFromSkeletonJson(
                skeletonPayload,
                await ResolveRequirementTextAsync(context, ct),
                pipelineTitle);
            _logger.LogInformation(
                "AnalystSkill: Round2 语义分析后强制 Compile（pending=0）events={Count} hash={Hash}",
                compileResult.EventResults.Count, compileResult.BundleHash);

            yield return new AppendIrEventRequest
            {
                EventType = IrEventTypes.SaNineViewCompiled,
                Payload = JsonSerializer.Serialize(new
                {
                    tenantId = context.TenantId,
                    projectId = context.ProjectId,
                    pipelineId = context.PipelineId,
                    bundleHash = compileResult.BundleHash,
                    compileMs = compileResult.CompileDurationMs,
                    eventCount = compileResult.EventResults.Count,
                    source = "semantic-analysis-recompile",
                }, JsonOptions),
                SkillId = SkillId,
            };
        }

        // ── Round 3 工程一次性保障（27 号 §5.2 / §9：前两轮零工程步骤，仅 Round 3 落库）──
        // enableFinalization 由三轮编排器经 SkillRunOptions → SkillContext 透传：
        // Round 1/2=false（只编译 SA + 产出 IR 事件 + 内存收集 Assumptions，不投影/不门禁/不 Materializer），
        // Round 3=true（一次性工程保障）。默认 true 保持非编排器直接调用兼容。
        //
        // P0 修复：编排器 Round 2 已确认全部事件后，Round 3 再跑时 pendingEvents=0，
        // compileResult 为 null 会导致工程接线整块跳过。enableFinalization 时强制从 skeleton 重编译，
        // 并合并 IR 中跨轮持久化的 AssumptionsCollected fragment。
        if (context.EnableFinalization
            && compileResult == null
            && _pipelineOptions.IsCompileMode
            && _pipelineOptions.EnableEngineeringWiring)
        {
            _logger.LogInformation(
                "AnalystSkill: enableFinalization=true 但 compileResult 为空（断点续跑/Round3），从 skeleton 重编译");
            var pipelineTitle = await LoadPipelineTitleAsync(context.PipelineId, ct);
            compileResult = _compiler.CompileFromSkeletonJson(
                skeletonPayload,
                await ResolveRequirementTextAsync(context, ct),
                pipelineTitle);
            compileResult = MergePersistedAssumptions(context.Snapshot, compileResult);
        }
        else if (compileResult != null)
        {
            compileResult = MergePersistedAssumptions(context.Snapshot, compileResult);
        }

        if (context.EnableFinalization
            && _pipelineOptions.EnableEngineeringWiring
            && _pipelineOptions.IsCompileMode
            && compileResult == null)
        {
            throw Oops.Bah("Round 3 工程保障失败：无法从 skeleton 重编译 compileResult，禁止静默跳过");
        }

        yield return await FinalizeAsync(context, compileResult, businessEvents.Count, context.EnableFinalization, ct);
    }

    /// <summary>
    /// Round 2 受控语义分析：基于用户澄清与当前骨架，产出 Typed patches 并写回 Skeleton。
    /// 失败即抛硬错误（禁止静默跳过）；禁止散文改骨架。
    /// </summary>
    private async IAsyncEnumerable<AppendIrEventRequest> EnrichSkeletonViaSemanticAnalysisAsync(
        SkillContext context,
        IrSnapshotFragment skeleton,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var skeletonJson = skeleton.Payload?.Trim() ?? "{}";
        if (skeletonJson.Length >= 2 && skeletonJson[0] == '"' && skeletonJson[^1] == '"')
        {
            try { skeletonJson = JsonSerializer.Deserialize<string>(skeletonJson) ?? "{}"; }
            catch (JsonException) { /* keep */ }
        }

        if (!skeletonJson.TrimStart().StartsWith('{'))
            yield break;

        var answersHint = context.UserRequirement ?? string.Empty;
        var brief = skeletonJson.Length > 3500 ? skeletonJson[..3500] : skeletonJson;
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是系统需求分析师。在 C# 编译器 Compile 之前，请对需求骨架做受控语义完善。
                只输出 JSON：{"patches":[],"summaryMarkdown":""}。
                patches 操作：AddEntity|AddEvent|PatchRule|AddField|PatchSummary|AddStateTransition。
                字段格式：{"operation":"","target":"","name":"","displayName":"","type":"","description":"","required":false,"references":"","scopeEventId":"","from":"","to":""}
                约束：
                - 根据用户澄清补充业务规则、状态流转、缺失字段、事件说明。
                - 不要删除已有实体；不确定的不要编造。
                - 必须把关键分析结论写入 PatchRule 或 PatchSummary。
                """,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                    用户澄清 / 上下文：
                    {answersHint}

                    当前骨架 JSON（节选）：
                    {brief}
                    """),
            },
            Temperature = 0.2,
            MaxTokens = 2048,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        IReadOnlyList<AmendmentPatch> patches = Array.Empty<AmendmentPatch>();
        try
        {
            var response = await Llm.ChatAsync(request, ct);
            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                // 硬错误：LLM 失败即抛
                throw Oops.Bah($"分析师语义分析 LLM 失败: {response.Error ?? "(空响应)"} pipeline={context.PipelineId} tenantId={context.TenantId}");
            }

            var json = response.Content.Trim();
            if (json.StartsWith("```"))
            {
                var s = json.IndexOf('{');
                var e = json.LastIndexOf('}');
                if (s >= 0 && e > s) json = json[s..(e + 1)];
            }
            using var doc = JsonDocument.Parse(json);
            patches = AmendmentPatchApplier.ParsePatches(doc.RootElement);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            // 硬错误：异常即抛（FriendlyException 透传，其余包装为业务错误）
            if (ex is JNPF.FriendlyException.AppFriendlyException) throw;
            _logger.LogError(ex, "AnalystSkill 语义分析解析失败 pipeline={PipelineId}", context.PipelineId);
            throw Oops.Bah($"分析师语义分析 LLM 失败: {ex.Message} pipeline={context.PipelineId} tenantId={context.TenantId}");
        }

        if (patches.Count == 0)
            yield break;

        var patched = AmendmentPatchApplier.ApplyToSkeletonJson(skeletonJson, patches);
        if (string.Equals(patched, skeletonJson, StringComparison.Ordinal))
            yield break;

        _logger.LogInformation(
            "AnalystSkill 语义分析写回骨架 tenant={TenantId} project={ProjectId} pipeline={PipelineId} patches={Count}",
            context.TenantId, context.ProjectId, context.PipelineId, patches.Count);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = skeleton.FragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            Payload = patched,
            SkillId = SkillId,
        };

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.FragmentStabilized,
            FragmentId = skeleton.FragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId = skeleton.FragmentId,
                stabilityState = IrStabilityStates.Stable,
                confirmedBy = "analyst-skill-semantic",
            }, JsonOptions),
            SkillId = SkillId,
        };
    }

    /// <summary>
    /// 27 号 §5.2：Round 3 工程一次性保障 —— 投影 ai_entity_field → 轻量校验 →
    /// R1-R3 一致性门禁 → Materializer 九表 → sa_assumptions → 完整性门禁 → AnalysisCompleted。
    /// enableFinalization=false（Round 1/2）时跳过投影/门禁/Materializer，仅产出 AnalysisCompleted 标记本轮编译完成。
    /// </summary>
    /// <remarks>
    /// 从 ThinkAsync 内联块（原 :252-315）抽取，行为严格不变：
    ///   - 工程接线守卫条件保留：_pipelineOptions.EnableEngineeringWiring && IsCompileMode && compileResult != null
    ///   - completenessGate + AnalysisCompleted 始终执行（不受 enableFinalization 影响）
    /// </remarks>
    private async Task<AppendIrEventRequest> FinalizeAsync(
        SkillContext context,
        SaNineViewCompileResult? compileResult,
        int businessEventCount,
        bool enableFinalization,
        CancellationToken ct)
    {
        var freshSnapshot = await BuildSnapshotAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);

        if (enableFinalization
            && _pipelineOptions.EnableEngineeringWiring
            && _pipelineOptions.IsCompileMode
            && compileResult != null)
        {
            _logger.LogInformation("AnalystSkill: 开始 Round 3 工程接线（投影+门禁+物化+假设落库）");

            var triple = new PipelineTriple(context.TenantId, context.ProjectId, context.PipelineId);

            // ① 投影（内存）——三元组使用 context 真实值
            var projectionOptions = new EntityDesignProjectionOptions
            {
                TenantId = context.TenantId,
                ProjectId = context.ProjectId,
                PipelineId = context.PipelineId.ToString(),
            };
            var projection = EntityDesignProjector.Project(freshSnapshot, projectionOptions);
            _logger.LogInformation(
                "AnalystSkill: 投影完成 {FieldCount} fields, {TableCount} tables, hash={Hash}",
                projection.Fields.Count, projection.TableNames().Count, projection.ProjectionHash);

            // ② 轻量结构校验器（WARNING 日志，不阻断）
            var warnings = _lightValidator.Validate(compileResult.Source);
            foreach (var w in warnings)
                _logger.LogWarning("LightStructureValidator: {Warning}", w);

            // ③ R1-R3 跨层一致性门禁（阻断）——须在落库前，避免门禁失败留下半成品
            var consistencyResult = await _consistencyGate.ValidateAsync(freshSnapshot, triple, ct);
            if (!consistencyResult.IsValid)
                throw Oops.Bah(consistencyResult.ErrorMessage ?? "跨层一致性门禁未通过");

            // ④ 投影 + Materializer 同事务（26 号 §7.3）：门禁通过后一次性提交
            await _db.Ado.BeginTranAsync();
            try
            {
                await _entityDesignRepo.PersistAsync(projection, ct);
                var materializeResult = await _materializer.MaterializeAsync(triple, compileResult, ct);
                await _db.Ado.CommitTranAsync();
                _logger.LogInformation(
                    "AnalystSkill: 物化完成 scope={ScopeId} dict={DictId} events={EventCount} ms={DurationMs}",
                    materializeResult.ScopeId, materializeResult.DictId,
                    materializeResult.EventCount, materializeResult.DurationMs);
            }
            catch
            {
                try { await _db.Ado.RollbackTranAsync(); } catch { /* best effort */ }
                throw;
            }

            // ⑤ sa_assumptions 独立事务写入（衍生数据可重建，失败→告警不阻断）
            if (compileResult.Assumptions is { Count: > 0 })
            {
                try
                {
                    await PersistAssumptionsAsync(context, compileResult.Assumptions, ct);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "sa_assumptions 写入失败（不阻断工程保障）pipeline={PipelineId}", context.PipelineId);
                }
            }

            // ⑥ DDD 实时推导（28 号 §3：纯内存，<50ms，不落库，供质量评分 + 渲染）
            var dddResult = _dddProjection.Project(compileResult, projection);
            _logger.LogInformation(
                "AnalystSkill: DDD 推导完成 confidence={Conf:F2} pending={Pending} subDomains={Sub} roots={Roots}",
                dddResult.OverallConfidence, dddResult.PendingConfirmations.Count,
                dddResult.DomainModel.SubDomains.Count,
                dddResult.AggregateDesign.RootEntities.Count);

            // ⑥b SG2-E1/E2 企业可用门禁：身份非空；低置信度必须有待确认清单（禁止静默假绿）
            var pipelineTitle = await LoadPipelineTitleAsync(context.PipelineId, ct);
            var requirementText = await ResolveRequirementTextAsync(context, ct);
            var identity = compileResult.Source.ResolveIdentity(pipelineTitle, requirementText);
            if (string.IsNullOrWhiteSpace(identity.SystemName)
                || string.IsNullOrWhiteSpace(identity.RequirementSummary)
                || identity.SystemName is "—" or "-"
                || identity.RequirementSummary is "—" or "-")
            {
                throw Oops.Bah(
                    "Round 3 Finalize 阻断：项目名称/需求概要为空或「—」，禁止产出空壳 02（SG2-E1）");
            }

            if (dddResult.HasUnguardedLowConfidence)
            {
                var views = string.Join("、", dddResult.CollectLowConfidenceViews());
                throw Oops.Bah(
                    $"Round 3 Finalize 阻断：DDD 视角 [{views}] 置信度偏低且未生成待确认项（SG2-E2）");
            }

            // ⑦ 一致性检查器（28 号 §5：4 条规则，写 sa_consistency）
            var consistencyFindings = await _consistencyChecker.CheckAsync(
                triple, compileResult, projection, roundNumber: 3, ct);

            // ⑧ 质量评分器（28 号 §6：5 维度加权，写 sa_quality_score）
            var qualityScore = await _qualityScoreCalculator.CalculateAsync(
                triple, compileResult, projection, dddResult, consistencyFindings, roundNumber: 3, ct);

            // ⑨ 需求分析书渲染（28 号 §7：渲染并落盘 deliverable）
            // 用身份补全后的 Source 渲染，避免表头「—」
            var renderCompile = new SaNineViewCompileResult
            {
                Source = identity,
                ProjectSteps = compileResult.ProjectSteps,
                EventResults = compileResult.EventResults,
                Assumptions = compileResult.Assumptions,
                BundleHash = compileResult.BundleHash,
                CompileDurationMs = compileResult.CompileDurationMs,
            };
            var clarificationAnswers = await LoadRequirementClarificationAppendicesAsync(
                context.TenantId, context.ProjectId, context.PipelineId, ct);
            var documentMarkdown = _documentRenderer.Render(
                triple, renderCompile, dddResult, projection,
                consistencyFindings, qualityScore, roundNumber: 3, clarificationAnswers, ct);

            // Gap 2：LLM 审查澄清答案 vs 编译规范完整性（失败即抛，禁止伪成功）
            if (clarificationAnswers is { Count: > 0 })
            {
                try
                {
                    var reviewResult = await ReviewClarificationAnswersAgainstSpecAsync(
                        compileResult.Source, documentMarkdown, clarificationAnswers, context, ct);
                    if (reviewResult.Executed && !string.IsNullOrWhiteSpace(reviewResult.ReviewMarkdown))
                    {
                        documentMarkdown = InjectReviewAppendix(documentMarkdown, reviewResult);
                        _logger.LogInformation(
                            "AnalystSkill: 澄清→规范审查完成 missedItems={Count}", reviewResult.MissedItems.Count);
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
                {
                    throw Oops.Bah($"分析师澄清答案审查 LLM 失败: {ex.Message} pipeline={context.PipelineId}");
                }
            }

            await _deliverables.SaveRequirementSpecAsync(
                context.TenantId, context.PipelineId, documentMarkdown, ct);
            _logger.LogInformation(
                "AnalystSkill: 需求分析书渲染落盘 quality={Total:F1} pass={Pass} docLen={Len}",
                qualityScore.TotalScore, qualityScore.PassesGate(
                    consistencyFindings.Count(f => f.Severity == "CRITICAL")), documentMarkdown.Length);
        }

        // 完整性门禁
        var gate = await _completenessGate.ValidateAsync(
            context.TenantId, context.ProjectId, freshSnapshot, context.RunId, ct);
        if (!gate.IsValid)
            throw Oops.Bah(gate.ErrorMessage ?? "AnalysisCompleted 完整性门禁未通过");

        return new AppendIrEventRequest
        {
            EventType = IrEventTypes.AnalysisCompleted,
            Payload = JsonSerializer.Serialize(new
            {
                tenantId = context.TenantId,
                projectId = context.ProjectId,
                pipelineId = context.PipelineId,
                eventSpecCount = businessEventCount,
                s2Mode = _pipelineOptions.S2Mode,
                allStable = true,
                finalized = enableFinalization,  // 标记是否完成最终工程保障（编排器据此区分 Round 1/2 vs Round 3）
            }, JsonOptions),
        };
    }

    private async Task RecordS2DualReviewAsync(
        SkillContext context,
        SaNineViewCompileResult compileResult,
        int skeletonEventCount,
        CancellationToken ct)
    {
        if (compileResult.EventResults.Count != skeletonEventCount)
            throw Oops.Bah($"Compiler 事件数 {compileResult.EventResults.Count} 与骨架 {skeletonEventCount} 不一致");

        var pmDetail = JsonSerializer.Serialize(new
        {
            source = "compile-dual-review",
            pipelineId = context.PipelineId,
            bundleHash = compileResult.BundleHash,
            eventCount = compileResult.EventResults.Count,
        }, JsonOptions);

        await _experience.RecordReviewAsync(
            context.ProjectId, context.TenantId, "pm-skill", context.RunId,
            "pm-s2-pass", pmDetail, ct);

        await _experience.RecordReviewAsync(
            context.ProjectId, context.TenantId, SkillId, context.RunId,
            "analyst-s2-pass", pmDetail, ct);
    }

    private async Task ValidateEventSpecViaIoiAsync(string eventSpecPayload, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { eventSpecPayload }, JsonOptions);
        var result = await Mcp.CallToolAsync("ioi.validate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"ioi.validate 工具失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        if (doc.RootElement.TryGetProperty("valid", out var validEl)
            && validEl.ValueKind == JsonValueKind.False)
        {
            var reason = doc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
            throw Oops.Bah(reason ?? "EventSpec IOI 不变量校验失败");
        }

        if (!doc.RootElement.TryGetProperty("valid", out var okEl) || !okEl.GetBoolean())
            throw Oops.Bah("EventSpec IOI 不变量校验失败");
    }

    private async Task<IrSnapshot> BuildSnapshotAsync(string tenantId, string projectId, string pipelineId, CancellationToken ct)
    {
        var dtos = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId, ct);
        return new IrSnapshot
        {
            Fragments = dtos.Select(d => new IrSnapshotFragment
            {
                FragmentId = d.FragmentId,
                FragmentType = d.FragmentType,
                StabilityState = d.StabilityState,
                Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload, JsonOptions),
                SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
            }).ToList(),
        };
    }

    /// <summary>
    /// 将 Round 3 假设项独立事务写入 sa_assumptions 表。
    /// Materializer 事务已提交，此方法与 SA 九表写入不在同一事务内。
    /// </summary>
    private async Task PersistAssumptionsAsync(
        SkillContext context, IReadOnlyList<Assumption> assumptions, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = assumptions.Select(a => new SaAssumptionRow
        {
            F_Id = Guid.NewGuid().ToString("N"),
            F_TenantId = context.TenantId,
            F_ProjectId = context.ProjectId,
            F_PIPELINE_ID = context.PipelineId.ToString(),
            F_EventId = string.IsNullOrWhiteSpace(a.EventId) ? null : a.EventId,
            F_SourceStep = a.SourceStep,
            F_AssumptionText = a.Text,
            F_Confidence = a.Confidence,
            F_IsUserConfirmed = false,
            F_UserVerdict = null,
            F_RoundCreated = 3,
            F_CreatedAt = now,
        }).ToList();

        await _db.Insertable(rows).AS("sa_assumptions").ExecuteCommandAsync(ct);
        _logger.LogInformation("AnalystSkill: 写入 {Count} 条假设项到 sa_assumptions", rows.Count);
    }

    /// <summary>sa_assumptions 表行映射（SqlSugar Insertable 要求具体类型，不支持匿名）。</summary>
    private sealed class SaAssumptionRow
    {
        public string F_Id { get; set; } = string.Empty;
        public string F_TenantId { get; set; } = string.Empty;
        public string F_ProjectId { get; set; } = string.Empty;
        public string F_PIPELINE_ID { get; set; } = string.Empty;
        public string? F_EventId { get; set; }
        public string F_SourceStep { get; set; } = string.Empty;
        public string F_AssumptionText { get; set; } = string.Empty;
        public decimal F_Confidence { get; set; }
        public bool F_IsUserConfirmed { get; set; }
        public string? F_UserVerdict { get; set; }
        public int F_RoundCreated { get; set; }
        public DateTime F_CreatedAt { get; set; }
    }

    /// <summary>
    /// 合并 IR fragment <c>assumptions:{projectId}</c> 中跨轮持久化的假设项（P1）。
    /// 编排器 Round 1/2 暂停恢复后内存 Assumptions 会丢失，须从 IR 回捞。
    /// </summary>
    private static SaNineViewCompileResult MergePersistedAssumptions(
        IrSnapshot snapshot, SaNineViewCompileResult compileResult)
    {
        var frag = snapshot.Fragments.FirstOrDefault(f =>
            f.FragmentType == IrFragmentTypes.Assumptions
            && f.StabilityState is IrStabilityStates.InProgress or IrStabilityStates.Stable);
        if (frag?.Payload is null || string.IsNullOrWhiteSpace(frag.Payload))
            return compileResult;

        try
        {
            using var doc = JsonDocument.Parse(frag.Payload);
            if (!doc.RootElement.TryGetProperty("assumptions", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return compileResult;

            var merged = compileResult.Assumptions.ToList();
            var existing = new HashSet<string>(
                merged.Select(a => $"{a.EventId}|{a.SourceStep}|{a.Text}"),
                StringComparer.Ordinal);

            foreach (var el in arr.EnumerateArray())
            {
                var eventId = el.TryGetProperty("eventId", out var e) ? e.GetString() ?? "" : "";
                var source = el.TryGetProperty("sourceStep", out var s) ? s.GetString() ?? "" : "";
                var text = el.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                var conf = el.TryGetProperty("confidence", out var c) && c.TryGetDecimal(out var d) ? d : 0.5m;
                if (string.IsNullOrWhiteSpace(text)) continue;
                var key = $"{eventId}|{source}|{text}";
                if (!existing.Add(key)) continue;
                merged.Add(new Assumption(eventId, source, text, conf));
            }

            return new SaNineViewCompileResult
            {
                Source = compileResult.Source,
                ProjectSteps = compileResult.ProjectSteps,
                EventResults = compileResult.EventResults,
                CompileDurationMs = compileResult.CompileDurationMs,
                BundleHash = compileResult.BundleHash,
                Assumptions = merged,
            };
        }
        catch (JsonException)
        {
            return compileResult;
        }
    }

    private async Task<string?> LoadPipelineTitleAsync(long pipelineId, CancellationToken ct)
    {
        try
        {
            var name = await _db.Queryable<AiPipelineEntity>()
                .Where(x => x.Id == pipelineId.ToString())
                .Select(x => x.Name)
                .FirstAsync(ct);
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            _logger.LogWarning(ex, "读取 pipeline 标题失败 pipeline={PipelineId}", pipelineId);
            return null;
        }
    }

    /// <summary>
    /// 需求原文：优先 SkillContext；空/JSON 时回退到 pipeline 用户消息（不因 Tenant 过滤丢原文）。
    /// </summary>
    private async Task<string?> ResolveRequirementTextAsync(SkillContext context, CancellationToken ct)
    {
        var fromContext = NormalizeRequirementText(context.UserRequirement);
        if (fromContext != null)
            return fromContext;

        try
        {
            var msg = await _db.Queryable<AiPipelineMessageEntity>()
                .Where(x => x.PipelineId == context.PipelineId.ToString() && x.Role == "user")
                .OrderByDescending(x => x.CreatorTime)
                .Select(x => x.Content)
                .FirstAsync(ct);
            return NormalizeRequirementText(msg);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            _logger.LogWarning(ex, "回退读取用户需求失败 pipeline={PipelineId}", context.PipelineId);
            return null;
        }
    }

    /// <summary>空串 / Skeleton JSON 不得当作需求原文（否则表头退化成「业务」）。</summary>
    private static string? NormalizeRequirementText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var t = text.TrimStart();
        if (t.StartsWith('{') || t.StartsWith('['))
            return null;
        return text.Trim();
    }

    private async Task<IReadOnlyList<ClarificationAnswerAppendix>> LoadRequirementClarificationAppendicesAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        var result = new List<ClarificationAnswerAppendix>();
        foreach (var evt in events)
        {
            if (!string.Equals(evt.EventType, IrEventTypes.ClarificationAnswered, StringComparison.Ordinal))
                continue;
            var raw = evt.PayloadPreview;
            if (string.IsNullOrWhiteSpace(raw)) continue;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                var stage = root.TryGetProperty("stage", out var stageEl) && stageEl.ValueKind == JsonValueKind.String
                    ? stageEl.GetString() ?? ""
                    : "";
                if (!RequirementAnalysisStages.IsRequirementAnalysisStage(stage))
                    continue;
                var round = root.TryGetProperty("round", out var roundEl) && roundEl.TryGetInt32(out var r) ? r : 0;
                var answersText = root.TryGetProperty("answersText", out var textEl) && textEl.ValueKind == JsonValueKind.String
                    ? textEl.GetString() ?? ""
                    : "";
                result.Add(new ClarificationAnswerAppendix(stage, round, answersText));
            }
            catch (JsonException)
            {
                _logger.LogWarning("澄清作答 payload 解析失败，跳过 eventId={EventId}", evt.EventId);
            }
        }

        var deduped = result
            .GroupBy(x => $"{x.Stage}:{x.Round}", StringComparer.Ordinal)
            .Select(g => g.Last())
            .OrderBy(x => x.Round)
            .ToList();

        return DeduplicateAcrossRounds(deduped);
    }

    /// <summary>
    /// Gap 3：跨轮次确定性文本去重。
    /// 对于 j &gt; i，若第 i 轮答案文本 ≥80% 含于第 j 轮答案文本，
    /// 则标记第 i 轮为 <see cref="ClarificationAnswerAppendix.ResolvedByLaterRound"/> = j。
    /// 无 LLM 调用，纯确定性算法。
    /// </summary>
    private static List<ClarificationAnswerAppendix> DeduplicateAcrossRounds(
        IReadOnlyList<ClarificationAnswerAppendix> answers)
    {
        if (answers.Count <= 1)
            return answers.ToList();

        var result = new List<ClarificationAnswerAppendix>(answers.Count);
        for (var i = 0; i < answers.Count; i++)
        {
            var item = answers[i];
            var resolvedBy = (int?)null;
            var wordsI = NormalizeWords(item.AnswersText);

            for (var j = i + 1; j < answers.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(answers[j].AnswersText))
                    continue;
                var wordsJ = NormalizeWords(answers[j].AnswersText);
                if (wordsI.Count == 0 || wordsJ.Count == 0)
                    continue;

                var matched = wordsI.Count(w => wordsJ.Contains(w, StringComparer.OrdinalIgnoreCase));
                var ratio = (double)matched / wordsI.Count;
                if (ratio >= 0.80)
                {
                    resolvedBy = answers[j].Round;
                    break; // 标记为被最早涵盖的后续轮次
                }
            }

            result.Add(item with { ResolvedByLaterRound = resolvedBy });
        }

        return result;
    }

    private static HashSet<string> NormalizeWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return text
            .Split(new[] { ' ', '\n', '\r', '\t', '，', '。', '、', '；', '：', ',', '.', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length >= 2) // 过滤单字噪音
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildEventSpecPayload(
        string eventId, BusinessEventMeta meta, IReadOnlyDictionary<string, object> previousSteps)
        => EventSpecAssembler.BuildPayloadJson(eventId, meta, previousSteps);

    public static List<BusinessEventMeta> ParseBusinessEvents(string skeletonJson)
    {
        var list = new List<BusinessEventMeta>();
        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            if (!doc.RootElement.TryGetProperty("businessEvents", out var events))
                return list;

            foreach (var evt in events.EnumerateArray())
            {
                var eventId = evt.TryGetProperty("eventId", out var idEl) ? idEl.GetString() : null;
                var eventName = evt.TryGetProperty("eventName", out var nameEl) ? nameEl.GetString() : eventId;
                var hint = evt.TryGetProperty("complexityHint", out var hintEl) ? hintEl.GetString() : "simple";
                if (string.IsNullOrWhiteSpace(eventId)) continue;
                list.Add(new BusinessEventMeta(eventId, eventName ?? eventId, hint ?? "simple"));
            }
        }
        catch (JsonException)
        {
            // 非法 JSON 由调用方以空列表 + Oops.Bah 处理
        }

        return list;
    }

    public sealed record BusinessEventMeta(string EventId, string EventName, string ComplexityHint);

    // ═══════════════════════════════════════════════════════════════
    // Gap 2：澄清答案 vs 编译规范完整性审查
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// LLM 审查澄清答案与编译规范的覆盖/一致性。
    /// 失败即抛硬错误（禁止伪成功）；Executed=false 仅在无澄清答案的前置条件分支返回。
    /// </summary>
    private async Task<ClarificationSpecReviewResult> ReviewClarificationAnswersAgainstSpecAsync(
        PreAnalysisModel model,
        string fullDocumentMarkdown,
        IReadOnlyList<ClarificationAnswerAppendix> answers,
        SkillContext context,
        CancellationToken ct)
    {
        if (answers.Count == 0)
            return new ClarificationSpecReviewResult { Executed = false, Error = "无澄清答案" };

        try
        {
            var specSummary = BuildSpecSummaryForReview(model);
            var answersSummary = BuildAnswersSummaryForReview(answers);

            var request = new ChatCompletionRequest
            {
                ProviderCode = Llm.ResolveProvider(SkillId),
                SystemPrompt = """
                    你是需求审查专家。请对照编译规范摘要，审查多轮澄清 Q&A 答案是否完整覆盖了规范中的关键要素。
                    只输出 JSON：{"missedItems":[{"category":"","description":"","suggestion":""}],"reviewMarkdown":""}。

                    审查维度：
                    - 业务事件：规范中每个事件是否有对应的澄清覆盖？
                    - 实体/字段：关键实体和字段是否在澄清中被讨论？
                    - 业务规则：规范中的规则是否在澄清中被确认或细化？
                    - 状态流转：状态转换是否有澄清覆盖？
                    - 角色权限：权限矩阵是否被讨论？

                    reviewMarkdown 为附录 F 的完整 Markdown 内容，以「## 附录 F：澄清→规范完整性审查」开头。
                    若未发现漏项，reviewMarkdown 仍须输出审查通过的结论。
                    """,
                Messages = new List<ChatMessage>
                {
                    new("user", $"""
                        三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                        【编译规范摘要】
                        {specSummary}

                        【多轮澄清答案汇总】
                        {answersSummary}
                        """),
                },
                Temperature = 0.1,
                MaxTokens = 2048,
                TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
                ResponseFormat = "json",
            };

            var response = await Llm.ChatAsync(request, ct);
            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                // 硬错误：LLM 失败/空响应即抛，禁止返回伪成功对象
                throw Oops.Bah($"分析师澄清答案审查 LLM 失败: {response.Error ?? "(空响应)"} pipeline={context.PipelineId}");
            }

            var json = response.Content.Trim();
            if (json.StartsWith("```"))
            {
                var s = json.IndexOf('{');
                var e = json.LastIndexOf('}');
                if (s >= 0 && e > s) json = json[s..(e + 1)];
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var missedItems = new List<ClarificationMissedItem>();
            if (root.TryGetProperty("missedItems", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    missedItems.Add(new ClarificationMissedItem
                    {
                        Category = item.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "",
                        Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        Suggestion = item.TryGetProperty("suggestion", out var s2) ? s2.GetString() ?? "" : "",
                    });
                }
            }

            var reviewMarkdown = root.TryGetProperty("reviewMarkdown", out var rm) && rm.ValueKind == JsonValueKind.String
                ? rm.GetString() ?? ""
                : "";

            return new ClarificationSpecReviewResult
            {
                Executed = true,
                MissedItems = missedItems,
                ReviewMarkdown = reviewMarkdown,
            };
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            // 硬错误：异常即抛（FriendlyException 透传，其余包装为业务错误）
            if (ex is JNPF.FriendlyException.AppFriendlyException) throw;
            _logger.LogError(ex, "AnalystSkill 澄清→规范审查异常 pipeline={PipelineId}", context.PipelineId);
            throw Oops.Bah($"分析师澄清答案审查 LLM 失败: {ex.Message} pipeline={context.PipelineId}");
        }
    }

    /// <summary>
    /// 构建编译规范的结构化文本摘要，供 LLM 审查使用。
    /// </summary>
    private static string BuildSpecSummaryForReview(PreAnalysisModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## 系统名称：{model.SystemName ?? "（未指定）"}");
        sb.AppendLine();
        sb.AppendLine($"## 需求概要：{TruncateForReview(model.RequirementSummary)}");
        sb.AppendLine();

        // 业务事件
        sb.AppendLine($"## 业务事件（{model.BusinessEvents.Count} 个）");
        foreach (var evt in model.BusinessEvents)
        {
            sb.AppendLine($"- [{evt.EventId}] {evt.EventName}（复杂度：{evt.ComplexityHint}）");
            if (!string.IsNullOrWhiteSpace(evt.Description))
                sb.AppendLine($"  描述：{TruncateForReview(evt.Description)}");
            if (evt.DependsOn.Count > 0)
                sb.AppendLine($"  依赖：{string.Join(", ", evt.DependsOn)}");
        }
        sb.AppendLine();

        // 实体草稿
        sb.AppendLine($"## 实体草稿（{model.EntityDrafts.Count} 个）");
        foreach (var entity in model.EntityDrafts)
        {
            sb.AppendLine($"- {entity.EntityName}" +
                (string.IsNullOrWhiteSpace(entity.DisplayName) ? "" : $"（{entity.DisplayName}）") +
                (string.IsNullOrWhiteSpace(entity.Description) ? "" : $"：{TruncateForReview(entity.Description)}"));
            foreach (var field in entity.Fields)
            {
                var flags = new List<string>();
                if (field.IsPrimaryKey) flags.Add("PK");
                if (field.Required) flags.Add("必填");
                if (!string.IsNullOrWhiteSpace(field.References)) flags.Add($"FK→{field.References}");
                var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
                sb.AppendLine($"    - {field.Name}: {field.Type}{flagStr}");
            }
        }
        sb.AppendLine();

        // 业务规则
        if (model.BusinessRules.Count > 0)
        {
            sb.AppendLine($"## 业务规则（{model.BusinessRules.Count} 条）");
            foreach (var rule in model.BusinessRules)
            {
                var scope = string.IsNullOrWhiteSpace(rule.ScopeEventId) ? "" : $"（作用事件：{rule.ScopeEventId}）";
                sb.AppendLine($"- [{rule.RuleId}]{scope} {TruncateForReview(rule.Description)}");
            }
            sb.AppendLine();
        }

        // 状态流转
        if (model.StateTransitions.Count > 0)
        {
            sb.AppendLine($"## 状态流转（{model.StateTransitions.Count} 条）");
            foreach (var t in model.StateTransitions)
            {
                var trigger = string.IsNullOrWhiteSpace(t.TriggerEventId) ? "" : $" 触发：{t.TriggerEventId}";
                sb.AppendLine($"- {t.Entity}: {t.From} → {t.To}{trigger}");
            }
            sb.AppendLine();
        }

        // 角色矩阵
        if (model.RoleMatrix is { Roles: { Count: > 0 } })
        {
            sb.AppendLine($"## 角色矩阵（{model.RoleMatrix.Roles.Count} 个角色）");
            sb.AppendLine($"角色：{string.Join(", ", model.RoleMatrix.Roles)}");
            sb.AppendLine($"事件-角色映射：{model.RoleMatrix.Matrix.Count} 个事件");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建多轮澄清答案的结构化文本汇总，供 LLM 审查使用。
    /// </summary>
    private static string BuildAnswersSummaryForReview(IReadOnlyList<ClarificationAnswerAppendix> answers)
    {
        var sb = new StringBuilder();
        foreach (var a in answers)
        {
            sb.AppendLine($"### 第 {a.Round} 轮 — {a.Stage}");
            sb.AppendLine(a.AnswersText);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// 将审查结果注入需求文档末尾，追加「附录 F：澄清→规范完整性审查」。
    /// </summary>
    private static string InjectReviewAppendix(string documentMarkdown, ClarificationSpecReviewResult review)
    {
        if (string.IsNullOrWhiteSpace(review.ReviewMarkdown))
            return documentMarkdown;

        // 若文档已含附录 F，不重复注入
        if (documentMarkdown.Contains("## 附录 F", StringComparison.Ordinal))
            return documentMarkdown;

        var sb = new StringBuilder(documentMarkdown);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(review.ReviewMarkdown);
        return sb.ToString();
    }

    private static string TruncateForReview(string? text, int maxLen = 200)
    {
        if (string.IsNullOrWhiteSpace(text)) return "（无）";
        var t = text.Trim();
        return t.Length <= maxLen ? t : t[..maxLen] + "…";
    }

    /// <summary>
    /// Gap 2：LLM 审查结果 — 澄清答案 vs 编译规范的完整性分析。
    /// 在 FinalizeAsync 中调用，失败即抛硬错误（Finalize 会被中断）。
    /// </summary>
    internal sealed record ClarificationSpecReviewResult
    {
        public bool Executed { get; init; }
        public string? Error { get; init; }
        public IReadOnlyList<ClarificationMissedItem> MissedItems { get; init; } = Array.Empty<ClarificationMissedItem>();
        public string ReviewMarkdown { get; init; } = string.Empty;
    }

    /// <summary>
    /// Gap 2：澄清→规范完整性漏项。
    /// </summary>
    internal sealed record ClarificationMissedItem
    {
        public string Category { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Suggestion { get; init; } = string.Empty;
    }
}
