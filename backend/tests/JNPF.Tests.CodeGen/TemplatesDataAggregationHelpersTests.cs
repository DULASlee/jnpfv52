using JNPF.CodeGen.Helpers;
using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.VisualDev.Entitys.Enum;
using Xunit;

namespace JNPF.Tests.CodeGen;

/// <summary>
/// Characterization: TemplatesDataAggregation pure shaping helpers.
/// </summary>
public class TemplatesDataAggregationHelpersTests
{
    [Theory]
    [InlineData(true, true, 2, true)]
    [InlineData(true, false, 2, true)]
    [InlineData(false, true, 2, true)]
    [InlineData(false, false, 2, false)]
    [InlineData(true, true, 1, false)]
    [InlineData(false, true, 1, false)]
    public void ResolveUseDataPermission_MatchesLegacyBranches(bool pc, bool app, int webType, bool expected)
        => Assert.Equal(expected, TemplatesDataAggregationHelpers.ResolveUseDataPermission(pc, app, webType));

    [Theory]
    [InlineData(4, 2, true)]
    [InlineData(5, 2, true)]
    [InlineData(4, 1, false)]
    [InlineData(5, 1, false)]
    [InlineData(3, 2, false)]
    public void ShouldApplyUnifiedFormControls_TypeAndWebTypeGate(int type, int webType, bool expected)
        => Assert.Equal(expected, TemplatesDataAggregationHelpers.ShouldApplyUnifiedFormControls(type, webType));

    [Fact]
    public void ApplyColumnDataAggregationPatches_PureFormInlineEditor_ForcesType1()
    {
        var json = """{"type":4,"complexHeaderList":[{"a":1}]}""";
        var result = TemplatesDataAggregationHelpers.ApplyColumnDataAggregationPatches(json, webType: 1, pcColumnType: 4);
        var dict = result.ToObject<Dictionary<string, object>>();
        Assert.Equal(1, Convert.ToInt32(dict["type"]));
        Assert.True(dict.ContainsKey("complexHeaderList"));
    }

    [Fact]
    public void ApplyColumnDataAggregationPatches_GroupOrTree_ClearsComplexHeaders()
    {
        var json = """{"type":3,"complexHeaderList":[{"a":1}]}""";
        var result = TemplatesDataAggregationHelpers.ApplyColumnDataAggregationPatches(json, webType: 2, pcColumnType: 3);
        var dict = result.ToObject<Dictionary<string, object>>();
        var headers = dict["complexHeaderList"].ToObject<List<object>>();
        Assert.Empty(headers);
    }

    [Fact]
    public void JudgeGenerationModel_PrimaryTable_WhenSingleRelation()
    {
        var tables = new List<DbTableRelationModel> { Main("t_main") };
        var controls = new List<FieldsModel> { Field("input1", "input") };
        Assert.Equal(GeneratePatterns.PrimaryTable, TemplatesDataAggregationHelpers.JudgeGenerationModel(tables, controls));
    }

    [Fact]
    public void JudgeGenerationModel_MainBelt_WhenTableControlAndNoJnpfAux()
    {
        var tables = new List<DbTableRelationModel> { Main("t_main"), Child("t_child") };
        var controls = new List<FieldsModel> { TableControl("t_child", "tableField101") };
        Assert.Equal(GeneratePatterns.MainBelt, TemplatesDataAggregationHelpers.JudgeGenerationModel(tables, controls));
    }

    [Fact]
    public void JudgeGenerationModel_MainBeltVice_WhenJnpfAuxWithoutTableControl()
    {
        var tables = new List<DbTableRelationModel> { Main("t_main"), Child("t_aux") };
        var controls = new List<FieldsModel> { Field("t_aux_jnpf_name", "input") };
        Assert.Equal(GeneratePatterns.MainBeltVice, TemplatesDataAggregationHelpers.JudgeGenerationModel(tables, controls));
    }

    [Fact]
    public void JudgeGenerationModel_PrimarySecondary_WhenBothAuxAndTable()
    {
        var tables = new List<DbTableRelationModel> { Main("t_main"), Child("t_aux"), Child("t_child") };
        var controls = new List<FieldsModel>
        {
            Field("t_aux_jnpf_name", "input"),
            TableControl("t_child", "tableField101"),
        };
        Assert.Equal(GeneratePatterns.PrimarySecondary, TemplatesDataAggregationHelpers.JudgeGenerationModel(tables, controls));
    }

    [Fact]
    public void JudgeGenerationModel_MainBeltUpgradesWhenTableControlCountShort()
    {
        // two child relations but only one TABLE control → force PrimarySecondary
        var tables = new List<DbTableRelationModel> { Main("t_main"), Child("t_child"), Child("t_extra") };
        var controls = new List<FieldsModel> { TableControl("t_child", "tableField101") };
        Assert.Equal(GeneratePatterns.PrimarySecondary, TemplatesDataAggregationHelpers.JudgeGenerationModel(tables, controls));
    }

    [Fact]
    public void SplitSubAndSecondaryTables_ClassifiesByTableControl()
    {
        var relations = new List<DbTableRelationModel> { Child("t_child"), Child("t_aux") };
        var controls = new List<FieldsModel> { TableControl("t_child", "tableField101") };

        var (sub, secondary) = TemplatesDataAggregationHelpers.SplitSubAndSecondaryTables(relations, controls);

        Assert.Single(sub);
        Assert.Equal("t_child", sub[0].table);
        Assert.Single(secondary);
        Assert.Equal("t_aux", secondary[0].table);
    }

    [Fact]
    public void BuildChildTableRelation_ShapesCoreFlags()
    {
        var item = Child("t_child");
        item.className = "Child";
        item.relationTable = "t_main";
        item.relationField = "f_id";
        item.tableName = "子表";

        var cfg = new CodeGenConfigModel
        {
            BusName = "子表控件",
            IsSearchMultiple = true,
            TableField = new List<TableColumnConfigModel>
            {
                new() { PrimaryKey = true, ColumnName = "Id", OriginalColumnName = "f_id", jnpfKey = "input" },
                new() { ForeignKeyField = true, ColumnName = "MainId", OriginalColumnName = "f_main_id", jnpfKey = "input" },
                new() { jnpfKey = "input", QueryWhether = true, IsShow = true, IsUnique = true, IsConversion = true, IsDetailConversion = true, IsImportField = true, IsControlParsing = true },
            },
        };

        var model = TemplatesDataAggregationHelpers.BuildChildTableRelation(item, cfg, "tableField101", 2);

        Assert.Equal("Child", model.ClassName);
        Assert.Equal("t_child", model.OriginalTableName);
        Assert.Equal("Id", model.PrimaryKey);
        Assert.Equal("MainId", model.TableField);
        Assert.Equal("Id", model.RelationField);
        Assert.Equal("tableField101", model.ControlModel);
        Assert.Equal(2, model.TableNo);
        Assert.Equal(1, model.ChilderColumnConfigListCount);
        Assert.True(model.IsQueryWhether);
        Assert.True(model.IsShowField);
        Assert.True(model.IsUnique);
        Assert.True(model.IsConversion);
        Assert.True(model.IsDetailConversion);
        Assert.True(model.IsImportData);
        Assert.True(model.IsSearchMultiple);
        Assert.True(model.IsControlParsing);
    }

    [Fact]
    public void BuildAuxiliaryTableRelation_ShapesFieldCountAndSystemFlags()
    {
        var item = Child("t_aux");
        item.relationTable = "t_main";
        item.relationField = "f_id";
        item.tableName = "副表";

        var cfg = new CodeGenConfigModel
        {
            ClassName = "Aux",
            IsSearchMultiple = false,
            TableField = new List<TableColumnConfigModel>
            {
                new() { PrimaryKey = true, ColumnName = "Id", OriginalColumnName = "f_id" },
                new() { ForeignKeyField = true, ColumnName = "MainId", OriginalColumnName = "f_main_id" },
                new() { jnpfKey = "input", IsSystemControl = true, IsUpdate = true, IsConversion = true },
            },
        };

        var model = TemplatesDataAggregationHelpers.BuildAuxiliaryTableRelation(item, cfg, 3, fieldCount: 4);

        Assert.Equal("Aux", model.ClassName);
        Assert.Equal(3, model.TableNo);
        Assert.Equal(4, model.FieldCount);
        Assert.True(model.IsSystemControl);
        Assert.True(model.IsUpdate);
        Assert.True(model.IsConversion);
        Assert.False(model.IsSearchMultiple);
    }

    [Theory]
    [InlineData(GeneratePatterns.MainBelt, true)]
    [InlineData(GeneratePatterns.PrimarySecondary, true)]
    [InlineData(GeneratePatterns.MainBeltVice, false)]
    [InlineData(GeneratePatterns.PrimaryTable, false)]
    public void NeedsChildTablePrimaryKeyInjection_OnlyBeltAndPrimarySecondary(GeneratePatterns model, bool expected)
        => Assert.Equal(expected, TemplatesDataAggregationHelpers.NeedsChildTablePrimaryKeyInjection(model));

    [Fact]
    public void ApplyChildTablePrimaryKeys_WritesLowerPascalPrimary()
    {
        var controls = new List<FieldsModel> { TableControl("t_child", "tableField101") };
        var map = new Dictionary<string, string> { ["t_child"] = "f_id" };

        TemplatesDataAggregationHelpers.ApplyChildTablePrimaryKeys(controls, map);

        Assert.Equal("id", controls[0].TablePrimaryKey);
    }

    [Fact]
    public void ResolveMainBackendPaths_FlowFormInlineEditor_ReturnsNull()
    {
        // WebType=2 + TableType=4 + Type=3 → legacy empty break (leave prior lists)
        var paths = TemplatesDataAggregationHelpers.ResolveMainBackendPaths(
            webType: 2, type: 3, enableFlow: 0, tableType: 4,
            className: "Demo", fileName: "pack", isMapper: true, genModel: "1-SingleTable");

        Assert.Null(paths);
    }

    private static DbTableRelationModel Main(string table) => new()
    {
        typeId = "1",
        table = table,
        relationTable = string.Empty,
        className = "Main",
        tableName = "主表",
    };

    private static DbTableRelationModel Child(string table) => new()
    {
        typeId = "0",
        table = table,
        relationTable = "t_main",
        relationField = "f_id",
        className = "Child",
        tableName = "子表",
    };

    private static FieldsModel Field(string vModel, string jnpfKey) => new()
    {
        __vModel__ = vModel,
        __config__ = new ConfigModel { jnpfKey = jnpfKey },
    };

    private static FieldsModel TableControl(string tableName, string vModel) => new()
    {
        __vModel__ = vModel,
        __config__ = new ConfigModel
        {
            jnpfKey = JnpfKeyConst.TABLE,
            tableName = tableName,
            children = new List<FieldsModel>(),
        },
    };
}
