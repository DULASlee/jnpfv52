using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Llm;

namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// 设计 Skill 专用 ToT——经 ISkillLlmBudgetGuard 多路调用（施工包 21 R3），
/// 每路独立 Acquire+Execute，计入 Skill 级 maxCalls/token 预算。
/// </summary>
public static class BudgetGuardTreeSearch
{
    public sealed class BranchResult
    {
        public int BranchIndex { get; init; }
        public double Temperature { get; init; }
        public bool IsSuccess { get; init; }
        public string Content { get; init; } = string.Empty;
        public string? Error { get; init; }
    }

    public static async Task<IReadOnlyList<BranchResult>> RunAsync(
        ISkillLlmBudgetGuard budgetGuard,
        SkillContext context,
        string skillId,
        string systemPrompt,
        string userPrompt,
        int branchCount,
        double baseTemperature,
        double temperatureStep,
        string? responseFormat,
        CancellationToken ct)
    {
        var schedule = TreeSearchPlanner.BuildTemperatureSchedule(branchCount, baseTemperature, temperatureStep);
        var results = new List<BranchResult>(schedule.Length);

        for (var i = 0; i < schedule.Length; i++)
        {
            var slot = await budgetGuard.AcquireAsync(
                context.ProjectId, skillId, context.RunId, context.TenantId, context.PipelineId, ct);

            var response = await budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
            {
                ProviderCode = context.ProviderCode ?? string.Empty,
                SystemPrompt = systemPrompt,
                Messages = new List<ChatMessage> { new("user", userPrompt) },
                Temperature = schedule[i],
                ResponseFormat = responseFormat,
                MaxTokens = slot.MaxTokens,
                TimeoutMs = slot.TimeoutMs,
                MaxRetries = 1,
            }, ct);

            results.Add(new BranchResult
            {
                BranchIndex = i,
                Temperature = schedule[i],
                IsSuccess = response.IsSuccess && !string.IsNullOrWhiteSpace(response.Content),
                Content = response.Content,
                Error = response.Error,
            });
        }

        return results;
    }
}
