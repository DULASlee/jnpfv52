using JNPF.DependencyInjection;
using System.Text.Json.Serialization;

namespace JNPF.Common.Core.Job;

/// <summary>
/// JNPF-HTTP 作业消息.
/// </summary>
[SuppressSniffer]
public class JNPFHttpJobMessage
{
    /// <summary>
    /// 请求地址.
    /// </summary>
    public string RequestUri { get; set; }

    /// <summary>
    /// 请求方法.
    /// </summary>
    public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

    /// <summary>
    /// 请求头.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 请求报文体.
    /// </summary>
    public string Body { get; set; }

    /// <summary>
    /// 确保请求成功，否则抛异常.
    /// </summary>
    public bool EnsureSuccessStatusCode { get; set; } = true;

    /// <summary>
    /// 描述信息.
    /// </summary>
    [JsonIgnore]
    public string Description { get; set; }

    /// <summary>
    /// 作业组名称.
    /// </summary>
    [JsonIgnore]
    public string GroupName { get; set; }

    /// <summary>
    /// 任务ID.
    /// </summary>
    public string TaskId { get; set; }

    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; }
}