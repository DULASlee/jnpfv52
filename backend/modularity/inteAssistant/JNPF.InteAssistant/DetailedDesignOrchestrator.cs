using System.Collections.Concurrent;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// 详细设计编排器
/// 6 个 SubAgent 两批次并行执行:
///   批次 1: 功能模块 + 业务流程 + 数据库
///   批次 2: UI + 权限 + API (依赖批次 1 产出)
/// </summary>
public class DetailedDesignOrchestrator : ITransient
{
    private readonly IEnumerable<ISubAgent> _subAgents;
    private readonly IDocumentMerger _merger;
    private readonly ILogger<DetailedDesignOrchestrator> _logger;

    public DetailedDesignOrchestrator(
        IEnumerable<ISubAgent> subAgents,
        IDocumentMerger merger,
        ILogger<DetailedDesignOrchestrator> logger)
    {
        _subAgents = subAgents;
        _merger = merger;
        _logger = logger;
    }

    public async Task<DetailedDesignResult> ExecuteAsync(
        DetailedDesignContext context,
        IProgress<object>? progress,
        CancellationToken ct = default)
    {
        var agentMap = _subAgents.ToDictionary(a => a.AgentName);
        // P1-5: 用 ConcurrentDictionary 消除并行写竞争(Dictionary 并发读+写会抛异常/返回脏值)
        var results = new ConcurrentDictionary<string, SubAgentResult>();

        _logger.LogInformation("[详细设计] 启动批次1: 功能模块, 业务流程, 数据库 (并行)");
        var batch1Names = new[] { "functional_module", "business_process", "database" };

        var batch1Tasks = batch1Names.Select(async name =>
        {
            if (!agentMap.TryGetValue(name, out var agent))
            {
                results[name] = new SubAgentResult
                { AgentName = name, IsSuccess = false, Error = $"Agent '{name}' 未注册" };
                return;
            }
            var result = await agent.ExecuteAsync(context, results, ct);
            results[name] = result;  // ConcurrentDictionary 写入原子,无需 lock
            _logger.LogInformation("[详细设计] {Agent}: {Status}", name,
                result.IsSuccess ? "✅" : $"❌ {result.Error}");
        });

        // P1-5: 每批次加超时,防 LLM 卡死拖垮整批(单 Agent 60-120s,3 并行给 90s)
        var batch1All = Task.WhenAll(batch1Tasks);
        var batch1Timeout = Task.Delay(TimeSpan.FromSeconds(90), ct);
        if (await Task.WhenAny(batch1All, batch1Timeout) == batch1Timeout)
            _logger.LogWarning("[详细设计] 批次1超时(90s),继续批次2(部分结果可用)");
        await batch1All;  // 等待所有 task 完成(或异常传播)

        if (results.Values.Count(r => r.IsSuccess) < 2)
        {
            return new DetailedDesignResult
            { IsSuccess = false, Error = "批次 1 执行失败，无法继续进行批次 2" };
        }

        _logger.LogInformation("[详细设计] 启动批次2: UI, 权限, API (并行)");
        var batch2Names = new[] { "ui_design", "permission", "api_design" };

        var batch2Tasks = batch2Names.Select(async name =>
        {
            if (!agentMap.TryGetValue(name, out var agent))
            {
                results[name] = new SubAgentResult
                { AgentName = name, IsSuccess = false, Error = $"Agent '{name}' 未注册" };
                return;
            }
            var result = await agent.ExecuteAsync(context, results, ct);
            results[name] = result;
            _logger.LogInformation("[详细设计] {Agent}: {Status}", name,
                result.IsSuccess ? "✅" : $"❌ {result.Error}");
        });

        var batch2All = Task.WhenAll(batch2Tasks);
        var batch2Timeout = Task.Delay(TimeSpan.FromSeconds(90), ct);
        if (await Task.WhenAny(batch2All, batch2Timeout) == batch2Timeout)
            _logger.LogWarning("[详细设计] 批次2超时(90s),继续合并(部分结果可用)");
        await batch2All;

        // 合并为完整文档
        _logger.LogInformation("[详细设计] 合并文档...");
        var mergedDocument = await _merger.MergeAsync(context.ProjectName, results, ct);

        return new DetailedDesignResult
        {
            IsSuccess = true,
            SubAgentResults = results,
            MergedDocument = mergedDocument,
            DocumentUrl = mergedDocument.StoragePath
        };
    }
}
