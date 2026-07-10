using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.Ir;
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
        var businessEvents = ParseBusinessEvents(skeleton.Payload);
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
            "AnalystSkill: 共 {Total} 个事件，{Pending} 个待分析，{Done} 个已完成（断点续跑）",
            businessEvents.Count, pendingEvents.Count, alreadyConfirmed.Count);

        SaNineViewCompileResult? compileResult = null;

        if (pendingEvents.Count > 0)
        {
            SaProjectResult saResult;

            if (_pipelineOptions.IsCompileMode)
            {
                compileResult = _compiler.CompileFromSkeletonJson(
                    skeleton.Payload,
                    context.UserRequirement ?? skeleton.Payload);

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
            compileResult = _compiler.CompileFromSkeletonJson(
                skeleton.Payload,
                context.UserRequirement ?? skeleton.Payload);
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
                "AnalystSkill: DDD 推导完成 confidence={Conf:F2} subDomains={Sub} roots={Roots}",
                dddResult.OverallConfidence, dddResult.DomainModel.SubDomains.Count,
                dddResult.AggregateDesign.RootEntities.Count);

            // ⑦ 一致性检查器（28 号 §5：4 条规则，写 sa_consistency）
            var consistencyFindings = await _consistencyChecker.CheckAsync(
                triple, compileResult, projection, roundNumber: 3, ct);

            // ⑧ 质量评分器（28 号 §6：5 维度加权，写 sa_quality_score）
            var qualityScore = await _qualityScoreCalculator.CalculateAsync(
                triple, compileResult, projection, dddResult, consistencyFindings, roundNumber: 3, ct);

            // ⑨ 需求分析书渲染（28 号 §7：渲染并落盘 deliverable）
            var documentMarkdown = _documentRenderer.Render(
                triple, compileResult, dddResult, projection,
                consistencyFindings, qualityScore, roundNumber: 3, ct);
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
        var rows = assumptions.Select(a => new
        {
            F_Id = Guid.NewGuid().ToString("N"),
            F_TenantId = context.TenantId,
            F_ProjectId = context.ProjectId,
            F_PIPELINE_ID = context.PipelineId.ToString(),
            F_EventId = string.IsNullOrWhiteSpace(a.EventId) ? null : (string?)a.EventId,
            F_SourceStep = a.SourceStep,
            F_AssumptionText = a.Text,
            F_Confidence = a.Confidence,
            F_IsUserConfirmed = false,
            F_UserVerdict = (string?)null,
            F_RoundCreated = 3,
            F_CreatedAt = now,
        }).ToList();

        await _db.Insertable(rows).AS("sa_assumptions").ExecuteCommandAsync(ct);
        _logger.LogInformation("AnalystSkill: 写入 {Count} 条假设项到 sa_assumptions", rows.Count);
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
}
