using JNPF.Runtime.Capability.Modes;

namespace JNPF.Runtime.Capability.Loading;

/// <summary>
/// Policy provider for Runtime.Core integration.
/// This is the ONLY port from Runtime to Mode layer.
/// 
/// M17: Runtime → Mode one-way dependency via this interface only.
/// </summary>
public interface IPolicyProvider
{
    /// <summary>
    /// Resolves an execution policy from a Mode type.
    /// </summary>
    /// <param name="modeType">The Mode type.</param>
    /// <param name="authorizationToken">Optional authorization token.</param>
    /// <returns>Policy data that Runtime.Core can use.</returns>
    PolicyData ResolvePolicy(ModeType modeType, string? authorizationToken = null);
}

/// <summary>
/// Policy data passed from Mode layer to Runtime layer.
/// Contains only minimal data needed for admission decisions.
/// </summary>
public readonly struct PolicyData
{
    /// <summary>
    /// Gets whether read operations are allowed.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Gets whether verify/build operations are allowed.
    /// </summary>
    public bool CanVerify { get; }

    /// <summary>
    /// Gets whether write/modify operations are allowed.
    /// </summary>
    public bool CanWrite { get; }

    /// <summary>
    /// Gets whether explicit authorization is required.
    /// </summary>
    public bool RequiresExplicitAuthorization { get; }

    /// <summary>
    /// Gets the Mode type that created this policy.
    /// </summary>
    public ModeType ModeType { get; }

    public PolicyData(
        ModeType modeType,
        bool canRead,
        bool canVerify,
        bool canWrite,
        bool requiresExplicitAuthorization)
    {
        ModeType = modeType;
        CanRead = canRead;
        CanVerify = canVerify;
        CanWrite = canWrite;
        RequiresExplicitAuthorization = requiresExplicitAuthorization;
    }
}
