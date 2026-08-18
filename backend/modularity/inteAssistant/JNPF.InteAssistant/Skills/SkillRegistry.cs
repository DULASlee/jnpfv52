using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.InteAssistant.Skills;

public interface ISkillRegistry
{
    IReadOnlyCollection<string> SkillIds { get; }
    IBaseSkill GetRequired(string skillId);
    bool TryGet(string skillId, out IBaseSkill? skill);
}

public sealed class SkillRegistry : ISkillRegistry, ISingleton
{
    private readonly IReadOnlyDictionary<string, IBaseSkill> _skills;

    public SkillRegistry(IServiceProvider serviceProvider)
    {
        _skills = serviceProvider.GetServices<IBaseSkill>()
            .GroupBy(s => s.SkillId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> SkillIds => _skills.Keys.ToList();

    public IBaseSkill GetRequired(string skillId)
    {
        if (_skills.TryGetValue(skillId, out var skill))
            return skill;
        throw Oops.Bah($"未注册的 Skill: {skillId}");
    }

    public bool TryGet(string skillId, out IBaseSkill? skill)
        => _skills.TryGetValue(skillId, out skill);
}
