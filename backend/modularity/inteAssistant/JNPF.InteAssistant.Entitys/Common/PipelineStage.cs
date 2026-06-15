namespace JNPF.InteAssistant.Entitys.Common;

/// <summary>
/// 流水线阶段常量
/// 统一前后端阶段定义
/// 对齐前端 src/core/ai/pipeline/stages.ts STAGES
/// </summary>
public static class PipelineStage
{
    /// <summary>阶段1: 需求分析</summary>
    public const string Requirement = "requirement";

    /// <summary>阶段2: 架构设计</summary>
    public const string Architecture = "architecture";

    /// <summary>阶段3: 总体设计（多 SubAgent 并行）</summary>
    public const string Design = "design";

    /// <summary>阶段4: 自动开发（代码生成 + 编译验证）</summary>
    public const string Development = "development";

    /// <summary>阶段5: 交付（沙箱部署 + ZIP 导出）</summary>
    public const string Delivery = "delivery";

    /// <summary>
    /// 阶段顺序（用于确定下一阶段）
    /// </summary>
    public static readonly string[] Order =
        [Requirement, Architecture, Design, Development, Delivery];

    /// <summary>
    /// 获取下一阶段
    /// </summary>
    public static string? GetNext(string current)
    {
        var idx = Array.IndexOf(Order, current);
        return idx >= 0 && idx < Order.Length - 1 ? Order[idx + 1] : null;
    }

    /// <summary>
    /// 获取上一阶段
    /// </summary>
    public static string? GetPrev(string current)
    {
        var idx = Array.IndexOf(Order, current);
        return idx > 0 ? Order[idx - 1] : null;
    }

    /// <summary>
    /// 阶段内状态
    /// </summary>
    public static class Status
    {
        /// <summary>等待执行</summary>
        public const string Pending = "pending";

        /// <summary>执行中</summary>
        public const string Running = "running";

        /// <summary>等待人工确认</summary>
        public const string Review = "review";

        /// <summary>已确认通过</summary>
        public const string Approved = "approved";

        /// <summary>执行失败</summary>
        public const string Failed = "failed";

        /// <summary>已跳过</summary>
        public const string Skipped = "skipped";
    }
}
