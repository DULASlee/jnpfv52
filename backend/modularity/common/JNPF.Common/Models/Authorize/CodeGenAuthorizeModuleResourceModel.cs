using SqlSugar;

namespace JNPF.Common.Models.Authorize;

/// <summary>
/// 代码生成权限资源.
/// </summary>
public class CodeGenAuthorizeModuleResourceModel
{
    /// <summary>
    /// 字段规则
    /// 0-主表,1-副表,2-子表.
    /// </summary>
    public int FieldRule { get; set; }

    /// <summary>
    /// 表名称针对非主表.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 权限条件.
    /// </summary>
    public List<IConditionalModel> conditionalModel { get; set; }
}

public class CodeGenAuthorizeModuleResource
{
    /// <summary>
    /// 字段规则
    /// 0-主表,1-副表,2-子表.
    /// </summary>
    public int FieldRule { get; set; }

    /// <summary>
    /// 表名称针对非主表.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 权限条件.
    /// </summary>
    public List<object> conditionalModel { get; set; }
}

/// <summary>
/// 代码生成 数据过滤.
/// </summary>
public class CodeGenDataRuleModuleResourceModel : CodeGenAuthorizeModuleResourceModel
{
    /// <summary>
    /// whereType.
    /// </summary>
    public string WhereType { get; set; }

    /// <summary>
    /// 请求类型 pc 和 app.
    /// </summary>
    public string UserOrigin { get; set; }

    /// <summary>
    /// 权限条件 json.
    /// </summary>
    public string conditionalModelJson { get; set; }
}