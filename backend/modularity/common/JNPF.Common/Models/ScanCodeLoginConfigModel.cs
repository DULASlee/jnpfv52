namespace JNPF.Common.Models;

/// <summary>
/// 通用扫码登录模型.
/// </summary>
public class ScanCodeLoginConfigModel
{
    /// <summary>
    /// 状态.
    /// </summary>
    public int status = 0;

    /// <summary>
    /// 额外的值, 登录Token.
    /// </summary>
    public string value;

    /// <summary>
    /// 前端主题.
    /// </summary>
    public string theme;

    /// <summary>
    /// 票据有效期, 时间戳.
    /// </summary>
    public long ticketTimeout;
}

public enum ScanCodeLoginTicketStatus
{
    /// <summary>
    /// 已失效.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// 未扫码.
    /// </summary>
    UnScanCode = 0,

    /// <summary>
    /// 已扫码.
    /// </summary>
    ScanCode = 1,

    /// <summary>
    /// 登录成功.
    /// </summary>
    Success = 2,
}