using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.PrintDev;

/// <summary>
/// 打印模板配置Sql模型.
/// </summary>
[SuppressSniffer]
public class PrintDevSqlModel
{
    /// <summary>
    /// sql.
    /// </summary>
    public string sql { get; set; }
}