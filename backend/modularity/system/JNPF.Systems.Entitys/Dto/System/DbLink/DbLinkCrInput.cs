using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbLink;

/// <summary>
/// 数据连接创建输入.
/// </summary>
[SuppressSniffer]
public class DbLinkCrInput
{
    /// <summary>
    /// 数据库名.
    /// </summary>
    public string serviceName { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string userName { get; set; }

    /// <summary>
    /// 状态(1-可用,0-禁用).
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 端口.
    /// </summary>
    public string port { get; set; }

    /// <summary>
    /// 数据库驱动类型.
    /// </summary>
    public string dbType { get; set; }

    /// <summary>
    /// 连接名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 主机地址.
    /// </summary>
    public string host { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    public string password { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 模式.
    /// </summary>
    public string dbSchema { get; set; }

    /// <summary>
    /// 表空间.
    /// </summary>
    public string tableSpace { get; set; }

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