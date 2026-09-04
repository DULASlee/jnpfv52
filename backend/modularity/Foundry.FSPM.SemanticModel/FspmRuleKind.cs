namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: generic rule kinds. Rules are data, not behavior: evaluation
/// lives in FspmRuleEvaluator (Compiler side) so this assembly stays
/// logic-free and Roslyn-free.
/// </summary>
public enum FspmRuleKind
{
    Required,
    Forbidden,
    Allowed,
    ExactlyOne,
    AtLeastOne,
    TypeCompatible,
    OperationCompatible,
}
