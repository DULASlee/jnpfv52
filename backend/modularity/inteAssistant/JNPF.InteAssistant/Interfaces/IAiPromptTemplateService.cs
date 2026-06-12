using JNPF.InteAssistant.Entitys.Dto.InteAssistant;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// AI Prompt 模板服务接口
/// </summary>
public interface IAiPromptTemplateService
{
    /// <summary>
    /// 获取模板列表
    /// </summary>
    Task<dynamic> GetList(AiPromptTemplateListQueryInput input);

    /// <summary>
    /// 获取模板详情
    /// </summary>
    Task<dynamic> GetInfo(string id);

    /// <summary>
    /// 按分类获取模板列表（不分页）
    /// </summary>
    Task<List<AiPromptTemplateListOutput>> GetByCategory(string category);

    /// <summary>
    /// 按名称获取当前激活模板
    /// </summary>
    Task<dynamic> GetActiveByName(string name);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<dynamic> Create(AiPromptTemplateCrInput input);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task<dynamic> Update(string id, AiPromptTemplateUpInput input);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task Delete(string id);
}
