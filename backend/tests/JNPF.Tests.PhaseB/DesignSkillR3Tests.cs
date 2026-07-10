using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;

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
    }

    private static void T1_AllDesignSkills_AreCognitiveMold()
    {
        var toolkit = new FakeDesignToolkit();
        var skills = new CognitiveSkill[]
        {
            new ArchitectSkillService(toolkit, null!, null!, null!),
            new DbDesignSkillService(toolkit, null!, null!),
            new UiDesignSkillService(toolkit, null!, null!),
            new SystemDesignSkillService(toolkit, new JNPF.InteAssistant.Constraints.ConstraintEngineService(null!), null!),
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
        var skill = new ArchitectSkillService(new FakeDesignToolkit(), null!, null!, null!);
        var result = skill.ValidateInputAsync(IrSnapshot.Empty).GetAwaiter().GetResult();
        if (result.IsValid)
            throw new Exception("T4 无 IR-1 应校验失败");
    }

    private static void T5_NoFallbackMethods_InCodebase()
    {
        // 静态断言：R3 删除的 fallback 方法名不得再出现（grep 级契约）
        var forbidden = new[] { "BuildFallbackArchitecture", "BuildFallbackDdl", "BuildFallbackFormPageIr" };
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "modularity", "inteAssistant", "JNPF.InteAssistant", "Skills"));
        if (!Directory.Exists(dir))
            return; // 测试运行目录可能不同，跳过文件扫描

        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            foreach (var name in forbidden)
            {
                if (text.Contains(name, StringComparison.Ordinal))
                    throw new Exception($"T5 发现已删除 fallback 方法 {name} 于 {file}");
            }
        }
    }

    private sealed class FakeDesignToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }
}
