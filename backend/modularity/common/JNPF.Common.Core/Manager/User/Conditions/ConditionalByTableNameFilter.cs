using JNPF.Common.Extension;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Filter / strip table-qualified field names for CodeGen authorize splitting
/// (<c>GetCodeGenAuthorizeModuleResource</c> path). Mutates <paramref name="cList"/> in place
/// (same as legacy UserManager private helper).
/// </summary>
/// <remarks>
/// VisualDev RunService has a different algorithm (WhereType.Null on first tree child;
/// Contains(tableName) without strip) — do not reuse this type there without a dedicated mode.
/// </remarks>
public static class ConditionalByTableNameFilter
{
    /// <summary>
    /// When <paramref name="tableName"/> is null/empty: keep only fields without '.' (main table).
    /// When set: keep fields containing <c>{tableName}.</c>, then strip to the last segment.
    /// </summary>
    public static List<IConditionalModel> Filter(List<IConditionalModel> cList, string? tableName)
    {
        for (var i = 0; i < cList.Count; i++)
        {
            if (cList[i] is ConditionalTree)
            {
                var newItem = (ConditionalTree)cList[i];
                for (var j = 0; j < newItem.ConditionalList.Count; j++)
                {
                    var value = Filter(new List<IConditionalModel> { newItem.ConditionalList[j].Value }, tableName);
                    if (value != null && value.Any())
                    {
                        newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(
                            newItem.ConditionalList[j].Key, value.First());
                    }
                    else
                    {
                        newItem.ConditionalList.RemoveAt(j);
                        j--;
                    }
                }

                if (newItem.ConditionalList.Any())
                {
                    cList[i] = newItem;
                }
                else
                {
                    cList.RemoveAt(i);
                    i--;
                }
            }
            else if (cList[i] is ConditionalCollections)
            {
                var newItemList = (ConditionalCollections)cList[i];

                for (var j = 0; j < newItemList.ConditionalList.Count; j++)
                {
                    if ((tableName.IsNullOrEmpty() && newItemList.ConditionalList[j].Value.FieldName.Contains("."))
                        || (tableName.IsNotEmptyOrNull() && !newItemList.ConditionalList[j].Value.FieldName.Contains(tableName + ".")))
                    {
                        // Fix: legacy omitted j-- and skipped the next leaf after a removal.
                        newItemList.ConditionalList.RemoveAt(j);
                        j--;
                    }
                    else
                    {
                        newItemList.ConditionalList[j].Value.FieldName =
                            newItemList.ConditionalList[j].Value.FieldName.Split(".").Last();
                    }
                }

                if (newItemList.ConditionalList.Any())
                {
                    cList[i] = newItemList;
                }
                else
                {
                    cList.RemoveAt(i);
                    i--;
                }
            }
            else if (cList[i] is ConditionalModel)
            {
                var newItem = (ConditionalModel)cList[i];
                if ((tableName.IsNullOrEmpty() && newItem.FieldName.Contains("."))
                    || (tableName.IsNotEmptyOrNull() && !newItem.FieldName.Contains(tableName + ".")))
                {
                    // Fix: legacy omitted i-- and skipped the next model after a removal.
                    cList.RemoveAt(i);
                    i--;
                }
                else
                {
                    newItem.FieldName = newItem.FieldName.Split(".").Last();
                    cList[i] = newItem;
                }
            }
        }

        return cList;
    }

    /// <summary>
    /// Collect BindTable names from authorize scheme ConditionJson (CodeGen table loop).
    /// </summary>
    public static List<string> CollectBindTableNames(
        IEnumerable<(bool HasJson, IEnumerable<string?> BindTables)> schemes)
    {
        var all = new List<string>();
        foreach (var scheme in schemes)
        {
            if (!scheme.HasJson)
                continue;
            foreach (var t in scheme.BindTables)
            {
                if (t != null)
                    all.Add(t);
            }
        }

        return all;
    }
}
