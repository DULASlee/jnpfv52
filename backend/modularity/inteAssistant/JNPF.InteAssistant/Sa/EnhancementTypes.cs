using System.Text.Json;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// 假设项记录——SA 编译器或 LLM 精化器推导的非用户明确指定的项。
/// Round 1/2 内存传递（注入 LLM prompt），Round 3 落库到 sa_assumptions。
/// </summary>
public record Assumption(
    string EventId,
    string SourceStep,
    string Text,
    decimal Confidence);

/// <summary>
/// 步骤增强结果——PSpec/DecisionTable 的 LLM 精化产出。
/// 在 Compile 之后执行（不在 Compile 内部），保持 Compile 零 LLM 纯净。
/// </summary>
public record StepEnhancement(
    string StepName,
    JsonElement? RefinedPayload,
    IReadOnlyList<Assumption> Assumptions,
    int LlmTokensUsed);
