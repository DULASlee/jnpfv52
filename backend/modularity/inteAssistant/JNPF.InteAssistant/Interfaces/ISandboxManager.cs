namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// Docker 沙箱调度器接口 (Phase 6 Day 3-5).
/// 管理 Docker 容器的创建、部署、销毁和状态查询.
/// </summary>
public interface ISandboxManager
{
    /// <summary>
    /// 创建沙箱实例.
    /// </summary>
    Task<SandboxInstance> CreateAsync(SandboxConfig config);

    /// <summary>
    /// 部署 zip 内容到沙箱.
    /// </summary>
    Task DeployAsync(string sandboxId, byte[] zipContent);

    /// <summary>
    /// 销毁沙箱实例.
    /// </summary>
    Task DestroyAsync(string sandboxId);

    /// <summary>
    /// 获取沙箱状态.
    /// </summary>
    Task<SandboxInstance?> GetStatusAsync(string sandboxId);

    /// <summary>
    /// 获取所有沙箱列表.
    /// </summary>
    Task<IReadOnlyList<SandboxInstance>> GetAllAsync();

    /// <summary>
    /// 销毁所有沙箱（用于紧急清理）.
    /// </summary>
    Task DestroyAllAsync();
}

/// <summary>
/// 沙箱配置.
/// </summary>
public class SandboxConfig
{
    /// <summary>
    /// 沙箱唯一标识.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 租户 ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// CPU 限制（核数）.
    /// </summary>
    public int CpuLimit { get; set; } = 1;

    /// <summary>
    /// 内存限制（如 "4Gi"）.
    /// </summary>
    public string MemoryLimit { get; set; } = "4Gi";

    /// <summary>
    /// 超时秒数.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 沙箱镜像名.
    /// </summary>
    public string Image { get; set; } = "jnpf-sandbox:latest";

    /// <summary>
    /// 沙箱暴露端口.
    /// </summary>
    public int Port { get; set; } = 8080;
}

/// <summary>
/// 沙箱实例状态.
/// </summary>
public class SandboxInstance
{
    /// <summary>
    /// 沙箱 ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 状态: creating/ready/testing/destroying/destroyed/error.
    /// </summary>
    public string Status { get; set; } = "creating";

    /// <summary>
    /// 沙箱访问 URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 沙箱数据库连接字符串.
    /// </summary>
    public string? DbConnectionString { get; set; }

    /// <summary>
    /// Docker 容器 ID.
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 沙箱配置.
    /// </summary>
    public SandboxConfig Config { get; set; } = new();
}
