using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys;

/// <summary>
/// 在线聊天
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_IM_CONTENT")]
public class IMContentEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 发送者.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SEND_USER_ID")]
    public string SendUserId { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SEND_TIME")]
    public DateTime? SendTime { get; set; }

    /// <summary>
     /// 接收者.
     /// </summary>
     /// <returns></returns>
    [SugarColumn(ColumnName = "F_RECEIVE_USER_ID")]
    public string ReceiveUserId { get; set; }

    /// <summary>
    /// 接收时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_RECEIVE_TIME")]
    public DateTime? ReceiveTime { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string Content { get; set; }

    /// <summary>
    /// 内容类型：text、img、file.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT_TYPE")]
    public string ContentType { get; set; }
}