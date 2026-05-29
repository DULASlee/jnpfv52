using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.Menu;

namespace JNPF.Systems.Entitys.Model.Authorize;

/// <summary>
/// 权限功能模型.
/// </summary>
[SuppressSniffer]
public class AuthorizeModel
{
    /// <summary>
    /// 功能.
    /// </summary>
    public List<FunctionalModel> FunctionList { get; set; }

    /// <summary>
    /// 按钮.
    /// </summary>
    public List<FunctionalButtonModel> ButtonList { get; set; }

    /// <summary>
    /// 视图.
    /// </summary>
    public List<FunctionalViewModel> ColumnList { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public List<FunctionalFormModel> FormList { get; set; }

    /// <summary>
    /// 资源.
    /// </summary>
    public List<FunctionalResourceModel> ResourceList { get; set; }
}