namespace JNPF.Bridges;

/// <summary>
/// Snapshot of BASE_INTEGRATE row fields needed by event-trigger enqueue (no Sugar entity).
/// </summary>
public sealed class InteAssistantDefinitionDto
{
    public string Id { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? TemplateJson { get; init; }
}
