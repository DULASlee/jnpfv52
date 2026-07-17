namespace JNPF.InteAssistant.Entitys.Ir;

/// <summary>
/// 阶段一 IR 事件类型常量
/// </summary>
public static class IrEventTypes
{
    public const string ProjectCreated = "ProjectCreated";
    public const string StageConfirmed = "StageConfirmed";
    public const string SkeletonCreated = "SkeletonCreated";
    public const string SaStepCompleted = "SA_Step_Completed";
    public const string EventSpecRevised = "EventSpecRevised";
    public const string FragmentStabilized = "FragmentStabilized";
    public const string EventSpecConfirmed = "EventSpecConfirmed";
    public const string AnalysisCompleted = "AnalysisCompleted";
    public const string FragmentInvalidated = "FragmentInvalidated";
    public const string InferredRulesAcknowledged = "InferredRulesAcknowledged";

    // 31 号 P0：PM 专家闭环（终评 / Amend 回显确认）
    public const string RequirementSpecPmReviewed = "RequirementSpecPmReviewed";
    public const string RequirementAmendmentProposed = "RequirementAmendmentProposed";
    public const string RequirementAmendmentApplied = "RequirementAmendmentApplied";

    // ── 交互式澄清问答事件（跨阶段：需求分析 / 架构设计 / 总体设计，ADR-005）──
    // ClarificationRequested：LLM 产出结构化选择题后投递，fragment 进入 in-progress，暂停流程等待用户作答
    public const string ClarificationRequested = "ClarificationRequested";
    // ClarificationAnswered：用户作答（required 题齐全）后投递，fragment 进入 stable，恢复流程
    public const string ClarificationAnswered = "ClarificationAnswered";

    /// <summary>
    /// ADR-005 P3：总体设计澄清完成。SystemDesignClarificationSkill 阶段二产出，
    /// 携带用户作答汇总（answersText），作为 SystemDesignLocked payload 的 assumptions 来源。
    /// 不驱动 fragment 状态（仅留痕），由 SystemDesignClarificationCompleted 事件本身存在性判断。
    /// </summary>
    public const string SystemDesignClarificationCompleted = "SystemDesignClarificationCompleted";

    // 阶段三 IR-2 设计事件
    public const string ArchitectureDecisionRecorded = "ArchitectureDecisionRecorded";
    public const string DDLStabilized = "DDLStabilized";
    public const string UIDesignStabilized = "UIDesignStabilized";
    public const string SystemDesignLocked = "SystemDesignLocked";
    public const string ConstraintViolationReported = "ConstraintViolationReported";
    public const string DesignSkillCompleted = "DesignSkillCompleted";

    // 阶段四 IR-3 开发事件
    public const string CodeGenerated = "CodeGenerated";
    public const string CodegenFailed = "CodegenFailed";
    public const string CodegenBuildValidated = "CodegenBuildValidated";
    public const string CodeGeneratedStablePromoted = "CodeGeneratedStablePromoted";
    public const string ArchViolationDetected = "ArchViolationDetected";
    public const string DeveloperSkillCompleted = "DeveloperSkillCompleted";
    public const string TestSuiteGenerated = "TestSuiteGenerated";
    public const string TesterSkillCompleted = "TesterSkillCompleted";

    // 阶段五 Bugfix / Deploy 事件
    public const string BugReported = "BugReported";
    public const string BugRootCauseLocated = "BugRootCauseLocated";
    public const string AffectedFragmentsMarked = "AffectedFragmentsMarked";
    public const string BugFixed = "BugFixed";
    public const string DeploymentVerified = "DeploymentVerified";
    public const string DeploymentFailed = "DeploymentFailed";

    // CognitiveSkill 进化层经验事件（施工包 21 §3.4，投影引擎 default→null 天然兼容）
    public const string SkillReviewRecorded = "SkillReviewRecorded";
    public const string SkillFailureRecorded = "SkillFailureRecorded";
    public const string HumanCorrectionRecorded = "HumanCorrectionRecorded";

    /// <summary>P6-L01：项目 Token Budget 四级降级 tier 切换审计事件。</summary>
    public const string BudgetTierChanged = "BudgetTierChanged";

    /// <summary>S2 Compiler 九步视图编译完成（payload 含 bundleHash）。</summary>
    public const string SaNineViewCompiled = "SaNineViewCompiled";

    /// <summary>跨轮 Assumptions 收集（编排器 Round 1/2 写入，Round 3 Finalize 合并）。</summary>
    public const string AssumptionsCollected = "AssumptionsCollected";

    /// <summary>用户确认后 SA 九表物化完成。</summary>
    public const string SaMaterializationCompleted = "SaMaterializationCompleted";

    /// <summary>SA 九表物化失败。</summary>
    public const string SaMaterializationFailed = "SaMaterializationFailed";

    // ── CR-20260713-03：PM 核心流程重构 — 4 步线性（回归"完善需求"初衷）──
    // 步骤①：PM 用提示词+种子完善需求文本（对话式追问 0~N 次后产出）
    public const string RequirementEnhanced = "RequirementEnhanced";
    // 步骤③：PM 分析九步数据反向完善需求文本（可重跑步骤②）
    public const string RequirementRefined = "RequirementRefined";
    // 步骤④：渲染需求分析说明书（文本+九步表引用），等用户确认
    public const string RequirementSpecRendered = "RequirementSpecRendered";
    // CR-20260714-01 步骤⑤：用户确认需求说明书 → PM 终评 + Analyst Finalize → 进入架构设计
    public const string RequirementSpecConfirmed = "RequirementSpecConfirmed";
}

public static class IrFragmentTypes
{
    public const string Skeleton = "IR0_Skeleton";
    public const string EventSpec = "IR1_EventSpec";

    /// <summary>跨轮 Assumptions 持久化（暂停恢复后可重建）。fragmentId = assumptions:{projectId}</summary>
    public const string Assumptions = "IR1_Assumptions";

    // 澄清问答片段（ADR-005）：stage 区分需求/设计阶段，fragmentId 形如 clarification:{stage}:{projectId}
    public const string Clarification = "IR1_Clarification";

    public const string Architecture = "IR2_Architecture";
    public const string DDL = "IR2_DDL";
    public const string FormPageIR = "IR2_FormPageIR";
    public const string SystemDesign = "IR2_SystemDesign";

    public const string GeneratedCode = "IR3_GeneratedCode";
    public const string ArchReport = "IR3_ArchReport";
    public const string TestSuite = "IR3_TestSuite";

    // ── CR-20260713-03：PM 核心流程重构 — 需求文本迭代 fragment ──
    // 步骤①完善后的需求文本；步骤③二次完善覆盖此 fragment。fragmentId = requirement:{projectId}
    public const string Requirement = "IR0_Requirement";
}

/// <summary>澄清问答阶段标识（ClarificationSet.stage 取值）。</summary>
public static class ClarificationStages
{
    public const string Requirement = "requirement";
    public const string Architecture = "architecture";
    public const string SystemDesign = "system-design";
}

public static class DesignSkillIds
{
    public const string Architect = "architect-skill";
    public const string DbDesign = "db-design-skill";
    public const string UiDesign = "ui-design-skill";
    public const string SystemDesign = "system-design-skill";
    /// <summary>ADR-005 P3：总体设计澄清提问 Skill（两阶段，提问后自包含跑约束引擎）。</summary>
    public const string SystemDesignClarification = "system-design-clarification-skill";
}

public static class DevelopmentSkillIds
{
    public const string Developer = "developer-skill";
    public const string Tester = "tester-skill";
}

public static class BugfixSkillIds
{
    public const string Bugfix = "bugfix-skill";
}

public static class DeploySkillIds
{
    public const string Deploy = "deploy-skill";
}

public static class IrStabilityStates
{
    public const string Draft = "draft";
    public const string InProgress = "in-progress";
    public const string Stable = "stable";
    public const string Locked = "locked";
    public const string Invalidated = "invalidated";
}

public static class IrSaSteps
{
    public static readonly string[] All =
    {
        "DomainModel",
        "AggregateDesign",
        "EventCatalog",
        "CommandQuery",
        "IntegrationPoints",
        "WorkflowSpec",
        "UISpec",
        "DataModel",
        "DeliveryChecklist",
    };
}
