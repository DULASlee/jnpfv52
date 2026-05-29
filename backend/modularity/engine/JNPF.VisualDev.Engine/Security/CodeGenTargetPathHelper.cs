using JNPF.Common.Configuration;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成目标路径帮助类.
/// </summary>
public class CodeGenTargetPathHelper
{
    #region 前端相关文件

    /// <summary>
    /// 前端页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="enableFlow">是否开启流程(0-否,1-是).</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns></returns>
    public static List<string> FrontEndTargetPathList(string tableName, string fileName, int webType, int enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "PC", tableName, "index.vue");
        var formPath = Path.Combine(frontendPath, "html", "PC", tableName, "Form.vue");
        var detailPath = Path.Combine(frontendPath, "html", "PC", tableName, "Detail.vue");
        var exportJsonPath = Path.Combine(frontendPath, "fff", "flowForm.fff");
        var columnJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "columnList.js");
        var superQueryJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "superQueryJson.js");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                if (enableFlow == 1)
                    pathList.Add(exportJsonPath);
                break;
            case 2:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                switch (enableFlow)
                {
                    case 0:
                        if (isDetail)
                            pathList.Add(detailPath);
                        break;
                    case 1:
                        pathList.Add(exportJsonPath);
                        break;
                }
                pathList.Add(columnJsonPath);
                if (hasSuperQuery)
                    pathList.Add(superQueryJsonPath);
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 前端页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="enableFlow">是否开启流程(0-否,1-是).</param>
    /// <param name="hasDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns></returns>
    public static List<string> FrontEndTargetPathList(string tableName, string fileName, int webType, bool enableFlow, bool hasDetail = false, bool hasSuperQuery = false)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "PC", tableName, "index.vue");
        var formPath = Path.Combine(frontendPath, "html", "PC", tableName, webType == 1 && !enableFlow ? "FormPopup.vue" : "Form.vue");
        var detailPath = Path.Combine(frontendPath, "html", "PC", tableName, "Detail.vue");
        var exportJsonPath = Path.Combine(frontendPath, "fff", "flowForm.fff");
        var apiJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "api.ts");
        var columnJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "columnList.ts");
        var searchJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "searchList.ts");
        var superQueryJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "superQueryJson.ts");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                if (enableFlow)
                    pathList.Add(exportJsonPath);
                pathList.Add(apiJsonPath);
                break;
            case 2:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                switch (enableFlow)
                {
                    case false:
                        if (hasDetail)
                            pathList.Add(detailPath);
                        break;
                    case true:
                        pathList.Add(exportJsonPath);
                        break;
                }
                pathList.Add(apiJsonPath);
                pathList.Add(columnJsonPath);
                pathList.Add(searchJsonPath);
                if (hasSuperQuery)
                    pathList.Add(superQueryJsonPath);
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 前端页面模板文件路径集合.
    /// </summary>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="enableFlow">是否开启流程(0-否,1-是).</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns>返回前端模板地址列表.</returns>
    public static List<string> FrontEndTemplatePathList(int webType, int enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                switch (enableFlow)
                {
                    case 1:
                        pathList.Add(Path.Combine(templatePath, "PureForm", "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "WorkflowForm.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "ExportJson.json.vm"));
                        break;
                    default:
                        pathList.Add(Path.Combine(templatePath, "Form.vue.vm"));
                        break;
                }
                break;
            case 2:
                switch (enableFlow)
                {
                    case 1:
                        pathList.Add(Path.Combine(templatePath, "WorkflowIndex.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "WorkflowForm.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "ExportJson.json.vm"));
                        break;
                    default:
                        pathList.Add(Path.Combine(templatePath, "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "Form.vue.vm"));
                        if (isDetail)
                            pathList.Add(Path.Combine(templatePath, "Detail.vue.vm"));
                        break;
                }
                pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
                if (hasSuperQuery)
                    pathList.Add(Path.Combine(templatePath, "superQueryJson.js.vm"));
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 前端页面模板文件路径集合.
    /// </summary>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="enableFlow">是否开启流程(0-否,1-是).</param>
    /// <param name="hasDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns>返回前端模板地址列表.</returns>
    public static List<string> FrontEndTemplatePathList(int webType, bool enableFlow, bool hasDetail = false, bool hasSuperQuery = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                switch (enableFlow)
                {
                    case true:
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "PureForm", "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "Form.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "ExportJson.json.vm"));
                        break;
                    default:
                        pathList.Add(Path.Combine(templatePath, "vue3", "PureForm", "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "PureForm", "Form.vue.vm"));
                        break;
                }
                pathList.Add(Path.Combine(templatePath, "vue3", "api.ts.vm"));
                break;
            case 2:
                switch (enableFlow)
                {
                    case true:
                        pathList.Add(Path.Combine(templatePath, "vue3", "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "Form.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "ExportJson.json.vm"));
                        break;
                    default:
                        pathList.Add(Path.Combine(templatePath, "vue3", "index.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "Form.vue.vm"));
                        if (hasDetail)
                            pathList.Add(Path.Combine(templatePath, "vue3", "Detail.vue.vm"));
                        break;
                }
                pathList.Add(Path.Combine(templatePath, "vue3", "api.ts.vm"));
                pathList.Add(Path.Combine(templatePath, "vue3", "columnList.ts.vm"));
                pathList.Add(Path.Combine(templatePath, "vue3", "searchList.ts.vm"));
                if (hasSuperQuery)
                    pathList.Add(Path.Combine(templatePath, "vue3", "superQueryJson.ts.vm"));
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 前端行内编辑页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns></returns>
    public static List<string> FrontEndInlineEditorTargetPathList(string tableName, string fileName, int enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "PC", tableName, "index.vue");
        var extraForm = Path.Combine(frontendPath, "html", "PC", tableName, "extraForm.vue");
        var detailPath = Path.Combine(frontendPath, "html", "PC", tableName, "Detail.vue");
        var formPath = Path.Combine(frontendPath, "html", "PC", tableName, "Form.vue");
        var exportJsonPath = Path.Combine(frontendPath, "fff", "flowForm.fff");
        var columnJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "columnList.js");
        var superQueryJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "superQueryJson.js");
        var pathList = new List<string>();

        pathList.Add(indexPath);
        switch (enableFlow)
        {
            case 0:
                pathList.Add(extraForm);
                if (isDetail)
                    pathList.Add(detailPath);
                break;
            default:
                if (isDetail)
                    pathList.Add(formPath);
                pathList.Add(exportJsonPath);
                break;
        }
        pathList.Add(columnJsonPath);
        if (hasSuperQuery)
            pathList.Add(superQueryJsonPath);

        return pathList;
    }

    /// <summary>
    /// 前端行内编辑页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns></returns>
    public static List<string> FrontEndInlineEditorTargetPathList(string tableName, string fileName, bool enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "PC", tableName, "index.vue");
        var extraForm = Path.Combine(frontendPath, "html", "PC", tableName, "extraForm.vue");
        var detailPath = Path.Combine(frontendPath, "html", "PC", tableName, "Detail.vue");
        var formPath = Path.Combine(frontendPath, "html", "PC", tableName, "Form.vue");
        var exportJsonPath = Path.Combine(frontendPath, "fff", "flowForm.fff");
        var apiJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "api.ts");
        var columnJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "columnList.ts");
        var searchJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "searchList.ts");
        var superQueryJsonPath = Path.Combine(frontendPath, "html", "PC", tableName, "helper", "superQueryJson.ts");
        var pathList = new List<string>();
        pathList.Add(indexPath);
        switch (enableFlow)
        {
            case false:
                pathList.Add(extraForm);
                if (isDetail)
                    pathList.Add(detailPath);
                break;
            default:
                if (isDetail)
                    pathList.Add(formPath);
                pathList.Add(exportJsonPath);
                break;
        }
        pathList.Add(apiJsonPath);
        pathList.Add(columnJsonPath);
        pathList.Add(searchJsonPath);
        if (hasSuperQuery)
            pathList.Add(superQueryJsonPath);

        return pathList;
    }

    /// <summary>
    /// 前端行内编辑页面模板文件路径集合.
    /// </summary>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns>返回前端模板地址列表.</returns>
    public static List<string> FrontEndInlineEditorTemplatePathList(bool enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        var pathList = new List<string>();
        pathList.Add(Path.Combine(templatePath, "vue3", "InlineEditing", "index.vue.vm"));
        switch (enableFlow)
        {
            case false:
                pathList.Add(Path.Combine(templatePath, "vue3", "InlineEditing", "Form.vue.vm"));
                if (isDetail)
                    pathList.Add(Path.Combine(templatePath, "vue3", "Detail.vue.vm"));
                break;
            default:
                if (isDetail)
                    pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "Form.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "ExportJson.json.vm"));
                break;
        }
        pathList.Add(Path.Combine(templatePath, "vue3", "api.ts.vm"));
        pathList.Add(Path.Combine(templatePath, "vue3", "columnList.ts.vm"));
        pathList.Add(Path.Combine(templatePath, "vue3", "searchList.ts.vm"));
        if (hasSuperQuery)
            pathList.Add(Path.Combine(templatePath, "vue3", "superQueryJson.ts.vm"));

        return pathList;
    }

    /// <summary>
    /// 前端行内编辑页面模板文件路径集合.
    /// </summary>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <param name="isDetail">是否有详情.</param>
    /// <param name="hasSuperQuery">高级查询.</param>
    /// <returns>返回前端模板地址列表.</returns>
    public static List<string> FrontEndInlineEditorTemplatePathList(int enableFlow, bool isDetail = false, bool hasSuperQuery = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        var pathList = new List<string>();
        switch (enableFlow)
        {
            case 0:
                pathList.Add(Path.Combine(templatePath, "editorIndex.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "extraForm.vue.vm"));
                if (isDetail)
                    pathList.Add(Path.Combine(templatePath, "Detail.vue.vm"));
                break;
            case 1:
                pathList.Add(Path.Combine(templatePath, "editorWorkflowIndex.vue.vm"));
                if (isDetail)
                    pathList.Add(Path.Combine(templatePath, "WorkflowForm.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "ExportJson.json.vm"));
                break;
        }
        pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
        if (hasSuperQuery)
            pathList.Add(Path.Combine(templatePath, "superQueryJson.js.vm"));

        return pathList;
    }

    /// <summary>
    /// App前端带流程页面模板文件路径集合.
    /// </summary>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="isVue3">是否vue3.</param>
    /// <returns></returns>
    public static List<string> AppFrontEndWorkflowTemplatePathList(int webType, bool isVue3 = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        if (isVue3)
            templatePath = Path.Combine(templatePath, "vue3");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                if (isVue3)
                {
                    templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
                    pathList.Add(Path.Combine(templatePath, "PureForm", "appWorkflowIndex.vue.vm"));
                    templatePath = Path.Combine(templatePath, "vue3");
                }
                else
                    pathList.Add(Path.Combine(templatePath, "PureForm", "appWorkflowIndex.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "appWorkflowForm.vue.vm"));
                break;
            case 2:
                pathList.Add(Path.Combine(templatePath, "appWorkflowIndex.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "appWorkflowForm.vue.vm"));
                if (isVue3)
                {
                    templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
                    pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
                }
                else
                    pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 设置App前端带流程页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <returns></returns>
    public static List<string> AppFrontEndWorkflowTargetPathList(string tableName, string fileName, int webType)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "App", "index", tableName, "index.vue");
        var formPath = Path.Combine(frontendPath, "html", "App", "form", tableName, "index.vue");
        var columnJsonPath = Path.Combine(frontendPath, "html", "App", "index", tableName, "columnList.js");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                break;
            case 2:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                pathList.Add(columnJsonPath);
                break;
        }

        return pathList;
    }

    /// <summary>
    /// App前端页面模板文件路径集合.
    /// </summary>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="isDetail">是否开启详情.</param>
    /// <param name="isVue3">是否vue3.</param>
    /// <returns></returns>
    public static List<string> AppFrontEndTemplatePathList(int webType, bool isDetail, bool isVue3 = false)
    {
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        if (isVue3)
            templatePath = Path.Combine(templatePath, "vue3");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                pathList.Add(Path.Combine(templatePath, "appForm.vue.vm"));
                break;
            case 2:
                pathList.Add(Path.Combine(templatePath, "appIndex.vue.vm"));
                pathList.Add(Path.Combine(templatePath, "appForm.vue.vm"));
                if (isDetail)
                    pathList.Add(Path.Combine(templatePath, "appDetail.vue.vm"));
                if (isVue3)
                {
                    templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
                    pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
                }
                else
                {
                    pathList.Add(Path.Combine(templatePath, "columnList.js.vm"));
                }
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 设置App前端页面生成文件路径.
    /// </summary>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="isDetail">是否开启详情.</param>
    /// <returns></returns>
    public static List<string> AppFrontEndTargetPathList(string tableName, string fileName, int webType, bool isDetail)
    {
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "App", tableName, "index.vue");
        var formPath = Path.Combine(frontendPath, "html", "App", tableName, "form.vue");
        var detailPath = Path.Combine(frontendPath, "html", "App", tableName, "detail.vue");
        var columnJsonPath = Path.Combine(frontendPath, "html", "App", tableName, "columnList.js");
        var pathList = new List<string>();
        switch (webType)
        {
            case 1:
                pathList.Add(indexPath);
                break;
            case 2:
                pathList.Add(indexPath);
                pathList.Add(formPath);
                if (isDetail)
                    pathList.Add(detailPath);
                pathList.Add(columnJsonPath);
                break;
        }

        return pathList;
    }

    /// <summary>
    /// 流程前端页面模板文件路径集合.
    /// </summary>
    /// <param name="logicType">逻辑类型4-pc,5-app.</param>
    /// <param name="vueVersion">vue版本.</param>
    /// <returns></returns>
    public static List<string> FlowFrontEndTemplatePathList(int logicType, int vueVersion)
    {
        var pathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        switch (vueVersion)
        {
            case 3:
                switch (logicType)
                {
                    case 4:
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "Form.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "vue3", "WorkFlow", "ExportJson.json.vm"));
                        break;
                    case 5:
                        pathList.Add(Path.Combine(templatePath, "vue3", "appWorkflowForm.vue.vm"));
                        pathList.Add(Path.Combine(templatePath, "ExportJson.json.vm"));
                        break;
                }

                break;
            default:
                switch (logicType)
                {
                    case 4:
                        pathList.Add(Path.Combine(templatePath, "WorkflowForm.vue.vm"));
                        break;
                    case 5:
                        pathList.Add(Path.Combine(templatePath, "appWorkflowForm.vue.vm"));
                        break;
                }
                pathList.Add(Path.Combine(templatePath, "ExportJson.json.vm"));
                break;
        }
        return pathList;
    }

    /// <summary>
    /// 流程前端页面生成文件路径.
    /// </summary>
    /// <param name="logicType">逻辑类型4-pc,5-app.</param>
    /// <param name="tableName">主表名称.</param>
    /// <param name="fileName">压缩包名称.</param>
    /// <returns></returns>
    public static List<string> FlowFrontEndTargetPathList(int logicType, string tableName, string fileName)
    {
        var pathList = new List<string>();
        var frontendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var indexPath = Path.Combine(frontendPath, "html", "PC", tableName, "index.vue");
        var indexAppPath = Path.Combine(frontendPath, "html", "APP", tableName, "index.vue");
        var exportJsonPath = Path.Combine(frontendPath, "fff", "flowForm.fff");
        switch (logicType)
        {
            case 4:
                pathList.Add(indexPath);
                break;
            case 5:
                pathList.Add(indexAppPath);
                break;
        }
        pathList.Add(exportJsonPath);
        return pathList;
    }

    #endregion

    #region 单主表相关文件

    /// <summary>
    /// 后端模板文件路径集合.
    /// </summary>
    /// <param name="genModel">SingleTable-单主表,MainBelt-主带子,,,.</param>
    /// <param name="webType">生成模板类型（1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="enableFlow">是否开启工作流.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <returns></returns>
    public static List<string> BackendTemplatePathList(string genModel, int webType, int enableFlow, bool isMapper)
    {
        List<string> templatePathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        switch (webType)
        {
            case 1:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Service.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, "IService.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                if (isMapper)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                switch (enableFlow)
                {
                    case 1:
                        templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                        break;
                }
                break;
            case 2:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Service.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, "IService.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                if (isMapper)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                switch (enableFlow)
                {
                    case 0:
                        templatePathList.Add(Path.Combine(templatePath, "UpInput.cs.vm"));
                        break;
                }
                templatePathList.Add(Path.Combine(templatePath, genModel, "ListQueryInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "ListOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "DetailOutput.cs.vm"));
                break;
        }

        return templatePathList;
    }

    /// <summary>
    /// 后端主表生成文件路径.
    /// </summary>
    /// <param name="tableName">表名.</param>
    /// <param name="fileName">文件价名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="enableFlow">是否开启工作流.</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <returns></returns>
    public static List<string> BackendTargetPathList(string tableName, string fileName, int webType, int enableFlow, bool isInlineEditor, bool isMapper)
    {
        List<string> targetPathList = new List<string>();
        var backendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var servicePath = Path.Combine(backendPath, "Controller", tableName, tableName + "Service.cs");
        var iservicePath = Path.Combine(backendPath, "Controller", tableName, "I" + tableName + "Service.cs");
        var entityPath = Path.Combine(backendPath, "Models", "Entity", tableName, tableName + "Entity.cs");
        var mapperPath = Path.Combine(backendPath, "Models", "Mapper", tableName, tableName + "Mapper.cs");
        var inputCrPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "CrInput.cs");
        var inputUpPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "UpInput.cs");
        var inputListQueryPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "ListQueryInput.cs");
        var outputInfoPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "InfoOutput.cs");
        var outputListPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "ListOutput.cs");
        var outputDetailPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "DetailOutput.cs");
        var inlineEditorListPath = Path.Combine(backendPath, "Models", "Dto", tableName, tableName + "InlineEditorOutput.cs");
        switch (webType)
        {
            case 1:
                targetPathList.Add(servicePath);
                targetPathList.Add(iservicePath);
                targetPathList.Add(entityPath);
                if (isMapper)
                    targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                switch (enableFlow)
                {
                    case 1:
                        targetPathList.Add(outputInfoPath);
                        break;
                }
                break;
            case 2:
                targetPathList.Add(servicePath);
                targetPathList.Add(iservicePath);
                targetPathList.Add(entityPath);
                if (isMapper)
                    targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                switch (enableFlow)
                {
                    case 0:
                        targetPathList.Add(inputUpPath);
                        break;
                }
                targetPathList.Add(inputListQueryPath);
                targetPathList.Add(outputInfoPath);
                targetPathList.Add(outputListPath);
                if (isInlineEditor)
                    targetPathList.Add(inlineEditorListPath);
                targetPathList.Add(outputDetailPath);
                break;
        }

        return targetPathList;
    }

    /// <summary>
    /// 后端行内编辑模板文件路径集合.
    /// </summary>
    /// <param name="genModel">SingleTable-单主表,MainBelt-主带子,,,.</param>
    /// <param name="webType">生成模板类型（1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="enableFlow">是否开启工作流.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <returns></returns>
    public static List<string> BackendInlineEditorTemplatePathList(string genModel, int webType, int enableFlow, bool isMapper)
    {
        List<string> templatePathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        switch (webType)
        {
            case 2:
                templatePathList.Add(Path.Combine(templatePath, genModel, "InlineEditor", "Service.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, "IService.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                if (isMapper)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                switch (enableFlow)
                {
                    case 0:
                        templatePathList.Add(Path.Combine(templatePath, "UpInput.cs.vm"));
                        break;
                }
                templatePathList.Add(Path.Combine(templatePath, genModel, "ListQueryInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InlineEditor", "ListOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InlineEditor", "InlineEditorOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "DetailOutput.cs.vm"));
                break;
        }

        return templatePathList;
    }

    #endregion

    #region 后端子表

    /// <summary>
    /// 后端模板文件路径集合.
    /// </summary>
    /// <param name="genModel">SingleTable-单主表,MainBelt-主带子,,,.</param>
    /// <param name="webType">生成模板类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="type">模板类型.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <param name="isShowSubTableField">是否展示子表字段.</param>
    /// <returns></returns>
    public static List<string> BackendChildTableTemplatePathList(string genModel, int webType, int type, bool isMapper, bool isShowSubTableField)
    {
        List<string> templatePathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");
        switch (webType)
        {
            case 1:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                if (isMapper)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                break;
            case 2:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                if (isMapper)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, "UpInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                if (isShowSubTableField)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "ListOutput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "DetailOutput.cs.vm"));
                break;
        }

        return templatePathList;
    }

    /// <summary>
    /// 后端主表生成文件路径.
    /// </summary>
    /// <param name="tableName">表名.</param>
    /// <param name="fileName">文件价名称.</param>
    /// <param name="webType">页面类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="type">模板类型.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <param name="isShowSubTableField">是否展示子表字段.</param>
    /// <returns></returns>
    public static List<string> BackendChildTableTargetPathList(string tableName, string fileName, int webType, int type, bool isMapper, bool isShowSubTableField)
    {
        List<string> targetPathList = new List<string>();
        var backendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var entityPath = Path.Combine(backendPath, "Models", "Entity", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "Entity.cs");
        var mapperPath = Path.Combine(backendPath, "Models", "Mapper", tableName, tableName + "Mapper.cs");
        var inputCrPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "CrInput.cs");
        var inputUpPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "UpInput.cs");
        var outputInfoPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "InfoOutput.cs");
        var outputListPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "ListOutput.cs");
        var inputListQueryPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "ListQueryInput.cs");
        var outputDetailPath = Path.Combine(backendPath, "Models", "Dto", (type == 3 ? "WorkFlowForm\\" : string.Empty) + tableName, tableName + "DetailOutput.cs");
        switch (webType)
        {
            case 1:
                targetPathList.Add(entityPath);
                if (isMapper)
                    targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                targetPathList.Add(outputInfoPath);
                break;
            case 2:
                targetPathList.Add(entityPath);
                if (isMapper)
                    targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                targetPathList.Add(inputUpPath);
                targetPathList.Add(outputInfoPath);
                if (isShowSubTableField)
                    targetPathList.Add(outputListPath);
                targetPathList.Add(outputDetailPath);
                break;
        }

        return targetPathList;
    }

    #endregion

    #region 后端副表

    /// <summary>
    /// 后端副表生成文件路径.
    /// </summary>
    /// <param name="tableName">表名.</param>
    /// <param name="fileName">文件价名称.</param>
    /// <param name="webType">生成模板类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="type">模板类型.</param>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <returns></returns>
    public static List<string> BackendAuxiliaryTargetPathList(string tableName, string fileName, int webType, int type, int enableFlow)
    {
        List<string> targetPathList = new List<string>();
        var backendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var entityPath = Path.Combine(backendPath, "Models", "Entity", type == 3 ? "WorkFlowForm\\" + tableName : tableName, tableName + "Entity.cs");
        var mapperPath = Path.Combine(backendPath, "Models", "Mapper", tableName, tableName + "Mapper.cs");
        var inputCrPath = Path.Combine(backendPath, "Models", "Dto", type == 3 ? "WorkFlowForm\\" + tableName : tableName, tableName + "CrInput.cs");
        var outputInfoPath = Path.Combine(backendPath, "Models", "Dto", type == 3 ? "WorkFlowForm\\" + tableName : tableName, tableName + "InfoOutput.cs");

        switch (webType)
        {
            case 1:
                targetPathList.Add(entityPath);
                targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                if (enableFlow == 1 || type == 3)
                    targetPathList.Add(outputInfoPath);
                break;
            default:
                targetPathList.Add(entityPath);
                targetPathList.Add(mapperPath);
                targetPathList.Add(inputCrPath);
                targetPathList.Add(outputInfoPath);
                break;
        }

        return targetPathList;
    }

    /// <summary>
    /// 后端副表模板文件路径集合.
    /// </summary>
    /// <param name="genModel">SingleTable-单主表,MainBelt-主带子,,,.</param>
    /// <param name="webType">生成模板类型（1、纯表单，2、表单加列表）.</param>
    /// <param name="enableFlow">是否开启流程.</param>
    /// <returns></returns>
    public static List<string> BackendAuxiliaryTemplatePathList(string genModel, int webType, int type, int enableFlow)
    {
        List<string> templatePathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");

        switch (webType)
        {
            case 1:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                if (enableFlow == 1 || type == 3)
                    templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                break;
            default:
                templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
                templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));
                break;
        }

        return templatePathList;
    }

    #endregion

    #region 后端流程

    /// <summary>
    /// 后端流程模板文件路径集合.
    /// </summary>
    /// <param name="genModel">SingleTable-单主表,MainBelt-主带子,,,.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <returns></returns>
    public static List<string> BackendFlowTemplatePathList(string genModel, bool isMapper)
    {
        List<string> templatePathList = new List<string>();
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Template");

        templatePathList.Add(Path.Combine(templatePath, genModel, "Service.cs.vm"));
        templatePathList.Add(Path.Combine(templatePath, "IService.cs.vm"));
        templatePathList.Add(Path.Combine(templatePath, genModel, "Entity.cs.vm"));
        if (isMapper)
            templatePathList.Add(Path.Combine(templatePath, genModel, "Mapper.cs.vm"));
        templatePathList.Add(Path.Combine(templatePath, genModel, "CrInput.cs.vm"));
        templatePathList.Add(Path.Combine(templatePath, genModel, "InfoOutput.cs.vm"));

        return templatePathList;
    }

    /// <summary>
    /// 后端主表生成文件路径.
    /// </summary>
    /// <param name="tableName">表名.</param>
    /// <param name="fileName">文件价名称.</param>
    /// <param name="isMapper">是否对象映射.</param>
    /// <returns></returns>
    public static List<string> BackendFlowTargetPathList(string tableName, string fileName, bool isMapper)
    {
        List<string> targetPathList = new List<string>();
        var backendPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName, "Net");
        var servicePath = Path.Combine(backendPath, "Controller", tableName, tableName + "Service.cs");
        var iservicePath = Path.Combine(backendPath, "Controller", tableName, "I" + tableName + "Service.cs");
        var entityPath = Path.Combine(backendPath, "Models", "Entity", "WorkFlowForm", tableName + "Entity.cs");
        var mapperPath = Path.Combine(backendPath, "Models", "Mapper", tableName, tableName + "Mapper.cs");
        var inputCrPath = Path.Combine(backendPath, "Models", "Dto", "WorkFlowForm", tableName, tableName + "CrInput.cs");
        var outputInfoPath = Path.Combine(backendPath, "Models", "Dto", "WorkFlowForm", tableName, tableName + "InfoOutput.cs");

        targetPathList.Add(servicePath);
        targetPathList.Add(iservicePath);
        targetPathList.Add(entityPath);
        if (isMapper)
            targetPathList.Add(mapperPath);
        targetPathList.Add(inputCrPath);
        targetPathList.Add(outputInfoPath);

        return targetPathList;
    }

    #endregion
}