using JNPF.ConfigurableOptions;

namespace JNPF.Options;

/// <summary>
/// 运行时基座特性开关（M11）.
/// 四布尔位精确熔断四个运行时特性（ADR-2 回滚轴：特性轨=开关，重构轨=git revert）.
/// 默认全 false：配置节缺失时平台行为与现状逐字节一致（安全侧倒）.
/// 绑定节名 = 类名去 Options 后缀（平台 Penetrates.GetOptionsConfiguration 约定），即 App.json "RuntimeFoundation" 节.
/// 落位依据：假设 A-1 闭环结论——framework/JNPF 为全部消费方（API.Entry/Common.Core/EventBus.Outbox/InteAssistant）共同可达的最下层.
/// </summary>
public sealed class RuntimeFoundationOptions : IConfigurableOptions
{
    /// <summary>
    /// App.json 配置节名.
    /// </summary>
    public const string Section = "RuntimeFoundation";

    /// <summary>
    /// M10 异常边界：非 HTTP 入口统一异常捕获与结构化记录.
    /// </summary>
    public bool ExceptionBoundary { get; set; }

    /// <summary>
    /// M8 Outbox 可靠性：卡死消息回收器（Sweeper）与 DB 单行互斥锁.
    /// </summary>
    public bool OutboxSweeper { get; set; }

    /// <summary>
    /// M9 出站韧性：LLM/MCP 出站调用的重试/熔断/超时管道.
    /// </summary>
    public bool OutboundResilience { get; set; }

    /// <summary>
    /// M7 可查询日志：全级别文件 JSON 日志、请求日志与内置查询 API.
    /// </summary>
    public bool QueryableLogging { get; set; }
}
