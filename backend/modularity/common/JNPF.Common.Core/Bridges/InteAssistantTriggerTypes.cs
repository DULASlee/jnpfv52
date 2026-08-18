namespace JNPF.Bridges;

/// <summary>
/// Characterization of CreateInte trigger remapping (batch add/delete → stored trigger).
/// </summary>
public static class InteAssistantTriggerTypes
{
    /// <summary>
    /// Map event-bus trigger type to BASE_INTEGRATE.F_TRIGGER_TYPE filter value.
    /// </summary>
    public static int ToStoredTriggerType(int eventTriggerType) => eventTriggerType switch
    {
        4 => 1,
        5 => 3,
        _ => eventTriggerType,
    };
}
