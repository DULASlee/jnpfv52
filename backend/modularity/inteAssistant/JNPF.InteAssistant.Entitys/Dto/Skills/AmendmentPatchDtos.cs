namespace JNPF.InteAssistant.Entitys.Dto.Skills;

/// <summary>
/// 需求修订补丁操作。Apply 阶段只消费这些类型化操作，避免用散文再次驱动 LLM 改骨架。
/// </summary>
public enum AmendmentPatchOperation
{
    AddEntity,
    AddEvent,
    PatchRule,
    AddField,
    PatchSummary,
    AddStateTransition,
}

public sealed record AmendmentPatch(
    AmendmentPatchOperation Operation,
    string Target,
    string Name,
    string? DisplayName = null,
    string? Type = null,
    string? Description = null,
    bool Required = false,
    string? References = null,
    string? ScopeEventId = null,
    string? From = null,
    string? To = null);

public sealed record AmendmentPatchSet(IReadOnlyList<AmendmentPatch> Patches)
{
    public static AmendmentPatchSet Empty { get; } = new(Array.Empty<AmendmentPatch>());
}
