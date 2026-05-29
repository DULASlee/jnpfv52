using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_TEMPLATE")]
public class MessageTemplateEntity : CLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 模板类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_TYPE")]
    public string? TemplateType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_SOURCE")]
    public string? MessageSource { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_TYPE")]
    public string? MessageType { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string? Title { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string? Content { get; set; }

    /// <summary>
    /// 模板编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_CODE")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 跳转方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_WX_SKIP")]
    public string? WxSkip { get; set; }

    /// <summary>
    /// 小程序id.
    /// </summary>
    [SugarColumn(ColumnName = "F_XCX_APP_ID")]
    public string? XcxAppId { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
