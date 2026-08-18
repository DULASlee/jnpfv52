using JNPF.Bridges;
using JNPF.Common.Security;
using JNPF.InteAssistant.Entitys.Entity;
using SqlSugar;

namespace JNPF.InteAssistant.Bridges;

/// <summary>
/// InteAssistant-side implementation of <see cref="IInteAssistantBridge"/> (registered in API.Entry).
/// </summary>
public sealed class InteAssistantBridge : IInteAssistantBridge
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InteAssistantDefinitionDto>> ListEnabledFormEventIntegrationsAsync(
        ISqlSugarClient db,
        string formId,
        int eventTriggerType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var storedTrigger = InteAssistantTriggerTypes.ToStoredTriggerType(eventTriggerType);
        var rows = await db.Queryable<IntegrateEntity>()
            .Where(it => it.Type.Equals(1)
                && it.FormId.Equals(formId)
                && it.DeleteMark == null
                && it.EnabledMark.Equals(1)
                && it.TriggerType.Equals(storedTrigger))
            .ToListAsync(cancellationToken);

        return rows.Select(r => new InteAssistantDefinitionDto
        {
            Id = r.Id,
            FullName = r.FullName,
            TemplateJson = r.TemplateJson,
        }).ToList();
    }

    /// <inheritdoc />
    public InteAssistantQueueCreateDto CreateQueueItem(
        string fullName,
        string integrateId,
        int state,
        string description,
        string userId)
    {
        return new InteAssistantQueueCreateDto
        {
            Id = SnowflakeIdHelper.NextId(),
            FullName = fullName,
            IntegrateId = integrateId,
            State = state,
            Description = description,
            CreatorTime = DateTime.Now,
            CreatorUserId = userId,
            EnabledMark = 1,
        };
    }

    /// <inheritdoc />
    public Task<int> InsertQueueAsync(
        ISqlSugarClient db,
        IReadOnlyList<InteAssistantQueueCreateDto> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (items == null || items.Count == 0)
            return Task.FromResult(0);

        var entities = items.Select(i => new IntegrateQueueEntity
        {
            Id = i.Id,
            FullName = i.FullName,
            IntegrateId = i.IntegrateId,
            ExecutionTime = null,
            State = i.State,
            Description = i.Description,
            CreatorTime = i.CreatorTime,
            CreatorUserId = i.CreatorUserId,
            EnabledMark = i.EnabledMark,
        }).ToList();

        return db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }
}
