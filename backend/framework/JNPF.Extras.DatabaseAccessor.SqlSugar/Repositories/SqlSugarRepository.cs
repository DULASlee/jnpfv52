using SqlSugar;

namespace SqlSugar;

/// <summary>
/// SqlSugar 仓储实现类
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public partial class SqlSugarRepository<TEntity> : SimpleClient<TEntity>, ISqlSugarRepository<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContextProvider">数据库上下文提供器（负责租户解析、系统过滤、AOP 配置）</param>
    public SqlSugarRepository(ISqlSugarDbContextProvider dbContextProvider)
        : base(dbContextProvider.GetDbContext())
    {
    }
}
