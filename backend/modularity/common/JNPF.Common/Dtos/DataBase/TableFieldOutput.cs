using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.DataBase;

/// <summary>
/// 表字段输出.
/// </summary>
[SuppressSniffer]
public class TableFieldOutput
{
    /// <summary>
    /// 是否主键（0：是，1：否）.
    /// </summary>
    public int primaryKey { get; set; }

    /// <summary>
    /// 是否允许为空.
    /// </summary>
    public int allowNull { get; set; }


    /// <summary>
    /// 长度.
    /// </summary>
    public string dataLength { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 列名.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段注释.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 是否自增（0：是，1：否）.
    /// </summary>
    public int identity { get; set; }
	
	//是否为虚拟字段
    public int virtualField { get; set; }
}