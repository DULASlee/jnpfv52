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
}

public static class IrFragmentTypes
{
    public const string Skeleton = "IR0_Skeleton";
    public const string EventSpec = "IR1_EventSpec";

    public const string Architecture = "IR2_Architecture";
    public const string DDL = "IR2_DDL";
    public const string FormPageIR = "IR2_FormPageIR";
    public const string SystemDesign = "IR2_SystemDesign";

    public const string GeneratedCode = "IR3_GeneratedCode";
    public const string ArchReport = "IR3_ArchReport";
    public const string TestSuite = "IR3_TestSuite";
}

public static class DesignSkillIds
{
    public const string Architect = "architect-skill";
    public const string DbDesign = "db-design-skill";
    public const string UiDesign = "ui-design-skill";
    public const string SystemDesign = "system-design-skill";
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
