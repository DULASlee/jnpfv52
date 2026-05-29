using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenRecord;

/// <summary>
/// 大屏数据源详情输出.
/// </summary>
[SuppressSniffer]
public class ScreenRecordInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// url.
    /// </summary>
    public string url { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public int? dataType { get; set; }

    /// <summary>
    /// 数据方法.
    /// </summary>
    public string dataMethod { get; set; }

    /// <summary>
    /// 数据头部.
    /// </summary>
    public string dataHeader { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public string data { get; set; }

    /// <summary>
    /// 数据查询.
    /// </summary>
    public string dataQuery { get; set; }

    /// <summary>
    /// 数据查询类型.
    /// </summary>
    public string dataQueryType { get; set; }

    /// <summary>
    /// 数据格式化程序.
    /// </summary>
    public string dataFormatter { get; set; }

    /// <summary>
    /// 是否跨域.
    /// </summary>
    public bool proxy { get; set; } = false;

    /// <summary>
    /// WS_URL.
    /// </summary>
    public string wsUrl { get; set; }

    /// <summary>
    /// 数据库sql.
    /// </summary>
    public string dbsql { get; set; }

    /// <summary>
    /// fsql.
    /// </summary>
    public string fsql { get; set; }

    /// <summary>
    /// 结果.
    /// </summary>
    public string result { get; set; }
}
