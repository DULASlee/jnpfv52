namespace JNPF.Schedule;

/// <summary>
/// 作业触发器配置选项
/// </summary>
[SuppressSniffer]
public sealed class TriggerOptions
{
    /// <summary>
    /// 构造函数
    /// </summary>
    internal TriggerOptions()
    {
    }

    /// <summary>
    /// 重写 <see cref="ConvertToSQL"/>
    /// </summary>
    public Func<string, string[], Trigger, PersistenceBehavior, NamingConventions, string> ConvertToSQL
    {
        get
        {
            return ConvertToSQLConfigure;
        }
        set
        {
            ConvertToSQLConfigure = value;
        }
    }

    /// <summary>
    /// <see cref="ConvertToSQL"/> 静态配置
    /// </summary>
    internal static Func<string, string[], Trigger, PersistenceBehavior, NamingConventions, string> ConvertToSQLConfigure { get; private set; }
}