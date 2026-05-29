using JNPF;
using JNPF.LinqBuilder;

namespace SqlSugar;

/// <summary>
/// JNPF多租户拓展.
/// </summary>
public class JNPFTenantExtensions
{
    /// <summary>
    /// 获取普通链接.
    /// </summary>
    /// <param name="configId">配置ID.</param>
    /// <param name="tableName">数据库名称.</param>
    /// <param name="isolationField">隔离字段.</param>
    /// <returns></returns>
    public static ConnectionConfigOptions GetLinkToOrdinary(string configId, string tableName, string isolationField = null)
    {
        var defaultConnection = GetDbConnectionConfig(configId, tableName, null, isolationField);
        List<DBConnectionConfig> configList = new List<DBConnectionConfig>();
        configList.Add(new DBConnectionConfig()
        {
            IsMaster = true,
            ServiceName = tableName,
            dbType = defaultConnection.DbType,
            connectionStr = ToConnectionString(defaultConnection)
        });
        return new ConnectionConfigOptions()
        {
            ConfigId = configId,
            IsCustom = false,
            IsolationField = isolationField,
            IsMasterSlaveSeparation = false,
            ConfigList = configList
        };
    }

    /// <summary>
    /// 获取自定义链接
    /// </summary>
    /// <param name="configId">配置ID.</param>
    /// <param name="tenantLinkModels">数据库连接列表.</param>
    /// <returns></returns>
    public static ConnectionConfigOptions GetLinkToCustom(string configId, List<TenantLinkModel> tenantLinkModels)
    {
        List<DBConnectionConfig> configList = new List<DBConnectionConfig>();
        foreach (var item in tenantLinkModels)
        {
            var defaultConnection = GetDbConnectionConfig(configId, string.Empty, item);
            if (!string.IsNullOrEmpty(item.connectionStr))
            {
                configList.Add(new DBConnectionConfig()
                {
                    IsMaster = item.configType == 0,
                    dbType = ToDbType(item.dbType),
                    ServiceName = item.serviceName,
                    connectionStr = item.connectionStr,
                });
            }
            else
            {
                configList.Add(new DBConnectionConfig()
                {
                    IsMaster = true,
                    dbType = ToDbType(item.dbType),
                    connectionStr = ToConnectionString(defaultConnection)
                });
            }
        }
        return new ConnectionConfigOptions()
        {
            ConfigId = configId,
            IsCustom = true,
            IsMasterSlaveSeparation = tenantLinkModels.Any(it => it.configType.Equals(1)),
            ConfigList = configList
        };
    }

    /// <summary>
    /// 获取配置.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ConnectionConfig GetConfig(ConnectionConfigOptions options)
    {
        if (!options.IsCustom)
        {
            DBConnectionConfig config = options.ConfigList.FirstOrDefault();
            return new ConnectionConfig()
            {
                DbType = config.dbType,
                ConfigId = options.ConfigId,
                IsAutoCloseConnection = true,
                ConnectionString = config.connectionStr
            };
        }
        else
        {
            var slaveConnection = new List<SlaveConnectionConfig>();
            foreach (var item in options.ConfigList.FindAll(it => it.IsMaster.Equals(false)))
            {
                slaveConnection.Add(new SlaveConnectionConfig()
                {
                    HitRate = 10,
                    ConnectionString = item.connectionStr
                });
            }
            return new ConnectionConfig()
            {
                DbType = options.ConfigList.Find(it => it.IsMaster.Equals(true)).dbType,
                ConfigId = options.ConfigId,
                IsAutoCloseConnection = true,
                ConnectionString = options.ConfigList.Find(it => it.IsMaster.Equals(true)).connectionStr,
                SlaveConnectionConfigs = slaveConnection
            };
        }
    }

    /// <summary>
    /// 获取切换连接.
    /// </summary>
    /// <param name="configId"></param>
    /// <param name="dbName"></param>
    /// <param name="tenantLinkModel"></param>
    /// <returns></returns>
    public static DbConnectionConfig GetDbConnectionConfig(string configId, string dbName, TenantLinkModel tenantLinkModel, string isolationField = null)
    {
        if (!string.IsNullOrEmpty(dbName))
        {
            var dbOptions = App.GetOptions<ConnectionStringsOptions>();
            var defaultConnection = dbOptions.DefaultConnectionConfig;
            if (!"default".Equals(configId) && isolationField.IsNullOrEmpty())
            {
                switch (defaultConnection.DbType)
                {
                    case DbType.SqlServer:
                    case DbType.MySql:
                        defaultConnection.DBName = dbName;
                        break;
                    case DbType.Dm:
                    case DbType.Kdbndp:
                    case DbType.PostgreSQL:
                        defaultConnection.DBSchema = dbName;
                        break;
                    case DbType.Oracle:
                        defaultConnection.UserName = dbName;
                        break;
                    default:
                        break;
                }
            }
            return defaultConnection;
        }
        else
        {
            return new DbConnectionConfig
            {
                DbType = ToDbType(tenantLinkModel.dbType),
                Host = tenantLinkModel.host,
                Port = Convert.ToInt32(tenantLinkModel.port),
                DBName = tenantLinkModel.serviceName,
                UserName = tenantLinkModel.userName,
                Password = tenantLinkModel.password,
                DBSchema = tenantLinkModel.dbSchema,
            };
        }

    }

    /// <summary>
    /// 转换数据库类型.
    /// </summary>
    /// <param name="dbType">数据库类型.</param>
    /// <returns></returns>
    public static DbType ToDbType(string dbType)
    {
        switch (dbType.ToLower())
        {
            case "mysql":
                return SqlSugar.DbType.MySql;
            case "oracle":
                return SqlSugar.DbType.Oracle;
            case "dm8":
            case "dm":
                return SqlSugar.DbType.Dm;
            case "kdbndp":
            case "kingbasees":
                return SqlSugar.DbType.Kdbndp;
            case "postgresql":
                return SqlSugar.DbType.PostgreSQL;
            default:
                return SqlSugar.DbType.SqlServer;
        }
    }

    /// <summary>
    /// 转换连接字符串.
    /// </summary>
    /// <param name="dBConnectionConfig"></param>
    /// <returns></returns>
    public static string ToConnectionString(DbConnectionConfig dBConnectionConfig)
    {
        switch (dBConnectionConfig.DbType)
        {
            case DbType.SqlServer:
                // 命名实例（如 (local)\SQLEXPRESS）不需要端口号
                if (dBConnectionConfig.Host.Contains('\\'))
                    return string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};Connection Timeout=5;MultipleActiveResultSets=true;", dBConnectionConfig.Host, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password);
                return string.Format("Data Source={0},{4};Initial Catalog={1};User ID={2};Password={3};Connection Timeout=5;MultipleActiveResultSets=true;", dBConnectionConfig.Host, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password, dBConnectionConfig.Port);
            case DbType.Oracle:
                return string.Format("Data Source={0}:{1}/{2};User ID={3};Password={4};", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBSchema, dBConnectionConfig.UserName, dBConnectionConfig.Password);
            case DbType.MySql:
                return string.Format("server={0};Port={1};Database={2};Uid={3};Pwd={4};AllowLoadLocalInfile=true;SslMode=none;CharSet=utf8mb4;", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password);
            case DbType.Dm:
                return string.Format("server={0};port={1};database={2};User Id={3};PWD={4};SCHEMA={5};", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password, dBConnectionConfig.DBSchema);
            case DbType.Kdbndp:
                return string.Format("server={0};port={1};database={2};UID={3};PWD={4};searchpath={5};", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password, dBConnectionConfig.DBSchema);
            case DbType.PostgreSQL:
                return string.Format("server={0};port={1};Database={2};User Id={3};Password={4};searchpath={5};", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password, dBConnectionConfig.DBSchema);
            default:
                return string.Format("server={0};port={1};database={2};user={3};password={4};AllowLoadLocalInfile=true;", dBConnectionConfig.Host, dBConnectionConfig.Port, dBConnectionConfig.DBName, dBConnectionConfig.UserName, dBConnectionConfig.Password);
        }
    }
}