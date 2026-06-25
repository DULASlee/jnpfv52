// 文件：Gates/GateResult.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：门控结果（不可变 record）

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

    public static GateResult Fail(string reason, List<string>? warnings = null, string hint = "") =>
        new()
        {
            Passed = false,
            Reason = reason,
            Hint = hint,
            Warnings = warnings != null ? warnings.AsReadOnly() : (IReadOnlyList<string>)Array.Empty<string>()
        };
}
