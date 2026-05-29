using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成左侧树配置模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndLeftTreeFieldDesignModel
{
    /// <summary>
    /// 是否开启左侧查询.
    /// </summary>
    public bool HasSearch { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 数据来源.
    /// </summary>
    public string TreeDataSource { get; set; }

    /// <summary>
    /// 树数据字典.
    /// </summary>
    public string TreeDictionary { get; set; }

    /// <summary>
    /// 异步接口ID.
    /// </summary>
    public string TreeInterfaceId { get; set; }

    /// <summary>
    /// 数据接口.
    /// </summary>
    public string TreePropsUrl { get; set; }

    /// <summary>
    /// 主键字段.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    public string ShowField { get; set; }

    /// <summary>
    /// 子级字段.
    /// </summary>
    public string Children { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string TreeRelation { get; set; }

    /// <summary>
    /// 是否开启同步.
    /// </summary>
    public bool HasSynType { get; set; }

    /// <summary>
    /// 左侧树模板Json.
    /// </summary>
    public string TreeTemplateJson { get; set; }

    /// <summary>
    /// 左侧树模板Json.
    /// </summary>
    public string TemplateJson { get; set; }

    /// <summary>
    /// 左侧树控件KEY.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool IsMultiple { get; set; }
}