using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Common.Enums;

/// <summary>
/// 模块导出文件类型.
/// </summary>
[SuppressSniffer]
public enum ExportFileType
{
    /// <summary>
    ///  默认.
    /// </summary>
    [Description("默认")]
    json,

    /// <summary>
    ///  数据接口.
    /// </summary>
    [Description("数据接口")]
    bd,

    /// <summary>
    ///  单据规则.
    /// </summary>
    [Description("单据规则")]
    bb,

    /// <summary>
    ///  菜单.
    /// </summary>
    [Description("菜单")]
    bm,

    /// <summary>
    ///  数据建模.
    /// </summary>
    [Description("数据建模")]
    bdb,

    /// <summary>
    ///  数据字典.
    /// </summary>
    [Description("数据字典")]
    bdd,

    /// <summary>
    ///  打印模板.
    /// </summary>
    [Description("打印模板")]
    bp,

    /// <summary>
    ///  大屏导出.
    /// </summary>
    [Description("大屏导出")]
    vd,

    /// <summary>
    ///  在线开发.
    /// </summary>
    [Description("在线开发")]
    vdd,

    /// <summary>
    ///  APP导出.
    /// </summary>
    [Description("APP导出")]
    va,

    /// <summary>
    ///  门户导出.
    /// </summary>
    [Description("门户导出")]
    vp,

    /// <summary>
    ///  流程设计.
    /// </summary>
    [Description("流程设计")]
    ffe,

    /// <summary>
    ///  表单设计.
    /// </summary>
    [Description("流程设计")]
    fff,

    /// <summary>
    ///  数据接口变量.
    /// </summary>
    [Description("数据接口变量")]
    ffa,

    /// <summary>
    ///  集成助手.
    /// </summary>
    [Description("集成助手")]
    bi,
}