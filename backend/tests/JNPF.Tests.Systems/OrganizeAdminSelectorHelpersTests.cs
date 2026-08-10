using JNPF.Systems.Common.OrganizeAdmin;
using JNPF.Systems.Entitys.Dto.Permission.OrganizeAdministrator;
using JNPF.Systems.Entitys.Permission;
using Xunit;

namespace JNPF.Tests.Systems;

/// <summary>
/// Characterization tests for OrganizeAdministratorService.GetSelector pure helpers.
/// </summary>
public class OrganizeAdminSelectorHelpersTests
{
    [Theory]
    [InlineData(0, 0, -1)]
    [InlineData(1, 1, 1)]
    [InlineData(3, 1, 1)]
    [InlineData(1, 0, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(0, 1, 2)]
    [InlineData(0, 3, 3)]
    [InlineData(2, 1, 0)] // no branch → 0
    public void MergeAdminUserPermissionFlag_MatchesFiveBranchUiMatrix(int admin, int user, int expected)
    {
        Assert.Equal(expected, OrganizeAdminSelectorHelpers.MergeAdminUserPermissionFlag(admin, user));
    }

    [Fact]
    public void HasAnyLayerPermission_TrueWhenAnyOfEightIsOne()
    {
        Assert.False(OrganizeAdminSelectorHelpers.HasAnyLayerPermission(new OrganizeAdministratorEntity()));
        Assert.True(OrganizeAdminSelectorHelpers.HasAnyLayerPermission(new OrganizeAdministratorEntity { ThisLayerSelect = 1 }));
        Assert.True(OrganizeAdminSelectorHelpers.HasAnyLayerPermission(new OrganizeAdministratorEntity { SubLayerDelete = 1 }));
        Assert.False(OrganizeAdminSelectorHelpers.HasAnyLayerPermission(new OrganizeAdministratorEntity { ThisLayerAdd = 3 }));
    }

    [Fact]
    public void HasAnySubLayerPermission_OnlySubLayerOnes()
    {
        Assert.False(OrganizeAdminSelectorHelpers.HasAnySubLayerPermission(new OrganizeAdministratorEntity { ThisLayerAdd = 1 }));
        Assert.True(OrganizeAdminSelectorHelpers.HasAnySubLayerPermission(new OrganizeAdministratorEntity { SubLayerAdd = 1 }));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 2)]
    [InlineData(3, 3)]
    [InlineData(2, 0)]
    public void MapUserOnlyPermissionFlag_MatchesLegacyBranches(int userVal, int expected)
    {
        Assert.Equal(expected, OrganizeAdminSelectorHelpers.MapUserOnlyPermissionFlag(userVal));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(2, -1)]
    public void MapAdminOnlyPermissionFlag_VisibleZeroOrHiddenMinusOne(int adminVal, int expected)
    {
        Assert.Equal(expected, OrganizeAdminSelectorHelpers.MapAdminOnlyPermissionFlag(adminVal));
    }

    [Fact]
    public void ApplyMergedAdminUserPermissionFlags_FillsAllEightFields()
    {
        var admin = new OrganizeAdministratorEntity
        {
            ThisLayerAdd = 0,
            ThisLayerEdit = 1,
            ThisLayerDelete = 0,
            ThisLayerSelect = 3,
            SubLayerAdd = 0,
            SubLayerEdit = 1,
            SubLayerDelete = 0,
            SubLayerSelect = 0,
        };
        var user = new OrganizeAdministratorEntity
        {
            ThisLayerAdd = 0,
            ThisLayerEdit = 1,
            ThisLayerDelete = 1,
            ThisLayerSelect = 0,
            SubLayerAdd = 3,
            SubLayerEdit = 0,
            SubLayerDelete = 0,
            SubLayerSelect = 1,
        };
        var output = new OrganizeAdministratorSelectorOutput();

        OrganizeAdminSelectorHelpers.ApplyMergedAdminUserPermissionFlags(admin, user, output);

        Assert.Equal(-1, output.thisLayerAdd);
        Assert.Equal(1, output.thisLayerEdit);
        Assert.Equal(2, output.thisLayerDelete);
        Assert.Equal(0, output.thisLayerSelect);
        Assert.Equal(3, output.subLayerAdd);
        Assert.Equal(0, output.subLayerEdit);
        Assert.Equal(-1, output.subLayerDelete);
        Assert.Equal(2, output.subLayerSelect);
    }

    [Fact]
    public void StripNegativePermissionKeys_RemovesMinusOneValues()
    {
        var nodes = new List<Dictionary<string, object>>
        {
            new()
            {
                ["thisLayerAdd"] = -1,
                ["thisLayerEdit"] = 1,
                ["keep"] = "x",
            },
            new()
            {
                ["subLayerAdd"] = 0,
                ["subLayerEdit"] = -1,
            },
        };

        OrganizeAdminSelectorHelpers.StripNegativePermissionKeys(nodes);

        Assert.False(nodes[0].ContainsKey("thisLayerAdd"));
        Assert.Equal(1, nodes[0]["thisLayerEdit"]);
        Assert.Equal("x", nodes[0]["keep"]);
        Assert.True(nodes[1].ContainsKey("subLayerAdd"));
        Assert.False(nodes[1].ContainsKey("subLayerEdit"));
    }

    [Fact]
    public void StripNegativePermissionKeys_NoMinusOne_Unchanged()
    {
        var nodes = new List<Dictionary<string, object>>
        {
            new() { ["a"] = 0, ["b"] = 1 },
        };

        OrganizeAdminSelectorHelpers.StripNegativePermissionKeys(nodes);

        Assert.Equal(2, nodes[0].Count);
    }

    [Fact]
    public void ResolveExpandedFlag_InheritAsThree_VsSaveModeOne()
    {
        Assert.Equal(3, OrganizeAdminSelectorHelpers.ResolveExpandedFlag(false, 0, 1, inheritAs: 3));
        Assert.Equal(1, OrganizeAdminSelectorHelpers.ResolveExpandedFlag(false, 0, 1, inheritAs: 1));
        Assert.Equal(0, OrganizeAdminSelectorHelpers.ResolveExpandedFlag(false, 0, 0, inheritAs: 3));
        Assert.Equal(1, OrganizeAdminSelectorHelpers.ResolveExpandedFlag(true, 1, 1, inheritAs: 3));
        Assert.Equal(3, OrganizeAdminSelectorHelpers.ResolveExpandedFlag(true, 0, 1, inheritAs: 3));
    }

    [Fact]
    public void RepairOrgSelectorTreeGaps_ReparentsMissingParentAndStripsPrefix()
    {
        var result = new List<OrganizeAdministratorSelectorOutput>
        {
            new()
            {
                id = "root",
                organizeId = "root",
                parentId = "-1",
                fullName = "公司",
                organizeIdTree = "root",
            },
            new()
            {
                id = "leaf",
                organizeId = "leaf",
                parentId = "missing",
                fullName = "公司/部门/叶子",
                organizeIdTree = "root,mid,leaf",
            },
        };

        OrganizeAdminSelectorHelpers.RepairOrgSelectorTreeGaps(result);

        var leaf = result.Single(x => x.id == "leaf");
        Assert.Equal("root", leaf.parentId);
        Assert.Equal("部门/叶子", leaf.fullName);
    }

    [Fact]
    public void MapOrganizeToSelectorNode_UsesFullNameOrDescription()
    {
        var org = new OrganizeEntity
        {
            Id = "o1",
            ParentId = "-1",
            FullName = "显示名",
            Description = "描述名",
            Category = "company",
            OrganizeIdTree = "o1",
        };

        var byName = OrganizeAdminSelectorHelpers.MapOrganizeToSelectorNode(org, useDescriptionAsFullName: false);
        var byDesc = OrganizeAdminSelectorHelpers.MapOrganizeToSelectorNode(org, useDescriptionAsFullName: true);

        Assert.Equal("显示名", byName.fullName);
        Assert.Equal("描述名", byDesc.fullName);
        Assert.Equal("icon-ym icon-ym-tree-organization3", byName.icon);
        Assert.Equal("o1", byDesc.organizeId);
    }
}
