namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// Docker 沙箱调度器接口 (Phase 6 Day 3-5).
/// 管理 Docker 容器的创建、部署、销毁和状态查询.
/// </summary>
public interface ISandboxManager
{
    // ─── 现有方法（添加 CancellationToken）───

    /// <summary>
    /// 创建沙箱实例.
    /// </summary>
    Task<SandboxInstance> CreateAsync(SandboxConfig config, CancellationToken ct = default);

    /// <summary>
    /// 部署 zip 内容到沙箱（保留向后兼容）.
    /// </summary>
    [Obsolete("Use UploadFilesAsync for multi-file deployment.")]
    Task DeployAsync(string sandboxId, byte[] zipContent);

    /// <summary>
    /// 销毁沙箱实例.
    /// </summary>
    Task DestroyAsync(string sandboxId, CancellationToken ct = default);

    /// <summary>
    /// 获取沙箱状态.
    /// </summary>
    Task<SandboxInstance?> GetStatusAsync(string sandboxId, CancellationToken ct = default);

    /// <summary>
    /// 获取所有沙箱列表.
    /// </summary>
    Task<IReadOnlyList<SandboxInstance>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 销毁所有沙箱（用于紧急清理）.
    /// </summary>
    Task DestroyAllAsync(CancellationToken ct = default);

    // ─── 新增方法（P0-3 修复）───

    /// <summary>
    /// 上传文件到沙箱容器（docker cp 实现）.
    /// </summary>
    /// <param name="sandboxId">沙箱 ID</param>
    /// <param name="files">文件列表</param>
    /// <param name="ct">取消令牌</param>
    Task UploadFilesAsync(string sandboxId, List<GeneratedFile> files, CancellationToken ct = default);

    /// <summary>
    /// 在沙箱中执行命令（docker exec 实现）.
    /// </summary>
    /// <param name="sandboxId">沙箱 ID</param>
    /// <param name="command">要执行的 shell 命令</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>命令执行结果</returns>
    Task<CommandResult> ExecuteCommandAsync(string sandboxId, string command, CancellationToken ct = default);

    /// <summary>
    /// 在沙箱中执行脚本（docker exec + 脚本文件）.
    /// </summary>
    /// <param name="sandboxId">沙箱 ID</param>
    /// <param name="scriptType">脚本类型 ("bash", "sql")</param>
    /// <param name="scriptContent">脚本内容</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>命令执行结果</returns>
    Task<CommandResult> ExecuteScriptAsync(string sandboxId, string scriptType, string scriptContent, CancellationToken ct = default);

    /// <summary>
    /// 获取沙箱访问信息（IP、端口、连接串）.
    /// </summary>
    /// <param name="sandboxId">沙箱 ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>沙箱访问信息</returns>
    Task<SandboxInfo> GetSandboxInfoAsync(string sandboxId, CancellationToken ct = default);
}

// ─── 现有类型（保留）───

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

// ─── 新增类型（P0-3 修复）───

/// <summary>
/// 命令执行结果.
/// </summary>
public record CommandResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = "";
    public string Error { get; init; } = "";
    public int ExecutionTimeMs { get; init; }
}

/// <summary>
/// 沙箱访问信息.
/// </summary>
public record SandboxInfo
{
    public string SandboxId { get; init; } = "";
    public string Host { get; init; } = "";
    public int Port { get; init; }
    public string ApiUrl { get; init; } = "";
    public string FrontendUrl { get; init; } = "";
    public string DbConnectionString { get; init; } = "";
}

/// <summary>
/// 生成的文件（用于上传到沙箱）.
/// </summary>
public record GeneratedFile
{
    public string FilePath { get; init; } = "";
    public string Content { get; init; } = "";
    public string FileType { get; init; } = "";
}
