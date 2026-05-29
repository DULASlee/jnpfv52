using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.PrintDev;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板配置数据输出.
/// </summary>
[SuppressSniffer]
public class PrintDevDataOutput
{
    /// <summary>
    /// sql数据.
    /// </summary>
    public object printData { get; set; }

    /// <summary>
    /// 模板数据.
    /// </summary>
    public string printTemplate { get; set; }

    /// <summary>
    /// 纸张参数.
    /// </summary>
    public string pageParam { get; set; }

    /// <summary>
    /// 审批数据.
    /// </summary>
    public List<PrintDevDataModel> operatorRecordList { get; set; } = new List<PrintDevDataModel>();
}