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

    // ==================== D1.4 S1 特征用例（规格 §2.4 不变量 I1-I6 缺口补齐） ====================
    // 金标准纪律：本批用例在当前实现上全绿后才允许拆分；拆分后逐条等价。

    [Fact]
    public void D1_I1_TripleDuplicate_AppendsExactlyNMinusOneMessagesPerRow()
    {
        // I1（Q8 保真）：3 重复 → 每行追加精确 N-1=2 条同文错误（for i=1 循环，非去重语义）
        var fields = new List<FieldsModel> { Field("code", "编码", unique: true) };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new() { ["code"] = "A" },
                new() { ["code"] = "A" },
                new() { ["code"] = "A" },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string>(), dataType: "1");

        foreach (var row in rows)
            Assert.Equal(",编码: 值不能重复,编码: 值不能重复", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void D1_I5_ContainsValuePrefilter_ValueOnlyElsewhere_NoDuplicate()
    {
        // I5（Q9 实测修正保真）：ContainsValue 仅为候选粗筛，重复判定以内层键值相等为准 —
        // 他行无 code 键但含同值（note=A）→ 不入重复集，不报错（锁定粗筛非错误来源）
        var fields = new List<FieldsModel> { Field("code", "编码", unique: true) };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new() { ["code"] = "A" },
                new() { ["note"] = "A" },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string>(), dataType: "1");

        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
        Assert.Equal(string.Empty, rows[1][ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void D1_I6_NullValues_SkipDuplicateCheck()
    {
        // I6：双空值守卫 — null 值不参与重复判定（两行同 null 不报错）
        var fields = new List<FieldsModel> { Field("code", "编码", unique: true) };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new() { ["code"] = null! },
                new() { ["code"] = null! },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string>(), dataType: "1");

        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
        Assert.Equal(string.Empty, rows[1][ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void D1_NoUniqueFields_RowsUntouched()
    {
        // 无唯一字段配置 → 整体早退，错误串保持 Seed 形态（调用方依赖零触碰）
        var fields = new List<FieldsModel> { Field("code", "编码") };
        var rows = ImportFirstVerifyHelpers.SeedWithEmptyErrors(
            new List<Dictionary<string, object>>
            {
                new() { ["code"] = "A" },
                new() { ["code"] = "A" },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string>(), dataType: "1");

        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
        Assert.Equal(string.Empty, rows[1][ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void D1_I4_ChildNonType2_NullChildValue_Skipped()
    {
        // I4+I6 子表：错误模式下 null 子值不参与唯一判定（与主字段守卫对称）
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
                        new() { ["f_code"] = null! },
                        new() { ["f_code"] = null! },
                    },
                },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string> { "tableField111" }, dataType: "1");

        Assert.Equal(string.Empty, rows[0][ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void D1_I3_DataType2_NoDuplicates_ReplacesListWithSameContent()
    {
        // I3："2" 模式无重复时仍整体替换子表列表（内容不变，替换行为保真锁定）
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
                        new() { ["f_code"] = "2", ["f_name"] = "b" },
                    },
                },
            });

        ImportFirstVerifyHelpers.ValidateBatchUnique(
            rows, fields, childTableVModels: new List<string> { "tableField111" }, dataType: "2");

        var child = Assert.IsType<List<Dictionary<string, object>>>(rows[0]["tableField111"]);
        Assert.Equal(2, child.Count);
        Assert.Equal("a", child[0]["f_name"]);
        Assert.Equal("b", child[1]["f_name"]);
    }
}
