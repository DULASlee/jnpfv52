using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Group;

/// <summary>
/// 获取分组列表输入.
/// </summary>
[SuppressSniffer]
public class GroupConditionInput : KeywordInput
{
    /// <summary>
    /// 选择的分组id.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();
}
