using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Contracts;

/// <summary>
/// 创更删实体基类(包含标识字段).
/// </summary>
[SuppressSniffer]
public abstract class CLDSEntityBase : CLDEntityBase
{ 

    /// <summary>
    /// 获取或设置 启用标识
    /// 0-禁用,1-启用.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENABLED_MARK", ColumnDescription = "启用标识")]
    public virtual int? EnabledMark { get; set; }

    /// <summary>
    /// 创建.
    /// </summary>
    public virtual void Creator()
    {
        base.Creator();
        this.EnabledMark = this.EnabledMark == null ? 1 : this.EnabledMark;
    }

    /// <summary>
    /// 创建.
    /// </summary>
    public virtual void Create()
    {
        base.Create();
        this.EnabledMark = this.EnabledMark == null ? 1 : this.EnabledMark;
    }

    
}