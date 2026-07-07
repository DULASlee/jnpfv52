namespace JNPF.InteAssistant.Llm;

/// <summary>
/// LLM 调用审计上下文（AsyncLocal），供 LlmGatewayService.WriteCallLogAsync 写入 runId/skillId/projectId。
/// </summary>
public static class LlmCallAuditContext
{
    private static readonly AsyncLocal<LlmCallAudit?> Current = new();

    public static LlmCallAudit? CurrentAudit => Current.Value;

    public static IDisposable Begin(string runId, string skillId, string projectId, string tenantId, string? pipelineId = null)
    {
        var previous = Current.Value;
        Current.Value = new LlmCallAudit(runId, skillId, projectId, tenantId, pipelineId ?? projectId);
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly LlmCallAudit? _previous;
        public Scope(LlmCallAudit? previous) => _previous = previous;
        public void Dispose() => Current.Value = _previous;
    }
}

public sealed record LlmCallAudit(string RunId, string SkillId, string ProjectId, string TenantId, string PipelineId);
