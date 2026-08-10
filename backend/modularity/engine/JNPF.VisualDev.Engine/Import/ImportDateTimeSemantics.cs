namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Preserved behavioral fork between online VisualDev and CodeGen import DATE/TIME handling.
/// </summary>
public enum ImportDateTimeSemantics
{
    /// <summary>
    /// Null-safe relation fields; skip MinValue in range compare; empty value leaves key untouched;
    /// enter bound rules when start/endTimeRule alone is true.
    /// </summary>
    VisualDev = 0,

    /// <summary>
    /// Enter bound rules only when start/endTimeValue is present; empty clears to null;
    /// range compare without MinValue skip.
    /// </summary>
    CodeGen = 1,
}
