using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 大屏基本信息.
/// </summary>
[SugarTable("BLADE_VISUAL")]
[Tenant(ClaimConst.TENANTID)]
public class VisualEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 大屏标题.
    /// </summary>
    [SugarColumn(ColumnName = "TITLE", ColumnDescription = "大屏标题")]
    public string Title { get; set; }

    /// <summary>
    /// 大屏背景.
    /// </summary>
    [SugarColumn(ColumnName = "BACKGROUND_URL", ColumnDescription = "大屏背景")]
    public string BackgroundUrl { get; set; }

    /// <summary>
    /// 大屏类型.
    /// </summary>
    [SugarColumn(ColumnName = "CATEGORY", ColumnDescription = "大屏类型")]
    public int Category { get; set; }

    /// <summary>
    /// 发布密码.
    /// </summary>
    [SugarColumn(ColumnName = "PASSWORD", ColumnDescription = "发布密码")]
    public string Password { get; set; }

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
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    [SugarColumn(ColumnName = "UPDATE_USER", ColumnDescription = "修改人")]
    public string UpdateUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    [SugarColumn(ColumnName = "UPDATE_TIME", ColumnDescription = "修改时间")]
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 业务状态.
    /// </summary>
    [SugarColumn(ColumnName = "STATUS", ColumnDescription = "状态")]
    public int Status { get; set; }

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
        var userId = App.User.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
        this.CreateTime = DateTime.Now;
        this.Id = SnowflakeIdHelper.NextId();
        this.BackgroundUrl = (this.BackgroundUrl == null || this.BackgroundUrl == string.Empty) ? "/api/file/VisusalImg/BiVisualPath/bg/bg1.png" : this.BackgroundUrl;
        this.IsDeleted = 0;
        this.Status = 1;
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
        var userId = App.User.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
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
