using JNPF.FriendlyException;
using JNPF.InteAssistant.Skills;
using Microsoft.AspNetCore.Http;

namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// Skill 失败经验 errorKind 分类（施工包 21 R4）。
/// </summary>
public static class SkillExperienceClassifier
{
    public static string Classify(Exception ex) => ex switch
    {
        OperationCanceledException => "cancelled",
        AbortSkillChainException => "aborted",
        AppFriendlyException friendly when friendly.StatusCode == StatusCodes.Status409Conflict => "conflict",
        AppFriendlyException => "business",
        _ => ex.GetType().Name,
    };
}
