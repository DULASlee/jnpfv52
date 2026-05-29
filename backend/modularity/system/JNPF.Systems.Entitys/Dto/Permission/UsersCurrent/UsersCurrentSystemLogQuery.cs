using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 当前用户系统日记查询输入.
/// </summary>
[SuppressSniffer]
public class UsersCurrentSystemLogQuery : PageInputBase
{
    /// <summary>
    /// 日记类型.
    /// </summary>
    public int category { get; set; }

    /// <summary>
    /// 日记类型.
    /// </summary>
    public int? loginType { get; set; }

    /// <summary>
    /// 日记类型.
    /// </summary>
    public int? loginMark { get; set; }

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
}