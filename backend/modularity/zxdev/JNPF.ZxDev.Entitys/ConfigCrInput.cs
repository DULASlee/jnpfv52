using JNPF.JsonSerialization;
using Newtonsoft.Json;

namespace JNPF.ZxDev.Entitys.Dto.Config;
 
/// <summary>
/// 系统配置信息修改输入参数.
/// </summary>
public class ConfigCrInput
{
    /// <summary>
    /// .
    /// </summary>
    public int? id { get; set; }

    /// <summary>
    /// 关键值.
    /// </summary>
    public string? keyValue { get; set; }

}