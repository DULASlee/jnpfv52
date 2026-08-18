// 文件：Gates/GateResult.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：门控结果 + 语义评估 DTO（不可变 record）

using System.Text;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 门控执行结果
/// record + init = 不可变，返回后调用方无法篡改
/// </summary>
public sealed record GateResult
{
    public bool Passed { get; init; }
    public string Reason { get; init; } = "";
    public string Hint { get; init; } = "";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string MergedText { get; init; } = "";
    public string AttachmentText { get; init; } = "";
    public int AttachmentCount { get; init; }
    public int BlockedCount { get; init; }

    /// <summary>语义评估结果（门控通过时也携带，供Stage 1骨架提取使用）</summary>
    public SemanticFitnessResult? SemanticFitness { get; init; }

    public static GateResult Fail(string reason, List<string>? warnings = null, string hint = "") =>
        new()
        {
            Passed = false,
            Reason = reason,
            Hint = hint,
            Warnings = warnings != null ? warnings.AsReadOnly() : (IReadOnlyList<string>)Array.Empty<string>()
        };

    /// <summary>语义不合格的工厂方法</summary>
    public static GateResult SemanticallyUnfit(SemanticFitnessResult fitness, List<string>? warnings = null) =>
        new()
        {
            Passed = false,
            SemanticFitness = fitness,
            Reason = fitness.BuildSummary(),
            Hint = fitness.BuildGuidance(),
            Warnings = warnings?.AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()
        };
}

/// <summary>
/// 语义合格性评估结果（不可变 record）
/// </summary>
public sealed record SemanticFitnessResult
{
    public bool Passed { get; init; }
    public double Score { get; init; }
    public FitnessLevel Level { get; init; } = FitnessLevel.Insufficient;
    public List<IdentifiedElement> Identified { get; init; } = new();
    public List<MissingElement> Missing { get; init; } = new();
    public string NextStepGuidance { get; init; } = "";

    public string BuildSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"需求材料评估结果：{Level}（评分 {Score:F0}/100）");

        if (Identified.Count > 0)
        {
            sb.AppendLine("\n✅ 已识别的要素：");
            foreach (var item in Identified)
                sb.AppendLine($"  - {item.Category}：{item.Description}");
        }

        var criticalMissing = Missing.Where(m => m.Severity == "critical").ToList();
        if (criticalMissing.Count > 0)
        {
            sb.AppendLine("\n❌ 缺失的关键要素（必须补充）：");
            foreach (var item in criticalMissing)
                sb.AppendLine($"  - {item.Category}：{item.Description}");
        }

        return sb.ToString();
    }

    public string BuildGuidance()
    {
        if (!string.IsNullOrEmpty(NextStepGuidance))
            return NextStepGuidance;

        var criticalMissing = Missing.Where(m => m.Severity == "critical").ToList();
        if (criticalMissing.Count == 0)
            return "请补充缺失要素后重新提交。";

        var sb = new StringBuilder();
        sb.AppendLine("请根据以下建议补充需求材料：\n");
        for (int i = 0; i < criticalMissing.Count; i++)
        {
            sb.AppendLine($"{i + 1}. 【{criticalMissing[i].Category}】{criticalMissing[i].HowToFix}");
        }
        return sb.ToString();
    }
}

public enum FitnessLevel
{
    /// <summary>足够：至少1个完整业务事件+角色+实体+5字段</summary>
    Sufficient,
    /// <summary>部分：有部分内容但不完整</summary>
    Partial,
    /// <summary>不足：几乎无法提取有效信息</summary>
    Insufficient
}

/// <summary>已识别的要素</summary>
public sealed record IdentifiedElement
{
    public string Category { get; init; } = "";      // 业务事件/角色/数据实体/字段/流程
    public string Description { get; init; } = "";
    public string Evidence { get; init; } = "";       // 从原文中提取的证据
}

/// <summary>缺失的要素</summary>
public sealed record MissingElement
{
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Severity { get; init; } = "critical";  // critical / warning
    public string HowToFix { get; init; } = "";           // 具体的修复建议
}
