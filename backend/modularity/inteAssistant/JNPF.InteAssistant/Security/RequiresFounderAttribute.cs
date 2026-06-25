using System;

namespace JNPF.InteAssistant.Security;

/// <summary>
/// 标注方法需要创始人权限才能调用。
///
/// 用于 DisableQueryFilter 白名单：
///   只有标注了此特性的方法才被允许调用 ISqlSugarClient.QueryFilter.Disable()。
///   运行时 DisableQueryFilterGuard.Verify() 会双重校验：
///     1. 调用栈自检 — 调用方法必须标注此特性
///     2. 角色校验 — 当前 HTTP 用户必须是 founder 角色
///
/// 使用示例：
///   [RequiresFounder]
///   public async Task AdminPurgeExpiredData() {
///       DisableQueryFilterGuard.Verify(serviceProvider);
///       db.QueryFilter.Disable();  // ← 安全：已白名单 + 角色校验
///   }
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RequiresFounderAttribute : Attribute
{
    /// <summary>
    /// 可选：标注此操作的原因（记录到审计日志）
    /// </summary>
    public string? Reason { get; set; }
}
