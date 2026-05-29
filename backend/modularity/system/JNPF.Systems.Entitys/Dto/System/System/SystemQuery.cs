using JNPF.Common.Filter;

namespace JNPF.Systems.Entitys.Dto.System.System;

public class SystemQuery : KeywordInput
{
    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}
