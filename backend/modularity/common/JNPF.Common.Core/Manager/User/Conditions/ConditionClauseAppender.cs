using JNPF.Common.Enums;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Shared clause append logic extracted from UserManager switch arms (behavior-preserving).
/// </summary>
public static class ConditionClauseAppender
{
    public static void AppendIds(List<object> conditionalList, ConditionStrategyContext ctx)
    {
        if (ctx.Ids == null || ctx.Ids.Count == 0)
            return;

        var isCurrentRole = ctx.IsCurrentRole;
        for (var i = 0; i < ctx.Ids.Count; i++)
        {
            var id = ctx.Ids[i];
            if (i == 0)
            {
                switch (ctx.Logic)
                {
                    case "and":
                        conditionalList.Add(new
                        {
                            Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And,
                            Value = new
                            {
                                FieldName = ctx.ItemField,
                                FieldValue = id,
                                ConditionalType = (int)ctx.ConditionalType,
                            },
                        });
                        break;
                    case "or":
                        conditionalList.Add(new
                        {
                            Key = (int)WhereType.Or,
                            Value = new
                            {
                                FieldName = ctx.ItemField,
                                FieldValue = id,
                                ConditionalType = (int)ctx.ConditionalType,
                            },
                        });
                        break;
                }
            }
            else
            {
                if (ctx.ItemMethod.Equals(QueryType.NotEqual) || ctx.ItemMethod.Equals(QueryType.NotIncluded))
                {
                    conditionalList.Add(new
                    {
                        Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And,
                        Value = new
                        {
                            FieldName = ctx.ItemField,
                            FieldValue = id,
                            ConditionalType = (int)ctx.ConditionalType,
                        },
                    });
                }
                else
                {
                    conditionalList.Add(new
                    {
                        Key = (int)WhereType.Or,
                        Value = new
                        {
                            FieldName = ctx.ItemField,
                            FieldValue = id,
                            ConditionalType = (int)ctx.ConditionalType,
                        },
                    });
                }
            }

            isCurrentRole = false;
        }

        ctx.IsCurrentRole = isCurrentRole;
    }
}
