using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlSugar;

/// <summary>
/// SqlSugar 仓储实现类（ADR-012：租户安全兜底）.
/// SimpleClient 的 CRUD 方法非 virtual，无法直接 override。
/// 通过 Safe* 方法提供带租户保护的写操作，DataExecuting AOP 自动填充读操作。
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public partial class SqlSugarRepository<TEntity> : SimpleClient<TEntity>, ISqlSugarRepository<TEntity>
    where TEntity : class, new()
{
    private readonly ITenantContext? _tenantContext;

    /// <summary>
    /// TenantId PropertyInfo 缓存，避免每个实体写操作都反射查找
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _tenantIdPropertyCache = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContextProvider">数据库上下文提供器（负责租户解析、系统过滤、AOP 配置）</param>
    /// <param name="tenantContext">租户上下文（ADR-012：写操作安全兜底）</param>
    public SqlSugarRepository(
        ISqlSugarDbContextProvider dbContextProvider,
        ITenantContext? tenantContext = null)
        : base(dbContextProvider.GetDbContext())
    {
        _tenantContext = tenantContext;
    }

    // ─── ADR-012: 租户安全写操作 ───

    /// <summary>
    /// 安全更新 — 自动附加 WHERE TenantId 条件，防止跨租户修改.
    /// </summary>
    public async Task<bool> SafeUpdateAsync(TEntity entity)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Updateable(entity).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Updateable(entity)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全更新（批量） — 自动附加 WHERE TenantId 条件.
    /// </summary>
    public async Task<bool> SafeUpdateAsync(List<TEntity> entities)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Updateable(entities).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Updateable(entities)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全更新（指定列） — 仅更新指定列，附加 TenantId 条件.
    /// </summary>
    public async Task<bool> SafeUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> columns)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Updateable(entity).UpdateColumns(columns).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Updateable(entity)
            .UpdateColumns(columns)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全更新（Where 表达式） — 合并租户条件.
    /// </summary>
    public async Task<bool> SafeUpdateAsync(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TEntity>> columns)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Updateable(columns).Where(where).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        var tenantWhere = CombineWithTenantFilter(where, tenantId);
        return await Context.Updateable(columns).Where(tenantWhere).ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全删除 — 自动附加 WHERE TenantId 条件，防止跨租户删除.
    /// </summary>
    public async Task<bool> SafeDeleteAsync(TEntity entity)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Deleteable(entity).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Deleteable(entity)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全删除（Where 表达式） — 合并租户条件.
    /// </summary>
    public async Task<bool> SafeDeleteAsync(Expression<Func<TEntity, bool>> where)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Deleteable(where).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        var tenantWhere = CombineWithTenantFilter(where, tenantId);
        return await Context.Deleteable(tenantWhere).ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全删除（主键） — 附加 TenantId 条件.
    /// </summary>
    public async Task<bool> SafeDeleteByIdAsync(object id)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Deleteable<TEntity>().In(id).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Deleteable<TEntity>()
            .In(id)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全删除（主键集合） — 附加 TenantId 条件.
    /// </summary>
    public async Task<bool> SafeDeleteByIdsAsync(IEnumerable<object> ids)
    {
        if (!ShouldApplyTenantProtection())
            return await Context.Deleteable<TEntity>().In(ids).ExecuteCommandHasChangeAsync();

        var tenantId = _tenantContext!.TenantId;
        return await Context.Deleteable<TEntity>()
            .In(ids)
            .Where($"{GetTenantIdColumnName()}=@tid", new { tid = tenantId })
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 安全插入 — 仅在 TenantId 为空时自动设置，不覆盖已有的值.
    /// </summary>
    public async Task<int> SafeInsertAsync(TEntity entity)
    {
        if (ShouldApplyTenantProtection() && !HasTenantId(entity))
            SetTenantId(entity, _tenantContext!.TenantId);
        return await Context.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 安全插入（批量） — 仅在 TenantId 为空时自动设置.
    /// </summary>
    public async Task<int> SafeInsertAsync(List<TEntity> entities)
    {
        if (ShouldApplyTenantProtection())
        {
            var tenantId = _tenantContext!.TenantId;
            foreach (var entity in entities)
            {
                if (!HasTenantId(entity))
                    SetTenantId(entity, tenantId);
            }
        }
        return await Context.Insertable(entities).ExecuteCommandAsync();
    }

    /// <summary>
    /// 安全插入并返回自增 ID.
    /// </summary>
    public async Task<long> SafeInsertReturnSnowflakeIdAsync(TEntity entity)
    {
        if (ShouldApplyTenantProtection() && !HasTenantId(entity))
            SetTenantId(entity, _tenantContext!.TenantId);
        return await Context.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 安全插入并返回实体.
    /// </summary>
    public async Task<TEntity> SafeInsertReturnEntityAsync(TEntity entity)
    {
        if (ShouldApplyTenantProtection() && !HasTenantId(entity))
            SetTenantId(entity, _tenantContext!.TenantId);
        return await Context.Insertable(entity).ExecuteReturnEntityAsync();
    }

    // ─── 私有辅助方法 ───

    /// <summary>
    /// 判断是否需要应用租户保护.
    /// 条件：多租户模式 + 非默认租户 + 实体实现 ITenantFilter.
    /// </summary>
    private bool ShouldApplyTenantProtection()
    {
        if (_tenantContext == null)
            return false;
        if (!_tenantContext.IsMultiTenant || _tenantContext.IsDefaultTenant())
            return false;
        if (string.IsNullOrEmpty(_tenantContext.TenantId))
            return false;
        return typeof(ITenantFilter).IsAssignableFrom(typeof(TEntity));
    }

    /// <summary>
    /// 检查实体的 TenantId 属性是否已有值.
    /// </summary>
    private static bool HasTenantId(TEntity entity)
    {
        var prop = _tenantIdPropertyCache.GetOrAdd(typeof(TEntity), t =>
            t.GetProperty("TenantId", BindingFlags.Public | BindingFlags.Instance));
        if (prop == null || prop.PropertyType != typeof(string))
            return false;
        var value = prop.GetValue(entity) as string;
        return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 通过反射设置实体的 TenantId 属性.
    /// </summary>
    private static void SetTenantId(TEntity entity, string tenantId)
    {
        var prop = _tenantIdPropertyCache.GetOrAdd(typeof(TEntity), t =>
            t.GetProperty("TenantId", BindingFlags.Public | BindingFlags.Instance));
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
        {
            prop.SetValue(entity, tenantId);
        }
    }

    /// <summary>
    /// 将用户 Where 表达式与 TenantId 条件合并（AND）.
    /// 使用参数替换（Replace）而非 Invoke，确保 SqlSugar 能正确翻译.
    /// </summary>
    private static Expression<Func<TEntity, bool>> CombineWithTenantFilter(
        Expression<Func<TEntity, bool>> userWhere, string tenantId)
    {
        var param = Expression.Parameter(typeof(TEntity), "it");
        var tenantProp = Expression.Property(param, "TenantId");
        var tenantValue = Expression.Constant(tenantId);
        var tenantEqual = Expression.Equal(tenantProp, tenantValue);

        // 替换用户表达式中的参数引用，使其指向统一的 param
        var replacedBody = new ParameterReplacer(userWhere.Parameters[0], param)
            .Visit(userWhere.Body);
        var body = Expression.AndAlso(replacedBody!, tenantEqual);

        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    /// <summary>
    /// 获取 TenantId 对应的数据库列名（用于原始 SQL 的 WHERE 子句）.
    /// </summary>
    private string GetTenantIdColumnName()
    {
        return Context.EntityMaintenance.GetDbColumnName<TEntity>("TenantId");
    }
}

/// <summary>
/// 表达式树参数替换器 — 将表达式中的旧参数替换为新参数.
/// 用于 CombineWithTenantFilter 合并用户表达式与租户条件.
/// </summary>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly ParameterExpression _newParam;

    public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
    {
        _oldParam = oldParam;
        _newParam = newParam;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
