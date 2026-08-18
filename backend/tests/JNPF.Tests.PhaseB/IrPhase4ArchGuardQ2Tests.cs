using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B04b / D10 Q2 — 违规模板 profile → Critical → ArchViolationDetected → 无 TestSuiteGenerated。
/// </summary>
public static class IrPhase4ArchGuardQ2Tests
{
    public static async Task RunAllAsync(string? profileFilter = null)
    {
        TestViolationProfiles_Discoverable();
        var profiles = ArchGuardViolationProfiles.ListProfileIds()
            .Where(id => profileFilter == null
                || string.Equals(id, profileFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (profiles.Count == 0)
            throw new InvalidOperationException($"未找到违规模板 profile: {profileFilter ?? "(all)"}");

        foreach (var profileId in profiles)
            await RunProfileAsync(profileId);

        Console.WriteLine($"[Phase4-Q2] ArchGuard violation profiles passed ({profiles.Count}).");
    }

    private static void TestViolationProfiles_Discoverable()
    {
        var profiles = ArchGuardViolationProfiles.ListProfileIds();
        if (!profiles.Contains("ag001-ddl-controller-ref"))
            throw new InvalidOperationException("Missing profile ag001-ddl-controller-ref");
        if (!profiles.Contains("ag002-no-tenant-filter"))
            throw new InvalidOperationException("Missing profile ag002-no-tenant-filter");
    }

    public static async Task<Q2ProfileResult> RunProfileAsync(string profileId)
    {
        var profile = ArchGuardViolationProfiles.Load(profileId);
        const string tenantId = "_phase4-q2";
        var projectId = $"q2-{profileId}";

        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);

        var devSkill = IrPhase4DeveloperTests.CreateDeveloperSkillPublic();
        var snapshot = IrPhase4DeveloperTests.BuildLeaveSimpleSnapshotPublic(includeSystemDesign: true);
        snapshot = ArchGuardViolationProfiles.ApplySnapshotPatches(profileId, snapshot);

        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9020,
            UserRequirement = $"Q2 profile {profileId}",
            Snapshot = snapshot,
        };

        await foreach (var _ in devSkill.ReasonAsync(context))
        {
        }

        ArchGuardViolationProfiles.ApplyToBackend(profileId, backendRoot);

        var injectedFiles = Directory.Exists(backendRoot)
            ? Directory.GetFiles(backendRoot, "*.cs", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(backendRoot, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList()
            : new List<string>();

        using var loggerFactory = LoggerFactory.Create(static _ => { });
        var gate = new DeveloperCodegenSandboxGate(
            new CodeSandboxService(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                loggerFactory.CreateLogger<CodeSandboxService>()),
            loggerFactory.CreateLogger<DeveloperCodegenSandboxGate>());
        var sandbox = await gate.RunAsync(tenantId, 9020, projectId);
        if (!sandbox.Success)
        {
            throw new InvalidOperationException(
                $"Q2 profile {profileId}: sandbox should pass before arch scan, got {sandbox.Phase}");
        }

        var eventStore = new CapturingIrEventStore();
        var archGuard = new ArchGuardService(eventStore, loggerFactory.CreateLogger<ArchGuardService>());
        var archResult = await archGuard.ScanAndPersistAsync(
            projectId, tenantId, snapshot, DevelopmentSkillIds.Developer);

        if (archResult.CriticalCount == 0)
        {
            var rules = string.Join(", ", archResult.Violations.Select(v => v.RuleId));
            throw new InvalidOperationException(
                $"Q2 profile {profileId}: expected Critical violations, got [{rules}]; files=[{string.Join("; ", injectedFiles)}]");
        }

        foreach (var expectedRule in profile.ExpectedRuleIds)
        {
            if (archResult.Violations.All(v => v.RuleId != expectedRule))
            {
                throw new InvalidOperationException(
                    $"Q2 profile {profileId}: missing expected rule {expectedRule}");
            }
        }

        AbortSkillChainException? abort = null;
        try
        {
            var first = archResult.CriticalViolations[0];
            throw new AbortSkillChainException($"[{first.RuleId}] {first.Message}", "ArchAbort");
        }
        catch (AbortSkillChainException ex)
        {
            abort = ex;
        }

        if (abort?.Phase != "ArchAbort")
            throw new InvalidOperationException("AbortSkillChainException phase must be ArchAbort");

        if (!eventStore.Events.Any(e => e.EventType == IrEventTypes.ArchViolationDetected))
        {
            throw new InvalidOperationException(
                $"Q2 profile {profileId}: ArchViolationDetected not appended");
        }

        if (eventStore.Events.Any(e => e.EventType == IrEventTypes.TestSuiteGenerated))
        {
            throw new InvalidOperationException(
                $"Q2 profile {profileId}: TestSuiteGenerated must not exist after Critical abort");
        }

        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);

        return new Q2ProfileResult
        {
            ProfileId = profileId,
            CriticalCount = archResult.CriticalCount,
            WarningCount = archResult.WarningCount,
            MatchedRuleIds = archResult.CriticalViolations.Select(v => v.RuleId).Distinct().ToList(),
            AbortPhase = abort.Phase,
            ArchViolationAppended = true,
            TestSuiteGenerated = false,
            SandboxPassed = true,
        };
    }

    private sealed class CapturingIrEventStore : IIrEventStoreService
    {
        public List<AppendIrEventRequest> Events { get; } = new();

        public Task<AiIrEventEntity> AppendAsync(
            string projectId,
            string tenantId,
            AppendIrEventRequest request,
            CancellationToken ct = default)
        {
            Events.Add(request);
            return Task.FromResult(new AiIrEventEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectId = projectId,
                TenantId = tenantId,
                EventType = request.EventType,
            });
        }

        public Task<List<IrEventDto>> ListEventsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default) =>
            Task.FromResult(new List<IrEventDto>());

        public Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default) =>
            Task.FromResult(new List<IrFragmentSnapshotDto>());

        public Task<IrStabilityDto?> GetStabilityAsync(
            string projectId, string tenantId, string pipelineId, CancellationToken ct = default) =>
            Task.FromResult<IrStabilityDto?>(null);

        public Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
            string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default) =>
            Task.FromResult<IrFragmentSnapshotDto?>(null);

        public Task EnsureProjectAsync(
            string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<string?> GetLatestEventPayloadAsync(
            string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task<List<string>> ListFullEventPayloadsAsync(
            string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default) =>
            Task.FromResult(new List<string>());
    }
}

public sealed class Q2ProfileResult
{
    public required string ProfileId { get; init; }
    public int CriticalCount { get; init; }
    public int WarningCount { get; init; }
    public required IReadOnlyList<string> MatchedRuleIds { get; init; }
    public required string AbortPhase { get; init; }
    public bool ArchViolationAppended { get; init; }
    public bool TestSuiteGenerated { get; init; }
    public bool SandboxPassed { get; init; }
}
