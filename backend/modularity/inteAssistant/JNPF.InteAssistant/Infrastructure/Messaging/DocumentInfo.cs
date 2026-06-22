// 文件：Infrastructure/Messaging/DocumentInfo.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>文档下载事件 DTO（前端用此渲染下载卡片）</summary>
public sealed record DocumentInfo(string Title, string DownloadUrl, string FileName);
