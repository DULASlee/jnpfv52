using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using JNPF.Schedule;
using System.Text.Json;

namespace JNPF.Schedule;

/// <summary>
/// 作业执行器 — 租户传播.
/// 在 Job 执行前从 JobDetail.Properties 提取 TenantId 并设置租户上下文.
/// ADR-013: 非 HTTP 上下文传播拦截器.
/// </summary>
public class TenantPropagationJobExecutor : IJobExecutor
{
    public async Task ExecuteAsync(JobExecutingContext context, IJob jobHandler, CancellationToken stoppingToken)
    {
        // 从 JobDetail.Properties 提取 TenantId
        var tenantId = ExtractTenantId(context.JobDetail);

        if (!string.IsNullOrEmpty(tenantId))
        {
            TenantContextImpl.SetTenant(tenantId);
        }

        try
        {
            await jobHandler.ExecuteAsync(context, stoppingToken);
        }
        finally
        {
            // 铁律：必须清除，防止线程池复用导致的幽灵租户
            TenantContextImpl.ClearCurrent();
        }
    }

    /// <summary>
    /// 从 JobDetail.Properties JSON 提取 TenantId.
    /// </summary>
    private static string? ExtractTenantId(JobDetail jobDetail)
    {
        // 从 Properties JSON 解析
        if (string.IsNullOrWhiteSpace(jobDetail.Properties) || jobDetail.Properties == "{}")
            return null;

        try
        {
            using var doc = JsonDocument.Parse(jobDetail.Properties);
            if (doc.RootElement.TryGetProperty("tenantId", out var tenantIdElement))
            {
                return tenantIdElement.GetString();
            }
            if (doc.RootElement.TryGetProperty("TenantId", out var tenantIdUpperElement))
            {
                return tenantIdUpperElement.GetString();
            }
        }
        catch (JsonException)
        {
            // JSON 解析失败，降级为无租户
        }

        return null;
    }
}
