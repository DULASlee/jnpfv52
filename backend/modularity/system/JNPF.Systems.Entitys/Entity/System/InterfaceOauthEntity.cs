using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 接口认证
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DATA_INTERFACE_OAUTH")]
public class InterfaceOauthEntity : CLDSEntityBase
{
    /// <summary>
    /// 应用id.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_NAME")]
    public string AppName { get; set; }

    /// <summary>
    /// 应用秘钥.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_SECRET")]
    public string AppSecret { get; set; }

    /// <summary>
    /// 验证签名.
    /// </summary>
    [SugarColumn(ColumnName = "F_VERIFY_SIGNATURE")]
    public int? VerifySignature { get; set; }

    /// <summary>
    /// 使用期限.
    /// </summary>
    [SugarColumn(ColumnName = "F_USEFUL_LIFE")]
    public DateTime? UsefulLife { get; set; }

    /// <summary>
    /// 白名单.
    /// </summary>
    [SugarColumn(ColumnName = "F_WHITE_LIST")]
    public string WhiteList { get; set; }

    /// <summary>
    /// 黑名单.
    /// </summary>
    [SugarColumn(ColumnName = "F_BLACK_LIST")]
    public string BlackList { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 接口列表.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_INTERFACE_IDS")]
    public string DataInterfaceIds { get; set; }
}