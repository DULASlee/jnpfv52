namespace JNPF.Runtime.Core;

/// <summary>
/// Authorization token for operations requiring explicit authorization.
/// </summary>
public sealed class AuthorizationToken
{
    /// <summary>
    /// Gets the token value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the expiration time (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; }

    /// <summary>
    /// Gets whether the token is still valid.
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Creates a new authorization token.
    /// </summary>
    /// <param name="value">Token value.</param>
    /// <param name="expiresAt">Expiration time.</param>
    public AuthorizationToken(string value, DateTime expiresAt)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Creates an authorization token that never expires.
    /// </summary>
    /// <param name="value">Token value.</param>
    /// <returns>Non-expiring token.</returns>
    public static AuthorizationToken NeverExpires(string value) =>
        new(value, DateTime.MaxValue);
}
