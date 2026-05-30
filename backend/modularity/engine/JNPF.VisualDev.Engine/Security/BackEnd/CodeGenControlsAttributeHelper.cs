using JNPF.Common.Const;
using JNPF.Extensions;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Extension;
using JNPF.Common.Models;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Enum;
using SqlSugar;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成控件属性帮助类.
/// </summary>
public class CodeGenControlsAttributeHelper
{
    /// <summary>
    /// 获取控件是否存储类型.
    /// </summary>
    /// <returns></returns>
    public static bool GetIsControlStoreType(string jnpfKey, int isStorage)
    {
        bool flag = true;

        switch (jnpfKey)
        {
            case JnpfKeyConst.POPUPATTR:
            case JnpfKeyConst.RELATIONFORMATTR:
                switch (isStorage)
                {
                    case 0:
                        flag = false;
                        break;
                }
                break;
        }
        return flag;
    }

    /// <summary>
    /// 转换静态数据.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<StaticDataModel> ConversionStaticData(string data)
    {
        var list = new List<StaticDataModel>();
        if (!string.IsNullOrEmpty(data))
        {
            var conData = data.ToObject<List<StaticDataModel>>();
            if (conData != null)
            {
                foreach (var item in conData)
                {
                    list.Add(new StaticDataModel()
                    {
                        id = item.id,
                        fullName = item.fullName,
                    });
                    if (item.children != null)
                        list.AddRange(ConversionStaticData(item.children.ToJsonString()));
                }
            }
        }

        return list;
    }

    /// <summary>
    /// 获取当前控件联动配置.
    /// </summary>
    /// <param name="fieldsModel">联动配置.</param>
    /// <param name="tableRelationship">0-主表,1-副表,2-子表.</param>
    /// <param name="tableName">表名称.</param>
    /// <returns></returns>
    public static List<DataInterfaceParameter> ObtainTheCurrentControlLinkageConfiguration(FieldsModel fieldsModel, int tableRelationship, string tableName = null)
    {
        var list = new List<DataInterfaceParameter>();
        var linkageConfigs = fieldsModel.templateJson == null ? fieldsModel.__config__.templateJson : fieldsModel.templateJson;
        if (linkageConfigs?.Count > 0)
        {
            linkageConfigs.ForEach(item =>
            {
                var relationField = string.Empty;
                var primaryAndSecondaryField = false;
                switch (tableRelationship)
                {
                    case 2:
                        relationField = item.relationField.Replace(string.Format("{0}-", tableName), string.Empty);
                        primaryAndSecondaryField = !item.relationField.Contains(tableName) ? true : false;
                        break;
                    default:
                        relationField = item.relationField;
                        break;
                }

                if (relationField.IsNotEmptyOrNull())
                {
                    switch(item.sourceType)
                    {
                        case 1:
                            relationField = relationField;
                            break;
                        case 2:
                            relationField = (item.dataType.Equals("int") ? relationField : "\"" + relationField + "\"");
                            break;
                        case 3:
                            break;
                        case 4:
                            break;
                    }
                }
                list.Add(new DataInterfaceParameter
                {
                    field = item.field,
                    relationField = relationField.IsNullOrEmpty() ? "string.Empty" : relationField,
                    isSubTable = primaryAndSecondaryField,
                    dataType = item.dataType,
                    defaultValue = item.defaultValue?.Replace("\"", "\\\""),
                    fieldName = item.fieldName,
                    sourceType = item.sourceType
                });
            });
        }
        return list;
    }

    /// <summary>
    /// 当前是否控件联动配置.
    /// </summary>
    /// <param name="fieldsModel">联动配置.</param>
    /// <returns></returns>
    public static bool IsControlLinkageConfiguration(FieldsModel fieldsModel)
    {
        var linkageConfigs = fieldsModel.templateJson == null ? fieldsModel.__config__.templateJson : fieldsModel.templateJson;
        return (linkageConfigs == null || !linkageConfigs.Any()) ? false : true;
    }

    /// <summary>
    /// 判断控件是否数据转换.
    /// </summary>
    /// <returns></returns>
    public static bool JudgeControlIsDataConversion(string jnpfKey, string dataType, bool multiple)
    {
        bool tag = false;
        switch (jnpfKey)
        {
            case JnpfKeyConst.LOCATION:
            case JnpfKeyConst.UPLOADFZ:
            case JnpfKeyConst.UPLOADIMG:
                tag = true;
                break;
            case JnpfKeyConst.SELECT:
            case JnpfKeyConst.TREESELECT:
                {
                    switch (dataType)
                    {
                        case "dictionary":
                            if (!multiple)
                            {
                                tag = false;
                            }
                            else
                            {
                                tag = true;
                            }

                            break;
                        default:
                            tag = true;
                            break;
                    }
                }

                break;
            case JnpfKeyConst.RADIO:
                {
                    switch (dataType)
                    {
                        case "dictionary":
                            tag = false;
                            break;
                        default:
                            tag = true;
                            break;
                    }
                }

                break;
            case JnpfKeyConst.DEPSELECT:
            case JnpfKeyConst.POSSELECT:
            case JnpfKeyConst.USERSELECT:
            case JnpfKeyConst.ROLESELECT:
            case JnpfKeyConst.GROUPSELECT:
                {
                    if (!multiple)
                        tag = false;
                    else
                        tag = true;
                }

                break;
            case JnpfKeyConst.CHECKBOX:
            case JnpfKeyConst.CASCADER:
            case JnpfKeyConst.COMSELECT:
            case JnpfKeyConst.POPUPTABLESELECT:
            case JnpfKeyConst.ADDRESS:
            case JnpfKeyConst.USERSSELECT:
                tag = true;
                break;
        }

        return tag;
    }

    /// <summary>
    /// 获取各模式控件是否列表转换.
    /// </summary>
    /// <param name="generatePattern">生成模式.</param>
    /// <param name="jnpfKey">控件Key.</param>
    /// <param name="dataType">数据类型 dictionary-数据字段,dynamic-远端数据.</param>
    /// <param name="multiple">是否多选.</param>
    /// <param name="subTableListDisplay">子表列表显示.</param>
    /// <returns>true-ThenMapper转换,false-列表转换.</returns>
    public static bool GetWhetherToConvertAllModeControlsIntoLists(GeneratePatterns generatePattern, string jnpfKey, string dataType, bool multiple, bool subTableListDisplay)
    {
        bool tag = false;

        /*
         * 因ORM原因 导航查询 一对多 列表查询
         * 不能使用ORM 自带函数 待作者开放.Select()
         * 导致一对多列表查询转换必须全使用子查询
         * 远端数据与静态数据无法列表转换所以全部ThenMapper内转换
         * 数据字典又分为两种值转换ID与EnCode
         */
        switch (subTableListDisplay)
        {
            case true:
                switch (generatePattern)
                {
                    case GeneratePatterns.MainBelt:
                    case GeneratePatterns.PrimarySecondary:
                        switch (jnpfKey)
                        {
                            case JnpfKeyConst.CREATEUSER:
                            case JnpfKeyConst.MODIFYUSER:
                            case JnpfKeyConst.CURRORGANIZE:
                            case JnpfKeyConst.CURRPOSITION:
                            case JnpfKeyConst.DEPSELECT:
                            case JnpfKeyConst.POSSELECT:
                            case JnpfKeyConst.USERSELECT:
                            case JnpfKeyConst.POPUPTABLESELECT:
                            case JnpfKeyConst.ROLESELECT:
                            case JnpfKeyConst.GROUPSELECT:
                            case JnpfKeyConst.RADIO:
                            case JnpfKeyConst.SELECT:
                            case JnpfKeyConst.TREESELECT:
                            case JnpfKeyConst.CHECKBOX:
                            case JnpfKeyConst.CASCADER:
                            case JnpfKeyConst.COMSELECT:
                            case JnpfKeyConst.ADDRESS:
                            case JnpfKeyConst.SWITCH:
                                tag = true;
                                break;
                        }

                        break;
                }
                break;
            default:
                switch (jnpfKey)
                {
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.TREESELECT:
                        {
                            switch (dataType)
                            {
                                case "dictionary":
                                    if (multiple)
                                        tag = true;
                                    else
                                        tag = false;
                                    break;
                                default:
                                    tag = true;
                                    break;
                            }
                        }

                        break;
                    case JnpfKeyConst.RADIO:
                        {
                            switch (dataType)
                            {
                                case "dictionary":
                                    tag = false;
                                    break;
                                default:
                                    tag = true;
                                    break;
                            }
                        }

                        break;
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.USERSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                        {
                            if (multiple)
                                tag = true;
                            else
                                tag = false;
                        }

                        break;
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.ADDRESS:
                    case JnpfKeyConst.POPUPTABLESELECT:
                        tag = true;
                        break;
                }
                break;
        }
        return tag;
    }

    /// <summary>
    /// 判断含子表字段控件是否数据转换.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <returns></returns>
    public static bool JudgeContainsChildTableControlIsDataConversion(string jnpfKey)
    {
        bool tag = false;
        switch (jnpfKey)
        {
            case JnpfKeyConst.LOCATION:
            case JnpfKeyConst.UPLOADFZ:
            case JnpfKeyConst.UPLOADIMG:
            case JnpfKeyConst.CREATEUSER:
            case JnpfKeyConst.MODIFYUSER:
            case JnpfKeyConst.CURRORGANIZE:
            case JnpfKeyConst.CURRPOSITION:
            case JnpfKeyConst.DEPSELECT:
            case JnpfKeyConst.POSSELECT:
            case JnpfKeyConst.USERSELECT:
            case JnpfKeyConst.USERSSELECT:
            case JnpfKeyConst.POPUPTABLESELECT:
            case JnpfKeyConst.ROLESELECT:
            case JnpfKeyConst.GROUPSELECT:
            case JnpfKeyConst.RADIO:
            case JnpfKeyConst.SELECT:
            case JnpfKeyConst.TREESELECT:
            case JnpfKeyConst.CHECKBOX:
            case JnpfKeyConst.CASCADER:
            case JnpfKeyConst.COMSELECT:
            case JnpfKeyConst.ADDRESS:
            case JnpfKeyConst.SWITCH:
            case JnpfKeyConst.DATE:
                tag = true;
                break;
        }

        return tag;
    }

    /// <summary>
    /// 系统控件不更新.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <returns></returns>
    public static bool JudgeControlIsSystemControls(string jnpfKey)
    {
        bool tag = true;
        switch (jnpfKey)
        {
            case JnpfKeyConst.CREATEUSER:
            case JnpfKeyConst.CREATETIME:
            case JnpfKeyConst.CURRPOSITION:
            case JnpfKeyConst.CURRORGANIZE:
            case JnpfKeyConst.BILLRULE:
                tag = false;
                break;
        }

        return tag;
    }

    /// <summary>
    /// 获取控件数据来源ID.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <param name="dataType">数据类型.</param>
    /// <param name="control">控件全属性.</param>
    /// <returns></returns>
    public static string GetControlsPropsUrl(string jnpfKey, string dataType, FieldsModel control)
    {
        string propsUrl = string.Empty;
        switch (jnpfKey)
        {
            case JnpfKeyConst.POPUPTABLESELECT:
                propsUrl = control.interfaceId;
                break;
            default:
                switch (dataType)
                {
                    case "dictionary":
                        propsUrl = control.__config__.dictionaryType;
                        break;
                    default:
                        propsUrl = control.__config__.propsUrl;
                        break;
                }

                break;
        }

        return propsUrl;
    }

    /// <summary>
    /// 获取控件指定选项的值.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <param name="dataType">数据类型.</param>
    /// <param name="control">控件全属性.</param>
    /// <returns></returns>
    public static string GetControlsLabel(string jnpfKey, string dataType, FieldsModel control)
    {
        string label = string.Empty;
        switch (jnpfKey)
        {
            case JnpfKeyConst.POPUPTABLESELECT:
                label = control.relationField;
                break;
            case JnpfKeyConst.CASCADER:
            case JnpfKeyConst.TREESELECT:
                label = control.props.label;
                break;
            default:
                label = control.props?.label;
                break;
        }

        return label;
    }

    /// <summary>
    /// 获取控件指定选项标签.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <param name="dataType">数据类型.</param>
    /// <param name="control">控件全属性.</param>
    /// <returns></returns>
    public static string GetControlsValue(string jnpfKey, string dataType, FieldsModel control)
    {
        string value = string.Empty;
        switch (jnpfKey)
        {
            case JnpfKeyConst.POPUPTABLESELECT:
                value = control.propsValue;
                break;
            case JnpfKeyConst.CASCADER:
            case JnpfKeyConst.TREESELECT:
                value = control.props.value;
                break;
            default:
                value = control.props?.value;
                break;
        }

        return value;
    }

    /// <summary>
    /// 获取控件指定选项的子选项.
    /// </summary>
    /// <param name="jnpfKey">控件Key.</param>
    /// <param name="dataType">数据类型.</param>
    /// <param name="control">控件全属性.</param>
    /// <returns></returns>
    public static string GetControlsChildren(string jnpfKey, string dataType, FieldsModel control)
    {
        string children = string.Empty;
        switch (jnpfKey)
        {
            case JnpfKeyConst.CASCADER:
            case JnpfKeyConst.TREESELECT:
                children = control.props.children;
                break;
            default:
                children = control.props?.children;
                break;
        }

        return children;
    }

    /// <summary>
    /// 获取导出配置.
    /// </summary>
    /// <param name="columnDesignModel">控件全.</param>
    /// <param name="control">控件全属性.</param>
    /// <param name="model">数据库真实字段.</param>
    /// <param name="tableName">表名称.</param>
    /// <returns></returns>
    public static CodeGenFieldsModel GetImportConfig(ColumnDesignModel columnDesignModel, FieldsModel control, string model, string tableName)
    {
        // 处理发杂表头
        if (columnDesignModel != null && columnDesignModel.complexHeaderList != null && columnDesignModel.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(control.__vModel__))))
        {
            var comlex = columnDesignModel.complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(control.__vModel__)));

            // 复杂表头格式 label 调整
            control.__config__.label = string.Format("{0}@@{1}@@{2}@@{3}", comlex.id, comlex.fullName, comlex.align, control.__config__.label);
        }

        var fieldModel = new CodeGenFieldsModel();
        var configModel = new CodeGenConfigModel();
        fieldModel.__vModel__ = model;
        fieldModel.level = control.level;
        fieldModel.min = control.min;
        fieldModel.max = control.max;
        fieldModel.activeTxt = control.activeTxt;
        fieldModel.inactiveTxt = control.inactiveTxt;
        fieldModel.format = control.format;
        fieldModel.multiple = CodeGenFieldJudgeHelper.IsMultipleColumn(control, model);
        fieldModel.separator = control.separator;
        fieldModel.props = control.props?.ToObject<CodeGenPropsBeanModel>()?.ToJsonString().ToJsonString();
        fieldModel.options = control.options?.ToObject<List<object>>()?.ToJsonString().ToJsonString();
        fieldModel.propsValue = control.propsValue;
        fieldModel.relationField = control.relationField;
        fieldModel.modelId = control.modelId;
        fieldModel.interfaceId = control.interfaceId;
        fieldModel.selectType = control.selectType;
        fieldModel.ableIds = control.ableIds?.ToJsonString().ToJsonString();
        if(control.templateJson != null && control.templateJson.Any() && control.__config__.templateJson == null)
            control.__config__.templateJson = control.templateJson;
        configModel = control.__config__.ToObject<CodeGenConfigModel>();
        configModel.tableName = tableName;
        configModel.showLevel = control.showLevel;
        fieldModel.__config__ = configModel.ToJsonString().ToJsonString();
        return fieldModel;
    }

    /// <summary>
    /// 获取需解析的字段集合.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="isInlineEditor"></param>
    /// <returns>jnpfKey @@ vmodel集合以 , 号隔开.</returns>
    public static List<string[]> GetParsJnpfKeyConstList(List<FieldsModel> control, bool isInlineEditor)
    {
        var res = new Dictionary<string, List<string>>();

        control.ForEach(item =>
        {
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.USERSSELECT: // 用户选择组件(包含组织、角色、岗位、分组、用户 Id)
                    if (!res.ContainsKey(JnpfKeyConst.USERSSELECT)) res.Add(JnpfKeyConst.USERSSELECT, new List<string>());
                    res[JnpfKeyConst.USERSSELECT].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.POPUPSELECT: // 弹窗选择
                    if (!res.ContainsKey(JnpfKeyConst.POPUPSELECT)) res.Add(JnpfKeyConst.POPUPSELECT, new List<string>());
                    res[JnpfKeyConst.POPUPSELECT].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.RELATIONFORM: // 关联表单
                    if (!res.ContainsKey(JnpfKeyConst.RELATIONFORM)) res.Add(JnpfKeyConst.RELATIONFORM, new List<string>());
                    res[JnpfKeyConst.RELATIONFORM].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.TABLE: // 遍历 子表 控件
                    var ctRes = GetParsJnpfKeyConstList(item.__config__.children, false);
                    if (ctRes != null && ctRes.Any())
                    {
                        foreach (var ct in ctRes)
                        {
                            if (!res.ContainsKey(ct.FirstOrDefault())) res.Add(ct.FirstOrDefault(), new List<string>());
                            var lasts = ct.LastOrDefault().Split(",");
                            foreach (var strItem in lasts) res[ct.FirstOrDefault()].Add(item.__vModel__ + "-" + strItem);
                        }
                    }
                    break;
            }
        });

        var ret = new List<string[]>();
        foreach (var item in res)
        {
            // 如果是行内编辑
            if (isInlineEditor)
            {
                var newValue = new List<string>();
                foreach (var it in item.Value) newValue.Add(it + "_name");
                res[item.Key] = newValue;
            }
        }
        foreach (var item in res)
        {
            ret.Add(new string[] { item.Key, string.Join(",", item.Value) });
        }
        return ret;
    }

    /// <summary>
    /// 获取需解析的字段集合.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="isInlineEditor"></param>
    /// <returns>jnpfKey @@ vmodel集合以 , 号隔开.</returns>
    public static List<string[]> GetParsJnpfKeyConstListDetails(List<FieldsModel> control)
    {
        var res = new Dictionary<string, List<string>>();

        control.ForEach(item =>
        {
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.USERSSELECT: // 用户选择组件(包含组织、角色、岗位、分组、用户 Id)
                    if (!res.ContainsKey(JnpfKeyConst.USERSSELECT)) res.Add(JnpfKeyConst.USERSSELECT, new List<string>());
                    res[JnpfKeyConst.USERSSELECT].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.POPUPSELECT: // 弹窗选择.
                    if (!res.ContainsKey(JnpfKeyConst.POPUPSELECT)) res.Add(JnpfKeyConst.POPUPSELECT, new List<string>());
                    res[JnpfKeyConst.POPUPSELECT].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.RELATIONFORM: // 关联表单.
                    if (!res.ContainsKey(JnpfKeyConst.RELATIONFORM)) res.Add(JnpfKeyConst.RELATIONFORM, new List<string>());
                    res[JnpfKeyConst.RELATIONFORM].Add(item.__vModel__);
                    break;
                case JnpfKeyConst.TABLE: // 遍历 子表 控件
                    var ctRes = GetParsJnpfKeyConstListDetails(item.__config__.children);
                    if (ctRes != null && ctRes.Any())
                    {
                        foreach (var ct in ctRes)
                        {
                            if (!res.ContainsKey(ct.FirstOrDefault())) res.Add(ct.FirstOrDefault(), new List<string>());
                            var lasts = ct.LastOrDefault().Split(",");
                            foreach (var strItem in lasts) res[ct.FirstOrDefault()].Add(item.__vModel__ + "-" + strItem);
                        }
                    }

                    break;
            }
        });

        var ret = new List<string[]>();
        foreach (var item in res)
        {
            ret.Add(new string[] { item.Key, string.Join(",", item.Value) });
        }

        return ret;
    }

    /// <summary>
    /// 获取模板配置的数据过滤.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="codeGenConfigModel"></param>
    /// <returns></returns>
    public static string GetDataRuleList(VisualDevEntity templateEntity, JNPF.Engine.Entity.Model.CodeGen.CodeGenConfigModel codeGenConfigModel)
    {
        var ruleList = new List<CodeGenDataRuleModuleResourceModel>();
        var tInfo = new TemplateParsingBase(templateEntity);

        // 数据过滤
        if (tInfo.ColumnData.ruleList != null && tInfo.ColumnData.ruleList.conditionList != null && tInfo.ColumnData.ruleList.conditionList.Any())
        {
            tInfo.ColumnData.ruleList.conditionList.ForEach(item =>
            {
                var addItems = GetItemRule(tInfo.ColumnData.ruleList.matchLogic, item, tInfo, "pc", codeGenConfigModel, ref ruleList);
                addItems.ForEach(addItem => { if (addItem.conditionalModel.Any()) ruleList.Add(addItem); });
            });
        }
        if (tInfo.AppColumnData.ruleListApp != null && tInfo.AppColumnData.ruleListApp.conditionList != null && tInfo.AppColumnData.ruleListApp.conditionList.Any())
        {
            tInfo.AppColumnData.ruleListApp.conditionList.ForEach(item =>
            {
                var addItems = GetItemRule(tInfo.AppColumnData.ruleListApp.matchLogic, item, tInfo, "app", codeGenConfigModel, ref ruleList);
                addItems.ForEach(addItem => { if (addItem.conditionalModel.Any()) ruleList.Add(addItem); });
            });
        }

        var res = new List<CodeGenDataRuleModuleResourceModel>();
        foreach (var userOriginItem in new List<string>() { "pc", "app" })
        {
            ruleList.Where(x => x.UserOrigin.Equals(userOriginItem)).Select(x => x.TableName).Distinct().ToList().ForEach(tName =>
            {
                var first = ruleList.FirstOrDefault(x => x.UserOrigin.Equals(userOriginItem) && x.TableName.Equals(tName));
                var condList = ruleList.Where(x => x.UserOrigin.Equals(userOriginItem) && x.TableName.Equals(tName)).ToList();
                var condTree = new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>() };
                var whereType = condList.Any() && condList.FirstOrDefault().WhereType.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
                condList.ForEach(cItems =>
                {
                    var condTreeItem = new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>() };
                    var cItemsWhereType = cItems.WhereType.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
                    var isNewGroupItem = true;
                    cItems.conditionalModel.ForEach(cItem =>
                    {
                        condTreeItem.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(isNewGroupItem ? whereType : cItemsWhereType, cItem));
                        if (isNewGroupItem) isNewGroupItem = false;
                    });
                    condTree.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(whereType, condTreeItem));
                });

                res.Add(new CodeGenDataRuleModuleResourceModel()
                {
                    FieldRule = first.TableName.Contains("@ChildFieldIsNull") ? -1 : first.FieldRule,
                    TableName = first.TableName.Replace("@ChildFieldIsNull", string.Empty),
                    UserOrigin = first.UserOrigin,
                    conditionalModelJson = new List<IConditionalModel>() { condTree }.ToJsonString()
                });
            });
        }

        return res.ToJsonString().Replace("\r\n", string.Empty).Replace("\\r\\n", string.Empty).Replace("\"", "\\\"").Replace("\\\\\"", "\\\\\\\"").Replace("\\\\\\\\\"", "\\\\\\\\\\\\\"");
    }

    private static List<CodeGenDataRuleModuleResourceModel> GetItemRule(string matchLogic, DataFilteringConditionListModel condModel, TemplateParsingBase tInfo, string userOrigin, JNPF.Engine.Entity.Model.CodeGen.CodeGenConfigModel codeGenConfigModel, ref List<CodeGenDataRuleModuleResourceModel> ruleList)
    {
        var resultList = new List<CodeGenDataRuleModuleResourceModel>();
        foreach (var item in condModel.groups)
        {
            item.field = item.field.Replace("--And", "");
            var result = new CodeGenDataRuleModuleResourceModel() { WhereType = matchLogic, FieldRule = 0, TableName = tInfo.MainTableName.ToLower(), UserOrigin = userOrigin, conditionalModel = new List<IConditionalModel>() };
            var vModel = item.field;
            if (tInfo.AuxiliaryTableFields.ContainsKey(vModel))
            {
                var tf = tInfo.AuxiliaryTableFields[vModel].Split('.');
                result.FieldRule = 1;
                result.TableName = tf.FirstOrDefault().ToLower();
                item.field = tf.LastOrDefault();
            }
            else if (tInfo.ChildTableFields.ContainsKey(vModel))
            {
                var tf = tInfo.ChildTableFields[vModel].Split('.');
                result.FieldRule = 2;
                result.TableName = tf.FirstOrDefault().ToLower();
                item.field = tf.LastOrDefault();

                if (item.symbol.Equals("null"))
                {
                    var mainTableRelationsQuery = result.Copy();
                    mainTableRelationsQuery.TableName = mainTableRelationsQuery.TableName + "@ChildFieldIsNull";
                    var ctPrimaryKey = codeGenConfigModel.TableRelations.Find(x => x.OriginalTableName.Equals(result.TableName)).ChilderColumnConfigList.Find(x => x.ColumnName.Equals(codeGenConfigModel.TableRelations.Find(x => x.OriginalTableName.Equals(result.TableName)).PrimaryKey)).OriginalColumnName;
                    var condTree = new ConditionalCollections()
                    {
                        ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel() { FieldName = ctPrimaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
                    };

                    mainTableRelationsQuery.conditionalModel = new List<IConditionalModel>() { condTree };
                    ruleList.Add(mainTableRelationsQuery);
                }
            }

            if (codeGenConfigModel.TableField.Any(x => x.LowerColumnName.Equals(item.field)))
            {
                var fieldList = codeGenConfigModel.TableField.Where(x => x.LowerColumnName.Equals(item.field)).ToList();
                if (fieldList.Any() && fieldList.Count.Equals(1)) item.field = fieldList.First().OriginalColumnName;
                else if (fieldList.Any(x => x.TableName != null && x.TableName.Equals(item.__config__.tableName))) item.field = fieldList.Find(x => x.TableName != null && x.TableName.Equals(item.__config__.tableName))?.OriginalColumnName;
                else if (codeGenConfigModel.TableRelations.Any(x => x.OriginalTableName.Equals(result.TableName)))
                {
                    var tableRelations = codeGenConfigModel.TableRelations.Find(x => x.OriginalTableName.Equals(result.TableName) && x.ChilderColumnConfigList.Any(xx => xx.LowerColumnName.Equals(item.field)));
                    if (tableRelations != null) item.field = tableRelations.ChilderColumnConfigList.Find(x => x.LowerColumnName.Equals(item.field)).OriginalColumnName;
                }
            }
            else if (codeGenConfigModel.TableRelations.Any(x => x.OriginalTableName.Equals(result.TableName)))
            {
                var tableRelations = codeGenConfigModel.TableRelations.Find(x => x.OriginalTableName.Equals(result.TableName) && x.ChilderColumnConfigList.Any(xx => xx.LowerColumnName.Equals(item.field)));
                if (tableRelations != null) item.field = tableRelations.ChilderColumnConfigList.Find(x => x.LowerColumnName.Equals(item.field)).OriginalColumnName;
            }

            var conditionalType = ConditionalType.Equal;
            var between = new List<string>();
            if (item.fieldValue.IsNotEmptyOrNull())
            {
                if (item.symbol.Equals("between")) between = item.fieldValue.ToObject<List<string>>();
                switch (item.jnpfKey)
                {
                    case JnpfKeyConst.CURRORGANIZE:
                        if (item.fieldValue.IsNotEmptyOrNull() && item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[[")) item.fieldValue = item.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList();
                        else if (item.fieldValue.IsNotEmptyOrNull() && item.fieldValue.ToJsonString().Contains("[")) item.fieldValue = item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                        break;
                    case JnpfKeyConst.COMSELECT:
                        if (item.fieldValue.IsNotEmptyOrNull() && item.symbol.Equals("==") || item.symbol.Equals("<>"))
                        {
                            item.fieldValue = item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                        }
                        else
                        {
                            if (item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[[")) item.fieldValue = item.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList();
                            else if (item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[") && item.fieldValue.ToObject<List<string>>().Any(x => x.Contains("[")))
                                item.fieldValue = item.fieldValue.ToObject<List<string>>().Select(x => x + "\"]").ToList();
                        }
                        break;
                    case JnpfKeyConst.CREATETIME:
                    case JnpfKeyConst.MODIFYTIME:
                    case JnpfKeyConst.DATE:
                        {
                            if (item.symbol.Equals("between"))
                            {
                                var startTime = between.First().TimeStampToDateTime();
                                var endTime = between.Last().TimeStampToDateTime();
                                between[0] = startTime.ToString();
                                between[1] = endTime.ToString();
                            }
                            else
                            {
                                if (item.fieldValue is DateTime)
                                {
                                    item.fieldValue = item.fieldValue.ToString();
                                }
                                else if (item.fieldValue is long)
                                {
                                    item.fieldValue = item.fieldValue.ToString().TimeStampToDateTime();
                                }
                            }
                        }
                        break;
                    case JnpfKeyConst.TIME:
                        {
                            if (!item.symbol.Equals("between"))
                            {
                                item.fieldValue = string.Format("{0:" + item.format + "}", Convert.ToDateTime(item.fieldValue));
                            }
                        }
                        break;
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.ADDRESS:
                        if (item.fieldValue.IsNotEmptyOrNull() && (item.symbol.Equals("==") || item.symbol.Equals("<>")))
                            item.fieldValue = item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                        break;

                }
            }

            switch (item.symbol)
            {
                case ">=":
                    conditionalType = ConditionalType.GreaterThanOrEqual;
                    break;
                case ">":
                    conditionalType = ConditionalType.GreaterThan;
                    break;
                case "==":
                    conditionalType = ConditionalType.Equal;
                    break;
                case "<=":
                    conditionalType = ConditionalType.LessThanOrEqual;
                    break;
                case "<":
                    conditionalType = ConditionalType.LessThan;
                    break;
                case "<>":
                    conditionalType = ConditionalType.NoEqual;
                    break;
                case "like":
                    if (item.fieldValue != null && item.fieldValue.ToString().Contains("[")) item.fieldValue = item.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                    conditionalType = ConditionalType.Like;
                    break;
                case "notLike":
                    if (item.fieldValue != null && item.fieldValue.ToString().Contains("[")) item.fieldValue = item.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                    conditionalType = ConditionalType.NoLike;
                    break;
                case "in":
                case "notIn":
                    if (item.fieldValue != null && item.fieldValue.ToString().Contains("["))
                    {
                        var isListValue = false;
                        var itemField = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(vModel));
                        if (itemField != null && (itemField.multiple || item.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) || item.jnpfKey.Equals(JnpfKeyConst.CASCADER) || item.jnpfKey.Equals(JnpfKeyConst.ADDRESS)))
                            isListValue = true;
                        if (item.jnpfKey.Equals(JnpfKeyConst.COMSELECT)) isListValue = false;
                        var conditionalList = new ConditionalCollections() { ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>() };
                        var ids = new List<string>();
                        if (item.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[[")) ids = item.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
                        else ids = item.fieldValue.ToObject<List<string>>();
                        for (var i = 0; i < ids.Count; i++)
                        {
                            var it = ids[i];
                            var whereType = WhereType.And;
                            if (item.symbol.Equals("in")) whereType = i.Equals(0) && condModel.logic.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
                            else whereType = i.Equals(0) && condModel.logic.ToUpper().Equals("OR") ? WhereType.Or : WhereType.And;
                            conditionalList.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(whereType, new ConditionalModel
                            {
                                FieldName = item.field,
                                ConditionalType = item.symbol.Equals("in") ? (item.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.Equal : ConditionalType.Like) : (item.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.NoEqual : ConditionalType.NoLike),
                                FieldValue = isListValue ? it.ToJsonString() : it
                            }));
                        }

                        if (item.symbol.Equals("notIn"))
                        {
                            conditionalList.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                            {
                                FieldName = item.field,
                                ConditionalType = ConditionalType.IsNot,
                                FieldValue = string.Empty
                            }));
                            conditionalList.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                            {
                                FieldName = item.field,
                                ConditionalType = ConditionalType.IsNot,
                                FieldValue = string.Empty
                            }));
                        }

                        result.conditionalModel.Add(conditionalList);
                        resultList.Add(result);
                        continue;
                    }
                    conditionalType = item.symbol.Equals("in") ? ConditionalType.In : ConditionalType.NotIn;
                    break;
                case "null":
                    conditionalType = (item.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || item.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty;
                    break;
                case "notNull":
                    conditionalType = ConditionalType.IsNot;
                    if (item.fieldValue.IsNullOrEmpty())
                        item.fieldValue = null;
                    break;
                case "between":
                    var condItem = new ConditionalCollections()
                    {
                        ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>((condModel.logic.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or), new ConditionalModel
                        {
                            FieldName = item.field,
                            ConditionalType = ConditionalType.GreaterThanOrEqual,
                            FieldValue = between.First(),
                            FieldValueConvertFunc = it => Convert.ToDateTime(it)
                        }),
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                        {
                            FieldName = item.field,
                            ConditionalType = ConditionalType.LessThanOrEqual,
                            FieldValue = between.Last(),
                            FieldValueConvertFunc = it => Convert.ToDateTime(it)
                        })
                    }
                    };

                    result.conditionalModel.Add(condItem);
                    resultList.Add(result);
                    continue;
            }

            var resItem = new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
            {
                new KeyValuePair<WhereType, ConditionalModel>((condModel.logic.ToUpper().Equals("AND") ? WhereType.And : WhereType.Or), new ConditionalModel
                {
                    FieldName = item.field,
                    ConditionalType = conditionalType,
                    FieldValue = item.fieldValue == null ? string.Empty : item.fieldValue.ToString()
                })
            }
            };
            result.conditionalModel.Add(resItem);
            resultList.Add(result);
        }
        return resultList;
    }
}