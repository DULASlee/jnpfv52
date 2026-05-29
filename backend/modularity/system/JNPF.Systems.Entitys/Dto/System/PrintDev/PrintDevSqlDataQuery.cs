using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板配置sql数据查询.
/// </summary>
[SuppressSniffer]
public class PrintDevSqlDataQuery
{
    /// <summary>
    /// 模板id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 参数.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public List<string> ids { get; set; }
}