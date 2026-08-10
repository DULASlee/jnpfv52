using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.VisualDev.Engine.Security;
using JNPF.VisualDev.Entitys.Enum;

namespace JNPF.CodeGen.Helpers;

/// <summary>
/// Pure helpers for CodeGenService.TemplatesDataAggregation
/// (column JSON patches, permission flag, generation-model judgment,
/// path switch, table-relation DTO shaping). DB / view-engine / file IO stay in the service.
/// </summary>
public static class TemplatesDataAggregationHelpers
{
    /// <summary>
    /// Force pure-form + inline-editor → list type; clear complex headers for group/tree.
    /// Uses in-memory <paramref name="pcColumnType"/> for both gates (legacy fidelity).
    /// </summary>
    public static string ApplyColumnDataAggregationPatches(string? columnDataJson, int webType, int pcColumnType)
    {
        var json = columnDataJson;

        // 既是行内编辑又是纯表单 强制改成普通列表.
        if (webType.Equals(1) && pcColumnType.Equals(4))
        {
            var columnData = json.ToObject<Dictionary<string, object>>();
            columnData["type"] = 1;
            json = columnData.ToJsonString();
        }

        // 分组和树形表格去掉复杂表头
        if (pcColumnType.Equals(3) || pcColumnType.Equals(5))
        {
            var columnData = json.ToObject<Dictionary<string, object>>();
            columnData["complexHeaderList"] = new List<object>();
            json = columnData.ToJsonString();
        }

        return json;
    }

    /// <summary>
    /// PC/App data-permission OR, then pure-form (WebType=1) forces false.
    /// </summary>
    public static bool ResolveUseDataPermission(bool pcUseDataPermission, bool appUseDataPermission, int webType)
    {
        bool useDataPermission;
        if (pcUseDataPermission && appUseDataPermission)
            useDataPermission = true;
        else if (!pcUseDataPermission && appUseDataPermission)
            useDataPermission = true;
        else if (pcUseDataPermission && !appUseDataPermission)
            useDataPermission = true;
        else
            useDataPermission = false;

        switch (webType)
        {
            case 1:
                useDataPermission = false;
                break;
        }

        return useDataPermission;
    }

    /// <summary>
    /// Type 4/5 and not pure-form → run unified form/control handlers.
    /// </summary>
    public static bool ShouldApplyUnifiedFormControls(int type, int webType)
    {
        switch (type)
        {
            case 4:
            case 5:
                switch (webType)
                {
                    case 1:
                        return false;
                    default:
                        return true;
                }
            default:
                return false;
        }
    }

    /// <summary>
    /// 判断生成模式：1-纯主表、2-主带子、3-主带副、4-主带副与子.
    /// </summary>
    public static GeneratePatterns JudgeGenerationModel(List<DbTableRelationModel> tableRelation, List<FieldsModel> controls)
    {
        var codeModel = GeneratePatterns.PrimaryTable;

        if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")) && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.PrimarySecondary;
        else if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")))
            codeModel = GeneratePatterns.MainBeltVice;
        else if (tableRelation.Count > 1 && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.MainBelt;

        switch (codeModel)
        {
            case GeneratePatterns.MainBelt:
                // 在子表模式下 设计子表控件数量对不上表扣除主表后数量 强制定义为主子副模式
                if (controls.Count(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) < tableRelation.Count - 1)
                    codeModel = GeneratePatterns.PrimarySecondary;
                break;
        }

        return codeModel;
    }

    /// <summary>
    /// Resolve main-table backend target/template paths.
    /// Returns null when legacy switch leaves prior lists unchanged
    /// (WebType=2 + TableType=4 + Type=3 empty break).
    /// </summary>
    public static (List<string> TargetPathList, List<string> TemplatePathList)? ResolveMainBackendPaths(
        int webType,
        int type,
        int enableFlow,
        int tableType,
        string className,
        string fileName,
        bool isMapper,
        string genModel)
    {
        List<string>? targetPathList = null;
        List<string>? templatePathList = null;

        switch (webType)
        {
            case 1:
                switch (type)
                {
                    case 3:
                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(className, fileName, isMapper);
                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList(genModel, isMapper);
                        break;
                    default:
                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(className, fileName, webType, enableFlow, tableType == 4, isMapper);
                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList(genModel, webType, enableFlow, isMapper);
                        break;
                }
                break;
            case 2:
                switch (tableType)
                {
                    case 4:
                        switch (type)
                        {
                            case 3:
                                break;
                            default:
                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(className, fileName, webType, enableFlow, tableType == 4, isMapper);
                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList(genModel, webType, enableFlow, isMapper);
                                break;
                        }
                        break;
                    default:
                        switch (type)
                        {
                            case 3:
                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(className, fileName, isMapper);
                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList(genModel, isMapper);
                                break;
                            default:
                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(className, fileName, webType, enableFlow, tableType == 4, isMapper);
                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList(genModel, webType, enableFlow, isMapper);
                                break;
                        }
                        break;
                }
                break;
        }

        if (targetPathList == null || templatePathList == null)
            return null;

        return (targetPathList, templatePathList);
    }

    /// <summary>
    /// Split typeId=0 relations into sub-table vs secondary (auxiliary) by TABLE control presence.
    /// </summary>
    public static (List<DbTableRelationModel> SubTable, List<DbTableRelationModel> SecondaryTable) SplitSubAndSecondaryTables(
        IEnumerable<DbTableRelationModel> childRelations,
        List<FieldsModel> controls)
    {
        var subTable = new List<DbTableRelationModel>();
        var secondaryTable = new List<DbTableRelationModel>();

        foreach (DbTableRelationModel? item in childRelations)
        {
            switch (controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table)))
            {
                case true:
                    subTable.Add(item);
                    break;
                default:
                    secondaryTable.Add(item);
                    break;
            }
        }

        return (subTable, secondaryTable);
    }

    /// <summary>
    /// Shape child-table CodeGenTableRelationsModel (MainBelt / PrimarySecondary).
    /// </summary>
    public static CodeGenTableRelationsModel BuildChildTableRelation(
        DbTableRelationModel item,
        CodeGenConfigModel codeGenConfigModel,
        string controlId,
        int tableNo)
    {
        return new CodeGenTableRelationsModel
        {
            ClassName = item.className,
            OriginalTableName = item.table,
            RelationTable = item.relationTable,
            TableName = item.table.ParseToPascalCase(),
            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
            OriginalRelationField = item.relationField,
            ControlTableComment = codeGenConfigModel.BusName,
            TableComment = item.tableName,
            ChilderColumnConfigList = codeGenConfigModel.TableField,
            ChilderColumnConfigListCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && !it.ForeignKeyField && it.jnpfKey != null).Count(),
            TableNo = tableNo,
            ControlModel = controlId,
            IsQueryWhether = codeGenConfigModel.TableField.Any(it => it.QueryWhether),
            IsShowField = codeGenConfigModel.TableField.Any(it => it.IsShow),
            IsUnique = codeGenConfigModel.TableField.Any(it => it.IsUnique),
            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
        };
    }

    /// <summary>
    /// Shape auxiliary-table CodeGenTableRelationsModel (MainBeltVice / PrimarySecondary).
    /// </summary>
    public static CodeGenTableRelationsModel BuildAuxiliaryTableRelation(
        DbTableRelationModel item,
        CodeGenConfigModel codeGenConfigModel,
        int tableNo,
        int fieldCount)
    {
        return new CodeGenTableRelationsModel
        {
            ClassName = codeGenConfigModel.ClassName,
            OriginalTableName = item.table,
            RelationTable = item.relationTable,
            TableName = item.table.ParseToPascalCase(),
            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
            ChilderColumnConfigList = codeGenConfigModel.TableField,
            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
            OriginalRelationField = item.relationField,
            TableComment = item.tableName,
            TableNo = tableNo,
            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
            IsSystemControl = codeGenConfigModel.TableField.Any(it => it.IsSystemControl),
            IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate),
            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
            FieldCount = fieldCount,
        };
    }

    /// <summary>
    /// Whether child-table primary keys should be written onto TABLE controls before front-end gen.
    /// </summary>
    public static bool NeedsChildTablePrimaryKeyInjection(GeneratePatterns modelType)
        => modelType.Equals(GeneratePatterns.MainBelt) || modelType.Equals(GeneratePatterns.PrimarySecondary);

    /// <summary>
    /// Inject Pascal/lower primary-key names onto TABLE controls from ctPrimaryKey map.
    /// </summary>
    public static void ApplyChildTablePrimaryKeys(List<FieldsModel> controls, IReadOnlyDictionary<string, string> ctPrimaryKey)
    {
        foreach (var item in controls)
        {
            if (item.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                item.TablePrimaryKey = ctPrimaryKey[item.__config__.tableName].ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
        }
    }
}
