using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 修改外链信息 输入.
/// </summary>
[SuppressSniffer]
public class VisualdevShortLinkFormInput
{
    public string id { get; set; }
    public string shortLink { get; set; }
    public int formUse { get; set; }
    public string formLink { get; set; }
    public int formPassUse { get; set; }
    public string formPassword { get; set; }
    public int columnUse { get; set; }
    public string columnLink { get; set; }
    public int columnPassUse { get; set; }
    public string columnPassword { get; set; }
    public string columnCondition { get; set; }
    public string columnText { get; set; }
    public int enabledMark { get; set; }
}
