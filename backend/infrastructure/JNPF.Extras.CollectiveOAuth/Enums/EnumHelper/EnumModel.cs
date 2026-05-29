namespace JNPF.Extras.CollectiveOAuth.Enums;

/// <summary>
/// 枚举模型.
/// </summary>
public struct EnumModel
{
    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="um"></param>
    public EnumModel(Enum um)
    {
        this.value = (int)Convert.ChangeType(um, typeof(int));
        this.name = um.ToString();
        this.text = um.GetDesc();
    }

    public int value { get; set; }

    public string name { get; set; }

    public string text { get; set; }
}