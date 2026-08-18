namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// 认知技能所处的决策层级（施工包 21 §3.1 骨架层）。
/// </summary>
public enum SkillLayer
{
    /// <summary>决策层——划定业务边界、取舍方向（PM）。</summary>
    Decision = 1,

    /// <summary>精化层——把边界内的模糊需求收敛为可执行规格（Analyst / 设计四技能）。</summary>
    Refinement = 2,

    /// <summary>执行层——按已锁定规格产出制品（Developer / Tester / Deploy）。</summary>
    Execution = 3,
}

/// <summary>
/// 认知技能的使命类型——一个技能只承担一种使命。
/// </summary>
public enum SkillMission
{
    /// <summary>定义边界：从原始需求中裁决“做什么/不做什么”。</summary>
    DefineBoundary = 1,

    /// <summary>精化规格：填补不确定性槽位，产出可验证契约。</summary>
    RefineSpecification = 2,

    /// <summary>生成制品：代码 / DDL / 测试套件 / 部署单元。</summary>
    GenerateArtifact = 3,

    /// <summary>诊断修复：定位根因并给出最小修复（Bugfix）。</summary>
    DiagnoseAndRepair = 4,
}
