using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// GetCondition QueryType → conditionalList append (legacy codegen path).
/// Pure mapping; returns true when the caller should <c>continue</c> to the next field.
/// </summary>
public static class GetConditionQueryClauseAppender
{
    // TODO: CC31 超标，基线锁定于 Task 3.4（maxComplexity=31，只许下降），待拆分重构（Tech-Debt: CC31-Append-Refactor，归因 456e2d6b）
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

        switch (itemMethod)
        {
            case QueryType.Equal:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                return false;
            case QueryType.NotEqual:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoEqual } });
                return false;
            case QueryType.Included:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Like } });
                return false;
            case QueryType.NotIncluded:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoLike } });
                return false;
            case QueryType.GreaterThan:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThan } });
                return false;
            case QueryType.GreaterThanOrEqual:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThanOrEqual } });
                return false;
            case QueryType.LessThan:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThan } });
                return false;
            case QueryType.LessThanOrEqual:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThanOrEqual } });
                return false;
            case QueryType.Between:
                if (between.IsNotEmptyOrNull())
                {
                    conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = between![0], ConditionalType = ConditionalType.GreaterThanOrEqual } });
                    conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[1], ConditionalType = ConditionalType.LessThanOrEqual } });
                    return true;
                }

                return false;
            case QueryType.Null:
                if (fieldType.Equals("double") || fieldType.Equals("int") || fieldType.Equals("bigint"))
                    conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.EqualNull } });
                else
                    conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNullOrEmpty } });
                return false;
            case QueryType.NotNull:
                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNot } });
                return false;
            case QueryType.In:
            case QueryType.NotIn:
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

                conditionalList.Add(new { Key = logicWhere, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                return false;
            default:
                return false;
        }
    }
}
