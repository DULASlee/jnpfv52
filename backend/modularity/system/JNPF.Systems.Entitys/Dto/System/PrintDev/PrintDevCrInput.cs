using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板创建输入.
/// </summary>
[SuppressSniffer]
public class PrintDevCrInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 类型（1：流程表单，2：功能表单）.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 数据连接id.
    /// </summary>
    public string dbLinkId { get; set; }

    /// <summary>
    /// sql模板.
    /// </summary>
    public string sqlTemplate { get; set; }

    /// <summary>
    /// 左侧字段.
    /// </summary>
    public string leftFields { get; set; }

    /// <summary>
    /// 打印模板.
    /// </summary>
    public string printTemplate { get; set; }

    /// <summary>
    /// 纸张参数.
    /// </summary>
    public string pageParam { get; set; }

    /// <summary>
    /// 数据来源.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 数据接口参数.
    /// </summary>
    public string parameterJson { get; set; }
}