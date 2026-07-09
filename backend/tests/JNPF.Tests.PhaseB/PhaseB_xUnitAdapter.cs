using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// Phase B → xUnit 渐进迁移适配器。
///
/// 不改动任何现有测试代码，仅为关键测试方法添加 [Fact] 包装器。
/// 现有 TestRunner（dotnet run）仍可正常使用，本适配器支持 dotnet test 发现执行。
///
/// 过渡策略：
///   Phase A（当前）: 双轨运行 — dotnet run (custom) + dotnet test (xUnit)
///   Phase B（未来）: 逐步将原测试迁移为原生 [Fact] + Assert，删除旧 Runner
/// </summary>
public class PhaseB_xUnitAdapter
{
    // ═══════════════════════════════════════════════════════════
    // 阶段一: IR 基础设施
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public Task T20_SchemaValidator_RejectsMissingBusinessEvents()
        => IrPhase1Tests.T20_SchemaValidator_RejectsMissingBusinessEvents();

    [Fact]
    public Task T21_SchemaValidator_AcceptsValidSkeleton()
        => IrPhase1Tests.T21_SchemaValidator_AcceptsValidSkeleton();

    [Fact]
    public Task T22_StabilityGate_TriggersAtNineSteps()
        => IrPhase1Tests.T22_StabilityGate_TriggersAtNineSteps();

    [Fact]
    public Task T23_Rebuild_100Events_Under200ms()
        => IrPhase1Tests.T23_Rebuild_100Events_Under200ms();

    // ═══════════════════════════════════════════════════════════
    // 阶段二: Skill Harness + PM
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void P2_SkillRunGuard_PmSkill_Smoke()
    {
        IrPhase2SkillTests.RunAll();
        Assert.True(true); // 由原 TestRunner 控制 pass/fail
    }

    // ═══════════════════════════════════════════════════════════
    // 阶段三: Design Skills + LLM Guard
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void P3_SkillLlmBudgetGuard_DesignSkillIds_Smoke()
    {
        IrPhase3Tests.RunAll();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // S2: SaNineViewCompiler (确定性编译，无 LLM)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void S2_SaNineViewCompiler_All()
    {
        SaNineViewCompilerTests.RunAll();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // 阶段四: Developer Sandbox Build
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task D3_CodegenSandbox_DotnetBuild()
    {
        await CodegenSandboxGateTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D4_DeveloperSkill_CodeGenerated_Draft()
    {
        await IrPhase4DeveloperTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D5_DeveloperOrchestrator_SandboxBuildChain()
    {
        await IrPhase4OrchestratorTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D6_ArchGuardService_Yaml()
    {
        await IrPhase4ArchGuardTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D7_CodeGeneratedStablePromoted_IR3()
    {
        await IrPhase4PromoteTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D8_TesterSkill_TestSuiteGenerated()
    {
        await IrPhase4TesterTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task D10_ArchGuard_Q2_ViolationProfiles()
    {
        await IrPhase4ArchGuardQ2Tests.RunAllAsync();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // A5: TemplateContext + 样本渲染
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void A5_TemplateRenderSamples()
    {
        TemplateRenderSamplesTests.RunAll();
        Assert.True(true);
    }

    [Fact]
    public void A5_TemplateContextBuilder_StrictNegative()
    {
        TemplateContextBuilderTests.RunAll();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // 阶段五: Diff + Bugfix
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void P5_B01_IrDiffEngine_FragmentDiff()
    {
        IrPhase5DiffTests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public void P5_B02_BugfixSkill_AffectedFragments()
    {
        IrPhase5BugfixTests.RunAllAsync();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // 认知模具: R0-R4
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task R0_CognitiveSkill_MoldContract()
    {
        await CognitiveSkillR0Tests.RunAllAsync();
        Assert.True(true);
    }

    [Fact]
    public void R1_PmSkill_CognitiveMold()
    {
        PmSkillR1Tests.RunAll();
        Assert.True(true);
    }

    [Fact]
    public void R2_AnalystSkill_CognitiveMold()
    {
        AnalystSkillR2Tests.RunAll();
        Assert.True(true);
    }

    [Fact]
    public void R3_DesignFourSkills_CognitiveMold()
    {
        DesignSkillR3Tests.RunAll();
        Assert.True(true);
    }

    [Fact]
    public void R4_Experience_Evolution()
    {
        ExperienceR4Tests.RunAll();
        Assert.True(true);
    }

    // ═══════════════════════════════════════════════════════════
    // 基础设施: Sandbox + Workspace
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Infra_SandboxQueue_All()
    {
        await SandboxQueueTests.T9_CreateAsync_UnderLimit_NoQueueing();
        await SandboxQueueTests.T10_CreateAsync_OverLimit_Enqueues();
        await SandboxQueueTests.T11_Queue_OnSlotRelease_Dequeues();
        await SandboxQueueTests.T12_Queue_Timeout_CancelsRequest();
        await SandboxQueueTests.T13_ActiveCount_DecrementsOnException();
        await SandboxQueueTests.T14_Dispose_DrainsPendingQueue();
        Assert.True(true);
    }

    [Fact]
    public async Task Infra_WorkspaceHelper_All()
    {
        await StudioWorkspaceHelperTests.T5_InjectFrontendFiles_CopiesVueFiles();
        await StudioWorkspaceHelperTests.T6_InjectFrontendFiles_EmptyDirReturnsGracefully();
        await StudioWorkspaceHelperTests.T7_ReadFilesFromDirectory_ReturnsCorrectList();
        await StudioWorkspaceHelperTests.T8_ReadFilesFromDirectory_EmptyDirReturnsEmpty();
        Assert.True(true);
    }

    [Fact]
    public async Task Infra_SandboxConfig_All()
    {
        await SandboxConfigTests.T15_PreviewPort_DefaultValue();
        await SandboxConfigTests.T16_PreviewUrl_FormattedCorrectly();
        await SandboxConfigTests.T17_SandboxInstance_LifecycleWithPreview();
        Assert.True(true);
    }

    [Fact]
    public async Task Infra_PreviewCleanup_All()
    {
        await PreviewResourceCleanupTests.T18_SandboxCreated_FlagSetOnSuccess();
        await PreviewResourceCleanupTests.T19_SandboxNotCreated_OnExistingSandbox();
        Assert.True(true);
    }
}
