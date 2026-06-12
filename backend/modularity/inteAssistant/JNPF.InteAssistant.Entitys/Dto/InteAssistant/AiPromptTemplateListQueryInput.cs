using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI Prompt 模板列表查询输入
/// </summary>
[SuppressSniffer]
public class AiPromptTemplateListQueryInput : PageInputBase
{
    /// <summary>
    /// 模板分类筛选
    /// form / dashboard / workflow / code
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 是否激活 (1/0)
    /// </summary>
    public int? isActive { get; set; }

    /// <summary>
    /// 模板名称模糊搜索
    /// </summary>
    public string? name { get; set; }
}
