namespace SemanticGolden.Domain;

/// <summary>
/// Phase 10 G10-9 Cross Assembly fixture (counterpart of
/// <c>SemanticGolden.Contracts.Customer</c>). Different property set
/// — same short name, different assembly, different identity.
/// </summary>
public sealed class Customer
{
    public string InternalId { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
