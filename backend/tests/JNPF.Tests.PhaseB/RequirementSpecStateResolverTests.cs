using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// CR-20260718-01 P4 阶段 1：<see cref="RequirementSpecStateResolver"/> 七态 + FormalSpecGate。
/// </summary>
public class RequirementSpecStateResolverTests
{
    private const string TenantId = "tenant-test";
    private const string ProjectId = "407";
    private const long PipelineId = 407;

    private static readonly string FormalMarkdown =
        "# 需求分析规格说明书\n\n## 概要\n请假系统\n\n请你确认需求分析说明书";

    [Fact]
    public void InferPhase_Absent_WhenNoEvents()
    {
        var phase = RequirementSpecStateResolver.InferPhase(Array.Empty<IrEventDto>(), workingText: null);
        Assert.Equal(RequirementSpecPhase.Absent, phase);
    }

    [Fact]
    public void InferPhase_Refining_WhenRequirementRefined()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementRefined, """{"text":"完善后需求"}"""),
        };
        var phase = RequirementSpecStateResolver.InferPhase(events, workingText: "完善后需求");
        Assert.Equal(RequirementSpecPhase.Refining, phase);
    }

    [Fact]
    public void InferPhase_Rendered_WhenSpecRenderedEvent()
    {
        var events = new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}""") };
        Assert.Equal(RequirementSpecPhase.Rendered, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void InferPhase_Confirmed_AfterUserConfirm()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, "{}"),
            Evt(IrEventTypes.RequirementSpecConfirmed, "{}"),
        };
        Assert.Equal(RequirementSpecPhase.Confirmed, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void InferPhase_PmReviewed_AfterPmReview()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, "{}"),
            Evt(IrEventTypes.RequirementSpecConfirmed, "{}"),
            Evt(IrEventTypes.RequirementSpecPmReviewed, """{"score":90}"""),
        };
        Assert.Equal(RequirementSpecPhase.PmReviewed, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void InferPhase_Finalized_WhenAnalysisCompletedFinalized()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.AnalysisCompleted, """{"finalized":true}"""),
        };
        Assert.Equal(RequirementSpecPhase.Finalized, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void InferPhase_Superseded_WhenSupersededAfterRendered()
    {
        var events = new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, "{}"),
            Evt(IrEventTypes.RequirementSpecSuperseded, """{"reason":"用户反馈"}"""),
        };
        Assert.Equal(RequirementSpecPhase.Superseded, RequirementSpecStateResolver.InferPhase(events, null));
    }

    [Fact]
    public void FormalSpecGate_AcceptsFormalMarkdown()
    {
        var result = FormalSpecGate.Validate(FormalMarkdown);
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void FormalSpecGate_RejectsRawText()
    {
        var result = FormalSpecGate.Validate("这是 PM 草稿，不是正式版");
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task ResolveAsync_Rendered_SetsCanUserConfirm_WhenFormalFileExists()
    {
        var store = new SpecTestEventStore(
            events: new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}""") });
        var reader = new SpecTestMarkdownReader(FormalMarkdown);
        var resolver = CreateResolver(store, reader);

        var snap = await resolver.ResolveAsync(TenantId, ProjectId, PipelineId);

        Assert.Equal(RequirementSpecPhase.Rendered, snap.Phase);
        Assert.True(snap.CanUserConfirm);
        Assert.True(snap.CanUserFeedback);
        Assert.False(snap.CanFinalize);
        Assert.NotNull(snap.ContentHash);
        Assert.Null(snap.FormalMarkdown);
    }

    [Fact]
    public async Task ResolveAsync_Rendered_IncludesMarkdown_WhenRequested()
    {
        var store = new SpecTestEventStore(
            events: new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, "{}") });
        var reader = new SpecTestMarkdownReader(FormalMarkdown);
        var resolver = CreateResolver(store, reader);

        var snap = await resolver.ResolveAsync(TenantId, ProjectId, PipelineId, includeFormalMarkdown: true);

        Assert.Equal(FormalMarkdown, snap.FormalMarkdown);
    }

    [Fact]
    public async Task ResolveAsync_Confirmed_SetsCanFinalize()
    {
        var store = new SpecTestEventStore(events: new List<IrEventDto>
        {
            Evt(IrEventTypes.RequirementSpecRendered, "{}"),
            Evt(IrEventTypes.RequirementSpecConfirmed, "{}"),
        });
        var resolver = CreateResolver(store, new SpecTestMarkdownReader(FormalMarkdown));

        var snap = await resolver.ResolveAsync(TenantId, ProjectId, PipelineId);

        Assert.Equal(RequirementSpecPhase.Confirmed, snap.Phase);
        Assert.True(snap.CanFinalize);
        Assert.False(snap.CanUserConfirm);
    }

    [Fact]
    public async Task ResolveAsync_Rendered_BlockReason_WhenFileMissing()
    {
        var store = new SpecTestEventStore(
            events: new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, "{}") });
        var resolver = CreateResolver(store, new SpecTestMarkdownReader(null));

        var snap = await resolver.ResolveAsync(TenantId, ProjectId, PipelineId);

        Assert.Equal(RequirementSpecPhase.Rendered, snap.Phase);
        Assert.False(snap.CanUserConfirm);
        Assert.NotNull(snap.BlockReason);
    }

    [Fact]
    public async Task ResolveAsync_UsesProgressRow_WhenPresent()
    {
        var store = new SpecTestEventStore(
            events: new List<IrEventDto> { Evt(IrEventTypes.RequirementSpecRendered, """{"specVersion":1}""") });
        var reader = new SpecTestMarkdownReader(FormalMarkdown);
        var progress = new SpecTestProgressStore(new AiPipelineS2ProgressEntity
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            PipelineId = PipelineId.ToString(),
            PipelineStage = (int)S2PipelineStage.SpecAwaitingUserConfirm,
            SpecPhase = (int)RequirementSpecPhase.Rendered,
            SpecVersion = 2,
            ContentHash = "abc",
            ContentLength = FormalMarkdown.Length,
            AwaitingUser = true,
        });
        var resolver = CreateResolver(store, reader, progress);

        var snap = await resolver.ResolveAsync(TenantId, ProjectId, PipelineId);

        Assert.Equal(RequirementSpecPhase.Rendered, snap.Phase);
        Assert.Equal(S2PipelineStage.SpecAwaitingUserConfirm, snap.PipelineStage);
        Assert.Equal(2, snap.Version);
        Assert.True(snap.HasProgressRow);
        Assert.True(snap.AwaitingUser);
        Assert.True(snap.CanUserConfirm);
    }

    private static RequirementSpecStateResolver CreateResolver(
        IIrEventStoreService store,
        IRequirementSpecMarkdownReader reader,
        IPipelineS2ProgressStore? progressStore = null)
        => new(store, reader, NullLogger<RequirementSpecStateResolver>.Instance, progressStore);

    private static IrEventDto Evt(string type, string payloadPreview) => new()
    {
        EventId = Guid.NewGuid().ToString("N"),
        EventType = type,
        PayloadPreview = payloadPreview,
    };

    private sealed class SpecTestEventStore : IIrEventStoreService
    {
        private readonly List<IrEventDto> _events;
        private readonly List<IrFragmentSnapshotDto> _snapshots;

        public SpecTestEventStore(
            List<IrEventDto>? events = null,
            List<IrFragmentSnapshotDto>? snapshots = null)
        {
            _events = events ?? new();
            _snapshots = snapshots ?? new();
        }

        public Task<List<IrEventDto>> ListEventsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_events);

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_snapshots);

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
        private readonly string? _markdown;

        public SpecTestMarkdownReader(string? markdown) => _markdown = markdown;

        public Task<(bool Exists, string? Markdown, string? ContentHash, int ContentLength)> TryReadFormalAsync(
            string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_markdown))
                return Task.FromResult((false, (string?)null, (string?)null, 0));

            var hash = RequirementSpecDeliverableMarkdownReader.ComputeSha256Hex(_markdown);
            return Task.FromResult((true, _markdown, hash, _markdown.Length));
        }
    }

    private sealed class SpecTestProgressStore : IPipelineS2ProgressStore
    {
        private readonly AiPipelineS2ProgressEntity? _row;

        public SpecTestProgressStore(AiPipelineS2ProgressEntity? row) => _row = row;

        public Task<AiPipelineS2ProgressEntity?> TryGetAsync(
            string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
            => Task.FromResult(_row);

        public Task UpsertAsync(S2ProgressUpdate update, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
