namespace JNPF.Runtime.Core;

/// <summary>
/// Result of an authorization check.
/// </summary>
public readonly struct AuthorizationResult
{
    /// <summary>
    /// Gets whether the authorization was granted.
    /// </summary>
    public bool IsAuthorized { get; }

    /// <summary>
    /// Gets the rejection reason if not authorized.
    /// </summary>
    public string? Reason { get; }

    private AuthorizationResult(bool isAuthorized, string? reason)
    {
        IsAuthorized = isAuthorized;
        Reason = reason;
    }

    /// <summary>
    /// Creates an authorized result.
    /// </summary>
    /// <returns>Authorized result.</returns>
    public static AuthorizationResult Allowed() => new(true, null);

    /// <summary>
    /// Creates a rejected result.
    /// </summary>
    /// <param name="reason">Rejection reason.</param>
    /// <returns>Rejected result.</returns>
    public static AuthorizationResult Rejected(string reason) => new(false, reason);
}
