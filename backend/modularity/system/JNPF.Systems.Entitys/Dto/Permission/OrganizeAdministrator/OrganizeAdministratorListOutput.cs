using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.OrganizeAdministrator;

/// <summary>
/// 分级管理列表输出.
/// </summary>
[SuppressSniffer]
public class OrganizeAdministratorListOutput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 账号.
    /// </summary>
    public string account { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string realName { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public string gender { get; set; }

    /// <summary>
    /// 手机.
    /// </summary>
    public string mobilePhone { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 管理组.
    /// </summary>
    public string managerGroup { get; set; }
}