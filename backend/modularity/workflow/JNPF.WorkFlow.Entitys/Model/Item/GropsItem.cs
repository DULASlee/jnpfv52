namespace JNPF.WorkFlow.Entitys.Model.Item;

public class GropsItem
{
    /// <summary>
    /// 逻辑符号.
    /// </summary>
    public string? logic { get; set; }

    /// <summary>
    /// 分组逻辑.
    /// </summary>
    public List<ConditionsItem>? groups { get; set; }
}
