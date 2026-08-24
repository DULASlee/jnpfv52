using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using SqlSugar;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Rewrites VisualDev list superQueryJson into ConditionalModel-oriented query list JSON.
/// Extracted from <c>RunService.GetSuperQueryInput</c>.
/// Do not merge with <c>SuperQueryHelper.GetSuperQueryInput</c> (typed CodeGen / entity columns).
/// D1 拆分（战役 D1 · 规格 §2.1）：纯结构重构，行为不变量 I1-I10 由
/// ListSuperQueryInputRewriterTests 32 用例全分支锁定（含怪异行为保真，注释以 I 编号标注）。
/// </summary>
public static class ListSuperQueryInputRewriter
{
    /// <summary>
    /// 简单比较符号 → ConditionalType 直映表（I1，原 switch 分支表驱动化）.
    /// </summary>
    private static readonly Dictionary<string, ConditionalType> SimpleSymbolMap = new()
    {
        [">="] = ConditionalType.GreaterThanOrEqual,
        [">"] = ConditionalType.GreaterThan,
        ["=="] = ConditionalType.Equal,
        ["<="] = ConditionalType.LessThanOrEqual,
        ["<"] = ConditionalType.LessThan,
        ["<>"] = ConditionalType.NoEqual,
    };

    /// <summary>
    /// 门面：信封解析 → 组遍历 → 项分派 → 序列化（签名与输出契约不变，I1/I2）.
    /// </summary>
    public static string Rewrite(string? superQueryJson)
    {
        Dictionary<string, object>? dic = string.IsNullOrEmpty(superQueryJson)
            ? null
            : superQueryJson.ToObject<Dictionary<string, object>>();

        if (dic == null)
            return string.Empty;

        var whereType = ParseLogic(dic.FirstOrDefault().Value);
        var queryList = new List<Dictionary<string, object>>();

        foreach (var dicItem in dic.LastOrDefault().Value.ToObject<List<Dictionary<string, object>>>())
        {
            var subWhereType = ParseLogic(dicItem.FirstOrDefault().Value);
            var firstItem = true;
            var between = new List<string>();

            foreach (var item in dicItem.LastOrDefault().Value.ToObject<List<Dictionary<string, object>>>())
            {
                var query = new Dictionary<string, object>
                {
                    { "whereType", subWhereType },
                    { "jnpfKey", item["jnpfKey"] },
                    { "field", item["field"].ToString() },
                };
                if (firstItem)
                {
                    query.Add("where", whereType);
                    firstItem = false;
                }

                NormalizeFieldValue(item);
                if (item["fieldValue"].IsNotEmptyOrNull())
                {
                    if (item["symbol"].Equals("between"))
                        between = item["fieldValue"].ToString().ToObject<List<string>>();
                    ApplyValueShape(item, query, between);
                }

                query.Add("fieldValue", item["fieldValue"]);

                if (TryEmitEmptyClause(item, query, queryList))
                    continue;
                if (EmitSymbolClause(item, query, queryList, subWhereType, between))
                    continue;

                queryList.Add(query);
            }
        }

        return queryList.ToJsonStringOld();
    }

    /// <summary>
    /// 组间/组内逻辑解析（AND 显式，其余按 OR 语义由调用形态决定，保持原判定逐字）.
    /// </summary>
    private static WhereType ParseLogic(object matchLogic)
        => matchLogic.ToString().ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;

    /// <summary>
    /// I3 fieldValue 归一化：非空剥 \r\n 与空格；空值时数值控件族置 null、其余置空串.
    /// </summary>
    private static void NormalizeFieldValue(Dictionary<string, object> item)
    {
        if (item.ContainsKey("fieldValue") && item["fieldValue"].IsNotEmptyOrNull())
        {
            item["fieldValue"] = item["fieldValue"].ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty);
        }
        else
        {
            if (item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT) || item["jnpfKey"].Equals(JnpfKeyConst.RATE) || item["jnpfKey"].Equals(JnpfKeyConst.SLIDER))
            {
                item["fieldValue"] = null;
            }
            else
            {
                item["fieldValue"] = string.Empty;
            }
        }
    }

    /// <summary>
    /// I4 值形态：DATE 族时间戳转换（between 对/单值）+ TIME 格式化 + RATE/SLIDER decimal 标注.
    /// </summary>
    private static void ApplyValueShape(Dictionary<string, object> item, Dictionary<string, object> query, List<string> between)
    {
        switch (item["jnpfKey"])
        {
            case JnpfKeyConst.DATE:
            case JnpfKeyConst.CREATETIME:
            case JnpfKeyConst.MODIFYTIME:
                if (item["symbol"].Equals("between"))
                {
                    var startTime = between.First().TimeStampToDateTime();
                    var endTime = between.Last().TimeStampToDateTime();
                    between[0] = startTime.ToString();
                    between[1] = endTime.ToString();
                }
                else
                {
                    item["fieldValue"] = item["fieldValue"].ToString().TimeStampToDateTime().ToString();
                }
                query["CSharpTypeName"] = "datetime";
                break;
            case JnpfKeyConst.TIME:
                if (!item["symbol"].Equals("between"))
                {
                    item["fieldValue"] = string.Format("{0:" + item["format"] + "}", Convert.ToDateTime(item["fieldValue"]));
                }
                break;
            case JnpfKeyConst.RATE:
            case JnpfKeyConst.SLIDER:
                query["CSharpTypeName"] = "decimal";
                break;
        }
    }

    /// <summary>
    /// I5 空值短路：== / &lt;&gt; + 空值直发条款.
    /// I8（Q1 怪异，保真）：ContainsKey(...).Equals("[]") 为 bool vs string 恒 false 比较，原样保留.
    /// </summary>
    private static bool TryEmitEmptyClause(Dictionary<string, object> item, Dictionary<string, object> query, List<Dictionary<string, object>> queryList)
    {
        // Legacy: ContainsKey(...).Equals("[]") is always false (bool vs string); kept for fidelity.
        if ((!item.ContainsKey("fieldValue") || item.ContainsKey("fieldValue").Equals("[]")) && item["symbol"].Equals("=="))
        {
            if (item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT)) query.Add("ConditionalType", ConditionalType.EqualNull);
            else query.Add("ConditionalType", ConditionalType.IsNullOrEmpty);
            queryList.Add(query);
            return true;
        }

        if ((!item.ContainsKey("fieldValue") || item.ContainsKey("fieldValue").Equals("[]") || item["fieldValue"].IsNullOrEmpty()) && item["symbol"].Equals("<>"))
        {
            query.Add("ConditionalType", ConditionalType.IsNot);
            queryList.Add(query);
            return true;
        }

        return false;
    }

    /// <summary>
    /// symbol 分派：简单符号查表（I1）+ 特判转发；返回 true=条款已直接入列（调用方 continue）；
    /// 返回 false=仅修饰 query 或未命中（未命中即 I10：条款无 ConditionalType 仍入列）.
    /// </summary>
    private static bool EmitSymbolClause(Dictionary<string, object> item, Dictionary<string, object> query, List<Dictionary<string, object>> queryList, WhereType subWhereType, List<string> between)
    {
        var symbol = item["symbol"].ToString();

        if (SimpleSymbolMap.TryGetValue(symbol, out var simpleType))
        {
            query.Add("ConditionalType", simpleType);
            return false;
        }

        switch (symbol)
        {
            case "like":
                EmitLikeClause(item, query);
                return false;
            case "notLike":
                EmitNotLikeClause(item, query);
                return false;
            case "in":
            case "notIn":
                if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
                {
                    EmitExpandedInNotInClauses(item, query, queryList, subWhereType);
                    return true;
                }
                query.Add("ConditionalType", item["symbol"].Equals("in") ? ConditionalType.In : ConditionalType.NotIn);
                return false;
            case "null":
                if (item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT) || item["jnpfKey"].Equals(JnpfKeyConst.RATE) || item["jnpfKey"].Equals(JnpfKeyConst.SLIDER))
                    query.Add("ConditionalType", ConditionalType.EqualNull);
                else
                    query.Add("ConditionalType", ConditionalType.IsNullOrEmpty);
                return false;
            case "notNull":
                query.Add("ConditionalType", ConditionalType.IsNot);
                if (query["fieldValue"].IsNullOrEmpty()) query["fieldValue"] = null;
                return false;
            case "between":
                EmitBetweenClauses(item, query, queryList, between);
                return true;
        }

        return false;
    }

    /// <summary>
    /// like：空值回退三元（数值族 EqualNull / 其余 IsNullOrEmpty）+ "[" 剥离.
    /// </summary>
    private static void EmitLikeClause(Dictionary<string, object> item, Dictionary<string, object> query)
    {
        query.Add("ConditionalType", item["fieldValue"].IsNotEmptyOrNull() ? ConditionalType.Like : ((item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT)) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty));
        if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
            query["fieldValue"] = query["fieldValue"].ToString().Replace("[", string.Empty).Replace("]", string.Empty);
    }

    /// <summary>
    /// notLike：NoLike + "[" 剥离（与 like 对称）.
    /// </summary>
    private static void EmitNotLikeClause(Dictionary<string, object> item, Dictionary<string, object> query)
    {
        query.Add("ConditionalType", ConditionalType.NoLike);
        if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
            query["fieldValue"] = query["fieldValue"].ToString().Replace("[", string.Empty).Replace("]", string.Empty);
    }

    /// <summary>
    /// I6/I9 解析段：in/notIn 列表值拍平为 id 序列（[[ 嵌套取末级；COMSELECT/CURRORGANIZE 追加 "] 后缀 = Q2 怪异，保真）.
    /// </summary>
    private static List<string> ParseInIds(string fieldValue, object jnpfKey)
    {
        var isComFamily = jnpfKey.Equals(JnpfKeyConst.COMSELECT) || jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE);
        if (fieldValue.Replace("\r\n", "").Replace(" ", "").Contains("[["))
        {
            return isComFamily
                ? fieldValue.ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList()
                : fieldValue.ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
        }

        return isComFamily
            ? fieldValue.ToObject<List<string>>().Select(x => x + "\"]").ToList()
            : fieldValue.ToObject<List<string>>();
    }

    /// <summary>
    /// I6 展开段：逐 id 条款（whereType 序列/控件特判/列表值包裹/首条款 where 透传）+ notIn 追加 IsNot 双条款（Q3，保真）.
    /// </summary>
    private static void EmitExpandedInNotInClauses(Dictionary<string, object> item, Dictionary<string, object> query, List<Dictionary<string, object>> queryList, WhereType subWhereType)
    {
        var isListValue = item["jnpfKey"].Equals(JnpfKeyConst.CHECKBOX) || item["jnpfKey"].Equals(JnpfKeyConst.CASCADER) || item["jnpfKey"].Equals(JnpfKeyConst.ADDRESS);
        if (item["jnpfKey"].Equals(JnpfKeyConst.COMSELECT)) isListValue = false;

        var ids = ParseInIds(item["fieldValue"].ToString(), item["jnpfKey"]);

        for (var i = 0; i < ids.Count; i++)
        {
            var it = ids[i];
            var conditionWhereType = WhereType.And;
            if (item["symbol"].Equals("in")) conditionWhereType = i.Equals(0) && subWhereType.Equals(WhereType.And) ? WhereType.And : WhereType.Or;
            else conditionWhereType = i.Equals(0) && subWhereType.Equals(WhereType.Or) ? WhereType.Or : WhereType.And;

            var newQuery = new Dictionary<string, object>();
            newQuery.Add("whereType", conditionWhereType);
            newQuery.Add("jnpfKey", item["jnpfKey"]);
            newQuery.Add("field", item["field"].ToString());
            newQuery.Add("fieldValue", isListValue ? it.ToJsonString() : it);
            newQuery.Add("ConditionalType", item["symbol"].Equals("in") ? (item["jnpfKey"].Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.Equal : ConditionalType.Like) : (item["jnpfKey"].Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.NoEqual : ConditionalType.NoLike));
            if (query.ContainsKey("where") && i.Equals(0))
                newQuery.Add("where", query["where"]);

            queryList.Add(newQuery);
        }

        if (item["symbol"].Equals("notIn"))
        {
            var nullQuery = new Dictionary<string, object>();
            nullQuery.Add("whereType", WhereType.And);
            nullQuery.Add("jnpfKey", item["jnpfKey"]);
            nullQuery.Add("field", item["field"].ToString());
            nullQuery.Add("fieldValue", null);
            nullQuery.Add("ConditionalType", ConditionalType.IsNot);
            queryList.Add(nullQuery);

            var emptyQuery = new Dictionary<string, object>();
            emptyQuery.Add("whereType", WhereType.And);
            emptyQuery.Add("jnpfKey", item["jnpfKey"]);
            emptyQuery.Add("field", item["field"].ToString());
            emptyQuery.Add("fieldValue", string.Empty);
            emptyQuery.Add("ConditionalType", ConditionalType.IsNot);
            queryList.Add(emptyQuery);
        }
    }

    /// <summary>
    /// I7 between 双条款：GreaterThanOrEqual（值=between[0]）+ And + LessThanOrEqual（值=between[1]，CSharpTypeName 透传）.
    /// </summary>
    private static void EmitBetweenClauses(Dictionary<string, object> item, Dictionary<string, object> query, List<Dictionary<string, object>> queryList, List<string> between)
    {
        query.Add("ConditionalType", ConditionalType.GreaterThanOrEqual);
        query["fieldValue"] = between[0];
        queryList.Add(query);

        var queryAnd = new Dictionary<string, object>();
        queryAnd.Add("whereType", WhereType.And);
        queryAnd.Add("jnpfKey", item["jnpfKey"]);
        queryAnd.Add("field", item["field"].ToString());
        queryAnd.Add("ConditionalType", ConditionalType.LessThanOrEqual);
        queryAnd.Add("fieldValue", between[1]);
        if (query.ContainsKey("CSharpTypeName"))
            queryAnd.Add("CSharpTypeName", query["CSharpTypeName"]);
        queryList.Add(queryAnd);
    }
}
