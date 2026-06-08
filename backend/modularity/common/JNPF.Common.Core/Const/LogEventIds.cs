using Microsoft.Extensions.Logging;

namespace JNPF.Common.Core.Const;

/// <summary>
/// 日志事件 ID 常量 — 用于结构化日志过滤和统计
/// 按业务域分段：1xxx=认证, 2xxx=系统管理, 3xxx=工作流, 4xxx=数据操作, 9xxx=系统内部
/// </summary>
public static class LogEventIds
{
    // === 认证域 ===
    public static readonly EventId UserLogin = new(1001, "UserLogin");
    public static readonly EventId UserLogout = new(1002, "UserLogout");
    public static readonly EventId UserLoginFailed = new(1003, "UserLoginFailed");
    public static readonly EventId TokenRefreshed = new(1004, "TokenRefreshed");
    public static readonly EventId TokenRevoked = new(1005, "TokenRevoked");

    // === 系统管理域 ===
    public static readonly EventId UserCreated = new(2001, "UserCreated");
    public static readonly EventId UserUpdated = new(2002, "UserUpdated");
    public static readonly EventId UserDeleted = new(2003, "UserDeleted");
    public static readonly EventId RoleAssigned = new(2004, "RoleAssigned");
    public static readonly EventId PermissionChanged = new(2005, "PermissionChanged");
    public static readonly EventId DataExported = new(2006, "DataExported");

    // === 工作流域 ===
    public static readonly EventId WorkflowSubmitted = new(3001, "WorkflowSubmitted");
    public static readonly EventId WorkflowApproved = new(3002, "WorkflowApproved");
    public static readonly EventId WorkflowRejected = new(3003, "WorkflowRejected");

    // === 数据操作域 ===
    public static readonly EventId DiffLogRecorded = new(4001, "DiffLogRecorded");
    public static readonly EventId SlowQueryDetected = new(4002, "SlowQueryDetected");

    // === 系统内部 ===
    public static readonly EventId HealthCheckFailed = new(9001, "HealthCheckFailed");
    public static readonly EventId EventBusError = new(9002, "EventBusError");
    public static readonly EventId DiskSpaceWarning = new(9003, "DiskSpaceWarning");
}
