namespace JNPF.Runtime.Capability.Constraints;

/// <summary>
/// 标记 Mode 需要显式授权才能激活 (M10)。
///
/// 当前阶段仅作为静态约束存在（Registry / Provider 可基于此约束生成授权提示）。
/// 实际授权拦截由 Section 8 Runtime 完成，本阶段不引入运行时授权逻辑。
/// </summary>
public sealed class RequiresExplicitAuthorizationConstraint : IConstraint
{
    public RequiresExplicitAuthorizationConstraint()
    {
        Id = "mode:requires:explicit-authorization";
    }

    public string Id { get; }
}
