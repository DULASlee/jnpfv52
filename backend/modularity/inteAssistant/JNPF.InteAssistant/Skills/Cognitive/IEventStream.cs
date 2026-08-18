using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;

namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// 认知技能的"血液"门面（施工包 21 §3.3）——
/// 唯一职责是把 SkillExecutionScope 中的 project/tenant 上下文接到
/// IIrEventStoreService.AppendAsync（Schema/IOI 校验、投影、SSE 全部复用既有管线）。
/// </summary>
public interface IEventStream
{
    /// <summary>后台技能线程内追加事件，project/tenant 取自 SkillExecutionScope.CurrentScope。</summary>
    Task<AiIrEventEntity> AppendAsync(AppendIrEventRequest request, CancellationToken ct = default);

    /// <summary>显式上下文重载（API 层 / 人工纠偏等无技能作用域的场景）。</summary>
    Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest request, CancellationToken ct = default);
}

public sealed class IrEventStreamFacade : IEventStream, ITransient
{
    private readonly IIrEventStoreService _eventStore;

    public IrEventStreamFacade(IIrEventStoreService eventStore) => _eventStore = eventStore;

    public Task<AiIrEventEntity> AppendAsync(AppendIrEventRequest request, CancellationToken ct = default)
    {
        var scope = SkillExecutionScope.CurrentScope
            ?? throw Oops.Oh("IEventStream.AppendAsync 需要 SkillExecutionScope 上下文，请使用显式重载");
        return _eventStore.AppendAsync(scope.ProjectId, scope.TenantId, request, ct);
    }

    public Task<AiIrEventEntity> AppendAsync(string projectId, string tenantId, AppendIrEventRequest request, CancellationToken ct = default)
        => _eventStore.AppendAsync(projectId, tenantId, request, ct);
}
