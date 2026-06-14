using System.Security.Cryptography;
using System.Text;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace JNPF.InteAssistant;

/// <summary>
/// TOTP 双因素认证服务 (Phase 6 Day 7).
/// RFC 6238 — 基于时间的一次性密码.
/// HMAC-SHA1, 6 位数字, 30 秒步长.
/// </summary>
public sealed class TotpService : ITransient
{
    private readonly IConfiguration _configuration;
    private const int DigitCount = 6;
    private const int PeriodSeconds = 30;
    private const int WindowSize = 1; // 允许前后 1 个时间窗口

    public TotpService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 生成 Base32 编码的 TOTP 密钥.
    /// </summary>
    public string GenerateSecret()
    {
        var bytes = new byte[20]; // 160 bits
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    /// <summary>
    /// 生成 Google Authenticator 兼容的二维码 URL.
    /// </summary>
    public string GetQrCodeUrl(string secret, string email)
    {
        var issuer = _configuration.GetValue<string>("App:FounderTotpIssuer", "JNPF-Founder");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={DigitCount}&period={PeriodSeconds}";
    }

    /// <summary>
    /// 验证 TOTP 码（支持前后窗口容差）.
    /// </summary>
    public bool ValidateCode(string secret, int code)
    {
        if (string.IsNullOrEmpty(secret) || code < 0 || code > 999999)
            return false;

        var keyBytes = Base32Decode(secret);
        if (keyBytes.Length == 0)
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var counter = now / PeriodSeconds;

        // 检查当前窗口 + 前后各 WindowSize 个窗口
        for (long offset = -WindowSize; offset <= WindowSize; offset++)
        {
            var expectedCode = ComputeTotp(keyBytes, counter + offset);
            if (expectedCode == code)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 计算给定计数器的 TOTP 值.
    /// </summary>
    private static int ComputeTotp(byte[] key, long counter)
    {
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        // 动态截断 (RFC 4226 §5.3)
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return binary % (int)Math.Pow(10, DigitCount);
    }

    // ─── Base32 编解码 ───

    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string Base32Encode(byte[] data)
    {
        if (data.Length == 0) return string.Empty;

        var bits = 0;
        var value = 0;
        var output = new StringBuilder();

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                output.Append(Base32Chars[(value >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
            output.Append(Base32Chars[(value << (5 - bits)) & 0x1F]);

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.TrimEnd('=').ToUpperInvariant();
        var result = new List<byte>();
        var bits = 0;
        var value = 0;

        foreach (var c in input)
        {
            var index = Base32Chars.IndexOf(c);
            if (index < 0) continue;

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                result.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return result.ToArray();
    }
}
