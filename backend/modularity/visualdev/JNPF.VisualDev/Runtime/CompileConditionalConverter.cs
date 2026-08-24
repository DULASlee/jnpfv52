using SqlSugar;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// SqlSugar 条件模型 ↔ 平台条件模型 双向转换（裁决 C 边界适配器）.
/// 归属=RunService 边界侧（非引擎类，不计入 4.3.8 验收① grep 对象）；递归深拷贝.
/// 事实：CustomConditionalFunc/CustomParameterValue 全仓零消费不承载（裁决 C 记录）；
/// FieldValueConvertFunc 引用直传（只读语义，跨边界共享无变异风险）.
/// </summary>
public static class CompileConditionalConverter
{
    /// <summary>
    /// SqlSugar → 平台（列表）.
    /// </summary>
    public static List<ICompileConditionalModel> ToCompile(List<IConditionalModel> source)
        => source?.Select(ToCompile).ToList();

    /// <summary>
    /// SqlSugar → 平台（单项，按运行时类型分派）.
    /// </summary>
    public static ICompileConditionalModel ToCompile(IConditionalModel source)
    {
        switch (source)
        {
            case null:
                return null;
            case ConditionalTree tree:
                return new CompileConditionalTree
                {
                    ConditionalList = tree.ConditionalList?.Select(kv =>
                        new KeyValuePair<CompileWhereType, ICompileConditionalModel>(
                            (CompileWhereType)(int)kv.Key, ToCompile(kv.Value))).ToList()
                };
            case ConditionalCollections collections:
                return new CompileConditionalCollections
                {
                    ConditionalList = collections.ConditionalList?.Select(kv =>
                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(
                            (CompileWhereType)(int)kv.Key, ToCompileModel(kv.Value))).ToList()
                };
            case ConditionalModel model:
                return ToCompileModel(model);
            default:
                throw new NotSupportedException($"未承载的条件模型类型：{source.GetType().FullName}");
        }
    }

    /// <summary>
    /// 平台 → SqlSugar（列表）.
    /// </summary>
    public static List<IConditionalModel> ToSqlSugar(List<ICompileConditionalModel> source)
        => source?.Select(ToSqlSugar).ToList();

    /// <summary>
    /// 平台 → SqlSugar（单项，按运行时类型分派）.
    /// </summary>
    public static IConditionalModel ToSqlSugar(ICompileConditionalModel source)
    {
        switch (source)
        {
            case null:
                return null;
            case CompileConditionalTree tree:
                return new ConditionalTree
                {
                    ConditionalList = tree.ConditionalList?.Select(kv =>
                        new KeyValuePair<WhereType, IConditionalModel>(
                            (WhereType)(int)kv.Key, ToSqlSugar(kv.Value))).ToList()
                };
            case CompileConditionalCollections collections:
                return new ConditionalCollections
                {
                    ConditionalList = collections.ConditionalList?.Select(kv =>
                        new KeyValuePair<WhereType, ConditionalModel>(
                            (WhereType)(int)kv.Key, ToSqlSugarModel(kv.Value))).ToList()
                };
            case CompileConditionalModel model:
                return ToSqlSugarModel(model);
            default:
                throw new NotSupportedException($"未承载的平台条件模型类型：{source.GetType().FullName}");
        }
    }

    private static CompileConditionalModel ToCompileModel(ConditionalModel model)
    {
        if (model == null) return null;
        return new CompileConditionalModel
        {
            FieldName = model.FieldName,
            FieldValue = model.FieldValue,
            CSharpTypeName = model.CSharpTypeName,
            ConditionalType = (CompileConditionalType)(int)model.ConditionalType,
            FieldValueConvertFunc = model.FieldValueConvertFunc,
        };
    }

    private static ConditionalModel ToSqlSugarModel(CompileConditionalModel model)
    {
        if (model == null) return null;
        return new ConditionalModel
        {
            FieldName = model.FieldName,
            FieldValue = model.FieldValue,
            CSharpTypeName = model.CSharpTypeName,
            ConditionalType = (ConditionalType)(int)model.ConditionalType,
            FieldValueConvertFunc = model.FieldValueConvertFunc,
        };
    }
}
