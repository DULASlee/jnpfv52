namespace JNPF.Runtime.Core;

/// <summary>
/// Immutable snapshot of Mode context for an Execution.
/// Created at admission time and never modified afterwards.
/// </summary>
public sealed class ModeContext
{
    /// <summary>
    /// Gets the Mode type identifier for this execution.
    /// </summary>
    public int ModeTypeId { get; }

    /// <summary>
    /// Gets the Mode type name.
    /// </summary>
    public string ModeTypeName { get; }

    /// <summary>
    /// Gets the execution policy derived from the Mode.
    /// </summary>
    public ExecutionPolicy Policy { get; }

    /// <summary>
    /// Gets when the policy was snapshotted (UTC).
    /// </summary>
    public DateTime PolicySnapshotTime { get; }

    /// <summary>
    /// Gets the authorized user identifier (if provided).
    /// </summary>
    public string? AuthorizedUserId { get; }

    internal ModeContext(int modeTypeId, string modeTypeName, ExecutionPolicy policy, string? authorizedUserId = null)
    {
        ModeTypeId = modeTypeId;
        ModeTypeName = modeTypeName;
        Policy = policy;
        PolicySnapshotTime = DateTime.UtcNow;
        AuthorizedUserId = authorizedUserId;
    }

    /// <summary>
    /// Creates a ModeContext from PolicyData.
    /// </summary>
    /// <param name="policyData">Policy data from IPolicyProvider.</param>
    /// <param name="auth">Optional authorization token.</param>
    /// <param name="authorizedUserId">Optional authorized user ID.</param>
    /// <returns>ModeContext snapshot.</returns>
    public static ModeContext FromPolicyData(JNPF.Runtime.Capability.Loading.PolicyData policyData, AuthorizationToken? auth = null, string? authorizedUserId = null)
    {
        var policy = ExecutionPolicy.FromPolicyData(policyData, auth);
        return new ModeContext(
            (int)policyData.ModeType,
            policyData.ModeType.ToString(),
            policy,
            authorizedUserId);
    }

    /// <summary>
    /// Creates a ModeContext with a rejected policy.
    /// </summary>
    /// <param name="modeTypeId">The Mode type ID.</param>
    /// <param name="modeTypeName">The Mode type name.</param>
    /// <param name="reason">Rejection reason.</param>
    /// <returns>Rejected ModeContext.</returns>
    public static ModeContext Rejected(int modeTypeId, string modeTypeName, string reason)
    {
        var policy = ExecutionPolicy.Rejected(reason);
        return new ModeContext(modeTypeId, modeTypeName, policy);
    }
}
