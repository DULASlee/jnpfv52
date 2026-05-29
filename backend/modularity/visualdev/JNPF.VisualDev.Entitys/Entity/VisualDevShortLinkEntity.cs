using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 可视化开发功能实体.
/// </summary>
[SugarTable("BASE_VISUAL_LINK")]
public class VisualDevShortLinkEntity : CLDSEntityBase
{
    /// <summary>
    /// 短链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_SHORT_LINK")]
    public string ShortLink { get; set; }

    /// <summary>
    /// 外链填单开关.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_USE")]
    public int FormUse { get; set; }

    /// <summary>
    /// 外链填单.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_LINK")]
    public string FormLink { get; set; }

    /// <summary>
    /// 外链密码开关(1：开 , 0：关).
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_PASS_USE")]
    public int FormPassUse { get; set; }

    /// <summary>
    /// 外链填单密码.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_PASSWORD")]
    public string FormPassword { get; set; }

    /// <summary>
    /// 公开查询开关.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_USE")]
    public int ColumnUse { get; set; }

    /// <summary>
    /// 公开查询.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_LINK")]
    public string ColumnLink { get; set; }

    /// <summary>
    /// 查询密码开关.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_PASS_USE")]
    public int ColumnPassUse { get; set; }

    /// <summary>
    /// 公开查询密码.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_PASSWORD")]
    public string ColumnPassword { get; set; }

    /// <summary>
    /// 查询条件.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_CONDITION")]
    public string ColumnCondition { get; set; }

    /// <summary>
    /// 显示内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_TEXT")]
    public string ColumnText { get; set; }

    /// <summary>
    /// PC端链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_REAL_PC_LINK")]
    public string RealPcLink { get; set; }

    /// <summary>
    /// App端链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_REAL_APP_LINK")]
    public string RealAppLink { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }
}