using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbLink;

/// <summary>
/// 数据连接测试连接输入.
/// </summary>
[SuppressSniffer]
public class DbLinkActionsTestInput
{
    /// <summary>
    /// 连接类型.
    /// </summary>
    public string dbType { get; set; }

    /// <summary>
    /// 主机.
    /// </summary>
    public string host { get; set; }

    /// <summary>
    /// 端口.
    /// </summary>
    public string port { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string password { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 库名.
    /// </summary>
    public string serviceName { get; set; }

    /// <summary>
    /// 模式.
    /// </summary>
    public string dbSchema { get; set; }

    /// <summary>
    /// 表空间.
    /// </summary>
    public string tableSpace { get; set; }

    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// oracle扩展属性开启.
    /// </summary>
    public bool oracleExtend { get; set; }

    /// <summary>
    /// oracle连接方式.
    /// </summary>
    public string oracleLinkType { get; set; }

    /// <summary>
    /// oracle角色.
    /// </summary>
    public string oracleRole { get; set; }

    /// <summary>
    /// oracle服务名.
    /// </summary>
    public string oracleService { get; set; }
}