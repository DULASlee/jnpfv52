using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleDataAuthorizeScheme;

/// <summary>
/// 功能权限数据计划创建输入.
/// </summary>
[SuppressSniffer]
public class ModuleDataAuthorizeSchemeCrInput
{
    /// <summary>
    /// 功能主键.
    /// </summary>
    public string moduleId { get; set; }

    /// <summary>
    /// 方案名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 方案编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 方案对象.
    /// </summary>
    public string conditionJson { get; set; }

    /// <summary>
    /// 方案对象描述.
    /// </summary>
    public string conditionText { get; set; }

    /// <summary>
    /// 分组逻辑.
    /// </summary>
    public string matchLogic { get; set; }
}

public class ConditionJsonItem
{
    /// <summary>
    /// 逻辑.
    /// </summary>
    public string logic { get; set; }

    /// <summary>
    /// 组.
    /// </summary>
    public List<GroupsItem> groups { get; set; }
}

/// <summary>
/// 组项.
/// </summary>
public class GroupsItem
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 条件符号.
    /// </summary>
    public string op { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 条件值.
    /// </summary>
    public string conditionText { get; set; }
}