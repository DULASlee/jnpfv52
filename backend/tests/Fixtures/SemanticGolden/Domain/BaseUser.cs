namespace SemanticGolden.Domain;

/// <summary>
/// Phase 8 inheritance fixture (施工包 §57): base declaring an inherited
/// property (<c>Name</c>) and a virtual operation (<c>Describe</c>).
/// </summary>
public class BaseUser
{
    public string Name { get; set; } = string.Empty;

    public virtual string Describe() => "base";
}
