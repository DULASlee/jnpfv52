using System.Text.Json;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Skills.Testing;

public static class TestSuiteManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string BuildTestSuiteGeneratedPayload(
        string projectId,
        string runId,
        TesterBuildResult input,
        IReadOnlyList<DerivedTestCase> cases)
    {
        var fragmentId = $"testsuite:{projectId}";
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = fragmentId,
            stabilityState = IrStabilityStates.Stable,
            derivedAt = DateTime.UtcNow.ToString("O"),
            runId,
            derivationMode = input.DerivationMode,
            scenarioCount = cases.Count,
            scenarios = cases.Select(c => new
            {
                caseId = c.CaseId,
                rule = c.Rule,
                kind = c.Kind,
                description = c.Description,
            }),
            metadata = new
            {
                formPageName = input.FormPageName,
                archGuardWarnings = input.ArchGuardWarnings.Select(w => new
                {
                    w.RuleId,
                    w.Message,
                    w.FilePath,
                }),
                minimumRequired = input.DerivationMode == "field-and-state-machine"
                    ? TestCaseDeriver.MinFieldAndStateMachine
                    : TestCaseDeriver.MinFieldOnly,
            },
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildTesterSkillCompletedPayload(string projectId, int scenarioCount)
    {
        return JsonSerializer.Serialize(new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            projectId,
            scenarioCount,
            completedAt = DateTime.UtcNow.ToString("O"),
        }, JsonOptions);
    }
}
