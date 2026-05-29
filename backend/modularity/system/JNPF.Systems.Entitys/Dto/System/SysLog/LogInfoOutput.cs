using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysLog;

/// <summary>
/// 登录日记输出.
/// </summary>
[SuppressSniffer]
public class LogInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 登录时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 登录用户.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 登录IP.
    /// </summary>
    public string ipAddress { get; set; }

    /// <summary>
    /// IP所在城市.
    /// </summary>
    public string ipAddressName { get; set; }

    /// <summary>
    /// 浏览器.
    /// </summary>
    public string browser { get; set; }

    /// <summary>
    /// 平台设备.
    /// </summary>
    public string platForm { get; set; }

    /// <summary>
    /// 操作模块.
    /// </summary>
    public string moduleName { get; set; }

    /// <summary>
    /// 请求耗时.
    /// </summary>
    public int? requestDuration { get; set; }

    /// <summary>
    /// 请求地址.
    /// </summary>
    public string requestUrl { get; set; }

    /// <summary>
    /// 请求类型.
    /// </summary>
    public string requestMethod { get; set; }

    /// <summary>
    /// 请求参数.
    /// </summary>
    public string requestParam { get; set; }

    /// <summary>
    /// 请求方法.
    /// </summary>
    public string requestTarget { get; set; }

    /// <summary>
    /// 结果.
    /// </summary>
    public string jsons { get; set; }
}