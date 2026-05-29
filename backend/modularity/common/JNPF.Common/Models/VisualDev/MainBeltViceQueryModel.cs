using JNPF.DependencyInjection;

namespace JNPF.Common.Models.VisualDev;

/// <summary>
/// 主带副查询模型.
/// </summary>
[SuppressSniffer]
public class MainBeltViceQueryModel
{
    /// <summary>
    /// 查询列表.
    /// </summary>
    public List<ListSearchParametersModel> searchList { get; set; }

    /// <summary>
    /// 排序规则.
    /// </summary>
    public string sort { get; set; }

    /// <summary>
    /// 默认排序字段.
    /// </summary>
    public string defaultSidx { get; set; }
}

/// <summary>
/// 列表查询参数.
/// </summary>
[SuppressSniffer]
public class ListSearchParametersModel
{
    /// <summary>
    /// 控件Key.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 时间格式化.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 控件是否多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// 查询类型.
    /// </summary>
    public int searchType { get; set; }

    /// <summary>
    /// 参数名称.
    /// </summary>
    public string vModel { get; set; }
}