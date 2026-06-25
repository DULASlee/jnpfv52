using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// AI Prompt 模板详情输出
/// </summary>
[SuppressSniffer]
public class AiPromptTemplateInfoOutput
{
    /// <summary>
    /// 主键
    /// </summary>
    public string id { get; set; }

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

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime? lastModifyTime { get; set; }
}
