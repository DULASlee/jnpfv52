using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Transfer;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: SaveDataToDataByFId in-memory map / child-table transfer.
/// </summary>
public class FlowFormDataMapperTests
{
    private static FieldsModel Field(string vModel, string jnpfKey = JnpfKeyConst.COMINPUT)
        => new()
        {
            __vModel__ = vModel,
            __config__ = new ConfigModel { jnpfKey = jnpfKey },
        };

    [Theory]
    [InlineData("leaveApply", "-")]
    [InlineData("salesOrder", "-")]
    [InlineData("crmOrder", "-")]
    [InlineData("normalForm", "tablefield")]
    public void ResolveChildTableSplitKey_SpecialForms(string enCode, string expected)
        => Assert.Equal(expected, FlowFormDataMapper.ResolveChildTableSplitKey(enCode, "other"));

    [Fact]
    public void CoerceSystemFormFieldsToComInput_SkipsModifyAndTable()
    {
        var fields = new List<FieldsModel>
        {
            Field("a", JnpfKeyConst.NUMINPUT),
            Field("m", JnpfKeyConst.MODIFYTIME),
            Field("t", JnpfKeyConst.TABLE),
        };
        FlowFormDataMapper.CoerceSystemFormFieldsToComInput(fields);
        Assert.Equal(JnpfKeyConst.COMINPUT, fields[0].__config__.jnpfKey);
        Assert.Equal(JnpfKeyConst.MODIFYTIME, fields[1].__config__.jnpfKey);
        Assert.Equal(JnpfKeyConst.TABLE, fields[2].__config__.jnpfKey);
    }

    [Fact]
    public void ApplyMapRules_MissingModel_SetsEmptyString()
    {
        var form = new Dictionary<string, object> { ["a"] = "keep" };
        var map = new List<Dictionary<string, string>> { new() { ["a"] = "b" } };
        var oldIdx = new Dictionary<string, FieldsModel>(); // missing
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("b") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal(string.Empty, form["a"]);
    }

    [Fact]
    public void ApplyMapRules_ChildToChild_CopiesByRowIndex()
    {
        var oldField = Field("tableField111-f_name");
        var newField = Field("tableField222-f_title");
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "r1" },
                new() { ["f_name"] = "r2" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "tableField222-f_title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField222"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(2, rows.Count);
        Assert.Equal("r1", rows[0]["f_title"]);
        Assert.Equal("r2", rows[1]["f_title"]);
    }

    [Fact]
    public void ApplyMapRules_MainToChild_AddsOrUpdatesFirstRow()
    {
        var oldField = Field("title");
        var newField = Field("tableField111-f_name");
        var form = new Dictionary<string, object> { ["title"] = "hello" };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["title"] = "tableField111-f_name" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Single(rows);
        Assert.Equal("hello", rows[0]["f_name"]);
    }

    [Fact]
    public void ApplyMapRules_ChildToMain_TakesFirstRowValue()
    {
        var oldField = Field("tableField111-f_name");
        var newField = Field("title");
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "first" },
                new() { ["f_name"] = "second" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("first", form["tableField111-f_name"]);
    }

    [Fact]
    public void ApplyMapRules_IncompatibleMain_NullsOldValue()
    {
        var oldField = Field("qty", JnpfKeyConst.NUMINPUT);
        var newField = Field("day", JnpfKeyConst.DATE);
        var form = new Dictionary<string, object> { ["qty"] = 3 };
        var map = new List<Dictionary<string, string>> { new() { ["qty"] = "day" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Null(form["qty"]);
    }

    [Fact]
    public void BuildResult_RemapsKeysAndIncludesChildTableBucket()
    {
        var form = new Dictionary<string, object>
        {
            ["oldName"] = "v",
            ["x"] = "ignored",
            ["tableField111"] = new List<Dictionary<string, object>> { new() { ["f_a"] = 1 } },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["oldName"] = "newName" },
            new() { ["x"] = "tableField111-f_a" },
        };

        var res = FlowFormDataMapper.BuildResult(form, map, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("v", res["newName"]);
        Assert.True(res.ContainsKey("tableField111"));
    }

    [Fact]
    public void ApplyPrevNodeFormId_WritesIdOntoMappedKey()
    {
        var form = new Dictionary<string, object> { ["id"] = "ID-1" };
        var res = new Dictionary<string, object>();
        var map = new List<Dictionary<string, string>>
        {
            new() { ["@prevNodeFormId"] = "prevId" },
        };

        FlowFormDataMapper.ApplyPrevNodeFormId(res, form, map);
        Assert.Equal("ID-1", res["prevId"]);
    }

    [Fact]
    public void ApplyPrevNodeFormId_TableFieldKey_WritesEachChildRow()
    {
        var form = new Dictionary<string, object> { ["id"] = "ID-9" };
        var res = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_ref"] = "old" },
                new() { ["f_ref"] = "old2" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["@prevNodeFormId"] = "tableField111-f_ref" },
        };

        FlowFormDataMapper.ApplyPrevNodeFormId(res, form, map);
        var rows = res["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal("ID-9", rows[0]["f_ref"]);
        Assert.Equal("ID-9", rows[1]["f_ref"]);
        Assert.Equal("ID-9", res["tableField111-f_ref"]);
    }

    [Fact]
    public void ApplyMapRules_SpecialFormSplitKey_SkipsIncompatibleNulling()
    {
        var oldField = Field("qty", JnpfKeyConst.NUMINPUT);
        var newField = Field("day", JnpfKeyConst.DATE);
        var form = new Dictionary<string, object> { ["qty"] = 3 };
        var map = new List<Dictionary<string, string>> { new() { ["qty"] = "day" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(
            form, map, oldIdx, newIdx, FlowFormDataMapper.SpecialFormChildTableSplitKey);
        Assert.Equal(3, form["qty"]);
    }

    // ==================== D1.3 S1 特征用例（规格 §2.3 不变量 I1-I6 缺口补齐） ====================
    // 金标准纪律：本批用例在当前实现上全绿后才允许拆分；拆分后逐条等价。

    [Fact]
    public void D1_I1_ModifyGuards_SetEmptyAndContinue()
    {
        // I1：旧控件 MODIFYTIME/MODIFYUSER → 置空串 continue（即使新模型存在且兼容）
        var form = new Dictionary<string, object> { ["mt"] = "v1", ["mu"] = "v2" };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["mt"] = "t1" },
            new() { ["mu"] = "t2" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[]
        {
            Field("mt", JnpfKeyConst.MODIFYTIME),
            Field("mu", JnpfKeyConst.MODIFYUSER),
        });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("t1"), Field("t2") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal(string.Empty, form["mt"]);
        Assert.Equal(string.Empty, form["mu"]);
    }

    [Fact]
    public void D1_I1_TableEitherSide_SkipsWithoutNeutralizing()
    {
        // I1：任一侧 TABLE → 跳过且不置空（与缺失模型的置空语义对照）
        var form = new Dictionary<string, object> { ["a"] = "v1", ["b"] = "v2" };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["a"] = "x" },
            new() { ["b"] = "y" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[]
        {
            Field("a", JnpfKeyConst.TABLE),
            Field("b"),
        });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[]
        {
            Field("x"),
            Field("y", JnpfKeyConst.TABLE),
        });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("v1", form["a"]);
        Assert.Equal("v2", form["b"]);
    }

    [Fact]
    public void D1_I1_MissingNewModel_SetsEmptyString()
    {
        // I1：新模型缺失 → 置空串（与旧模型缺失对称）
        var form = new Dictionary<string, object> { ["a"] = "keep" };
        var map = new List<Dictionary<string, string>> { new() { ["a"] = "b" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("a") });
        var newIdx = new Dictionary<string, FieldsModel>();

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal(string.Empty, form["a"]);
    }

    [Fact]
    public void D1_I2_ChildToChild_AppendsRowsWhenNewTableShorter()
    {
        // I2 越界补行：新表已有 1 行（他字段）→ 行 0 就地写入，行 1/2 补新行
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "r1" },
                new() { ["f_name"] = "r2" },
                new() { ["f_name"] = "r3" },
            },
            ["tableField222"] = new List<Dictionary<string, object>>
            {
                new() { ["f_keep"] = "k" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "tableField222-f_title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_name") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField222-f_title") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField222"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(3, rows.Count);
        Assert.Equal("r1", rows[0]["f_title"]);
        Assert.Equal("k", rows[0]["f_keep"]);
        Assert.Equal("r2", rows[1]["f_title"]);
        Assert.Equal("r3", rows[2]["f_title"]);
    }

    [Fact]
    public void D1_I2_ChildToChild_SkipsRowsMissingField()
    {
        // I2：旧表行缺失目标字段 → 该行不搬运（新表不补行）
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "r1" },
                new() { ["f_other"] = "skip" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "tableField222-f_title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_name") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField222-f_title") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField222"].ToObject<List<Dictionary<string, object>>>();
        Assert.Single(rows);
        Assert.Equal("r1", rows[0]["f_title"]);
    }

    [Fact]
    public void D1_I2_ChildToChild_EmptyChildField_SkipsRuleEntirely()
    {
        // I2：vModel 拆分后字段段为空 → continue（新表键不被创建，formData 零触碰）
        var form = new Dictionary<string, object> { ["x"] = "v" };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-"] = "tableField222-f_t" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField222-f_t") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.False(form.ContainsKey("tableField222"));
        Assert.Equal("v", form["x"]);
    }

    [Fact]
    public void D1_I2_ChildToChild_Incompatible_NoTransferAndNoNull()
    {
        // I2：CanTransfer 拒绝 → 双端子表分支整体不搬运且不置空（与主-主回退置 null 语义对照）
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>> { new() { ["f_q"] = 1 } },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_q"] = "tableField222-f_d" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_q", JnpfKeyConst.NUMINPUT) });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField222-f_d", JnpfKeyConst.DATE) });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.False(form.ContainsKey("tableField222"));
        var rows = form["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.NotNull(rows[0]["f_q"]);
    }

    [Fact]
    public void D1_I3_ChildToMain_EmptyChildTable_NoWrite()
    {
        // I3：旧子表空 → 不写入 formData[旧vModel]
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>(),
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_name") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("title") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.False(form.ContainsKey("tableField111-f_name"));
    }

    [Fact]
    public void D1_I4_MainToChild_UpdatesExistingFirstRow()
    {
        // I4：新子表已有首行（无目标键）→ 首行新增键（与空表补行分支对照）
        var form = new Dictionary<string, object>
        {
            ["title"] = "hello",
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_other"] = "keep" },
                new() { ["f_other"] = "row2" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["title"] = "tableField111-f_name" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("title") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_name") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(2, rows.Count);
        Assert.Equal("hello", rows[0]["f_name"]);
        Assert.Equal("keep", rows[0]["f_other"]);
        Assert.False(rows[1].ContainsKey("f_name"));
    }

    [Fact]
    public void D1_I4_MainToChild_OverwritesExistingFirstRowKey()
    {
        // I4：首行已含目标键 → 覆盖（与新增键分支对照）
        var form = new Dictionary<string, object>
        {
            ["title"] = "new",
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "old" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["title"] = "tableField111-f_name" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("title") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("tableField111-f_name") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal("new", rows[0]["f_name"]);
    }

    [Fact]
    public void D1_I5_CompatibleMainToMain_KeepsValue()
    {
        // I5 对照：CanTransfer 通过 → 主-主分支不置空（仅失败才置 null）
        var form = new Dictionary<string, object> { ["a"] = "v" };
        var map = new List<Dictionary<string, string>> { new() { ["a"] = "b" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("a") });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("b") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("v", form["a"]);
    }
}
