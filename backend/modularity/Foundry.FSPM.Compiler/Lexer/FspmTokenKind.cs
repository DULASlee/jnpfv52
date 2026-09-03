namespace Foundry.FSPM.Compiler.Lexer;

public enum FspmTokenKind
{
    EndOfFile,

    Identifier,

    EntityKeyword,
    PropertyKeyword,
    OperationKeyword,

    // ===== Phase 12 Construction keywords (L2 composite layer) =====
    PageKeyword,
    FormKeyword,
    FieldKeyword,
    SubmitKeyword,

    Dot,

    // ===== Phase 12 Construction punctuation =====
    LBrace,
    RBrace,
    LParen,
    RParen,
    LBracket,
    RBracket,
    Question,
    Arrow,

    StringLiteral,
    NumericLiteral,

    NewLine
}
