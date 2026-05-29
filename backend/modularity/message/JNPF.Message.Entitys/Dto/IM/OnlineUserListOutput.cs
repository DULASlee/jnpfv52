using JNPF.DependencyInjection;

namespace JNPF.Message.Entitys.Dto.IM;

/// <summary>
/// 在线用户
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2017.09.20.
/// </summary>
[SuppressSniffer]
public class OnlineUserListOutput
{
    /// <summary>
    /// 用户ID.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 用户账号.
    /// </summary>
    public string userAccount { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 登录时间.
    /// </summary>
    public string loginTime { get; set; }

    /// <summary>
    /// 登录IP地址.
    /// </summary>
    public string loginIPAddress { get; set; }

    /// <summary>
    /// 登录平台设备.
    /// </summary>
    public string loginPlatForm { get; set; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string tenantId { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 登录地点.
    /// </summary>
    public string loginAddress { get; set; }

    /// <summary>
    /// 浏览器.
    /// </summary>
    public string loginBrowser { get; set; }

    /// <summary>
    /// 操作系统.
    /// </summary>
    public string loginSystem { get; set; }

}