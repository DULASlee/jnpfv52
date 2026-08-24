using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Runtime;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Inc-2（裁决 C / 计划 3.5 提拉）：RunSqlCompiler 特征捕获（剥离前基线）.
/// 覆盖纯计算路径（无 DB 分支）：GetQueryJson 主要控件分支 / GetSuperQueryJson 分组 /
/// GetInfoQuerySql 双分支 / GetListQuerySql 主表与主副表分支 / GetSuperQueryInput 委托.
/// 快照来源=当前实现实测捕获（禁止手写猜测）；Inc-3 剥离后同一快照断言等价.
/// 残余风险登记：DB 依赖分支（数据权限渲染/软删除/租户隔离/子表递归/USERSSELECT 多选）
/// 不在本特征集，由路由快照零 diff + 存量测试 + Inc-1 往返等价共同守护.
/// </summary>
public class RunSqlCompilerFeatureTests
{
    /// <summary>
    /// 快照基线目录（随仓审计）；文件不存在=首次捕获落盘并失败待登记，存在=严格断言.
    /// </summary>
    private static readonly string EvidenceDir = ResolveEvidenceDir();

    private static string ResolveEvidenceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".claude"))) dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("仓库根未找到（缺 .claude 目录）");
        return Path.Combine(dir.FullName, ".claude", "evidence", "runservice-engine-refactor", "feature-capture");
    }

    private static void AssertSnapshot(string caseName, string actual)
    {
        var path = Path.Combine(EvidenceDir, caseName + ".txt");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllText(path, actual);
            Assert.Fail($"特征快照已捕获待登记 [{caseName}]：{path}");
        }

        var expected = File.ReadAllText(path);
        Assert.True(string.Equals(expected, actual, StringComparison.Ordinal),
            $"特征快照不符 [{caseName}]（剥离前基线，禁止擅改；差异先归因）\n期望：{expected}\n实际：{actual}");
    }

    private static readonly RunSqlCompiler Compiler = new();

    /// <summary>
    /// 纯路径上下文：多租户关闭；其余成员在覆盖分支内不被触达.
    /// </summary>
    private static RunSqlCompileContext PureContext() => new()
    {
        Tenant = new TenantOptions { MultiTenancy = false },
    };

    private static FieldsModel Field(string vModel, string tableName, string jnpfKey) => new()
    {
        __vModel__ = vModel,
        __config__ = new ConfigModel { tableName = tableName, jnpfKey = jnpfKey },
    };

    private static IndexSearchFieldModel Search(string id, string jnpfKey, Action<IndexSearchFieldModel> setup = null)
    {
        var m = new IndexSearchFieldModel
        {
            id = id,
            __vModel__ = id,
            __config__ = new ConfigModel { jnpfKey = jnpfKey },
        };
        setup?.Invoke(m);
        return m;
    }

    private static TemplateParsingBase MainOnlyTemplate(List<IndexSearchFieldModel> searchList) => new()
    {
        WebType = 2,
        MainTableName = "T_MAIN",
        FormModel = new FormDataModel { logicalDelete = false },
        ColumnData = new ColumnDesignModel { columnList = new List<IndexGridFieldModel>(), searchList = searchList },
        AppColumnData = new ColumnDesignModel { columnList = new List<IndexGridFieldModel>(), searchList = new List<IndexSearchFieldModel>() },
        ChildTableFields = new Dictionary<string, string>(),
        AuxiliaryTableFieldsModelList = new List<FieldsModel>(),
        MainTableFieldsModelList = new List<FieldsModel>
        {
            Field("main_jnpf_f_name", "T_MAIN", JnpfKeyConst.COMINPUT),
            Field("main_jnpf_f_status", "T_MAIN", JnpfKeyConst.SELECT),
        },
        SingleFormData = new List<FieldsModel>(),
        AllTable = new List<TableModel>(),
    };

    [Fact]
    public void GetSuperQueryJson_TwoGroups_MixedTypes()
    {
        // 生产形态：ListSuperQueryInputRewriter 输出的 where/whereType/ConditionalType 均为数值（实测修正）
        var superQueryJson = """
            [
              {"where":0,"ConditionalType":0,"field":"T_MAIN.f_name","fieldValue":"alice"},
              {"whereType":1,"ConditionalType":1,"field":"T_MAIN.f_remark","fieldValue":"note"},
              {"where":1,"ConditionalType":2,"CSharpTypeName":"decimal","field":"T_MAIN.f_qty","fieldValue":"3"}
            ]
            """;
        var tInfo = MainOnlyTemplate(new List<IndexSearchFieldModel>());

        var result = Compiler.GetSuperQueryJson(superQueryJson, tInfo);

        AssertSnapshot("GetSuperQueryJson_TwoGroups", result.ToJsonStringOld());
    }

    [Fact]
    public void GetQueryJson_MainBranches_MixedKeys()
    {
        var searchList = new List<IndexSearchFieldModel>
        {
            Search("keyword", JnpfKeyConst.COMINPUT, m => { m.isKeyword = true; m.__config__.tableName = "T_MAIN"; }),
            Search("main_jnpf_f_date", JnpfKeyConst.DATE, m => m.format = "yyyy-MM-dd"),
            Search("main_jnpf_f_qty", JnpfKeyConst.NUMINPUT),
            Search("main_jnpf_f_status", JnpfKeyConst.SELECT, m => m.searchType = 1),
            Search("main_jnpf_f_tag", JnpfKeyConst.SELECT, m => m.multiple = true),
            Search("main_jnpf_f_check", JnpfKeyConst.CHECKBOX),
            Search("main_jnpf_f_user", JnpfKeyConst.USERSSELECT),
            Search("main_jnpf_f_tree", JnpfKeyConst.TREESELECT),
            Search("main_jnpf_f_casc", JnpfKeyConst.CASCADER),
            Search("main_jnpf_f_txt_like", JnpfKeyConst.COMINPUT),
            Search("main_jnpf_f_txt_eq", JnpfKeyConst.COMINPUT, m => m.searchType = 1),
        };
        var columnDesign = new ColumnDesignModel { searchList = searchList };
        var queryJson = """
            {
              "keyword": "alice",
              "main_jnpf_f_date": ["1735689600000", "1735776000000"],
              "main_jnpf_f_qty": ["1", "10"],
              "main_jnpf_f_status": "1",
              "main_jnpf_f_tag": ["a", "b"],
              "main_jnpf_f_check": ["x", "y"],
              "main_jnpf_f_user": "u1",
              "main_jnpf_f_tree": "t1",
              "main_jnpf_f_casc": ["c1", "c2"],
              "main_jnpf_f_txt_like": "hello",
              "main_jnpf_f_txt_eq": "world"
            }
            """;

        var result = Compiler.GetQueryJson(PureContext(), queryJson, columnDesign);

        AssertSnapshot("GetQueryJson_MainBranches", result.ToJsonStringOld());
    }

    [Fact]
    public void GetQueryJson_InteAssisFlag_On()
    {
        var columnDesign = new ColumnDesignModel { searchList = new List<IndexSearchFieldModel>() };

        var result = Compiler.GetQueryJson(PureContext(), string.Empty, columnDesign, 1);

        AssertSnapshot("GetQueryJson_InteAssisOn", result.ToJsonStringOld());
    }

    [Fact]
    public void GetInfoQuerySql_MainOnly()
    {
        var tInfo = MainOnlyTemplate(new List<IndexSearchFieldModel>());
        tInfo.MainTableFieldsModelList.Add(Field("main_jnpf_f_date", "T_MAIN", JnpfKeyConst.DATE));
        var tableFieldKeyValue = new Dictionary<string, string>();

        var sql = Compiler.GetInfoQuerySql("ID123", "f_id", tInfo, ref tableFieldKeyValue);

        AssertSnapshot("GetInfoQuerySql_MainOnly", sql);
    }

    [Fact]
    public void GetInfoQuerySql_WithAux()
    {
        var tInfo = MainOnlyTemplate(new List<IndexSearchFieldModel>());
        tInfo.AuxiliaryTableFieldsModelList = new List<FieldsModel> { Field("aux_jnpf_f_extra", "T_AUX", JnpfKeyConst.COMINPUT) };
        tInfo.SingleFormData = new List<FieldsModel>
        {
            Field("main_jnpf_f_name", "T_MAIN", JnpfKeyConst.COMINPUT),
            Field("main_jnpf_f_status", "T_MAIN", JnpfKeyConst.SELECT),
        };
        tInfo.AllTable = new List<TableModel>
        {
            new() { typeId = "1", table = "T_MAIN", tableField = "F_MAIN_ID" },
            new() { typeId = "0", table = "T_AUX", tableField = "F_AUX_MAIN_ID", relationField = "F_MAIN_ID" },
        };
        var tableFieldKeyValue = new Dictionary<string, string>();

        var sql = Compiler.GetInfoQuerySql("ID123", "f_id", tInfo, ref tableFieldKeyValue);

        AssertSnapshot("GetInfoQuerySql_WithAux", sql + "|" + tableFieldKeyValue.ToJsonString());
    }

    [Fact]
    public void GetListQuerySql_MainOnly_NoFilters()
    {
        var tInfo = MainOnlyTemplate(new List<IndexSearchFieldModel>());
        var input = new VisualDevModelListQueryInput { queryJson = string.Empty, superQueryJson = string.Empty, dataRuleJson = string.Empty };
        var tableFieldKeyValue = new Dictionary<string, string>();

        var sql = Compiler.GetListQuerySql(PureContext(), "f_id", tInfo, ref input, ref tableFieldKeyValue, new List<IConditionalModel>());

        AssertSnapshot("GetListQuerySql_MainOnly_NoFilters", sql);
    }

    [Fact]
    public void GetListQuerySql_MainWithAux_NoFilters()
    {
        var tInfo = MainOnlyTemplate(new List<IndexSearchFieldModel>());
        tInfo.AuxiliaryTableFieldsModelList = new List<FieldsModel> { Field("aux_jnpf_f_extra", "T_AUX", JnpfKeyConst.COMINPUT) };
        tInfo.SingleFormData = new List<FieldsModel>
        {
            Field("main_jnpf_f_name", "T_MAIN", JnpfKeyConst.COMINPUT),
            Field("aux_jnpf_f_extra", "T_AUX", JnpfKeyConst.COMINPUT),
        };
        tInfo.AllTable = new List<TableModel>
        {
            new() { typeId = "1", table = "T_MAIN", tableField = "F_MAIN_ID" },
            new() { typeId = "0", table = "T_AUX", tableField = "F_AUX_MAIN_ID", relationField = "F_MAIN_ID" },
        };
        var input = new VisualDevModelListQueryInput { queryJson = string.Empty, superQueryJson = string.Empty, dataRuleJson = string.Empty };
        var tableFieldKeyValue = new Dictionary<string, string>();

        var sql = Compiler.GetListQuerySql(PureContext(), "f_id", tInfo, ref input, ref tableFieldKeyValue, new List<IConditionalModel>());

        AssertSnapshot("GetListQuerySql_MainWithAux_NoFilters", sql);
    }

    [Fact]
    public void GetSuperQueryInput_TextEqual()
    {
        var json = """
            {
              "matchLogic": "AND",
              "conditionList": [
                {
                  "logic": "AND",
                  "groups": [
                    {
                      "jnpfKey": "input",
                      "field": "f_name",
                      "symbol": "==",
                      "fieldValue": "alice"
                    }
                  ]
                }
              ]
            }
            """;

        var result = Compiler.GetSuperQueryInput(json);

        AssertSnapshot("GetSuperQueryInput_TextEqual", result);
    }
}
