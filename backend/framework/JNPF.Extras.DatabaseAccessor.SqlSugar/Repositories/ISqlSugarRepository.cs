using System.Linq.Expressions;

namespace SqlSugar;

/// <summary>
/// SqlSugar 仓储接口定义（ADR-012：含租户安全写操作）.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public partial interface ISqlSugarRepository<TEntity> : ISimpleClient<TEntity>
    where TEntity : class, new()
{
    // ─── ADR-012: 租户安全写操作 ───

    Task<bool> SafeUpdateAsync(TEntity entity);
    Task<bool> SafeUpdateAsync(List<TEntity> entities);
    Task<bool> SafeUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> columns);
    Task<bool> SafeUpdateAsync(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TEntity>> columns);
    Task<bool> SafeDeleteAsync(TEntity entity);
    Task<bool> SafeDeleteAsync(Expression<Func<TEntity, bool>> where);
    Task<bool> SafeDeleteByIdAsync(object id);
    Task<bool> SafeDeleteByIdsAsync(IEnumerable<object> ids);
    Task<int> SafeInsertAsync(TEntity entity);
    Task<int> SafeInsertAsync(List<TEntity> entities);
    Task<long> SafeInsertReturnSnowflakeIdAsync(TEntity entity);
    Task<TEntity> SafeInsertReturnEntityAsync(TEntity entity);
}
