namespace JNPF.InteAssistant.Sa;

/// <summary>
/// sa-service Agent 名 ↔ IR SaStepName 映射（P2-B06）
/// </summary>
public static class SaStepMapping
{
    public static readonly (string AgentName, string IrStepName)[] All =
    {
        ("ScopeAgent", "DomainModel"),
        ("DFDAgent", "AggregateDesign"),
        ("BPMAgent", "EventCatalog"),
        ("DictAgent", "CommandQuery"),
        ("PSpecAgent", "IntegrationPoints"),
        ("DecisionTableAgent", "WorkflowSpec"),
        ("StateMachineAgent", "UISpec"),
        ("ERAgent", "DataModel"),
        ("UIAgent", "DeliveryChecklist"),
    };

    public static string ToAgentName(string irStepName)
    {
        var pair = All.FirstOrDefault(x => x.IrStepName == irStepName);
        return pair.AgentName ?? irStepName;
    }

    public static string ToIrStepName(string agentName)
    {
        var pair = All.FirstOrDefault(x => x.AgentName == agentName);
        return pair.IrStepName ?? agentName;
    }

    public static IReadOnlyList<string> IrStepOrder { get; } = All.Select(x => x.IrStepName).ToList();
}
