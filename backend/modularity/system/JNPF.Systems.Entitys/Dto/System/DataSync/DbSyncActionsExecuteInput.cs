using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataSync;

/// <summary>
/// 数据同步动作执行输入.
/// </summary>
[SuppressSniffer]
public class DbSyncActionsExecuteInput
{
    /// <summary>
    /// 源数据库id.
    /// </summary>
    public string dbConnectionFrom { get; set; }

    /// <summary>
    /// 目前数据库id.
    /// </summary>
    public string dbConnectionTo { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    public string dbTable { get; set; }

    /// <summary>
    /// 表名(批量).
    /// </summary>
    public List<string> dbTableList { get; set; }

    /// <summary>
    /// 操作类型.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 字段类型.
    /// </summary>
    public Dictionary<string, string> convertRuleMap { get; set; }
}