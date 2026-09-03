namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01: unified semantic identity ("which logical node" +
/// "currently what"). LogicalId reuses the frozen P13 FspmSymbolId value
/// (Assembly|DocId, stable across compilations); Fingerprint is the P13
/// semantic fingerprint (changes when shape changes). Plain strings only.
/// </summary>
public sealed record FspmSemanticIdentity(
    string LogicalId,
    string Fingerprint);
