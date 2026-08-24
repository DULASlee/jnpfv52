using JNPF.Systems.Entitys.Permission;
// 注：GlobalTenantCacheModel 为平台自有模型（JNPF.Extras.DatabaseAccessor.SqlSugar/Models），
// 历史命名落在 SqlSugar 命名空间，非 ORM 库类型；编译层零 ORM 验收对象为 RunSqlCompiler.cs。

namespace JNPF.VisualDev.Runtime;

using SqlSugar; // GlobalTenantCacheModel 平台模型所在命名空间（见顶部注释）

/// <summary>
/// 用户选择类默认值元数据（GetVisualDevModelDataConfig 惰性供数载体）.
/// </summary>
public sealed class UserSelectDefaults
{
    /// <summary>
    /// 当前用户Id.
    /// </summary>
    public string UserId { get; init; }

    /// <summary>
    /// 用户主部门Id.
    /// </summary>
    public string DepId { get; init; }

    /// <summary>
    /// 用户岗位Ids.
    /// </summary>
    public List<string> PosIds { get; init; }

    /// <summary>
    /// 用户角色Ids.
    /// </summary>
    public List<string> RoleIds { get; init; }

    /// <summary>
    /// 用户分组Ids.
    /// </summary>
    public List<string> GroupIds { get; init; }

    /// <summary>
    /// 全量用户关系（默认值绑定辅助用）.
    /// </summary>
    public List<UserRelationEntity> AllUserRelationList { get; init; }

    /// <summary>
    /// 用户首选岗位Id.
    /// </summary>
    public string PositionId { get; init; }
}

/// <summary>
/// 编译层供数网关（Task 3.3 裁决 C；替代裁决 A 过渡载体 RunSqlCompileContext）.
/// 职责：RunSqlCompiler 零 SqlSugar/零 DI 前提下，DB 读取与 SQL 渲染能力由调用方
/// （RunService）以委托/数据形式供入；本类及成员类型零 SqlSugar 引用.
/// 纪律：惰性成员（UserSelectDefaults/渲染委托）仅在原方法对应分支触达时调用，行为不变.
/// </summary>
public sealed class RunSqlCompileGateway
{
    /// <summary>
    /// 请求端类型（"pc"/其它），决定列配置选择.
    /// </summary>
    public string UserOrigin { get; init; }

    /// <summary>
    /// 多租户开关.
    /// </summary>
    public bool MultiTenancy { get; init; }

    /// <summary>
    /// 当前模板数据连接对应的租户缓存（调用侧按 DbLink.Id 预查）.
    /// </summary>
    public GlobalTenantCacheModel TenantCache { get; init; }

    /// <summary>
    /// 列存在判定（绑定模板数据连接）：(表名, 列名) → 是否存在.
    /// </summary>
    public Func<string, string, bool> ColumnExists { get; init; }

    /// <summary>
    /// 条件 JSON → 平台条件模型列表（调用侧经 Utilities 解析后转换）.
    /// </summary>
    public Func<string, List<ICompileConditionalModel>> JsonToConditions { get; init; }

    /// <summary>
    /// 外部源条件渲染：平台条件 → WHERE 片段（调用侧绑定模板数据连接，切库/复位语义不变）.
    /// </summary>
    public Func<List<ICompileConditionalModel>, string> RenderLinkWhere { get; init; }

    /// <summary>
    /// 主库条件渲染：平台条件 → WHERE 片段（SqlQueryable("@") 形态）.
    /// </summary>
    public Func<List<ICompileConditionalModel>, string> RenderDefaultWhere { get; init; }

    /// <summary>
    /// 已拼 SQL 追加条件渲染：(基础 SQL, 平台条件) → 完整 SQL.
    /// </summary>
    public Func<string, List<ICompileConditionalModel>, string> RenderSqlWhere { get; init; }

    /// <summary>
    /// USERSSELECT 用户关系解析（原始 Id 列表 → 关系实体；"--user" 规约归调用侧）.
    /// </summary>
    public Func<List<string>, List<UserRelationEntity>> ResolveUserRelations { get; init; }

    /// <summary>
    /// 用户选择类默认值元数据（惰性：仅在模板含对应默认值控件分支调用）.
    /// </summary>
    public Func<UserSelectDefaults> UserSelectDefaults { get; init; }
}
