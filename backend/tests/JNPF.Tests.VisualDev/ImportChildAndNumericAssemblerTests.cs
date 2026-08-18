using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ImportChildAndNumericAssemblerTests
{
    [Fact]
    public void PrefixAndStrip_RoundTrip()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["name"] = "a", ["age"] = 1 },
        };
        var prefixed = ImportChildTableAssembler.PrefixRows("tablefield101", rows);
        Assert.Equal("a", prefixed[0]["tablefield101-name"]);
        var stripped = ImportChildTableAssembler.StripPrefix(prefixed, "tablefield101-");
        Assert.Equal("a", stripped[0]["name"]);
        Assert.Equal(1, stripped[0]["age"]);
    }

    [Fact]
    public void MergeChildErrors_BubblesAndRemovesErrorRow()
    {
        var parent = new Dictionary<string, object>();
        var children = new List<Dictionary<string, object>>
        {
            new() { ["ok"] = 1 },
            new() { [ImportAssembleErrors.ErrorKey] = "子: 值不正确" },
        };
        ImportChildTableAssembler.MergeChildErrors(parent, children);
        Assert.Equal("子: 值不正确", parent[ImportAssembleErrors.ErrorKey]);
        Assert.Single(children);
        Assert.False(children[0].ContainsKey(ImportAssembleErrors.ErrorKey));
    }

    [Fact]
    public void MapRate_VisualDev_RejectsHalfWhenNotAllowed()
    {
        var v = new FieldsModel
        {
            allowHalf = false,
            count = 5,
            __config__ = new ConfigModel { label = "评分", jnpfKey = JnpfKeyConst.RATE },
        };
        var row = new Dictionary<string, object>();
        ImportNumericFieldAssembler.MapRate(v, "f_rate", "1.5", row, ImportNumericSemantics.VisualDev);
        Assert.Equal("评分: 值不正确", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void MapRate_CodeGen_UsesMaxAndClearsEmpty()
    {
        var v = new FieldsModel
        {
            max = 5,
            __config__ = new ConfigModel { label = "评分", jnpfKey = JnpfKeyConst.RATE },
        };
        var over = new Dictionary<string, object>();
        ImportNumericFieldAssembler.MapRate(v, "f_rate", "9", over, ImportNumericSemantics.CodeGen);
        Assert.Contains("评分超过设置的最大值", over[ImportAssembleErrors.ErrorKey].ToString());

        var empty = new Dictionary<string, object> { ["f_rate"] = "x" };
        ImportNumericFieldAssembler.MapRate(v, "f_rate", null, empty, ImportNumericSemantics.CodeGen);
        Assert.Null(empty["f_rate"]);
    }

    [Fact]
    public void MapSlider_VisualDev_MinMax()
    {
        var v = new FieldsModel
        {
            min = 1,
            max = 10,
            __config__ = new ConfigModel { label = "滑块", jnpfKey = JnpfKeyConst.SLIDER },
        };
        var row = new Dictionary<string, object>();
        ImportNumericFieldAssembler.MapSlider(v, "f_s", "0", row, ImportNumericSemantics.VisualDev);
        Assert.Contains("滑块超过设置的最小值", row[ImportAssembleErrors.ErrorKey].ToString());
    }

    [Fact]
    public void MapNumberInput_CodeGen_FormatErrorMessage()
    {
        var v = new FieldsModel
        {
            __config__ = new ConfigModel { label = "数量", jnpfKey = JnpfKeyConst.NUMINPUT },
        };
        var row = new Dictionary<string, object>();
        ImportNumericFieldAssembler.MapNumberInput(v, "f_n", "abc", row, ImportNumericSemantics.CodeGen);
        Assert.Equal("数量: 数字输入格式错误", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public async Task MapAsync_Table_ClearWhenEmpty_CodeGenOnly()
    {
        var v = new FieldsModel
        {
            __vModel__ = "tablefield101",
            __config__ = new ConfigModel
            {
                jnpfKey = JnpfKeyConst.TABLE,
                children = new List<FieldsModel>(),
            },
        };
        Task<List<Dictionary<string, object>>> Noop(
            List<FieldsModel> _, List<Dictionary<string, object>> rows, Dictionary<string, List<Dictionary<string, string>>> __)
            => Task.FromResult(rows);

        var clear = new Dictionary<string, object> { ["tablefield101"] = "x" };
        await ImportChildTableAssembler.MapAsync(
            v, "tablefield101", null, new(), clear, Noop, clearWhenEmpty: true);
        Assert.Null(clear["tablefield101"]);

        var keep = new Dictionary<string, object> { ["tablefield101"] = "x" };
        await ImportChildTableAssembler.MapAsync(
            v, "tablefield101", null, new(), keep, Noop, clearWhenEmpty: false);
        Assert.Equal("x", keep["tablefield101"]);
    }
}
