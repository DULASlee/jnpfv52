using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills.Bugfix;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段五 P5-B02 — bugfix-skill：BugReported → diff → AffectedFragmentsMarked → BugFixed。
/// </summary>
public sealed class BugfixSkillService : IBaseSkill, ITransient
{
    private readonly IIrDiffEngine _diffEngine;
    private readonly ILogger<BugfixSkillService> _logger;

    public BugfixSkillService(IIrDiffEngine diffEngine, ILogger<BugfixSkillService> logger)
    {
        _diffEngine = diffEngine;
        _logger = logger;
    }

    public string SkillId => BugfixSkillIds.Bugfix;
    public string Version { get; } = "1.0.0-p5b02";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes =
        [
            IrFragmentTypes.EventSpec,
            IrFragmentTypes.GeneratedCode,
            IrFragmentTypes.TestSuite,
        ],
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes =
        [
            IrEventTypes.BugReported,
            IrEventTypes.BugRootCauseLocated,
            IrEventTypes.AffectedFragmentsMarked,
            IrEventTypes.BugFixed,
        ],
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        _ = ct;
        var codegen = snapshot.Find(IrFragmentTypes.GeneratedCode, IrStabilityStates.Stable);
        if (codegen == null)
        {
            return Task.FromResult(SkillValidationResult.Fail(
                "IR3_GeneratedCode 须 stable 后才可运行 bugfix-skill"));
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var bugfix = context.Bugfix
            ?? throw new InvalidOperationException("bugfix-skill 缺少 BugfixRunContext（fromSequence/toSequence）");

        if (bugfix.FromSequence >= bugfix.ToSequence)
            throw new InvalidOperationException("fromSequence 须小于 toSequence");

        _logger.LogInformation(
            "Bugfix diff compute pipeline={PipelineId} from={From} to={To}",
            context.PipelineId,
            bugfix.FromSequence,
            bugfix.ToSequence);

        var diff = await _diffEngine.CompareAsync(
            context.ProjectId,
            context.TenantId,
            bugfix.FromSequence,
            bugfix.ToSequence,
            new IrDiffOptions
            {
                ForceUnlock = bugfix.ForceUnlock,
                PropagateDownstream = true,
            },
            ct);

        if (diff.IsEmpty)
            throw new InvalidOperationException("Bugfix 空 diff：无受影响片段，拒绝 append BugFixed");

        var fragmentMap = context.Snapshot.Fragments.ToDictionary(
            f => f.FragmentId,
            f => f.FragmentType,
            StringComparer.Ordinal);

        var rootCauseLayer = BugfixRootCauseClassifier.Classify(
            diff,
            fragmentMap,
            bugfix.RootCauseLayer);

        var bugFragmentId = $"bugfix:{context.ProjectId}";

        if (!string.IsNullOrWhiteSpace(bugfix.Description))
        {
            yield return new AppendIrEventRequest
            {
                EventType = IrEventTypes.BugReported,
                FragmentId = bugFragmentId,
                FragmentType = IrFragmentTypes.EventSpec,
                FragmentVersion = 1,
                Payload = BugfixManifestBuilder.BuildBugReportedPayload(
                    context.ProjectId, context.RunId, bugfix.Description),
                SkillId = SkillId,
            };
        }

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.BugRootCauseLocated,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 2,
            Payload = BugfixManifestBuilder.BuildBugRootCauseLocatedPayload(
                context.ProjectId,
                context.RunId,
                rootCauseLayer,
                bugfix.RevisionType,
                diff),
            SkillId = SkillId,
        };

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.AffectedFragmentsMarked,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 3,
            Payload = BugfixManifestBuilder.BuildAffectedFragmentsMarkedPayload(
                context.ProjectId, context.RunId, diff),
            SkillId = SkillId,
        };

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.BugFixed,
            FragmentId = bugFragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 4,
            Payload = BugfixManifestBuilder.BuildBugFixedPayload(context.ProjectId, context.RunId, diff),
            SkillId = SkillId,
        };

        _logger.LogInformation(
            "Bugfix skill marked {Invalidated} invalidated, {Changed} changed pipeline={PipelineId}",
            diff.Invalidated.Count,
            diff.Changed.Count,
            context.PipelineId);

        await Task.CompletedTask;
    }

    public Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default)
    {
        _ = ct;
        if (!events.Any(e => e.EventType == IrEventTypes.BugRootCauseLocated))
            return Task.FromResult(SkillValidationResult.Fail("缺少 BugRootCauseLocated"));

        if (!events.Any(e => e.EventType == IrEventTypes.AffectedFragmentsMarked))
            return Task.FromResult(SkillValidationResult.Fail("缺少 AffectedFragmentsMarked"));

        if (!events.Any(e => e.EventType == IrEventTypes.BugFixed))
            return Task.FromResult(SkillValidationResult.Fail("缺少 BugFixed"));

        try
        {
            var marked = events.First(e => e.EventType == IrEventTypes.AffectedFragmentsMarked);
            using var doc = JsonDocument.Parse(marked.Payload);
            if (!doc.RootElement.TryGetProperty("invalidated", out var inv)
                || inv.ValueKind != JsonValueKind.Array
                || inv.GetArrayLength() == 0)
            {
                return Task.FromResult(SkillValidationResult.Fail(
                    "AffectedFragmentsMarked 须含非空 invalidated"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillValidationResult.Fail($"AffectedFragmentsMarked 解析失败: {ex.Message}"));
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }
}
