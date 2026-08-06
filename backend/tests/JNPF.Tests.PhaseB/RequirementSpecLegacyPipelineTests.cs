using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 343/407 抽样 pipeline 事件序 → Resolver Phase 契约（无 DB，纯 IR 推断 + progress 行模拟）。
/// </summary>
public class RequirementSpecLegacyPipelineTests
{
    [Fact]
    public void Pipeline407_BeforeConfirm_InferPhase_Rendered()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementEnhanced, """{"text":"完善需求"}"""),
            Evt(IrEventTypes.RequirementRefined, """{"text":"反向完善"}"""),
            Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1,"contentHash":"abc","contentLength":1200}"""),
        };

        var phase = RequirementSpecStateResolver.InferPhase(events, workingText: "反向完善");
        Assert.Equal(RequirementSpecPhase.Rendered, phase);
    }

    [Fact]
    public void Pipeline407_AfterConfirm_InferPhase_Confirmed()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}"""),
            Evt(IrEventTypes.RequirementSpecConfirmed, """{"specVersion":1}"""),
        };

        Assert.Equal(RequirementSpecPhase.Confirmed, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void Pipeline343_Finalized_InferPhase_Finalized()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}"""),
            Evt(IrEventTypes.RequirementSpecConfirmed, "{}"),
            Evt(IrEventTypes.RequirementSpecPmReviewed, """{"score":90,"verdict":"pass"}"""),
            Evt(IrEventTypes.AnalysisCompleted, """{"finalized":true}"""),
            Evt(IrEventTypes.StageConfirmed, """{"stage":"S2"}"""),
        };

        Assert.Equal(RequirementSpecPhase.Finalized, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public async Task Pipeline407_ProgressRow_OverridesLegacyInference()
    {
        var store = new SpecTestEventStore(
            events: new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}""") });
        var reader = new SpecTestMarkdownReader(FormalMarkdown);
        var progress = new SpecTestProgressStore(new AiPipelineS2ProgressEntity
        {
            TenantId = "tenant-test",
            ProjectId = "407",
            PipelineId = "407",
            PipelineStage = (int)S2PipelineStage.SpecAwaitingUserConfirm,
            SpecPhase = (int)RequirementSpecPhase.Rendered,
            SpecVersion = 1,
            AwaitingUser = true,
        });
        var resolver = new RequirementSpecStateResolver(
            store, reader, Microsoft.Extensions.Logging.Abstractions.NullLogger<RequirementSpecStateResolver>.Instance, progress);

        var snap = await resolver.ResolveAsync("tenant-test", "407", 407);

        Assert.True(snap.HasProgressRow);
        Assert.Equal(S2PipelineStage.SpecAwaitingUserConfirm, snap.PipelineStage);
        Assert.True(snap.CanUserConfirm);
        Assert.True(snap.AwaitingUser);
    }

    private const string FormalMarkdown =
        "# 需求分析规格说明书\n\n## 概要\n请假系统\n\n请你确认需求分析说明书";

    private static IrEventDto Evt(string type, string payloadPreview) => new()
    {
        EventId = Guid.NewGuid().ToString("N"),
        EventType = type,
        PayloadPreview = payloadPreview,
    };

    private sealed class SpecTestEventStore : IIrEventStoreService
    {
        private readonly List<IrEventDto> _events;

        public SpecTestEventStore(List<IrEventDto> events) => _events = events;

        public Task<List<IrEventDto>> ListEventsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_events);

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(new List<IrFragmentSnapshotDto>());

        public Task<string?> GetLatestEventPayloadAsync(
            string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default)
            => Task.FromResult(_events.LastOrDefault(e => e.EventType == eventType)?.PayloadPreview);

        public Task<List<string>> ListFullEventPayloadsAsync(
            string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default)
            => Task.FromResult(_events.Where(e => e.EventType == eventType).Select(e => e.PayloadPreview).ToList());

        public Task<AiIrEventEntity> AppendAsync(
            string projectId, string tenantId, AppendIrEventRequest request, CancellationToken ct = default)
            => Task.FromResult(new AiIrEventEntity());

        public Task<IrStabilityDto?> GetStabilityAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult<IrStabilityDto?>(null);

        public Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
            string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default)
            => Task.FromResult<IrFragmentSnapshotDto?>(null);

        public Task EnsureProjectAsync(
            string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class SpecTestMarkdownReader : IRequirementSpecMarkdownReader
    {
        private readonly string _markdown;

        public SpecTestMarkdownReader(string markdown) => _markdown = markdown;

        public Task<(bool Exists, string? Markdown, string? ContentHash, int ContentLength)> TryReadFormalAsync(
            string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
        {
            var hash = RequirementSpecDeliverableMarkdownReader.ComputeSha256Hex(_markdown);
            return Task.FromResult<(bool, string?, string?, int)>((true, _markdown, hash, _markdown.Length));
        }
    }

    private sealed class SpecTestProgressStore : IPipelineS2ProgressStore
    {
        private readonly AiPipelineS2ProgressEntity _row;

        public SpecTestProgressStore(AiPipelineS2ProgressEntity row) => _row = row;

        public Task<AiPipelineS2ProgressEntity?> TryGetAsync(
            string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
            => Task.FromResult<AiPipelineS2ProgressEntity?>(_row);

        public Task UpsertAsync(S2ProgressUpdate update, CancellationToken ct = default) => Task.CompletedTask;
    }
}
