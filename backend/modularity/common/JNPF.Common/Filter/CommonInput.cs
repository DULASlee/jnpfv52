namespace JNPF.Common.Filter;

public class CommonInput : PageInputBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public int? flowType { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public virtual string? type { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public virtual int? enabledMark { get; set; }
}
