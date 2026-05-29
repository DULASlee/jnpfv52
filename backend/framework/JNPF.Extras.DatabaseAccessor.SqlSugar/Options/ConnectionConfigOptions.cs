namespace SqlSugar;

/// <summary>
/// 数据库连接字符串配置选项.
/// </summary>
public class ConnectionConfigOptions
{
    /// <summary>
    /// 配置ID.
    /// </summary>
    public string ConfigId { get; set; }

    /// <summary>
    /// 是否自定义配置.
    /// </summary>
    public bool IsCustom { get; set; }

    /// <summary>
    /// 隔离字段.
    /// </summary>
    public string IsolationField { get; set; }

    /// <summary>
    /// 是否主从分离.
    /// </summary>
    public bool IsMasterSlaveSeparation { get; set; }

    /// <summary>
    /// 配置列表.
    /// </summary>
    public List<DBConnectionConfig> ConfigList { get; set; }

    public DBConnectionConfig DefaultConfig
    {
        get
        {
            return ConfigList.FirstOrDefault();
        }
    }
}

/// <summary>
/// 数据库连接字符串配置.
/// </summary>
public class DBConnectionConfig
{
    /// <summary>
    /// 是否主库.
    /// </summary>
    public bool IsMaster { get; set; }

    /// <summary>
    /// 数据库名.
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// 数据库类型.
    /// </summary>
    public DbType dbType { get; set; }

    /// <summary>
    /// 自定义连接语句.
    /// </summary>
    public string connectionStr { get; set; }
}