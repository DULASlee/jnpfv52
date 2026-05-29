using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 微信公众号用户
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_WECHAT_USER")]
public class MessageWechatUserEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 公众号id.
    /// </summary>
    [SugarColumn(ColumnName = "F_GZH_ID")]
    public string? GzhId { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }

    /// <summary>
    /// 公众号用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPEN_ID")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 是否关注.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLOSE_MARK")]
    public int? CloseMark { get; set; }
}
