using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// R4 进化层经验回流测试（施工包 21 R4）.
/// </summary>
public static class ExperienceR4Tests
{
    public static void RunAll()
    {
        T1_ExperienceRecorder_WritesThreeEventTypes();
        T2_SkillExperienceClassifier_MapsKnownExceptions();
        T3_EventSpecRevision_RecordsHumanCorrection();
    }

    private static void T1_ExperienceRecorder_WritesThreeEventTypes()
    {
        var stream = new CapturingEventStream();
        var recorder = new ExperienceRecorder(stream);

        recorder.RecordReviewAsync("p1", "t1", "pm-skill", "run1", "approved", """{"ok":true}""").GetAwaiter().GetResult();
        recorder.RecordFailureAsync("p1", "t1", "analyst-skill", "run2", "business", "SA failed").GetAwaiter().GetResult();
        recorder.RecordHumanCorrectionAsync("p1", "t1", "analyst-skill", "eventspec:BE-001", "{}", """{"patched":true}""", "fieldTypeOrConstraint").GetAwaiter().GetResult();

        if (stream.Requests.Count != 3)
            throw new Exception($"T1 应写入 3 条经验事件，实际 {stream.Requests.Count}");

        var types = stream.Requests.Select(r => r.EventType).ToHashSet(StringComparer.Ordinal);
        if (!types.Contains(IrEventTypes.SkillReviewRecorded)
            || !types.Contains(IrEventTypes.SkillFailureRecorded)
            || !types.Contains(IrEventTypes.HumanCorrectionRecorded))
        {
            throw new Exception("T1 经验事件类型不完整");
        }
    }

    private static void T2_SkillExperienceClassifier_MapsKnownExceptions()
    {
        if (SkillExperienceClassifier.Classify(new OperationCanceledException()) != "cancelled")
            throw new Exception("T2 cancelled 分类失败");
        if (SkillExperienceClassifier.Classify(new AbortSkillChainException("x")) != "aborted")
            throw new Exception("T2 aborted 分类失败");
    }

    private static void T3_EventSpecRevision_RecordsHumanCorrection()
    {
        var stream = new CapturingEventStream();
        var store = new FakeIrEventStore(stream);
        var recorder = new ExperienceRecorder(stream);
        var svc = new EventSpecRevisionService(store, recorder);

        svc.ReviseAsync("p1", "t1", "pipe-1", "eventspec:BE-001", new ReviseEventSpecInput
        {
            RevisionType = EventSpecRevisionPlanner.FieldTypeOrConstraint,
            PayloadPatch = """{"note":"human-patched"}""",
        }).GetAwaiter().GetResult();

        if (!stream.Requests.Any(r => r.EventType == IrEventTypes.HumanCorrectionRecorded))
            throw new Exception("T3 EventSpecRevision 应写入 HumanCorrectionRecorded");
    }

    private sealed class CapturingEventStream : IEventStream
    {
        public List<AppendIrEventRequest> Requests { get; } = new();

        public Task<AiIrEventEntity> AppendAsync(AppendIrEventRequest request, CancellationToken ct = default)
            => AppendAsync("", "", request, ct);

        public Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(new AiIrEventEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                EventType = request.EventType,
                ProjectId = projectId,
                TenantId = tenantId,
            });
        }
    }

    private sealed class FakeIrEventStore : IIrEventStoreService
    {
        private readonly CapturingEventStream _stream;
        private readonly List<IrFragmentSnapshotDto> _snapshots;

        public FakeIrEventStore(CapturingEventStream stream)
        {
            _stream = stream;
            _snapshots = new List<IrFragmentSnapshotDto>
            {
                new()
                {
                    FragmentId = "eventspec:BE-001",
                    FragmentType = IrFragmentTypes.EventSpec,
                    StabilityState = IrStabilityStates.Stable,
                    CurrentVersion = 1,
                    Payload = """{"eventId":"BE-001","confirmedFields":[]}""",
                    SaStepsCompleted = IrSaSteps.All,
                },
            };
        }

        public Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest request, CancellationToken ct = default)
            => _stream.AppendAsync(projectId, tenantId, request, ct);

        public Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(new List<IrEventDto>());

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult(_snapshots);

        public Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
            => Task.FromResult<IrStabilityDto?>(null);

        public Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default)
            => Task.FromResult(_snapshots.FirstOrDefault(s => s.FragmentId == fragmentId));

        public Task EnsureProjectAsync(string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
