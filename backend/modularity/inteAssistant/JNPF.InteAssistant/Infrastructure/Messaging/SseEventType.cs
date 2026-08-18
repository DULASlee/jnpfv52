// 文件：Infrastructure/Messaging/SseEventType.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging
// 职责：SSE 事件类型常量——与前端 SSE_EVENT 共享语义

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>
/// SSE 事件类型
///
/// 前端对应：src/views/studio/composables/useSseChat.ts 中的 SSE_EVENT 常量
/// 新增事件类型必须同步修改前端
/// </summary>
public static class SseEventType
{
    /// <summary>AI 回复文字片段</summary>
    public const string Token = "token";

    /// <summary>思考过程</summary>
    public const string Thinking = "thinking";

    /// <summary>生成的文档下载链接</summary>
    public const string Document = "document";

    /// <summary>阶段完成（确认进入下一阶段的按钮）</summary>
    public const string StageComplete = "stage_complete";

    /// <summary>流结束信号</summary>
    public const string Done = "done";

    /// <summary>错误</summary>
    public const string Error = "error";

    /// <summary>IR 事件写入（阶段一）</summary>
    public const string IrEvent = "ir_event";

    /// <summary>IR 片段投影更新（阶段一）</summary>
    public const string FragmentUpdated = "fragment_updated";

    /// <summary>Skill 执行进度（阶段二）</summary>
    public const string SkillProgress = "skill_progress";

    /// <summary>需求分析完成（阶段二）</summary>
    public const string AnalysisCompleted = "analysis_completed";

    /// <summary>沙箱预览就绪（交付链末尾，含试用链接）</summary>
    public const string PreviewReady = "preview_ready";
}
