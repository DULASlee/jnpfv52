namespace Foundry.FSPM.Compiler.Lexer;

public enum FspmTokenKind
{
    EndOfFile,

    Identifier,

    EntityKeyword,
    PropertyKeyword,
    OperationKeyword,

    Dot,

    NewLine
}
