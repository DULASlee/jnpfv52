using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据接口日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DATA_INTERFACE_LOG")]
public class DataInterfaceLogEntity : CLDEntityBase
{
    /// <summary>
    /// 调用接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_ID")]
    public string InvokId { get; set; }

    /// <summary>
    /// 调用时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_TIME")]
    public DateTime? InvokTime { get; set; }

    /// <summary>
    /// 调用者.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 请求ip.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_IP")]
    public string InvokIp { get; set; }

    /// <summary>
    /// 请求设备.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_DEVICE")]
    public string InvokDevice { get; set; }

    /// <summary>
    /// 请求耗时.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_WASTE_TIME")]
    public int? InvokWasteTime { get; set; }

    /// <summary>
    /// 请求类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_INVOK_TYPE")]
    public string InvokType { get; set; }

    /// <summary>
    /// 授权appid.
    /// </summary>
    [SugarColumn(ColumnName = "F_OAUTH_APP_ID")]
    public string OauthAppId { get; set; }
}