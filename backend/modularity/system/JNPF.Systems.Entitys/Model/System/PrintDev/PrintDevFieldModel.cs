using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.PrintDev;

/// <summary>
/// 打印模板配置字段模型.
/// </summary>
[SuppressSniffer]
public class PrintDevFieldModel : TreeModel
{
    /// <summary>
    /// 字段说明.
    /// </summary>
    public string fullName { get; set; }
}