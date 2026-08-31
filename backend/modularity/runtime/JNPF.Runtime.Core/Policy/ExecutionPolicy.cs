namespace JNPF.Runtime.Core;

/// <summary>
/// Execution policy derived from Mode capabilities.
/// This is a minimal contract - Runtime knows only these properties, not Mode specifics.
/// </summary>
public readonly struct ExecutionPolicy
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
    /// Gets whether explicit authorization is required for this policy.
    /// </summary>
    public bool RequiresExplicitAuthorization { get; }

    /// <summary>
    /// Gets the authorization token (if provided).
    /// </summary>
    public AuthorizationToken? AuthorizationToken { get; }

    /// <summary>
    /// Gets whether this policy is a rejection (from unauthorized execution).
    /// </summary>
    public bool IsRejected { get; }

    /// <summary>
    /// Gets the rejection reason (if rejected).
    /// </summary>
    public string? RejectionReason { get; }

    private ExecutionPolicy(
        bool canRead,
        bool canVerify,
        bool canWrite,
        bool requiresExplicitAuthorization,
        AuthorizationToken? authorizationToken,
        bool isRejected = false,
        string? rejectionReason = null)
    {
        CanRead = canRead;
        CanVerify = canVerify;
        CanWrite = canWrite;
        RequiresExplicitAuthorization = requiresExplicitAuthorization;
        AuthorizationToken = authorizationToken;
        IsRejected = isRejected;
        RejectionReason = rejectionReason;
    }

    /// <summary>
    /// Creates an execution policy from PolicyData.
    /// </summary>
    /// <param name="policyData">Policy data from IPolicyProvider.</param>
    /// <param name="auth">Optional authorization token.</param>
    /// <returns>Execution policy.</returns>
    public static ExecutionPolicy FromPolicyData(JNPF.Runtime.Capability.Loading.PolicyData policyData, AuthorizationToken? auth = null)
    {
        return new ExecutionPolicy(
            policyData.CanRead,
            policyData.CanVerify,
            policyData.CanWrite,
            policyData.RequiresExplicitAuthorization,
            auth);
    }

    /// <summary>
    /// Creates a rejected policy (for unauthorized execution attempts).
    /// </summary>
    /// <param name="reason">Rejection reason.</param>
    /// <returns>Rejected policy.</returns>
    public static ExecutionPolicy Rejected(string reason) => new(
        canRead: false,
        canVerify: false,
        canWrite: false,
        requiresExplicitAuthorization: false,
        authorizationToken: null,
        isRejected: true,
        rejectionReason: reason);

    /// <summary>
    /// Authorizes the policy based on explicit authorization requirements.
    /// </summary>
    /// <returns>Authorization result.</returns>
    public AuthorizationResult Authorize()
    {
        // If already rejected, return rejected
        if (IsRejected)
            return AuthorizationResult.Rejected(RejectionReason ?? "Execution rejected");

        // Check explicit authorization requirement
        if (RequiresExplicitAuthorization && AuthorizationToken == null)
            return AuthorizationResult.Rejected("Explicit authorization required for this operation");

        // Check token validity
        if (RequiresExplicitAuthorization && AuthorizationToken != null && !AuthorizationToken.IsValid)
            return AuthorizationResult.Rejected("Authorization token has expired or is invalid");

        return AuthorizationResult.Allowed();
    }

    /// <summary>
    /// Creates a policy that allows all operations (for testing).
    /// </summary>
    /// <returns>Permissive policy.</returns>
    public static ExecutionPolicy Permissive() => new(
        canRead: true,
        canVerify: true,
        canWrite: true,
        requiresExplicitAuthorization: false,
        authorizationToken: null);
}
