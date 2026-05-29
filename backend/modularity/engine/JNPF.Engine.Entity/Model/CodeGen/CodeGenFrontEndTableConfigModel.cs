using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成表格配置模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndTableConfigModel
{
    /// <summary>
    /// 是否开启高级查询.
    /// </summary>
    public bool HasSuperQuery { get; set; }

    /// <summary>
    /// 是否开启分页.
    /// </summary>
    public bool HasPage { get; set; }

    /// <summary>
    /// 分页条数.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 合计配置.
    /// </summary>
    public bool ShowSummary { get; set; }

    /// <summary>
    /// 合计字段.
    /// </summary>
    public string SummaryField { get; set; }

    /// <summary>
    /// 分组字段.
    /// </summary>
    public string GroupField { get; set; }

    /// <summary>
    /// 子表样式
    /// 1-分组展示,2-折叠展示.
    /// </summary>
    public int ChildTableStyle { get; set; }

    /// <summary>
    /// 排序类型.
    /// </summary>
    public string Sort { get; set; }

    /// <summary>
    /// 排序字段.
    /// </summary>
    public string Sidx { get; set; }

    /// <summary>
    /// 溢出省略.
    /// </summary>
    public bool ShowOverflow { get; set; }

    /// <summary>
    /// 默认排序.
    /// </summary>
    public string DefaultSortConfig { get; set; }

}