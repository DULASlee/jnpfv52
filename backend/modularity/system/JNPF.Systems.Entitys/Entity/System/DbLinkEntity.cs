using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据连接
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DB_LINK")]
public class DbLinkEntity : CLDEntityBase
{
    /// <summary>
    /// 连接名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 连接驱动.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_TYPE")]
    public string DbType { get; set; }

    /// <summary>
    /// 主机名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_HOST")]
    public string Host { get; set; }

    /// <summary>
    /// 端口.
    /// </summary>
    [SugarColumn(ColumnName = "F_PORT")]
    public int? Port { get; set; }

    /// <summary>
    /// 用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_NAME")]
    public string UserName { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    [SugarColumn(ColumnName = "F_PASSWORD")]
    public string Password { get; set; }

    /// <summary>
    /// 服务名称（ORACLE 用的）.
    /// </summary>
    [SugarColumn(ColumnName = "F_SERVICE_NAME")]
    public string ServiceName { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 表模式.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_SCHEMA")]
    public string DbSchema { get; set; }

    /// <summary>
    /// 表空间.
    /// </summary>
    [SugarColumn(ColumnName = "F_TABLE_SPACE")]
    public string TableSpace { get; set; }

    /// <summary>
    /// Oracle参数字段.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORACLE_PARAM")]
    public string OracleParam { get; set; }

    /// <summary>
    /// Oracle扩展开关 1:开启 0:关闭.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORACLE_EXTEND")]
    public int? OracleExtend { get; set; }
}