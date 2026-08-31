using System.Collections.Immutable;

namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 执行上下文 (R12 三元组载体)。
///
/// 约束：
///   - 三元组 (TenantId, ProjectId, PipelineId) 不可为空；
///   - 不可变对象，修改必须通过 With* 方法创建新实例；
///   - 不包含 Intelligence/Workflow 概念。
/// </summary>
public sealed class RuntimeContext
{
    /// <summary>
    /// 租户标识。
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// 项目标识。
    /// </summary>
    public string ProjectId { get; }

    /// <summary>
    /// 管道标识。
    /// </summary>
    public string PipelineId { get; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// 创建者用户 ID（同租户用户隔离）。
    /// </summary>
    public string CreatorUserId { get; }

    /// <summary>
    /// 附加元数据（用于扩展，不可包含业务数据）。
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private RuntimeContext(
        string tenantId,
        string projectId,
        string pipelineId,
        DateTime createdAtUtc,
        string creatorUserId,
        IReadOnlyDictionary<string, string> metadata)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        PipelineId = pipelineId;
        CreatedAtUtc = createdAtUtc;
        CreatorUserId = creatorUserId;
        Metadata = metadata ?? ImmutableDictionary<string, string>.Empty;
    }

    /// <summary>
    /// 创建新的 RuntimeContext。
    /// </summary>
    public static RuntimeContext Create(
        string tenantId,
        string projectId,
        string pipelineId,
        string creatorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUserId);

        return new RuntimeContext(
            tenantId,
            projectId,
            pipelineId,
            DateTime.UtcNow,
            creatorUserId,
            ImmutableDictionary<string, string>.Empty);
    }

    /// <summary>
    /// 创建带元数据的副本。
    /// </summary>
    public RuntimeContext WithMetadata(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var newMetadata = new Dictionary<string, string>(Metadata) { [key] = value };
        return new RuntimeContext(TenantId, ProjectId, PipelineId, CreatedAtUtc, CreatorUserId, newMetadata);
    }
}
