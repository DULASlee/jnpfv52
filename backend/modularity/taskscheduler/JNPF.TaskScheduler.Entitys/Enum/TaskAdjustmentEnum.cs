using System.ComponentModel;

namespace JNPF.TaskScheduler.Entitys.Enum;

/// <summary>
/// 任务调整.
/// </summary>
public enum TaskAdjustmentEnum
{
    /// <summary>
    /// 纯更新.
    /// </summary>
    [Description("纯更新")]
    Update = 0,

    /// <summary>
    /// 纯暂停.
    /// </summary>
    [Description("纯暂停")]
    Suspend = 1,

    /// <summary>
    /// 改内置且暂停.
    /// </summary>
    [Description("改内置且暂停")]
    ChangeBuiltInAndPause = 2,

    /// <summary>
    /// 改HTTP且暂停.
    /// </summary>
    [Description("改HTTP且暂停")]
    ChangeHttpAndPause = 3,

    /// <summary>
    /// 纯开启.
    /// </summary>
    [Description("开启")]
    Open = 4,

    /// <summary>
    /// 开启且新增.
    /// </summary>
    [Description("开启且新增")]
    OpenAndAdd = 5,

    /// <summary>
    /// 改内置且开启.
    /// </summary>
    [Description("改内置且开启")]
    ChangeBuiltInAndOpen = 6,

    /// <summary>
    /// 改HTTP且开启.
    /// </summary>
    [Description("改HTTP且开启")]
    ChangeHttpAndOpen = 7,

    /// <summary>
    /// 改内置.
    /// </summary>
    [Description("改内置")]
    ChangeBuiltIn = 8,

    /// <summary>
    /// 改HTTP.
    /// </summary>
    [Description("改HTTP")]
    ChangeHttp = 9,
}