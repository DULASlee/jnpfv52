using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Enum;

/// <summary>
/// 菜单类型.
/// </summary>
[SuppressSniffer]
public enum MenuType
{
    /// <summary>
    /// 目录.
    /// </summary>
    [Description("目录")]
    Directory = 0,

    /// <summary>
    /// 页面.
    /// </summary>
    [Description("页面")]
    View = 1,

    /// <summary>
    /// 功能.
    /// </summary>
    [Description("功能")]
    Function = 2,

    /// <summary>
    /// 字典.
    /// </summary>
    [Description("字典")]
    Dictionary = 3,

    /// <summary>
    /// 报表.
    /// </summary>
    [Description("报表")]
    Report = 4,

    /// <summary>
    /// 大屏.
    /// </summary>
    [Description("大屏")]
    Screen = 5,

    /// <summary>
    /// 门户.
    /// </summary>
    [Description("门户")]
    Portal = 6,

    /// <summary>
    /// 外链.
    /// </summary>
    [Description("外链")]
    Link = 7,
}