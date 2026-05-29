using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 转递逻辑模型.
/// </summary>
[SuppressSniffer]
public class TargetLogicModel
{
    /// <summary>
    /// 目标字段.
    /// </summary>
    public string targetField { get; set; }

    /// <summary>
    /// 赋值类型
    /// 1-主传主,2-子传主,3-主传子,4-子传子.
    /// </summary>
    public int assignType { get; set; }

    /// <summary>
    /// 来源类型
    /// 1-字段,2-自定义.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 来源值.
    /// </summary>
    public string sourceValue { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 子表.
    /// </summary>
    public List<TargetLogicModel> SubTable { get; set; }
}