using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 可视化数据源配置表.
/// </summary>
[SugarTable("BLADE_VISUAL_DB")]
[Tenant(ClaimConst.TENANTID)]
public class VisualDBEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "Name", ColumnDescription = "名称")]
    public string Name { get; set; }

    /// <summary>
    /// 驱动类.
    /// </summary>
    [SugarColumn(ColumnName = "DRIVER_CLASS", ColumnDescription = "驱动类")]
    public string DriverClass { get; set; }

    /// <summary>
    /// 连接地址.
    /// </summary>
    [SugarColumn(ColumnName = "URL", ColumnDescription = "连接地址")]
    public string Url { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    [SugarColumn(ColumnName = "USERNAME", ColumnDescription = "用户名")]
    public string UserName { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    [SugarColumn(ColumnName = "PASSWORD", ColumnDescription = "密码")]
    public string Password { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "REMARK", ColumnDescription = "备注")]
    public string Remark { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    [SugarColumn(ColumnName = "CREATE_USER", ColumnDescription = "创建人")]
    public string CreateUser { get; set; }

    /// <summary>
    /// 创建部门.
    /// </summary>
    [SugarColumn(ColumnName = "CREATE_DEPT", ColumnDescription = "创建部门")]
    public string CreateDept { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    [SugarColumn(ColumnName = "CREATE_TIME", ColumnDescription = "创建时间")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    [SugarColumn(ColumnName = "UPDATE_USER", ColumnDescription = "修改人")]
    public string UpdateUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    [SugarColumn(ColumnName = "UPDATE_TIME", ColumnDescription = "修改时间")]
    public DateTime UpdateTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    [SugarColumn(ColumnName = "STATUS", ColumnDescription = "状态")]
    public string Status { get; set; }

    /// <summary>
    /// 是否已删除.
    /// </summary>
    [SugarColumn(ColumnName = "IS_DELETED", ColumnDescription = "是否已删除")]
    public int IsDeleted { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }

    /// <summary>
    /// 创建.
    /// </summary>
    public virtual void Create()
    {
        string? userId = App.User.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
        this.CreateTime = DateTime.Now;
        this.IsDeleted = 0;
        this.Id = SnowflakeIdHelper.NextId();
        if (!string.IsNullOrEmpty(userId))
        {
            this.CreateUser = userId;
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    public virtual void LastModify()
    {
        string? userId = App.User.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
        this.UpdateTime = DateTime.Now;
        if (!string.IsNullOrEmpty(userId))
        {
            this.UpdateUser = userId;
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    public virtual void Delete()
    {
        this.IsDeleted = 1;
    }
}
