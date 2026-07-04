using System.Reflection;
using static JNPF.Tests.PhaseB.StudioWorkspaceHelperTests;
using static JNPF.Tests.PhaseB.SandboxQueueTests;
using static JNPF.Tests.PhaseB.SandboxConfigTests;
using static JNPF.Tests.PhaseB.PreviewResourceCleanupTests;
using static JNPF.Tests.PhaseB.IrPhase1Tests;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// Phase B 单元测试入口 — 覆盖 B1 预览资源清理 + B2 队列逻辑 + 路径工具类.
/// 使用自建轻量测试框架（与 Phase6 模式一致），不依赖 Moq/xUnit.
/// </summary>
public static class TestRunner
{
    static int _passed;
    static int _failed;

    public static async Task<int> Main()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  Phase B — Unit Tests");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            // ── 工作区 1: StudioWorkspaceHelper (不依赖 App 配置) ──
            await T5_InjectFrontendFiles_CopiesVueFiles();
            await T6_InjectFrontendFiles_EmptyDirReturnsGracefully();
            await T7_ReadFilesFromDirectory_ReturnsCorrectList();
            await T8_ReadFilesFromDirectory_EmptyDirReturnsEmpty();

            // ── 工作区 2: SandboxManager 队列逻辑 ──
            await T9_CreateAsync_UnderLimit_NoQueueing();
            await T10_CreateAsync_OverLimit_Enqueues();
            await T11_Queue_OnSlotRelease_Dequeues();
            await T12_Queue_Timeout_CancelsRequest();
            await T13_ActiveCount_DecrementsOnException();
            await T14_Dispose_DrainsPendingQueue();

            // ── 工作区 3: SandboxConfig/Info 扩展 ──
            await T15_PreviewPort_DefaultValue();
            await T16_PreviewUrl_FormattedCorrectly();
            await T17_SandboxInstance_LifecycleWithPreview();

            // ── 工作区 4: 资源清理逻辑 ──
            await T18_SandboxCreated_FlagSetOnSuccess();
            await T19_SandboxNotCreated_OnExistingSandbox();

            // ── 工作区 5: 阶段一 IR 基础设施 ──
            await T20_SchemaValidator_RejectsMissingBusinessEvents();
            await T21_SchemaValidator_AcceptsValidSkeleton();
            await T22_StabilityGate_TriggersAtNineSteps();
            await T23_Rebuild_100Events_Under200ms();

            // ── 工作区 6: 阶段二 Skill Harness ──
            IrPhase2SkillTests.RunAll();
            Pass("P2 SkillRunGuard + PmSkill smoke");

            // ── 工作区 7: 阶段三 Design Skills + LLM Guard ──
            IrPhase3Tests.RunAll();
            Pass("P3 SkillLlmBudgetGuard + DesignSkillIds smoke");

            // ── 工作区 8: 阶段四 A5 TemplateContext + 样本渲染 ──
            TemplateRenderSamplesTests.RunAll();
            Pass("A5 Ir2CodegenContext + 3 leave IR-2 render samples");

            TemplateContextBuilderTests.RunAll();
            Pass("A5 TemplateContextBuilder strict negative tests");

            // ── 工作区 9: 阶段四 D3 sandbox dotnet build ──
            await CodegenSandboxGateTests.RunAllAsync();
            Pass("D3 leave-simple sandbox dotnet build");

            // ── 工作区 10: 阶段四 D4 DeveloperSkill ──
            await IrPhase4DeveloperTests.RunAllAsync();
            Pass("D4 DeveloperSkillService + CodeGenerated draft");

            await IrPhase4OrchestratorTests.RunAllAsync();
            Pass("D5 DeveloperSkillOrchestrator + sandbox build chain");

            await IrPhase4ArchGuardTests.RunAllAsync();
            Pass("D6 ArchGuardService + yaml AG-000～003");

            await IrPhase4PromoteTests.RunAllAsync();
            Pass("D7 CodeGeneratedStablePromoted + IR3 promote stable");

            await IrPhase4TesterTests.RunAllAsync();
            Pass("D8 TesterSkillService + TestSuiteGenerated + IR3_TestSuite");

            await IrPhase4ArchGuardQ2Tests.RunAllAsync();
            Pass("D10 ArchGuard Q2 violation profiles (ag001/ag002)");

            await CodegenHostDemoTests.RunAllAsync();
            Pass("D11-D12 codegen-host-demo inject (full build: phase4-d11-host-build.mjs)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] 未捕获异常: {ex}");
            _failed++;
        }

        Console.WriteLine();
        Console.WriteLine($"  Phase B 测试结果: {_passed} 通过, {_failed} 失败");
        Console.WriteLine($"  总计: {_passed + _failed} 用例");
        Console.WriteLine();

        return _failed > 0 ? 1 : 0;
    }

    public static void Pass(string name)
    {
        _passed++;
        Console.WriteLine($"  ✅ PASS: {name}");
    }

    public static void Fail(string name, string reason)
    {
        _failed++;
        Console.WriteLine($"  ❌ FAIL: {name} — {reason}");
    }

    public static void Skip(string name)
    {
        Console.WriteLine($"  ⏭️  SKIP: {name}");
    }
}
