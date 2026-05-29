namespace JNPF.Schedule;

/// <summary>
/// SQL 类型
/// </summary>
/// <remarks>用于控制生成 SQL 格式</remarks>
[SuppressSniffer]
public enum SqlTypes
{
    /// <summary>
    /// 标准 SQL
    /// </summary>
    Standard = 0,

    /// <summary>
    /// SqlServer
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// Sqlite
    /// </summary>
    Sqlite = 2,

    /// <summary>
    /// MySql
    /// </summary>
    MySql = 3,

    /// <summary>
    /// PostgresSQL
    /// </summary>
    PostgresSQL = 4,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 5,

    /// <summary>
    /// Firebird
    /// </summary>
    Firebird = 6
}