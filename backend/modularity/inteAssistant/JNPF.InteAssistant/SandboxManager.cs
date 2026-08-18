using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// Docker 沙箱调度器 (Phase B2: 并发队列).
/// 使用 docker CLI 管理容器生命周期，共享 SQL Server 实例.
/// 最大 5 并发，超限自动排队 + 后台调度.
/// </summary>
public sealed class SandboxManager : ISandboxManager, ISingleton, IDisposable
{
    private readonly ILogger<SandboxManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, SandboxInstance> _instances = new();
    private readonly ConcurrentQueue<PendingCreateRequest> _pendingQueue = new();
    private readonly CancellationTokenSource _loopCts = new();

    private int _activeCount;
    private const int MaxConcurrent = 5;
    private const int MaxQueueLength = 50;

    private const string DockerNetwork = "jnpf-sandbox-net";
    private const string DbServerHost = "host.docker.internal"; // Docker 容器访问宿主机 SQL Server

    /// <summary>
    /// 排队中的创建请求.
    /// </summary>
    private sealed class PendingCreateRequest
    {
        public SandboxConfig Config { get; init; } = null!;
        public TaskCompletionSource<SandboxInstance> Tcs { get; init; } = null!;
        public CancellationToken CancellationToken { get; init; }
        public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 沙箱实例创建完成回调（用于 SSE 推送等外部通知）.
    /// </summary>
    public event Action<SandboxInstance>? OnInstanceReady;

    public SandboxManager(ILogger<SandboxManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // 启动后台队列处理循环
        _ = ProcessQueueLoopAsync(_loopCts.Token);
    }

    /// <summary>
    /// 获取当前排队中的请求数.
    /// </summary>
    public int QueueLength => _pendingQueue.Count;

    public void Dispose()
    {
        _loopCts.Cancel();

        // 清空排队请求：逐个取消，避免应用关闭时请求永久挂起
        while (_pendingQueue.TryDequeue(out var pending))
        {
            pending.Tcs.TrySetCanceled();
        }

        _loopCts.Dispose();
    }

    /// <inheritdoc/>
    public async Task<SandboxInstance> CreateAsync(SandboxConfig config, CancellationToken ct = default)
    {
        // 检查并发容量：有空闲槽位 → 直接创建；无 → 排队
        var currentCount = Interlocked.Increment(ref _activeCount);

        if (currentCount <= MaxConcurrent)
            {
                try
                {
                    var instance = await CreateContainerInternalAsync(config, ct);
                    OnInstanceReady?.Invoke(instance);
                    return instance;
                }
                finally
                {
                    Interlocked.Decrement(ref _activeCount);
                }
            }

            // 超并发 → 排队
            Interlocked.Decrement(ref _activeCount); // 回滚占用的槽位

            if (_pendingQueue.Count >= MaxQueueLength)
            {
                _logger.LogWarning("沙箱队列已满: SandboxId={Id}, QueueLength={Len}",
                    config.Id, _pendingQueue.Count);
                throw new InvalidOperationException(
                    $"沙箱创建队列已满（最大 {MaxQueueLength}），请稍后重试");
            }

            var tcs = new TaskCompletionSource<SandboxInstance>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = new PendingCreateRequest
            {
                Config = config,
                Tcs = tcs,
                CancellationToken = ct,
                EnqueuedAt = DateTime.UtcNow
            };
            _pendingQueue.Enqueue(pending);

            _logger.LogInformation(
                "沙箱创建请求已入队: SandboxId={Id}, QueuePosition={Pos}",
                config.Id, _pendingQueue.Count);

            // 5 分钟排队超时
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, ct);

            linkedCts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetException(new TimeoutException(
                        $"沙箱 {config.Id} 排队超时 (5min)，请稍后重试"));
                }
            });

            return await tcs.Task;
    }

    /// <summary>
    /// 实际创建 Docker 容器（从 CreateAsync 或队列调度中调用）.
    /// </summary>
    private async Task<SandboxInstance> CreateContainerInternalAsync(
        SandboxConfig config, CancellationToken ct)
    {
        var instance = new SandboxInstance
        {
            Id = config.Id,
            Status = "creating",
            CreatedAt = DateTime.UtcNow,
            Config = config,
        };

        _instances[config.Id] = instance;

        // 1. 确保 Docker 网络存在
        await EnsureNetworkAsync();

        // 2. 构建容器名
        var containerName = $"jnpf-sandbox-{config.Id}";

        // 3. 构建数据库连接字符串（共享 SQL Server，per-tenant database）
        var dbName = $"JNPF_Sandbox_{config.TenantId}";
        var dbConnectionString = BuildConnectionString(dbName);
        instance.DbConnectionString = dbConnectionString;

        // 4. 启动 Docker 容器（先校验镜像，避免静默失败）
        var imageCheck = await RunDockerAsync($"image inspect {config.Image}", ct);
        if (imageCheck.ExitCode != 0)
        {
            instance.Status = "error";
            var hint = $"沙箱镜像不存在: {config.Image}。请先执行: docker build -t jnpf-sandbox:latest -f docker/jnpf-sandbox/Dockerfile .";
            _logger.LogError("{Hint} stderr={Stderr}", hint, imageCheck.Stderr);
            throw new InvalidOperationException(hint);
        }

        var port = config.Port;
        var args = new StringBuilder();
        args.Append("run -d --rm ");
        args.Append($"--name {containerName} ");
        args.Append($"--network {DockerNetwork} ");
        args.Append($"--cpus={config.CpuLimit} ");
        args.Append($"--memory={config.MemoryLimit} ");
        args.Append($"-p {port}:8080 ");
        args.Append($"-p {config.PreviewPort} ");
        args.Append($"-e ASPNETCORE_ENVIRONMENT=Sandbox ");
        args.Append($"-e ConnectionStrings__Default=\"{dbConnectionString}\" ");
        args.Append($"-e TenantId={config.TenantId} ");
        args.Append($"{config.Image}");

        var result = await RunDockerAsync(args.ToString(), ct);

        if (result.ExitCode != 0)
        {
            instance.Status = "error";
            _logger.LogError("Docker 容器创建失败: {Error}", result.Stderr);
            throw new InvalidOperationException($"Docker 容器创建失败: {result.Stderr}");
        }

        // 5. 记录容器 ID
        instance.ContainerId = result.Stdout.Trim();
        instance.Status = "ready";

        // 6. 查询 Docker 实际端口映射（应用端口 + 预览端口）
        var appPortResult = await RunDockerAsync($"port {instance.ContainerId} 8080", ct);
        var previewPortResult = await RunDockerAsync($"port {instance.ContainerId} {config.PreviewPort}", ct);

        var appHostPort = appPortResult.ExitCode == 0
            ? appPortResult.Stdout.Trim().Split(':').Last().Trim()
            : port.ToString();
        var previewHostPort = previewPortResult.ExitCode == 0
            ? previewPortResult.Stdout.Trim().Split(':').Last().Trim()
            : "0";

        instance.Url = $"http://localhost:{appHostPort}";
        instance.PreviewUrl = $"http://localhost:{previewHostPort}";

        _logger.LogInformation("沙箱 {SandboxId} 创建成功, 容器 {ContainerId}, URL: {Url}",
            config.Id, instance.ContainerId, instance.Url);

        return instance;
    }

    // ─── 后台队列调度 ───

    /// <summary>
    /// 后台循环：轮询队列，有空闲槽位时取出执行.
    /// </summary>
    private async Task ProcessQueueLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Volatile.Read(ref _activeCount) < MaxConcurrent
                    && _pendingQueue.TryDequeue(out var request))
                {
                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        request.Tcs.TrySetCanceled(request.CancellationToken);
                        continue;
                    }

                    Interlocked.Increment(ref _activeCount);
                    _logger.LogInformation(
                        "沙箱从队列中调度: SandboxId={Id}, RemainingQueue={Remaining}",
                        request.Config.Id, _pendingQueue.Count);

                    _ = ExecuteQueuedCreateAsync(request);
                }
                else
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "队列调度循环异常");
            }
        }
    }

    private async Task ExecuteQueuedCreateAsync(PendingCreateRequest request)
    {
        try
        {
            var instance = await CreateContainerInternalAsync(
                request.Config, request.CancellationToken);
            request.Tcs.TrySetResult(instance);
            OnInstanceReady?.Invoke(instance);
        }
        catch (Exception ex)
        {
            request.Tcs.TrySetException(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _activeCount);
        }
    }

    /// <inheritdoc/>
    public async Task DeployAsync(string sandboxId, byte[] zipContent)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance) || instance.ContainerId == null)
            throw new InvalidOperationException($"沙箱 {sandboxId} 不存在或未就绪");

        instance.Status = "testing";

        try
        {
            // 将 zip 复制到容器并解压
            var tempFile = Path.GetTempFileName() + ".zip";
            await File.WriteAllBytesAsync(tempFile, zipContent);

            var copyResult = await RunDockerAsync($"cp \"{tempFile}\" {instance.ContainerId}:/app/deploy.zip");
            if (copyResult.ExitCode != 0)
                throw new InvalidOperationException($"部署文件复制失败: {copyResult.Stderr}");

            var unzipResult = await RunDockerAsync(
                $"exec {instance.ContainerId} unzip -o /app/deploy.zip -d /app/");
            if (unzipResult.ExitCode != 0)
                throw new InvalidOperationException($"解压失败: {unzipResult.Stderr}");

            // 清理临时文件
            try { File.Delete(tempFile); } catch { /* ignore */ }

            instance.Status = "ready";
            _logger.LogInformation("沙箱 {SandboxId} 部署成功", sandboxId);
        }
        catch
        {
            instance.Status = "error";
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DestroyAsync(string sandboxId, CancellationToken ct = default)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance))
        {
            _logger.LogWarning("沙箱 {SandboxId} 不存在，跳过销毁", sandboxId);
            return;
        }

        instance.Status = "destroying";

        try
        {
            if (instance.ContainerId != null)
            {
                await RunDockerAsync($"stop -t 10 {instance.ContainerId}");
                // --rm 标志会自动删除容器
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "销毁沙箱 {SandboxId} 时出错", sandboxId);
        }

        instance.Status = "destroyed";
        _instances.TryRemove(sandboxId, out _);
        _logger.LogInformation("沙箱 {SandboxId} 已销毁", sandboxId);
    }

    /// <inheritdoc/>
    public Task<SandboxInstance?> GetStatusAsync(string sandboxId, CancellationToken ct = default)
    {
        _instances.TryGetValue(sandboxId, out var instance);
        return Task.FromResult(instance);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SandboxInstance>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<SandboxInstance>>(
            _instances.Values.ToList());
    }

    /// <inheritdoc/>
    public async Task DestroyAllAsync(CancellationToken ct = default)
    {
        var ids = _instances.Keys.ToList();
        foreach (var id in ids)
        {
            await DestroyAsync(id, ct);
        }
    }

    // ─── 新增方法（P0-3 修复）───

    /// <inheritdoc/>
    public async Task UploadFilesAsync(
        string sandboxId, List<GeneratedFile> files, CancellationToken ct = default)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance) || instance.ContainerId == null)
            throw new InvalidOperationException($"沙箱 {sandboxId} 不存在或未就绪");

        // 创建临时目录结构
        var tempDir = Path.Combine(Path.GetTempPath(), $"sandbox-upload-{sandboxId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var file in files)
            {
                var filePath = Path.Combine(tempDir, file.FilePath.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(filePath, file.Content, ct);
            }

            // docker cp 到容器
            var result = await RunDockerAsync($"cp {tempDir}/. {instance.ContainerId}:/app/");
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"docker cp 失败: {result.Stderr}");

            _logger.LogInformation("沙箱 {SandboxId} 上传 {Count} 个文件成功", sandboxId, files.Count);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteCommandAsync(
        string sandboxId, string command, CancellationToken ct = default)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance) || instance.ContainerId == null)
            throw new InvalidOperationException($"沙箱 {sandboxId} 不存在或未就绪");

        var sw = Stopwatch.StartNew();
        // 转义单引号，避免 shell 注入
        var escapedCommand = command.Replace("'", "'\\''");
        var result = await RunDockerAsync($"exec {instance.ContainerId} sh -c '{escapedCommand}'", ct);
        sw.Stop();

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            Output = result.Stdout,
            Error = result.Stderr,
            ExecutionTimeMs = (int)sw.ElapsedMilliseconds
        };
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteScriptAsync(
        string sandboxId, string scriptType, string scriptContent, CancellationToken ct = default)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance) || instance.ContainerId == null)
            throw new InvalidOperationException($"沙箱 {sandboxId} 不存在或未就绪");

        // 创建临时脚本文件
        var ext = scriptType == "sql" ? ".sql" : ".sh";
        var tempScript = Path.GetTempFileName() + ext;
        await File.WriteAllTextAsync(tempScript, scriptContent, ct);

        try
        {
            // 上传脚本
            var copyResult = await RunDockerAsync($"cp \"{tempScript}\" {instance.ContainerId}:/tmp/sandbox-script{ext}");
            if (copyResult.ExitCode != 0)
                throw new InvalidOperationException($"脚本上传失败: {copyResult.Stderr}");

            // 执行脚本
            var sw = Stopwatch.StartNew();
            var execCmd = scriptType == "sql"
                ? $"exec {instance.ContainerId} sh -c \"mysql -u sa -p -e 'source /tmp/sandbox-script.sql'\""
                : $"exec {instance.ContainerId} sh /tmp/sandbox-script.sh";
            var result = await RunDockerAsync(execCmd);
            sw.Stop();

            return new CommandResult
            {
                ExitCode = result.ExitCode,
                Output = result.Stdout,
                Error = result.Stderr,
                ExecutionTimeMs = (int)sw.ElapsedMilliseconds
            };
        }
        finally
        {
            try { File.Delete(tempScript); } catch { /* ignore */ }
        }
    }

    /// <inheritdoc/>
    public async Task<SandboxInfo> GetSandboxInfoAsync(
        string sandboxId, CancellationToken ct = default)
    {
        if (!_instances.TryGetValue(sandboxId, out var instance))
            throw new InvalidOperationException($"沙箱 {sandboxId} 不存在");

        // docker inspect 获取容器 IP 和端口映射
        var result = await RunDockerAsync(
            $"inspect --format '{{{{.NetworkSettings.IPAddress}}}}' {instance.ContainerId}");
        var host = result.ExitCode == 0 ? result.Stdout.Trim() : "localhost";

        var port = instance.Config?.Port ?? 8080;
        var previewPort = instance.Config?.PreviewPort ?? 4173;

        // 查询预览端口实际映射
        var previewPortResult = await RunDockerAsync(
            $"port {instance.ContainerId} {previewPort}");
        var previewHostPort = previewPortResult.ExitCode == 0
            ? previewPortResult.Stdout.Trim().Split(':').Last().Trim()
            : "0";

        return new SandboxInfo
        {
            SandboxId = sandboxId,
            Host = host,
            Port = port,
            ApiUrl = $"http://{host}:5000",
            FrontendUrl = $"http://{host}:3000",
            PreviewUrl = $"http://{host}:{previewHostPort}",
            DbConnectionString = instance.DbConnectionString ?? ""
        };
    }

    // ─── Private helpers ───

    private async Task EnsureNetworkAsync()
    {
        var result = await RunDockerAsync($"network inspect {DockerNetwork}");
        if (result.ExitCode != 0)
        {
            await RunDockerAsync($"network create {DockerNetwork}");
            _logger.LogInformation("创建 Docker 网络: {Network}", DockerNetwork);
        }
    }

    private string BuildConnectionString(string dbName)
    {
        // 从配置读取 SQL Server 连接模板
        var template = _configuration.GetValue<string>("Sandbox:ConnectionStringTemplate")
            ?? $"Server={DbServerHost},1433;Database={{DB}};User Id=sa;Password=YourPassword123;TrustServerCertificate=True;";
        return template.Replace("{DB}", dbName);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDockerAsync(
        string arguments, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("无法启动 docker 进程");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}
