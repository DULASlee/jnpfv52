using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI Prompt 模板存储
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_AI_PROMPT_TEMPLATE", TableDescription = "AI Prompt模板")]
public class AiPromptTemplateEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnName = "F_NAME")]
    public string Name { get; set; }

    /// <summary>
    /// 模板分类
    /// form / dashboard / workflow / code
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// Prompt 模板正文
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE")]
    public string Template { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public int Version { get; set; }

    /// <summary>
    /// 是否激活 (1-激活, 0-未激活)
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_ACTIVE")]
    public int IsActive { get; set; }
}
