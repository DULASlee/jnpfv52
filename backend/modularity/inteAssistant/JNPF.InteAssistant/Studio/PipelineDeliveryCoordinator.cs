using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 交付阶段：沙箱预览 + 源码 ZIP（从 AIDevelopmentPipelineService 抽取，供阶段确认主链调用）。
/// </summary>
public interface IPipelineDeliveryCoordinator
{
    Task RunPreviewAndPackageAsync(long pipelineId, string tenantId, CancellationToken ct = default);
}

public sealed class PipelineDeliveryCoordinator : IPipelineDeliveryCoordinator, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ISandboxManager _sandbox;
    private readonly IConfiguration _configuration;
    private readonly IGeneratedProjectRegistry _generatedProjectRegistry;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly ILogger<PipelineDeliveryCoordinator> _logger;

    public PipelineDeliveryCoordinator(
        ISqlSugarClient db,
        ISandboxManager sandbox,
        IConfiguration configuration,
        IGeneratedProjectRegistry generatedProjectRegistry,
        IPipelineSseChannelHub sseHub,
        ILogger<PipelineDeliveryCoordinator> logger)
    {
        _db = db;
        _sandbox = sandbox;
        _configuration = configuration;
        _generatedProjectRegistry = generatedProjectRegistry;
        _sseHub = sseHub;
        _logger = logger;
    }

    public async Task RunPreviewAndPackageAsync(long pipelineId, string tenantId, CancellationToken ct = default)
    {
        var pipelineIdStr = pipelineId.ToString();
        var tenantIdStr = tenantId;
        var projectId = await ResolveProjectIdAsync(pipelineId, ct);

        try
        {
            PushDeployProgress(pipelineId, "running", 82, "正在启动沙箱预览…");
            await TryStartPreviewAsync(pipelineId, tenantIdStr, projectId, pipelineIdStr, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "沙箱预览失败，继续尝试打包: PipelineId={Id}", pipelineId);
            PushDeployProgress(pipelineId, "running", 90, $"预览启动失败，继续打包：{ex.Message}");
        }

        try
        {
            PushDeployProgress(pipelineId, "running", 92, "正在打包源码 ZIP…");
            var zipPath = StudioWorkspaceHelper.CreateDeliveryZip(tenantIdStr, projectId, pipelineIdStr);
            StudioWorkspaceHelper.ClearAiDevContext();
            var downloadUrl = $"/api/file/download?path={Uri.EscapeDataString(zipPath)}";
            await _generatedProjectRegistry.UpdateDeliveryArtifactsAsync(pipelineId, null, downloadUrl);
            PushDeployProgress(pipelineId, "running", 98, "源码包已生成");
            _logger.LogInformation("交付包已生成: PipelineId={Id}, Path={Path}", pipelineId, zipPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "交付打包失败: PipelineId={Id}", pipelineId);
            throw;
        }
    }

    private void PushDeployProgress(long pipelineId, string phase, int percent, string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            skillId = DeploySkillIds.Deploy,
            phase,
            percent,
            message,
        });
        _sseHub.TryPush(pipelineId, SseEventType.SkillProgress, payload);
    }

    private async Task TryStartPreviewAsync(
        long pipelineId, string tenantIdStr, string projectId, string pipelineIdStr, CancellationToken ct)
    {
        var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(tenantIdStr, projectId, pipelineIdStr);

        if (!Directory.Exists(generatedDir)
            || !Directory.GetFiles(generatedDir, "*.vue", SearchOption.AllDirectories).Any())
        {
            throw Oops.Bah("无可预览的前端文件：请先在 development 阶段生成 Vue 代码");
        }

        var previewProjectDir = _configuration.GetValue<string>("StudioPreview:ProjectPath")
            ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "studio-preview"));

        if (!Directory.Exists(previewProjectDir))
            throw Oops.Bah($"壳工程不存在: {previewProjectDir}");

        StudioWorkspaceHelper.InjectFrontendFiles(generatedDir, previewProjectDir);

        var sandboxId = $"pipeline-{pipelineId}";
        var sandboxCreated = false;
        var sandbox = await _sandbox.GetStatusAsync(sandboxId);

        if (sandbox == null || sandbox.Status is "destroyed" or "error")
        {
            sandbox = await _sandbox.CreateAsync(new SandboxConfig
            {
                Id = sandboxId,
                TenantId = tenantIdStr,
                CpuLimit = 2,
                MemoryLimit = "4Gi",
                TimeoutSeconds = 600,
                Port = 8080,
                PreviewPort = 4173,
                Image = _configuration.GetValue<string>("Sandbox:Image") ?? "jnpf-sandbox:latest",
            });
            sandboxCreated = true;
        }

        try
        {
            var projectFiles = StudioWorkspaceHelper.ReadFilesFromDirectory(previewProjectDir);
            await _sandbox.UploadFilesAsync(sandboxId, projectFiles);

            using var npmCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            npmCts.CancelAfter(TimeSpan.FromSeconds(120));
            PushDeployProgress(pipelineId, "running", 85, "沙箱内 npm install…");
            var installResult = await _sandbox.ExecuteCommandAsync(
                sandboxId, "cd /app && npm install --prefer-offline 2>&1 | tail -5", npmCts.Token);

            if (installResult.ExitCode != 0)
                throw Oops.Bah($"npm install 失败: {installResult.Error}");

            PushDeployProgress(pipelineId, "running", 88, "正在启动 Vite 预览服务…");
            await _sandbox.ExecuteCommandAsync(sandboxId, "cd /app && nohup npx vite --port 4173 --host > /tmp/vite.log 2>&1 &");

            var ready = false;
            for (var i = 0; i < 15; i++)
            {
                await Task.Delay(2000, ct);
                PushDeployProgress(pipelineId, "running", 88 + Math.Min(i, 3),
                    $"等待预览服务就绪（{i + 1}/15）…");
                var check = await _sandbox.ExecuteCommandAsync(
                    sandboxId, "curl -s -o /dev/null -w '%{http_code}' http://localhost:4173");
                if (check.ExitCode == 0 && check.Output.Trim() == "200")
                {
                    ready = true;
                    break;
                }
            }

            if (!ready)
                throw Oops.Bah("Vite dev server 启动超时（30s）");

            var sandboxInfo = await _sandbox.GetSandboxInfoAsync(sandboxId);
            var previewUrl = sandboxInfo.PreviewUrl;

            _sseHub.TryPush(pipelineId, SseEventType.PreviewReady, JsonSerializer.Serialize(new
            {
                previewUrl,
                sandboxId,
                status = "running",
            }));
            PushDeployProgress(pipelineId, "running", 91, $"试用环境已就绪：{previewUrl}");

            await _generatedProjectRegistry.UpdateDeliveryArtifactsAsync(pipelineId, previewUrl, null);
            _logger.LogInformation("预览就绪: PipelineId={Id}, Url={Url}", pipelineId, previewUrl);
        }
        catch
        {
            if (sandboxCreated)
            {
                try { await _sandbox.DestroyAsync(sandboxId); }
                catch (Exception destroyEx)
                {
                    _logger.LogWarning(destroyEx, "预览失败，沙箱销毁异常: {SandboxId}", sandboxId);
                }
            }

            throw;
        }
    }

    private async Task<string> ResolveProjectIdAsync(long pipelineId, CancellationToken ct)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => new { x.ProjectId })
            .FirstAsync(ct);

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        return string.IsNullOrWhiteSpace(pipeline.ProjectId)
            ? pipelineId.ToString()
            : pipeline.ProjectId;
    }
}
