namespace JNPF.Runtime.Core;

/// <summary>
/// Result of an execution admission check.
/// </summary>
public readonly struct AdmissionResult
{
    /// <summary>
    /// Gets whether the execution is admitted.
    /// </summary>
    public bool IsAdmitted { get; }

    /// <summary>
    /// Gets the rejection reason if not admitted.
    /// </summary>
    public string? RejectionReason { get; }

    private AdmissionResult(bool isAdmitted, string? rejectionReason)
    {
        IsAdmitted = isAdmitted;
        RejectionReason = rejectionReason;
    }

    /// <summary>
    /// Creates an admitted result.
    /// </summary>
    /// <returns>Admitted result.</returns>
    public static AdmissionResult Admitted() => new(true, null);

    /// <summary>
    /// Creates a rejected result.
    /// </summary>
    /// <param name="reason">Rejection reason.</param>
    /// <returns>Rejected result.</returns>
    public static AdmissionResult Rejected(string reason) => new(false, reason);
}
