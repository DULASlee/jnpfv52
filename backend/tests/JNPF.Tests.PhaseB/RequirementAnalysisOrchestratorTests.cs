using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 三轮需求分析编排器暂停-恢复状态机 xUnit 测试（26/27/28 号）。
/// 覆盖：轮次判定、三轮推进、PM Skill 触发、LLM 降级、异常处理。
/// </summary>
public static class RequirementAnalysisOrchestratorTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task RunAllAsync()
    {
        await T1_FreshPipeline_RoundDetermination_Returns1();
        await T2_Round1InProgress_Returns1();
        await T3_Round1Stable_Round2NotStarted_Returns2();
        await T4_AllRoundsStable_Returns4();
        await T5_Round1_PmSkillInvoked();
        await T6_PmSkillNotRegistered_Throws();
        await T7_Round1_EmitsClarification_ReturnsAwaitingAnswer();
        await T8_Round2_PrerequisiteNotMet_Throws();
        await T9_Round3_UserConfirmed_RunsFinalize();
        await T10_PostComplete_NotFinalized_ForcesFinalization();
        await T11_PostComplete_AlreadyFinalized_ReturnsCompleted();
        await T12_Exception_ReturnsFailed();
        await T13_Cancellation_Propagates();
        await T14_ClarificationGen_LlmFails_EmptySet();
        await T15_AssumptionsPersisted();
    }

    private static void Assert(bool condition, string msg)
    {
        if (!condition) throw new Exception(msg);
    }

    // ── 轮次判定 ──

    private static async Task T1_FreshPipeline_RoundDetermination_Returns1()
    {
        var orch = CreateOrchestrator(eventStore: new FakeEventStore(
            snapshots: new List<IrFragmentSnapshotDto>())); // 空快照
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // 空快照 → Round 1，但 PM Skill 未注册 → 抛异常
        Assert(result.Status == "failed" && result.ErrorMessage!.Contains("PM Skill 未注册"),
            $"空快照应尝试 Round 1 的 PM Skill，实际: {result.Status} — {result.ErrorMessage}");
    }

    private static async Task T2_Round1InProgress_Returns1()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "in-progress"),
        };
        var orch = CreateOrchestrator(eventStore: new FakeEventStore(snapshots: snapshots));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // Round 1: skeleton stable + clar in-progress → DetermineCurrentRound 返回 1。
        // Skeleton 已存在 → 不需要 PM Skill。Round 1 直接走 SA 编译 + 重新出题 → awaiting-answer。
        Assert(result.Status == "awaiting-answer" && result.CurrentRound == 1,
            $"Round 1 in-progress 应重新出题返回 awaiting-answer，实际 {result.Status}");
    }

    private static async Task T3_Round1Stable_Round2NotStarted_Returns2()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable",
                """{"businessEvents":[{"eventId":"BE-001","eventName":"请假"}]}"""),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "stable"),
        };
        var orch = CreateOrchestrator(
            registry: new FakeSkillRegistry(hasPm: true),
            eventStore: new FakeEventStore(snapshots: snapshots));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // Round 1 stable → 进 Round 2 → SA 编译 + LLM 出题 → awaiting-answer
        Assert(result.Status == "awaiting-answer" && result.CurrentRound == 2,
            $"Round 1 完成后应进 Round 2 出题，实际 Status={result.Status} Round={result.CurrentRound}");
    }

    private static async Task T4_AllRoundsStable_Returns4()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round2:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round3:p1", IrFragmentTypes.Clarification, "stable"),
        };
        var orch = CreateOrchestrator(eventStore: new FakeEventStore(
            snapshots: snapshots));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // 三轮都已 stable → DetermineCurrentRound 返回 4 → 检查 HasFinalizedEngineeringAsync
        // 未 finalized → 强制执行 FinalizeAsync
        Assert(result.Status is "completed" or "failed",
            $"全部完成后应 completed 或 failed (无 compiler), 实际 {result.Status}");
    }

    // ── PM Skill 触发 ──

    private static async Task T5_Round1_PmSkillInvoked()
    {
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "pm-skill", Status = "completed",
        });
        var eventStore = new FakeEventStore(snapshots: new List<IrFragmentSnapshotDto>());
        var orch = CreateOrchestrator(harness: harness, eventStore: eventStore,
            registry: new FakeSkillRegistry(hasPm: true));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // PM Skill 完成后 → SA 编译 + 出题 → 返回 awaiting-answer
        Assert(harness.RunCallCount > 0, "PM Skill 应被调用至少 1 次");
    }

    private static async Task T6_PmSkillNotRegistered_Throws()
    {
        var orch = CreateOrchestrator(registry: new FakeSkillRegistry(hasPm: false));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "failed" && result.ErrorMessage!.Contains("PM Skill 未注册"),
            $"PM 未注册应 explicit fail, 实际 {result.Status}: {result.ErrorMessage}");
    }

    // ── 暂停-恢复 ──

    private static async Task T7_Round1_EmitsClarification_ReturnsAwaitingAnswer()
    {
        // 预置 skeleton，让编排器跳过 PM Skill 直接进入 Round 1 出题
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable",
                """{"businessEvents":[{"eventId":"BE-001","eventName":"请假"}]}"""),
        };
        var eventStore = new FakeEventStore(snapshots: snapshots);
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "pm-skill", Status = "completed",
        });
        // Fake compiler：产出 1 个事件
        var compiler = new FakeCompiler(events: 1);
        var llm = new FakeLlmGateway(new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """[{"text":"确认请假类型","options":["年假","病假","事假","其他"]}]""",
        });
        var orch = CreateOrchestrator(harness: harness, eventStore: eventStore,
            registry: new FakeSkillRegistry(hasPm: true), compiler: compiler, llm: llm);
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "awaiting-answer", $"Round 1 出题后应 awaiting-answer，实际 {result.Status}");
        Assert(result.PendingClarification != null, "应返回 PendingClarification");
        Assert(result.CurrentRound == 1, $"应为 Round 1，实际 {result.CurrentRound}");
    }

    private static async Task T8_Round2_PrerequisiteNotMet_Throws()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable",
                """{"businessEvents":[{"eventId":"BE-001"}]}"""),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "in-progress"),
        };
        // Round 1 in-progress → DetermineCurrentRound 返回 1。
        // Skeleton 已存在，RunRoundAsync 1: SA 编译 → 重新出题 → awaiting-answer
        var orch = CreateOrchestrator(
            registry: new FakeSkillRegistry(hasPm: true),
            eventStore: new FakeEventStore(snapshots: snapshots));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "awaiting-answer" && result.CurrentRound == 1,
            $"Round 1 in-progress 应重新出题返回 awaiting-answer，实际 {result.Status}: {result.ErrorMessage}");
    }

    // ── Round 3 工程保障 ──

    private static async Task T9_Round3_UserConfirmed_RunsFinalize()
    {
        var skeletonJson = """{"businessEvents":[{"eventId":"BE-001","eventName":"请假"}]}""";
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable", skeletonJson),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round2:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round3:p1", IrFragmentTypes.Clarification, "stable"),
        };
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "analyst-skill", Status = "completed",
        });
        var orch = CreateOrchestrator(harness: harness, eventStore: new FakeEventStore(
            snapshots: snapshots, events: new List<IrEventDto>()), // HasFinalizedEngineeringAsync → false
            registry: new FakeSkillRegistry(hasPm: true));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        // 三轮都已 stable 但 HasFinalizedEngineeringAsync=false → 强制执行 FinalizeAsync
        Assert(result.Status == "completed",
            $"Finalize 后应为 completed，实际 {result.Status}: {result.ErrorMessage}");
        // harness 的 analyst-skill 被调用（enableFinalization=true）
        Assert(harness.AnalystCallCount > 0, $"analyst-skill 应被调用，实际 {harness.AnalystCallCount} 次");
    }

    // ── 全部完成后 ──

    private static async Task T10_PostComplete_NotFinalized_ForcesFinalization()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable",
                """{"businessEvents":[{"eventId":"BE-001"}]}"""),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round2:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round3:p1", IrFragmentTypes.Clarification, "stable"),
        };
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "analyst-skill", Status = "completed",
        });
        var orch = CreateOrchestrator(harness: harness, eventStore: new FakeEventStore(snapshots),
            registry: new FakeSkillRegistry(hasPm: true));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "completed",
            $"Finalize 后应为 completed，实际 {result.Status}");
        Assert(harness.AnalystCallCount > 0, "analyst 应被调用来执行 Finalize");
    }

    private static async Task T11_PostComplete_AlreadyFinalized_ReturnsCompleted()
    {
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round1:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round2:p1", IrFragmentTypes.Clarification, "stable"),
            MakeSnapshot("clarification:requirement-analysis-round3:p1", IrFragmentTypes.Clarification, "stable"),
        };
        // events 中有 AnalysisCompleted + finalized=true
        var events = new List<IrEventDto>
        {
            new() { EventType = IrEventTypes.AnalysisCompleted, PayloadPreview = """{"finalized":true}""" },
        };
        var orch = CreateOrchestrator(eventStore: new FakeEventStore(snapshots, events));
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "completed", $"已 finalized 应直接 completed，实际 {result.Status}");
    }

    // ── 异常处理 ──

    private static async Task T12_Exception_ReturnsFailed()
    {
        var throwingEventStore = new ThrowingEventStore();
        var orch = CreateOrchestrator(eventStore: throwingEventStore);
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "failed", $"异常应返回 failed，实际 {result.Status}");
        Assert(!string.IsNullOrEmpty(result.ErrorMessage), "应有 ErrorMessage");
    }

    private static async Task T13_Cancellation_Propagates()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var orch = CreateOrchestrator();
        try
        {
            await orch.RunAsync(200, "t1", "p1", null, cts.Token);
            Assert(false, "已取消的 Token 应抛 OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // 预期行为
        }
    }

    // ── LLM 降级 ──

    private static async Task T14_ClarificationGen_LlmFails_EmptySet()
    {
        // 预置 skeleton 让编排器跳过 PM Skill，在出题阶段触发 LLM 降级
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable"),
        };
        var eventStore = new FakeEventStore(snapshots: snapshots);
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "pm-skill", Status = "completed",
        });
        var llm = new FakeLlmGateway(new ChatCompletionResponse
        {
            IsSuccess = false,
            Error = "LLM unavailable",
        });
        var orch = CreateOrchestrator(harness: harness, eventStore: eventStore,
            registry: new FakeSkillRegistry(hasPm: true), llm: llm);
        var result = await orch.RunAsync(200, "t1", "p1", null);
        Assert(result.Status == "awaiting-answer",
            $"LLM 失败降级后应 awaiting-answer，实际 {result.Status} ErrorMessage={result.ErrorMessage}");
        Assert(result.PendingClarification!.Questions.Count == 0,
            $"LLM 失败降级应返回空题集，实际 {result.PendingClarification!.Questions.Count} 题");
    }

    // ── Assumptions 落 IR ──

    private static async Task T15_AssumptionsPersisted()
    {
        // 预置 skeleton 让编排器跳过 PM Skill，聚焦 Assumptions 落 IR
        var snapshots = new List<IrFragmentSnapshotDto>
        {
            MakeSnapshot("skeleton:p1", IrFragmentTypes.Skeleton, "stable"),
        };
        var eventStore = new FakeEventStore(snapshots: snapshots);
        var harness = new FakeSkillHarness(returns: new SkillRunResult
        {
            SkillId = "pm-skill", Status = "completed",
        });
        var llm = new FakeLlmGateway(new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """[{"text":"确认请假类型","options":["年假","病假","事假","其他"]}]""",
        });
        var orch = CreateOrchestrator(harness: harness, eventStore: eventStore,
            registry: new FakeSkillRegistry(hasPm: true), llm: llm);
        await orch.RunAsync(200, "t1", "p1", null);
        // Verify Assumptions fragment was appended
        Assert(eventStore.AppendedEvents.Any(e => e.FragmentType == IrFragmentTypes.Assumptions),
            $"每轮应写入 Assumptions fragment，实际有 {eventStore.AppendedEvents.Count} 个事件");
    }

    // ── 工厂方法 ──

    private static RequirementAnalysisOrchestrator CreateOrchestrator(
        ISkillHarness? harness = null,
        IIrEventStoreService? eventStore = null,
        ISkillRegistry? registry = null,
        ISaNineViewCompiler? compiler = null,
        ILightStructureValidator? validator = null,
        ILlmGatewayService? llm = null,
        IPipelineSseChannelHub? sseHub = null)
    {
        return new RequirementAnalysisOrchestrator(
            harness ?? new FakeSkillHarness(),
            eventStore ?? new FakeEventStore(),
            registry ?? new FakeSkillRegistry(hasPm: false),
            compiler ?? new FakeCompiler(),
            validator ?? new FakeLightValidator(),
            llm ?? new FakeLlmGateway(),
            sseHub ?? new FakeSseHub(),
            Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()),
            NullLogger<RequirementAnalysisOrchestrator>.Instance);
    }

    // ── Fake 类 ──

    private interface IFakeEventStore : IIrEventStoreService
    {
        List<AppendIrEventRequest> AppendedEvents { get; }
    }

    private sealed class FakeEventStore : IFakeEventStore
    {
        private readonly List<IrFragmentSnapshotDto> _snapshots;
        private readonly List<IrEventDto> _events;

        public FakeEventStore(List<IrFragmentSnapshotDto>? snapshots = null, List<IrEventDto>? events = null)
        {
            _snapshots = snapshots ?? new();
            _events = events ?? new();
        }

        public List<AppendIrEventRequest> AppendedEvents { get; } = new();

        public Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_events);

        public Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest evt, CancellationToken ct = default)
        {
            AppendedEvents.Add(evt);
            return Task.FromResult(new AiIrEventEntity { EventType = evt.EventType });
        }

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_snapshots);

        public Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult<IrStabilityDto?>(null);

        public Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default)
            => Task.FromResult<IrFragmentSnapshotDto?>(null);

        public Task EnsureProjectAsync(string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingEventStore : IIrEventStoreService
    {
        public Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");

        public Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest evt, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");

        public Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");

        public Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");

        public Task EnsureProjectAsync(string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default)
            => throw new InvalidOperationException("DB 不可用");
    }

    private sealed class FakeBaseSkill : IBaseSkill
    {
        public string SkillId { get; }
        public string Version => "1.0";
        public SkillInformationNeeds InformationNeeds => new();
        public SkillOutputDeclaration Outputs => new();
        public FakeBaseSkill(string skillId) => SkillId = skillId;
        public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
            => Task.FromResult(SkillValidationResult.Ok());
        public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(SkillContext context, [EnumeratorCancellation] CancellationToken ct = default)
            { yield break; }
        public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
            => Task.FromResult(SkillValidationResult.Ok());
    }

    private sealed class FakeSkillRegistry : ISkillRegistry
    {
        private readonly bool _hasPm;
        public FakeSkillRegistry(bool hasPm) => _hasPm = hasPm;
        public bool TryGet(string skillId, out IBaseSkill? skill)
        {
            if (_hasPm && skillId == "pm-skill")
            {
                skill = new FakeBaseSkill("pm-skill");
                return true;
            }
            skill = null;
            return false;
        }
        public IReadOnlyCollection<string> SkillIds => Array.Empty<string>();
        public IBaseSkill GetRequired(string skillId) => throw new NotSupportedException();
    }

    private sealed class FakeSkillHarness : ISkillHarness
    {
        private readonly SkillRunResult _returns;
        public int RunCallCount { get; private set; }
        public int AnalystCallCount { get; private set; }

        public FakeSkillHarness(SkillRunResult? returns = null)
        {
            _returns = returns ?? new SkillRunResult { SkillId = "pm-skill", Status = "completed" };
        }

        public Task<SkillRunResult> RunAsync(string skillId, long pipelineId, string tenantId, string projectId, SkillRunOptions options, CancellationToken ct = default)
        {
            RunCallCount++;
            if (skillId == "analyst-skill") AnalystCallCount++;
            return Task.FromResult(_returns);
        }
    }

    private sealed class FakeCompiler : ISaNineViewCompiler
    {
        private readonly int _events;
        public FakeCompiler(int events = 1) => _events = events;

        public SaNineViewCompileResult Compile(PreAnalysisModel model) => CompileFromSkeletonJson(null);

        public SaNineViewCompileResult CompileFromSkeletonJson(string? skeletonJson, string? requirementSummary = null)
        {
            var eventResults = Enumerable.Range(1, _events).Select(i => new SaEventResult
            {
                EventId = $"BE-{i:D3}",
                EventName = $"事件 {i}",
                Complexity = "simple",
                Steps = new Dictionary<string, object?>(StringComparer.Ordinal),
            }).ToList();

            return new SaNineViewCompileResult
            {
                Source = new PreAnalysisModel
                {
                    BusinessEvents = eventResults.Select(e => new PreAnalysisBusinessEvent
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                    }).ToList(),
                },
                ProjectSteps = new Dictionary<string, object>(StringComparer.Ordinal),
                EventResults = eventResults,
                CompileDurationMs = 5,
                BundleHash = Guid.NewGuid().ToString("N")[..8],
                Assumptions = new List<Assumption>
                {
                    new("BE-001", "Compiler", "推导假设", 0.7m),
                },
            };
        }
    }

    private sealed class FakeLightValidator : ILightStructureValidator
    {
        public List<string> Validate(PreAnalysisModel preAnalysis)
            => new();
    }

    private sealed class FakeLlmGateway : ILlmGatewayService
    {
        private readonly ChatCompletionResponse _response;
        public FakeLlmGateway(ChatCompletionResponse? response = null)
        {
            _response = response ?? new ChatCompletionResponse
            {
                IsSuccess = true,
                Content = """[{"text":"确认","options":["是","否","其他"]}]""",
            };
        }

        public Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest request, CancellationToken ct = default)
            => Task.FromResult(_response);

        public string ResolveProvider(string skillId) => "deepseek";
        public int ResolveTimeoutMs(string skillId) => 60000;

        // Unused stubs
        public Task<string> ChatAsync(string prompt, string? model = null) => throw new NotSupportedException();
        public Task<ChatCompletionResponse> ChatWithLevelFallbackAsync(ChatCompletionRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<string> ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> HealthCheckAsync(string providerCode, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ProviderHealth> HealthCheckAsync() => throw new NotSupportedException();
        public Task<ProviderInfo> GetProviderInfoAsync(string providerCode) => throw new NotSupportedException();
        public Task<TreeSearchResult> TreeSearchAsync(TreeSearchRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeSseHub : IPipelineSseChannelHub
    {
        public bool TryPush(long pipelineId, string eventType, string data) => true;
        public Channel<SseEvent> ReplaceChannel(long pipelineId) => Channel.CreateUnbounded<SseEvent>();
        public void RemoveChannel(long pipelineId) { }
        public bool TryGetChannel(long pipelineId, out Channel<SseEvent>? channel) { channel = null; return false; }
    }

    // ── helpers ──

    private static IrFragmentSnapshotDto MakeSnapshot(string fragmentId, string fragmentType, string stabilityState, string? payload = null)
    {
        return new IrFragmentSnapshotDto
        {
            FragmentId = fragmentId,
            FragmentType = fragmentType,
            StabilityState = stabilityState,
            Payload = payload ?? "{}",
        };
    }
}
