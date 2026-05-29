using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.CommonWords;

/// <summary>
/// 审批常用语列表输入.
/// </summary>
[SuppressSniffer]
public class CommonWordsListInput : PageInputBase
{
    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}
