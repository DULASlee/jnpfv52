using JNPF.Runtime.Capability.Constraints;
using JNPF.Runtime.Capability.Modes;

namespace JNPF.Runtime.Capability.Loading;

/// <summary>
/// 默认 Provider：基于 4 Default Modes 解析新实例。
///
/// Lifetime 策略：
///   - 每次 Resolve 返回新实例（§3.4 + Gate-9-5 + Iron Law-05 Lifetime Guard）；
///   - 不维护 Singleton / Static Cache / Global Current；
///   - 实例为不可变 Contract，调用方无需释放。
/// </summary>
public sealed class DefaultModeProvider : IModeProvider, IPolicyProvider
{
    public IMode Resolve(ModeType modeType)
    {
        return modeType switch
        {
            ModeType.Audit => new AuditMode(),
            ModeType.Verify => new VerifyMode(),
            ModeType.Execute => new ExecuteMode(),
            ModeType.Assist => new AssistMode(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(modeType),
                modeType,
                $"Unknown ModeType: {modeType}")
        };
    }

    /// <inheritdoc />
    public PolicyData ResolvePolicy(ModeType modeType, string? authorizationToken = null)
    {
        var mode = Resolve(modeType);
        var capabilities = mode.Capabilities;
        var constraints = mode.Constraints;

        // CanRead: Mode has Observe or Evaluate
        var canRead = capabilities.Allows(Capability.Observe)
                   || capabilities.Allows(Capability.Evaluate);

        // CanVerify: Mode has Build or Test capability
        var canVerify = capabilities.Allows(Capability.Build)
                     || capabilities.Allows(Capability.Test);

        // CanWrite: Mode has ModifyState, ApplyApprovedPatch, or WriteEvidence
        var canWrite = capabilities.Allows(Capability.ModifyState)
                    || capabilities.Allows(Capability.ApplyApprovedPatch)
                    || capabilities.Allows(Capability.WriteEvidence);

        // RequiresExplicitAuthorization: check if constraint exists
        var requiresExplicit = constraints.Items.Any(c => 
            c is RequiresExplicitAuthorizationConstraint);

        return new PolicyData(modeType, canRead, canVerify, canWrite, requiresExplicit);
    }
}
