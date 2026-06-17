namespace JNPF.InteAssistant.Studio;

/// <summary>
/// TOTP 验证请求
/// </summary>
public class TotpVerifyInput
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// TOTP 验证响应
/// </summary>
public class TotpVerifyOutput
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
}
