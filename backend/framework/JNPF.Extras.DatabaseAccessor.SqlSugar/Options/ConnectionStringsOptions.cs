using JNPF;
using JNPF.ConfigurableOptions;
using JNPF.LinqBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SqlSugar;

/// <summary>
/// 数据库配置.
/// </summary>
public sealed class ConnectionStringsOptions : IConfigurableOptions<ConnectionStringsOptions>
{
    //private DbConnectionConfig defaultConnectionConfig;

    /// <summary>
    /// 数据库集合
    /// </summary>
    public List<DbConnectionConfig> ConnectionConfigs { get; set; }

    /// <summary>
    /// 默认数据库
    /// </summary>
    public DbConnectionConfig DefaultConnectionConfig
    {
        get
        {
            return GetDomainConnectionConfigs();
        }
        //set => defaultConnectionConfig = value;
    }
    private DbConnectionConfig GetDomainConnectionConfigs()
    {
        if (!string.IsNullOrEmpty(CurrentDomain))
        {
            var conn = ConnectionConfigs.Where(x => !x.Domain.IsNullOrEmpty() && CurrentDomain.StartsWith(x.Domain)).FirstOrDefault();
            if (conn != null)
            {
                return conn;
            }
        }

        return ConnectionConfigs.FirstOrDefault(x => x.ConfigId.ToString() == "default");
    }
    public void PostConfigure(ConnectionStringsOptions options, IConfiguration configuration)
    {
        foreach (var dbConfig in options.ConnectionConfigs)
        {
            if (string.IsNullOrWhiteSpace(dbConfig.ConfigId.ToString()))
                dbConfig.ConfigId = "default";
        }

        //DefaultConnectionConfig = ConnectionConfigs.FirstOrDefault(x => x.ConfigId.ToString() == "default");
        //if (!string.IsNullOrEmpty(CurrentDomain))
        //{
        //    //domain = "frame_v1.zhixuan.cloud";
        //    var conn = ConnectionConfigs.Where(x => !x.Domain.IsNullOrEmpty() && CurrentDomain.StartsWith(x.Domain)).FirstOrDefault();
        //    if (conn != null)
        //    {
        //        DefaultConnectionConfig = conn;
        //    }
        //}
    }

    private string CurrentDomain
    {
        get
        {
            var domain = string.Empty;

            try
            {

                var httpContextAccessor = App.GetService<IHttpContextAccessor>();
                if (httpContextAccessor != null)
                {
                    domain = httpContextAccessor?.HttpContext?.Request?.Host.Value;
                    domain = httpContextAccessor?.HttpContext?.Request.Headers["referer"].ToString().Replace("http://", "").Replace("https://", "");
                }

                Console.WriteLine("Domain:" + DateTime.Now.ToString() + " " + domain);
            }
            catch (Exception e)
            {
                Console.WriteLine("Domain Exception:" +e.Message);
            }

            return domain;
        }
    }
}

/// <summary>
/// 数据库连接配置.
/// </summary>
public sealed class DbConnectionConfig : ConnectionConfig
{
    /// <summary>
    /// 数据库名称.
    /// </summary>
    public string DBName { get; set; }

    /// <summary>
    /// 数据库地址.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// 数据库端口号.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 账号.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 模式.
    /// </summary>
    public string DBSchema { get; set; }

    /// <summary>
    /// 域名.
    /// </summary>
    public string Domain { get; set; }
}

