using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Engine.Entity.Model;
// IndexGridFieldModel lives in JNPF.Engine.Entity.Model

namespace JNPF.VisualDev.Query;

/// <summary>
/// Pure post-shape helpers for RunService.GetListResult (paging / filters / field rewrites).
/// </summary>
public static class ListResultShapeHelpers
{
    /// <summary>
    /// Rewrite permission JSON FieldName from logical key → physical value (AllTableFields map).
    /// Legacy code discarded Replace() return value and used inverted Replace args — dead code.
    /// This implements the intended Key→Value rewrite and assigns the result.
    /// </summary>
    public static string RewritePermissionFieldNames(
        string pvalueJson,
        IReadOnlyDictionary<string, string> allTableFields)
    {
        if (pvalueJson.IsNullOrEmpty() || allTableFields == null || allTableFields.Count == 0)
            return pvalueJson;

        var json = pvalueJson;
        foreach (var item in allTableFields)
        {
            var from = string.Format("\"FieldName\":\"{0}\",", item.Key);
            if (!json.Contains(from))
                continue;
            var to = string.Format("\"FieldName\":\"{0}\",", item.Value);
            json = json.Replace(from, to);
        }

        return json;
    }

    public static List<Dictionary<string, object>> ApplyInMemorySort(
        List<Dictionary<string, object>> list,
        string? sidx,
        string? sort)
    {
        if (list == null || list.Count == 0 || sidx.IsNullOrEmpty())
            return list;

        // Match legacy GetListResult: exact "desc" (case-sensitive).
        if (sort == "desc")
        {
            return list.OrderByDescending(x =>
            {
                var dic = x as IDictionary<string, object>;
                dic.GetOrAdd(sidx!, () => null);
                return dic[sidx!];
            }).ToList();
        }

        return list.OrderBy(x =>
        {
            var dic = x as IDictionary<string, object>;
            dic.GetOrAdd(sidx!, () => null);
            return dic[sidx!];
        }).ToList();
    }

    public static void ApplyInMemoryPaging(
        PageResult<Dictionary<string, object>> realList,
        int pageSize,
        int currentPage,
        bool takePageSlice)
    {
        realList.pagination = new PageResult
        {
            total = realList.list.Count,
            pageSize = pageSize,
            currentPage = currentPage,
        };
        if (takePageSlice)
            realList.list = realList.list.Skip(pageSize * (currentPage - 1)).Take(pageSize).ToList();
    }

    public static List<Dictionary<string, object>> FilterProcessReviewCompleted(
        List<Dictionary<string, object>> list)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var item in list)
        {
            if (item.ContainsKey("flowState") && item["flowState"].Equals(2))
                result.Add(item);
        }

        return result;
    }

    public static List<Dictionary<string, object>> FilterOnlyId(
        List<Dictionary<string, object>> list)
    {
        var onlyIdDic = new List<Dictionary<string, object>>();
        foreach (var item in list)
        {
            if (item.ContainsKey("id"))
                onlyIdDic.Add(new Dictionary<string, object> { ["id"] = item["id"] });
        }

        return onlyIdDic;
    }

    public static string ResolveGroupShowField(IReadOnlyList<IndexGridFieldModel> showFieldList)
    {
        var leftFixed = showFieldList.FirstOrDefault(it => it.@fixed != null && it.@fixed.Equals("left"));
        return leftFixed != null ? leftFixed.__vModel__ : showFieldList.First().__vModel__;
    }

    public static void AttachTreeParentMirror(
        List<Dictionary<string, object>> list,
        string parentField)
    {
        var key = parentField + "_pid";
        foreach (var item in list)
            item[key] = item[parentField];
    }
}
