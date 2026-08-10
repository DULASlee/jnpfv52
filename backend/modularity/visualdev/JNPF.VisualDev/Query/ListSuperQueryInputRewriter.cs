using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using SqlSugar;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Rewrites VisualDev list superQueryJson into ConditionalModel-oriented query list JSON.
/// Extracted from <c>RunService.GetSuperQueryInput</c>.
/// Do not merge with <c>SuperQueryHelper.GetSuperQueryInput</c> (typed CodeGen / entity columns).
/// </summary>
public static class ListSuperQueryInputRewriter
{
    public static string Rewrite(string? superQueryJson)
    {
        Dictionary<string, object>? dic = string.IsNullOrEmpty(superQueryJson)
            ? null
            : superQueryJson.ToObject<Dictionary<string, object>>();

        if (dic == null)
            return string.Empty;

        var matchLogic = dic.FirstOrDefault().Value;
        var whereType = matchLogic.ToString().ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
        var queryList = new List<Dictionary<string, object>>();

        foreach (var dicItem in dic.LastOrDefault().Value.ToObject<List<Dictionary<string, object>>>())
        {
            var subMatchLogic = dicItem.FirstOrDefault().Value;
            var subWhereType = subMatchLogic.ToString().ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;

            var firstItem = true;
            var between = new List<string>();
            foreach (var item in dicItem.LastOrDefault().Value.ToObject<List<Dictionary<string, object>>>())
            {
                var query = new Dictionary<string, object>();
                query.Add("whereType", subWhereType);
                query.Add("jnpfKey", item["jnpfKey"]);
                query.Add("field", item["field"].ToString());
                if (firstItem)
                {
                    query.Add("where", whereType);
                    firstItem = false;
                }
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
                if (item["fieldValue"].IsNotEmptyOrNull())
                {
                    if (item["symbol"].Equals("between"))
                        between = item["fieldValue"].ToString().ToObject<List<string>>();
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

                query.Add("fieldValue", item["fieldValue"]);

                // Legacy: ContainsKey(...).Equals("[]") is always false (bool vs string); kept for fidelity.
                if ((!item.ContainsKey("fieldValue") || item.ContainsKey("fieldValue").Equals("[]")) && item["symbol"].Equals("=="))
                {
                    if (item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT)) query.Add("ConditionalType", ConditionalType.EqualNull);
                    else query.Add("ConditionalType", ConditionalType.IsNullOrEmpty);
                    queryList.Add(query);
                    continue;
                }

                if ((!item.ContainsKey("fieldValue") || item.ContainsKey("fieldValue").Equals("[]") || item["fieldValue"].IsNullOrEmpty()) && item["symbol"].Equals("<>"))
                {
                    query.Add("ConditionalType", ConditionalType.IsNot);
                    queryList.Add(query);
                    continue;
                }

                switch (item["symbol"])
                {
                    case ">=":
                        query.Add("ConditionalType", ConditionalType.GreaterThanOrEqual);
                        break;
                    case ">":
                        query.Add("ConditionalType", ConditionalType.GreaterThan);
                        break;
                    case "==":
                        query.Add("ConditionalType", ConditionalType.Equal);
                        break;
                    case "<=":
                        query.Add("ConditionalType", ConditionalType.LessThanOrEqual);
                        break;
                    case "<":
                        query.Add("ConditionalType", ConditionalType.LessThan);
                        break;
                    case "like":
                        query.Add("ConditionalType", item["fieldValue"].IsNotEmptyOrNull() ? ConditionalType.Like : ((item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT)) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty));
                        if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
                            query["fieldValue"] = query["fieldValue"].ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                        break;
                    case "<>":
                        query.Add("ConditionalType", ConditionalType.NoEqual);
                        break;
                    case "notLike":
                        query.Add("ConditionalType", ConditionalType.NoLike);
                        if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
                            query["fieldValue"] = query["fieldValue"].ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                        break;
                    case "in":
                    case "notIn":
                        if (query["fieldValue"] != null && query["fieldValue"].ToString().Contains("["))
                        {
                            var isListValue = false;
                            if (item["jnpfKey"].Equals(JnpfKeyConst.CHECKBOX) || item["jnpfKey"].Equals(JnpfKeyConst.CASCADER) || item["jnpfKey"].Equals(JnpfKeyConst.ADDRESS))
                                isListValue = true;
                            if (item["jnpfKey"].Equals(JnpfKeyConst.COMSELECT)) isListValue = false;
                            var ids = new List<string>();
                            if (query["fieldValue"].ToString().Replace("\r\n", "").Replace(" ", "").Contains("[["))
                            {
                                if (item["jnpfKey"].Equals(JnpfKeyConst.COMSELECT) || item["jnpfKey"].Equals(JnpfKeyConst.CURRORGANIZE))
                                {
                                    ids = query["fieldValue"].ToString().ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList();
                                }
                                else
                                {
                                    ids = query["fieldValue"].ToString().ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
                                }
                            }
                            else
                            {
                                if (item["jnpfKey"].Equals(JnpfKeyConst.COMSELECT) || item["jnpfKey"].Equals(JnpfKeyConst.CURRORGANIZE))
                                {
                                    ids = query["fieldValue"].ToString().ToObject<List<string>>().Select(x => x + "\"]").ToList();
                                }
                                else
                                {
                                    ids = query["fieldValue"].ToString().ToObject<List<string>>();
                                }
                            }

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

                            continue;
                        }
                        query.Add("ConditionalType", item["symbol"].Equals("in") ? ConditionalType.In : ConditionalType.NotIn);
                        break;
                    case "null":
                        if (item["jnpfKey"].Equals(JnpfKeyConst.CALCULATE) || item["jnpfKey"].Equals(JnpfKeyConst.NUMINPUT) || item["jnpfKey"].Equals(JnpfKeyConst.RATE) || item["jnpfKey"].Equals(JnpfKeyConst.SLIDER))
                            query.Add("ConditionalType", ConditionalType.EqualNull);
                        else
                            query.Add("ConditionalType", ConditionalType.IsNullOrEmpty);
                        break;
                    case "notNull":
                        query.Add("ConditionalType", ConditionalType.IsNot);
                        if (query["fieldValue"].IsNullOrEmpty()) query["fieldValue"] = null;
                        break;
                    case "between":
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
                        continue;
                }

                queryList.Add(query);
            }
        }

        return queryList.ToJsonStringOld();
    }
}
