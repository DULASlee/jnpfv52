using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板字段查询输入.
/// </summary>
[SuppressSniffer]
public class PrintDevFieldsQuery
{
    /// <summary>
    /// sql语句.
    /// </summary>
    public string sqlTemplate { get; set; }

    /// <summary>
    /// 连接id.
    /// </summary>
    public string dbLinkId { get; set; }
}