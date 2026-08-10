using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Data-permission short-circuit builders shared by GetConditionAsync / GetDataConditionAsync / GetCondition.
/// </summary>
public static class DataPermissionShortCircuits
{
    /// <summary>管理员：无额外 WHERE（全权限）.</summary>
    public static List<IConditionalModel> Admin() => new();

    /// <summary>全数据 / 关闭数据权限：primaryKey &lt;&gt; '0'.</summary>
    /// <param name="primaryKeyAsInt">GetCondition primaryKeyPolicy=true 时用 int 转换（与旧实现一致）.</param>
    public static List<IConditionalModel> AllowAll(string fieldName, bool primaryKeyAsInt = false)
    {
        return new List<IConditionalModel>
        {
            new ConditionalCollections
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
                {
                    new(WhereType.And, new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.NoEqual,
                        FieldValue = "0",
                        FieldValueConvertFunc = ConvertFunc(primaryKeyAsInt),
                    }),
                },
            },
        };
    }

    /// <summary>无授权资源：primaryKey = '0'（查不到行）.</summary>
    public static List<IConditionalModel> DenyAll(string fieldName, bool primaryKeyAsInt = false)
    {
        return new List<IConditionalModel>
        {
            new ConditionalCollections
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
                {
                    new(WhereType.And, new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.Equal,
                        FieldValue = "0",
                        FieldValueConvertFunc = ConvertFunc(primaryKeyAsInt),
                    }),
                },
            },
        };
    }

    private static Func<object, object> ConvertFunc(bool primaryKeyAsInt)
        => primaryKeyAsInt
            ? it => UtilMethods.ChangeType2(it, typeof(int))
            : it => UtilMethods.ChangeType2(it, typeof(string));
}
