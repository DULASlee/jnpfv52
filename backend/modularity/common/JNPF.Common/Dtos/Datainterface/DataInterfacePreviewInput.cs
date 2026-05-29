using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.Datainterface;

/// <summary>
/// 数据接口预览输入.
/// </summary>
[SuppressSniffer]
public class DataInterfacePreviewInput : PageInputBase
{
    /// <summary>
    /// 租户id.
    /// </summary>
    public string? tenantId { get; set; }

    /// <summary>
    /// 查询 字段名.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 弹窗选中 值.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 弹窗选中 字段名.
    /// </summary>
    public string propsValue { get; set; }

    /// <summary>
    /// 设定显示的所有列  以 , 号隔开.
    /// </summary>
    public string columnOptions { get; set; }

    /// <summary>
    /// 弹窗选中 值.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();

    /// <summary>
    /// 关联表单数据源.
    /// </summary>
    public object sourceData { get; set; }

    /// <summary>
    /// 强制分页.
    /// </summary>
    public bool coercePage { get; set; }

    /// <summary>
    /// 系统变量参数.
    /// </summary>
    public Dictionary<string, object> systemParamter { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 预览参数.
    /// </summary>
    public List<DataInterfaceParameter> paramList { get; set; } = new List<DataInterfaceParameter>();

    /// <summary>
    /// 数据接口参数.
    /// </summary>
    public Dictionary<string, string> dicParameters { get; set; } = new Dictionary<string, string>();

    public object formdata;
}
