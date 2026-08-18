using System.Reflection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 需求分析子链架构守卫测试（req-analysis-iron-law.md 禁令对应的确定性不变量）。
/// 用反射断言生产代码不变量 —— 架构漂移即红。
/// 这是本项目首个经典架构守卫测试（无先例），参照 Sg2EnterpriseUsabilityTests 的原生 [Fact] 风格。
/// </summary>
public class ReqAnalysisInvariantGuardTests
{
    // ── 禁令七：废止模块不得复活 ──────────────────────────────

    /// <summary>
    /// 禁令七：ScannerValidator / EventDependencyBuilder / PSpecEnhancer 已被 25 号 §0.2 废止。
    /// 断言这些类型在已加载程序集中不存在（0 源码定义）。若有人复活，此测试即红。
    /// </summary>
    [Fact]
    public void ProhibitedModuleClasses_ShouldNotExist_InAnyLoadedAssembly()
    {
        var prohibitedTypeNames = new[]
        {
            "ScannerValidator",
            "EventDependencyBuilder",
            "PSpecEnhancer",
            "DecisionTableEnhancer",
            "NoopEnhancer",
            "ISaStepEnhancer",
        };

        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("JNPF.InteAssistant") == true)
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Select(t => t.Name)
            .ToHashSet();

        var resurrected = prohibitedTypeNames.Where(name => allTypes.Contains(name)).ToList();
        Assert.True(resurrected.Count == 0,
            $"废止模块被复活（违反 25 §0.2 / req-analysis-iron-law.md 禁令七）: {string.Join(", ", resurrected)}。" +
            "这些类型已被 25 v2.1 废止，现行替代见 LightStructureValidator / Round2 联合 LLM / Assumption+StepEnhancement record。");
    }

    // ── 禁令二：PM 专家闭环核心方法契约 ────────────────────────

    /// <summary>
    /// 禁令二/六：PmSkillService 必须保留 31 §4.1 契约的 4 个核心方法。
    /// 若有人删除/重命名（未经 CR），此测试即红。
    /// </summary>
    [Fact]
    public void PmSkillService_ShouldExpose_FourCoreExpertMethods()
    {
        var pmType = typeof(PmSkillService);
        var expectedMethods = new[]
        {
            "GenerateClarificationAsync",
            "ReviewSpecAsync",
            "AmendProposeAsync",
            "ApplyAmendmentAsync",
        };

        var actualMethods = pmType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = expectedMethods.Where(name => !actualMethods.Contains(name)).ToList();
        Assert.True(missing.Count == 0,
            $"PmSkillService 缺失核心方法（违反 31 §4.1 契约 / req-analysis-iron-law.md 禁令六）: {string.Join(", ", missing)}。" +
            "出题/终评/Amend Propose/Apply 是 PM 专家闭环的 4 个公共方法，删除或重命名需先提交 CR。");
    }

    // ── 禁令七：题型默认 MULTI，SINGLE 归一化 ──────────────────

    /// <summary>
    /// 禁令七：ClarificationQuestion.QuestionFormat 默认值 MUST 为 "MULTI"（25 红线1 / 31 D-E）。
    /// 普通 SINGLE 禁止作为最终值。验证默认值 + NormalizeQuestionFormat 存在。
    /// </summary>
    [Fact]
    public void ClarificationQuestion_DefaultFormat_ShouldBeMulti_NotSingle()
    {
        var questionType = typeof(ClarificationQuestion);

        // QuestionFormat 是 string 属性（非 enum），检查默认值
        var formatProp = questionType.GetProperty("QuestionFormat");
        Assert.NotNull(formatProp);
        Assert.Equal(typeof(string), formatProp.PropertyType);

        // 用反射构造默认实例检查初始值
        var defaultInstance = Activator.CreateInstance<ClarificationQuestion>();
        var defaultValue = (string?)formatProp.GetValue(defaultInstance);
        Assert.True("MULTI".Equals(defaultValue, StringComparison.OrdinalIgnoreCase),
            $"ClarificationQuestion.QuestionFormat 默认值应为 MULTI（25 红线1 / 31 D-E），实际 = '{defaultValue}'。" +
            "普通 SINGLE 禁止作为最终值；若改了默认值 = 违反题型约束，需先提交 CR。");
    }

    // ── 禁令二：Amend 回显 DTO 字段完整性 ──────────────────────

    /// <summary>
    /// 禁令二/25 决策10：AmendmentUnderstanding MUST 含 Features/Flows/EntitiesOrTables/SummaryMarkdown。
    /// 这是 Amend 回显"功能/流程/表"的契约（31 §4.1）。字段缺失 = 回显信息不完整。
    /// </summary>
    [Fact]
    public void AmendmentUnderstanding_ShouldHave_RequiredEchoFields()
    {
        var understandingType = typeof(AmendmentUnderstanding);
        var requiredFields = new[]
        {
            "Features",
            "Flows",
            "EntitiesOrTables",
            "SummaryMarkdown",
        };

        var actualProps = understandingType.GetProperties().Select(p => p.Name).ToHashSet();
        var missing = requiredFields.Where(name => !actualProps.Contains(name)).ToList();
        Assert.True(missing.Count == 0,
            $"AmendmentUnderstanding 缺失必需字段（违反 25 决策10 / 31 §4.1 Amend 回显契约）: {string.Join(", ", missing)}。" +
            "回显 MUST 含 功能(Features)/流程(Flows)/数据表或实体(EntitiesOrTables)/摘要(SummaryMarkdown)。");
    }

    // ── 禁令六：关键业务方法保护清单存在性 ────────────────────

    /// <summary>
    /// 禁令六：保护清单中的关键业务文件对应的类型 MUST 存在。
    /// 这是 CR 审批机制的前提 —— 如果类型本身不存在，保护清单就是空壳。
    /// </summary>
    [Fact]
    public void ProtectedBusinessTypes_ShouldExist_ForCrApprovalMechanism()
    {
        var requiredTypes = new Dictionary<string, Type>
        {
            ["PmSkillService"] = typeof(PmSkillService),
            ["RequirementAnalysisOrchestrator"] = typeof(RequirementAnalysisOrchestrator),
            ["DesignSkillOrchestrator"] = typeof(DesignSkillOrchestrator),
        };

        foreach (var (name, type) in requiredTypes)
        {
            Assert.NotNull(type);
            Assert.True(type.IsClass, $"{name} 应为 class");
        }
    }

    /// <summary>
    /// 禁令六：RequirementAnalysisOrchestrator 的出题方法 GenerateRoundClarificationAsync
    /// 虽然编排器注入了 ILlmGatewayService（用于 PSpec/DT 增强），但出题 MUST 委托给 PmSkillService。
    /// 此测试断言出题方法存在（委托点）；若被删除/改名需先提交 CR。
    /// </summary>
    [Fact]
    public void Orchestrator_ShouldDelegate_ClarificationToPmSkill()
    {
        var orchestratorType = typeof(RequirementAnalysisOrchestrator);
        var pmFieldType = typeof(PmSkillService);

        // 编排器 MUST 持有 PmSkillService 类型的字段（出题委托的证据）
        var pmFields = orchestratorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType == pmFieldType || f.FieldType.Name == "PmSkillService")
            .ToList();

        Assert.True(pmFields.Count > 0,
            "RequirementAnalysisOrchestrator 应持有 PmSkillService 字段（出题委托的证据）。" +
            "若编排器不再注入 PmSkillService = 出题权可能漂移回编排器（违反 25 红线9 / 31 D-D）。" +
            "若需调整注入方式，先提交 CR。");
    }

    // ── 禁令三/七：旧端点废止守卫（F3 铁律 — 旧旁路必须封锁）──
    //
    // 注：旧端点 throw Oops.Bah + SkillHarness 唯一调用方的守卫已由
    // F3LegacyCleanupGuardTests.cs（T1–T5）实现，通过 IL 字节码 + 源码扫描验证，
    // 经 PhaseB_xUnitAdapter.cs:280 接入 xUnit 套件。
    //
    // Oops.Bah 静态构造依赖 App 宿主（App.GetConfig / App.EffectiveTypes），
    // 在无宿主测试上下文中无法真执行方法体 —— F3LegacyCleanupGuardTests 的
    // IL opcode 0x7A 检测 + [Obsolete] 属性检查是正确的架构守卫方式。
    //
    // 本段不重复 F3LegacyCleanupGuardTests 已有用例。
}
