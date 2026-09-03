namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H5: the four separated identities (chief §十五 frozen model).
/// <list type="bullet">
/// <item>CompilationIdentity — "which build?" (project, references, options, documents).</item>
/// <item>NativeBindingIdentity — "currently bound to what?" (assembly-qualified DocId).</item>
/// <item>LogicalSemanticIdentity — "logically who?" (stable across edits that keep meaning).</item>
/// <item>SemanticFingerprint — "currently what?" (detects semantic change: type/signature/constraints).</item>
/// </list>
/// Logical identity SAME + fingerprint CHANGED (e.g. string→int) is the
/// legal and expected outcome of a semantic edit — never conflated.
/// </summary>
public sealed record CompilationIdentity(
    string ProjectName,
    string AssemblyName,
    IReadOnlyList<string> ReferenceDisplayNames,
    string OptimizationLevel,
    string LanguageVersion,
    IReadOnlyList<string> DocumentPaths,
    string SnapshotId);

/// <summary>
/// P13-H5: binding identity = where Roslyn currently binds the symbol
/// (assembly-qualified declaration id). Rebinds when the compilation changes.
/// </summary>
public sealed record NativeBindingIdentity(
    string AssemblyName,
    string DeclarationId);

/// <summary>
/// P13-H5: logical identity = which semantic node this is, independent of
/// its current shape. Two versions of User.PhoneNumber share it even when
/// the property type changes from string to int.
/// </summary>
public sealed record LogicalSemanticIdentity(
    string AssemblyName,
    string Namespace,
    string ContainingTypeName,
    string MemberName,
    string MemberKind);

/// <summary>
/// P13-H5: semantic fingerprint = what the node currently is. Any change
/// in type, signature, constraints or nullability changes the fingerprint
/// while the logical identity stays SAME.
/// </summary>
public sealed record SemanticFingerprint(string Value);
