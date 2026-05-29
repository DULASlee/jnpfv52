using JNPF.Systems.Entitys.Dto.Database;

namespace JNPF.Systems.Entitys.Dto.System.DataSync;

/// <summary>
/// 数据同步动作执行输出.
/// </summary>
public class DbSyncOutput
{
    /// <summary>
    /// 同步数据库是否正确.
    /// </summary>
    public bool checkDbFlag { get; set; }

    /// <summary>
    /// 源数据库表数据.
    /// </summary>
    public List<DatabaseTableListOutput> tableList { get; set; }

    /// <summary>
    /// 映射字段.
    /// </summary>
    public Dictionary<string, List<string>> convertRuleMap { get; set; }
}
