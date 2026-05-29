using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板字段输出.
/// </summary>
[SuppressSniffer]
public class PrintDevFieldsOutput
{
    /// <summary>
    /// 我的财产.
    /// </summary>
    public string MyProperty { get; set; }
}