using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成基础信息模型.
/// </summary>
[SuppressSniffer]
public class CodeGenBasicInfoAttributeModel
{
    /// <summary>
    /// 表单ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string EnCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 主表名称.
    /// </summary>
    public string MianTable { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string PropertyJson { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string TableJson { get; set; }

    /// <summary>
    /// 关联数据连接id.
    /// </summary>
    public string DbLinkId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public long CreatorTime { get; set; }

    /// <summary>
    /// 创建用户.
    /// </summary>
    public string CreatorUserId { get; set; }
}