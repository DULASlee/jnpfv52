using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JNPF.Tests.Gate.Auth;

/// <summary>
/// 与 scripts/lib/jnpf-auth.mjs 一致的 OAuth 登录（MD5 + AES-ECB）
/// </summary>
public static class JnpfTestAuthHelper
{
    private const string DefaultCipherKey = "EY8WePvjM5GGwQzn";

    public static string EncryptPassword(string plainPassword, string cipherKey = DefaultCipherKey)
    {
        var md5Hex = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(plainPassword))).ToLowerInvariant();
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(cipherKey);
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(md5Hex);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToHexString(encrypted).ToLowerInvariant();
    }

    public static async Task<string> GetTokenAsync(
        HttpClient client,
        string account = "admin",
        string password = "123456",
        CancellationToken ct = default)
    {
        var envToken = Environment.GetEnvironmentVariable("JNPF_TEST_TOKEN");
        if (!string.IsNullOrWhiteSpace(envToken))
            return NormalizeToken(envToken);

        var sessionPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", ".jnpf-session.json"));
        if (File.Exists(sessionPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath, ct));
                if (doc.RootElement.TryGetProperty("token", out var tokenEl))
                {
                    var cached = NormalizeToken(tokenEl.GetString());
                    if (!string.IsNullOrEmpty(cached))
                        return cached;
                }
            }
            catch { /* fall through to login */ }
        }

        var encrypted = EncryptPassword(password);
        var form = new Dictionary<string, string>
        {
            ["account"] = account,
            ["password"] = encrypted,
            ["code"] = "",
            ["timestamp"] = "",
            ["origin"] = "password",
            ["grant_type"] = "password",
        };

        using var content = new FormUrlEncodedContent(form);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/oauth/Login") { Content = content };
        request.Headers.TryAddWithoutValidation("jnpf-origin", "pc");

        var response = await client.SendAsync(request, ct);
        var bodyText = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(bodyText);
        if (body.RootElement.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
            throw new InvalidOperationException($"Login failed: {bodyText}");

        var token = body.RootElement.GetProperty("data").GetProperty("token").GetString();
        return NormalizeToken(token ?? throw new InvalidOperationException("Login OK but no token"));
    }

    public static void SetBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
    }

    private static string NormalizeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token[7..].Trim()
            : token.Trim();
    }
}
