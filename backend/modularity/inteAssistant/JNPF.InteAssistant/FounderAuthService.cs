using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace JNPF.InteAssistant;

/// <summary>
/// 创始人认证服务 (Phase 6 Day 6-8).
/// 管理 TOTP 设置、验证和 founder_token 签发.
/// </summary>
public sealed class FounderAuthService : ITransient
{
    private readonly IConfiguration _configuration;
    private readonly TotpService _totpService;
    private readonly IMemoryCache _cache;

    // founder_token 有效期 12 小时
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public FounderAuthService(
        IConfiguration configuration,
        TotpService totpService,
        IMemoryCache memoryCache)
    {
        _configuration = configuration;
        _totpService = totpService;
        _cache = memoryCache;
    }

    /// <summary>
    /// 设置 TOTP — 生成密钥和二维码 URL.
    /// </summary>
    public (string Secret, string QrCodeUrl) SetupTotp(string email)
    {
        var secret = _totpService.GenerateSecret();

        // 暂存到缓存（5 分钟窗口期用于验证）
        _cache.Set($"founder:totp:pending:{email}", secret, TimeSpan.FromMinutes(5));

        var qrCodeUrl = _totpService.GetQrCodeUrl(secret, email);
        return (secret, qrCodeUrl);
    }

    /// <summary>
    /// 验证 TOTP 码并签发 founder_token.
    /// </summary>
    public (bool Success, string? Token, string? Error) VerifyTotpAndIssueToken(string email, int code)
    {
        // 1. 从缓存获取待验证密钥
        var cacheKey = $"founder:totp:pending:{email}";
        if (!_cache.TryGetValue<string>(cacheKey, out var pendingSecret) || string.IsNullOrEmpty(pendingSecret))
        {
            // 也尝试从已生效密钥查找
            var activeKey = $"founder:totp:active:{email}";
            if (!_cache.TryGetValue<string>(activeKey, out var activeSecret) || string.IsNullOrEmpty(activeSecret))
                return (false, null, "TOTP 未设置或已过期，请重新设置");
            pendingSecret = activeSecret;
        }

        // 2. 验证 TOTP 码
        if (!_totpService.ValidateCode(pendingSecret, code))
            return (false, null, "TOTP 验证码无效");

        // 3. 验证通过，将密钥标记为已激活
        _cache.Set($"founder:totp:active:{email}", pendingSecret, TokenLifetime);
        _cache.Remove(cacheKey);

        // 4. 签发 founder_token (JWT)
        var token = IssueFounderToken(email);

        return (true, token, null);
    }

    /// <summary>
    /// 验证 founder_token 是否有效.
    /// </summary>
    public bool ValidateFounderToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var signingKey = GetFounderSigningKey();
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ValidateIssuer = true,
                ValidIssuer = "JNPF-Founder",
                ValidateAudience = true,
                ValidAudience = "founder-console",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从 founder_token 中提取用户邮箱.
    /// </summary>
    public string? ExtractEmailFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }
        catch
        {
            return null;
        }
    }

    // ─── Private ───

    private string IssueFounderToken(string email)
    {
        var signingKey = GetFounderSigningKey();
        var securityKey = new SymmetricSecurityKey(signingKey);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Founder"),
            new Claim("auth_method", "totp"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "JNPF-Founder",
            audience: "founder-console",
            claims: claims,
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private byte[] GetFounderSigningKey()
    {
        // 优先从配置读取，否则使用 AES Key 派生
        var configKey = _configuration.GetValue<string>("App:FounderJwtKey");
        if (!string.IsNullOrEmpty(configKey))
            return Encoding.UTF8.GetBytes(configKey);

        // 从现有 AES Key 派生
        var aesKey = _configuration.GetValue<string>("JNPF_App:AesKey", "EY8WePvjM5GGwQzn");
        var derived = SHA256.HashData(Encoding.UTF8.GetBytes(aesKey + "-founder-jwt"));
        return derived;
    }
}
