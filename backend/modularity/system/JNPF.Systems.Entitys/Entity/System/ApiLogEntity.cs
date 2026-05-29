using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 日记.
/// </summary>
[SugarTable("BASE_API_LOG")]
public class ApiLogEntity : CLDEntityBase
{
    /// <summary>
    /// 用户主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_NAME")]
    public string UserName { get; set; }

    /// <summary>
    /// 日志类型
    /// 1.登录日记,2-访问日志,3-操作日志,4-异常日志,5-请求日志.
    /// </summary>
    [SugarColumn(ColumnName = "F_Type")]
    public int? Type { get; set; }

    /// <summary>
    /// 日志级别.
    /// </summary>
    [SugarColumn(ColumnName = "F_LEVEL")]
    public int? Level { get; set; }

    /// <summary>
    /// IP地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_IP_ADDRESS")]
    public string IPAddress { get; set; }

    /// <summary>
    /// IP所在城市.
    /// </summary>
    [SugarColumn(ColumnName = "F_IP_ADDRESS_NAME")]
    public string IPAddressName { get; set; }

    /// <summary>
    /// 请求地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_URL")]
    public string RequestURL { get; set; }

    /// <summary>
    /// 请求方法.
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_METHOD")]
    public string RequestMethod { get; set; }

    /// <summary>
    /// 请求耗时.
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_DURATION")]
    public int? RequestDuration { get; set; }

    /// <summary>
    /// 日志内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_JSON")]
    public string Json { get; set; }

    /// <summary>
    /// 平台设备.
    /// </summary>
    [SugarColumn(ColumnName = "F_PLAT_FORM")]
    public string PlatForm { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_NAME")]
    public string ModuleName { get; set; }

    /// <summary>
    /// 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID")]
    public string ObjectId { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 浏览器.
    /// </summary>
    [SugarColumn(ColumnName = "F_BROWSER")]
    public string Browser { get; set; }

    /// <summary>
    /// 请求参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_PARAM")]
    public string RequestParam { get; set; }

    /// <summary>
    /// 请求方法.
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_TARGET")]
    public string RequestTarget { get; set; }

    /// <summary>
    /// 是否登录成功标志.
    /// </summary>
    [SugarColumn(ColumnName = "F_LOGIN_MARK")]
    public int? LoginMark { get; set; }

    /// <summary>
    /// 登录类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_LOGIN_TYPE")]
    public int? LoginType { get; set; }

    [SugarColumn(ColumnName = "F_REQUEST_Body_Type")]
    public string RequestBodyType { get; set; }

    [SugarColumn(ColumnName = "F_REQUEST_Body")]
    public string RequestBody { get; set; }


    [SugarColumn(ColumnName = "F_REQUEST_Headers")]
    public string RequestHeaders { get; set; }

    [SugarColumn(ColumnName = "F_REQUEST_Result")]
    public string RequestResult { get; set; }

    [SugarColumn(ColumnName = "F_Msg")]
    public string Msg { get; set; }

    [SugarColumn(ColumnName = "F_Status")]
    public int Status { get; set; }

}