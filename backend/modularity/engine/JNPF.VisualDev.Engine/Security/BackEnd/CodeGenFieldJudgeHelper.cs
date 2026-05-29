using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成表字段判断帮助类.
/// </summary>
public class CodeGenFieldJudgeHelper
{
    /// <summary>
    /// 是否查询列.
    /// </summary>
    /// <param name="searchList">模板内查询列表.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static bool IsColumnQueryWhether(List<IndexSearchFieldModel>? searchList, string fieldName)
    {
        var column = searchList?.Any(s => s.id == fieldName);
        return column ?? false;
    }

    /// <summary>
    /// 列查询类型.
    /// </summary>
    /// <param name="searchList">模板内查询列表.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static int ColumnQueryType(List<IndexSearchFieldModel>? searchList, string fieldName)
    {
        var column = searchList?.Find(s => s.id == fieldName);
        return column?.searchType ?? 0;
    }

    /// <summary>
    /// 列表查询多选.
    /// </summary>
    /// <param name="searchList">模板内查询列表.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static bool ColumnQueryMultiple(List<IndexSearchFieldModel>? searchList, string fieldName)
    {
        var column = searchList?.Find(s => s.id == fieldName);
        return (column?.searchMultiple).ParseToBool();
    }

    /// <summary>
    /// 是否展示列.
    /// </summary>
    /// <param name="columnList">模板内展示字段.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static bool IsShowColumn(List<IndexGridFieldModel>? columnList, string fieldName)
    {
        bool? column = columnList?.Any(s => s.id == fieldName);
        return column ?? false;
    }

    /// <summary>
    /// 获取是否多选.
    /// </summary>
    /// <param name="columnList">模板内控件列表.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static bool IsMultipleColumn(List<FieldsModel> columnList, string fieldName)
    {
        bool isMultiple = false;
        var column = columnList.Find(s => s.__vModel__ == fieldName);
        if (column != null)
        {
            switch (column?.__config__.jnpfKey)
            {
                default:
                    isMultiple = column.multiple;
                    break;
            }
        }

        return isMultiple;
    }

    /// <summary>
    /// 获取是否多选.
    /// </summary>
    /// <param name="column">模板内控件.</param>
    /// <param name="fieldName">字段名称.</param>
    /// <returns></returns>
    public static bool IsMultipleColumn(FieldsModel column, string fieldName)
    {
        bool isMultiple = false;
        if (column != null)
        {
            switch (column?.__config__.jnpfKey)
            {
                default:
                    isMultiple = column.multiple;
                    break;
            }
        }

        return isMultiple;
    }

    /// <summary>
    /// 控制解析.
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public static bool IsControlParsing(FieldsModel column)
    {
        bool isExist = false;
        switch (column?.__config__.jnpfKey)
        {
            case JnpfKeyConst.RELATIONFORM:
            case JnpfKeyConst.POPUPSELECT:
            case JnpfKeyConst.USERSSELECT:
                isExist = true;
                break;
        }
        return isExist;
    }

    /// <summary>
    /// 是否datetime.
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static bool IsDateTime(FieldsModel? fields)
    {
        bool isDateTime = false;
        if (fields?.__config__.jnpfKey == JnpfKeyConst.DATE || fields?.__config__.jnpfKey == JnpfKeyConst.TIME)
            isDateTime = true;
        return isDateTime;
    }

    /// <summary>
    /// 是否副表datetime.
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static bool IsSecondaryTableDateTime(FieldsModel? fields)
    {
        bool isDateTime = false;
        if (fields?.__config__.jnpfKey == JnpfKeyConst.DATE || fields?.__config__.jnpfKey == JnpfKeyConst.TIME || fields?.__config__.jnpfKey == JnpfKeyConst.CREATETIME || fields?.__config__.jnpfKey == JnpfKeyConst.MODIFYTIME)
            isDateTime = true;
        return isDateTime;
    }

    /// <summary>
    /// 是否子表映射.
    /// </summary>
    /// <param name="tableColumnConfig">表列.</param>
    /// <returns></returns>
    public static bool IsChildTableMapper(List<TableColumnConfigModel> tableColumnConfig)
    {
        bool isOpen = false;
        tableColumnConfig.ForEach(item =>
        {
            switch (item.jnpfKey)
            {
                case JnpfKeyConst.CASCADER:
                case JnpfKeyConst.ADDRESS:
                case JnpfKeyConst.COMSELECT:
                case JnpfKeyConst.UPLOADIMG:
                case JnpfKeyConst.UPLOADFZ:
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.TIME:
                    isOpen = true;
                    break;
                case JnpfKeyConst.SELECT:
                case JnpfKeyConst.USERSELECT:
                case JnpfKeyConst.TREESELECT:
                case JnpfKeyConst.DEPSELECT:
                case JnpfKeyConst.POSSELECT:
                    switch (item.IsMultiple)
                    {
                        case true:
                            isOpen = true;
                            break;
                    }
                    break;
            }
        });
        return isOpen;
    }
}