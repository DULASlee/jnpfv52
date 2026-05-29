using JNPF.Common.Configuration;
using JNPF.Common.Models.NPOI;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using System.Text.RegularExpressions;

namespace JNPF.Common.Security;

/// <summary>
/// 代码生成导出数据帮助类.
/// </summary>
[SuppressSniffer]
public static class CodeGenExportDataHelper
{
    /// <summary>
    /// 组装导出带子表的数据,返回 第一个合并行标头,第二个导出数据.
    /// </summary>
    /// <param name="selectKey">导出选择列.</param>
    /// <param name="realList">原数据集合.</param>
    /// <param name="paramsModels">模板信息.</param>
    /// <returns>第一行标头 , 导出数据.</returns>
    public static (Dictionary<string, int>, List<Dictionary<string, object>>) GetCreateFirstColumnsHeader(List<string> selectKey, List<Dictionary<string, object>> realList, List<ParamsModel> paramsModels)
    {
        selectKey.ForEach(item =>
        {
            realList.ForEach(it =>
            {
                if (!it.ContainsKey(item)) it.Add(item, string.Empty);
            });
        });

        var addItemList = new List<Dictionary<string, object>>();
        realList.ForEach(items =>
        {
            var rowChildDatas = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var item in items)
            {
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctList = item.Value.ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                    rowChildDatas.Add(item.Key, ctList);
                }
            }

            var len = rowChildDatas.Select(x => x.Value.Count()).OrderByDescending(x => x).FirstOrDefault();

            if (len != null && len > 0)
            {
                for (int i = 0; i < len; i++)
                {
                    if (i == 0)
                    {
                        var newRealItem = realList.Find(x => x["id"].Equals(items["id"]));
                        foreach (var cData in rowChildDatas)
                        {
                            var itemData = cData.Value.FirstOrDefault();
                            if (itemData != null)
                            {
                                foreach (var key in itemData)
                                    if (newRealItem.ContainsKey(cData.Key + "-" + key.Key)) newRealItem[cData.Key + "-" + key.Key] = key.Value;
                            }
                        }
                    }
                    else
                    {
                        var newRealItem = new Dictionary<string, object>();
                        foreach (var it in items)
                        {
                            if (it.Key.Equals("id")) newRealItem.Add(it.Key, it.Value);
                            else newRealItem.Add(it.Key, string.Empty);
                        }
                        foreach (var cData in rowChildDatas)
                        {
                            if (cData.Value.Count > i)
                            {
                                foreach (var it in cData.Value[i])
                                    if (newRealItem.ContainsKey(cData.Key + "-" + it.Key)) newRealItem[cData.Key + "-" + it.Key] = it.Value;
                            }
                        }
                        addItemList.Add(newRealItem);
                    }
                }
            }
        });

        if (addItemList.Any()) realList.AddRange(addItemList);

        var resultList = new List<Dictionary<string, object>>();

        if (selectKey.Any(x => x.Contains("-") && x.ToLower().Contains("tablefield")))
        {
            for (var i = 0; i < realList.Count; i++)
            {
                if (!resultList.Any(x => x["id"].Equals(realList[i]["id"]))) resultList.AddRange(realList.Where(x => x["id"].Equals(realList[i]["id"])).ToList());
            }
        }
        else
        {
            resultList = realList;
        }

        var firstColumns = new Dictionary<string, int>();

        if (selectKey.Any(x => x.Contains("-") && x.ToLower().Contains("tablefield")))
        {
            var empty = string.Empty;
            var keyList = selectKey.Select(x => x.Split("-").First()).Distinct().ToList();
            var mainFieldIndex = 1;
            keyList.ForEach(item =>
            {
                if (item.ToLower().Contains("tablefield"))
                {
                    var title = paramsModels.FirstOrDefault(x => x.field.Contains(item))?.value.Split("-")[0];
                    firstColumns.Add(title + empty, selectKey.Count(x => x.Contains(item)));
                    empty += " ";
                    mainFieldIndex = 1;
                }
                else
                {
                    if (mainFieldIndex == 1) empty += " ";
                    if (!firstColumns.ContainsKey(empty)) firstColumns.Add(empty, mainFieldIndex);
                    else firstColumns[empty] = mainFieldIndex;
                    mainFieldIndex++;
                }
            });
        }

        return (firstColumns, resultList);
    }

    /// <summary>
    /// 数据导出通用.
    /// </summary>
    /// <param name="fileName">导出文件名.</param>
    /// <param name="selectKey">selectKey.</param>
    /// <param name="userId">用户ID.</param>
    /// <param name="realList">数据集合.</param>
    /// <param name="paramList">参数.</param>
    /// <param name="isGroupTable">是否分组表格.</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <returns></returns>
    public static dynamic GetDataExport(string fileName, string selectKey, string userId, List<Dictionary<string, object>> realList, List<ParamsModel> paramList, bool isGroupTable = false, bool isInlineEditor = false)
    {
        switch (isInlineEditor)
        {
            case true:
                paramList.ForEach(item =>
                {
                    item.field = string.Format("{0}_name", item.field);
                });
                break;
        }

        // 如果是 分组表格 类型
        if (isGroupTable)
        {
            List<Dictionary<string, object>>? newValueList = new List<Dictionary<string, object>>();
            realList.ForEach(item =>
            {
                List<Dictionary<string, object>>? tt = item["children"].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                newValueList.AddRange(tt);
            });
            realList = newValueList;

            var selectKeyList = new List<string>();
            selectKey.Split(',').ToList().ForEach(item => { if (realList.Any(x => x.ContainsKey(item)) || item.ToLower().Contains("tablefield")) selectKeyList.Add(item); });
            selectKey = string.Join(",", selectKeyList);
        }

        var res = GetCreateFirstColumnsHeader(selectKey.Split(',').ToList(), realList, paramList);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        List<string> newSelectKey = selectKey.Split(',').ToList();

        List<ParamsModel> newParamList = new List<ParamsModel>();

        // 全部参数顺序
        foreach (var item in firstColumns)
        {
            Regex re = new Regex(@"[\u4e00-\u9fa5]+");
            switch (re.IsMatch(item.Key))
            {
                case false:
                    {
                        var param = newSelectKey.GetRange(0, item.Value);
                        newParamList.AddRange(paramList.FindAll(it => param.Contains(it.field)));
                        newSelectKey.RemoveAll(it => newParamList.Select(it => it.field).ToList().Contains(it));
                    }
                    break;
                default:
                    var childTable = paramList.FindAll(it => it.value.Contains(item.Key.TrimEnd(' ')));
                    childTable = childTable.FindAll(it => selectKey.Split(',').ToList().Contains(it.field));
                    newParamList.AddRange(childTable);
                    newSelectKey.RemoveAll(it => newParamList.Select(it => it.field).ToList().Contains(it));
                    break;
            }
        }

        if (newParamList.Count > 0) newSelectKey = newParamList.Select(it => it.field).ToList();

        try
        {
            List<string> columnList = new List<string>();
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = string.Format("{0}.xls", fileName);
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (var item in newSelectKey)
            {
                ParamsModel isExist = new ParamsModel();
                switch (isInlineEditor)
                {
                    case true:
                        isExist = paramList.Find(p => p.field.Equals(string.Format("{0}_name", item)));
                        break;
                    default:
                        isExist = paramList.Find(p => p.field == item);
                        break;
                }
                if (isExist != null)
                {
                    excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = isExist.field, ExcelColumn = isExist.value });
                    columnList.Add(isExist.value);
                }
            }

            string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
            var fs = (firstColumns == null || firstColumns.Count() < 1) ? ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(resultList, excelconfig, columnList) : ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(resultList, excelconfig, columnList, firstColumns);
            ExcelExportHelper<Dictionary<string, object>>.Export(fs, addPath);
            var fName = userId + "|" + excelconfig.FileName + "|xls";
            return new {
                name = excelconfig.FileName,
                url = "/api/File/Download?encryption=" + DESCEncryption.Encrypt(fName, "JNPF")
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 模板导出.
    /// </summary>
    /// <param name="fileName">导出文件名.</param>
    /// <param name="selectKey">selectKey.</param>
    /// <param name="userId">用户ID.</param>
    /// <param name="realList">数据集合.</param>
    /// <param name="paramList">参数.</param>
    /// <returns></returns>
    public static dynamic GetTemplateExport(string fileName, string selectKey, string userId, List<Dictionary<string, object>> realList, List<ParamsModel> paramList = default)
    {
        var res = GetCreateFirstColumnsHeader(selectKey.Split(',').ToList(), realList, paramList);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        List<string> newSelectKey = selectKey.Split(',').ToList();

        try
        {
            List<string> columnList = new List<string>();
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = string.Format("{0}.xls", fileName);
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (var item in newSelectKey)
            {
                ParamsModel isExist = new ParamsModel();
                var import = realList.FirstOrDefault().Where(p => p.Key.Contains(string.Format("({0})", item)));
                if (import.Any())
                {
                    isExist = new ParamsModel()
                    {
                        field = item,
                        value = import.FirstOrDefault().Key
                    };
                    if (isExist != null)
                    {
                        excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = isExist.field, ExcelColumn = isExist.value });
                        columnList.Add(isExist.value);
                    }
                }
            }

            string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
            var fs = (firstColumns == null || firstColumns.Count() < 1) ? ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList) : ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList, firstColumns);
            ExcelExportHelper<Dictionary<string, object>>.Export(fs, addPath);
            var fName = userId + "|" + excelconfig.FileName + "|xls";
            return new {
                name = excelconfig.FileName,
                url = "/api/File/Download?encryption=" + DESCEncryption.Encrypt(fName, "JNPF")
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
}