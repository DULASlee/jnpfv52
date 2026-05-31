using JNPF.Common.Const;
using JNPF.Common.Enums;
using JNPF.Extensions;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Engine.Security;
using JNPF.VisualDev.Entitys;
using Mapster;
using SqlSugar;

namespace JNPF.VisualDev.Engine.CodeGen;

/// <summary>
/// 代码生成方式.
/// </summary>
public class CodeGenWay
{
    /// <summary>
    /// 副表表字段配置.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="tableNo">表序号.</param>
    /// <param name="modelType">0-主带副,1-主带子副.</param>
    /// <returns></returns>
    public static CodeGenConfigModel AuxiliaryTableBackEnd(string? tableName, List<DbTableFieldModel> dbTableFields, List<FieldsModel> controls, VisualDevEntity templateEntity, int tableNo, int modelType, string tableField)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        var tableColumnList = new List<TableColumnConfigModel>();
        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsConversion = false,
                        IsSystemControl = false,
                        IsAuxiliary = true,
                        IsControlParsing = false,
                        IsUpdate = false,
                        TableNo = tableNo,
                        TableName = tableName,
                    });
                    break;
                default:
                    var childControl = string.Format("jnpf_{0}_jnpf_{1}", tableName, field);
                    switch (controls.Any(c => c.__vModel__.Equals(childControl)))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__.Equals(childControl));
                            var isImportField = templateEntity.WebType == 1 ? false : columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(childControl));
                            switch (control.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    var isConversion = modelType == 1 ? CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey) : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, childControl));
                                    if (control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE)) isConversion = true;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, childControl),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, childControl),
                                        IsShow = control.isIndexShow,
                                        IsUnique = control.__config__.unique,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsSecondaryTableDateTime(control),
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        IsDetailConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) ? true : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, childControl)),
                                        IsConversion = isConversion,
                                        IsSystemControl = true,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = true,
                                        TableNo = tableNo,
                                        TableName = tableName,
                                        FormatTableName = tableName.ParseToPascalCase(),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        IsControlParsing = false,
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowLevel = control.showLevel,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = childControl.Equals(columnDesignModel.parentField),
                                    });
                                    break;
                                default:
                                    var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = CodeGenFieldJudgeHelper.IsColumnQueryWhether(searchList: columnDesignModel.searchList, childControl),
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, childControl),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, childControl),
                                        IsShow = CodeGenFieldJudgeHelper.IsShowColumn(columnDesignModel.columnList, childControl),
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, childControl),
                                        IsUnique = control.__config__.unique,
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsSecondaryTableDateTime(control),
                                        Format = control.format,
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        ControlsDataType = dataType,
                                        StaticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || control.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()),
                                        propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                        Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                        Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                        Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                        Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                        IsDetailConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, childControl)),
                                        IsConversion = modelType == 1 ? CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey) : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, childControl)),
                                        IsSystemControl = false,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = true,
                                        TableNo = tableNo,
                                        TableName = tableName,
                                        FormatTableName = tableName.ParseToPascalCase(),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        IsControlParsing = CodeGenFieldJudgeHelper.IsControlParsing(control),
                                        ShowField = control.relational,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = childControl.Equals(columnDesignModel.parentField),
                                        isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                        IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                        LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 1),
                                    });
                                    break;
                            }
                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                TableName = tableName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = false,
                                ForeignKeyField = tableField.ToLower().Equals(field.ToLower()) ? true : false,
                                IsImportField = false,
                                IsSystemControl = false,
                                IsAuxiliary = true,
                                IsUpdate = false,
                                TableNo = tableNo,
                                IsControlParsing = false,
                            });
                            break;
                    }

                    break;
            }
        }

        // 条形码关联字段
        var relationField = new List<TableColumnConfigModel>();
        foreach (var item in controls.FindAll(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.BARCODE) && x.dataType.Equals("relation")))
        {
            if (item.relationField.Contains("_jnpf_"))
            {
                var rField = item.relationField.Split("jnpf_").ToList();
                rField.RemoveAt(0);
                var tt = rField.FirstOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                var ff = rField.LastOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                relationField.Add(new TableColumnConfigModel() { ColumnName = string.Format("{0}.{1}", tt, ff), RelationColumnName = item.relationField + "_id" });
            }
            else
            {
                relationField.Add(new TableColumnConfigModel() { ColumnName = item.relationField.ParseToPascalCase(), RelationColumnName = item.relationField + "_id" });
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey))
        {
            throw Oops.Oh(ErrorCode.D2104);
        }

        // 是否存在上传控件.
        bool isUpload = tableColumnList.Any(it => it.PrimaryKey.Equals(false) && it.ForeignKeyField.Equals(false) && it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG) || it.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ)));

        // 是否对象映射
        bool isMapper = CodeGenFieldJudgeHelper.IsChildTableMapper(tableColumnList);

        // 是否查询条件多选
        bool isSearchMultiple = tableColumnList.Any(it => it.QueryMultiple);

        bool isLogicalDelete = formDataModel.logicalDelete;

        return new CodeGenConfigModel()
        {
            NameSpace = formDataModel.areasName,
            BusName = templateEntity.FullName,
            ClassName = formDataModel.className.FirstOrDefault(),
            PrimaryKey = tableColumnList.Find(t => t.PrimaryKey).ColumnName,
            OriginalPrimaryKey = tableColumnList.Find(t => t.PrimaryKey).OriginalColumnName,
            MainTable = tableName.ParseToPascalCase(),
            OriginalMainTableName = tableName,
            TableField = tableColumnList,
            RelationsField = relationField,
            IsUpload = isUpload,
            IsMapper = isMapper,
            WebType = templateEntity.WebType,
            Type = templateEntity.Type,
            PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
            IsImportData = tableColumnList.Any(it => it.IsImportField.Equals(true)),
            EnableFlow = templateEntity.EnableFlow == 1 ? true : false,
            IsSearchMultiple = isSearchMultiple,
            IsLogicalDelete = isLogicalDelete,
        };
    }

    /// <summary>
    /// 子表表字段配置.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="className">功能名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="controlId">子表控件vModel.</param>
    /// <param name="tableField">关联字段.</param>
    /// <returns></returns>
    public static CodeGenConfigModel ChildTableBackEnd(string tableName, string className, List<DbTableFieldModel> dbTableFields, List<FieldsModel> controls, VisualDevEntity templateEntity, string controlId, string tableField)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        var tableColumnList = new List<TableColumnConfigModel>();
        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsControlParsing = false,
                    });
                    break;
                default:
                    switch (controls.Any(c => c.__vModel__ == field))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__ == field);
                            var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                            var isImportField = templateEntity.WebType == 1 ? false : columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(string.Format("{0}-{1}", controlId, field)));
                            var staticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString());
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = column.primaryKey.ParseToBool(),
                                QueryWhether = control.isQueryField,
                                QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, string.Format("{0}-{1}", controlId, field)),
                                QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, string.Format("{0}-{1}", controlId, field)),
                                IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                IsUnique = control.__config__.unique,
                                jnpfKey = control.__config__.jnpfKey,
                                Rule = control.__config__.rule,
                                IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                ActiveTxt = control.activeTxt,
                                InactiveTxt = control.inactiveTxt,
                                IsShow = control.isIndexShow,
                                ControlsDataType = dataType,
                                StaticData = staticData,
                                Format = control.format,
                                propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                IsConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                IsDetailConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                ControlLabel = control.__config__.label,
                                ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(null, control, column.field, tableName),
                                IsImportField = isImportField.ParseToBool(),
                                ChildControlKey = controlId,
                                ShowField = control.relational,
                                ShowAllLevels = control.showAllLevels,
                                IsControlParsing = CodeGenFieldJudgeHelper.IsControlParsing(control),
                                isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 2, controlId),
                            });
                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                ForeignKeyField = tableField.ToLower().Equals(field.ToLower()) ? true : false,
                                IsImportField = false,
                                IsControlParsing = false,
                            });
                            break;
                    }
                    break;
            }
        }

        // 条形码关联字段
        var relationField = new List<TableColumnConfigModel>();
        foreach (var item in controls.FindAll(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.BARCODE) && x.dataType.Equals("relation")))
        {
            if (item.relationField.Contains("_jnpf_"))
            {
                var rField = item.relationField.Split("jnpf_").ToList();
                rField.RemoveAt(0);
                var tt = rField.FirstOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                var ff = rField.LastOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                relationField.Add(new TableColumnConfigModel() { ColumnName = string.Format("{0}.{1}", tt, ff), RelationColumnName = item.relationField + "_id" });
            }
            else
            {
                relationField.Add(new TableColumnConfigModel() { ColumnName = item.relationField.ParseToPascalCase(), RelationColumnName = item.relationField + "_id" });
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey))
        {
            throw Oops.Oh(ErrorCode.D2104, tableName);
        }

        // 是否存在上传控件.
        bool isUpload = tableColumnList.Any(it => it.PrimaryKey.Equals(false) && it.ForeignKeyField.Equals(false) && it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG) || it.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ)));

        // 是否对象映射
        bool isMapper = CodeGenFieldJudgeHelper.IsChildTableMapper(tableColumnList);

        bool isShowSubTableField = tableColumnList.Any(it => it.IsShow.Equals(true));

        // 是否查询条件多选
        bool isSearchMultiple = tableColumnList.Any(it => it.QueryMultiple);

        bool isLogicalDelete = formDataModel.logicalDelete;

        return new CodeGenConfigModel()
        {
            NameSpace = formDataModel.areasName,
            BusName = templateEntity.FullName,
            ClassName = className,
            PrimaryKey = tableColumnList.Find(t => t.PrimaryKey).ColumnName,
            OriginalPrimaryKey = tableColumnList.Find(t => t.PrimaryKey).OriginalColumnName,
            TableField = tableColumnList,
            RelationsField = relationField,
            IsUpload = isUpload,
            IsMapper = isMapper,
            WebType = templateEntity.WebType,
            Type = templateEntity.Type,
            PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
            IsImportData = tableColumnList.Any(it => it.IsImportField.Equals(true)),
            IsShowSubTableField = isShowSubTableField,
            IsSearchMultiple = isSearchMultiple,
            IsLogicalDelete = isLogicalDelete,
            TableType = columnDesignModel.type,
        };
    }

    /// <summary>
    /// 主表带子表.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenConfigModel MainBeltBackEnd(string? tableName, List<DbTableFieldModel> dbTableFields, List<FieldsModel> controls, VisualDevEntity templateEntity)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        var table = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();

        var tableColumnList = new List<TableColumnConfigModel>();
        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsConversion = false,
                        IsSystemControl = false,
                        IsUpdate = false,
                    });
                    break;
                default:
                    switch (controls.Any(c => c.__vModel__ == field))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__ == field);
                            var childControl = string.Empty;
                            var isImportField = templateEntity.WebType == 1 ? false : columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field));

                            switch (control.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsUnique = control.__config__.unique,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                        IsDetailConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) ? true : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = true,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField == null ? false : (bool)isImportField,
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowLevel = control.showLevel,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = childControl.Equals(columnDesignModel.parentField),
                                    });
                                    break;
                                default:
                                    var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        IsUnique = control.__config__.unique,
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        Format = control.format,
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        ControlsDataType = dataType,
                                        StaticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || control.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()),
                                        propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                        Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                        Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                        Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                        Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                        IsDetailConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = false,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField == null ? false : (bool)isImportField,
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowField = control.relational,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = childControl.Equals(columnDesignModel.parentField),
                                        isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                        IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                        LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 0),
                                    });
                                    break;
                            }
                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = false,
                                IsConversion = false,
                                IsSystemControl = false,
                                ForeignKeyField = true,
                                IsUpdate = false,
                                IsControlParsing = false,
                            });
                            break;
                    }
                    break;
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey)) throw Oops.Oh(ErrorCode.D2104);

        return GetCodeGenConfigModel(formDataModel, columnDesignModel, tableColumnList, controls, tableName, templateEntity);
    }

    /// <summary>
    /// 主表带副表.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="auxiliaryTableColumnList">副表字段配置.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenConfigModel MainBeltViceBackEnd(string? tableName, List<DbTableFieldModel> dbTableFields, List<TableColumnConfigModel> auxiliaryTableColumnList, List<FieldsModel> controls, VisualDevEntity templateEntity)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        var tableColumnList = new List<TableColumnConfigModel>();
        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsConversion = false,
                        IsSystemControl = false,
                        IsAuxiliary = false,
                        IsUpdate = false,
                    });
                    break;
                default:
                    switch (controls.Any(c => c.__vModel__ == field))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__ == field);
                            var isImportField = templateEntity.WebType == 1 ? false : columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field));
                            switch (control.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    var isConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field));
                                    if (control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE)) isConversion = true;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsUnique = control.__config__.unique,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        IsConversion = isConversion,
                                        IsDetailConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) ? true : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = true,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = false,
                                        TableName = tableName,
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowLevel = control.showLevel,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                    });
                                    break;
                                default:
                                    var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        IsUnique = control.__config__.unique,
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        Format = control.format,
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        ControlsDataType = dataType,
                                        StaticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || control.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()),
                                        propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                        Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                        Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                        Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                        Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsDetailConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = false,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = false,
                                        TableName = tableName,
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                        isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                        IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                        LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 0),
                                    });
                                    break;
                            }
                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = false,
                                IsConversion = false,
                                IsSystemControl = false,
                                IsAuxiliary = false,
                                IsUpdate = false,
                                IsControlParsing = false,
                            });
                            break;
                    }

                    break;
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey))
        {
            throw Oops.Oh(ErrorCode.D2104);
        }

        tableColumnList.AddRange(auxiliaryTableColumnList);

        return GetCodeGenConfigModel(formDataModel, columnDesignModel, tableColumnList, controls, tableName, templateEntity);
    }

    /// <summary>
    /// 主表带子副表.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="auxiliaryTableColumnList">副表字段配置.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenConfigModel PrimarySecondaryBackEnd(string? tableName, List<DbTableFieldModel> dbTableFields, List<TableColumnConfigModel> auxiliaryTableColumnList, List<FieldsModel> controls, VisualDevEntity templateEntity)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        var tableColumnList = new List<TableColumnConfigModel>();

        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsAuxiliary = false,
                        IsUpdate = false,
                    });
                    break;
                default:
                    switch (controls.Any(c => c.__vModel__.Equals(field)))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__ == field);
                            switch (control.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsUnique = control.__config__.unique,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                        IsDetailConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) ? true : CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, "", CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = true,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = false,
                                        TableName = tableName,
                                        ControlLabel = control.__config__.label,
                                        IsImportField = columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field)) == null ? false : (bool)columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field)),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowLevel = control.showLevel,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                    });
                                    break;
                                default:
                                    var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        IsUnique = control.__config__.unique,
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        Format = control.format,
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        ControlsDataType = dataType,
                                        StaticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || control.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()),
                                        propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                        Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                        Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                        Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                        Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion(control.__config__.jnpfKey),
                                        IsDetailConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = false,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        IsAuxiliary = false,
                                        TableName = tableName,
                                        ControlLabel = control.__config__.label,
                                        IsImportField = columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field)) == null ? false : (bool)columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field)),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                        ShowField = control.relational,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                        IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                        LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 0),
                                    });
                                    break;
                            }
                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = false,
                                IsAuxiliary = false,
                                IsUpdate = false,
                            });
                            break;
                    }
                    break;
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey)) throw Oops.Oh(ErrorCode.D2104);

        tableColumnList.AddRange(auxiliaryTableColumnList);

        return GetCodeGenConfigModel(formDataModel, columnDesignModel, tableColumnList, controls, tableName, templateEntity);
    }

    /// <summary>
    /// 单表后端.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="dbTableFields">表字段.</param>
    /// <param name="controls">控件列表.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenConfigModel SingleTableBackEnd(string? tableName, List<DbTableFieldModel> dbTableFields, List<FieldsModel> controls, VisualDevEntity templateEntity)
    {
        // 表单数据
        ColumnDesignModel columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        columnDesignModel ??= new ColumnDesignModel();
        columnDesignModel.searchList = GetMultiEndQueryMerging(templateEntity, controls);
        columnDesignModel.columnList = GetMultiTerminalListDisplayAndConsolidation(templateEntity);
        FormDataModel formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();
        var tableColumnList = new List<TableColumnConfigModel>();

        // 移除乐观锁
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("version"));

        // 移除真实流程ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_task_id"));

        // 移除流程引擎ID
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("flow_id"));

        // 移除逻辑删除
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_mark"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_time"));
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("delete_user_id"));

        // 多租户隔离字段
        dbTableFields.RemoveAll(it => it.field.ReplaceRegex("^f_", string.Empty).ToLower().Equals("tenant_id"));

        foreach (DbTableFieldModel? column in dbTableFields)
        {
            var field = column.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            switch (column.primaryKey)
            {
                case true:
                    tableColumnList.Add(new TableColumnConfigModel()
                    {
                        ColumnName = field.ToUpperCase(),
                        OriginalColumnName = column.field,
                        ColumnComment = column.fieldName,
                        DataType = column.dataType,
                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                        PrimaryKey = true,
                        IsConversion = false,
                        IsSystemControl = false,
                        IsUpdate = false,
                    });
                    break;
                default:
                    // 存在表单内控件
                    switch (controls.Any(c => c.__vModel__ == field))
                    {
                        case true:
                            FieldsModel control = controls.Find(c => c.__vModel__ == field);
                            bool? isImportField = templateEntity.WebType == 1 ? false : columnDesignModel?.uploaderTemplateJson?.selectKey?.Any(it => it.Equals(field));
                            switch (control.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsUnique = control.__config__.unique,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        IsConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE),
                                        IsDetailConversion = control.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE),
                                        IsSystemControl = true,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowLevel = control.showLevel,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                    });
                                    break;
                                default:
                                    var dataType = control.__config__.dataType != null ? control.__config__.dataType : null;
                                    tableColumnList.Add(new TableColumnConfigModel()
                                    {
                                        ColumnName = field.ToUpperCase(),
                                        OriginalColumnName = column.field,
                                        ColumnComment = column.fieldName,
                                        DataType = column.dataType,
                                        NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                        PrimaryKey = column.primaryKey.ParseToBool(),
                                        QueryWhether = control.isQueryField,
                                        QueryType = CodeGenFieldJudgeHelper.ColumnQueryType(searchList: columnDesignModel.searchList, field),
                                        QueryMultiple = CodeGenFieldJudgeHelper.ColumnQueryMultiple(searchList: columnDesignModel.searchList, field),
                                        IsShow = control.isIndexShow,
                                        IsMultiple = CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field),
                                        IsUnique = control.__config__.unique,
                                        jnpfKey = control.__config__.jnpfKey,
                                        Rule = control.__config__.rule,
                                        IsDateTime = CodeGenFieldJudgeHelper.IsDateTime(control),
                                        Format = control.format,
                                        ActiveTxt = control.activeTxt,
                                        InactiveTxt = control.inactiveTxt,
                                        ControlsDataType = dataType,
                                        StaticData = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) || control.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()) : CodeGenControlsAttributeHelper.ConversionStaticData(control.options.ToJsonString()),
                                        propsUrl = CodeGenControlsAttributeHelper.GetControlsPropsUrl(control.__config__.jnpfKey, dataType, control),
                                        Label = CodeGenControlsAttributeHelper.GetControlsLabel(control.__config__.jnpfKey, dataType, control),
                                        Value = CodeGenControlsAttributeHelper.GetControlsValue(control.__config__.jnpfKey, dataType, control),
                                        Children = CodeGenControlsAttributeHelper.GetControlsChildren(control.__config__.jnpfKey, dataType, control),
                                        Separator = control.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? "/" : control.separator,
                                        IsConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsDetailConversion = CodeGenControlsAttributeHelper.JudgeControlIsDataConversion(control.__config__.jnpfKey, dataType, CodeGenFieldJudgeHelper.IsMultipleColumn(controls, field)),
                                        IsSystemControl = false,
                                        IsUpdate = CodeGenControlsAttributeHelper.JudgeControlIsSystemControls(control.__config__.jnpfKey),
                                        ControlLabel = control.__config__.label,
                                        IsImportField = isImportField.ParseToBool(),
                                        ImportConfig = CodeGenControlsAttributeHelper.GetImportConfig(columnDesignModel, control, column.field, tableName),
                                        ShowField = control.relational,
                                        ShowAllLevels = control.showAllLevels,
                                        IsTreeParentField = field.Equals(columnDesignModel.parentField),
                                        isStorage = CodeGenControlsAttributeHelper.GetIsControlStoreType(control.__config__.jnpfKey, control.isStorage),
                                        IsLinkage = CodeGenControlsAttributeHelper.IsControlLinkageConfiguration(control),
                                        LinkageConfig = CodeGenControlsAttributeHelper.ObtainTheCurrentControlLinkageConfiguration(control, 0),
                                    });
                                    break;
                            }

                            break;
                        case false:
                            tableColumnList.Add(new TableColumnConfigModel()
                            {
                                ColumnName = field.ToUpperCase(),
                                OriginalColumnName = column.field,
                                ColumnComment = column.fieldName,
                                DataType = column.dataType,
                                NetType = CodeGenHelper.ConvertDataType(column.dataType),
                                PrimaryKey = false,
                                IsConversion = false,
                                IsSystemControl = false,
                                IsUpdate = false,
                            });
                            break;
                    }

                    break;
            }
        }

        if (!tableColumnList.Any(t => t.PrimaryKey))
            throw Oops.Oh(ErrorCode.D2104);

        return GetCodeGenConfigModel(formDataModel, columnDesignModel, tableColumnList, controls, tableName, templateEntity);
    }

    /// <summary>
    /// 代码生成前端引擎.
    /// </summary>
    /// <param name="logic">生成逻辑;4-pc,5-app.</param>
    /// <param name="formDataModel">表单Json包.</param>
    /// <param name="controls">移除布局控件后的控件列表.</param>
    /// <param name="tableColumns">表字段.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenFrontEndConfigModel CodeGenFrontEndEngine(int logic, FormDataModel formDataModel, List<FieldsModel> controls, List<TableColumnConfigModel> tableColumns, VisualDevEntity templateEntity)
    {
        var result = new CodeGenFrontEndConfigModel();
        ColumnDesignModel columnDesignModel = new ColumnDesignModel();
        var columnOptions = string.Empty; // 前端要原生模板的 ColumnOptions

        // 是否开启流程
        var hasFlow = templateEntity.EnableFlow == 1 ? true : false;
        if (templateEntity.Type == 3)
            hasFlow = true;

        // 导入URL
        var importUrl = string.Format("{0}/{1}", formDataModel.areasName, formDataModel.className.FirstOrDefault());

        // 判断生成逻辑模版读取方案
        switch (logic)
        {
            case 4:
                columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                if (columnDesignModel != null)
                {
                    var opt = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
                    if (opt.ContainsKey("columnOptions")) columnOptions = opt["columnOptions"].ToJsonString();
                }
                columnDesignModel ??= new ColumnDesignModel();

                switch (columnDesignModel.type)
                {
                    case 4:
                        break;
                    case 3:
                    case 5:
                        // 分组表格、树形表格 没有分页
                        columnDesignModel.hasPage = false;
                        break;
                }
                break;
            case 5:
                columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                if (columnDesignModel != null)
                {
                    var opt = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
                    if (opt.ContainsKey("columnOptions")) columnOptions = opt["columnOptions"].ToJsonString();
                }
                columnDesignModel ??= new ColumnDesignModel();
                break;
        }

        // 发起表单 没有查询条件
        switch (templateEntity.Type)
        {
            case 3:
                break;
            default:
                if (templateEntity.WebType != 1)
                    controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerFormDataModel(controls, columnDesignModel);
                break;
        }

        // 联动关系链判断
        controls = CodeGenUnifiedHandlerHelper.LinkageChainJudgment(controls, columnDesignModel);

        /*
        *  PC 逻辑时： 行内编辑时 pc端需要循环子表日期控件
        *  APP 逻辑时：循环出除子表外全部开启千位符的数字输入控件字段
        */
        List<CodeGenSpecifyDateFormatSetModel> specifyDateFormatSet = new List<CodeGenSpecifyDateFormatSetModel>();
        var appThousandField = string.Empty;
        switch (logic)
        {
            case 4:
                switch (columnDesignModel.type)
                {
                    case 4:
                        foreach (var item in controls)
                        {
                            var config = item.__config__;
                            switch (config.jnpfKey)
                            {
                                case JnpfKeyConst.TABLE:
                                    var model = CodeGenFormControlDesignHelper.CodeGenSpecifyDateFormatSetModel(item);
                                    if (model != null)
                                        specifyDateFormatSet.Add(model);
                                    break;
                            }
                        }
                        break;
                }
                break;
            case 5:
                appThousandField = controls.FindAll(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) && it.thousands).Select(it => it.__vModel__).ToList().ToJsonString();
                appThousandField = appThousandField == "[]" ? null : appThousandField;
                break;
        }

        var FormScriptConfigModel = CodeGenFormControlDesignHelper.FormScriptDesign(formDataModel.fields, controls, string.Empty, formDataModel, columnDesignModel?.columnList, columnDesignModel.type, logic, hasFlow, true);

        // 列表顶部按钮
        var indexTopButton = new List<IndexButtonDesign>();

        // 列表行按钮
        var indexColumnButtonDesign = new List<IndexButtonDesign>();

        // 显示列字符串
        var columnList = string.Empty;

        // 查询条件字符串
        var searchList = string.Empty;

        // 高级查询字符串
        var superQueryJson = string.Empty;

        switch (templateEntity.Type)
        {
            case 3:
                break;
            default:
                switch (templateEntity.WebType)
                {
                    case 1:
                        {
                            var pureFormBtnsList = columnDesignModel?.btnsList?.FindAll(it => it.value.Equals("add"));
                            if (pureFormBtnsList == null)
                            {
                                pureFormBtnsList = new List<ButtonConfigModel>
                                {
                                    new ButtonConfigModel
                                    {
                                        icon = "icon-ym icon-ym-btn-add",
                                        label = "新增",
                                        value = "add"
                                    }
                                };
                            }

                            // 生成头部按钮信息
                            foreach (var item in pureFormBtnsList)
                            {
                                indexTopButton.Add(new IndexButtonDesign()
                                {
                                    Type = item.value.Equals("add") ? "primary" : "link",
                                    Icon = item.icon,
                                    Method = GetCodeGenIndexButtonHelper.IndexTopButton(item.value, importUrl, hasFlow),
                                    Value = item.value,
                                    Label = item.label
                                });
                            }
                        }
                        break;
                    case 2:
                        columnList = templateEntity.ColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("columnList")?.ToJsonStringOld();
                        searchList = columnDesignModel?.searchList.ToJsonString();
                        superQueryJson = columnOptions;

                        // 生成头部按钮信息
                        foreach (var item in columnDesignModel?.btnsList)
                        {
                            indexTopButton.Add(new IndexButtonDesign()
                            {
                                Type = item.value.Equals("add") ? "primary" : "link",
                                Icon = item.icon,
                                Method = GetCodeGenIndexButtonHelper.IndexTopButton(item.value, importUrl, hasFlow),
                                Value = item.value,
                                Label = item.label
                            });
                        }

                        // 生成行按钮信息
                        foreach (var item in columnDesignModel.columnBtnsList)
                        {
                            indexColumnButtonDesign.Add(new IndexButtonDesign()
                            {
                                Type = item.value == "remove" ? "class='JNPF-table-delBtn' " : string.Empty,
                                Icon = item.icon,
                                Method = GetCodeGenIndexButtonHelper.IndexColumnButton(item.value, tableColumns.Find(it => it.PrimaryKey.Equals(true))?.LowerColumnName, formDataModel.primaryKeyPolicy, templateEntity.EnableFlow == 1 ? true : false, columnDesignModel?.type == 4 ? true : false),
                                Value = item.value,
                                Label = item.label,
                                Disabled = GetCodeGenIndexButtonHelper.WorkflowIndexColumnButton(item.value),
                            });
                        }

                        break;
                }

                break;
        }

        var multipleQueryFields = GetMultiEndQueryMerging(templateEntity, controls);

        // 控件查询多选数组
        var controlQueryMultipleSelectionArray = new List<string>
        {
            JnpfKeyConst.SELECT,
            JnpfKeyConst.DEPSELECT,
            JnpfKeyConst.ROLESELECT,
            JnpfKeyConst.USERSELECT,
            JnpfKeyConst.USERSSELECT,
            JnpfKeyConst.COMSELECT,
            JnpfKeyConst.POSSELECT,
            JnpfKeyConst.GROUPSELECT,
        };

        // 查询条件查询差异列表
        var queryCriteriaQueryVarianceList = columnDesignModel?.searchList?.FindAll(it => controlQueryMultipleSelectionArray.Contains(it.__config__.jnpfKey)).ToList().FindAll(it => !it.searchMultiple.Equals(multipleQueryFields.Find(x => x.__config__.jnpfKey.Equals(it.__config__.jnpfKey) && x.prop.Equals(it.prop)).searchMultiple));

        queryCriteriaQueryVarianceList?.ForEach(item =>
        {
            switch (item.__config__.isSubTable)
            {
                case true:
                    item.__vModel__ = string.Format("{0}_{1}", item.__config__.parentVModel, item.__vModel__);
                    break;
            }
        });

        List<string> queryVarianceList = queryCriteriaQueryVarianceList?.Select(it => it.__vModel__).ToList();

        // 判断下左侧控件是否为组织选择控件 如果是的话从查询差异列表移除该字段
        switch (logic)
        {
            case 4:
                if (controls.Any(it => it.__vModel__.Equals(columnDesignModel?.treeRelation) && it.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT)))
                    queryVarianceList.Remove(columnDesignModel?.treeRelation);
                break;
        }

        // 重置 ColumnData 参数
        switch (logic)
        {
            case 4:
                columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel ??= new ColumnDesignModel();
                switch (columnDesignModel.type)
                {
                    case 4:
                        break;
                    case 3:
                    case 5:
                        // 分组表格、树形表格 没有分页
                        columnDesignModel.hasPage = false;
                        break;
                }
                break;
            case 5:
                columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel ??= new ColumnDesignModel();
                break;
        }

        // 主键字段
        var primaryKeyField = tableColumns.Find(it => it.PrimaryKey.Equals(true))?.LowerColumnName;

        // 是否存在查询
        var hasSearch = columnDesignModel?.searchList?.Any();

        // 是否存在子表
        var hasChildTable = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE));

        // 是否开启`新增`
        var hasAdd = indexTopButton.Any(it => it.Value.Equals("add"));

        // 是否开启`导出`
        var hasDownload = indexTopButton.Any(it => it.Value.Equals("download"));

        // 是否开启`导入`
        var hasUpload = indexTopButton.Any(it => it.Value.Equals("upload"));

        // 是否开启`批量删除`
        var hasBatchRemove = indexTopButton.Any(it => it.Value.Equals("batchRemove"));

        // 是否开启`批量打印`
        var hasBatchPrint = indexTopButton.Any(it => it.Value.Equals("batchPrint"));

        // 批量打印字段列表
        var batchPrints = columnDesignModel?.printIds?.ToJsonString();

        // 是否开启`编辑`
        var hasEdit = indexColumnButtonDesign.Any(it => it.Value.Equals("edit"));

        // 是否开启`删除`
        var hasRemove = indexColumnButtonDesign.Any(it => it.Value.Equals("remove"));

        // 是否开启`详情`
        var hasDetail = indexColumnButtonDesign.Any(it => it.Value.Equals("detail"));

        // 是否开启`关联表单详情`
        var hasRelationDetail = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM));

        // 是否开启子表`关联表单详情`
        var hasSubTableRelationDetail = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.children.Any(c => c.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)));

        // 是否开启数组输入千位符
        var hasThousands = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) && it.thousands);

        // 是否开启子表数组输入千位符
        var hasSubTableThousands = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.children.Any(c => c.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) && c.thousands));

        // 左侧树控件
        IndexSearchFieldModel? treeControl = new IndexSearchFieldModel();

        // 左侧树配置
        CodeGenFrontEndLeftTreeFieldDesignModel leftTree = new CodeGenFrontEndLeftTreeFieldDesignModel();

        // PC端
        switch (logic)
        {
            case 4:
                {
                    // 列表布局为：左侧树
                    switch (columnDesignModel.type)
                    {
                        case 2:
                            {
                                treeControl = multipleQueryFields?.Find(it => it.id.Equals(columnDesignModel?.treeRelation));
                                leftTree = new CodeGenFrontEndLeftTreeFieldDesignModel
                                {
                                    jnpfKey = treeControl.__config__.jnpfKey,
                                    HasSearch = (columnDesignModel?.hasTreeQuery).ParseToBool(),
                                    Title = columnDesignModel?.treeTitle,
                                    TreeDataSource = columnDesignModel?.treeDataSource,
                                    TreeDictionary = columnDesignModel?.treeDictionary,
                                    TreeInterfaceId = columnDesignModel?.treeSyncInterfaceId,
                                    TreePropsUrl = columnDesignModel?.treePropsUrl,
                                    Key = columnDesignModel?.treePropsValue,
                                    ShowField = columnDesignModel?.treePropsLabel,
                                    Children = columnDesignModel?.treePropsChildren,
                                    TreeRelation = columnDesignModel?.treeRelation,
                                    HasSynType = columnDesignModel?.treeSyncType == 1 ? false : true,
                                    TreeTemplateJson = columnDesignModel?.treeSyncTemplateJson?.ToJsonString(),
                                    TemplateJson = columnDesignModel?.treeTemplateJson?.ToJsonString(),
                                    IsMultiple = (treeControl?.searchMultiple).ParseToBool()
                                };
                            }

                            break;
                    }
                }

                break;
        }

        // 表格配置
        var tableConfig = new CodeGenFrontEndTableConfigModel()
        {
            HasSuperQuery = (columnDesignModel?.hasSuperQuery).ParseToBool(),
            HasPage = (columnDesignModel?.hasPage).ParseToBool(),
            PageSize = columnDesignModel.pageSize,
            DefaultSortConfig = columnDesignModel.defaultSortConfig != null ? columnDesignModel.defaultSortConfig.ToJsonString() : "[]",
            ShowSummary = (columnDesignModel?.showSummary).ParseToBool(),
            ChildTableStyle = columnDesignModel.childTableStyle,
            Sort = columnDesignModel?.sort,
            Sidx = columnDesignModel?.defaultSidx,
            SummaryField = columnDesignModel?.summaryField.ToJsonString(),
            GroupField = columnDesignModel?.groupField,
            ShowOverflow = columnDesignModel.showOverflow,
        };

        // 表单属性
        var formAttribute = new CodeGenFrontEndFormAttributeModel()
        {
            Size = formDataModel.size,
            LabelPosition = formDataModel.labelPosition,
            LabelWidth = formDataModel.labelWidth,
            PopupType = templateEntity.WebType == 1 ? "fullScreen" : formDataModel.popupType,
            Gutter = formDataModel.gutter,
            CancelButtonText = formDataModel.cancelButtonText,
            ConfirmButtonText = formDataModel.confirmButtonText,
            GeneralWidth = formDataModel.generalWidth,
            FullScreenWidth = formDataModel.fullScreenWidth,
            DrawerWidth = formDataModel.drawerWidth,
            PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
            HasPrintBtn = formDataModel.hasPrintBtn,
            PrintButtonText = formDataModel.printButtonText,
            PrintId = formDataModel.printId.ToJsonString(),
        };

        var basicInfo = new CodeGenBasicInfoAttributeModel()
        {
            Id = templateEntity.Id,
            FullName = templateEntity.FullName,
            EnCode = templateEntity.EnCode,
            Category = templateEntity.Category,
            PropertyJson = CodeGenFormControlDesignHelper.GetPropertyJson(controls).ToJsonString(),
            TableJson = templateEntity.Tables.ToJsonString(),
            CreatorTime = DateTime.Now.ParseToUnixTime(),
            DbLinkId = templateEntity.DbLinkId,
        };

        switch (templateEntity.WebType)
        {
            default:
                result = new CodeGenFrontEndConfigModel()
                {
                    NameSpace = formDataModel.areasName,
                    ClassName = formDataModel.className.FirstOrDefault(),
                    WebType = templateEntity.Type == 3 ? templateEntity.Type : templateEntity.WebType,
                    Type = columnDesignModel.type,
                    Title = templateEntity.FullName,
                    HasSearch = hasSearch.ParseToBool(),
                    HasChildTable = hasChildTable,
                    LeftTree = leftTree,
                    TableConfig = tableConfig,
                    TopButtonDesign = indexTopButton,
                    ColumnButtonDesign = indexColumnButtonDesign,
                    HasFlow = hasFlow,
                    HasAdd = hasAdd,
                    HasDownload = hasDownload,
                    HasUpload = hasUpload,
                    HasBatchRemove = hasBatchRemove,
                    HasBatchPrint = hasBatchPrint,
                    BatchPrints = batchPrints,
                    HasEdit = hasEdit,
                    HasRemove = hasRemove,
                    HasDetail = hasDetail,
                    HasRelationDetail = hasRelationDetail,
                    HasSubTableRelationDetail = hasSubTableRelationDetail,
                    HasThousands = hasThousands,
                    HasSubTableThousands = hasSubTableThousands,
                    UseBtnPermission = (columnDesignModel?.useBtnPermission).ParseToBool(),
                    UseColumnPermission = (columnDesignModel?.useColumnPermission).ParseToBool(),
                    UseFormPermission = (columnDesignModel?.useFormPermission).ParseToBool(),
                    PrimaryKeyField = primaryKeyField,
                    FormAttribute = formAttribute,
                    BasicInfo = basicInfo,
                    ColumnList = columnList,
                    SearchList = searchList,
                    SuperQueryJson = superQueryJson,
                    FormScript = FormScriptConfigModel,
                    QueryCriteriaQueryVarianceList = queryVarianceList,
                    ComplexColumns = columnDesignModel.complexHeaderList != null && columnDesignModel.complexHeaderList.Any() ? columnDesignModel.complexHeaderList.ToJsonString() : "[]",
                    HasConfirmAndAddBtn = formDataModel.hasConfirmAndAddBtn,
                };
                break;
        }

        return result;
    }

    /// <summary>
    /// 前端.
    /// </summary>
    /// <param name="logic">生成逻辑;4-pc,5-app.</param>
    /// <param name="formDataModel">表单Json包.</param>
    /// <param name="controls">移除布局控件后的控件列表.</param>
    /// <param name="tableColumns">表字段.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static FrontEndGenConfigModel SingleTableFrontEnd(int logic, FormDataModel formDataModel, List<FieldsModel> controls, List<TableColumnConfigModel> tableColumns, VisualDevEntity templateEntity)
    {
        ColumnDesignModel columnDesignModel = new ColumnDesignModel();
        var columnOptions = string.Empty; // 前端要原生模板的 ColumnOptions
        bool isInlineEditor = false;
        switch (logic)
        {
            case 4:
                columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                if (columnDesignModel != null)
                {
                    var opt = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
                    if (opt.ContainsKey("columnOptions")) columnOptions = opt["columnOptions"].ToJsonString();
                }

                columnDesignModel ??= new ColumnDesignModel();
                isInlineEditor = columnDesignModel.type == 4 ? true : false;
                break;
            case 5:
                ColumnDesignModel pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                if (pcColumnDesignModel != null)
                {
                    var opt = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
                    if (opt.ContainsKey("columnOptions")) columnOptions = opt["columnOptions"].ToJsonString();
                }
                isInlineEditor = pcColumnDesignModel?.type == 4 ? true : false;
                columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel ??= new ColumnDesignModel();

                // 移动端的分页遵循PC端
                columnDesignModel.hasPage = templateEntity.WebType == 1 ? false : pcColumnDesignModel.hasPage;
                break;
        }

        switch (templateEntity.Type)
        {
            case 3:
                break;
            default:
                if (templateEntity.WebType != 1)
                    controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerFormDataModel(controls, columnDesignModel);
                break;
        }

        // 联动关系链判断
        controls = CodeGenUnifiedHandlerHelper.LinkageChainJudgment(controls, columnDesignModel);

        Dictionary<string, List<string>> listQueryControls = CodeGenQueryControlClassificationHelper.ListQueryControl(logic);

        /*
         *  PC 逻辑时： 行内编辑时 pc端需要循环子表日期控件
         *  APP 逻辑时：循环出除子表外全部开启千位符的数字输入控件字段
         */
        List<CodeGenSpecifyDateFormatSetModel> specifyDateFormatSet = new List<CodeGenSpecifyDateFormatSetModel>();
        var appThousandField = string.Empty;
        switch (logic)
        {
            case 4:
                switch (columnDesignModel.type)
                {
                    case 4:
                        foreach (var item in controls)
                        {
                            var config = item.__config__;
                            switch (config.jnpfKey)
                            {
                                case JnpfKeyConst.TABLE:
                                    var model = CodeGenFormControlDesignHelper.CodeGenSpecifyDateFormatSetModel(item);
                                    if (model != null)
                                        specifyDateFormatSet.Add(model);
                                    break;
                            }
                        }
                        break;
                }
                break;
            case 5:
                if (formDataModel.labelPosition.Equals("right"))
                {
                    formDataModel.labelPosition = "left";
                }
                appThousandField = controls.FindAll(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) && it.thousands).Select(it => it.__vModel__).ToList().ToJsonString();
                appThousandField = appThousandField == "[]" ? null : appThousandField;
                break;
        }

        // 表单脚本设计
        List<FormScriptDesignModel>? formScriptDesign = CodeGenFormControlDesignHelper.FormScriptDesign("SingleTable", controls, tableColumns, columnDesignModel?.columnList);

        // 整个表单控件
        List<FormControlDesignModel>? formControlList = CodeGenFormControlDesignHelper.FormControlDesign(formDataModel.fields, controls, formDataModel, columnDesignModel?.columnList, columnDesignModel.type, logic, 2, true);

        var formRealControl = CodeGenFormControlDesignHelper.FormRealControl(controls);

        // 列表控件Option
        var indnxControlOption = CodeGenFormControlDesignHelper.FormControlProps(formDataModel.fields, controls, columnDesignModel, logic, true);

        // 列表查询字段设计
        var indexSearchFieldDesign = new List<IndexSearchFieldDesignModel>();

        // 查询条件查询差异列表
        var queryCriteriaQueryVarianceList = new List<IndexSearchFieldModel>();

        // 列表顶部按钮
        var indexTopButton = new List<IndexButtonDesign>();

        // 列表行按钮
        var indexColumnButtonDesign = new List<IndexButtonDesign>();

        // 列表页列表
        var indexColumnDesign = new List<IndexColumnDesign>();

        var indexSortFieldDesign = new List<IndexSearchFieldDesignModel>();

        switch (templateEntity.Type)
        {
            case 3:
                break;
            default:
                switch (templateEntity.WebType)
                {
                    case 2:

                        var newSearchList = new List<IndexSearchFieldModel>();

                        switch (logic)
                        {
                            case 4:
                                newSearchList = templateEntity.ColumnData?.ToObject<ColumnDesignModel>().searchList;

                                switch (columnDesignModel.type)
                                {
                                    case 2:
                                        newSearchList = CodeGenUnifiedHandlerHelper.UnifiedHandlerListQueries(newSearchList, templateEntity.ColumnData?.ToObject<ColumnDesignModel>(), templateEntity.AppColumnData?.ToObject<ColumnDesignModel>());
                                        break;
                                }
                                break;
                            case 5:
                                newSearchList = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>().searchList;
                                break;
                        }

                        // 本身查询列表内带有控件全属性 单表不需要匹配表字段
                        foreach (var item in newSearchList)
                        {
                            // 查询控件分类
                            var queryControls = listQueryControls.Where(q => q.Value.Contains(item.__config__.jnpfKey)).FirstOrDefault();

                            var childTableLabel = string.Empty;
                            var childControl = item.__config__.parentVModel;

                            // 是否子表查询
                            bool isChildQuery = false;

                            // 表单真实控件
                            FieldsModel? column = new FieldsModel();
                            if (!string.IsNullOrEmpty(childControl))
                            {
                                isChildQuery = true;
                                column = controls.Find(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__vModel__.Equals(childControl) && it.__config__.children.Any(child => child.__vModel__.Equals(item.__vModel__)));
                                childTableLabel = column.__config__.label;
                                column = column.__config__.children.Find(it => it.__vModel__ == item.__vModel__);
                            }
                            else
                            {
                                column = controls.Find(c => c.__vModel__ == item.__vModel__);
                            }

                            if (column.__config__.jnpfKey.Equals(column.__config__.jnpfKey) || column.__config__.jnpfKey.Equals(column.__config__.jnpfKey))
                                column.format = "yyyy-MM-dd HH:mm:ss";
                            var searchDefaultValue = item.value.IsNotEmptyOrNull() ? item.value.ToJsonString() : "undefined";
                            indexSearchFieldDesign.Add(new IndexSearchFieldDesignModel()
                            {
                                OriginalName = string.IsNullOrEmpty(column.__config__.parentVModel) ? column.__vModel__ : string.Format("{0}_{1}", column.__config__.parentVModel, column.__vModel__),
                                Name = string.IsNullOrEmpty(column.__config__.parentVModel) ? column.__vModel__ : string.Format("{0}_{1}", column.__config__.parentVModel, column.__vModel__),
                                LowerName = column.__vModel__,
                                DefaultValues = searchDefaultValue.Equals("[]") ? "undefined" : searchDefaultValue,
                                Tag = column.__config__.tag,
                                Clearable = item.clearable ? "clearable " : string.Empty,
                                Format = column.format,
                                ValueFormat = column.valueformat.IsNullOrEmpty() ? column.format : column.valueformat,
                                Label = item.label,
                                IsChildQuery = isChildQuery,
                                QueryControlsKey = queryControls.Key != null ? queryControls.Key : null,
                                Props = column.props,
                                Index = newSearchList.IndexOf(item),
                                Type = column.type,
                                ShowAllLevels = (column?.showAllLevels).ParseToBool() ? "true" : "false",
                                Level = column.level,
                                IsMultiple = item.searchMultiple,
                                IsKeyword = item.isKeyword,
                                jnpfKey = column.__config__.jnpfKey,
                                SelectType = column.selectType != null ? column.selectType.Equals("custom") ? string.Format("selectType='{0}' ", column.selectType) : string.Format("selectType='all' ") : string.Empty,
                                AbleIds = column.selectType != null && column.selectType == "custom" ? string.Format(":ableIds='{0}_AbleIds' ", !string.IsNullOrEmpty(childControl) ? string.Format("{0}_{1}", childControl, column.__vModel__) : item.__vModel__) : string.Empty,
                                RelationField = column.relationField,
                                InterfaceId = column.interfaceId,
                                Total = column.total,
                            });
                        }

                        var multipleQueryFields = GetMultiEndQueryMerging(templateEntity);

                        // 控件查询多选数组
                        var controlQueryMultipleSelectionArray = new List<string>
                        {
                            JnpfKeyConst.SELECT,
                            JnpfKeyConst.DEPSELECT,
                            JnpfKeyConst.ROLESELECT,
                            JnpfKeyConst.USERSELECT,
                            JnpfKeyConst.USERSSELECT,
                            JnpfKeyConst.COMSELECT,
                            JnpfKeyConst.POSSELECT,
                            JnpfKeyConst.GROUPSELECT,
                        };

                        // 查询条件查询差异列表
                        queryCriteriaQueryVarianceList = columnDesignModel.searchList.FindAll(it => controlQueryMultipleSelectionArray.Contains(it.__config__.jnpfKey)).ToList().FindAll(it => !it.searchMultiple.Equals(multipleQueryFields.Find(x => x.__config__.jnpfKey.Equals(it.__config__.jnpfKey) && x.prop.Equals(it.prop)).searchMultiple));

                        queryCriteriaQueryVarianceList?.ForEach(item =>
                        {
                            switch (item.__config__.isSubTable)
                            {
                                case true:
                                    item.__vModel__ = string.Format("{0}_{1}", item.__config__.parentVModel, item.__vModel__);
                                    break;
                            }
                        });

                        // 生成头部按钮信息
                        foreach (var item in columnDesignModel?.btnsList)
                        {
                            indexTopButton.Add(new IndexButtonDesign()
                            {
                                Type = columnDesignModel.btnsList.IndexOf(item) == 0 ? "primary" : "text",
                                Icon = item.icon,
                                Method = GetCodeGenIndexButtonHelper.IndexTopButton(item.value, templateEntity.EnableFlow == 1 ? true : false),
                                Value = item.value,
                                Label = item.label
                            });
                        }

                        // 生成行按钮信息
                        foreach (var item in columnDesignModel.columnBtnsList)
                        {
                            indexColumnButtonDesign.Add(new IndexButtonDesign()
                            {
                                Type = item.value == "remove" ? "class='JNPF-table-delBtn' " : string.Empty,
                                Icon = item.icon,
                                Method = GetCodeGenIndexButtonHelper.IndexColumnButton(item.value, tableColumns.Find(it => it.PrimaryKey.Equals(true))?.LowerColumnName, formDataModel.primaryKeyPolicy, templateEntity.EnableFlow == 1 ? true : false, columnDesignModel?.type == 4 ? true : false),
                                Value = item.value,
                                Label = item.label,
                                Disabled = GetCodeGenIndexButtonHelper.WorkflowIndexColumnButton(item.value)
                            });
                        }

                        List<string> ChildControlField = new List<string>();

                        // 生成列信息
                        foreach (var item in columnDesignModel.columnList)
                        {
                            if (!ChildControlField.Any(it => it == item.id))
                            {
                                var relationTable = item?.__config__?.relationTable;
                                if (relationTable != null && !indexColumnDesign.Any(it => it.TableName == relationTable))
                                {
                                    var childTableAll = columnDesignModel.columnList.FindAll(it => it.__config__.relationTable == relationTable);
                                    var childTable = controls.Find(it => it.__config__.tableName == relationTable);
                                    if (childTable.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                                    {
                                        var childTableColumnDesign = new List<IndexColumnDesign>();
                                        foreach (var child in childTableAll)
                                        {
                                            var columnControl = childTable.__config__.children.Find(it => it.__vModel__.Equals(child.id.Split('-')[1]));
                                            childTableColumnDesign.Add(new IndexColumnDesign()
                                            {
                                                TableName = child.__config__.tableName,
                                                Name = columnControl.__vModel__,
                                                OptionsName = columnControl.__vModel__,
                                                LowerName = columnControl.__vModel__,
                                                jnpfKey = child.__config__.jnpfKey,
                                                Label = columnControl.__config__.label,
                                                Width = child.width.ToString() == "0" ? "0" : string.Format("{0}", child.width),
                                                Align = child.align,
                                                IsSort = child.sortable ? string.Format("sortable='custom' ") : string.Empty,
                                                IsChildTable = true,
                                                Format = child.format?.ToLower().Replace(":mm", ":MM"),
                                                ModelId = child.modelId,
                                                Thousands = child.thousands,
                                                Precision = child.precision == null ? 0 : child.precision,
                                                UseMask = child.useMask ? "useMask " : string.Empty,
                                                UseScan = child.useScan ? "useScan " : string.Empty,
                                                MaskConfig = child.maskConfig != null ? $":maskConfig='{child.maskConfig.ToJsonString()}' " : string.Empty,
                                            });
                                            ChildControlField.Add(string.Format("{0}", child.id));
                                        }

                                        indexColumnDesign.Add(new IndexColumnDesign()
                                        {
                                            TableName = relationTable,
                                            Name = childTable.__vModel__,
                                            Label = childTable.__config__.label,
                                            jnpfKey = JnpfKeyConst.TABLE,
                                            IsChildTable = true,
                                            ChildTableDesigns = childTableColumnDesign,
                                            Fixed = string.Empty,
                                        });
                                    }
                                }
                                else
                                {
                                    indexColumnDesign.Add(new IndexColumnDesign()
                                    {
                                        TableName = item?.__config__?.tableName,
                                        Name = item.prop,
                                        OptionsName = item.prop,
                                        LowerName = item.prop,
                                        jnpfKey = item.__config__.jnpfKey,
                                        Label = item.label,
                                        Width = item.width == null ? string.Empty : string.Format("width='{0}' ", item.width),
                                        Fixed = columnDesignModel.childTableStyle == 1 ? (item.@fixed == "none" || item.@fixed == null ? string.Empty : string.Format("fixed='{0}' ", item.@fixed)) : string.Empty,
                                        Align = item.align,
                                        IsSort = item.sortable ? string.Format("sortable='custom' ") : string.Empty,
                                        IsChildTable = false,
                                        ModelId = item.modelId,
                                        Thousands = item.thousands,
                                        Precision = item.precision == null ? 0 : item.precision,
                                        UseMask = item.useMask ? "useMask " : string.Empty,
                                        UseScan = item.useScan ? "useScan " : string.Empty,
                                        MaskConfig = item.maskConfig != null ? $":maskConfig='{item.maskConfig.ToJsonString()}' " : string.Empty,
                                    });
                                }

                            }
                        }

                        foreach (var item in columnDesignModel?.columnList.Where(x => x.sortable).ToList())
                        {
                            indexSortFieldDesign.Add(new IndexSearchFieldDesignModel
                            {
                                Label = item.__config__.label,
                                Name = item.__vModel__,
                            });
                        }

                        break;
                }

                break;
        }

        // 复杂表头 处理
        if (columnDesignModel.type.Equals(3) || columnDesignModel.type.Equals(5)) columnDesignModel.complexHeaderList.Clear();

        var indexComplexHeaderList = new List<IndexColumnDesign>();
        List<FormControlDesignModel> complexFormAllContols = null;
        if (logic.Equals(4) && templateEntity.WebType != 1)
        {
            if (columnDesignModel.complexHeaderList != null && columnDesignModel.complexHeaderList.Any())
            {
                var newColumnList = new List<IndexColumnDesign>();
                var tfVModelList = columnDesignModel.columnList.Where(x => !x.__config__.tableFixed.Equals("none")).Select(x => x.id).ToList();
                foreach (var item in columnDesignModel.columnList)
                {
                    if (item.__config__.tableFixed.Equals("none") && columnDesignModel.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item.id))))
                    {
                        var complexPItem = columnDesignModel.complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item.id)));
                        if (!newColumnList.Any(x => x.Name.Equals(complexPItem.id)))
                        {
                            var addItem = new IndexColumnDesign();
                            addItem.Name = complexPItem.id;
                            addItem.Align = complexPItem.align;
                            addItem.Label = complexPItem.fullName;
                            addItem.Fixed = string.Empty;
                            addItem.ComplexColumns = new List<IndexColumnDesign>();
                            complexPItem.childColumns.Where(x => !tfVModelList.Contains(x)).ToList().ForEach(it =>
                            {
                                var cItem = columnDesignModel.columnList.Find(x => x.id.Equals(it));
                                addItem.ComplexColumns.Add(new IndexColumnDesign()
                                {
                                    TableName = cItem?.__config__?.tableName,
                                    Name = cItem.prop,
                                    OptionsName = cItem.prop,
                                    LowerName = cItem.prop,
                                    jnpfKey = cItem.__config__.jnpfKey,
                                    Label = cItem.label,
                                    Width = cItem.width == null ? string.Empty : string.Format("width='{0}' ", cItem.width),
                                    Fixed = columnDesignModel.childTableStyle == 1 ? (cItem.@fixed == "none" || cItem.@fixed == null ? string.Empty : string.Format("fixed='{0}' ", cItem.@fixed)) : string.Empty,
                                    Align = cItem.align,
                                    IsSort = cItem.sortable ? string.Format("sortable='custom' ") : string.Empty,
                                    IsChildTable = false,
                                    ModelId = cItem.modelId,
                                    Thousands = cItem.thousands,
                                    Precision = cItem.precision == null ? 0 : cItem.precision,
                                    UseMask = cItem.useMask ? "useMask " : string.Empty,
                                    UseScan = cItem.useScan ? "useScan " : string.Empty,
                                    MaskConfig = cItem.maskConfig != null ? $":maskConfig='{cItem.maskConfig.ToJsonString()}' " : string.Empty,
                                });
                            });
                            newColumnList.Add(addItem);
                        }
                    }
                    else
                    {
                        if (!newColumnList.Any(x => x.Name.Equals(item.id) || x.Name.Equals(item.__config__.parentVModel)))
                            newColumnList.Add(indexColumnDesign.Find(x => x.Name.Equals(item.id) || x.Name.Equals(item.__config__.parentVModel)));
                    }
                }

                foreach (var item in newColumnList)
                {
                    if (item.ComplexColumns != null && item.ComplexColumns.Any())
                    {

                        item.CurrentIndex = indexColumnDesign.IndexOf(indexColumnDesign.Find(x => x.Name.Equals(item.ComplexColumns.FirstOrDefault().Name)));
                    }
                    else
                    {
                        item.CurrentIndex = indexColumnDesign.IndexOf(item);
                    }
                }

                indexColumnDesign = newColumnList.OrderBy(x => x.CurrentIndex).ToList();

                if (isInlineEditor)
                {
                    var newFormControlList = new List<FormControlDesignModel>();
                    var flist = CodeGenFormControlDesignHelper.GetFormControlDesignByTree(formControlList);
                    var ctfVModelList = columnDesignModel.columnList.Where(x => !x.__config__.tableFixed.Equals("none")).Select(x => x.id).ToList();

                    foreach (var item in columnDesignModel.columnList)
                    {
                        if (item.__config__.tableFixed.Equals("none") && columnDesignModel.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item.id))))
                        {
                            var complexPItem = columnDesignModel.complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item.id)));
                            if (!newFormControlList.Any(x => x.Name.Equals(complexPItem.id)))
                            {
                                var addItem = new FormControlDesignModel();
                                addItem.Name = complexPItem.id;
                                addItem.OriginalName = complexPItem.id;
                                addItem.Align = complexPItem.align;
                                addItem.Label = complexPItem.fullName;
                                addItem.ComplexColumns = new List<FormControlDesignModel>();
                                complexPItem.childColumns.Where(x => !ctfVModelList.Contains(x)).ToList().ForEach(it =>
                                {
                                    var cItem = flist.Find(x => x.Name.Equals(it) && x.jnpfKey != JnpfKeyConst.POPUPATTR && x.jnpfKey != JnpfKeyConst.RELATIONFORMATTR).Copy();
                                    if (formDataModel.labelSuffix.IsNotEmptyOrNull()) cItem.Label = cItem.Label.Replace(formDataModel.labelSuffix, "");
                                    addItem.ComplexColumns.Add(cItem);
                                });
                                newFormControlList.Add(addItem);
                            }
                        }
                        else
                        {
                            if (!newFormControlList.Any(x => x.Name.Equals(item.__vModel__) || x.Name.Equals(item.__config__.parentVModel)))
                            {
                                if (flist.Any(x => x.Name.Equals(item.id)))
                                {
                                    var addItem = flist.Find(x => x.Name.Equals(item.id)).Copy();
                                    if (formDataModel.labelSuffix.IsNotEmptyOrNull()) addItem.Label = addItem.Label.Replace(formDataModel.labelSuffix, "");
                                    newFormControlList.Add(addItem);
                                }
                                else
                                {
                                    var addItem = flist.Find(x => x.Name.IsNotEmptyOrNull() && (x.Name.Equals(item.__vModel__) || x.Name.Equals(item.__config__.parentVModel))).Copy();
                                    if (formDataModel.labelSuffix.IsNotEmptyOrNull()) addItem.Label = addItem.Label.Replace(formDataModel.labelSuffix, "");
                                    if (addItem != null) newFormControlList.Add(addItem);
                                }
                            }
                        }
                    }

                    foreach (var item in newFormControlList)
                    {
                        if (item.ComplexColumns != null && item.ComplexColumns.Any())
                            item.CurrentIndex = columnDesignModel.columnList.IndexOf(columnDesignModel.columnList.Find(x => x.id.Equals(item.ComplexColumns.FirstOrDefault().Name)));
                        else
                            item.CurrentIndex = columnDesignModel.columnList.IndexOf(columnDesignModel.columnList.Find(x => x.id.Equals(item.Name)));
                    }

                    complexFormAllContols = newFormControlList.Where(x => x.jnpfKey != JnpfKeyConst.TABLE).OrderBy(x => x.CurrentIndex).ToList();
                }
            }
        }

        var propertyJson = CodeGenFormControlDesignHelper.GetPropertyJson(formScriptDesign);

        var printIds = columnDesignModel.printIds != null ? string.Join(",", columnDesignModel.printIds) : null;
        var isBatchRemoveDel = indexTopButton.Any(it => it.Value == "batchRemove");
        var isBatchPrint = indexTopButton.Any(it => it.Value == "batchPrint");
        var isUpload = indexTopButton.Any(it => it.Value == "upload");
        var isDownload = indexTopButton.Any(it => it.Value == "download");
        var isRemoveDel = indexColumnButtonDesign.Any(it => it.Value == "remove");
        var isEdit = indexColumnButtonDesign.Any(it => it.Value == "edit");
        var isDetail = indexColumnButtonDesign.Any(it => it.Value == "detail");
        var isAdd = indexTopButton.Any(it => it.Value == "add");
        var isSort = columnDesignModel?.columnList?.Any(it => it.sortable) ?? false;
        var isSummary = formScriptDesign.Any(it => it.jnpfKey.Equals("table") && it.ShowSummary.Equals(true));
        var isTreeRelation = !string.IsNullOrEmpty(columnDesignModel?.treeRelation);
        var isRelationForm = formControlList.Any(it => it.IsRelationForm);
        var isTreeRelationMultiple = indexSearchFieldDesign.Any(it => it.Name.Equals(columnDesignModel?.treeRelation?.Replace("-", "_")) && it.IsMultiple);
        var isFixed = columnDesignModel.childTableStyle == 1 ? indexColumnDesign.Any(it => it.Fixed.Equals("fixed='left' ") && !it.Name.Equals(columnDesignModel.groupField)) : false;
        var isChildrenRegular = formScriptDesign.Any(it => it.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.RegList != null && it.RegList.Count > 0);
        var treeRelationControlKey = indexSearchFieldDesign.Find(it => it.Name.Equals(columnDesignModel?.treeRelation?.Replace("-", "_")))?.jnpfKey;

        string allThousandsField = columnDesignModel.summaryField?.Intersect(formScriptDesign.FindAll(it => it.Thousands && !it.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(it => it.Name).ToList()).ToList().ToJsonString();
        bool isChildrenThousandsField = formScriptDesign.Any(it => it.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.Thousands);

        // 是否开启特殊属性
        var isDateSpecialAttribute = CodeGenFormControlDesignHelper.DetermineWhetherTheControlHasEnabledSpecialAttributes(controls, JnpfKeyConst.DATE);
        var isTimeSpecialAttribute = CodeGenFormControlDesignHelper.DetermineWhetherTheControlHasEnabledSpecialAttributes(controls, JnpfKeyConst.TIME);

        // 表单默认值控件列表
        var defaultFormControlList = new DefaultFormControlModel();
        var defaultSearchList = new List<DefaultSearchFieldModel>();
        switch (logic)
        {
            case 4:
                columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel ??= new ColumnDesignModel();
                defaultFormControlList = CodeGenFormControlDesignHelper.DefaultFormControlList(controls, columnDesignModel.searchList);
                defaultSearchList = CodeGenFormControlDesignHelper.DefaultSearchFieldList(columnDesignModel.searchList);
                break;
            case 5:
                ColumnDesignModel pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                columnDesignModel ??= new ColumnDesignModel();
                defaultFormControlList = CodeGenFormControlDesignHelper.DefaultFormControlList(controls, columnDesignModel.searchList);
                defaultSearchList = CodeGenFormControlDesignHelper.DefaultSearchFieldList(columnDesignModel.searchList);

                // 移动端的分页遵循PC端
                columnDesignModel.hasPage = templateEntity.WebType == 1 ? false : pcColumnDesignModel.hasPage;
                break;
        }

        var isDefaultFormControl = defaultFormControlList.IsExistTime || defaultFormControlList.IsExistDate || defaultFormControlList.IsExistDepSelect || defaultFormControlList.IsExistComSelect || defaultFormControlList.IsSignField ||
            defaultFormControlList.IsExistUserSelect || defaultFormControlList.IsExistUsersSelect || defaultFormControlList.IsExistRoleSelect || defaultFormControlList.IsExistPosSelect || defaultFormControlList.IsExistGroupsSelect || defaultFormControlList.IsExistSubTable ? true : false;

        // 是否查询默认值
        var isDefaultSearchField = defaultSearchList.Count > 0 ? true : false;

        switch (columnDesignModel.type)
        {
            case 3:
            case 5:
                columnDesignModel.hasPage = false;
                break;
        }

        switch (templateEntity.WebType)
        {
            case 1:
                return new FrontEndGenConfigModel()
                {
                    NameSpace = formDataModel.areasName,
                    ClassName = formDataModel.className.FirstOrDefault(),
                    FormRef = formDataModel.formRef,
                    FormModel = formDataModel.formModel,
                    Size = formDataModel.size,
                    LabelPosition = formDataModel.labelPosition,
                    LabelWidth = formDataModel.labelWidth,
                    FormRules = formDataModel.formRules,
                    GeneralWidth = formDataModel.generalWidth,
                    FullScreenWidth = formDataModel.fullScreenWidth,
                    DrawerWidth = formDataModel.drawerWidth,
                    FormStyle = formDataModel.formStyle,
                    Type = columnDesignModel.type,
                    PrimaryKey = tableColumns?.Find(it => it.PrimaryKey.Equals(true))?.LowerColumnName,
                    FormList = formScriptDesign,
                    PopupType = formDataModel.popupType,
                    OptionsList = indnxControlOption,
                    IsRemoveDel = isRemoveDel,
                    IsDetail = isDetail,
                    IsEdit = isEdit,
                    IsAdd = isAdd,
                    IsSort = isSort,
                    HasPage = columnDesignModel.hasPage,
                    FormAllContols = formControlList,
                    ComplexFormAllContols = complexFormAllContols,
                    CancelButtonText = formDataModel.cancelButtonText,
                    ConfirmButtonText = formDataModel.confirmButtonText,
                    UseBtnPermission = columnDesignModel.useBtnPermission,
                    UseColumnPermission = false,
                    UseFormPermission = false,
                    IsSummary = isSummary,
                    PageSize = columnDesignModel.pageSize,
                    Sort = columnDesignModel.sort,
                    HasPrintBtn = formDataModel.hasPrintBtn,
                    PrintButtonText = formDataModel.printButtonText,
                    PrintId = formDataModel.printId != null ? string.Join(",", formDataModel.printId) : null,
                    IsChildDataTransfer = formScriptDesign.Any(it => it.IsDataTransfer.Equals(true)),
                    IsChildTableQuery = indexSearchFieldDesign.Any(it => it.IsChildQuery.Equals(true)),
                    IsChildTableShow = indexColumnDesign.Any(it => it.IsChildTable.Equals(true)),
                    ColumnList = templateEntity.ColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("columnList")?.ToJsonStringOld(),
                    IsInlineEditor = isInlineEditor,
                    GroupField = columnDesignModel.groupField,
                    GroupShowField = columnDesignModel?.columnList?.Where(x => x.__vModel__.ToLower() != columnDesignModel?.groupField?.ToLower()).FirstOrDefault()?.__vModel__,
                    PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
                    IsRelationForm = isRelationForm,
                    ChildTableStyle = columnDesignModel.childTableStyle,
                    IsChildrenRegular = isChildrenRegular,
                    DefaultFormControlList = defaultFormControlList,
                    IsDefaultFormControl = isDefaultFormControl,
                    PropertyJson = propertyJson,
                    FormRealControl = formRealControl,
                    IsDateSpecialAttribute = isDateSpecialAttribute,
                    IsTimeSpecialAttribute = isTimeSpecialAttribute,
                    IsChildrenThousandsField = isChildrenThousandsField,
                    HasConfirmAndAddBtn = formDataModel.hasConfirmAndAddBtn,
                    ConfirmAndAddText = formDataModel.confirmAndAddText,
                };
                break;
            default:
                var codeGenColumnData = new CodeGenColumnData
                {
                    treeInterfaceId = columnDesignModel.treeSyncInterfaceId,
                    treeTemplateJson = columnDesignModel.treeSyncTemplateJson,
                    templateJson = columnDesignModel.treeTemplateJson,
                };
                return new FrontEndGenConfigModel()
                {
                    PrintIds = printIds,
                    NameSpace = formDataModel.areasName,
                    ClassName = formDataModel.className.FirstOrDefault(),
                    FormRef = formDataModel.formRef,
                    FormModel = formDataModel.formModel,
                    Size = formDataModel.size,
                    LabelPosition = formDataModel.labelPosition,
                    LabelWidth = formDataModel.labelWidth,
                    FormRules = formDataModel.formRules,
                    GeneralWidth = formDataModel.generalWidth,
                    FullScreenWidth = formDataModel.fullScreenWidth,
                    DrawerWidth = formDataModel.drawerWidth,
                    FormStyle = formDataModel.formStyle,
                    Type = columnDesignModel.type,
                    TreeRelation = columnDesignModel?.treeRelation?.Replace("-", "_"),
                    TreeSelectType = columnDesignModel?.columnList?.FirstOrDefault(x => x.prop.Equals(columnDesignModel?.treeRelation))?.selectType,
                    TreeAbleIds = columnDesignModel?.columnList?.FirstOrDefault(x => x.prop.Equals(columnDesignModel?.treeRelation))?.ableIds.ToJsonString(),
                    TreeJnpfKey = columnDesignModel?.columnList?.FirstOrDefault(x => x.prop.Equals(columnDesignModel?.treeRelation))?.jnpfKey,
                    TreeTitle = columnDesignModel?.treeTitle,
                    TreePropsValue = columnDesignModel?.treePropsValue,
                    TreeDataSource = columnDesignModel?.treeDataSource,
                    TreeDictionary = columnDesignModel?.treeDictionary,
                    TreePropsUrl = columnDesignModel?.treePropsUrl,
                    TreePropsChildren = columnDesignModel?.treePropsChildren,
                    TreePropsLabel = columnDesignModel?.treePropsLabel,
                    TreeRelationControlKey = treeRelationControlKey,
                    IsTreeRelationMultiple = isTreeRelationMultiple,
                    IsExistQuery = templateEntity.Type == 3 ? false : (bool)columnDesignModel?.searchList?.Any(it => it.prop.Equals(columnDesignModel?.treeRelation)),
                    PrimaryKey = tableColumns?.Find(it => it.PrimaryKey.Equals(true))?.LowerColumnName,
                    FormList = formScriptDesign,
                    PopupType = formDataModel.popupType,
                    SearchColumnDesign = indexSearchFieldDesign,
                    SortFieldDesign = indexSortFieldDesign,
                    TopButtonDesign = indexTopButton,
                    ColumnButtonDesign = indexColumnButtonDesign,
                    ColumnDesign = indexColumnDesign,
                    OptionsList = indnxControlOption,
                    IsBatchRemoveDel = isBatchRemoveDel,
                    IsBatchPrint = isBatchPrint,
                    IsDownload = isDownload,
                    IsRemoveDel = isRemoveDel,
                    IsDetail = isDetail,
                    IsEdit = isEdit,
                    IsAdd = isAdd,
                    IsUpload = isUpload,
                    IsSort = isSort,
                    HasPage = columnDesignModel.hasPage,
                    FormAllContols = formControlList,
                    ComplexFormAllContols = complexFormAllContols,
                    CancelButtonText = formDataModel.cancelButtonText,
                    ConfirmButtonText = formDataModel.confirmButtonText,
                    UseBtnPermission = columnDesignModel.useBtnPermission,
                    UseColumnPermission = columnDesignModel.useColumnPermission,
                    UseFormPermission = columnDesignModel.useFormPermission,
                    IsSummary = isSummary,
                    PageSize = columnDesignModel.pageSize,
                    Sort = columnDesignModel.sort,
                    HasPrintBtn = formDataModel.hasPrintBtn,
                    PrintButtonText = formDataModel.printButtonText,
                    PrintId = formDataModel.printId != null ? string.Join(",", formDataModel.printId) : null,
                    IsChildDataTransfer = formScriptDesign.Any(it => it.IsDataTransfer.Equals(true)),
                    IsChildTableQuery = indexSearchFieldDesign.Any(it => it.IsChildQuery.Equals(true)),
                    IsChildTableShow = indexColumnDesign.Any(it => it.IsChildTable.Equals(true)),
                    ColumnList = templateEntity.ColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("columnList")?.ToJsonStringOld(),
                    HasSuperQuery = columnDesignModel.hasSuperQuery,
                    ColumnOptions = columnOptions,
                    IsInlineEditor = isInlineEditor,
                    GroupField = columnDesignModel.groupField,
                    GroupShowField = columnDesignModel?.columnList?.Where(x => x.__vModel__.ToLower() != columnDesignModel?.groupField?.ToLower()).FirstOrDefault()?.__vModel__,
                    PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
                    IsRelationForm = isRelationForm,
                    ChildTableStyle = columnDesignModel.childTableStyle,
                    IsFixed = isFixed,
                    IsChildrenRegular = isChildrenRegular,
                    TreeSynType = columnDesignModel.treeSyncType,
                    HasTreeQuery = columnDesignModel.hasTreeQuery,
                    ColumnData = codeGenColumnData,
                    SummaryField = columnDesignModel.summaryField,
                    ShowSummary = columnDesignModel.showSummary,
                    DefaultFormControlList = defaultFormControlList,
                    IsDefaultFormControl = isDefaultFormControl,
                    IsDefaultSearchField = isDefaultSearchField,
                    DefaultSearchList = defaultSearchList,
                    PropertyJson = propertyJson,
                    FormRealControl = formRealControl,
                    QueryCriteriaQueryVarianceList = queryCriteriaQueryVarianceList,
                    IsDateSpecialAttribute = isDateSpecialAttribute,
                    IsTimeSpecialAttribute = isTimeSpecialAttribute,
                    AllThousandsField = allThousandsField,
                    IsChildrenThousandsField = isChildrenThousandsField,
                    SpecifyDateFormatSet = specifyDateFormatSet,
                    AppThousandField = appThousandField,
                    HasConfirmAndAddBtn = formDataModel.hasConfirmAndAddBtn,
                    ConfirmAndAddText = formDataModel.confirmAndAddText,
                    ShowOverflow = columnDesignModel.showOverflow,
                };
                break;
        }
    }

    /// <summary>
    /// 多端查询合并.
    /// </summary>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="controls">移除布局演示后的表单全控件.</param>
    /// <param name="logic">生成逻辑;4-pc,5-app.</param>
    /// <returns></returns>
    public static List<IndexSearchFieldModel> GetMultiEndQueryMerging(VisualDevEntity templateEntity, List<FieldsModel> controls = null, int logic = 4)
    {
        ColumnDesignModel pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        ColumnDesignModel appColumnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();

        // 当查询列表内没有选中某个字段 该字段又被作为左侧树查询字段 读取表单内该字段的多选属性 查询多选全由查询列表配置 
        if (templateEntity.Type != 3 && controls != null && pcColumnDesignModel?.type == 2 && (pcColumnDesignModel.searchList.Count == 0 || !pcColumnDesignModel.searchList.Any(it => it.prop.Equals(pcColumnDesignModel.treeRelation))))
        {
            var search = new IndexGridFieldModel();

            // 读取app查询列表 再读取表单内字段
            switch (appColumnDesignModel.searchList.Any(it => it.prop.Equals(pcColumnDesignModel.treeRelation)))
            {
                case true:
                    pcColumnDesignModel.searchList.Add(appColumnDesignModel.searchList.Find(it => it.prop.Equals(pcColumnDesignModel.treeRelation)));
                    break;
                default:
                    search = pcColumnDesignModel.columnOptions.Find(it => it.id.Equals(pcColumnDesignModel.treeRelation));
                    var treeRelation = search?.Adapt<IndexSearchFieldModel>();
                    treeRelation.multiple = false;
                    treeRelation.label = search.__config__.label;
                    treeRelation.prop = search.__vModel__;
                    treeRelation.searchType = 1;
                    pcColumnDesignModel.searchList.Add(treeRelation);
                    break;
            }
        }

        List<IndexSearchFieldModel>? newSearchList = new List<IndexSearchFieldModel>();

        switch (logic)
        {
            case 4:
                newSearchList = pcColumnDesignModel?.searchList?.Union(appColumnDesignModel?.searchList, EqualityHelper<IndexSearchFieldModel>.CreateComparer(it => it.id)).ToList();
                break;
            case 5:
                newSearchList = appColumnDesignModel?.searchList?.Union(pcColumnDesignModel?.searchList, EqualityHelper<IndexSearchFieldModel>.CreateComparer(it => it.id)).ToList();
                break;
        }
        newSearchList?.ForEach(item =>
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.SELECT:
                case JnpfKeyConst.DEPSELECT:
                case JnpfKeyConst.ROLESELECT:
                case JnpfKeyConst.USERSELECT:
                case JnpfKeyConst.USERSSELECT:
                case JnpfKeyConst.COMSELECT:
                case JnpfKeyConst.POSSELECT:
                case JnpfKeyConst.GROUPSELECT:
                    var pc = (pcColumnDesignModel?.searchList.Find(it => it.id.Equals(item.id))?.searchMultiple).ParseToBool();
                    var app = (appColumnDesignModel?.searchList.Find(it => it.prop.Equals(item.prop))?.searchMultiple).ParseToBool();
                    if (pc && app)
                        item.searchMultiple = true;
                    if (!pc && !app)
                        item.searchMultiple = false;
                    else if (pc || app)
                        item.searchMultiple = true;
                    else
                        item.searchMultiple = false;
                    break;
            }
        });
        return newSearchList;
    }

    /// <summary>
    /// 多端列表展示合并.
    /// </summary>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static List<IndexGridFieldModel> GetMultiTerminalListDisplayAndConsolidation(VisualDevEntity templateEntity)
    {
        ColumnDesignModel pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        ColumnDesignModel appColumnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
        return pcColumnDesignModel?.columnList?.Union(appColumnDesignModel?.columnList, EqualityHelper<IndexGridFieldModel>.CreateComparer(it => it.prop)).ToList();
    }

    /// <summary>
    /// 代码生成配置模型.
    /// </summary>
    /// <param name="formDataModel">表单Json包.</param>
    /// <param name="columnDesignModel">列设计模型.</param>
    /// <param name="tableColumnList">数据库表列.</param>
    /// <param name="controls">表单控件列表.</param>
    /// <param name="tableName">表名称.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    public static CodeGenConfigModel GetCodeGenConfigModel(FormDataModel formDataModel, ColumnDesignModel columnDesignModel, List<TableColumnConfigModel> tableColumnList, List<FieldsModel> controls, string tableName, VisualDevEntity templateEntity)
    {
        // 默认排序 没设置以ID排序.
        var defaultSidx = string.Empty;

        // 是否导出
        bool isExport = false;

        // 是否批量删除
        bool isBatchRemove = false;

        // 是否查询条件多选
        bool isSearchMultiple = false;

        // 是否树形表格
        bool isTreeTable = false;

        // 树形表格-父级字段
        string parentField = string.Empty;

        // 树形表格-显示字段
        string treeShowField = string.Empty;

        switch (templateEntity.WebType)
        {
            case 2:
                // 默认排序 没设置以ID排序.
                defaultSidx = columnDesignModel.defaultSidx ?? tableColumnList.Find(t => t.PrimaryKey).ColumnName;
                isExport = columnDesignModel.btnsList.Any(it => it.value == "download");
                isBatchRemove = columnDesignModel.btnsList.Any(it => it.value == "batchRemove");
                isSearchMultiple = tableColumnList.Any(it => it.QueryMultiple && !it.IsAuxiliary);
                break;
        }

        switch (columnDesignModel.type)
        {
            case 5:
                isTreeTable = true;
                parentField = string.Format("{0}_pid", columnDesignModel.parentField);
                treeShowField = columnDesignModel.columnList.Find(it => it.__vModel__.ToLower() != columnDesignModel.parentField.ToLower()).__vModel__;
                break;
            default:
                break;
        }

        // 是否存在上传
        bool isUpload = tableColumnList.Any(it => it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG) || it.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ)));

        // 是否对象映射
        bool isMapper = tableColumnList.Any(it => it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) || it.jnpfKey.Equals(JnpfKeyConst.CASCADER) || it.jnpfKey.Equals(JnpfKeyConst.ADDRESS) || it.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || it.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG) || it.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ) || (it.jnpfKey.Equals(JnpfKeyConst.SELECT) && it.IsMultiple) || (it.jnpfKey.Equals(JnpfKeyConst.USERSELECT) && it.IsMultiple) || (it.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && it.IsMultiple) || (it.jnpfKey.Equals(JnpfKeyConst.DEPSELECT) && it.IsMultiple) || (it.jnpfKey.Equals(JnpfKeyConst.POSSELECT) && it.IsMultiple)));

        // 是否存在单据规则控件
        bool isBillRule = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE));
        if (!isBillRule && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
        {
            foreach (var item in controls.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList())
            {
                if (item.__config__.children.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE)))
                {
                    isBillRule = true;
                    break;
                }
            }
        }

        // 条形码关联字段
        var relationField = new List<TableColumnConfigModel>();
        foreach (var item in controls.FindAll(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.BARCODE) && x.dataType.Equals("relation")))
        {
            if (item.relationField.Contains("_jnpf_"))
            {
                var rField = item.relationField.Split("jnpf_").ToList();
                rField.RemoveAt(0);
                var tt = rField.FirstOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                var ff = rField.LastOrDefault().ReplaceRegex("^f_", string.Empty).ParseToPascalCase();
                relationField.Add(new TableColumnConfigModel() { ColumnName = string.Format("{0}.{1}", tt, ff), RelationColumnName = item.relationField + "_id" });
            }
            else
            {
                relationField.Add(new TableColumnConfigModel() { ColumnName = item.relationField.ParseToPascalCase(), RelationColumnName = item.relationField + "_id" });
            }
        }

        bool isSystemControl = tableColumnList.Any(it => it.IsSystemControl);

        bool isUpdate = tableColumnList.Any(it => it.IsUpdate);

        bool isLogicalDelete = formDataModel.logicalDelete;

        List<CodeGenFunctionModel> function = new List<CodeGenFunctionModel>();

        switch (templateEntity.Type)
        {
            case 3:
                function = CodeGenFunctionHelper.GetPureFormWithProcessMethod();
                break;
            default:
                // 是否启用流程
                switch (templateEntity.EnableFlow)
                {
                    case 1:
                        switch (templateEntity.WebType)
                        {
                            case 1:
                                function = CodeGenFunctionHelper.GetPureFormWithProcessMethod();
                                break;
                            case 2:
                                columnDesignModel.btnsList.RemoveAll(it => it.value.Equals("add"));
                                columnDesignModel.btnsList.Add(new ButtonConfigModel()
                                {
                                    value = "save",
                                });
                                columnDesignModel.columnBtnsList.RemoveAll(it => it.value.Equals("edit"));
                                function = CodeGenFunctionHelper.GetGeneralListWithProcessMethod(columnDesignModel.hasPage, columnDesignModel.btnsList, columnDesignModel.columnBtnsList);
                                break;
                        }

                        break;
                    default:
                        switch (templateEntity.WebType)
                        {
                            case 1:
                                function = CodeGenFunctionHelper.GetPureFormMethod();
                                break;
                            default:
                                function = CodeGenFunctionHelper.GetGeneralListMethod(columnDesignModel.hasPage, columnDesignModel.btnsList, columnDesignModel.columnBtnsList);
                                break;
                        }
                        break;
                }
                break;
        }

        // 树形表格不管有没有导出 强行开双列表(分页与无分页接口)
        switch (columnDesignModel.type)
        {
            case 5:
                switch (function.Any(it => it.FullName.Equals("page") && it.FullName.Equals("noPage")))
                {
                    case false:
                        switch (function.Any(it => it.FullName.Equals("page")))
                        {
                            case true:
                                function.Add(new CodeGenFunctionModel()
                                {
                                    FullName = "noPage",
                                    IsInterface = true,
                                    orderBy = 3,
                                });
                                break;
                            default:
                                function.Add(new CodeGenFunctionModel()
                                {
                                    FullName = "page",
                                    IsInterface = true,
                                    orderBy = 3,
                                });
                                break;
                        }
                        break;
                    case true:
                        function.FindAll(it => it.FullName.Equals("page") || it.FullName.Equals("noPage")).ForEach(item =>
                        {
                            item.IsInterface = true;
                        });
                        break;
                }
                break;
        }

        if (columnDesignModel.type.Equals(3) || columnDesignModel.type.Equals(5)) columnDesignModel.complexHeaderList.Clear();
        var columnList = columnDesignModel.columnList.Copy();
        if (columnDesignModel.type.Equals(4)) columnList = columnList.Where(x => !x.id.ToLower().Contains("tablefield")).ToList();
        return new CodeGenConfigModel()
        {
            NameSpace = formDataModel.areasName,
            BusName = templateEntity.FullName,
            ClassName = formDataModel.className.FirstOrDefault(),
            PrimaryKey = tableColumnList.Find(t => t.PrimaryKey).ColumnName,
            OriginalPrimaryKey = tableColumnList.Find(t => t.PrimaryKey).OriginalColumnName,
            MainTable = tableName.ParseToPascalCase(),
            OriginalMainTableName = tableName,
            hasPage = columnDesignModel.hasPage,
            Function = function,
            TableField = tableColumnList,
            RelationsField = relationField,
            DefaultSidx = defaultSidx,
            IsExport = isExport,
            IsBatchRemove = isBatchRemove,
            IsUpload = isUpload,
            IsTableRelations = false,
            IsMapper = isMapper,
            IsBillRule = isBillRule,
            DbLinkId = templateEntity.DbLinkId,
            FormId = templateEntity.Id,
            WebType = templateEntity.WebType,
            Type = templateEntity.Type,
            EnableFlow = templateEntity.EnableFlow.ParseToBool(),
            IsMainTable = true,
            EnCode = templateEntity.EnCode,
            UseDataPermission = (bool)columnDesignModel?.useDataPermission,
            SearchControlNum = tableColumnList.FindAll(it => it.QueryType.Equals(1) || it.QueryType.Equals(2)).Count(),
            IsAuxiliaryTable = false,
            ExportField = templateEntity.Type == 3 || templateEntity.WebType == 1 ? null : CodeGenExportFieldHelper.ExportColumnField(columnList, columnDesignModel.complexHeaderList),
            FullName = templateEntity.FullName,
            IsConversion = tableColumnList.Any(it => it.IsConversion.Equals(true)),
            IsDetailConversion = tableColumnList.Any(it => it.IsDetailConversion.Equals(true)),
            PrimaryKeyPolicy = formDataModel.primaryKeyPolicy,
            ConcurrencyLock = formDataModel.concurrencyLock,
            HasSuperQuery = columnDesignModel.hasSuperQuery,
            IsUnique = tableColumnList.Any(it => it.IsUnique),
            GroupField = columnDesignModel?.groupField,
            GroupShowField = columnDesignModel?.columnList?.Where(x => x.__vModel__.ToLower() != columnDesignModel?.groupField?.ToLower()).FirstOrDefault()?.__vModel__,
            IsImportData = tableColumnList.Any(it => it.IsImportField.Equals(true)),
            ParsJnpfKeyConstList = CodeGenControlsAttributeHelper.GetParsJnpfKeyConstList(controls, (bool)columnDesignModel?.type.Equals(4)),
            ParsJnpfKeyConstListDetails = CodeGenControlsAttributeHelper.GetParsJnpfKeyConstListDetails(controls),
            ImportDataType = columnDesignModel?.uploaderTemplateJson?.dataType,
            IsSystemControl = isSystemControl,
            IsUpdate = isUpdate,
            IsSearchMultiple = isSearchMultiple,
            IsTreeTable = isTreeTable,
            ParentField = parentField,
            TreeShowField = treeShowField,
            IsLogicalDelete = isLogicalDelete,
            TableType = columnDesignModel.type,
        };
    }

    /// <summary>
    /// 获取关键词搜索条件 拼接.
    /// </summary>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="userOrigin">pc 、 app.</param>
    /// <returns></returns>
    public static string GetCodeGenKeywordSearchColumn(VisualDevEntity templateEntity, string userOrigin)
    {
        var tInfo = new TemplateParsingBase(templateEntity);
        var keywordSearchWhere = string.Empty;
        var columnWhere = string.Empty;
        var columnData = userOrigin.Equals("pc") ? tInfo.ColumnData : tInfo.AppColumnData;
        if (columnData != null && columnData.searchList != null && columnData.searchList.Any(x => x.isKeyword))
        {
            var whereList = new List<string>();
            columnData.searchList.Where(x => x.isKeyword).ToList().ForEach(item =>
            {
                var fieldItem = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.id));
                var tName = fieldItem.__config__.tableName == null ? string.Empty : fieldItem.__config__.tableName;
                var vModel = fieldItem.__vModel__;
                if (vModel.Contains("_jnpf_"))
                {
                    vModel = tName.ParseToPascalCase() + "." + vModel.Split("_jnpf_").Last().ToUpperCase();
                }

                if (fieldItem.__config__.parentVModel.IsNotEmptyOrNull() && vModel.Contains(fieldItem.__config__.parentVModel))
                {
                    tName = fieldItem.__config__.relationTable == null ? string.Empty : fieldItem.__config__.relationTable;
                    vModel = vModel.Replace(fieldItem.__config__.parentVModel + "-", "");
                    if (fieldItem.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) columnWhere = string.Format("it.{0}List.Any(x=>SqlFunc.ToString(x.{1}).Contains(input.jnpfKeyword))", tName.ParseToPascalCase(), vModel.ToUpperCase());
                    else columnWhere = string.Format("it.{0}List.Any(x=>x.{1}.Contains(input.jnpfKeyword))", tName.ParseToPascalCase(), vModel.ToUpperCase());
                }
                else
                {
                    if (fieldItem.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) columnWhere = string.Format("SqlFunc.ToString(it.{0}).Contains(input.jnpfKeyword)", vModel.ToUpperCase());
                    else columnWhere = string.Format("it.{0}.Contains(input.jnpfKeyword)", vModel.ToUpperCase());
                }

                whereList.Add(columnWhere);
            });

            keywordSearchWhere = string.Join(" || ", whereList);
        }

        return keywordSearchWhere;
    }

}
