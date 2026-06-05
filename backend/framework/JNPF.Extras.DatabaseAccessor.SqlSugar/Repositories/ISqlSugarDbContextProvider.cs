namespace SqlSugar;

/// <summary>
/// SqlSugar 数据库上下文提供器接口
/// 职责：根据当前请求上下文（租户、系统）解析并返回已配置好过滤器和 AOP 的 ISqlSugarClient 实例
/// </summary>
public interface ISqlSugarDbContextProvider
{
    /// <summary>
    /// 获取当前请求对应的 SqlSugar 客户端
    /// </summary>
    /// <remarks>
    /// 内部完成以下工作：
    /// 1. 根据租户配置解析正确的数据库连接作用域（支持数据库隔离 / Schema 隔离 / 字段隔离）
    /// 2. 应用系统级数据过滤（ZxSystemId）
    /// 3. 配置 DataExecuting 回调实现字段自动填充
    /// 4. 配置通用 AOP（命令超时、SQL 日志、错误日志、Oracle 适配）
    /// </remarks>
    /// <returns>已完全配置的 ISqlSugarClient 实例</returns>
    ISqlSugarClient GetDbContext();
}
