using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 掩码配置模型.
/// </summary>
[SuppressSniffer]
public class MaskConfigModel
{
    /// <summary>
    /// 填充符号.
    /// </summary>
    public string filler { get; set; }

    /// <summary>
    /// 掩码规则.
    /// </summary>
    public int maskType { get; set; }

    /// <summary>
    /// 开头显示.
    /// </summary>
    public int prefixType { get; set; }

    /// <summary>
    /// 开头字数.
    /// </summary>
    public int prefixLimit { get; set; }

    /// <summary>
    /// 开头字符.
    /// </summary>
    public string prefixSpecifyChar { get; set; }

    /// <summary>
    /// 结尾显示.
    /// </summary>
    public int suffixType { get; set; }

    /// <summary>
    /// 结尾字数.
    /// </summary>
    public int suffixLimit { get; set; }

    /// <summary>
    /// 结尾字符.
    /// </summary>
    public string suffixSpecifyChar { get; set; }

    /// <summary>
    /// 忽略掩码字符.
    /// </summary>
    public string ignoreChar { get; set; }

    /// <summary>
    /// 虚拟掩码.
    /// </summary>
    public bool useUnrealMask { get; set; }

    /// <summary>
    /// 虚拟掩码长度.
    /// </summary>
    public int unrealMaskLength { get; set; }
}