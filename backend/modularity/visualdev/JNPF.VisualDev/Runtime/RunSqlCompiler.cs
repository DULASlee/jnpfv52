using JNPF.Common.Extension;
using JNPF.Common.Const;
using JNPF.Common.Models.VisualDev;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.Extensions;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Query;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// M3 编译层 — SQL 编译引擎组件（规格 4.3，契约 C-M3-RunSqlCompiler@v1）.
/// 职责：将模型配置编译为 SQL/Json/条件模型.
/// 纪律：构造零 DI 依赖（白名单守护）；SQL 执行不归本组件（留调用方侧，经 IRuntimeDataStore 漏斗）.
/// 施工状态：Task 3.3 裁决 C 完成形态 — 零 ORM 直连引用；DB 读取/SQL 渲染经 <see cref="RunSqlCompileGateway"/> 由调用方供数.
/// 方法体来源：RunService 逐字迁移（机械适配三类，见裁决记录）.
/// </summary>
public class RunSqlCompiler : ISingleton
{
    /// <summary>
    /// 处理模板默认值 (针对流程表单).
    /// 用户选择 , 部门选择 , 岗位选择 , 角色选择 , 分组选择.
    /// </summary>
    /// <param name="propertyJson">表单json.</param>
    /// <param name="tableJson">关联表单.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    /// <returns></returns>
    public string GetVisualDevModelDataConfig(RunSqlCompileGateway gateway, string propertyJson, string tableJson, int formType)
    {
        var tInfo = new TemplateParsingBase(propertyJson, tableJson, formType);
        if (tInfo.AllFieldsModel.Any(x => (x.__config__.defaultCurrent) && (x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.DEPSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.ROLESELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.GROUPSELECT))))
        {
            var d = gateway.UserSelectDefaults();

            var configData = propertyJson.ToObject<Dictionary<string, object>>();
            var columnList = configData["fields"].ToObject<List<Dictionary<string, object>>>();
            FieldBindDefaultValueHelpers.Bind(ref columnList, d.UserId, d.DepId, d.PosIds, d.RoleIds, d.GroupIds, d.AllUserRelationList, d.PositionId);
            configData["fields"] = columnList;
            propertyJson = configData.ToJsonString();
        }

        return propertyJson;
    }

    /// <summary>
    /// 组装高级查询信息.
    /// </summary>
    /// <param name="superQueryJson">查询条件json.</param>
    public string GetSuperQueryInput(string superQueryJson)
        => ListSuperQueryInputRewriter.Rewrite(superQueryJson);

    /// <summary>
    /// 组装列表查询sql.
    /// </summary>
    /// <param name="primaryKey">主键.</param>
    /// <param name="templateInfo">模板.</param>
    /// <param name="input">查询输入.</param>
    /// <param name="tableFieldKeyValue">联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个).</param>
    /// <param name="dataPermissions">数据权限.</param>
    /// <param name="showColumnList">是否只查询显示列.</param>
    /// <returns></returns>
    public string GetListQuerySql(RunSqlCompileGateway gateway, string primaryKey, TemplateParsingBase templateInfo, ref VisualDevModelListQueryInput input, ref Dictionary<string, string> tableFieldKeyValue, List<ICompileConditionalModel> dataPermissions, bool showColumnList = false)
    {
        List<string> fields = new List<string>();

        string? sql = string.Empty; // 查询sql

        // 显示列和搜索列有子表字段
        if (templateInfo.ChildTableFields.Count > 0 && (templateInfo.ColumnData.columnList.Any(x => templateInfo.ChildTableFields.ContainsKey(x.prop)) || templateInfo.ColumnData.searchList.Any(xx => templateInfo.ChildTableFields.ContainsKey(xx.prop))))
        {
            var queryJson = input.queryJson;
            var superQueryJson = input.superQueryJson;
            foreach (var item in templateInfo.AllTableFields)
            {
                if (input.dataRuleJson.IsNotEmptyOrNull() && input.dataRuleJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.dataRuleJson = ListQueryFieldAliasRewriter.ReplaceQuotedKey(input.dataRuleJson, item.Key, item.Value);

                if (queryJson.Contains(string.Format("\"{0}\"", item.Key)))
                {
                    queryJson = ListQueryFieldAliasRewriter.ReplaceQuotedKey(queryJson, item.Key, item.Value);
                    var vmodel = templateInfo.ColumnData.searchList.FirstOrDefault(x => x != null && x.id != null && x.id.Equals(item.Key));
                    var appVModel = templateInfo.AppColumnData.searchList.FirstOrDefault(x => x != null && x.id != null && x.id.Equals(item.Key));
                    ListQuerySqlProjectionHelpers.RemapSearchListFieldAliases(vmodel, appVModel, item.Value, fields);
                }

                if (superQueryJson.IsNotEmptyOrNull() && superQueryJson.Contains(string.Format("\"{0}\"", item.Key)))
                    superQueryJson = ListQueryFieldAliasRewriter.ReplaceQuotedKey(superQueryJson, item.Key, item.Value);
            }

            var dataRuleQuerDic = new List<ICompileConditionalModel>();
            if (input.dataRuleJson.IsNotEmptyOrNull()) dataRuleQuerDic = gateway.JsonToConditions(input.dataRuleJson);
            var querDic = queryJson.IsNullOrEmpty() ? null : queryJson.ToObject<Dictionary<string, object>>();

            var superQuerDic = new List<CompileConditionalCollections>();
            var superCond = superQueryJson.IsNullOrEmpty() ? null : GetSuperQueryJson(superQueryJson, templateInfo);
            if (superCond != null) superQuerDic = superCond.ToObject<List<CompileConditionalCollections>>();
            var sqlStr = ListQuerySqlFragmentHelpers.SelectFromTemplate;

            // 查询
            var querySqlList = new List<string>();
            var isInteAssistant = false;
            if (querDic != null && querDic.Any())
            {
                foreach (var item in querDic)
                {
                    var dic = new Dictionary<string, object>();
                    dic.Add(item.Key, item.Value);
                    var where = GetQueryJson(gateway, dic.ToJsonString(), gateway.UserOrigin == "pc" ? templateInfo.ColumnData : templateInfo.AppColumnData);

                    if (item.Key.Equals(JnpfKeyConst.JNPFKEYWORD))
                    {
                        var keywordSql = string.Empty;
                        foreach (var con in where[0].ToObject<CompileConditionalCollections>().ConditionalList)
                        {
                            var model = con.Value;
                            if (templateInfo.AllTableFields.ContainsKey(model.FieldName))
                                model.FieldName = templateInfo.AllTableFields[model.FieldName];

                            var condition = new List<ICompileConditionalModel> { new CompileConditionalCollections() { ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>> { con } } };
                                                        var itemWhere = gateway.RenderLinkWhere(condition);

                            if (itemWhere.Contains("WHERE"))
                            {
                                var fieldName = model.FieldName.Split(".").FirstOrDefault();
                                SqlGuard.ValidateIdentifier(fieldName, "表名");
                                var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                                var itemSql = ListQuerySqlFragmentHelpers.BuildSelectFrom(idField, primaryKey, fieldName);
                                itemSql = string.Format("{0} where {1}", itemSql, itemWhere.Split("WHERE").Last());
                                var conditionSql = string.Format("({0} in ({1}))", primaryKey, itemSql);

                                if (keywordSql.IsNotEmptyOrNull())
                                    keywordSql = string.Format("{0} {1} {2}", keywordSql, con.Key, conditionSql);
                                else
                                    keywordSql = conditionSql;
                            }
                        }

                        if (keywordSql.IsNotEmptyOrNull())
                        {
                            keywordSql = "(" + keywordSql + ")";
                            querySqlList.Add(keywordSql);
                        }
                    }
                    else
                    {
                        var fieldName = item.Key.Split(".").FirstOrDefault();
                        SqlGuard.ValidateIdentifier(fieldName, "表名");
                        var table = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First();
                        // 去除多余的f_inte_assistant条件
                        if (table.typeId.Equals("1") && !isInteAssistant && where.Count > 1 && where.Last().ToJsonString().Contains("f_inte_assistant"))
                            isInteAssistant = true;
                        else
                            where.RemoveAt(where.Count - 1);

                        var itemSql = ListQuerySqlFragmentHelpers.BuildSelectFrom(table.tableField, primaryKey, fieldName);

                                                var itemWhere = gateway.RenderLinkWhere(where);
                        if (itemWhere.Contains("WHERE"))
                        {
                            itemSql = string.Format("({0} IN ({1}WHERE))", primaryKey, itemSql);
                            if (querySqlList.Any(it => it.Contains(itemSql.TrimEnd(')'))))
                            {
                                var oldSql = querySqlList.Find(it => it.Contains(itemSql.TrimEnd(')')));
                                querySqlList.Remove(oldSql);
                                querySqlList.Add(ListQuerySqlFragmentHelpers.MergeWhereIntoExistingInSubquery(oldSql, itemWhere));
                            }
                            else
                            {
            querySqlList.Add(ListQuerySqlFragmentHelpers.InjectWhereIntoExistingSubquery(itemSql, itemWhere));
                            }
                        }
                    }
                }
            }

            // 高级查询
            var superQuerySqlCondition = string.Empty;
            if (superQuerDic != null && superQuerDic.Any())
            {
                foreach (var item in superQuerDic)
                {
                    // 拼接分组sql条件
                    if (superQuerySqlCondition.IsNotEmptyOrNull())
                        superQuerySqlCondition = string.Format(superQuerySqlCondition + item.ConditionalList.FirstOrDefault().Key);

                    // 分组内的sql
                    var groupDataSql = string.Empty;
                    foreach (var subItem in item.ConditionalList)
                    {
                        if (subItem.Value.IsNotEmptyOrNull())
                        {
                            var fieldName = subItem.Value.FieldName.Split(".").FirstOrDefault();
                            SqlGuard.ValidateIdentifier(fieldName, "表名");
                            var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                            var itemSql = string.Format(sqlStr, idField.IsNullOrEmpty() ? primaryKey : idField, fieldName);

                            var where = new List<ICompileConditionalModel> { new CompileConditionalCollections() { ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>> { subItem } } };
                                                        var itemWhere = gateway.RenderLinkWhere(where);

                            if (itemWhere.Contains("WHERE"))
                            {
                                // 分组内的sql条件
                                var groupDataSqlCondition = subItem.Key.ToString();

                                if (item.ConditionalList.FirstOrDefault().Equals(subItem))
                                {
                                    groupDataSql = string.Format("( " + groupDataSql);
                                    groupDataSqlCondition = string.Empty;
                                }
                                var splitWhere = itemSql + " where";
                                itemSql = splitWhere + itemWhere.Split("WHERE").Last();

                                // 子表字段为空 查询 处理.
                                var subJson = subItem.ToJsonStringOld();
                                if (templateInfo.ChildTableFields.Any(x => x.Value.Contains(fieldName + "."))
                                    && ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson(subJson))
                                {
                                    groupDataSql = groupDataSql + groupDataSqlCondition
                                        + ListQuerySqlFragmentHelpers.BuildChildTableEmptyOrMatch(
                                            primaryKey,
                                            itemSql,
                                            templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField,
                                            fieldName);
                                }
                                else
                                {
                                    if (item.Equals(superQuerDic.FirstOrDefault()))
                                    {
                                        if (groupDataSql.Contains(splitWhere))
                                        {
                                            groupDataSql = string.Format(groupDataSql.Split(splitWhere).FirstOrDefault() + splitWhere + itemWhere.Split("WHERE").Last() + groupDataSqlCondition + groupDataSql.Split(splitWhere).LastOrDefault());
                                        }
                                        else
                                        {
                                            groupDataSql = groupDataSql + groupDataSqlCondition
                                                + ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery(primaryKey, itemSql);
                                        }
                                    }
                                    else
                                    {
                                        groupDataSql = groupDataSql + groupDataSqlCondition
                                            + ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery(primaryKey, itemSql);
                                    }
                                }

                                if (item.ConditionalList.LastOrDefault().Equals(subItem))
                                    groupDataSql = string.Format(groupDataSql + ")");
                            }
                        }
                    }

                    // 拼接分组sql
                    superQuerySqlCondition = string.Format(superQuerySqlCondition + groupDataSql);
                    groupDataSql = string.Empty;
                }
                superQuerySqlCondition = string.Format("and ({0})", superQuerySqlCondition);
            }

            // 数据过滤
            var dataRuleSqlCondition = string.Empty;
            if (dataRuleQuerDic != null && dataRuleQuerDic.Any())
            {
                var dataRule = (CompileConditionalTree)dataRuleQuerDic.FirstOrDefault();
                foreach (var item in dataRule.ConditionalList)
                {
                    // 拼接分组sql条件
                    if (dataRuleSqlCondition.IsNotEmptyOrNull())
                        dataRuleSqlCondition = string.Format(dataRuleSqlCondition + item.Key);

                    // 分组内的sql
                    var groupDataSql = string.Empty;

                    var groupDataValue = (CompileConditionalTree)item.Value;
                    foreach (var subItem in groupDataValue.ConditionalList)
                    {
                        if (subItem.Value.IsNotEmptyOrNull())
                        {
                            var field = ((CompileConditionalTree)subItem.Value).ConditionalList.FirstOrDefault();
                            var fieldName = ((CompileConditionalModel)field.Value).FieldName.Split(".").FirstOrDefault();
                            SqlGuard.ValidateIdentifier(fieldName, "表名");
                            var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                            var itemSql = string.Format(sqlStr, idField.IsNullOrEmpty() ? primaryKey : idField, fieldName);

                            var where = new List<ICompileConditionalModel> { new CompileConditionalTree() { ConditionalList = new List<KeyValuePair<CompileWhereType, ICompileConditionalModel>> { subItem } } };
                                                        var itemWhere = gateway.RenderLinkWhere(where);

                            if (itemWhere.Contains("WHERE"))
                            {
                                // 分组内的sql条件
                                var groupDataSqlCondition = subItem.Key.ToString();

                                if (groupDataValue.ConditionalList.FirstOrDefault().Equals(subItem))
                                {
                                    groupDataSql = string.Format("( " + groupDataSql);
                                    groupDataSqlCondition = string.Empty;
                                }
                                var splitWhere = itemSql + " where";
                                itemSql = splitWhere + itemWhere.Split("WHERE").Last();

                                // 子表字段为空 查询 处理.
                                var subJson = subItem.ToJsonStringOld();
                                if (templateInfo.ChildTableFields.Any(x => x.Value.Contains(fieldName + "."))
                                    && ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson(subJson))
                                {
                                    groupDataSql = groupDataSql + groupDataSqlCondition
                                        + ListQuerySqlFragmentHelpers.BuildChildTableEmptyOrMatch(
                                            primaryKey,
                                            itemSql,
                                            templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField,
                                            fieldName);
                                }
                                else
                                {
                                    groupDataSql = groupDataSql + groupDataSqlCondition
                                        + ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery(primaryKey, itemSql);
                                }

                                if (groupDataValue.ConditionalList.LastOrDefault().Equals(subItem))
                                    groupDataSql = string.Format(groupDataSql + ")");
                            }
                        }
                    }

                    // 拼接分组sql
                    dataRuleSqlCondition = string.Format(dataRuleSqlCondition + groupDataSql);
                    groupDataSql = string.Empty;
                }

                if (dataRuleSqlCondition.IsNotEmptyOrNull()) dataRuleSqlCondition = string.Format("and ({0})", dataRuleSqlCondition);
            }

            // 拼接数据权限
            var dataPermissionsSqlCondition = string.Empty;
            if (dataPermissions != null && dataPermissions.Any())
            {
                var allCondition = (CompileConditionalTree)dataPermissions.FirstOrDefault();
                foreach (var roleCondition in allCondition.ConditionalList)
                {
                    // 拼接多个权限组sql条件
                    if (dataPermissionsSqlCondition.IsNotEmptyOrNull())
                        dataPermissionsSqlCondition = string.Format("(" + dataPermissionsSqlCondition + ")" + roleCondition.Key);

                    var roleConditionSql = string.Empty;
                    if (roleCondition.Value.GetType().Name.Equals("CompileConditionalModel"))
                    {
                        var where = new List<ICompileConditionalModel> { new CompileConditionalTree() { ConditionalList = new List<KeyValuePair<CompileWhereType, ICompileConditionalModel>> { roleCondition } } };
                        var itemWhere = gateway.RenderDefaultWhere(where);
                        roleConditionSql = itemWhere.Split("WHERE").Last();
                    }
                    else
                    {
                        foreach (var dpCondition in ((CompileConditionalTree)roleCondition.Value).ConditionalList)
                        {
                            // 拼接多个权限sql条件
                            if (roleConditionSql.IsNotEmptyOrNull())
                                roleConditionSql = string.Format("(" + roleConditionSql + ")" + dpCondition.Key);

                            var dpConditionSql = string.Empty;
                            foreach (var groupCondition in ((CompileConditionalTree)dpCondition.Value).ConditionalList)
                            {
                                // 拼接分组sql条件
                                if (dpConditionSql.IsNotEmptyOrNull())
                                    dpConditionSql = string.Format(dpConditionSql + groupCondition.Key);

                                var groupConditionSql = string.Empty;
                                foreach (var condition in ((CompileConditionalTree)groupCondition.Value).ConditionalList)
                                {
                                    var fieldName = ((CompileConditionalModel)condition.Value).FieldName.Split(".").FirstOrDefault();
                                    SqlGuard.ValidateIdentifier(fieldName, "表名");
                                    var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                                    var itemSql = string.Format(sqlStr, idField.IsNullOrEmpty() ? primaryKey : idField, fieldName);
                                    var where = new List<ICompileConditionalModel> { new CompileConditionalTree() { ConditionalList = new List<KeyValuePair<CompileWhereType, ICompileConditionalModel>> { condition } } };
                                    var itemWhere = gateway.RenderDefaultWhere(where);
                                    if (itemWhere.Contains("WHERE"))
                                    {
                                        // 分组内的sql条件
                                        var conditionWhere = condition.Key.ToString();
                                        if (((CompileConditionalTree)groupCondition.Value).ConditionalList.FirstOrDefault().Equals(condition))
                                        {
                                            groupConditionSql = string.Format("( " + groupConditionSql);
                                            conditionWhere = string.Empty;
                                        }
                                        var splitWhere = itemSql + " where";
                                        itemSql = splitWhere + itemWhere.Split("WHERE").Last();

                                        // 子表字段为空 查询 处理.
                                        var condJson = condition.ToJsonStringOld();
                                        if (templateInfo.ChildTableFields.Any(x => x.Value.Contains(fieldName + "."))
                                            && ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson(condJson))
                                        {
                                            groupConditionSql = groupConditionSql + conditionWhere
                                                + ListQuerySqlFragmentHelpers.BuildChildTableEmptyOrMatch(
                                                    primaryKey,
                                                    itemSql,
                                                    templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField,
                                                    fieldName);
                                        }
                                        else
                                        {
                                            if (groupCondition.Equals(((CompileConditionalTree)dpCondition.Value).ConditionalList.FirstOrDefault()))
                                            {
                                                if (groupConditionSql.Contains(splitWhere))
                                                {
                                                    groupConditionSql = string.Format(groupConditionSql.Split(splitWhere).FirstOrDefault() + splitWhere + itemWhere.Split("WHERE").Last() + conditionWhere + groupConditionSql.Split(splitWhere).LastOrDefault());
                                                }
                                                else
                                                {
                                                    groupConditionSql = groupConditionSql + conditionWhere
                                                        + ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery(primaryKey, itemSql);
                                                }
                                            }
                                            else
                                            {
                                                groupConditionSql = groupConditionSql + conditionWhere
                                                    + ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery(primaryKey, itemSql);
                                            }
                                        }
                                    }

                                    if (((CompileConditionalTree)groupCondition.Value).ConditionalList.LastOrDefault().Equals(condition))
                                        groupConditionSql = string.Format(groupConditionSql + ")");
                                }

                                // 拼接分组sql
                                dpConditionSql = string.Format(dpConditionSql + groupConditionSql);
                                groupConditionSql = string.Empty;
                            }

                            // 拼接多个权限sql
                            roleConditionSql = string.Format(roleConditionSql + "(" + dpConditionSql + ")");
                            dpConditionSql = string.Empty;
                        }
                    }

                    // 拼接多个权限sql
                    dataPermissionsSqlCondition = string.Format(dataPermissionsSqlCondition + "(" + roleConditionSql + ")");
                    roleConditionSql = string.Empty;
                }
                dataPermissionsSqlCondition = string.Format("and ({0})", dataPermissionsSqlCondition);
            }

            if (templateInfo.FormModel.logicalDelete && gateway.ColumnExists(templateInfo.MainTableName, "f_delete_mark"))
                querySqlList.Add(ListQuerySqlFragmentHelpers.BuildSoftDeleteInSubquery(primaryKey, templateInfo.MainTableName)); // 处理软删除

            // 多租户字段隔离
            if (gateway.MultiTenancy)
            {
                var tenantCache = gateway.TenantCache;
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1) && gateway.ColumnExists(templateInfo.MainTableName, "f_tenant_id"))
                    querySqlList.Add(ListQuerySqlFragmentHelpers.BuildTenantIsolationInSubquery(
                        primaryKey,
                        templateInfo.MainTableName,
                        tenantCache.connectionConfig.IsolationField));
            }

            // 是否只展示流程数据
            //if (templateInfo.visualDevEntity.EnableFlow.Equals(1))
            //    querySqlList.Add(string.Format(" {0} in ({1}) ", primaryKey, string.Format(" select {0} from {1} where f_flow_id <> '' ", primaryKey, templateInfo.MainTableName)));
            //else
            //    querySqlList.Add(string.Format(" {0} in ({1}) ", primaryKey, string.Format(" select {0} from {1} where f_flow_id is null or f_flow_id = '' ", primaryKey, templateInfo.MainTableName)));

            if (!querySqlList.Any())
                querySqlList.Add(ListQuerySqlFragmentHelpers.BuildUnfilteredPrimaryInSubquery(primaryKey, templateInfo.MainTableName));

            var ctFields = templateInfo.ChildTableFields;
            templateInfo.ChildTableFields = new Dictionary<string, string>();
            var strSql = GetListQuerySql(gateway, primaryKey, templateInfo, ref input, ref tableFieldKeyValue, new List<ICompileConditionalModel>());
            input.dataRuleJson = string.Empty;
            input.queryJson = string.Empty;
            input.superQueryJson = string.Empty;
            templateInfo.ChildTableFields = ctFields;

            sql = ListQuerySqlFragmentHelpers.WrapOuterListQuery(
                strSql, querySqlList, superQuerySqlCondition, dataRuleSqlCondition, dataPermissionsSqlCondition);
        }
        else if (!templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            ListQuerySqlProjectionHelpers.SeedSystemProjectionFields(
                fields, tableFieldKeyValue, primaryKey, templateInfo.WebType.Equals(3), mainTablePrefix: null);

            var inputJson = input.queryJson?.ToObject<Dictionary<string, object>>();
            for (int i = 0; i < templateInfo.MainTableFieldsModelList.Count; i++)
            {
                var vmodel = templateInfo.MainTableFieldsModelList[i].__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty); // Field

                // 只显示要显示的列
                if (showColumnList && !templateInfo.ColumnData.columnList.Any(x => x.prop == templateInfo.MainTableFieldsModelList[i].__vModel__))
                    vmodel = string.Empty;

                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(templateInfo.MainTableFieldsModelList[i].__config__.tableName + "." + vmodel + " FIELD_" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD_" + i, templateInfo.MainTableFieldsModelList[i].__vModel__);

                    ListQuerySqlProjectionHelpers.RemapQueryInputsToFieldAlias(
                        input,
                        inputJson,
                        templateInfo.ColumnData.searchList,
                        templateInfo.MainTableFieldsModelList[i].__vModel__,
                        "FIELD_" + i,
                        remapSearchId: false);
                }
            }
			
			fields = fields.Distinct().ToList(); //modify by harry 过滤重复列

            sql = string.Format("select {0} from {1}", string.Join(",", fields), templateInfo.MainTableName);
            if (templateInfo.FormModel.logicalDelete && gateway.ColumnExists(templateInfo.MainTableName, "f_delete_mark"))
                sql += " where f_delete_mark is null "; // 处理软删除

            // 多租户字段隔离
            if (gateway.MultiTenancy)
            {
                var tenantCache = gateway.TenantCache;
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1) && gateway.ColumnExists(templateInfo.MainTableName, "f_tenant_id"))
                    sql += string.Format(" {0} f_tenant_id='{1}' ", sql.Contains("where") ? "and" : "where", tenantCache.connectionConfig.IsolationField);
            }

            // 是否只展示流程数据
            //if (templateInfo.visualDevEntity.EnableFlow.Equals(1)) sql += string.Format(" {0} f_flow_id <> '' ", sql.Contains("where") ? "and" : "where");
            //else sql += string.Format(" {0} f_flow_id is null or f_flow_id = '' ", sql.Contains("where") ? "and" : "where");

            // 拼接数据权限
            if (dataPermissions != null && dataPermissions.Any())
            {
                // 替换数据权限字段 别名
                var pvalue = ListQuerySqlFragmentHelpers.RewriteMainTablePermissionFieldNames(
                    dataPermissions.ToJsonStringOld(),
                    tableFieldKeyValue,
                    templateInfo.MainTableName);

                List<ICompileConditionalModel>? newPvalue = new List<ICompileConditionalModel>();
                if (pvalue.IsNotEmptyOrNull()) newPvalue = gateway.JsonToConditions(pvalue);

                sql = gateway.RenderSqlWhere(sql, newPvalue);
            }

        }
        else
        {
            #region 所有主、副表 字段名 和 处理查询、排序字段

            // 所有主、副表 字段名
            ListQuerySqlProjectionHelpers.SeedSystemProjectionFields(
                fields,
                tableFieldKeyValue,
                primaryKey,
                templateInfo.WebType.Equals(3),
                mainTablePrefix: templateInfo.MainTableName);
            Dictionary<string, object>? inputJson = input.queryJson?.ToObject<Dictionary<string, object>>();
            for (int i = 0; i < templateInfo.SingleFormData.Count; i++)
            {
                FieldsModel currField = templateInfo.SingleFormData[i];
                string? vmodel = currField.__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty); // Field

                //modify by harry
                //以下存在问题注释掉，如果列为过虑字段或者排序字段等，不一定会做为列表项目展示，但查询时缺少这些字段报异常
                //改进方法需要引用的列都加入
                // 只显示要显示的列
                //if (showColumnList && !templateInfo.ColumnData.columnList.Any(x => x.prop == currField.__vModel__)) 
                //{
                //    continue;
                //}
                //end

                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(currField.__config__.tableName + "." + vmodel + " FIELD_" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD_" + i, currField.__vModel__);

                    ListQuerySqlProjectionHelpers.RemapQueryInputsToFieldAlias(
                        input,
                        inputJson,
                        templateInfo.ColumnData.searchList,
                        currField.__vModel__,
                        "FIELD_" + i,
                        remapSearchId: true);
                }
            }

            #endregion

            #region 关联字段

            List<string>? auxiliaryFieldList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            List<string>? relationKey = ListQuerySqlProjectionHelpers.BuildAuxiliaryJoinPredicates(
                auxiliaryFieldList,
                templateInfo.AllTable,
                templateInfo.MainTableName);
            //modify by harry  同时检查主表与子表的软删除
            //if (templateInfo.FormModel.logicalDelete && gateway.ColumnExists(templateInfo.MainTableName, "f_delete_mark"))
            //    relationKey.Add(templateInfo.MainTableName + ".f_delete_mark is null "); // 处理软删除

            if (templateInfo.FormModel.logicalDelete ) 
            {                
                foreach (var item in templateInfo.AllTable)
                {
                    if( gateway.ColumnExists(item.table, "f_delete_mark")) 
                    {
                        relationKey.Add(item.table + ".f_delete_mark is null "); // 处理软删除
                    }
                }
            }
            //end modify 
			
			
            // 多租户字段隔离
            if (gateway.MultiTenancy)
            {
                var tenantCache = gateway.TenantCache;
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1) && gateway.ColumnExists(templateInfo.MainTableName, "f_tenant_id"))
                    relationKey.Add(string.Format(" {0}.f_tenant_id='{1}' ", templateInfo.MainTableName, tenantCache.connectionConfig.IsolationField));
            }

            // 是否只展示流程数据
            //if (templateInfo.visualDevEntity.EnableFlow.Equals(1)) relationKey.Add(templateInfo.MainTableName + ".f_flow_id <> '' ");
            //else relationKey.Add(templateInfo.MainTableName + ".f_flow_id is null or f_flow_id = '' ");

            string? whereStr = string.Join(" and ", relationKey);

            #endregion

            sql = string.Format("select {0} from {1} where {2}", string.Join(",", fields), templateInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList), whereStr); // 多表， 联合查询

            // 拼接数据权限
            if (dataPermissions != null && dataPermissions.Any())
            {
                // 替换数据权限字段 别名
                var pvalue = ListQuerySqlFragmentHelpers.RewriteJoinedPermissionFieldNames(
                    dataPermissions.ToJsonStringOld(),
                    tableFieldKeyValue,
                    templateInfo.AllTableFields,
                    templateInfo.MainTableName);

                List<ICompileConditionalModel>? newPvalue = new List<ICompileConditionalModel>();
                if (pvalue.IsNotEmptyOrNull()) newPvalue = gateway.JsonToConditions(pvalue);

                sql = gateway.RenderSqlWhere(sql, newPvalue);
            }
        }

        return sql;
    }

    public List<ICompileConditionalModel> GetIConditionalModelListByTableName(List<ICompileConditionalModel> cList, string tableName)
    {
        for (var i = 0; i < cList.Count; i++)
        {
            if (cList[i] is CompileConditionalTree newItem)
            {
                for (var j = 0; j < newItem.ConditionalList.Count; j++)
                {
                    var value = GetIConditionalModelListByTableName(new List<ICompileConditionalModel> { newItem.ConditionalList[j].Value }, tableName);
                    if (value != null && value.Any())
                    {
                        if (newItem.ConditionalList[j].Equals(newItem.ConditionalList.FirstOrDefault()))
                            newItem.ConditionalList[j] = new KeyValuePair<CompileWhereType, ICompileConditionalModel>(CompileWhereType.Null, value.First());
                        else
                            newItem.ConditionalList[j] = new KeyValuePair<CompileWhereType, ICompileConditionalModel>(newItem.ConditionalList[j].Key, value.First());
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
            else if (cList[i] is CompileConditionalModel model)
            {
                if (!model.FieldName.Contains(tableName))
                {
                    cList.RemoveAt(i);
                    i--;
                }
            }
        }

        return cList;
    }

    /// <summary>
    /// 组装单条信息查询sql.
    /// </summary>
    /// <param name="id">id.</param>
    /// <param name="mainPrimary">主键.</param>
    /// <param name="templateInfo">模板.</param>
    /// <param name="tableFieldKeyValue">联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个).</param>
    /// <returns></returns>
    public string GetInfoQuerySql(string id, string mainPrimary, TemplateParsingBase templateInfo, ref Dictionary<string, string> tableFieldKeyValue)
    {
        List<string> fields = new List<string>();
        string? sql = string.Empty; // 查询sql

        // 没有副表,只查询主表
        if (!templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            fields.Add(mainPrimary); // 主表主键
            if (templateInfo.WebType.Equals(3)) fields.Add("f_flow_id");
            templateInfo.MainTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item => fields.Add(item.__vModel__)); // 主表列名
            sql = string.Format("select {0} from {1} where {2}='{3}'", string.Join(",", fields), templateInfo.MainTableName, mainPrimary, id);
        }
        else
        {
            #region 所有主表、副表 字段名
            fields.Add(templateInfo.MainTableName + "." + mainPrimary); // 主表主键
            if (templateInfo.WebType.Equals(3)) fields.Add(templateInfo.MainTableName + ".f_flow_id");
            for (int i = 0; i < templateInfo.SingleFormData.Count; i++)
            {
                string? vmodel = templateInfo.SingleFormData[i].__vModel__.ReplaceRegex(@"(\w+)_jnpf_", ""); // Field
                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(templateInfo.SingleFormData[i].__config__.tableName + "." + vmodel + " FIELD" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD" + i, templateInfo.SingleFormData[i].__vModel__);
                }
            }
            #endregion

            #region 所有副表 关联字段
            List<string>? ctNameList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            List<string>? relationKey = new List<string>();
            relationKey.Add(string.Format(" {0}.{1}='{2}' ", templateInfo.MainTableName, mainPrimary, id)); // 主表ID
            ctNameList.ForEach(tName =>
            {
                var relTable = templateInfo.AllTable.Find(tf => tf.table == tName);
                string? tableField = relTable?.tableField;

                //原
                //relationKey.Add(string.Format(" {0}.{1}={2}.{3} ", templateInfo.MainTableName, mainPrimary, tName, tableField));
                //modify by harry
                relationKey.Add(string.Format(" {0}.{1}={2}.{3} ", templateInfo.MainTableName, relTable.relationField, tName, relTable.tableField));
            });

            string? whereStr = string.Join(" and ", relationKey);
            #endregion

            sql = string.Format("select {0} from {1} where {2}", string.Join(",", fields), templateInfo.MainTableName + "," + string.Join(",", ctNameList), whereStr); // 多表， 联合查询
        }

        return sql;
    }

    /// <summary>
    /// 组装 查询 json.
    /// </summary>
    /// <param name="queryJson"></param>
    /// <param name="columnDesign"></param>
    /// <param name="isInteAssisData">是否为集成助手数据</param>
    /// <returns></returns>
    public List<ICompileConditionalModel> GetQueryJson(RunSqlCompileGateway gateway, string queryJson, ColumnDesignModel columnDesign, int isInteAssisData = 0)
    {
        // 将查询的关键字json转成Dictionary
        Dictionary<string, object> keywordJsonDic = string.IsNullOrEmpty(queryJson) ? null : queryJson.ToObject<Dictionary<string, object>>();
        var conModels = new List<ICompileConditionalModel>();
        if (keywordJsonDic != null)
        {
            foreach (KeyValuePair<string, object> item in keywordJsonDic)
            {
                if (item.Key.Equals(JnpfKeyConst.JNPFKEYWORD) && columnDesign.searchList.Any(it => it.isKeyword))
                {
                    var con = new CompileConditionalCollections() { ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>() };
                    foreach (var model in columnDesign.searchList.FindAll(it => it.isKeyword))
                    {
                        var conditional = new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.Or, new CompileConditionalModel
                        {
                            FieldName = model.id,
                            ConditionalType = CompileConditionalType.Like,
                            FieldValue = item.Value.ToString()
                        });
                        con.ConditionalList.Add(conditional);
                    }

                    conModels.Add(con);
                }
                else
                {
                    var model = columnDesign.searchList.Find(it => it.id.Equals(item.Key));
                    if (model.IsNullOrEmpty())
                        model = columnDesign.searchList.Find(it => it.__vModel__.Equals(item.Key));

                    switch (model.__config__.jnpfKey)
                    {
                        case JnpfKeyConst.DATE:
                        case JnpfKeyConst.CREATETIME:
                        case JnpfKeyConst.MODIFYTIME:
                            {
                                var timeRange = item.Value.ToObject<List<string>>();
                                var startTime = timeRange.First().TimeStampToDateTime();
                                var endTime = timeRange.Last().TimeStampToDateTime();
                            // modify by harry  支持年 月 日
                            if (model.format.Equals("yyyy"))
                            {
                                startTime = new DateTime(startTime.Year, 1, 1, 0, 0, 0, 0);
                                endTime = new DateTime(endTime.Year, 1, 1, 0, 0, 0, 0);
                            }
                            else if (model.format.ToLower().Equals("yyyy-mm"))
                            {
                                startTime = new DateTime(startTime.Year, startTime.Month, 1, 0, 0, 0, 0);
                                endTime = endTime.AddMonths(1).AddTicks(-1);

                            }
                            else if (model.format.ToLower().Equals("yyyy-mm-dd"))
                            {
                                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0, 0);
                                endTime = endTime.AddDays(1).AddTicks(-1);
                            }
							//end modify

                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.GreaterThanOrEqual,
                                            FieldValue = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, 0).ToString(),
                                            CSharpTypeName = "datetime",
                                            FieldValueConvertFunc = it => Convert.ToDateTime(it)
                                        }),
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.LessThanOrEqual,
                                            FieldValue = new DateTime(endTime.Year, endTime.Month, endTime.Day, endTime.Hour, endTime.Minute, endTime.Second, 999).ToString(),
                                            CSharpTypeName = "datetime",
                                            FieldValueConvertFunc = it => Convert.ToDateTime(it)
                                        })
                                    }
                                });
                            }

                            break;
                        case JnpfKeyConst.TIME:
                            {
                                var timeRange = item.Value.ToObject<List<string>>();
                                var startTime = string.Format("{0:" + model.format + "}", Convert.ToDateTime(timeRange.First()));
                                var endTime = string.Format("{0:" + model.format + "}", Convert.ToDateTime(timeRange.Last()));
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                {
                                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                    {
                                        FieldName = item.Key,
                                        ConditionalType = CompileConditionalType.GreaterThanOrEqual,
                                        FieldValue = startTime
                                    }),
                                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                    {
                                        FieldName = item.Key,
                                        ConditionalType = CompileConditionalType.LessThanOrEqual,
                                        FieldValue = endTime
                                    })
                                }
                                });
                            }

                            break;
                        case JnpfKeyConst.NUMINPUT:
                        case JnpfKeyConst.CALCULATE:
                            {
                                List<string> numArray = item.Value.ToObject<List<string>>();
                                var startNum = numArray.First().ParseToDecimal();
                                var endNum = numArray.Last() == null ? decimal.MaxValue : numArray.Last().ParseToDecimal();
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                {
                                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                    {
                                        CSharpTypeName="decimal",
                                        FieldName = item.Key,
                                        ConditionalType = CompileConditionalType.GreaterThanOrEqual,
                                        FieldValue = startNum.ToString()
                                    }),
                                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                    {
                                        CSharpTypeName="decimal",
                                        FieldName = item.Key,
                                        ConditionalType = CompileConditionalType.LessThanOrEqual,
                                        FieldValue = endNum.ToString()
                                    })
                                }
                                });
                            }

                            break;
                        case JnpfKeyConst.CHECKBOX:
                            {
                                //if (model.searchType.Equals(1))
                                //    conModels.Add(new CompileConditionalModel { FieldName = item.Key, ConditionalType = CompileConditionalType.Equal, FieldValue = item.Value.ToString() });
                                //else
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                    {
                                        FieldName = item.Key,
                                        ConditionalType = CompileConditionalType.Like,
                                        FieldValue = item.Value.ToJsonString()
                                    })
                                    }
                                });
                            }

                            break;
                        case JnpfKeyConst.ROLESELECT:
                        case JnpfKeyConst.GROUPSELECT:
                        case JnpfKeyConst.POSSELECT:
                        case JnpfKeyConst.USERSELECT:
                        case JnpfKeyConst.DEPSELECT:
                            {
                                // 多选时为模糊查询
                                if (model.multiple || model.searchMultiple)
                                {
                                    var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<object>>() : new List<object>() { item.Value.ToString() };
                                    var addItems = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
                                    for (int i = 0; i < value.Count; i++)
                                    {
                                        var add = new KeyValuePair<CompileWhereType, CompileConditionalModel>(i == 0 ? CompileWhereType.And : CompileWhereType.Or, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = model.multiple ? CompileConditionalType.Like : CompileConditionalType.Equal,
                                            FieldValue = model.multiple ? value[i].ToJsonString() : value[i].ToString()
                                        });
                                        addItems.Add(add);
                                    }

                                    conModels.Add(new CompileConditionalCollections() { ConditionalList = addItems });
                                }
                                else
                                {
                                    var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<string>>().FirstOrDefault() : item.Value.ToString();
                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.Equal,
                                            FieldValue = value
                                        })
                                    }
                                    });
                                }
                            }

                            break;
                        case JnpfKeyConst.USERSSELECT:
                            {
                                if (item.Value != null)
                                {
                                    if (model.multiple || model.searchMultiple)
                                    {
                                        var objIdList = new List<string>();
                                        if (item.Value.ToString().Contains("[")) objIdList = item.Value.ToObject<List<string>>();
                                        else objIdList.Add(item.Value.ToString());
                                        var rIdList = gateway.ResolveUserRelations(objIdList);
                                        rIdList.ForEach(x =>
                                        {
                                            if (x.ObjectType.Equals("Organize"))
                                            {
                                                objIdList.Add(x.ObjectId + "--company");
                                                objIdList.Add(x.ObjectId + "--department");
                                            }
                                            else
                                            {
                                                objIdList.Add(x.ObjectId + "--" + x.ObjectType.ToLower());
                                            }
                                        });

                                        var whereList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
                                        for (var i = 0; i < objIdList.Count(); i++)
                                        {
                                            if (i == 0)
                                            {
                                                whereList.Add(new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = CompileConditionalType.Like,
                                                    FieldValue = objIdList[i]
                                                }));
                                            }
                                            else
                                            {
                                                whereList.Add(new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.Or, new CompileConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = CompileConditionalType.Like,
                                                    FieldValue = objIdList[i]
                                                }));
                                            }
                                        }

                                        conModels.Add(new CompileConditionalCollections() { ConditionalList = whereList });
                                    }
                                    else
                                    {
                                        conModels.Add(new CompileConditionalCollections()
                                        {
                                            ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                          {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Equal,
                                                FieldValue = item.Value.ToString()
                                            })
                                          }
                                        });
                                    }
                                }
                            }

                            break;
                        case JnpfKeyConst.TREESELECT:
                            {
                                if (item.Value.IsNotEmptyOrNull() && item.Value.ToString().Contains("["))
                                {
                                    var value = item.Value.ToObject<List<string>>();

                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.Like,
                                            FieldValue = value.LastOrDefault()
                                        })
                                    }
                                    });
                                }
                                else
                                {
                                    // 多选时为模糊查询
                                    if (model.multiple)
                                    {
                                        conModels.Add(new CompileConditionalCollections()
                                        {
                                            ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                          {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Like,
                                                FieldValue = item.Value.ToString()
                                            })
                                          }
                                        });
                                    }
                                    else
                                    {
                                        conModels.Add(new CompileConditionalCollections()
                                        {
                                            ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                          {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Equal,
                                                FieldValue = item.Value.ToString()
                                            })
                                          }
                                        });
                                    }
                                }
                            }

                            break;
                        case JnpfKeyConst.CURRORGANIZE:
                            {
                                var itemValue = item.Value.ToString().Contains("[") ? item.Value?.ToString().ToObject<List<string>>().ToJsonString() : item.Value.ToString();
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.Equal,
                                            FieldValue = itemValue
                                        })
                                    }
                                });
                            }

                            break;
                        case JnpfKeyConst.CASCADER:
                            {
                                var itemValue = item.Value.ToString().Contains("[") ? item.Value?.ToString().ToObject<List<string>>().ToJsonStringOld() : item.Value.ToString();
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                        {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Like,
                                                FieldValue = itemValue.Replace("[", string.Empty).Replace("]", string.Empty)
                                            })
                                        }
                                });
                            }
                            break;
                        case JnpfKeyConst.ADDRESS:
                        case JnpfKeyConst.COMSELECT:
                            {
                                // 多选时为模糊查询
                                if (model.multiple || model.searchMultiple)
                                {
                                    var value = item.Value?.ToString().ToObject<List<object>>();
                                    if (value.Any())
                                    {
                                        var addItems = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
                                        for (int i = 0; i < value.Count; i++)
                                        {
                                            var add = new KeyValuePair<CompileWhereType, CompileConditionalModel>(i == 0 ? CompileWhereType.And : CompileWhereType.Or, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Like,
                                                FieldValue = value[i].ToJsonStringOld().Contains('[') ? value[i].ToJsonStringOld().Replace("[", string.Empty) : item.Value?.ToString().Replace("[", string.Empty).Replace("\r\n", string.Empty).Replace(" ", string.Empty),
                                            });
                                            addItems.Add(add);
                                        }
                                        conModels.Add(new CompileConditionalCollections() { ConditionalList = addItems });
                                    }
                                }
                                else
                                {
                                    var itemValue = item.Value.ToString().Contains('[') ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                    if (itemValue.Contains("[[")) itemValue = itemValue.ToObject<List<List<object>>>().FirstOrDefault().ToJsonStringOld();
                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                        {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Equal,
                                                FieldValue = itemValue
                                            })
                                        }
                                    });
                                }
                            }

                            break;
                        case JnpfKeyConst.SELECT:
                            {
                                var itemValue = item.Value.ToString().Contains("[") ? item.Value.ToJsonString() : item.Value.ToString();

                                // 多选时为模糊查询
                                if (model.multiple || model.searchMultiple)
                                {
                                    var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<object>>() : new List<object>() { item.Value.ToString() };
                                    var addItems = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
                                    for (int i = 0; i < value.Count; i++)
                                    {
                                        var add = new KeyValuePair<CompileWhereType, CompileConditionalModel>(i == 0 ? CompileWhereType.And : CompileWhereType.Or, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = model.multiple ? CompileConditionalType.Like : CompileConditionalType.Equal,
                                            FieldValue = model.multiple ? value[i].ToJsonString() : value[i].ToString()
                                        });
                                        addItems.Add(add);
                                    }

                                    conModels.Add(new CompileConditionalCollections() { ConditionalList = addItems });
                                }
                                else
                                {
                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.Equal,
                                            FieldValue = itemValue
                                        })
                                    }
                                    });
                                }
                            }

                            break;
                        case JnpfKeyConst.RATE:
                        case JnpfKeyConst.SLIDER:
                            {
                                var rateRange = item.Value.ToObject<List<string>>();
                                conModels.Add(new CompileConditionalCollections()
                                {
                                    ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                    {
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.GreaterThanOrEqual,
                                            FieldValue = rateRange.First(),
                                            CSharpTypeName = "decimal"
                                        }),
                                        new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                        {
                                            FieldName = item.Key,
                                            ConditionalType = CompileConditionalType.LessThanOrEqual,
                                            FieldValue = rateRange.Last(),
                                            CSharpTypeName = "decimal"
                                        })
                                    }
                                });
                            }

                            break;
                        default:
                            {
                                var itemValue = item.Value.ToString().Contains("[") ? item.Value.ToJsonString() : item.Value.ToString();

                                if (model.searchType == 1)
                                {
                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                          {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Equal,
                                                FieldValue = itemValue
                                            })
                                          }
                                    });
                                }
                                else
                                {
                                    conModels.Add(new CompileConditionalCollections()
                                    {
                                        ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                                          {
                                            new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = CompileConditionalType.Like,
                                                FieldValue = itemValue
                                            })
                                          }
                                    });
                                }
                            }

                            break;
                    }
                }
            }
        }

        if (isInteAssisData.Equals(1))
        {
            conModels.Add(new CompileConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                {
                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                    {
                        FieldName = "f_inte_assistant",
                        ConditionalType = CompileConditionalType.Equal,
                        FieldValue = "1"
                    })
                }
            });
        }
        else
        {
            conModels.Add(new CompileConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>()
                {
                    new KeyValuePair<CompileWhereType, CompileConditionalModel>(CompileWhereType.And, new CompileConditionalModel
                    {
                        FieldName = "f_inte_assistant",
                        ConditionalType = CompileConditionalType.EqualNull
                    })
                }
            });
        }

        return conModels;
    }

    /// <summary>
    /// 组装高级查询条件.
    /// </summary>
    /// <param name="superQueryJson"></param>
    /// <returns></returns>
    public List<ICompileConditionalModel> GetSuperQueryJson(string superQueryJson, TemplateParsingBase tInfo)
    {
        List<ICompileConditionalModel> conModels = new List<ICompileConditionalModel>();
        if (superQueryJson.IsNotEmptyOrNull())
        {
            var querList = superQueryJson.ToObject<List<Dictionary<string, object>>>();
            var whereTypeList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
            foreach (var item in querList)
            {
                var whereType = new CompileWhereType();

                // 判断是否为新分组
                if (item.ContainsKey("where"))
                {
                    if (item.Equals(querList.First()))
                        item["where"] = "0";

                    if (whereTypeList.Count > 0)
                    {
                        conModels.Add(new CompileConditionalCollections() { ConditionalList = whereTypeList });
                        whereTypeList = new List<KeyValuePair<CompileWhereType, CompileConditionalModel>>();
                    }

                    whereType = item["where"].ToString().ToObject<CompileWhereType>();
                }
                else
                {
                    whereType = item["whereType"].ToString().ToObject<CompileWhereType>();
                }

                var conditionalType = item["ConditionalType"].ToString().ToObject<CompileConditionalType>();
                string _CSharpTypeName = item.ContainsKey("CSharpTypeName") ? item["CSharpTypeName"].ToString() : null;
                whereTypeList.Add(new KeyValuePair<CompileWhereType, CompileConditionalModel>(whereType, new CompileConditionalModel
                {
                    CSharpTypeName = _CSharpTypeName,
                    FieldName = item["field"].ToString(),
                    ConditionalType = conditionalType,
                    FieldValue = item["fieldValue"] == null ? null : item["fieldValue"].ToString()
                }));

                if (item.Equals(querList.Last()))
                    conModels.Add(new CompileConditionalCollections() { ConditionalList = whereTypeList });
            }
        }

        return conModels;
    }

}
