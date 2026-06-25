using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 创始人认证日志
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_FOUNDER_AUTH_LOG", TableDescription = "创始人认证日志")]
public class FounderAuthLogEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTION")]
    public string Action { get; set; }

    /// <summary>
    /// 认证结果
    /// allow / deny / not_found
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULT")]
    public string Result { get; set; }

    /// <summary>
    /// 请求 IP
    /// </summary>
    [SugarColumn(ColumnName = "F_IP_ADDRESS")]
    public string IpAddress { get; set; }

    /// <summary>
    /// 浏览器 User-Agent
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_AGENT")]
    public string UserAgent { get; set; }

    /// <summary>
    /// 设备指纹（SHA256(IP + UA + Salt)）
    /// 用于跨 Session 识别创始人设备，异常设备告警
    /// </summary>
    [SugarColumn(ColumnName = "F_DEVICE_FINGERPRINT")]
    public string DeviceFingerprint { get; set; }
}
