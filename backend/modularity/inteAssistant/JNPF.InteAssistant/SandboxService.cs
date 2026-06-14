using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant;

/// <summary>
/// 沙箱管理 API (Phase 6 Day 4 — DynamicApiController).
/// </summary>
[ApiDescriptionSettings(Tag = "Sandbox", Name = "Sandbox", Order = 178)]
[Route("api/[controller]")]
public class SandboxService : IDynamicApiController, ITransient
{
    private readonly ISandboxManager _sandboxManager;

    public SandboxService(ISandboxManager sandboxManager)
    {
        _sandboxManager = sandboxManager;
    }

    /// <summary>
    /// 创建沙箱.
    /// </summary>
    [HttpPost("create")]
    public async Task<dynamic> Create([FromBody] SandboxCreateInput input)
    {
        if (string.IsNullOrEmpty(input.TenantId))
            throw Oops.Bah("TenantId 不能为空");

        var config = new SandboxConfig
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            TenantId = input.TenantId,
            CpuLimit = input.CpuLimit > 0 ? input.CpuLimit : 1,
            MemoryLimit = !string.IsNullOrEmpty(input.MemoryLimit) ? input.MemoryLimit : "4Gi",
            TimeoutSeconds = input.TimeoutSeconds > 0 ? input.TimeoutSeconds : 300,
            Image = !string.IsNullOrEmpty(input.Image) ? input.Image : "jnpf-sandbox:latest",
        };

        var instance = await _sandboxManager.CreateAsync(config);
        return instance;
    }

    /// <summary>
    /// 获取沙箱状态.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<dynamic> GetStatus(string id)
    {
        var instance = await _sandboxManager.GetStatusAsync(id);
        if (instance == null)
            throw Oops.Bah($"沙箱 {id} 不存在");

        return instance;
    }

    /// <summary>
    /// 销毁沙箱.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<dynamic> Destroy(string id)
    {
        await _sandboxManager.DestroyAsync(id);
        return new { success = true, message = $"沙箱 {id} 已销毁" };
    }

    /// <summary>
    /// 部署 zip 到沙箱.
    /// </summary>
    [HttpPost("{id}/deploy")]
    public async Task<dynamic> Deploy(string id)
    {
        // 从 multipart/form-data 读取 zip 文件
        var files = App.HttpContext.Request.Form.Files;
        if (files.Count == 0)
            throw Oops.Bah("请上传 zip 文件");

        var file = files[0];
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var zipBytes = ms.ToArray();

        await _sandboxManager.DeployAsync(id, zipBytes);
        return new { success = true, message = $"已部署到沙箱 {id}" };
    }

    /// <summary>
    /// 获取所有沙箱列表.
    /// </summary>
    [HttpGet("list")]
    public async Task<dynamic> List()
    {
        var instances = await _sandboxManager.GetAllAsync();
        return new { list = instances, total = instances.Count };
    }
}

/// <summary>
/// 沙箱创建请求.
/// </summary>
public class SandboxCreateInput
{
    public string TenantId { get; set; } = string.Empty;
    public int CpuLimit { get; set; } = 1;
    public string? MemoryLimit { get; set; }
    public int TimeoutSeconds { get; set; } = 300;
    public string? Image { get; set; }
}
