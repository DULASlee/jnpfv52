可以。下面不再做盘点清单、不再讨论方案，**直接给 AI 工程师一份可以开工的详细施工包**。

这次施工包只做一件事：

> **把 FSPM Compiler + Parser + Semantic Binder 真正实现出来，并以真实 C# 工程作为事实源完成可证伪验证。**
>
> **MCP、Construction、Verify Tool 全部暂缓。**
>
> **没有通过本施工包，不得进入下一阶段。**

# 《FSPM Compiler + Parser + Semantic Binder —— 真实 C# 语义编译内核施工包》

**文档编号：FSPM-COMPILER-IMP-001**
**版本：V1.0**
**执行角色：AI 中级工程师**
**架构裁决角色：Chief Architect**
**目标：建立可实际工作的 FSPM 编译、解析、语义绑定内核**

------

# 0. 最高优先级原则

本施工包有且只有一个核心架构原则：

```text
真实 C# 是事实源
FSPM 是真实 C# 的语义编译表示
AI 使用 FSPM
FSPM 必须能够反向定位真实 C# Symbol
```

禁止建立：

```text
FSPM 自己的一套假的 User
FSPM 自己的一套假的 Property
FSPM 自己的一套假的 Method
```

然后再试图“映射”到 C#。

正确关系必须是：

```text
真实 C# Source
      ↓
Roslyn
      ↓
C# ISymbol
      ↓
FSPM Symbol Identity
      ↓
FSPM Semantic Model
      ↓
AI Semantic Surface
```

------

# 1. 本阶段最终必须实现的能力

必须真正实现以下五层。

```text
Layer 1
FSPM Lexer / Parser

Layer 2
FSPM AST

Layer 3
C# Workspace / Roslyn Compiler

Layer 4
Semantic Binder

Layer 5
FSPM Semantic Model
```

最终能力：

```text
FSPM Source
    ↓
Parse
    ↓
AST
    ↓
Compile
    ↓
Bind
    ↓
Real C# Symbol
    ↓
Semantic Model
```

------

# 2. 第一版 FSPM 语言范围

第一版**不要设计一门大语言**。

只支持以下三类语义声明：

```text
entity
property
operation
```

语法锁定为：

```text
entity User

property User.UserName
property User.Password
property User.TenantId

operation User.Login
```

允许空白和注释：

```text
# comment

entity User

property User.UserName
```

禁止第一阶段加入：

```text
if
for
expression
generic
lambda
workflow
UI DSL
database DSL
SQL DSL
```

目标不是“大”，而是：

> **证明 FSPM 可以稳定、确定、真实地理解 C#。**

------

# 3. 工程目录

在现有 FSPM solution 中新增真正的 Compiler 项目。

目标结构：

```text
modularity/
├── Foundry.FSPM.Core/
├── Foundry.FSPM.Analyzer/
├── Foundry.FSPM.Login.Mvp/
│
└── Foundry.FSPM.Compiler/
    ├── Foundry.FSPM.Compiler.csproj
    │
    ├── Lexer/
    │   ├── FspmTokenKind.cs
    │   ├── FspmToken.cs
    │   └── FspmLexer.cs
    │
    ├── Syntax/
    │   ├── FspmSyntaxNode.cs
    │   ├── FspmCompilationUnitSyntax.cs
    │   ├── FspmEntityDeclarationSyntax.cs
    │   ├── FspmPropertyDeclarationSyntax.cs
    │   ├── FspmOperationDeclarationSyntax.cs
    │   └── FspmNameSyntax.cs
    │
    ├── Parser/
    │   ├── FspmParser.cs
    │   └── FspmParseResult.cs
    │
    ├── Diagnostics/
    │   ├── FspmDiagnosticSeverity.cs
    │   ├── FspmDiagnostic.cs
    │   └── FspmDiagnosticCodes.cs
    │
    ├── Workspace/
    │   ├── FspmWorkspace.cs
    │   ├── FspmProjectLoader.cs
    │   └── FspmCompilationLoader.cs
    │
    ├── Symbols/
    │   ├── FspmSymbolId.cs
    │   ├── FspmSymbolKind.cs
    │   ├── FspmBoundSymbol.cs
    │   └── FspmSymbolIdentity.cs
    │
    ├── Binding/
    │   ├── FspmSemanticBinder.cs
    │   ├── EntityBinder.cs
    │   ├── PropertyBinder.cs
    │   ├── OperationBinder.cs
    │   └── BindingResult.cs
    │
    ├── Semantic/
    │   ├── FspmSemanticModel.cs
    │   ├── FspmEntity.cs
    │   ├── FspmProperty.cs
    │   ├── FspmOperation.cs
    │   └── FspmType.cs
    │
    └── Compiler/
        ├── FspmCompilation.cs
        └── FspmCompiler.cs
```

测试：

```text
tests/
└── Foundry.FSPM.Compiler.Tests/
    ├── LexerTests.cs
    ├── ParserTests.cs
    ├── DiagnosticTests.cs
    ├── WorkspaceTests.cs
    ├── SymbolIdentityTests.cs
    ├── EntityBindingTests.cs
    ├── PropertyBindingTests.cs
    ├── OperationBindingTests.cs
    ├── AmbiguityTests.cs
    ├── CrossProjectBindingTests.cs
    ├── NegativeBindingTests.cs
    ├── RebindTests.cs
    └── RealJnpfSemanticTests.cs
```

------

# 4. 阶段一：建立 Compiler 项目

## 4.1 创建项目

执行：

```powershell
dotnet new classlib `
  -n Foundry.FSPM.Compiler `
  -o modularity\Foundry.FSPM.Compiler `
  --framework net8.0
```

加入 solution：

```powershell
dotnet sln zx_lowcode_netcore.sln add `
  modularity\Foundry.FSPM.Compiler\Foundry.FSPM.Compiler.csproj
```

------

# 5. Compiler 依赖

安装 Roslyn / MSBuild Workspace。

执行：

```powershell
dotnet add modularity\Foundry.FSPM.Compiler package Microsoft.CodeAnalysis.CSharp.Workspaces
dotnet add modularity\Foundry.FSPM.Compiler package Microsoft.CodeAnalysis.Workspaces.MSBuild
```

Compiler 必须引用：

```text
Foundry.FSPM.Core
```

执行：

```powershell
dotnet add modularity\Foundry.FSPM.Compiler reference `
  modularity\Foundry.FSPM.Core\Foundry.FSPM.Core.csproj
```

------

# 6. 阶段二：Lexer

Lexer 的任务只有：

```text
字符
 ↓
Token
```

------

## 6.1 FspmTokenKind

创建：

```text
Lexer/FspmTokenKind.cs
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
```

------

# 7. FspmToken

```csharp
namespace Foundry.FSPM.Compiler.Lexer;

public sealed record FspmToken(
    FspmTokenKind Kind,
    string Text,
    int Position,
    int Line,
    int Column);
```

------

# 8. FspmLexer

创建：

```text
Lexer/FspmLexer.cs
```

必须支持：

```text
entity
property
operation
identifier
.
newline
comment
EOF
```

核心实现：

```csharp
namespace Foundry.FSPM.Compiler.Lexer;

public sealed class FspmLexer
{
    public IReadOnlyList<FspmToken> Lex(
        string source)
    {
        var tokens = new List<FspmToken>();

        var position = 0;
        var line = 1;
        var column = 1;

        while (position < source.Length)
        {
            var ch = source[position];

            if (ch == '\r')
            {
                position++;
                continue;
            }

            if (ch == '\n')
            {
                tokens.Add(
                    new FspmToken(
                        FspmTokenKind.NewLine,
                        "\n",
                        position,
                        line,
                        column));

                position++;
                line++;
                column = 1;

                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                position++;
                column++;
                continue;
            }

            if (ch == '#')
            {
                while (
                    position < source.Length &&
                    source[position] != '\n')
                {
                    position++;
                    column++;
                }

                continue;
            }

            if (ch == '.')
            {
                tokens.Add(
                    new FspmToken(
                        FspmTokenKind.Dot,
                        ".",
                        position,
                        line,
                        column));

                position++;
                column++;

                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = position;
                var startColumn = column;

                while (
                    position < source.Length &&
                    (char.IsLetterOrDigit(
                         source[position]) ||
                     source[position] == '_'))
                {
                    position++;
                    column++;
                }

                var text =
                    source[start..position];

                var kind =
                    text switch
                    {
                        "entity" =>
                            FspmTokenKind.EntityKeyword,

                        "property" =>
                            FspmTokenKind.PropertyKeyword,

                        "operation" =>
                            FspmTokenKind.OperationKeyword,

                        _ =>
                            FspmTokenKind.Identifier
                    };

                tokens.Add(
                    new FspmToken(
                        kind,
                        text,
                        start,
                        line,
                        startColumn));

                continue;
            }

            throw new InvalidOperationException(
                $"Invalid character '{ch}' at {line}:{column}.");
        }

        tokens.Add(
            new FspmToken(
                FspmTokenKind.EndOfFile,
                string.Empty,
                position,
                line,
                column));

        return tokens;
    }
}
```

------

# 9. Lexer 必须验证

测试：

```text
entity User
```

必须产生：

```text
EntityKeyword
Identifier(User)
EOF
```

测试：

```text
property User.PhoneNumber
```

必须产生：

```text
PropertyKeyword
Identifier(User)
Dot
Identifier(PhoneNumber)
EOF
```

必须测试：

```text
# hello

entity User
```

注释不得进入 AST。

------

# 10. 阶段三：AST

建立基础语法树。

------

## 10.1 FspmSyntaxNode

```csharp
namespace Foundry.FSPM.Compiler.Syntax;

public abstract record FspmSyntaxNode(
    int Line,
    int Column);
```

------

# 11. CompilationUnit

```csharp
namespace Foundry.FSPM.Compiler.Syntax;

public sealed record FspmCompilationUnitSyntax(
    IReadOnlyList<FspmSyntaxNode> Declarations,
    int Line,
    int Column)
    : FspmSyntaxNode(Line, Column);
```

------

# 12. Entity

```csharp
namespace Foundry.FSPM.Compiler.Syntax;

public sealed record FspmEntityDeclarationSyntax(
    string Name,
    int Line,
    int Column)
    : FspmSyntaxNode(Line, Column);
```

------

# 13. Property

```csharp
namespace Foundry.FSPM.Compiler.Syntax;

public sealed record FspmPropertyDeclarationSyntax(
    string EntityName,
    string PropertyName,
    int Line,
    int Column)
    : FspmSyntaxNode(Line, Column);
```

------

# 14. Operation

```csharp
namespace Foundry.FSPM.Compiler.Syntax;

public sealed record FspmOperationDeclarationSyntax(
    string EntityName,
    string OperationName,
    int Line,
    int Column)
    : FspmSyntaxNode(Line, Column);
```

------

# 15. 阶段四：Parser

Parser 必须完整区分：

```text
Lexical error
Syntax error
Semantic error
```

三者绝对不能混。

------

# 16. Diagnostic

```csharp
namespace Foundry.FSPM.Compiler.Diagnostics;

public enum FspmDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
```

------

```csharp
namespace Foundry.FSPM.Compiler.Diagnostics;

public sealed record FspmDiagnostic(
    string Code,
    FspmDiagnosticSeverity Severity,
    string Message,
    int Line,
    int Column);
```

------

# 17. Diagnostic Codes

```csharp
namespace Foundry.FSPM.Compiler.Diagnostics;

public static class FspmDiagnosticCodes
{
    public const string UnexpectedToken = "FSPM001";
    public const string MissingIdentifier = "FSPM002";
    public const string MissingDot = "FSPM003";
    public const string DuplicateDeclaration = "FSPM004";

    public const string EntityNotFound = "FSPM101";
    public const string PropertyNotFound = "FSPM102";
    public const string OperationNotFound = "FSPM103";

    public const string AmbiguousEntity = "FSPM111";
    public const string AmbiguousProperty = "FSPM112";
    public const string AmbiguousOperation = "FSPM113";
}
```

------

# 18. FspmParser

必须实现：

```text
parseCompilationUnit
parseEntity
parseProperty
parseOperation
```

输入：

```text
entity User

property User.UserName

operation User.Login
```

产生：

```text
CompilationUnit
 ├── Entity(User)
 ├── Property(User.UserName)
 └── Operation(User.Login)
```

------

# 19. Parser 测试矩阵

必须全部测试。

### 正向

```text
entity User
property User.UserName
operation User.Login
entity User

property User.UserName

property User.Password

operation User.Login
```

### 负向

```text
foo User
```

必须：

```text
FSPM001
property User
```

必须：

```text
FSPM003
property .UserName
```

必须：

```text
FSPM002
```

------

# 20. 阶段五：Workspace Loader

此阶段正式进入：

> **真实 C#。**

必须采用：

```text
MSBuildWorkspace
```

加载：

```text
.sln
.csproj
```

------

# 21. FspmWorkspace

创建：

```text
Workspace/FspmWorkspace.cs
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Workspace;

public sealed class FspmWorkspace
{
    public required string RootPath { get; init; }

    public required AdhocWorkspace Workspace { get; init; }

    public required IReadOnlyList<Project> Projects { get; init; }
}
```

------

# 22. FspmProjectLoader

必须支持：

```text
Solution
Project
Compilation
```

入口：

```csharp
Task<FspmWorkspace> LoadSolutionAsync(
    string solutionPath,
    CancellationToken cancellationToken)
```

内部必须真实调用：

```csharp
MSBuildWorkspace.OpenSolutionAsync(...)
```

------

# 23. 编译加载标准

成功加载后必须取得：

```text
Microsoft.CodeAnalysis.Compilation
```

并且：

```text
compilation.AssemblyName
compilation.SourceModule
compilation.GlobalNamespace
```

都可访问。

------

# 24. 第一条硬门禁

真实项目无法获得：

```text
Compilation
```

则：

```text
FSPM Compiler = FAIL
```

不得继续 Semantic Binder。

------

# 25. 阶段六：Symbol Identity

这是整个系统最重要的一层。

必须创建：

```text
Symbols/FspmSymbolKind.cs
namespace Foundry.FSPM.Compiler.Symbols;

public enum FspmSymbolKind
{
    Entity,
    Property,
    Operation
}
```

------

# 26. FspmSymbolId

必须具备确定性。

```csharp
namespace Foundry.FSPM.Compiler.Symbols;

public readonly record struct FspmSymbolId(
    string Value);
```

------

# 27. Symbol Identity

**禁止：**

```text
User
User.Login
```

直接作为唯一 symbol identity。

这些只是 display name。

必须建立：

```text
Assembly
Namespace
MetadataName
ContainingType
MemberSignature
```

组成稳定身份。

对于 .NET Symbol，优先采用 Roslyn 提供的稳定标识机制，例如：

```text
SymbolKey
DocumentationCommentId
```

或者建立经过 canonicalization 的 Symbol Fingerprint。

------

# 28. SymbolIdentity

创建：

```text
Symbols/FspmSymbolIdentity.cs
```

必须至少支持：

```csharp
FspmSymbolId Create(ITypeSymbol symbol);
FspmSymbolId Create(IPropertySymbol symbol);
FspmSymbolId Create(IMethodSymbol symbol);
```

要求：

```text
同一真实 Symbol
→ 相同 ID

不同真实 Symbol
→ 不同 ID
```

必须有单元测试。

------

# 29. 阶段七：FSPM Semantic Model

此时才创建：

```text
FspmEntity
FspmProperty
FspmOperation
```

------

# 30. FspmEntity

```csharp
namespace Foundry.FSPM.Compiler.Semantic;

public sealed record FspmEntity(
    FspmSymbolId Id,
    string Name,
    string Namespace,
    string FilePath,
    int Line);
```

------

# 31. FspmProperty

```csharp
namespace Foundry.FSPM.Compiler.Semantic;

public sealed record FspmProperty(
    FspmSymbolId Id,
    FspmSymbolId EntityId,
    string Name,
    string TypeName,
    string FilePath,
    int Line);
```

------

# 32. FspmOperation

```csharp
namespace Foundry.FSPM.Compiler.Semantic;

public sealed record FspmOperation(
    FspmSymbolId Id,
    FspmSymbolId EntityId,
    string Name,
    string ReturnType,
    IReadOnlyList<string> Parameters,
    string FilePath,
    int Line);
```

------

# 33. Semantic Model

```csharp
namespace Foundry.FSPM.Compiler.Semantic;

public sealed class FspmSemanticModel
{
    public List<FspmEntity> Entities { get; } = [];

    public List<FspmProperty> Properties { get; } = [];

    public List<FspmOperation> Operations { get; } = [];
}
```

------

# 34. 但这里有一个关键要求

这些 FSPM 对象不能只有：

```text
Name
```

必须能够追溯：

```text
FspmSymbolId
     ↓
Real Roslyn ISymbol
     ↓
Source Location
```

因此必须内部维护：

```text
FspmBoundSymbol
```

------

# 35. FspmBoundSymbol

```csharp
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Symbols;

public sealed record FspmBoundSymbol(
    FspmSymbolId Id,
    FspmSymbolKind Kind,
    ISymbol Symbol);
```

这样：

```text
FspmProperty
```

与：

```text
IPropertySymbol
```

才能真正绑定。

------

# 36. 阶段八：Entity Binder

输入：

```text
entity User
```

必须去 Roslyn：

```text
Compilation.GlobalNamespace
```

寻找真实：

```text
INamedTypeSymbol
```

------

# 37. Entity Binder 的唯一成功条件

必须满足：

```text
匹配数量 == 1
```

结果：

```text
RESOLVED
```

------

如果：

```text
0
```

则：

```text
NOT_FOUND
```

------

如果：

```text
>1
```

则：

```text
AMBIGUOUS
```

**绝不能选择第一个。**

------

# 38. 类型搜索算法

必须支持：

```text
User
```

以及：

```text
JNPF.Domain.User
```

并且考虑：

```text
namespace
project
assembly
```

------

# 39. Property Binder

输入：

```text
User.UserName
```

首先解析：

```text
User
```

然后取得：

```text
INamedTypeSymbol
```

再：

```csharp
entity.GetMembers()
```

寻找：

```text
IPropertySymbol
```

匹配：

```text
Name == UserName
```

成功条件仍然：

```text

```

------

# 40. Property 必须保存真实类型

例如：

```csharp
public string UserName { get; set; }
```

最终：

```text
FspmProperty.TypeName = System.String
```

而不是简单：

```text
string
```

也不能只保留：

```text
"UserName"
```

------

# 41. Operation Binder

输入：

```text
User.Login
```

必须得到：

```text
IMethodSymbol
```

支持：

```text
Login()
Login(string)
Login(string,string)
```

等真实重载。

------

# 42. 第一版重载规则

如果 FSPM：

```text
operation User.Login
```

没有提供参数类型：

```text
0 match → NOT_FOUND
1 match → RESOLVED
>1 match → AMBIGUOUS
```

绝不允许：

```text
First()
```

------

# 43. 阶段九：统一 Semantic Binder

创建：

```text
Binding/FspmSemanticBinder.cs
```

主入口：

```csharp
BindingResult Bind(
    FspmCompilationUnitSyntax syntax,
    Compilation compilation);
```

执行：

```text
AST
 ↓
EntityBinder
 ↓
PropertyBinder
 ↓
OperationBinder
 ↓
FspmSemanticModel
```

------

# 44. BindingResult

```text
SUCCESS
PARTIAL
FAILED
```

并且每一项都有：

```text
Resolved
NotFound
Ambiguous
Invalid
```

------

# 45. 阶段十：FSPM Compiler

现在才允许创建：

```text
Compiler/FspmCompiler.cs
```

整个 Compiler：

```text
FspmCompiler.CompileAsync(...)
```

执行完整流水线：

```text
Source
 ↓
Lexer
 ↓
Tokens
 ↓
Parser
 ↓
AST
 ↓
Workspace Loader
 ↓
Roslyn Compilation
 ↓
Semantic Binder
 ↓
Semantic Model
 ↓
Diagnostics
```

------

# 46. Compiler API

要求提供：

```csharp
public sealed class FspmCompiler
{
    public async Task<FspmCompilationResult>
        CompileAsync(
            string fspmSource,
            string solutionPath,
            CancellationToken cancellationToken = default);
}
```

------

# 47. Compilation Result

```csharp
public sealed record FspmCompilationResult(
    bool Succeeded,
    FspmCompilationUnitSyntax Syntax,
    FspmSemanticModel SemanticModel,
    IReadOnlyList<FspmDiagnostic> Diagnostics);
```

------

# 48. 第一版 Compiler 的真实定义

只有同时满足：

```text
Lex PASS
Parse PASS
Roslyn Compilation PASS
Binding PASS
Semantic Model PASS
```

才允许：

```text
Succeeded = true
```

否则：

```text
Succeeded = false
```

------

# 49. 阶段十一：最重要的 Golden Fixture

建立专门的真实 C# fixture：

```text
tests/
└── Fixtures/
    └── SemanticGolden/
        ├── SemanticGolden.csproj
        ├── Domain/
        │   └── User.cs
        └── Application/
            └── UserService.cs
```

------

# 50. User.cs

```csharp
namespace SemanticGolden.Domain;

public sealed class User
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public bool Login()
    {
        return true;
    }
}
```

------

# 51. 第一组真实验证

输入 FSPM：

```text
entity User

property User.UserName

property User.Password

property User.TenantId

operation User.Login
```

Compiler 必须返回：

```text
Succeeded = true
```

并解析：

```text
Entity/User

Property/User.UserName

Property/User.Password

Property/User.TenantId

Operation/User.Login
```

------

# 52. 但必须进一步验证真实 Symbol

不能只验证：

```text
Name == User
```

必须：

```csharp
var entity = ...
entity.Symbol.Should().BeAssignableTo<INamedTypeSymbol>();
```

Property：

```csharp
property.Symbol
    .Should()
    .BeAssignableTo<IPropertySymbol>();
```

Operation：

```csharp
operation.Symbol
    .Should()
    .BeAssignableTo<IMethodSymbol>();
```

------

# 53. 必须做反向验证

这是本工程最关键的测试之一。

给定：

```text
Property/User.UserName
```

必须能够获取：

```text
真实 C# 文件
真实行号
真实 IPropertySymbol
```

然后断言：

```text
ContainingType.Name == User
Name == UserName
Type == System.String
```

------

# 54. Negative Test

测试：

```text
property User.NotExist
```

结果必须：

```text
PropertyNotFound
```

不能：

```text
Succeeded = true
```

------

# 55. Ambiguity Test

创建：

```text
namespace A;

public class User
{
}
```

和：

```text
namespace B;

public class User
{
}
```

输入：

```text
entity User
```

结果必须：

```text
AMBIGUOUS
```

不能自行选择：

```text
A.User
```

------

# 56. Fully Qualified Resolution

输入：

```text
entity A.User
```

必须：

```text
RESOLVED
```

到：

```text
A.User
```

输入：

```text
entity B.User
```

必须：

```text
RESOLVED
```

到：

```text
B.User
```

------

# 57. Property Ambiguity

测试真实继承：

```csharp
class BaseUser
{
    public string Name { get; set; } = "";
}

class User : BaseUser
{
}
```

判断：

```text
User.Name
```

能否解析。

如果支持继承：

必须确定性返回继承成员。

如果第一版不支持：

必须：

```text
UNSUPPORTED
```

而不能错误返回：

```text
NOT_FOUND
```

------

# 58. Operation Overload

建立：

```csharp
public bool Login()
{
    return true;
}

public bool Login(string userName)
{
    return true;
}
```

输入：

```text
operation User.Login
```

必须：

```text
AMBIGUOUS
```

然后扩展语法：

```text
operation User.Login(string)
```

必须能够唯一绑定。

**如果第一版暂时不支持参数签名语法，则明确返回 `AMBIGUOUS/UNSUPPORTED`，绝对禁止猜。**

------

# 59. Cross-project Binding

必须至少建立：

```text
Project A
    User.cs

Project B
    UserService.cs
```

B 引用 A。

然后：

```text
entity User
```

和：

```text
operation UserService.Login
```

能够跨项目解析。

------

# 60. Symbol Identity Test

同一个 `User`：

```text
Project A parse
Project A recompile
```

Symbol Identity 必须保持稳定。

不同：

```text
A.User
B.User
```

必须具有不同 Identity。

------

# 61. Determinism Test

同一个工程连续执行：

```text
compile #1
compile #2
compile #3
```

三次：

```text
SemanticRef
SymbolId
FilePath
Line
QualifiedName
```

都必须一致。

------

# 62. Compiler Mutation Test

修改：

```csharp
public string UserName
```

为：

```csharp
public int UserName
```

重新执行 Compiler。

FSPM 必须观察到：

```text
Property/User.UserName
Type = System.Int32
```

而不是缓存旧结果。

------

# 63. Source Mutation Rebind Test

再进一步：

初始：

```csharp
public string UserName { get; set; }
```

修改源码：

```csharp
public string Email { get; set; }
```

Compiler 重新运行：

```text
User.UserName
→ NOT_FOUND

User.Email
→ RESOLVED
```

这证明：

> FSPM 是实时绑定真实代码，而不是加载一次就永久缓存。

------

# 64. 第一个“昨天问题”的核心验收

必须执行：

```text
UserForm
    ↓
EntityRef = User

PhoneInput
    ↓
PropertyRef = User.PhoneNumber

SubmitButton
    ↓
OperationRef = User.Create
```

但这次不是把它们直接当 JSON。

必须建立：

```text
EntityRef
    ↓
FspmEntity
    ↓
FspmSymbolId
    ↓
INamedTypeSymbol(User)
PropertyRef
    ↓
FspmProperty
    ↓
FspmSymbolId
    ↓
IPropertySymbol(User.PhoneNumber)
OperationRef
    ↓
FspmOperation
    ↓
FspmSymbolId
    ↓
IMethodSymbol(User.Create)
```

这个测试不过：

> **FSPM Semantic Foundation = FAIL**

------

# 65. 最终 Compiler CLI

为了不依赖 MCP，先提供一个简单 CLI。

创建：

```text
tools/fspm-compiler/
```

或者在现有 Compiler 项目中增加：

```text
Program.cs
```

执行：

```powershell
dotnet run --project modularity\Foundry.FSPM.Compiler `
  -- `
  --solution D:\JNPF-v52\zx_lowcode_netcore.sln `
  --fspm D:\JNPF-v52\.fspm\sample.fspm
```

输出：

```text
FSPM Compilation
================

Parse        : PASS
C# Compile   : PASS
Entity Bind  : PASS
Property Bind: PASS
Operation Bind: PASS

Diagnostics  : 0

Result       : SUCCESS
```

------

# 66. 禁止这些伪实现

以下全部直接判定：

```text
FAIL
```

### 1

```csharp
if (name == "User")
    return true;
```

### 2

```csharp
return new SemanticRef("User");
```

但没有真实 Roslyn Symbol。

### 3

```csharp
FirstOrDefault()
```

处理 ambiguity。

### 4

```csharp
"User.Login"
```

直接字符串映射。

### 5

```csharp
Dictionary<string, string>
```

冒充 Symbol Resolver。

### 6

测试 fixture 与真实 C# 完全脱离。

### 7

Mock `ITypeSymbol` 来证明真实 Binding。

------

# 67. 必须实现的状态机

每个 Semantic Reference 必须有：

```text
UNRESOLVED
     ↓
RESOLVING
     ↓
RESOLVED
```

失败：

```text
NOT_FOUND
AMBIGUOUS
INVALID
UNSUPPORTED
```

禁止：

```text
catch Exception
→ RESOLVED = false
```

错误必须保留真实诊断。

------

# 68. Diagnostic 必须可以定位源码

例如：

```text
property User.NotExist
```

必须报告：

```text
FSPM102:
Property 'User.NotExist' could not be resolved.

Location:
line 3, column 1
```

这样未来 AI 才能修复 FSPM 源，而不是面对：

```text
false
```

------

# 69. 真实 JNPF 验证

只有 Golden Fixture 全部通过，才允许进入真实 JNPF。

执行：

```powershell
dotnet build <真实JNPF项目>
```

确认：

```text
Compilation 可获得
```

然后：

```text
FSPM Compiler
    ↓
真实 JNPF Solution
```

至少解析：

```text
真实 Entity
真实 Property
真实 Method
```

------

# 70. JNPF 第一目标不是“User.Login”

这一点也必须纠正。

工程师不得假定：

```text
User
User.UserName
User.Password
User.Login
```

一定存在。

应该：

```text
扫描真实 symbols
        ↓
找到真实 User 类
        ↓
找到真实认证相关 operation
        ↓
记录事实
```

如果真实代码名称不是：

```text
User
Login
```

也不能为了通过测试创建假的类。

------

# 71. 真正的 JNPF Semantic Report

最终必须生成：

```text
.fspm/
└── semantic/
    └── jnpf-baseline.json
```

内容至少包括：

```json
{
  "workspace": "...",
  "solution": "...",
  "projects": 0,
  "entities": 0,
  "properties": 0,
  "operations": 0,
  "diagnostics": []
}
```

数字必须来自实际扫描。

------

# 72. 必须有编译器综合测试

最终：

```powershell
dotnet test tests\Foundry.FSPM.Compiler.Tests
```

至少覆盖：

```text
Lexer
Parser
Diagnostics
Workspace
Entity Binding
Property Binding
Operation Binding
Ambiguity
Cross Project
Negative
Determinism
Rebind
Real JNPF
```

------

# 73. 最终 Gate

必须全部满足：

```text
G01 Lexer PASS
G02 Parser PASS
G03 Syntax Diagnostics PASS
G04 MSBuildWorkspace PASS
G05 Real Compilation PASS
G06 Entity Binding PASS
G07 Property Binding PASS
G08 Operation Binding PASS
G09 Symbol Identity PASS
G10 Source Location PASS
G11 NotFound PASS
G12 Ambiguous PASS
G13 CrossProject PASS
G14 Determinism PASS
G15 Rebind PASS
G16 Mutation PASS
G17 Golden Fixture PASS
G18 Real JNPF PASS
```

------

# 74. 最终 PROVEN 标准

只有出现以下结果，才能把阶段状态标记为：

```text
FSPM-COMPILER = PROVEN
FSPM-PARSER = PROVEN
FSPM-SEMANTIC-BINDER = PROVEN
```

并且必须留下：

```text
真实源码
+
真实测试
+
真实编译输出
+
真实错误触发证据
+
真实 Symbol Binding 证据
+
真实 JNPF 验证报告
```

------

# 75. 第一阶段最终成果

最终必须真正存在：

```text
FSPM Source
       ↓
Lexer
       ↓
Parser
       ↓
AST
       ↓
Roslyn
       ↓
C# Compilation
       ↓
Semantic Binder
       ↓
FSPM Semantic Model
       ↓
Real ISymbol
```

并且：

```text
EntityRef
PropertyRef
OperationRef
```

都能够严格做到：

```text
FSPM semantic identity
        ↕
FSPM SymbolId
        ↕
Roslyn ISymbol
        ↕
真实 C# 文件
        ↕
真实代码位置
```

到这一阶段结束之前：

**禁止实现 `fspm_construct`。**

**禁止实现 MCP Adapter。**

**禁止拿 Login.Mvp 的 HTTP 成功当成 FSPM 成功。**

------

## Chief Architect 最终施工命令

AI 工程师现在只执行下面这一个任务：

```text
START
  ↓
建立 Foundry.FSPM.Compiler
  ↓
实现 Lexer
  ↓
实现 Parser
  ↓
实现 AST
  ↓
实现 Diagnostics
  ↓
实现 MSBuild/Roslyn Workspace
  ↓
实现 C# Compilation
  ↓
实现 Symbol Identity
  ↓
实现 Entity Binder
  ↓
实现 Property Binder
  ↓
实现 Operation Binder
  ↓
实现 Semantic Model
  ↓
实现统一 FspmCompiler
  ↓
Golden C# Fixture
  ↓
Negative Tests
  ↓
Ambiguity Tests
  ↓
Cross-Project Tests
  ↓
Rebind Tests
  ↓
真实 JNPF
  ↓
18 Gates 全部通过
  ↓
FSPM Compiler / Parser / Binder = PROVEN
  ↓
STOP
```

**这一阶段的唯一完成定义就是：AI 不再需要“猜 User 是哪个 User、PhoneNumber 是哪个 Property、Login 是哪个 Method”。FSPM 必须通过 Roslyn 把 AI 的语义引用牢牢钉在真实 C# Symbol 上。**

这才是后面的 **Construction → Verification → MCP** 可以建立在上面的真正基础。