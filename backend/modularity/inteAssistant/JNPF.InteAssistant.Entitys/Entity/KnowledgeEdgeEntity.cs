using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 知识图谱边
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_KNOWLEDGE_EDGE", TableDescription = "知识图谱边")]
public class KnowledgeEdgeEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 起始节点 ID（FK → BASE_KNOWLEDGE_NODE）
    /// </summary>
    [SugarColumn(ColumnName = "F_SOURCE_NODE_ID")]
    public string SourceNodeId { get; set; }

    /// <summary>
    /// 目标节点 ID（FK → BASE_KNOWLEDGE_NODE）
    /// </summary>
    [SugarColumn(ColumnName = "F_TARGET_NODE_ID")]
    public string TargetNodeId { get; set; }

    /// <summary>
    /// 关系类型（contains / references / depends_on）
    /// </summary>
    [SugarColumn(ColumnName = "F_RELATION_TYPE")]
    public string RelationType { get; set; }

    /// <summary>
    /// JSON 扩展属性
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTIES")]
    public string Properties { get; set; }
}
