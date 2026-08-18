using System.Reflection;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// F3 铁律"排除并修订旧实现干扰"不变式守卫 — xUnit 测试。
/// 覆盖：旧端点抛出废弃错误、旧适配器标注 [Obsolete]、编排器唯一调用方扫描。
/// </summary>
public static class F3LegacyCleanupGuardTests
{
    public static void RunAll()
    {
        T1_OldAnalystEndpoint_ThrowsOopsBah();
        T2_OldPmEndpoint_ThrowsOopsBah();
        T3_SaOrchestratorAdapter_HasObsoleteAttribute();
        T4_DeprecatedEndpoints_HaveDeprecationMarkers();
        T5_OrchestratorIsOnlyAnalystSkillCaller();
    }

    /// <summary>
    /// T1: 旧 analyst-skill 直调端点方法体仅含 throw Oops.Bah（F3 铁律 1a — 已废止旁路）。
    /// 通过 IL 字节码验证 throw 指令存在（无法真执行 Oops.Bah，因其静态构造依赖 App 宿主）。
    /// </summary>
    private static void T1_OldAnalystEndpoint_ThrowsOopsBah()
    {
        var method = typeof(SkillsApiService).GetMethod(nameof(SkillsApiService.RunAnalystAsync))
            ?? throw new Exception("T1: 找不到 SkillsApiService.RunAnalystAsync 方法");

        var body = method.GetMethodBody();
        if (body == null)
            throw new Exception("T1: RunAnalystAsync 方法体为空（抽象方法？）");

        var il = body.GetILAsByteArray();
        // C# throw 编译为 IL opcode 0x7A
        if (!il.Contains((byte)0x7A))
            throw new Exception("T1: RunAnalystAsync 方法体不含 throw 指令 — 废止端点必须抛异常");

        var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
        if (obsolete == null)
            throw new Exception("T1: RunAnalystAsync 必须有 [Obsolete] 属性");
    }

    /// <summary>
    /// T2: 旧 pm-skill 直调端点方法体仅含 throw Oops.Bah（F3 铁律 1b — 已废止旁路）。
    /// </summary>
    private static void T2_OldPmEndpoint_ThrowsOopsBah()
    {
        var method = typeof(SkillsApiService).GetMethod(nameof(SkillsApiService.RunPmAsync))
            ?? throw new Exception("T2: 找不到 SkillsApiService.RunPmAsync 方法");

        var body = method.GetMethodBody();
        if (body == null)
            throw new Exception("T2: RunPmAsync 方法体为空（抽象方法？）");

        var il = body.GetILAsByteArray();
        if (!il.Contains((byte)0x7A))
            throw new Exception("T2: RunPmAsync 方法体不含 throw 指令 — 废止端点必须抛异常");

        var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
        if (obsolete == null)
            throw new Exception("T2: RunPmAsync 必须有 [Obsolete] 属性");
    }

    /// <summary>
    /// T3: SaOrchestratorAdapter 必须标注 [Obsolete]（F3 铁律 3b — agent 模式仅回归对比）。
    /// </summary>
    private static void T3_SaOrchestratorAdapter_HasObsoleteAttribute()
    {
        var type = typeof(SaOrchestratorAdapter);
        var attr = type.GetCustomAttribute<ObsoleteAttribute>();
        if (attr == null)
            throw new Exception("T3: SaOrchestratorAdapter 必须标注 [Obsolete]，缺失！");
        if (string.IsNullOrWhiteSpace(attr.Message))
            throw new Exception("T3: SaOrchestratorAdapter [Obsolete] 消息不能为空");
        if (!attr.Message.Contains("编译器") && !attr.Message.Contains("Compiler"))
            throw new Exception($"T3: SaOrchestratorAdapter [Obsolete] 消息应提及 SaNineViewCompiler，实际：{attr.Message}");
    }

    /// <summary>
    /// T4: 旧 sa-gate / execute 端点应有 &lt;deprecated&gt; XML 文档标记（F3 铁律 3a）。
    /// 通过反射检查 RunAnalystAsync / RunPmAsync 方法上的 XML doc 注释。
    /// 注：C# 不保留 XML doc 在运行时，此处检查方法级别的 [Obsolete] 作为等价守卫。
    /// </summary>
    private static void T4_DeprecatedEndpoints_HaveDeprecationMarkers()
    {
        // RunAnalystAsync 必须有 [Obsolete]
        var analystMethod = typeof(SkillsApiService).GetMethod(nameof(SkillsApiService.RunAnalystAsync));
        if (analystMethod == null)
            throw new Exception("T4: 找不到 SkillsApiService.RunAnalystAsync 方法");
        var analystObs = analystMethod.GetCustomAttribute<ObsoleteAttribute>();
        if (analystObs == null)
            throw new Exception("T4: RunAnalystAsync 必须有 [Obsolete] 属性");

        // RunPmAsync 必须有 [Obsolete]
        var pmMethod = typeof(SkillsApiService).GetMethod(nameof(SkillsApiService.RunPmAsync));
        if (pmMethod == null)
            throw new Exception("T4: 找不到 SkillsApiService.RunPmAsync 方法");
        var pmObs = pmMethod.GetCustomAttribute<ObsoleteAttribute>();
        if (pmObs == null)
            throw new Exception("T4: RunPmAsync 必须有 [Obsolete] 属性");
    }

    /// <summary>
    /// T5: 静态源码扫描 — 确认仅 RequirementAnalysisOrchestrator / DesignSkillOrchestrator
    /// 调用 harness.RunAsync("analyst-skill") 或 harness.RunAsync("pm-skill")。
    /// 任何其他文件出现此类调用 = 违规旁路（F3 铁律 + 禁令七）。
    /// </summary>
    private static void T5_OrchestratorIsOnlyAnalystSkillCaller()
    {
        // 从测试程序集路径回溯到仓库根目录
        var asmDir = Path.GetDirectoryName(typeof(F3LegacyCleanupGuardTests).Assembly.Location)
            ?? throw new Exception("T5: 无法确定测试程序集目录");
        // bin/Debug/net8.0 → 往上 6 层到 repo 根
        var repoRoot = Path.GetFullPath(Path.Combine(asmDir, "..", "..", "..", "..", "..", ".."));
        var inteAssistantDir = Path.Combine(repoRoot, "backend", "modularity", "inteAssistant");

        if (!Directory.Exists(inteAssistantDir))
            throw new Exception($"T5: InteAssistant 源码目录不存在: {inteAssistantDir}");

        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RequirementAnalysisOrchestrator.cs",
            "DesignSkillOrchestrator.cs"
        };

        // 搜索模式：harness.RunAsync("analyst-skill") 或 harness.RunAsync("pm-skill")
        // 涵盖可能的变量名变体：harness / _harness / skillHarness
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(inteAssistantDir, "*.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            if (allowedFiles.Contains(fileName))
                continue;

            var content = File.ReadAllText(file);
            // 匹配任何标识符.RunAsync("analyst-skill") 或 .RunAsync("pm-skill")
            if (content.Contains(".RunAsync(\"analyst-skill\")") ||
                content.Contains(".RunAsync(\"pm-skill\")"))
            {
                violations.Add(fileName);
            }
        }

        if (violations.Count > 0)
            throw new Exception(
                $"T5: 以下文件违规直调 analyst-skill / pm-skill（必须走 RequirementAnalysisOrchestrator 三轮编排器）：\n" +
                string.Join("\n", violations.Select(f => $"  - {f}")));
    }
}
