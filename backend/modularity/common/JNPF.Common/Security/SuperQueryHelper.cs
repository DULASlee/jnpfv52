using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Models;
using SqlSugar;

namespace JNPF.Common.Security;

/// <summary>
/// 高级查询帮助类.
/// </summary>
public class SuperQueryHelper
{
    /// <summary>
    /// 组装高级查询信息.
    /// </summary>
    /// <param name="superQueryJson">查询条件json.</param>
    /// <param name="replaceContent">取代内容.</param>
    /// <param name="entityInfo">实体信息.</param>
    /// <param name="tableType">表类型 0-主表,1-子表,2-副表.</param>
    public static ConvertSuper GetSuperQueryInput(string superQueryJson, string replaceContent, EntityInfo entityInfo, int tableType)
    {
        SuperQueryModel? model = string.IsNullOrEmpty(superQueryJson) ? null : superQueryJson.ToObject<SuperQueryModel>();
        var result = new ConvertSuper();

        if (model != null && model.conditionList != null && model.conditionList.Any())
        {
            result.whereType = model.matchLogic.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
            result.convertGroups = new List<ConvertSuperGroup>();
            foreach (var groupItem in model.conditionList)
            {
                var subWhereType = groupItem.logic.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
                var firstItem = true;
                var between = new List<string>();
                var convertGroupItems = new List<ConvertSuperQuery>();
                foreach (var item in groupItem.groups)
                {
                    var field = string.Empty;
                    switch (tableType)
                    {
                        case 1:
                            if (item.field.Contains(replaceContent))
                            {
                                field = entityInfo.Columns.Find(it => it.PropertyName.Equals(item.field.Replace(replaceContent, "").ToUpperCase()))?.DbColumnName;
                            }
                            break;
                        case 2:
                            if (item.field.Contains(replaceContent) && item.field.Contains("_jnpf_"))
                            {
                                var queryField = item.field.Replace("_jnpf_", "@").Split('@')[1];
                                field = entityInfo.Columns.Find(it => it.PropertyName.Equals(queryField.ToUpperCase()))?.DbColumnName;
                            }
                            break;
                        default:
                            field = entityInfo.Columns.Find(it => it.PropertyName.Equals(item.field.ToUpperCase()))?.DbColumnName;
                            break;
                    }

                    if (string.IsNullOrEmpty(field)) continue;

                    var query = new ConvertSuperQuery();
                    query.whereType = subWhereType;
                    query.jnpfKey = item.__config__.jnpfKey;
                    query.field = field;

                    if (firstItem)
                    {
                        query.whereType = result.whereType;
                        firstItem = false;
                    }

                    if (item.fieldValue.IsNotEmptyOrNull())
                    {
                        item.fieldValue = item.fieldValue.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty);
                    }
                    else
                    {
                        if (item.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) item.fieldValue = null;
                        else item.fieldValue = string.Empty;
                    }

                    if (item.fieldValue.IsNotEmptyOrNull())
                    {
                        if (item.symbol.Equals("between"))
                            between = item.fieldValue.ToString().ToObject<List<string>>();
                        switch (item.__config__.jnpfKey)
                        {
                            case JnpfKeyConst.DATE:
                            case JnpfKeyConst.CREATETIME:
                            case JnpfKeyConst.MODIFYTIME:
                                if (item.symbol.Equals("between"))
                                {
                                    var startTime = between.First().TimeStampToDateTime();
                                    var endTime = between.Last().TimeStampToDateTime();
                                    between[0] = startTime.ToString();
                                    between[1] = endTime.ToString();
                                }
                                else
                                {
                                    item.fieldValue = item.fieldValue.ToString().TimeStampToDateTime().ToString();
                                }
                                break;
                            case JnpfKeyConst.TIME:
                                if (!item.symbol.Equals("between"))
                                {
                                    item.fieldValue = string.Format("{0:" + item.format + "}", Convert.ToDateTime(item.fieldValue));
                                }
                                break;
                        }
                    }

                    query.fieldValue = item.fieldValue?.ToString();

                    if ((item.fieldValue.IsNullOrEmpty() && !item.symbol.Equals("notNull")) || ((item.fieldValue.IsNullOrEmpty() || item.fieldValue.ToString().Equals("[]")) && item.symbol.Equals("==")))
                    {
                        if (item.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) query.conditionalType = ConditionalType.EqualNull;
                        else query.conditionalType = ConditionalType.IsNullOrEmpty;
                        convertGroupItems.Add(query);
                        continue;
                    }

                    if ((item.fieldValue.IsNullOrEmpty() || (item.fieldValue.ToString().Equals("[]")) && item.symbol.Equals("<>")))
                    {
                        query.conditionalType = ConditionalType.IsNot;
                        convertGroupItems.Add(query);
                        continue;
                    }

                    switch (item.symbol)
                    {
                        case ">=":
                            query.conditionalType = ConditionalType.GreaterThanOrEqual;
                            break;
                        case ">":
                            query.conditionalType = ConditionalType.GreaterThan;
                            break;
                        case "==":
                            query.conditionalType = ConditionalType.Equal;
                            break;
                        case "<=":
                            query.conditionalType = ConditionalType.LessThanOrEqual;
                            break;
                        case "<":
                            query.conditionalType = ConditionalType.LessThan;
                            break;
                        case "<>":
                            query.conditionalType = ConditionalType.NoEqual;
                            break;
                        case "like":
                            query.conditionalType = item.fieldValue.IsNotEmptyOrNull() ? ConditionalType.Like : ((item.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty);
                            if (query.fieldValue.IsNotEmptyOrNull() && query.fieldValue.Contains("["))
                                query.fieldValue = query.fieldValue.Replace("[", string.Empty).Replace("]", string.Empty);
                            break;
                        case "notLike":
                            query.conditionalType = ConditionalType.NoLike;
                            if (query.fieldValue.IsNotEmptyOrNull() && query.fieldValue.Contains("["))
                                query.fieldValue = query.fieldValue.Replace("[", string.Empty).Replace("]", string.Empty);
                            break;
                        case "in":
                        case "notIn":
                            if (query.fieldValue.IsNotEmptyOrNull() && query.fieldValue.Contains("["))
                            {
                                var isListValue = false;
                                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || item.__config__.jnpfKey.Equals(JnpfKeyConst.ADDRESS))
                                    isListValue = true;
                                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT)) isListValue = false;
                                var ids = new List<string>();
                                if (query.fieldValue.Replace("\r\n", "").Replace(" ", "").Contains("[["))
                                {
                                    if (item.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE)) ids = query.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList();
                                    else ids = query.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
                                }
                                else
                                {
                                    if (item.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE)) ids = query.fieldValue.ToObject<List<string>>().Select(x => x + "\"]").ToList();
                                    else ids = query.fieldValue.ToObject<List<string>>();
                                }

                                var convertSuperQuery = new ConvertSuperQuery();
                                var childConvertSuperQuery = new List<ConvertSuperQuery>();
                                for (var i = 0; i < ids.Count; i++)
                                {
                                    var it = ids[i];
                                    var conditionWhereType = WhereType.And;
                                    if (item.symbol.Equals("in")) conditionWhereType = i.Equals(0) && subWhereType.Equals(WhereType.And) ? WhereType.And : WhereType.Or;
                                    else conditionWhereType = i.Equals(0) && subWhereType.Equals(WhereType.Or) ? WhereType.Or : WhereType.And;

                                    childConvertSuperQuery.Add(ControlAdvancedQueryAssembly(conditionWhereType, item.__config__.jnpfKey, field, isListValue ? it.ToJsonString() : it, item.symbol.Equals("in") ? (item.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.Equal : ConditionalType.Like) : (item.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.NoEqual : ConditionalType.NoLike)));
                                }
                                convertSuperQuery.whereType = WhereType.And;
                                convertSuperQuery.childConvertSuperQuery = childConvertSuperQuery;
                                convertGroupItems.Add(convertSuperQuery);

                                if (item.symbol.Equals("notIn"))
                                {
                                    convertGroupItems.Add(ControlAdvancedQueryAssembly(WhereType.And, item.__config__.jnpfKey, field, null, ConditionalType.IsNot));
                                    convertGroupItems.Add(ControlAdvancedQueryAssembly(WhereType.And, item.__config__.jnpfKey, field, string.Empty, ConditionalType.IsNot));
                                }

                                continue;
                            }

                            query.conditionalType = item.symbol.Equals("in") ? ConditionalType.In : ConditionalType.NotIn;
                            break;
                        case "null":
                            query.conditionalType = (item.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty;
                            break;
                        case "notNull":
                            query.conditionalType = ConditionalType.IsNot;
                            break;
                        case "between":
                            query.conditionalType = ConditionalType.GreaterThanOrEqual;
                            query.fieldValue = between[0];
                            convertGroupItems.Add(query);
                            convertGroupItems.Add(ControlAdvancedQueryAssembly(WhereType.And, item.__config__.jnpfKey, field, between[1], ConditionalType.LessThanOrEqual));
                            continue;
                    }

                    convertGroupItems.Add(query);
                }
                result.convertGroups.Add(new ConvertSuperGroup() { whereType = result.whereType, convertSuperQuery = convertGroupItems });
            }
        }
        else
        {
            return null;
        }

        return result;
    }

    public static ConvertSuperQuery ControlAdvancedQueryAssembly(WhereType whereType, string jnpfKey, string field, string fieldValue, ConditionalType conditionalType, bool mainWhere = false, string symbol = "")
    {
        return new ConvertSuperQuery
        {
            whereType = whereType,
            jnpfKey = jnpfKey,
            field = field,
            fieldValue = fieldValue,
            conditionalType = conditionalType,
            mainWhere = mainWhere,
            symbol = symbol
        };
    }

    /// <summary>
    /// 组装高级查询条件.
    /// </summary>
    /// <returns></returns>
    public static List<IConditionalModel> GetSuperQueryJson(ConvertSuper list)
    {
        List<IConditionalModel> conModels = new List<IConditionalModel>();
        if (list != null && list.convertGroups != null && list.convertGroups.Any())
        {
            list.convertGroups.ForEach(item =>
            {
                ConditionalTree conditional = new ConditionalTree();
                conditional.ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>();
                var isNewGroupItem = true;
                item.convertSuperQuery.ForEach(items =>
                {
                    if (items.childConvertSuperQuery != null && items.childConvertSuperQuery.Any())
                    {
                        var childItems = new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>() };
                        items.childConvertSuperQuery.ForEach((childItem) =>
                        {
                            childItems.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(isNewGroupItem ? list.whereType : childItem.whereType, new ConditionalModel
                            {
                                FieldName = childItem.field,
                                ConditionalType = childItem.conditionalType,
                                FieldValue = !string.IsNullOrEmpty(childItem.fieldValue) ? childItem.fieldValue.Replace("\r\n", "").Trim() : null,
                            }));
                            if (isNewGroupItem) isNewGroupItem = false;
                        });
                        conditional.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(WhereType.And, childItems));
                    }
                    else
                    {
                        conditional.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(isNewGroupItem ? list.whereType : items.whereType, new ConditionalModel
                        {
                            FieldName = items.field,
                            ConditionalType = items.conditionalType,
                            FieldValue = !string.IsNullOrEmpty(items.fieldValue) ? items.fieldValue.Replace("\r\n", "").Trim() : null,
                        }));
                    }
                    if (isNewGroupItem) isNewGroupItem = false;
                });
                if (conditional.ConditionalList.Any()) conModels.Add(conditional);
            });
        }
        return conModels;
    }

    /// <summary>
    /// 根据用户Id,获取用户关系id集合.
    /// </summary>
    /// <param name="fieldValue"></param>
    /// <returns></returns>
    private static List<Dictionary<string, string>> GetUserRelationByUserId(string fieldValue)
    {
        // 获取数据库连接选项
        ConnectionStringsOptions connectionStrings = App.GetOptions<ConnectionStringsOptions>();
        var defaultConnection = connectionStrings.DefaultConnectionConfig;
        SqlSugarClient db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = JNPFTenantExtensions.ToConnectionString(defaultConnection),
            DbType = defaultConnection.DbType,
            IsAutoCloseConnection = true,
            ConfigId = defaultConnection.ConfigId,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings()
            {
                IsAutoRemoveDataCache = true // 自动清理缓存
            }
        });

        var sql = string.Format("SELECT F_OBJECTID OBJECTID,F_OBJECTTYPE OBJECTTYPE FROM BASE_USERRELATION WHERE F_USERID='{0}'", fieldValue.ToString().Replace("--user", string.Empty));
        var res = db.SqlQueryable<object>(sql).ToDataTable();
        return res.ToObject<List<Dictionary<string, string>>>();
    }
}