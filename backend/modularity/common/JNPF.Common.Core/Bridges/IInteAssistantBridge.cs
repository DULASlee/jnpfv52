using SqlSugar;

namespace JNPF.Bridges;

/// <summary>
/// Dependency-inversion bridge: Common.Core / framework call InteAssistant capabilities
/// without a compile-time ProjectReference to JNPF.InteAssistant*.
/// W4 TopN: form-trigger lookup + queue insert (sole Common.Core → Entitys consumers).
/// </summary>
public interface IInteAssistantBridge
{
    /// <summary>
    /// List enabled Type=1 (event) integrations for a form.
    /// Applies CreateInte remapping: trigger 4→stored 1, 5→stored 3.
    /// </summary>
    Task<IReadOnlyList<InteAssistantDefinitionDto>> ListEnabledFormEventIntegrationsAsync(
        ISqlSugarClient db,
        string formId,
        int eventTriggerType,
        CancellationToken cancellationToken = default);

    InteAssistantQueueCreateDto CreateQueueItem(
        string fullName,
        string integrateId,
        int state,
        string description,
        string userId);

    Task<int> InsertQueueAsync(
        ISqlSugarClient db,
        IReadOnlyList<InteAssistantQueueCreateDto> items,
        CancellationToken cancellationToken = default);
}
