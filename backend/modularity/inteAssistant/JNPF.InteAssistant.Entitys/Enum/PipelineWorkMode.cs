namespace JNPF.InteAssistant.Entitys.Enum;

/// <summary>
/// 提交需求任务意图 — 决定入口编排路径（非逐步 if/else）。
/// </summary>
public static class PipelineWorkMode
{
    /// <summary>首次全量开发 — S0→S6 完整流水线</summary>
    public const string Greenfield = "greenfield";

    /// <summary>Debug/缺陷修复 — bugfix-skill 增量重算，跳过门控与九步 SA 全链</summary>
    public const string Bugfix = "bugfix";

    /// <summary>二次开发/迭代 — 继承 IR，局部重跑</summary>
    public const string Enhancement = "enhancement";

    public static bool IsValid(string? mode)
        => mode is Greenfield or Bugfix or Enhancement;

    public static string Normalize(string? mode)
        => IsValid(mode) ? mode! : Greenfield;
}
