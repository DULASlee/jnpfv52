using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Entitys.Dto.SysCache;

/// <summary>
/// 缓存列表输入.
/// </summary>
[SuppressSniffer]
public class CacheListInput : KeywordInput
{
    /// <summary>
    /// 过期开始时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? overdueStartTime { get; set; }

    /// <summary>
    /// 过期结束时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? overdueEndTime { get; set; }
}