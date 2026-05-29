namespace SqlSugar;

/// <summary>
/// SqlSugar 仓储接口定义
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public partial interface ISqlSugarRepository<TEntity> : ISimpleClient<TEntity>
    where TEntity : class, new()
{
}