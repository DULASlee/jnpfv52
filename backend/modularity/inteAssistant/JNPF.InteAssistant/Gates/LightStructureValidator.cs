using JNPF.DependencyInjection;
using JNPF.InteAssistant.Sa;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 轻量结构校验器（安全网 1）—— Round 1 SA 编译后执行的内存检查。
/// 不是独立模块，只是 3 条简单规则的确定性检查（无 LLM，纯字符串匹配，&lt; 10ms）。
/// 产出 WARNING 列表注入 Round 2 LLM prompt 上下文，不阻断流程。
/// </summary>
/// <remarks>
/// 与 ScannerValidator（已删除）的区别：ScannerValidator 是独立模块+独立表，本校验器是
/// 50 行代码的纯内存检查，逻辑更简单，不落表。
/// 升级路径：如果 R2 关键词召回率不足 → 引入 jieba 分词。
/// </remarks>
public interface ILightStructureValidator
{
    /// <summary>
    /// 对预分析模型执行 3 条结构规则检查，返回 WARNING 列表。
    /// 返回空列表表示无结构问题。
    /// </summary>
    List<string> Validate(PreAnalysisModel model);
}

public sealed class LightStructureValidator : ILightStructureValidator, ITransient
{
    /// <summary>
    /// 预定义业务关键词列表（可扩充）。
    /// 用于 Rule B：检查需求中的常见业务动词是否被事件覆盖。
    /// </summary>
    private static readonly string[] BusinessKeywords =
    {
        "审批", "申请", "查询", "统计", "报表",
        "导入", "导出", "通知", "提醒", "分配",
        "指派", "验证", "认证", "授权", "校验",
        "提交", "撤回", "驳回", "通过", "拒绝",
        "登记", "录入", "维护", "管理", "配置",
    };

    public List<string> Validate(PreAnalysisModel model)
    {
        var warnings = new List<string>();

        // ── Rule A：事件数量合理性 ─────────────────────────────────────
        // 预期事件数 = 需求文本字符数 / 100，偏差 > ±50% → WARNING
        var reqText = model.RequirementSummary ?? "";
        var textLen = string.IsNullOrWhiteSpace(reqText) ? 100 : reqText.Length;
        var expected = Math.Max(1, textLen / 100);
        var actual = model.BusinessEvents.Count;

        if (actual > 0 && expected > 0)
        {
            var deviation = Math.Abs(actual - expected) / (double)expected;
            if (deviation > 0.5)
            {
                warnings.Add(
                    $"Rule A: 事件数量({actual})与需求文本长度({textLen}字符, 预期约{expected}个事件)偏差 {deviation:P0}，"
                    + "可能存在事件粒度过粗或过细");
            }
        }

        // ── Rule B：关键词覆盖 ────────────────────────────────────────
        // 预定义关键词列表 vs 事件名称，纯字符串包含检查
        var eventNames = model.BusinessEvents
            .Select(e => e.EventName)
            .ToList();

        var uncovered = BusinessKeywords
            .Where(k => !eventNames.Any(name =>
                name.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (uncovered.Count > 0)
        {
            warnings.Add(
                $"Rule B: 以下业务关键词未在任何事件名中出现（共{uncovered.Count}个，显示前8个）: "
                + string.Join(", ", uncovered.Take(8))
                + (uncovered.Count > 8 ? $" ...等{uncovered.Count}个" : ""));
        }

        // ── Rule C：SIMPLE 事件占比 > 80% ──────────────────────────────
        // 提示用户可能低估了事件复杂度
        var totalEvents = model.BusinessEvents.Count;
        if (totalEvents > 0)
        {
            var simpleCount = model.BusinessEvents
                .Count(e => string.Equals(e.ComplexityHint, "simple", StringComparison.OrdinalIgnoreCase));
            var simpleRatio = (double)simpleCount / totalEvents;

            if (simpleRatio > 0.8)
            {
                warnings.Add(
                    $"Rule C: SIMPLE 事件占比 {simpleRatio:P0}（{simpleCount}/{totalEvents}）> 80%，"
                    + "可能存在复杂度低估。建议检查是否有事件应标记为 MEDIUM 或 COMPLEX");
            }
        }

        return warnings;
    }
}
