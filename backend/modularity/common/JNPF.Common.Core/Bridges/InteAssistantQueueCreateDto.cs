namespace JNPF.Bridges;

/// <summary>
/// Queue row to insert into BASE_INTEGRATE_QUEUE (no Sugar entity).
/// </summary>
public sealed class InteAssistantQueueCreateDto
{
    public string Id { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string IntegrateId { get; init; } = string.Empty;

    public int State { get; init; }

    public string? Description { get; init; }

    public DateTime CreatorTime { get; init; }

    public string CreatorUserId { get; init; } = string.Empty;

    public int EnabledMark { get; init; } = 1;
}
