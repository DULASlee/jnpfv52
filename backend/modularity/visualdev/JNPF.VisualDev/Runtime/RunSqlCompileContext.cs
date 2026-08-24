using JNPF.Common.Core.Manager;
using JNPF.Common.Manager;
using JNPF.VisualDev.Entitys;
using SqlSugar;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// 编译层过渡依赖载体（Task 3.2 裁决 A，2026-08-24）.
/// 用途：RunSqlCompiler 构造零 DI 前提下，七方法逐字迁移所需依赖由调用方（RunService）供入.
/// 生命周期：Task 3.3 参数化剥离（DB 读取改数据参数传入）完成后移除；移除前禁止新增依赖成员.
/// </summary>
public sealed class RunSqlCompileContext
{
    /// <summary>
    /// SqlSugar 客户端（含 ChangeDataBase 外部源切换；写回等价原 RunService 字段语义，调用末均复位 default）.
    /// </summary>
    public SqlSugarScope SqlSugarClient { get; set; }

    /// <summary>
    /// 在线开发仓储（AsSugarClient 平台元数据查询 / Utilities 条件模型序列化）.
    /// </summary>
    public ISqlSugarRepository<VisualDevEntity> VisualDevRepository { get; init; }

    /// <summary>
    /// 数据库管理器（ChangeDataBase / IsAnyColumn）.
    /// </summary>
    public IDataBaseManager DataBaseManager { get; init; }

    /// <summary>
    /// 用户上下文（UserId / UserOrigin / User）.
    /// </summary>
    public IUserManager UserManager { get; init; }

    /// <summary>
    /// 缓存管理器（租户缓存）.
    /// </summary>
    public ICacheManager CacheManager { get; init; }

    /// <summary>
    /// 多租户配置.
    /// </summary>
    public TenantOptions Tenant { get; init; }
}
