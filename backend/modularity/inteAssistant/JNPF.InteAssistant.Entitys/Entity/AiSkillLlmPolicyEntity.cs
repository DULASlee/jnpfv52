using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// Skill 级 LLM 调用策略（ai_skill_llm_policy）
/// </summary>
[SugarTable("ai_skill_llm_policy", TableDescription = "Skill LLM策略")]
public class AiSkillLlmPolicyEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SkillId")]
    public string SkillId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_MaxLlmCalls")]
    public int MaxLlmCalls { get; set; } = 3;

    [SugarColumn(ColumnName = "F_MaxTokensPerCall")]
    public int MaxTokensPerCall { get; set; } = 8192;

    [SugarColumn(ColumnName = "F_MaxTotalTokens")]
    public int MaxTotalTokens { get; set; } = 50_000;

    [SugarColumn(ColumnName = "F_ModelTier")]
    public string ModelTier { get; set; } = "strong";

    [SugarColumn(ColumnName = "F_TimeoutMs")]
    public int TimeoutMs { get; set; } = 120_000;

    [SugarColumn(ColumnName = "F_CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
