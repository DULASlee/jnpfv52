// 文件：Infrastructure/Messaging/StageCompleteInfo.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>阶段完成事件 DTO（前端用此渲染确认按钮）</summary>
public sealed record StageCompleteInfo(string Stage, string StageLabel, string NextStage);
