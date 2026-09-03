using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H2: Type Shape & Generic / Nullable Semantics. Every shape field is
// read from Roslyn (ITypeSymbol family); no generic inference reimplemented.
[Collection("RoslynWorkspace")]
public sealed class H2TypeShapeTests
{
    [Fact]
    public async Task NamedTypeShape_User_ReportsOriginalDefinition()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeUser");
            var shape = TypeShapeExtractor.ExtractTypeShape(user);

            Assert.Equal(NativeTypeShapeKind.NamedType, shape.Kind);
            Assert.Equal("SemanticGolden.Shapes.ShapeUser", shape.OriginalDefinition);
            Assert.Empty(shape.TypeArguments);
        }
    }

    [Fact]
    public async Task ConstructedGeneric_ListOfUser_ReportsTypeArguments()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var users = GoldenIdentity.RequireProperty(holder, "Users");
            var shape = TypeShapeExtractor.ExtractTypeShape(users.Type);

            Assert.Equal(NativeTypeShapeKind.ConstructedGeneric, shape.Kind);
            Assert.Equal("System.Collections.Generic.List<T>", shape.OriginalDefinition);
            var arg = Assert.Single(shape.TypeArguments);
            Assert.Equal("SemanticGolden.Shapes.ShapeUser", arg);
        }
    }

    [Fact]
    public async Task NullableReference_String_KeepsNullableAnnotation()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var maybe = GoldenIdentity.RequireProperty(holder, "MaybeName");
            var shape = TypeShapeExtractor.ExtractTypeShape(maybe.Type);

            Assert.Equal("Annotated", shape.NullableAnnotation);
        }
    }

    [Fact]
    public async Task NonNullableReference_Name_IsNotAnnotated()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var name = GoldenIdentity.RequireProperty(holder, "Name");
            var shape = TypeShapeExtractor.ExtractTypeShape(name.Type);

            Assert.Equal("NotAnnotated", shape.NullableAnnotation);
        }
    }

    [Fact]
    public async Task String_Differs_From_NullableString()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var plain = TypeShapeExtractor.ExtractTypeShape(
                GoldenIdentity.RequireProperty(holder, "Name").Type);
            var nullable = TypeShapeExtractor.ExtractTypeShape(
                GoldenIdentity.RequireProperty(holder, "MaybeName").Type);

            Assert.NotEqual(plain.NullableAnnotation, nullable.NullableAnnotation);
        }
    }

    [Fact]
    public async Task ListOfNullableUser_KeepsElementNullability()
    {
        // Chief §九 hard assertion: element-level nullability must survive.
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var shape = TypeShapeExtractor.ExtractTypeShape(
                GoldenIdentity.RequireProperty(holder, "MaybeUsers").Type);

            Assert.Equal(NativeTypeShapeKind.ConstructedGeneric, shape.Kind);
            var arg = Assert.Single(shape.TypeArguments);
            Assert.Equal("SemanticGolden.Shapes.ShapeUser?", arg);
        }
    }

    [Fact]
    public async Task ArrayShape_ReportsRank()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeHolder");
            var shape = TypeShapeExtractor.ExtractTypeShape(
                GoldenIdentity.RequireProperty(holder, "Matrix").Type);

            Assert.Equal(NativeTypeShapeKind.Array, shape.Kind);
            Assert.Equal(2, shape.ArrayRank);
            Assert.Equal("int", shape.ElementType);
        }
    }

    [Fact]
    public async Task GenericConstraints_AreReported()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var repo = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Shapes.ShapeRepository`1");
            var param = Assert.Single(repo.TypeParameters);
            var constraints = GenericConstraintExtractor.Extract(param);

            Assert.Contains("class", constraints.Constraints);
            Assert.Contains("new()", constraints.Constraints);
        }
    }
}
