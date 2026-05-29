using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成方法帮助类.
/// </summary>
public class CodeGenFunctionHelper
{
    /// <summary>
    /// 获取纯表单方法.
    /// </summary>
    /// <returns></returns>
    public static List<CodeGenFunctionModel> GetPureFormMethod()
    {
        return new List<CodeGenFunctionModel>
        {
            new CodeGenFunctionModel()
            {
                FullName = "add",
                IsInterface = true,
                orderBy = 1,
            }
        };
    }

    /// <summary>
    /// 获取纯表单带流程方法.
    /// </summary>
    /// <returns></returns>
    public static List<CodeGenFunctionModel> GetPureFormWithProcessMethod()
    {
        return new List<CodeGenFunctionModel>
        {
            new CodeGenFunctionModel()
            {
                FullName = "info",
                IsInterface = true,
                orderBy = 1,
            },
            new CodeGenFunctionModel()
            {
                FullName = "save",
                IsInterface = true,
                orderBy = 2,
            }
        };
    }

    /// <summary>
    /// 常规列表方法.
    /// </summary>
    /// <param name="hasPage">是否分页.</param>
    /// <param name="btnsList">头部按钮.</param>
    /// <param name="columnBtnsList">列表按钮.</param>
    /// <returns></returns>
    public static List<CodeGenFunctionModel> GetGeneralListMethod(bool hasPage, List<ButtonConfigModel> btnsList, List<ButtonConfigModel> columnBtnsList)
    {
        List<CodeGenFunctionModel> functionList = new List<CodeGenFunctionModel>
        {
            // 默认注入获取信息方法
            new CodeGenFunctionModel()
            {
                FullName = "info",
                IsInterface = true,
                orderBy = 1,
            }
        };

        // 根据是否分页注入默认列表方法.
        switch (hasPage)
        {
            case false:
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "noPage",
                    IsInterface = true,
                    orderBy = 3,
                });
                break;
            default:
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "page",
                    IsInterface = true,
                    orderBy = 3,
                });
                break;
        }

        btnsList?.ForEach(b =>
        {
            int orderBy = 0;

            switch (b.value)
            {
                case "add":
                    orderBy = 4;
                    break;
                case "upload":
                    orderBy = 5;
                    break;
                case "download":
                    orderBy = 9;
                    break;
                case "batchRemove":
                    orderBy = 8;
                    break;
            }

            if (b.value == "download" && !hasPage)
            {
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "page",
                    IsInterface = false,
                    orderBy = 10,
                });
            }
            else if (b.value == "download" && hasPage)
            {
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "noPage",
                    IsInterface = false,
                    orderBy = 10,
                });
            }

            functionList.Add(new CodeGenFunctionModel()
            {
                FullName = b.value,
                IsInterface = true,
                orderBy = orderBy,
            });
        });
        columnBtnsList?.ForEach(c =>
        {
            int orderBy = 0;
            switch (c.value)
            {
                case "edit":
                    orderBy = 7;
                    break;
                case "remove":
                    orderBy = 6;
                    break;
                case "detail":
                    orderBy = 2;
                    break;
            }

            functionList.Add(new CodeGenFunctionModel()
            {
                FullName = c.value,
                IsInterface = true,
                orderBy = orderBy,
            });
        });

        return functionList;
    }

    /// <summary>
    /// 常规列表带流程方法.
    /// </summary>
    /// <param name="hasPage"></param>
    /// <param name="btnsList"></param>
    /// <param name="columnBtnsList"></param>
    /// <returns></returns>
    public static List<CodeGenFunctionModel> GetGeneralListWithProcessMethod(bool hasPage, List<ButtonConfigModel> btnsList, List<ButtonConfigModel> columnBtnsList)
    {
        List<CodeGenFunctionModel> functionList = new List<CodeGenFunctionModel>
        {
            // 默认注入获取信息方法
            new CodeGenFunctionModel()
            {
                FullName = "info",
                IsInterface = true,
                orderBy = 1,
            }
        };

        // 根据是否分页注入默认列表方法.
        switch (hasPage)
        {
            case false:
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "noPage",
                    IsInterface = true,
                    orderBy = 3,
                });
                break;
            default:
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "page",
                    IsInterface = true,
                    orderBy = 3,
                });
                break;
        }

        btnsList?.ForEach(b =>
        {
            int orderBy = 0;

            switch (b.value)
            {
                case "save":
                    orderBy = 4;
                    break;
                case "upload":
                    orderBy = 5;
                    break;
                case "download":
                    orderBy = 9;
                    break;
                case "batchRemove":
                    orderBy = 8;
                    break;
            }

            if (b.value == "download" && !hasPage)
            {
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "page",
                    IsInterface = false,
                    orderBy = 10,
                });
            }
            else if (b.value == "download" && hasPage)
            {
                functionList.Add(new CodeGenFunctionModel()
                {
                    FullName = "noPage",
                    IsInterface = false,
                    orderBy = 10,
                });
            }

            functionList.Add(new CodeGenFunctionModel()
            {
                FullName = b.value,
                IsInterface = true,
                orderBy = orderBy,
            });
        });
        columnBtnsList?.ForEach(c =>
        {
            int orderBy = 0;
            switch (c.value)
            {
                case "remove":
                    orderBy = 6;
                    break;
                case "detail":
                    orderBy = 2;
                    break;
            }

            functionList.Add(new CodeGenFunctionModel()
            {
                FullName = c.value,
                IsInterface = true,
                orderBy = orderBy,
            });
        });

        return functionList;
    }
}