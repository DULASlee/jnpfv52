using SqlSugar;

namespace JNPF.VisualDev.Query;

/// <summary>
/// RunService.GetIConditionalModelListByTableName — keep conditions whose FieldName
/// <c>Contains(tableName)</c> (no strip). Distinct from CodeGen
/// <c>ConditionalByTableNameFilter</c> (qualified <c>table.</c> + strip last segment).
/// </summary>
public static class ListConditionalByTableNameFilter
{
    /// <summary>
    /// Mutates <paramref name="cList"/> in place (legacy). Tree first child becomes
    /// <see cref="WhereType.Null"/>; leaf match uses <c>Contains(tableName)</c> without renaming.
    /// </summary>
    public static List<IConditionalModel> Filter(List<IConditionalModel> cList, string tableName)
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
                        if (newItem.ConditionalList[j].Equals(newItem.ConditionalList.FirstOrDefault()))
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(WhereType.Null, value.First());
                        else
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(newItem.ConditionalList[j].Key, value.First());
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
            else if (cList[i] is ConditionalModel)
            {
                var newItem = (ConditionalModel)cList[i];
                if (!newItem.FieldName.Contains(tableName))
                {
                    // Legacy omitted i-- and skipped the next leaf after RemoveAt — fixed (same class of bug as CodeGen filter).
                    cList.RemoveAt(i);
                    i--;
                }
            }
        }

        return cList;
    }
}
