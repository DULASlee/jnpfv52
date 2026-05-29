using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbLink;

/// <summary>
/// 数据连接修改输入.
/// </summary>
[SuppressSniffer]
public class DbLinkUpInput : DbLinkCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}