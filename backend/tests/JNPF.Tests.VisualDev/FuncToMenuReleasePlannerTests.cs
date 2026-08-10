using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Release;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class FuncToMenuReleasePlannerTests
{
    [Fact]
    public void BuildRelease_Pc_KeepsExistingWebAndAddsSystemRoot()
    {
        var existing = new List<ModuleEntity>
        {
            new() { Id = "m1", SystemId = "sysA", ParentId = "p1", Category = "Web", PropertyJson = "{}" },
            new() { Id = "m2", SystemId = "sysA", ParentId = "p2", Category = "App", PropertyJson = "{}" },
        };
        var all = existing.ToList();
        var release = FuncToMenuReleasePlanner.BuildReleaseTargets(
            publishPc: true,
            publishApp: false,
            pcModuleParentIds: new[] { "sysA" },
            appModuleParentIds: Array.Empty<string>(),
            sysIdList: new[] { "sysA" },
            modulesLinkedToFeature: existing,
            allModules: all);

        Assert.True(release.ContainsKey("Web"));
        Assert.False(release.ContainsKey("App"));
        Assert.Equal(2, release["Web"].Count); // existing Web + new sys root
        Assert.Contains(release["Web"], d => d.ContainsKey("sysA") && d["sysA"] == "p1");
        Assert.Contains(release["Web"], d => d.ContainsKey("sysA") && d["sysA"] == "-1");
    }

    [Fact]
    public void BuildRelease_App_ParentIsModule_UsesModuleSystemAndId()
    {
        var parentModule = new ModuleEntity
        {
            Id = "folder-1",
            SystemId = "sysB",
            ParentId = "-1",
            Category = "App",
            PropertyJson = "{}",
        };
        var release = FuncToMenuReleasePlanner.BuildReleaseTargets(
            publishPc: false,
            publishApp: true,
            pcModuleParentIds: Array.Empty<string>(),
            appModuleParentIds: new[] { "folder-1" },
            sysIdList: new[] { "sysB" },
            modulesLinkedToFeature: Array.Empty<ModuleEntity>(),
            allModules: new[] { parentModule });

        Assert.Single(release["App"]);
        Assert.Equal("folder-1", release["App"][0]["sysB"]);
    }

    [Fact]
    public void BuildRelease_UnknownParent_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            FuncToMenuReleasePlanner.BuildReleaseTargets(
                publishPc: true,
                publishApp: false,
                pcModuleParentIds: new[] { "missing-id" },
                appModuleParentIds: Array.Empty<string>(),
                sysIdList: new[] { "sysA" },
                modulesLinkedToFeature: Array.Empty<ModuleEntity>(),
                allModules: Array.Empty<ModuleEntity>()));
    }
}
