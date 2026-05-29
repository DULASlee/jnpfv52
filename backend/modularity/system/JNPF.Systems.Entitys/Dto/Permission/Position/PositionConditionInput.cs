using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 获取岗位列表输入.
/// </summary>
[SuppressSniffer]
public class PositionConditionInput : KeywordInput
{
    /// <summary>
    /// 选择的岗位id、部门id、动态参数.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();
}
