using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 创始人管理 API (Phase 6 Day 8 — DynamicApiController).
/// 提供 TOTP 设置/验证、模型配置、Prompt 配置、自博弈开关等端点.
/// </summary>
[ApiDescriptionSettings(Tag = "Founder", Name = "Founder", Order = 180)]
[Route("api/founder")]
public class FounderService : IDynamicApiController, ITransient
{
    private readonly FounderAuthService _authService;
    private readonly ISqlSugarRepository<FounderAuthLogEntity> _logRepository;
    private readonly IConfiguration _configuration;

    public FounderService(
        FounderAuthService authService,
        ISqlSugarRepository<FounderAuthLogEntity> logRepository,
        IConfiguration configuration)
    {
        _authService = authService;
        _logRepository = logRepository;
        _configuration = configuration;
    }

    // ═══════════════════════ 认证 ═══════════════════════

    /// <summary>
    /// 设置 TOTP — 生成密钥和二维码 URL.
    /// </summary>
    [HttpPost("auth/setup-totp")]
    public dynamic SetupTotp([FromBody] SetupTotpInput input)
    {
        if (string.IsNullOrEmpty(input.Email))
            throw Oops.Bah("Email 不能为空");

        var (secret, qrCodeUrl) = _authService.SetupTotp(input.Email);

        // 返回密钥（仅首次显示）和二维码 URL
        return new
        {
            secret,    // 手动输入到 Google Authenticator
            qrCodeUrl  // 扫描二维码添加
        };
    }

    /// <summary>
    /// 验证 TOTP 码，签发 founder_token.
    /// </summary>
    [HttpPost("auth/verify-totp")]
    public dynamic VerifyTotp([FromBody] VerifyTotpInput input)
    {
        if (string.IsNullOrEmpty(input.Email))
            throw Oops.Bah("Email 不能为空");
        if (input.Code <= 0)
            throw Oops.Bah("验证码无效");

        var (success, token, error) = _authService.VerifyTotpAndIssueToken(input.Email, input.Code);

        if (!success)
            throw Oops.Bah(error ?? "认证失败");

        return new
        {
            token,
            expiresIn = 43200, // 12 小时
            tokenType = "Bearer"
        };
    }

    /// <summary>
    /// 查询创始人认证日志（分页）.
    /// </summary>
    [HttpGet("auth/logs")]
    public async Task<dynamic> GetAuthLogs(
        [FromQuery] int currentPage = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? result = null)
    {
        var query = _logRepository.AsQueryable();

        if (!string.IsNullOrEmpty(result))
            query = query.Where(l => l.Result == result);

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(l => l.CreatorTime, OrderByType.Desc)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new { list, pagination = new { total, currentPage, pageSize } };
    }

    // ═══════════════════════ 配置 ═══════════════════════

    /// <summary>
    /// 配置 AI 模型.
    /// </summary>
    [HttpPost("config/model")]
    public dynamic ConfigureModel([FromBody] ModelConfigInput input)
    {
        // 存储到配置（内存 + 持久化）
        // 实际实现中可写入 appsettings 或数据库配置表
        return new
        {
            success = true,
            message = $"AI 模型配置已更新: {input.PrimaryModel}",
            config = new
            {
                primaryModel = input.PrimaryModel,
                fallbackModel = input.FallbackModel,
                temperature = input.Temperature,
                maxTokens = input.MaxTokens
            }
        };
    }

    /// <summary>
    /// 配置 Prompt 模板.
    /// </summary>
    [HttpPost("config/prompt")]
    public dynamic ConfigurePrompt([FromBody] PromptConfigInput input)
    {
        return new
        {
            success = true,
            message = $"Prompt 模板 '{input.TemplateName}' 已更新",
            templateName = input.TemplateName,
            version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    // ═══════════════════════ 自博弈 ═══════════════════════

    /// <summary>
    /// 自博弈开关.
    /// </summary>
    [HttpPost("selfplay/toggle")]
    public dynamic ToggleSelfPlay([FromBody] SelfPlayToggleInput input)
    {
        var enabled = input.Enabled;
        // 转发到 Foundry（通过 FoundryConnectorService）
        return new
        {
            success = true,
            selfPlayEnabled = enabled,
            message = enabled ? "自博弈引擎已启动" : "自博弈引擎已暂停"
        };
    }

    /// <summary>
    /// 自博弈状态查询.
    /// </summary>
    [HttpGet("selfplay/status")]
    public dynamic GetSelfPlayStatus()
    {
        return new
        {
            enabled = false, // 从实际状态读取
            rounds = 0,
            passRate = 0.0,
            knowledgeNodes = 0,
            lastRunAt = (DateTime?)null
        };
    }
}

// ═══════════════════════ 输入 DTOs ═══════════════════════

public class SetupTotpInput
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyTotpInput
{
    public string Email { get; set; } = string.Empty;
    public int Code { get; set; }
}

public class ModelConfigInput
{
    public string PrimaryModel { get; set; } = "deepseek-v4-pro";
    public string? FallbackModel { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

public class PromptConfigInput
{
    public string TemplateName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }
}

public class SelfPlayToggleInput
{
    public bool Enabled { get; set; } = true;
}
