using JNPF.Common.Security;
using JNPF.VisualDev.Runtime;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Inc-1（裁决 C）：平台条件模型与 SqlSugar 条件模型的 JSON 往返字节等价 + 转换器深拷贝等价.
/// 守护目的：剥离后 input.queryJson/dataRuleJson 等 JSON 面与下游
/// Utilities.JsonToConditionalModels 解析保持逐字兼容.
/// </summary>
public class CompileConditionalEquivalenceTests
{
    /// <summary>
    /// 代表性混合形态：顶层 model / collections（And+Or）/ tree（嵌套 model+collections）.
    /// </summary>
    private static List<IConditionalModel> BuildSqlSugarSample() => new()
    {
        new ConditionalModel
        {
            FieldName = "main.f_name",
            ConditionalType = ConditionalType.Like,
            FieldValue = "abc",
        },
        new ConditionalModel
        {
            FieldName = "main.f_date",
            ConditionalType = ConditionalType.GreaterThanOrEqual,
            FieldValue = "2026-01-01 00:00:00",
            CSharpTypeName = "datetime",
        },
        new ConditionalCollections
        {
            ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
            {
                new(WhereType.And, new ConditionalModel { FieldName = "main.f_status", ConditionalType = ConditionalType.Equal, FieldValue = "1" }),
                new(WhereType.Or, new ConditionalModel { FieldName = "main.f_status", ConditionalType = ConditionalType.Equal, FieldValue = "2" }),
            },
        },
        new ConditionalTree
        {
            ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>
            {
                new(WhereType.And, new ConditionalModel { FieldName = "child_t.f_qty", ConditionalType = ConditionalType.GreaterThan, FieldValue = "0", CSharpTypeName = "decimal" }),
                new(WhereType.Or, new ConditionalCollections
                {
                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
                    {
                        new(WhereType.And, new ConditionalModel { FieldName = "child_t.f_remark", ConditionalType = ConditionalType.IsNullOrEmpty }),
                    },
                }),
            },
        },
    };

    /// <summary>
    /// 与 BuildSqlSugarSample 逐项对齐的平台孪生（手工构建，不经过转换器）.
    /// </summary>
    private static List<ICompileConditionalModel> BuildPlatformTwin() => new()
    {
        new CompileConditionalModel
        {
            FieldName = "main.f_name",
            ConditionalType = CompileConditionalType.Like,
            FieldValue = "abc",
        },
        new CompileConditionalModel
        {
            FieldName = "main.f_date",
            ConditionalType = CompileConditionalType.GreaterThanOrEqual,
            FieldValue = "2026-01-01 00:00:00",
            CSharpTypeName = "datetime",
        },
        new CompileConditionalCollections
        {
            ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>
            {
                new(CompileWhereType.And, new CompileConditionalModel { FieldName = "main.f_status", ConditionalType = CompileConditionalType.Equal, FieldValue = "1" }),
                new(CompileWhereType.Or, new CompileConditionalModel { FieldName = "main.f_status", ConditionalType = CompileConditionalType.Equal, FieldValue = "2" }),
            },
        },
        new CompileConditionalTree
        {
            ConditionalList = new List<KeyValuePair<CompileWhereType, ICompileConditionalModel>>
            {
                new(CompileWhereType.And, new CompileConditionalModel { FieldName = "child_t.f_qty", ConditionalType = CompileConditionalType.GreaterThan, FieldValue = "0", CSharpTypeName = "decimal" }),
                new(CompileWhereType.Or, new CompileConditionalCollections
                {
                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>
                    {
                        new(CompileWhereType.And, new CompileConditionalModel { FieldName = "child_t.f_remark", ConditionalType = CompileConditionalType.IsNullOrEmpty }),
                    },
                }),
            },
        },
    };

    [Fact]
    public void JsonRoundTrip_ByteIdentical_MixedShapes()
    {
        var sqlSugarJson = BuildSqlSugarSample().ToJsonStringOld();
        var platformJson = BuildPlatformTwin().ToJsonStringOld();

        Assert.Equal(sqlSugarJson, platformJson);
    }

    [Fact]
    public void Converter_SqlSugarToCompile_JsonIdentical()
    {
        var source = BuildSqlSugarSample();
        var converted = CompileConditionalConverter.ToCompile(source);

        Assert.Equal(source.ToJsonStringOld(), converted.ToJsonStringOld());
    }

    [Fact]
    public void Converter_CompileToSqlSugar_JsonIdentical()
    {
        var source = BuildPlatformTwin();
        var converted = CompileConditionalConverter.ToSqlSugar(source);

        Assert.Equal(source.ToJsonStringOld(), converted.ToJsonStringOld());
    }

    [Fact]
    public void Converter_RoundTrip_PreservesNestedStructure()
    {
        var source = BuildSqlSugarSample();
        var roundTripped = CompileConditionalConverter.ToSqlSugar(CompileConditionalConverter.ToCompile(source));

        // 顶层类型序列一致
        Assert.Equal(source.Select(x => x.GetType().Name), roundTripped.Select(x => x.GetType().Name));

        var sourceTree = (ConditionalTree)source[3];
        var rtTree = (ConditionalTree)roundTripped[3];
        Assert.Equal(sourceTree.ConditionalList.Count, rtTree.ConditionalList.Count);
        Assert.Equal(WhereType.Or, rtTree.ConditionalList[1].Key);
        var nested = Assert.IsType<ConditionalCollections>(rtTree.ConditionalList[1].Value);
        Assert.Equal("child_t.f_remark", nested.ConditionalList[0].Value.FieldName);
        Assert.Equal(ConditionalType.IsNullOrEmpty, nested.ConditionalList[0].Value.ConditionalType);

        var model = (ConditionalModel)roundTripped[1];
        Assert.Equal("datetime", model.CSharpTypeName);
    }

    [Fact]
    public void FieldValueConvertFunc_JsonIgnoreBothWorlds_ConverterPreservesReference()
    {
        // 实测（2026-08-24）：SqlSugar.ConditionalModel.FieldValueConvertFunc 带 [JsonIgnore]，
        // 置位与否均不入 JSON；平台类型同特性对齐，转换器保持委托引用直传（渲染路径需要）。
        var sqlSugarList = new List<IConditionalModel>
        {
            new ConditionalModel { FieldName = "f_date", CSharpTypeName = "datetime", FieldValueConvertFunc = it => Convert.ToDateTime(it) },
        };
        var platformList = CompileConditionalConverter.ToCompile(sqlSugarList);

        var sqlSugarJson = sqlSugarList.ToJsonStringOld();
        var platformJson = platformList.ToJsonStringOld();
        Assert.Equal(sqlSugarJson, platformJson);
        Assert.DoesNotContain("FieldValueConvertFunc", sqlSugarJson);

        // 委托引用直传（未丢失，供渲染路径使用）
        var model = Assert.IsType<CompileConditionalModel>(platformList[0]);
        Assert.NotNull(model.FieldValueConvertFunc);
        Assert.Same(((ConditionalModel)sqlSugarList[0]).FieldValueConvertFunc, model.FieldValueConvertFunc);
    }

    [Fact]
    public void SqlSugarUtilities_ParsesPlatformJson_BackToOriginalShape()
    {
        // 下游消费面实证：平台类型产出的 JSON 可被 SqlSugar Utilities 原样解析（剥离后 JSON 面兼容的硬证据）.
        var db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.SqlServer,
            ConnectionString = "Server=equivalence-test;Database=none;Integrated Security=true;",
            IsAutoCloseConnection = true,
        });

        var platformJson = BuildPlatformTwin().ToJsonStringOld();
        var parsed = db.Utilities.JsonToConditionalModels(platformJson);

        var reparsedJson = parsed.ToJsonStringOld();
        Assert.Equal(BuildSqlSugarSample().ToJsonStringOld(), reparsedJson);
    }
}
