namespace JNPF.Common.Models;

/// <summary>
/// oracle扩展属性模型.
/// </summary>
public class OracleParamModel
{
    /// <summary>
    /// oracle扩展属性开启.
    /// </summary>
    public bool oracleExtend { get; set; }

    /// <summary>
    /// oracle连接方式.
    /// </summary>
    public string oracleLinkType { get; set; }

    /// <summary>
    /// oracle角色.
    /// </summary>
    public string oracleRole { get; set; }

    /// <summary>
    /// oracle服务名.
    /// </summary>
    public string oracleService { get; set; }
}