using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys;

/// <summary>
/// 聊天会话.
/// </summary>
[SugarTable("BASE_IM_REPLY")]
public class ImReplyEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 发送者.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 接收用户.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_RECEIVE_USER_ID")]
    public string ReceiveUserId { get; set; }

    /// <summary>
    /// 接收用户时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_RECEIVE_TIME")]
    public DateTime? ReceiveTime { get; set; }
}