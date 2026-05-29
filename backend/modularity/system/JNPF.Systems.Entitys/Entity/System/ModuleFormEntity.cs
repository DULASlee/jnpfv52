using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 表单权限
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MODULE_FORM")]
[Tenant(ClaimConst.TENANTID)]
public class ModuleFormEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 功能上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 功能编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 绑定表格Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIND_TABLE")]
    public string BindTable { get; set; }

    /// <summary>
    /// 规则(0:主表，1：副表 2:子表).
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_RULE")]
    public int? FieldRule { get; set; }

    /// <summary>
    /// 子表规则key.
    /// </summary>
    [SugarColumn(ColumnName = "F_CHILD_TABLE_KEY")]
    public string ChildTableKey { get; set; }
}