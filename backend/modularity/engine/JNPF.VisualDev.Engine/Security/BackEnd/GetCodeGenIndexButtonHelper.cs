namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成列表按钮帮助类.
/// </summary>
public class GetCodeGenIndexButtonHelper
{
    /// <summary>
    /// 代码生成单表Index列表列按钮方法.
    /// </summary>
    /// <param name="value">按钮类型.</param>
    /// <param name="primaryKey">主键key.</param>
    /// <param name="primaryKeyPolicy">主键策略.</param>
    /// <param name="isFlow">是否工作流表单.</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <returns></returns>
    public static string IndexColumnButton(string value, string primaryKey, int primaryKeyPolicy, bool isFlow = false, bool isInlineEditor = false)
    {
        string method = string.Empty;
        switch (value)
        {
            case "edit":
                switch (isInlineEditor)
                {
                    case true:
                        method = string.Format("scope.row.rowEdit=true");
                        break;
                    default:
                        switch (isFlow)
                        {
                            case true:
                                method = string.Format("updateHandle(scope.row)");
                                break;
                            default:
                                method = string.Format("addOrUpdateHandle(scope.row.{0})", primaryKey);
                                break;
                        }
                        break;
                }
                break;
            case "remove":
                method = string.Format("handleDel(scope.row.{0})", primaryKey);
                break;
            case "detail":
                switch (isFlow)
                {
                    case true:
                        switch (isInlineEditor)
                        {
                            case true:
                                switch (primaryKeyPolicy)
                                {
                                    case 2:
                                        method = string.Format("goDetail(scope.row.flowTaskId,scope.row.flowState, scope.row.flowId)");
                                        break;
                                    default:
                                        method = string.Format("goDetail(scope.row.{0},scope.row.flowState, scope.row.flowId)", primaryKey);
                                        break;
                                }
                                break;
                            default:
                                method = string.Format("detailHandle(scope.row)", primaryKey);
                                break;
                        }
                        break;
                    default:
                        method = string.Format("goDetail(scope.row.{0})", primaryKey);
                        break;
                }

                break;
        }

        return method;
    }

    /// <summary>
    /// 代码生成单表Index列表头部按钮方法.
    /// </summary>
    /// <param name="value">按钮类型.</param>
    /// <param name="isFlow">是否工作流表单.</param>
    /// <returns></returns>
    public static string IndexTopButton(string value, bool isFlow)
    {
        var method = string.Empty;
        switch (value)
        {
            case "add":
                switch (isFlow)
                {
                    case true:
                        method = "addHandle()";
                        break;
                    default:
                        method = "addOrUpdateHandle()";
                        break;
                }
                break;
            case "download":
                method = "exportData()";
                break;
            case "batchRemove":
                method = "handleBatchRemoveDel()";
                break;
            case "upload":
                method = "handelUpload()";
                break;
            case "batchPrint":
                method = "printDialog()";
                break;
        }

        return method;
    }

    /// <summary>
    /// 代码生成单表Index列表头部按钮方法.
    /// </summary>
    /// <param name="value">按钮类型.</param>
    /// <param name="importUrl">导入url.</param>
    /// <param name="isFlow">是否工作流表单.</param>
    /// <returns></returns>
    public static string IndexTopButton(string value, string importUrl, bool isFlow)
    {
        var method = string.Empty;
        switch (value)
        {
            // 新增
            case "add":
                method = "addHandle()";
                break;

            // 导出
            case "download":
                method = "openExportModal(true, { columnList: state.exportList, selectIds: getSelectRowKeys() })";
                break;

            // 导入
            case "upload":
                switch (isFlow)
                {
                    case true:
                        method = string.Format("openImportModal(true, {{ url: '{0}', enableFlow: 1, flowList: state.flowList  }})", importUrl);
                        break;
                    default:
                        method = string.Format("openImportModal(true, {{ url: '{0}' }})", importUrl);
                        break;
                }
                break;

            // 批量删除
            case "batchRemove":
                method = "handelBatchRemove()";
                break;

            // 批量打印
            case "batchPrint":
                method = "handelBatchPrint()";
                break;
        }

        return method;
    }

    /// <summary>
    /// 代码生成流程Index列表列按钮是否禁用.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string WorkflowIndexColumnButton(string value)
    {
        var disabled = string.Empty;
        switch (value)
        {
            case "edit":
                disabled = ":disabled='[1, 2, 4, 5].indexOf(scope.row.flowState) > -1' ";
                break;
            case "remove":
                disabled = ":disabled='[1, 2, 3, 5].indexOf(scope.row.flowState) > -1' ";
                break;
            case "detail":
                disabled = ":disabled='!scope.row.flowState' ";
                break;
        }

        return disabled;
    }
}