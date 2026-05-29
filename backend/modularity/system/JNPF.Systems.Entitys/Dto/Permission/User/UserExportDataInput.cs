using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户数据导出 输入.
/// </summary>
[SuppressSniffer]
public class UserExportDataInput : CommonInput
{
    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 导出类型 (0：分页数据，其他：全部数据).
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 选择的导出 字段集合 按 , 号隔开.
    /// </summary>
    public string selectKey { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public int? gender { get; set; }
}