using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户信息基类.
/// </summary>
[SugarTable("BASE_USER")]
public class UserEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 账户.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT")]
    public string Account { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    [SugarColumn(ColumnName = "F_REAL_NAME")]
    public string RealName { get; set; }

    /// <summary>
    /// 快速查询.
    /// </summary>
    [SugarColumn(ColumnName = "F_QUICK_QUERY")]
    public string QuickQuery { get; set; }

    /// <summary>
    /// 呢称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NICK_NAME")]
    public string NickName { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    [SugarColumn(ColumnName = "F_HEAD_ICON")]
    public string HeadIcon { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    [SugarColumn(ColumnName = "F_GENDER")]
    public string Gender { get; set; }

    /// <summary>
    /// 生日.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIRTHDAY")]
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// 手机.
    /// </summary>
    [SugarColumn(ColumnName = "F_MOBILE_PHONE")]
    public string MobilePhone { get; set; }

    /// <summary>
    /// 电话.
    /// </summary>
    [SugarColumn(ColumnName = "F_TELE_PHONE")]
    public string TelePhone { get; set; }

    /// <summary>
    /// 固定电话.
    /// </summary>
    [SugarColumn(ColumnName = "F_LANDLINE")]
    public string Landline { get; set; }

    /// <summary>
    /// 邮箱.
    /// </summary>
    [SugarColumn(ColumnName = "F_EMAIL")]
    public string Email { get; set; }

    /// <summary>
    /// 民族.
    /// </summary>
    [SugarColumn(ColumnName = "F_NATION")]
    public string Nation { get; set; }

    /// <summary>
    /// 籍贯.
    /// </summary>
    [SugarColumn(ColumnName = "F_NATIVE_PLACE")]
    public string NativePlace { get; set; }

    /// <summary>
    /// 入职日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENTRY_DATE")]
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// 证件类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_CERTIFICATES_TYPE")]
    public string CertificatesType { get; set; }

    /// <summary>
    /// 证件号码.
    /// </summary>
    [SugarColumn(ColumnName = "F_CERTIFICATES_NUMBER")]
    public string CertificatesNumber { get; set; }

    /// <summary>
    /// 文化程度.
    /// </summary>
    [SugarColumn(ColumnName = "F_EDUCATION")]
    public string Education { get; set; }

    /// <summary>
    /// 紧急联系人.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_URGENT_CONTACTS")]
    public string UrgentContacts { get; set; }

    /// <summary>
    /// 紧急电话.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_URGENT_TELE_PHONE")]
    public string UrgentTelePhone { get; set; }

    /// <summary>
    /// 通讯地址.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_POSTAL_ADDRESS")]
    public string PostalAddress { get; set; }

    /// <summary>
    /// 自我介绍.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SIGNATURE")]
    public string Signature { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PASSWORD")]
    public string Password { get; set; }

    /// <summary>
    /// 秘钥.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SECRETKEY")]
    public string Secretkey { get; set; }

    /// <summary>
    /// 首次登录时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_FIRST_LOG_TIME")]
    public DateTime? FirstLogTime { get; set; }

    /// <summary>
    /// 首次登录IP.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_FIRST_LOG_IP")]
    public string FirstLogIP { get; set; }

    /// <summary>
    /// 前次登录时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PREV_LOG_TIME")]
    public DateTime? PrevLogTime { get; set; }

    /// <summary>
    /// 前次登录IP.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PREV_LOG_IP")]
    public string PrevLogIP { get; set; }

    /// <summary>
    /// 最后登录时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LAST_LOG_TIME")]
    public DateTime? LastLogTime { get; set; }

    /// <summary>
    /// 最后登录IP.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LAST_LOG_IP")]
    public string LastLogIP { get; set; }

    /// <summary>
    /// 登录成功次数.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LOG_SUCCESS_COUNT")]
    public int? LogSuccessCount { get; set; }

    /// <summary>
    /// 登录错误次数.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LOG_ERROR_COUNT")]
    public int? LogErrorCount { get; set; } = 0;

    /// <summary>
    /// 最后修改密码时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_CHANGE_PASSWORD_DATE")]
    public DateTime? ChangePasswordDate { get; set; }

    /// <summary>
    /// 系统语言.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LANGUAGE")]
    public string Language { get; set; }

    /// <summary>
    /// 系统样式.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_THEME")]
    public string Theme { get; set; }

    /// <summary>
    /// 常用菜单.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_COMMON_MENU")]
    public string CommonMenu { get; set; }

    /// <summary>
    /// 是否管理员【0-普通、1-管理员】.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_IS_ADMINISTRATOR")]
    public int IsAdministrator { get; set; } = 0;

    /// <summary>
    /// 扩展属性.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 主管主键.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_MANAGER_ID")]
    public string ManagerId { get; set; }

    /// <summary>
    /// 组织主键.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID")]
    public string OrganizeId { get; set; }

    /// <summary>
    /// 岗位主键.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_POSITION_ID")]
    public string PositionId { get; set; }

    /// <summary>
    /// 角色主键.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_ROLE_ID")]
    public string RoleId { get; set; }

    /// <summary>
    /// 门户Id.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PORTAL_ID")]
    public string PortalId { get; set; }

    /// <summary>
    /// 是否锁定（0：未锁，1：已锁）.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_LOCK_MARK")]
    public int? LockMark { get; set; }

    /// <summary>
    /// 解锁时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_UNLOCK_TIME")]
    public DateTime? UnLockTime { get; set; }

    /// <summary>
    /// 分组Id.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_GROUP_ID")]
    public string GroupId { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string SystemId { get; set; }

    /// <summary>
    /// app系统Id.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_APP_SYSTEM_ID")]
    public string AppSystemId { get; set; }

    /// <summary>
    /// 钉钉工号.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_DING_JOB_NUMBER")]
    public string DingJobNumber { get; set; }

    /// <summary>
    /// 离职工作被交接的UserId.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_HANDOVER_USERID")]
    public string HandoverUserId { get; set; }

    /// <summary>
    /// 职级Id.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_RANK")]
    public string Ranks { get; set; }

    /// <summary>
    /// 微信OpenId.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "f_openId")]
    public string OpenId { get; set; }

    [SugarColumn(ColumnName = "f_is_dev")]
    public int IsDev { get; set; }

    /// <summary>
    /// 开发模式下对应的业务系统
    /// </summary>

    [SugarColumn(ColumnName = "f_biz_system_Id")]
    public string BizSystemId { get; set; }
}