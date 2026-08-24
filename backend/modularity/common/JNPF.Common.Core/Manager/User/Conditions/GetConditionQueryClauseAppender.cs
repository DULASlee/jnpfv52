using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// GetCondition QueryType → conditionalList append (legacy codegen path).
/// Pure mapping; returns true when the caller should <c>continue</c> to the next field.
/// D1 拆分（战役 D1 · 规格 §2.5）：纯结构重构，行为不变量 I1-I6 由
/// GetConditionQueryClauseAppenderTests 33 用例全分支锁定
/// （含 Q10 回退 Equal / Q11 "null" 字符串 / 空 between 与嵌套数组异常保真，注释以 I 编号标注）。
/// </summary>
public static class GetConditionQueryClauseAppender
{
    // I1：八种直映 QueryType → ConditionalType（静态只读字典直映，取代原 8 个 case）
    private static readonly Dictionary<QueryType, ConditionalType> SimpleQueryTypeMap = new()
    {
        { QueryType.Equal, ConditionalType.Equal },
        { QueryType.NotEqual, ConditionalType.NoEqual },
        { QueryType.Included, ConditionalType.Like },
        { QueryType.NotIncluded, ConditionalType.NoLike },
        { QueryType.GreaterThan, ConditionalType.GreaterThan },
        { QueryType.GreaterThanOrEqual, ConditionalType.GreaterThanOrEqual },
        { QueryType.LessThan, ConditionalType.LessThan },
        { QueryType.LessThanOrEqual, ConditionalType.LessThanOrEqual },
    };

    public static bool Append(
        List<object> conditionalList,
        QueryType itemMethod,
        string itemField,
        object? itemValue,
        List<string>? between,
        string? fieldType,
        string logic)
    {
        var logicWhere = logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And;

        if (SimpleQueryTypeMap.TryGetValue(itemMethod, out var direct))
        {
            conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = direct } });
            return false;
        }

        switch (itemMethod)
        {
            case QueryType.Between:
                return AppendBetween(conditionalList, logicWhere, itemField, between);
            case QueryType.Null:
                AppendNull(conditionalList, logicWhere, itemField, itemValue, fieldType);
                return false;
            case QueryType.NotNull:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNot } });
                return false;
            case QueryType.In:
            case QueryType.NotIn:
                return AppendInNotIn(conditionalList, itemMethod, itemField, itemValue, logic);
            default: // I6：未映射 QueryType（如 Contains "模糊"）不追加、返回 false
                return false;
        }
    }

    /// <summary>
    /// I2：Between 双条款（GreaterThanOrEqual + And + LessThanOrEqual）。
    /// 空列表时 between[0] 抛 ArgumentOutOfRangeException 为既有行为（IsNotEmptyOrNull 仅判 null），保真不修。
    /// </summary>
    private static bool AppendBetween(
        List<object> conditionalList, int logicWhere, string itemField, List<string>? between)
    {
        if (between.IsNotEmptyOrNull())
        {
            conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = between![0], ConditionalType = ConditionalType.GreaterThanOrEqual } });
            conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[1], ConditionalType = ConditionalType.LessThanOrEqual } });
            return true;
        }

        return false;
    }

    /// <summary>
    /// I3：Null 按字段类型分流 —— 数值三型 EqualNull，其余 IsNullOrEmpty。
    /// </summary>
    private static void AppendNull(
        List<object> conditionalList, int logicWhere, string itemField, object? itemValue, string? fieldType)
    {
        if (fieldType.Equals("double") || fieldType.Equals("int") || fieldType.Equals("bigint"))
            conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.EqualNull } });
        else
            conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNullOrEmpty } });
    }

    /// <summary>
    /// I4+I5（Q10/Q11 保真）：In/NotIn 列表展开 —— 值含 '[' 时拍平逐 id 展开
    /// （whereType 序列：In 首条随 logic=and 取 And 否则 Or；NotIn 首条随 logic=or 取 Or 否则 And，其余反向恒定），
    /// NotIn 追加字符串 "null" 与空串两条 IsNot 守卫；值不含 '[' 时回退 Equal 条款。
    /// </summary>
    private static bool AppendInNotIn(
        List<object> conditionalList, QueryType itemMethod, string itemField, object? itemValue, string logic)
    {
        if (itemValue != null && itemValue.ToString()!.Contains('['))
        {
            var ids = new List<string>();
            foreach (var valueList in itemValue.ToString()!.ToObject<List<string>>())
            {
                if (valueList.Contains('['))
                {
                    var value = valueList.ToObject<List<string>>();
                    ids.AddRange(value);
                }
                else
                {
                    ids.Add(valueList);
                }
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var it = ids[i];
                var conditionWhereType = WhereType.And;
                if (itemMethod.Equals(QueryType.In))
                    conditionWhereType = i.Equals(0) && logic.Equals("and") ? WhereType.And : WhereType.Or;
                else
                    conditionWhereType = i.Equals(0) && logic.Equals("or") ? WhereType.Or : WhereType.And;

                conditionalList.Add(new
                {
                    Key = (int)conditionWhereType,
                    Value = new
                    {
                        FieldName = itemField,
                        FieldValue = it,
                        ConditionalType = itemMethod.Equals(QueryType.In) ? ConditionalType.Like : ConditionalType.NoLike,
                    },
                });
            }

            if (itemMethod.Equals(QueryType.NotIn))
            {
                conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = "null", ConditionalType = ConditionalType.IsNot } });
                conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNot } });
            }

            return true;
        }

        // I5（Q10 保真）：非列表值（含真实 null 与 "null" 字符串）回退 Equal 条款
        conditionalList.Add(new { Key = logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
        return false;
    }
}
