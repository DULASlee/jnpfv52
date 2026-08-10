using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: ImportFirstVerify in-memory required/unique (shared VisualDev + CodeGen).
/// </summary>
public class ImportFirstVerifyHelpersTests
{
    private static FieldsModel Field(string vModel, string label, bool required = false, bool unique = false, string jnpfKey = JnpfKeyConst.COMINPUT)
        => new()
        {
            __vModel__ = vModel,
            __config__ = new ConfigModel
            {
                label = label,
                required = required,
                unique = unique,
                jnpfKey = jnpfKey,
            },
        };

    [Fact]
    public void SeedWithEmptyErrors_AddsErrorsInfoKey()
    {
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>> { new() { ["name"] = "a" } });
        Assert.Single(rows);
        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
        Assert.Equal("a", rows[0]["name"]);
    }

    [Fact]
    public void ValidateRequired_MainFieldEmpty_AppendsLeadingCommaQuirk()
    {
        var fields = new List<FieldsModel> { Field("name", "名称", required: true) };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>> { new() { ["name"] = null } });

        ImportFirstVerifyHelpers.ValidateRequired(rows, fields, childTableVModels: new List<string>());

        // Legacy seeds empty errorsInfo then Append → leading comma
        Assert.Equal(",名称: 值不能为空", rows[0][ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void ValidateRequired_ChildTableEmpty_UsesTableDashKey()
    {
        var table = Field("tableField111", "明细", jnpfKey: JnpfKeyConst.TABLE);
        table.__config__.children = new List<FieldsModel>
        {
            Field("tableField111-f_name", "子名", required: true),
        };
        var fields = new List<FieldsModel>
        {
            table,
            Field("tableField111-f_name", "子名", required: true),
        };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new()
                {
                    ["tableField111"] = new List<Dictionary<string, object>>
                    {
                        new() { ["f_name"] = null },
                    },
                },
            });

        ImportFirstVerifyHelpers.ValidateRequired(
            rows, fields, childTableVModels: new List<string> { "tableField111" });

        Assert.Contains("子名: 值不能为空", rows[0][ImportAssembleErrors.ErrorKey]?.ToString());
    }

    [Fact]
    public void ValidateBatchUnique_MainDuplicates_MarksFromSecond()
    {
        var fields = new List<FieldsModel> { Field("code", "编码", unique: true) };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new() { ["code"] = "A" },
                new() { ["code"] = "A" },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string>(), dataType: "1");

        // Legacy marks the current row when vlist.Count>1 (both duplicate rows get the message)
        Assert.Contains("编码: 值不能重复", rows[0][ImportAssembleErrors.ErrorKey]?.ToString());
        Assert.Contains("编码: 值不能重复", rows[1][ImportAssembleErrors.ErrorKey]?.ToString());
    }

    [Fact]
    public void ValidateBatchUnique_ChildDataType2_KeepsLastOfDupes()
    {
        var fields = new List<FieldsModel>
        {
            Field("tableField111", "明细", jnpfKey: JnpfKeyConst.TABLE),
            Field("tableField111-f_code", "子编码", unique: true),
        };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new()
                {
                    ["tableField111"] = new List<Dictionary<string, object>>
                    {
                        new() { ["f_code"] = "1", ["f_name"] = "a" },
                        new() { ["f_code"] = "1", ["f_name"] = "b" },
                        new() { ["f_code"] = "2", ["f_name"] = "c" },
                    },
                },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string> { "tableField111" }, dataType: "2");

        var child = Assert.IsType<List<Dictionary<string, object>>>(rows[0]["tableField111"]);
        Assert.Equal(2, child.Count);
        Assert.Equal("b", child[0]["f_name"]);
        Assert.Equal("c", child[1]["f_name"]);
    }

    [Fact]
    public void ValidateBatchUnique_ChildNonType2_DedupesErrorText()
    {
        var fields = new List<FieldsModel>
        {
            Field("tableField111", "明细", jnpfKey: JnpfKeyConst.TABLE),
            Field("tableField111-f_code", "子编码", unique: true),
        };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new()
                {
                    ["tableField111"] = new List<Dictionary<string, object>>
                    {
                        new() { ["f_code"] = "1" },
                        new() { ["f_code"] = "1" },
                    },
                },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string> { "tableField111" }, dataType: "1");

        var err = rows[0][ImportAssembleErrors.ErrorKey]?.ToString();
        Assert.Contains("子编码: 值不能重复", err);
        // Deduped: one message even though loop may hit duplicates multiple times
        Assert.Equal(1, err!.Split("子编码: 值不能重复", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void SeedAndRequired_PreservesChildListType_NotJsonElement()
    {
        var table = Field("tableField111", "明细", jnpfKey: JnpfKeyConst.TABLE);
        table.__config__.children = new List<FieldsModel>
        {
            Field("tableField111-f_name", "子名", required: true),
        };
        var fields = new List<FieldsModel>
        {
            table,
            Field("tableField111-f_name", "子名", required: true),
        };
        var childRows = new List<Dictionary<string, object>> { new() { ["f_name"] = "ok" } };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>> { new() { ["tableField111"] = childRows } });

        ImportFirstVerifyHelpers.ValidateRequired(
            rows, fields, childTableVModels: new List<string> { "tableField111" });

        Assert.IsType<List<Dictionary<string, object>>>(rows[0]["tableField111"]);
        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
    }
}
