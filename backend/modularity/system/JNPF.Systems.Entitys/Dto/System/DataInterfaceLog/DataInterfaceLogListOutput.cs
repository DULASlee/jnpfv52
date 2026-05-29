using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataInterfaceLog;

/// <summary>
/// 数据接口日记列表输出.
/// </summary>
[SuppressSniffer]
public class DataInterfaceLogListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 接口名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 接口编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 调用时间.
    /// </summary>
    public DateTime? invokTime { get; set; }

    /// <summary>
    /// 调用者.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// ip.
    /// </summary>
    public string invokIp { get; set; }

    /// <summary>
    /// 设备.
    /// </summary>
    public string invokDevice { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string invokType { get; set; }

    /// <summary>
    /// 耗时.
    /// </summary>
    public int? invokWasteTime { get; set; }
}