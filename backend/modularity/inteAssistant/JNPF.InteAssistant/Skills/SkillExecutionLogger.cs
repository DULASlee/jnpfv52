using JNPF.DependencyInjection;
using JNPF.InteAssistant.Runtime;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace JNPF.InteAssistant.Skills;

public interface ISkillExecutionLogger
{
    IDisposable BeginScope(string runId, string tenantId, string projectId, long pipelineId, string skillId);
    void LogPhase(string phase, string outcome, long elapsedMs, string? eventId = null, string? saStepName = null, string? message = null);
}

public sealed class SkillExecutionLogger : ISkillExecutionLogger, ITransient
{
    private readonly ILogger<SkillExecutionLogger> _logger;

    public SkillExecutionLogger(ILogger<SkillExecutionLogger> logger) => _logger = logger;

    public IDisposable BeginScope(string runId, string tenantId, string projectId, long pipelineId, string skillId)
    {
        var scope = SkillExecutionScope.CurrentScope;
        runId = scope?.RunId ?? runId;
        tenantId = scope?.TenantId ?? tenantId;
        projectId = scope?.ProjectId ?? projectId;
        pipelineId = scope?.PipelineId ?? pipelineId;
        skillId = scope?.SkillId ?? skillId;

        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("RunId", runId),
            LogContext.PushProperty("TraceId", runId),
            LogContext.PushProperty("TenantId", tenantId),
            LogContext.PushProperty("ProjectId", projectId),
            LogContext.PushProperty("PipelineId", pipelineId),
            LogContext.PushProperty("SkillId", skillId),
        };
        return new CompositeDisposable(disposables);
    }

    public void LogPhase(string phase, string outcome, long elapsedMs, string? eventId = null, string? saStepName = null, string? message = null)
    {
        _logger.LogInformation(
            "SkillPhase {Phase} {Outcome} {ElapsedMs}ms EventId={EventId} SaStep={SaStepName} {Message}",
            phase, outcome, elapsedMs, eventId, saStepName, message);
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IReadOnlyList<IDisposable> _items;
        public CompositeDisposable(IReadOnlyList<IDisposable> items) => _items = items;
        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();
        }
    }
}
