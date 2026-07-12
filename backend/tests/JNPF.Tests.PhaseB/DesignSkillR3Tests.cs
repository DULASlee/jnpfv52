using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.Extensions.Logging.Abstractions;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 设计四 Skill R3 认知模具迁移测试（施工包 21 R3）.
/// </summary>
public static class DesignSkillR3Tests
{
    public static void RunAll()
    {
        T1_AllDesignSkills_AreCognitiveMold();
        T2_DbDesign_ValidateDdlSyntax_RejectsEmpty();
        T3_DbDesign_ExtractTableNames_FromSkeleton();
        T4_Architect_ValidateInput_RejectsMissingIr1();
        T5_NoFallbackMethods_InCodebase();
        T6_BuildFallbackClarification_HasAtLeastTwoRequired();
        T7_ValidateModuleResponsibilities_RejectsEmpty();
        T8_SystemDesign_FallbackClarification_HasAtLeastTwoRequired();
        T9_SystemDesign_DeriveStructuredDesign_ProducesNonEmptyOutput();
        T10_SystemDesignClarificationSkill_IsCognitiveMold();
    }

    private static void T1_AllDesignSkills_AreCognitiveMold()
    {
        var toolkit = new FakeDesignToolkit();
        var skills = new CognitiveSkill[]
        {
            new ArchitectSkillService(toolkit, null!, null!, null!, NullLogger<ArchitectSkillService>.Instance),
            new DbDesignSkillService(toolkit, null!, null!),
            new UiDesignSkillService(toolkit, null!, null!),
            new SystemDesignSkillService(toolkit, new JNPF.InteAssistant.Constraints.ConstraintEngineService(null!), null!, NullLogger<SystemDesignSkillService>.Instance),
        };

        foreach (var skill in skills)
        {
            if (skill.Version != "2.0.0-cognitive")
                throw new Exception($"{skill.SkillId} 版本应为 2.0.0-cognitive，实际 {skill.Version}");
            if (skill.Layer != SkillLayer.Refinement)
                throw new Exception($"{skill.SkillId} Layer 应为 Refinement");
        }
    }

    private static void T2_DbDesign_ValidateDdlSyntax_RejectsEmpty()
    {
        try
        {
            DbDesignSkillService.ValidateDdlSyntax("");
            throw new Exception("T2 空 DDL 应抛异常");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex.Message.Contains("T2"))
                throw;
        }
    }

    private static void T3_DbDesign_ExtractTableNames_FromSkeleton()
    {
        // S4 改造：ExtractTableNames 已删除，改为测试 EntityDesignProjector 投影
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "sk",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[{"entityName":"Leave","tableName":"OA_LEAVE","fields":[{"name":"id","type":"string","primaryKey":true}]}]}""",
                },
            },
        };
        var projection = JNPF.InteAssistant.Codegen.EntityDesign.EntityDesignProjector.Project(
            snapshot, new JNPF.InteAssistant.Codegen.EntityDesign.EntityDesignProjectionOptions
            {
                TenantId = "t",
                ProjectId = "p",
                PipelineId = "1",
            });
        var tables = projection.TableNames();
        if (tables.Count != 1 || tables[0] != "OA_LEAVE")
            throw new Exception($"T3 表名投影失败: {string.Join(",", tables)}");
    }

    private static void T4_Architect_ValidateInput_RejectsMissingIr1()
    {
        var skill = new ArchitectSkillService(new FakeDesignToolkit(), null!, null!, null!, NullLogger<ArchitectSkillService>.Instance);
        var result = skill.ValidateInputAsync(IrSnapshot.Empty).GetAwaiter().GetResult();
        if (result.IsValid)
            throw new Exception("T4 无 IR-1 应校验失败");
    }

    private static void T5_NoFallbackMethods_InCodebase()
    {
        // 静态断言：R3 删除的 fallback 方法名不得再出现（grep 级契约）
        // 使用 \b 单词边界避免子串误杀（如 BuildFallbackArchitectureClarification 是 ADR-005 合法方法）
        var forbidden = new[] { "BuildFallbackArchitecture", "BuildFallbackDdl", "BuildFallbackFormPageIr" };

        // 从 bin/{config}/{tfm} 向上 5 级到 repo 根（修复死代码：原来 4 级只到 tests/）
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "modularity", "inteAssistant", "JNPF.InteAssistant", "Skills"));
        if (!Directory.Exists(dir))
            throw new Exception($"T5 技能目录不存在: {dir}");

        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            foreach (var name in forbidden)
            {
                // 使用 \b 单词边界：BuildFallbackArchitecture 不匹配 BuildFallbackArchitectureClarification
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(name)}\b", RegexOptions.Multiline))
                    throw new Exception($"T5 发现已删除 fallback 方法 {name} 于 {Path.GetFileName(file)}");
            }
        }
    }

    private static void T6_BuildFallbackClarification_HasAtLeastTwoRequired()
    {
        // 30 号 §SG3 打回修复：skipAll 假绿 — fallback 澄清至少 2 个 Required 题
        var method = typeof(ArchitectSkillService).GetMethod(
            "BuildFallbackArchitectureClarification",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new Exception("T6 找不到 BuildFallbackArchitectureClarification 方法");

        var set = method.Invoke(null, null) as ClarificationSet
            ?? throw new Exception("T6 返回值不是 ClarificationSet");

        var requiredCount = set.Questions.Count(q => q.Required);
        if (requiredCount < 2)
            throw new Exception(
                $"T6 fallback 架构澄清 Required 题数量不足：{requiredCount}/3，至少需要 2 个（30 号 §SG3 skipAll 假绿修复）");
    }

    private static void T7_ValidateModuleResponsibilities_RejectsEmpty()
    {
        // 30 号 §SG3 打回修复：03 职责空 — 每个 module 必须有非空 responsibilities
        // 使用反射调用 internal static ValidateModuleResponsibilities (同 T6 模式)
        var method = typeof(ArchitectSkillService).GetMethod(
            "ValidateModuleResponsibilities",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new Exception("T7 找不到 ValidateModuleResponsibilities 方法");

        var goodJson = """
            {
              "pattern": "layered",
              "modules": [
                {"name":"用户管理","responsibilities":"管理用户注册、登录、权限分配与个人信息维护","aggregates":["User"]},
                {"name":"订单管理","responsibilities":"处理订单创建、状态流转、支付对接与退款流程","aggregates":["Order"]}
              ],
              "candidates": [],
              "selectedIndex": 0
            }
            """;

        var goodResult = (ValueTuple<bool, string>)method.Invoke(null, new object[] { goodJson })!;
        if (!goodResult.Item1)
            throw new Exception($"T7 合法 JSON 应通过验证: {goodResult.Item2}");

        var badJson = """
            {
              "pattern": "layered",
              "modules": [
                {"name":"用户管理","responsibilities":""},
                {"name":"订单管理"}
              ],
              "candidates": [],
              "selectedIndex": 0
            }
            """;

        var badResult = (ValueTuple<bool, string>)method.Invoke(null, new object[] { badJson })!;
        if (badResult.Item1)
            throw new Exception("T7 缺失 responsibilities 的 JSON 应拒绝，但通过了验证");
        if (string.IsNullOrWhiteSpace(badResult.Item2))
            throw new Exception("T7 拒绝时应提供错误信息");

        // 空 modules 数组也应拒绝
        var emptyModules = """{"pattern":"layered","modules":[],"candidates":[],"selectedIndex":0}""";
        var emptyResult = (ValueTuple<bool, string>)method.Invoke(null, new object[] { emptyModules })!;
        if (emptyResult.Item1)
            throw new Exception("T7 空 modules 数组应拒绝");
    }

    private static void T8_SystemDesign_FallbackClarification_HasAtLeastTwoRequired()
    {
        // 30 号 §SG4 打回修复：skipAll 假绿 — SystemDesign fallback 澄清至少 2 个 Required 题
        var method = typeof(SystemDesignClarificationSkill).GetMethod(
            "BuildFallbackSystemDesignClarification",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new Exception("T8 找不到 BuildFallbackSystemDesignClarification 方法");

        var set = method.Invoke(null, null) as ClarificationSet
            ?? throw new Exception("T8 返回值不是 ClarificationSet");

        var requiredCount = set.Questions.Count(q => q.Required);
        if (requiredCount < 2)
            throw new Exception(
                $"T8 fallback 总体设计澄清 Required 题数量不足：{requiredCount}/3，至少需要 2 个（30 号 §SG4 skipAll 假绿修复）");
    }

    private static void T9_SystemDesign_DeriveStructuredDesign_ProducesNonEmptyOutput()
    {
        // P9-S1：DeriveStructuredDesign 必须从 skeleton + formPage 派生非空输出
        // 使用反射调用 internal static DeriveStructuredDesign
        var method = typeof(SystemDesignSkillService).GetMethod(
            "DeriveStructuredDesign",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new Exception("T9 找不到 DeriveStructuredDesign 方法");

        var skeleton = new IrSnapshotFragment
        {
            FragmentId = "sk-test",
            FragmentType = IrFragmentTypes.Skeleton,
            StabilityState = IrStabilityStates.Stable,
            Payload = """
                {
                  "stateTransitions": [
                    {"entity":"Leave","from":"Draft","to":"Pending","trigger":"submit"},
                    {"entity":"Leave","from":"Pending","to":"Approved","trigger":"approve"},
                    {"entity":"Leave","from":"Pending","to":"Rejected","trigger":"reject"}
                  ]
                }
                """,
        };

        var formPage = new IrSnapshotFragment
        {
            FragmentId = "fp-test",
            FragmentType = IrFragmentTypes.FormPageIR,
            StabilityState = IrStabilityStates.Stable,
            Payload = """
                {
                  "pages": [
                    {"id":"page-leave-list","title":"请假列表","pageType":"list","entityBinding":"Leave"},
                    {"id":"page-leave-detail","title":"请假详情","pageType":"detail","entityBinding":"Leave"},
                    {"id":"page-leave-form","title":"请假表单","pageType":"form","entityBinding":"Leave"}
                  ]
                }
                """,
        };

        var result = method.Invoke(null, new object?[] { skeleton, formPage });
        if (result == null)
            throw new Exception("T9 DeriveStructuredDesign 返回 null");

        var (stateMachines, workflowNodes, menus) =
            ((List<object>, List<object>, List<object>))result;

        // 状态机：1 个实体 (Leave)
        if (stateMachines.Count != 1)
            throw new Exception($"T9 预期 1 个状态机，实际 {stateMachines.Count}");

        // 工作流节点：至少 2 个 (start + approver)
        if (workflowNodes.Count < 2)
            throw new Exception($"T9 预期至少 2 个工作流节点，实际 {workflowNodes.Count}");

        // 菜单：1 个列表页 (pageType=list)
        if (menus.Count != 1)
            throw new Exception($"T9 预期 1 个菜单项（仅列表页），实际 {menus.Count}");
    }

    private static void T10_SystemDesignClarificationSkill_IsCognitiveMold()
    {
        // 验证 SystemDesignClarificationSkill 正确注册为认知模具
        var skill = new SystemDesignClarificationSkill(
            new FakeDesignToolkit(),
            null!, // ISkillLlmBudgetGuard — 反射调用不经过 LLM
            null!, // IConstraintEngineService — 反射调用不经过约束引擎
            null!, // IPipelineSseChannelHub
            null!, // EntityDesignRepository
            NullLogger<SystemDesignClarificationSkill>.Instance);

        if (skill.SkillId != DesignSkillIds.SystemDesignClarification)
            throw new Exception(
                $"T10 SkillId 应为 {DesignSkillIds.SystemDesignClarification}，实际 {skill.SkillId}");

        if (skill.Version != "1.0.0-clarification")
            throw new Exception($"T10 Version 应为 1.0.0-clarification，实际 {skill.Version}");

        if (skill.Layer != SkillLayer.Refinement)
            throw new Exception($"T10 Layer 应为 Refinement，实际 {skill.Layer}");

        // 验证信息需求包含 Architecture / DDL / FormPageIR
        var needs = skill.InformationNeeds;
        if (needs.RequiredStability != IrStabilityStates.Stable)
            throw new Exception("T10 RequiredStability 应为 Stable");

        var fragTypes = new HashSet<string>(needs.IrFragmentTypes!);
        if (!fragTypes.Contains(IrFragmentTypes.Architecture)
            || !fragTypes.Contains(IrFragmentTypes.DDL)
            || !fragTypes.Contains(IrFragmentTypes.FormPageIR))
            throw new Exception("T10 InformationNeeds 缺少必要片段类型 (Architecture/DDL/FormPageIR)");

        // 验证输出事件包含 SystemDesignLocked + ClarificationRequested
        var outputs = skill.Outputs;
        var outEvents = new HashSet<string>(outputs.IrEventTypes!);
        if (!outEvents.Contains(IrEventTypes.SystemDesignLocked)
            || !outEvents.Contains(IrEventTypes.ClarificationRequested))
            throw new Exception("T10 Outputs 缺少必要事件类型 (SystemDesignLocked/ClarificationRequested)");
    }

    private sealed class FakeDesignToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }
}
