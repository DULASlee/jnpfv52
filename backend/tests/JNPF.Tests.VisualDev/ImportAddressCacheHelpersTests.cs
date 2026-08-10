using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: GetCDataList ADDRESS tree → import cache pairs (VisualDev + CodeGen).
/// </summary>
public class ImportAddressCacheHelpersTests
{
    [Fact]
    public void CacheKey_IsImportAddress()
        => Assert.Equal("Import_Address", ImportAddressCacheHelpers.CacheKey);

    [Fact]
    public void BuildPairs_TypedLevels_DuplicateAddIsLegacy()
    {
        // Type1 + Type2: each level adds during walk, then all Description rows add again.
        var provinces = new List<ProvinceEntity>
        {
            new() { Id = "p1", ParentId = "-1", Type = "1", FullName = "省A" },
            new() { Id = "c1", ParentId = "p1", Type = "2", FullName = "市B" },
        };

        var pairs = ImportAddressCacheHelpers.BuildPairs(provinces);

        Assert.Equal(4, pairs.Count); // p1, c1 during walk + p1, c1 again in ForEach
        Assert.Contains(pairs, d => d.ContainsKey("p1") && d["p1"] == "省A");
        Assert.Contains(pairs, d => d.ContainsKey("p1,c1") && d["p1,c1"] == "省A/市B");
        Assert.Equal(2, pairs.Count(d => d.ContainsKey("p1")));
    }

    [Fact]
    public void BuildPairs_Type4_SkipsWhenParentMissing()
    {
        var provinces = new List<ProvinceEntity>
        {
            new() { Id = "s1", ParentId = "missing", Type = "4", FullName = "街道" },
        };

        var pairs = ImportAddressCacheHelpers.BuildPairs(provinces);
        Assert.Empty(pairs);
    }

    [Fact]
    public void BuildPairs_NoType_UsesRecursiveParentPath()
    {
        var provinces = new List<ProvinceEntity>
        {
            new() { Id = "r1", ParentId = "-1", Type = null, FullName = "根" },
            new() { Id = "r2", ParentId = "r1", Type = "  ", FullName = "子" },
        };

        var pairs = ImportAddressCacheHelpers.BuildPairs(provinces);
        Assert.Contains(pairs, d => d.ContainsKey("r1") && d["r1"] == "根");
        Assert.Contains(pairs, d => d.ContainsKey("r1,r2") && d["r1,r2"] == "根/子");
    }

    [Fact]
    public void BuildOrganizeTreePairs_JoinsNamesByTree()
    {
        var all = new List<JNPF.Systems.Entitys.Permission.OrganizeEntity>
        {
            new() { Id = "a", OrganizeIdTree = "a", FullName = "集团" },
            new() { Id = "b", OrganizeIdTree = "a,b", FullName = "部门" },
        };

        var pairs = ImportAddressCacheHelpers.BuildOrganizeTreePairs(all, all);
        Assert.Equal(2, pairs.Count);
        Assert.Equal("集团", pairs[0]["a"]);
        Assert.Equal("集团/部门", pairs[1]["a,b"]);
    }

    [Fact]
    public void BuildOrganizeTreePairs_EmptyTree_FallsBackToId()
    {
        var all = new List<JNPF.Systems.Entitys.Permission.OrganizeEntity>
        {
            new() { Id = "x", OrganizeIdTree = null, FullName = "X" },
            new() { Id = "y", OrganizeIdTree = string.Empty, FullName = "Y" },
        };

        var pairs = ImportAddressCacheHelpers.BuildOrganizeTreePairs(all, all);
        Assert.Equal(2, pairs.Count);
        Assert.Equal("X", pairs[0]["x"]);
        Assert.Equal("Y", pairs[1]["y"]);
    }

    [Fact]
    public void BuildPairs_NoType_MutatesIdOnChild()
    {
        var child = new ProvinceEntity { Id = "r2", ParentId = "r1", Type = null, FullName = "子" };
        var provinces = new List<ProvinceEntity>
        {
            new() { Id = "r1", ParentId = "-1", Type = null, FullName = "根" },
            child,
        };

        ImportAddressCacheHelpers.BuildPairs(provinces);
        // GetAddressIdByPList mutates Id in place (legacy)
        Assert.Equal("r1,r2", child.Id);
    }

    [Fact]
    public void BuildIdEncodePairs_MapsIdToEncode()
    {
        var pairs = ImportAddressCacheHelpers.BuildIdEncodePairs(
            new[] { ("g1", "G01"), ("g2", "G02") });
        Assert.Equal(2, pairs.Count);
        Assert.Equal("G01", pairs[0]["g1"]);
        Assert.Equal("G02", pairs[1]["g2"]);
    }
}
