using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core;

/// <summary>
/// Phase 2-A 验收测试：RuntimeContext 三元组、R12 合规、不可变性。
/// </summary>
public sealed class RuntimeContextTests
{
    [Fact]
    public void Create_WithValidTriple_ReturnsContext()
    {
        // Act
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");

        // Assert
        Assert.Equal("tenant-1", context.TenantId);
        Assert.Equal("project-1", context.ProjectId);
        Assert.Equal("pipeline-1", context.PipelineId);
        Assert.Equal("user-1", context.CreatorUserId);
        Assert.NotEqual(default, context.CreatedAtUtc);
        Assert.Empty(context.Metadata);
    }

    [Fact]
    public void Create_WithEmptyTenantId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeContext.Create("", "project-1", "pipeline-1", "user-1"));
    }

    [Fact]
    public void Create_WithEmptyProjectId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeContext.Create("tenant-1", "", "pipeline-1", "user-1"));
    }

    [Fact]
    public void Create_WithEmptyPipelineId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeContext.Create("tenant-1", "project-1", "", "user-1"));
    }

    [Fact]
    public void Create_WithEmptyCreatorUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", ""));
    }

    [Fact]
    public void WithMetadata_ReturnsNewContext()
    {
        // Arrange
        var original = RuntimeContext.Create("t", "p", "pl", "u");

        // Act
        var updated = original.WithMetadata("key1", "value1");

        // Assert
        Assert.NotSame(original, updated);
        Assert.Equal("value1", updated.Metadata["key1"]);
        Assert.Empty(original.Metadata);
    }

    [Fact]
    public void WithMetadata_OverridesExistingKey()
    {
        // Arrange
        var original = RuntimeContext.Create("t", "p", "pl", "u").WithMetadata("key1", "v1");

        // Act
        var updated = original.WithMetadata("key1", "v2");

        // Assert
        Assert.Equal("v2", updated.Metadata["key1"]);
        Assert.Equal("v1", original.Metadata["key1"]);
    }
}
