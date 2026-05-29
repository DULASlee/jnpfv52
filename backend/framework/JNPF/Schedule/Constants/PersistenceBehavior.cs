namespace JNPF.Schedule;

/// <summary>
/// 作业持久化行为
/// </summary>
[SuppressSniffer]
public enum PersistenceBehavior : uint
{
    /// <summary>
    /// 添加
    /// </summary>
    Appended = 0,

    /// <summary>
    /// 更新
    /// </summary>
    Updated = 1,

    /// <summary>
    /// 删除
    /// </summary>
    Removed = 2,
}