using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Entitys.Dto.SysLog;

/// <summary>
/// 系统日志列表入参.
/// </summary>
[SuppressSniffer]
public class LogListQuery : PageInputBase
{
    /// <summary>
    /// 开始时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? endTime { get; set; }

    /// <summary>
    /// IP地址.
    /// </summary>
    public string ipAddress { get; set; }

    /// <summary>
    /// 用户.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public int? category { get; set; }

    /// <summary>
    /// 请求方式.
    /// </summary>
    public string requestMethod { get; set; }

    /// <summary>
    /// 登录类型.
    /// </summary>
    public int? loginType { get; set; }

    /// <summary>
    /// 是否登录成功标志.
    /// </summary>
    public int? loginMark { get; set; }
}