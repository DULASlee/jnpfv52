namespace JNPF.Runtime.Capability.Constraints;

/// <summary>
/// Section 9 Mode 静态约束标记接口。
///
/// 约束分类：
///   - <see cref="CapabilityConstraint"/>：声明 Mode 不得包含某 Capability；
///   - 后续可扩展 RequireExplicitAuthorization / RequireCapabilitySubset 等。
///
/// 不携带 Runtime 上下文，不引入 State。
/// </summary>
public interface IConstraint
{
    /// <summary>
    /// 约束的稳定 ID（用于诊断和测试断言）。
    /// </summary>
    string Id { get; }
}
