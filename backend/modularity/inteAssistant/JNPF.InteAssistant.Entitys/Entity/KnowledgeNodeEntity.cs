using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 知识图谱节点
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_KNOWLEDGE_NODE", TableDescription = "知识图谱节点")]
public class KnowledgeNodeEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 节点标签（entity / field / component 等）
    /// </summary>
    [SugarColumn(ColumnName = "F_LABEL")]
    public string Label { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [SugarColumn(ColumnName = "F_NAME")]
    public string Name { get; set; }

    /// <summary>
    /// JSON 扩展属性
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTIES")]
    public string Properties { get; set; }

    /// <summary>
    /// 乐观锁版本号（UPSERT冲突时用于指数退避重试）
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public int Version { get; set; }
}
