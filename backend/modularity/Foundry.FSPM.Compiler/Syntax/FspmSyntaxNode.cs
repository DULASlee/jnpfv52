namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// Base type for every node in the FSPM Syntax Tree (AST).
/// Phase 3 - 施工包 10.1:
///   - abstract record (immutable, value equality)
///   - carries Source Position so every AST node is traceable back to the
///     original source slice (Hard Constraint: AST 节点可追溯源码位置)
/// Architectural rule (Phase 3 授权硬门禁):
///   Syntax nodes MUST NOT contain any Semantic / Symbol references.
///   They express "what the source wrote", never "what the source means".
///   Symbol / Binding / Semantic Model belong to Phase 7 / 8 / 9.
/// </summary>
public abstract record FspmSyntaxNode(
    int Start,
    int Length,
    int Line,
    int Column);
