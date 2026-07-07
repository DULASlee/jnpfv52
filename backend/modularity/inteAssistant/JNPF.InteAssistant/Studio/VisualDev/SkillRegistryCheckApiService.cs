using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant.Studio.VisualDev;

/// <summary>
/// P8-R01 Skill Registry 完整性验证 API。
/// 验证 10 个 Skill（+ 1 bonus clarification）全部注册可 GetRequired。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "SkillRegistryCheck", Order = 206)]
[Route("api/studio/registry-check")]
public class SkillRegistryCheckApiService : IDynamicApiController, ITransient
{
    /// <summary>10 个核心 Skill + 1 bonus clarification 的预期清单</summary>
    private static readonly string[] ExpectedSkillIds =
    {
        "pm-skill",
        "analyst-skill",
        DesignSkillIds.Architect,           // architect-skill
        DesignSkillIds.DbDesign,            // db-design-skill
        DesignSkillIds.UiDesign,            // ui-design-skill
        DesignSkillIds.SystemDesign,        // system-design-skill
        DevelopmentSkillIds.Developer,      // developer-skill
        DevelopmentSkillIds.Tester,         // tester-skill
        DeploySkillIds.Deploy,              // deploy-skill
        BugfixSkillIds.Bugfix,              // bugfix-skill
        DesignSkillIds.SystemDesignClarification,  // bonus
    };

    private readonly ISkillRegistry _registry;

    public SkillRegistryCheckApiService(ISkillRegistry registry) => _registry = registry;

    /// <summary>
    /// GET /api/studio/skills/registry-check — 验证所有预期 Skill 已注册。
    /// 返回每个 skill 的注册状态 + 已注册总数。
    /// </summary>
    [HttpGet]
    public object Check()
    {
        var registered = _registry.SkillIds.ToHashSet();
        var items = ExpectedSkillIds.Select(id => new
        {
            skillId = id,
            registered = registered.Contains(id),
        }).ToList();

        // 实际注册但不在预期清单的（如未来新增）
        var extra = registered.Except(ExpectedSkillIds).ToList();

        return new
        {
            expectedCount = ExpectedSkillIds.Length,
            registeredCount = registered.Count,
            allExpectedRegistered = items.All(x => x.registered),
            items,
            extra,
        };
    }
}
