using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B01a — DeveloperSkill 落盘 + CodeGenerated payload。
/// </summary>
public static class IrPhase4DeveloperTests
{
    public static async Task RunAllAsync()
    {
        TestDevelopmentSkillIds_Defined();
        await TestDeveloperSkill_ValidateInputAsync();
        await TestDeveloperSkill_ReasonAsync_LeaveSimpleFixtureAsync();
        Console.WriteLine("[Phase4] Developer skill tests passed.");
    }

    private static void TestDevelopmentSkillIds_Defined()
    {
        if (DevelopmentSkillIds.Developer != "developer-skill")
            throw new InvalidOperationException("DevelopmentSkillIds.Developer mismatch");

        if (IrEventTypes.CodeGenerated != "CodeGenerated")
            throw new InvalidOperationException("IrEventTypes.CodeGenerated missing");
    }

    private static async Task TestDeveloperSkill_ValidateInputAsync()
    {
        var gate = new SystemDesignLockedCompletenessGate();
        var incomplete = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:1",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = "{}",
                },
            },
        };

        var result = await gate.ValidateAsync(incomplete);
        if (result.IsValid)
            throw new InvalidOperationException("ValidateInput should fail without SystemDesign locked");

        var skill = CreateDeveloperSkill();
        var okSnapshot = BuildLeaveSimpleSnapshot(includeSystemDesign: true);
        var ok = await skill.ValidateInputAsync(okSnapshot);
        if (!ok.IsValid)
            throw new InvalidOperationException($"ValidateInput should pass: {ok.ErrorMessage}");
    }

    private static async Task TestDeveloperSkill_ReasonAsync_LeaveSimpleFixtureAsync()
    {
        const string tenantId = "_phase4-test";
        const string projectId = "leave-simple-d4";

        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);

        var skill = CreateDeveloperSkill();
        var snapshot = BuildLeaveSimpleSnapshot(includeSystemDesign: true);
        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9001,
            UserRequirement = "请假 MVP",
            Snapshot = snapshot,
        };

        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(context))
            events.Add(evt);

        var outputValidation = await skill.ValidateOutputAsync(events);
        if (!outputValidation.IsValid)
            throw new InvalidOperationException(outputValidation.ErrorMessage ?? "ValidateOutput failed");

        if (events.Count != 2)
            throw new InvalidOperationException("Expected 2 IR events");

        if (!File.Exists(Path.Combine(backendRoot, "Entitys", "LeaveRequestEntity.cs")))
            throw new InvalidOperationException("Entity file not written");

        if (!File.Exists(Path.Combine(backendRoot, "Services", "LeaveRequestService.cs")))
            throw new InvalidOperationException("Service file not written");

        using var doc = JsonDocument.Parse(events[0].Payload);
        if (!doc.RootElement.TryGetProperty("templateVersions", out var tv) || tv.GetArrayLength() != 3)
            throw new InvalidOperationException("CodeGenerated templateVersions count != 3");

        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);
    }

    public static DeveloperSkillService CreateDeveloperSkillPublic() => CreateDeveloperSkill();

    public static IrSnapshot BuildLeaveSimpleSnapshotPublic(bool includeSystemDesign) =>
        BuildLeaveSimpleSnapshot(includeSystemDesign);

    private static DeveloperSkillService CreateDeveloperSkill()
    {
        using var loggerFactory = LoggerFactory.Create(static _ => { });
        return new DeveloperSkillService(
            new TemplateContextBuilder(),
            new CodegenWorkspaceWriter(),
            new SystemDesignLockedCompletenessGate(),
            loggerFactory.CreateLogger<DeveloperSkillService>());
    }

    private static IrSnapshot BuildLeaveSimpleSnapshot(bool includeSystemDesign)
    {
        var samplesDir = TemplateRenderSamplesTests.ResolveSamplesDirPublic();
        var json = File.ReadAllText(Path.Combine(samplesDir, "leave-simple.json"));
        using var doc = JsonDocument.Parse(json);
        var fragmentsEl = doc.RootElement.GetProperty("fragments");
        var fragments = new List<IrSnapshotFragment>();

        foreach (var f in fragmentsEl.EnumerateArray())
        {
            var fragmentType = f.GetProperty("fragmentType").GetString() ?? string.Empty;
            if (!includeSystemDesign && fragmentType == IrFragmentTypes.SystemDesign)
                continue;

            var payloadEl = f.GetProperty("payload");
            var payload = payloadEl.ValueKind == JsonValueKind.String
                ? payloadEl.GetString() ?? "{}"
                : payloadEl.GetRawText();

            fragments.Add(new IrSnapshotFragment
            {
                FragmentId = f.GetProperty("fragmentId").GetString() ?? string.Empty,
                FragmentType = fragmentType,
                StabilityState = f.TryGetProperty("stabilityState", out var st)
                    ? st.GetString() ?? IrStabilityStates.Stable
                    : IrStabilityStates.Stable,
                Payload = payload,
            });
        }

        return new IrSnapshot { Fragments = fragments };
    }
}
