using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据接口
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DATA_INTERFACE")]
public class DataInterfaceEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 分类ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 类型(1-sql，2-静态数据，3-api).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 类型(3-查询).
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTION")]
    public int? Action { get; set; }

    /// <summary>
    /// 分页(0-禁用，1-启用).
    /// </summary>
    [SugarColumn(ColumnName = "F_HAS_PAGE")]
    public int? HasPage { get; set; }

    /// <summary>
    /// 后置接口(0-禁用，1-启用).
    /// </summary>
    [SugarColumn(ColumnName = "f_is_postposition")]
    public int? IsPostposition { get; set; }

    /// <summary>
    /// 数据配置json.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_CONFIG_JSON")]
    public string DataConfigJson { get; set; }

    /// <summary>
    /// 数据统计json.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_COUNT_JSON")]
    public string DataCountJson { get; set; }

    /// <summary>
    /// 数据回显json.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_ECHO_JSON")]
    public string DataEchoJson { get; set; }

    /// <summary>
    /// 异常验证json.
    /// </summary>
    [SugarColumn(ColumnName = "f_data_exception_json")]
    public string DataExceptionJson { get; set; }

    /// <summary>
    /// 数据处理json.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_JS_JSON")]
    public string DataJsJson { get; set; }

    /// <summary>
    /// 参数json.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARAMETER_JSON")]
    public string ParameterJson { get; set; }

    /// <summary>
    /// 字段json.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_JSON")]
    public string FieldJson { get; set; }

    /// <summary>
    /// 获取或设置 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}