using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI Prompt 模板创建输入
/// </summary>
[SuppressSniffer]
public class AiPromptTemplateCrInput
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// Prompt 模板正文
    /// </summary>
    public string template { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int version { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public int isActive { get; set; }
}
