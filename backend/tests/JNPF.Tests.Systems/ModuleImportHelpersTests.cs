using JNPF.Systems.Common.ModuleImport;
using Xunit;

namespace JNPF.Tests.Systems;

/// <summary>
/// Characterization tests for ModuleService.ImportData pure helpers.
/// </summary>
public class ModuleImportHelpersTests
{
    [Fact]
    public void FormatImportCopySuffix_AppendsCopyAndRandom()
    {
        Assert.Equal("菜单.副本a1b2c", ModuleImportHelpers.FormatImportCopySuffix("菜单", "a1b2c"));
    }

    [Fact]
    public void RecordImportDuplicateKey_FirstThenAccumulateWithDunhao()
    {
        var dic = new Dictionary<string, string>();
        ModuleImportHelpers.RecordImportDuplicateKey(dic, "编码", "btn_add");
        ModuleImportHelpers.RecordImportDuplicateKey(dic, "编码", "btn_edit");
        ModuleImportHelpers.RecordImportDuplicateKey(dic, "名称", "新增");

        Assert.Equal("btn_add、btn_edit", dic["编码"]);
        Assert.Equal("新增", dic["名称"]);
        Assert.Equal(2, dic.Count);
    }

    [Fact]
    public void FormatSubTableDuplicateMessage_MatchesLegacyTemplate()
    {
        var dic = new Dictionary<string, string>
        {
            ["编码"] = "x",
            ["名称"] = "y",
        };

        Assert.Equal(
            "buttonEntityList：编码(x)、名称(y)重复",
            ModuleImportHelpers.FormatSubTableDuplicateMessage("buttonEntityList", dic));
    }

    [Fact]
    public void FormatSubTableDuplicateMessage_PreservesInsertionOrder()
    {
        var dic = new Dictionary<string, string>();
        ModuleImportHelpers.RecordImportDuplicateKey(dic, "ID", "1");
        ModuleImportHelpers.RecordImportDuplicateKey(dic, "名称", "n");

        Assert.Equal(
            "columnEntityList：ID(1)、名称(n)重复",
            ModuleImportHelpers.FormatSubTableDuplicateMessage("columnEntityList", dic));
    }

    [Fact]
    public void ApplyAppendRename_RenamesFullNameAndAppendsEncodeSuffix()
    {
        var (fullName, enCode) = ModuleImportHelpers.ApplyAppendRename("按钮", "btn", "Zz9q1");
        Assert.Equal("按钮.副本Zz9q1", fullName);
        Assert.Equal("btnZz9q1", enCode);
    }

    [Fact]
    public void RewriteConditionJsonIds_ReplacesAllMappedIds()
    {
        var map = new Dictionary<string, string>
        {
            ["old-a"] = "new-a",
            ["old-b"] = "new-b",
        };
        var json = """{"field":"old-a","and":"old-b","keep":"old-c"}""";

        var result = ModuleImportHelpers.RewriteConditionJsonIds(json, map);

        Assert.Equal("""{"field":"new-a","and":"new-b","keep":"old-c"}""", result);
    }

    [Fact]
    public void RewriteConditionJsonIds_NullOrEmpty_ReturnsInput()
    {
        var map = new Dictionary<string, string> { ["a"] = "b" };
        Assert.Null(ModuleImportHelpers.RewriteConditionJsonIds(null, map));
        Assert.Equal(string.Empty, ModuleImportHelpers.RewriteConditionJsonIds(string.Empty, map));
        Assert.Equal("{\"x\":1}", ModuleImportHelpers.RewriteConditionJsonIds("{\"x\":1}", null));
        Assert.Equal("{\"x\":1}", ModuleImportHelpers.RewriteConditionJsonIds("{\"x\":1}", new Dictionary<string, string>()));
    }
}
