using JNPF.Common.CodeGenUpload;
using JNPF.Common.Configuration;
using JNPF.Extensions;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Engine.Import;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Reflection;

namespace JNPF.Common.CodeGen.ExportImport;

/// <summary>
/// 代码生成导出数据帮助类.
/// </summary>
public class ExportImportDataHelper : ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 在线开发运行服务.
    /// </summary>
    private readonly RunService _runService;

    /// <summary>
    /// 模板表单列表数据解析.
    /// </summary>
    private readonly FormDataParsing _formDataParsing;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 工作流.
    /// </summary>
    private readonly IFlowTaskService _flowTaskService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public ExportImportDataHelper(
        ISqlSugarRepository<OrganizeEntity> repositoryRepository,
        IUserManager userManager,
        RunService runService,
        FormDataParsing formDataParsing,
        IBillRullService billRuleService,
        IDataInterfaceService dataInterfaceService,
        IDataBaseManager databaseService,
        ICacheManager cacheManager,
        IFlowTaskService flowTaskService,
        IFileManager fileManager,
        ISqlSugarClient context)
    {
        _repository = repositoryRepository;
        _sqlSugarClient = (SqlSugarScope)context;
        _billRuleService = billRuleService;
        _databaseService = databaseService;
        _dataInterfaceService = dataInterfaceService;
        _runService = runService;
        _formDataParsing = formDataParsing;
        _cacheManager = cacheManager;
        _flowTaskService = flowTaskService;
        _fileManager = fileManager;
        _userManager = userManager;
    }

    /// <summary>
    /// 组装导出带子表的数据,返回 第一个合并行标头,第二个导出数据.
    /// </summary>
    /// <param name="selectKey">导出选择列.</param>
    /// <param name="realList">原数据集合.</param>
    /// <param name="paramsModels">模板信息.</param>
    /// <returns>第一行标头 , 导出数据.</returns>
    public static (Dictionary<string, int>, List<Dictionary<string, object>>) GetCreateFirstColumnsHeader(List<string> selectKey, List<Dictionary<string, object>> realList, List<ParamsModel> paramsModels)
    {
        // 是否有复杂表头
        var complexHeaderList = GetComplexHeaderList(paramsModels, selectKey);

        selectKey.ForEach(item =>
        {
            realList.ForEach(it =>
            {
                if (!it.ContainsKey(item)) it.Add(item, it.FirstOrDefault(x => x.Key.Contains(string.Format("({0})", item))).Value != null ? it.FirstOrDefault(x => x.Key.Contains(string.Format("({0})", item))).Value : string.Empty);
            });
        });

        var newRealList = new List<Dictionary<string, object>>();
        realList.ForEach(items =>
        {
            newRealList.Add(items);
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
                        newRealList.Add(newRealItem);
                    }
                }
            }
        });
        realList = newRealList;

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

        if (selectKey.Any(x => x.Contains("-") && x.ToLower().Contains("tablefield")) || complexHeaderList.Any())
        {
            var empty = string.Empty;
            var keyList = selectKey.Select(x => x.Split("-").First()).Distinct().ToList();
            var copyKeyList = keyList.Copy();
            var mainFieldIndex = 1;

            keyList.ForEach(it =>
            {
                var item = copyKeyList.Find(x => x.Equals(it));
                if (item != null)
                {
                    if (item.ToLower().Contains("tablefield"))
                    {
                        var title = paramsModels.FirstOrDefault(x => x.field.Contains(item))?.value.Split("-")[0];
                        firstColumns.Add(title + empty, selectKey.Count(x => x.Contains(item) && !x.Equals(item)));
                        empty += " ";
                        mainFieldIndex = 1;
                    }
                    else if (complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item))))
                    {
                        var complexItem = complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item)));
                        firstColumns.Add(complexItem.fullName + empty, complexItem.childColumns.Count);
                        empty += " ";
                        mainFieldIndex = 1;
                        copyKeyList.RemoveAll(x => complexItem.childColumns.Contains(x));
                    }
                    else
                    {
                        if (mainFieldIndex == 1) empty += " ";
                        if (!firstColumns.ContainsKey("jnpf-singlefield" + empty)) firstColumns.Add("jnpf-singlefield" + empty, mainFieldIndex);
                        else firstColumns["jnpf-singlefield" + empty] = mainFieldIndex;
                        mainFieldIndex++;
                    }
                }
            });

            var selectKeyDic = new Dictionary<string, int>();
            for (var i = 0; i < selectKey.Count; i++) selectKeyDic.Add(selectKey[i], i);

            foreach (var item in selectKey)
            {
                if (complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item))))
                {
                    var columns = complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item)));
                    var firstIndex = selectKey.IndexOf(columns.childColumns.First());
                    columns.childColumns.ForEach(it =>
                    {
                        if (!selectKeyDic.ContainsKey(item)) selectKeyDic.Add(item, firstIndex);
                    });
                }
                else
                {
                    if (!selectKeyDic.ContainsKey(item)) selectKeyDic.Add(item, selectKey.IndexOf(item));
                }
            }

            selectKey = selectKeyDic.OrderBy(x => x.Value).Select(x => x.Key).ToList();
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
                var skList = new List<string>();
                selectKey.Split(',').ToList().ForEach(x => skList.Add(string.Format("{0}_name", x)));
                selectKey = string.Join(",", skList);
                var newList = new List<Dictionary<string, object>>();
                realList.ForEach(items =>
                {
                    var newItem = new Dictionary<string, object>();
                    foreach (var it in items) if (!it.Key.ToLower().Contains("tablefield")) newItem.Add(it.Key, it.Value);
                    newList.Add(newItem);
                });
                realList = newList;
                break;
        }

        // 如果是 分组表格 类型
        if (isGroupTable)
        {
            List<Dictionary<string, object>>? newValueList = new List<Dictionary<string, object>>();
            realList.ForEach(item =>
            {
                List<Dictionary<string, object>>? tt = item["children"].ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                newValueList.AddRange(tt);
            });
            realList = newValueList;
        }
        if (paramList != null && paramList.Any())
        {
            var newSelectKeyList = new List<string>();
            var tempselectKey = selectKey.Split(',').ToList();
            paramList.ForEach(x =>
            {
                if (tempselectKey.Any(xx => x.field.Equals(xx))) newSelectKeyList.Add(x.field);
            });
            selectKey = string.Join(",", newSelectKeyList);
        }
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
                isExist = paramList.Find(p => p.field == item);
                if (isExist != null)
                {
                    if (isExist.value.Contains("@@") && isExist.value.Split("@@").Count().Equals(4)) isExist.value = isExist.value.Split("@@").Last();
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
        var newParam = GetParamsModelListByFirstData(realList);
        if (paramList != null && paramList.Any())
        {
            paramList.ForEach(x =>
            {
                var npList = newParam.Where(it => (it.field + "-").Contains(x.field)).ToList();
                npList.ForEach(it => { it.value = x.value + "-" + it.value; });
            });
            newParam.ForEach(x => { if (!paramList.Select(xx => xx.field).Contains(x.field)) paramList.Add(x); });
        }
        else
        {
            paramList = newParam;
        }

        var selectKeyList = selectKey.Split(',').ToList();
        var res = GetCreateFirstColumnsHeader(selectKeyList, realList, paramList);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        List<string> newSelectKey = selectKeyList;

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

    /// <summary>
    /// 获取模板解析.
    /// </summary>
    public static List<FieldsModel> GetTemplateParsing<T>(T entity)
    {
        List<FieldsModel> fieldList = new List<FieldsModel>();
        foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            FieldsModel field = new FieldsModel();
            foreach (var att in prop.GetCustomAttributes(false))
            {
                if (att is CodeGenUploadAttribute)
                {
                    var configModel = new ConfigModel();
                    field.__vModel__ = (att as CodeGenUploadAttribute).__Model__;
                    field.superiorVModel = (att as CodeGenUploadAttribute).__vModel__;
                    field.level = (att as CodeGenUploadAttribute).level;
                    field.min = (att as CodeGenUploadAttribute).min == 0 ? null : (att as CodeGenUploadAttribute).min;
                    field.max = (att as CodeGenUploadAttribute).max == 0 ? null : (att as CodeGenUploadAttribute).max;
                    field.activeTxt = (att as CodeGenUploadAttribute).activeTxt;
                    field.inactiveTxt = (att as CodeGenUploadAttribute).inactiveTxt;
                    field.format = (att as CodeGenUploadAttribute).format;
                    field.multiple = (att as CodeGenUploadAttribute).multiple;
                    field.separator = (att as CodeGenUploadAttribute).separator;
                    field.props = (att as CodeGenUploadAttribute).props.Adapt<PropsBeanModel>();
                    field.options = (att as CodeGenUploadAttribute).options;
                    field.propsValue = (att as CodeGenUploadAttribute).propsValue;
                    field.relationField = (att as CodeGenUploadAttribute).relationField;
                    field.modelId = (att as CodeGenUploadAttribute).modelId;
                    field.interfaceId = (att as CodeGenUploadAttribute).interfaceId;
                    field.selectType = (att as CodeGenUploadAttribute).selectType;
                    field.ableIds = (att as CodeGenUploadAttribute).ableIds;
                    field.relational = (att as CodeGenUploadAttribute).showField;
                    configModel = (att as CodeGenUploadAttribute).__config__.Adapt<ConfigModel>();
                    field.showLevel = (att as CodeGenUploadAttribute).__config__.showLevel;
                    configModel.label = string.Format("{0}({1})", configModel.label, field.__vModel__);
                    field.__config__ = configModel;
                    fieldList.Add(field);
                }
            }
        }
        return fieldList;
    }

    /// <summary>
    /// 获取数据转换模板解析.
    /// </summary>
    public static List<FieldsModel> GetDataConversionTemplateParsing<T>(T entity)
    {
        List<FieldsModel> fieldList = new List<FieldsModel>();
        foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            FieldsModel field = new FieldsModel();
            foreach (var att in prop.GetCustomAttributes(false))
            {
                if (att is CodeGenUploadAttribute)
                {
                    var configModel = new ConfigModel();
                    field.__vModel__ = (att as CodeGenUploadAttribute).__vModel__ ?? (att as CodeGenUploadAttribute).__Model__;
                    field.level = (att as CodeGenUploadAttribute).level;
                    field.min = (att as CodeGenUploadAttribute).min == 0 ? null : (att as CodeGenUploadAttribute).min;
                    field.max = (att as CodeGenUploadAttribute).max == 0 ? null : (att as CodeGenUploadAttribute).max;
                    field.activeTxt = (att as CodeGenUploadAttribute).activeTxt;
                    field.inactiveTxt = (att as CodeGenUploadAttribute).inactiveTxt;
                    field.format = (att as CodeGenUploadAttribute).format;
                    field.multiple = (att as CodeGenUploadAttribute).multiple;
                    field.separator = (att as CodeGenUploadAttribute).separator;
                    field.props = (att as CodeGenUploadAttribute).props.Adapt<PropsBeanModel>();
                    field.options = (att as CodeGenUploadAttribute).options;
                    field.propsValue = (att as CodeGenUploadAttribute).propsValue;
                    field.relationField = (att as CodeGenUploadAttribute).relationField;
                    field.modelId = (att as CodeGenUploadAttribute).modelId;
                    field.interfaceId = (att as CodeGenUploadAttribute).interfaceId;
                    field.selectType = (att as CodeGenUploadAttribute).selectType;
                    field.ableIds = (att as CodeGenUploadAttribute).ableIds;
                    field.relational = (att as CodeGenUploadAttribute).showField;
                    configModel = (att as CodeGenUploadAttribute).__config__.Adapt<ConfigModel>();
                    configModel.templateJson = ((att as CodeGenUploadAttribute).__config__).templateJson.ToObject<List<LinkageConfig>>();
                    configModel.label = string.Format("{0}({1})", configModel.label, field.__vModel__);
                    field.__config__ = configModel;
                    fieldList.Add(field);
                }
            }
        }
        return fieldList;
    }

    /// <summary>
    /// 获取模板解析.
    /// </summary>
    public static List<FieldsModel> GetTemplateParsing<T>(T entity, string tableName)
    {
        List<FieldsModel> fieldList = new List<FieldsModel>();
        foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            FieldsModel field = new FieldsModel();
            foreach (var att in prop.GetCustomAttributes(false))
            {
                if (att is CodeGenUploadAttribute)
                {
                    var configModel = new ConfigModel();
                    field.__vModel__ = string.Format("jnpf_{0}_jnpf_{1}", tableName, (att as CodeGenUploadAttribute).__Model__);
                    field.level = (att as CodeGenUploadAttribute).level;
                    field.min = (att as CodeGenUploadAttribute).min == 0 ? null : (att as CodeGenUploadAttribute).min;
                    field.max = (att as CodeGenUploadAttribute).max == 0 ? null : (att as CodeGenUploadAttribute).max;
                    field.activeTxt = (att as CodeGenUploadAttribute).activeTxt;
                    field.inactiveTxt = (att as CodeGenUploadAttribute).inactiveTxt;
                    field.format = (att as CodeGenUploadAttribute).format;
                    field.multiple = (att as CodeGenUploadAttribute).multiple;
                    field.separator = (att as CodeGenUploadAttribute).separator;
                    field.props = (att as CodeGenUploadAttribute).props.Adapt<PropsBeanModel>();
                    field.options = (att as CodeGenUploadAttribute).options;
                    field.propsValue = (att as CodeGenUploadAttribute).propsValue;
                    field.relationField = (att as CodeGenUploadAttribute).relationField;
                    field.modelId = (att as CodeGenUploadAttribute).modelId;
                    field.interfaceId = (att as CodeGenUploadAttribute).interfaceId;
                    field.selectType = (att as CodeGenUploadAttribute).selectType;
                    field.ableIds = (att as CodeGenUploadAttribute).ableIds;
                    field.relational = (att as CodeGenUploadAttribute).showField;
                    configModel = (att as CodeGenUploadAttribute).__config__.Adapt<ConfigModel>();
                    configModel.label = string.Format("{0}({1})", configModel.label, field.__vModel__);
                    field.__config__ = configModel;
                    fieldList.Add(field);
                }
            }
        }

        return fieldList;
    }

    /// <summary>
    /// 获取数据转换模板解析.
    /// </summary>
    public static List<FieldsModel> GetDataConversionTemplateParsing<T>(T entity, string tableName)
    {
        List<FieldsModel> fieldList = new List<FieldsModel>();
        foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            FieldsModel field = new FieldsModel();
            foreach (var att in prop.GetCustomAttributes(false))
            {
                if (att is CodeGenUploadAttribute)
                {
                    var configModel = new ConfigModel();
                    field.__vModel__ = string.Format("jnpf_{0}_jnpf_{1}", tableName, (att as CodeGenUploadAttribute).__vModel__ ?? (att as CodeGenUploadAttribute).__Model__);
                    field.level = (att as CodeGenUploadAttribute).level;
                    field.min = (att as CodeGenUploadAttribute).min == 0 ? null : (att as CodeGenUploadAttribute).min;
                    field.max = (att as CodeGenUploadAttribute).max == 0 ? null : (att as CodeGenUploadAttribute).max;
                    field.activeTxt = (att as CodeGenUploadAttribute).activeTxt;
                    field.inactiveTxt = (att as CodeGenUploadAttribute).inactiveTxt;
                    field.format = (att as CodeGenUploadAttribute).format;
                    field.multiple = (att as CodeGenUploadAttribute).multiple;
                    field.separator = (att as CodeGenUploadAttribute).separator;
                    field.props = (att as CodeGenUploadAttribute).props.Adapt<PropsBeanModel>();
                    field.options = (att as CodeGenUploadAttribute).options;
                    field.propsValue = (att as CodeGenUploadAttribute).propsValue;
                    field.relationField = (att as CodeGenUploadAttribute).relationField;
                    field.modelId = (att as CodeGenUploadAttribute).modelId;
                    field.interfaceId = (att as CodeGenUploadAttribute).interfaceId;
                    field.selectType = (att as CodeGenUploadAttribute).selectType;
                    field.ableIds = (att as CodeGenUploadAttribute).ableIds;
                    field.relational = (att as CodeGenUploadAttribute).showField;
                    configModel = (att as CodeGenUploadAttribute).__config__.Adapt<ConfigModel>();
                    configModel.label = string.Format("{0}({1})", configModel.label, field.__vModel__);
                    field.__config__ = configModel;
                    fieldList.Add(field);
                }
            }
        }

        return fieldList;
    }

    /// <summary>
    /// 获取模板Excel头部.
    /// </summary>
    /// <typeparam name="T">对象.</typeparam>
    /// <param name="entity">实体.</param>
    /// <param name="type">1-主表,2-副表,3-子表.</param>
    /// <param name="replaceContent">替换内容.</param>
    /// <returns></returns>
    public static Dictionary<string, object> GetTemplateHeader<T>(T entity, int type, string replaceContent = default)
    {
        var dicItem = new Dictionary<string, object>();
        foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetCustomAttributes(typeof(CodeGenUploadAttribute), false).Length > 0))
        {
            foreach (var att in prop.GetCustomAttributes(typeof(CodeGenUploadAttribute), false))
            {
                string vModel = (att as CodeGenUploadAttribute).__Model__;
                CodeGenConfigModel congig = (att as CodeGenUploadAttribute).__config__.ToObject<CodeGenConfigModel>();
                bool multiple = (bool)(att as CodeGenUploadAttribute)?.multiple;
                string format = (att as CodeGenUploadAttribute)?.format;
                int level = (int)(att as CodeGenUploadAttribute)?.level;
                var dic = CodeGenHelper.CodeGenTemplate(congig.jnpfKey, multiple, congig?.label, format, level);
                var title = string.Empty;
                switch (type)
                {
                    case 2:
                        title = string.Format("{0}(jnpf_{1}_jnpf_{2})", dic.Keys.FirstOrDefault(), replaceContent, vModel);
                        break;
                    case 3:
                        title = string.Format("{0}({1}-{2})", dic.Keys.FirstOrDefault(), replaceContent, vModel);
                        break;
                    default:
                        title = string.Format("{0}({1})", dic.Keys.FirstOrDefault(), vModel);
                        break;
                }
                dicItem[title] = dic.Values.FirstOrDefault();
            }
        }
        return dicItem;
    }

    /// <summary>
    /// 获取表数据关联.
    /// </summary>
    /// <param name="entityInfo">实体信息.</param>
    /// <param name="type">表类型.</param>
    /// <param name="tableField">外键字段.</param>
    /// <param name="relationTable">关联主表.</param>
    /// <param name="relationField">关联主键.</param>
    /// <returns></returns>
    public static DbTableRelationModel GetTableRelation(EntityInfo entityInfo, string type, string tableField = default, string relationTable = default, string relationField = default)
    {
        DbTableRelationModel model = new DbTableRelationModel();
        model.typeId = type;
        model.table = entityInfo.DbTableName;
        model.tableName = entityInfo.TableDescription;
        model.tableKey = entityInfo.Columns.Find(it => it.IsPrimarykey).DbColumnName;
        model.relationTable = relationTable;
        model.tableField = tableField;
        model.relationField = relationField;
        return model;
    }

    /// <summary>
    /// 获取导入预览返回值.
    /// </summary>
    /// <param name="tInfo"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<dynamic> GetImportPreviewData(TemplateParsingBase tInfo, string fileName)
    {
        var resData = new List<Dictionary<string, object>>();
        var headerRow = new List<dynamic>();

        var isChildTable = tInfo.selectKey.Any(x => tInfo.ChildTableFields.ContainsKey(x));

        // 复杂表头
        if (!tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5) && tInfo.ColumnData.complexHeaderList != null && tInfo.ColumnData.complexHeaderList.Any())
        {
            foreach (var item in tInfo.ColumnData.complexHeaderList)
            {
                foreach (var subItem in item.childColumns)
                {
                    if (tInfo.selectKey.Contains(subItem))
                    {
                        isChildTable = true;
                        break;
                    }
                }
            }
        }

        try
        {
            var FileEncode = tInfo.AllFieldsModel.Where(x => tInfo.selectKey.Contains(x.__vModel__)).ToList();

            string? savePath = Path.Combine(FileVariable.TemporaryFilePath, fileName);

            // 得到数据
            var sr = await _fileManager.GetFileStream(savePath);
            var excelData = new System.Data.DataTable();
            if (isChildTable) excelData = ExcelImportHelper.ToDataTable(savePath, sr, 0, 0, 2);
            else excelData = ExcelImportHelper.ToDataTable(savePath, sr);
            if (excelData.Columns.Count > tInfo.selectKey.Count) excelData.Columns.RemoveAt(tInfo.selectKey.Count);
            foreach (object? item in excelData.Columns)
            {
                excelData.Columns[item.ToString()].ColumnName = FileEncode.Where(x => x.__config__.label == item.ToString()).FirstOrDefault()?.__vModel__;
            }

            resData = excelData.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
            if (resData.Any())
            {
                if (isChildTable)
                {
                    var hRow = resData[1].Copy();
                    var hRow2 = resData[0].Copy();
                    foreach (var it in hRow) if (it.Value.IsNullOrEmpty()) hRow[it.Key] = hRow2[it.Key];

                    foreach (var item in hRow)
                    {
                        if (item.Key.Contains("tableField") && item.Key.Contains("-"))
                        {
                            var childVModel = item.Key.Split("-").First();
                            if (!headerRow.Any(x => x.id.Equals(childVModel)))
                            {
                                var child = new List<dynamic>();
                                hRow.Where(x => x.Key.Contains(childVModel)).ToList().ForEach(x =>
                                {
                                    child.Add(new { id = x.Key.Replace(childVModel + "-", string.Empty), fullName = x.Value.ToString().Replace(string.Format("({0})", x.Key), string.Empty) });
                                });
                                headerRow.Add(new { id = childVModel, fullName = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(childVModel)).__config__.label.Replace(string.Format("({0})", childVModel), string.Empty), children = child, jnpfKey = "table" });
                            }
                        }
                        else if (tInfo.ColumnData.complexHeaderList != null && tInfo.ColumnData.complexHeaderList.Count > 0 && tInfo.ColumnData.complexHeaderList.Any(it => it.childColumns.Contains(item.Key)))
                        {
                            var complexHeaderModel = tInfo.ColumnData.complexHeaderList.Find(it => it.childColumns.Contains(item.Key));
                            if (!headerRow.Any(x => x.id.Equals(complexHeaderModel.id)))
                            {
                                var child = new List<dynamic>();
                                foreach (var childItem in complexHeaderModel.childColumns)
                                {
                                    if (hRow.ContainsKey(childItem))
                                        child.Add(new { id = childItem, fullName = hRow[childItem].ToString().Replace(string.Format("({0})", childItem), string.Empty) });
                                }
                                headerRow.Add(new { id = complexHeaderModel.id, fullName = complexHeaderModel.fullName, children = child, jnpfKey = "complexHeader" });
                            }
                        }
                        else
                        {
                            headerRow.Add(new { id = item.Key, fullName = item.Value.ToString().Replace(string.Format("({0})", item.Key), string.Empty) });
                        }
                    }
                    resData.Remove(resData.First());
                    resData.Remove(resData.First());
                }
                else
                {
                    foreach (var item in resData.First().Copy()) headerRow.Add(new { id = item.Key, fullName = item.Value.ToString().Replace(string.Format("({0})", item.Key), string.Empty) });
                    resData.Remove(resData.First());
                }
            }
        }
        catch (Exception e)
        {
            throw Oops.Oh(ErrorCode.D1410);
        }

        try
        {
            // 带子表字段数据导入
            if (isChildTable)
            {
                var newData = new List<Dictionary<string, object>>();
                var singleForm = tInfo.selectKey.Where(x => !x.Contains("tableField")).ToList();

                var childTableVModel = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();

                resData.ForEach(dataItem =>
                {
                    var addItem = new Dictionary<string, object>();
                    var isNextRow = false;
                    foreach (var item in dataItem)
                    {
                        if (singleForm.Contains(item.Key) && item.Value.IsNotEmptyOrNull())
                            isNextRow = true;
                    }

                    // 单条数据 (多行子表数据合并)
                    if (isNextRow)
                    {
                        singleForm.ForEach(item => addItem.Add(item, dataItem[item]));

                        // 子表数据
                        childTableVModel.ForEach(item =>
                        {
                            var childAddItem = new Dictionary<string, object>();
                            tInfo.selectKey.Where(x => x.Contains(item) && x != item).ToList().ForEach(it =>
                            {
                                childAddItem.Add(it.Replace(item + "-", string.Empty), dataItem[it]);
                            });

                            addItem.Add(item, new List<Dictionary<string, object>> { childAddItem });
                        });

                        newData.Add(addItem);
                    }
                    else
                    {
                        var item = newData.LastOrDefault();
                        if (item != null)
                        {
                            // 子表数据
                            childTableVModel.ForEach(citem =>
                            {
                                var childAddItem = new Dictionary<string, object>();
                                tInfo.selectKey.Where(x => x.Contains(citem)).ToList().ForEach(it =>
                                {
                                    if (dataItem.ContainsKey(it)) childAddItem.Add(it.Replace(citem + "-", string.Empty), dataItem[it]);
                                });

                                if (!item.ContainsKey(citem))
                                {
                                    item.Add(citem, new List<Dictionary<string, object>> { childAddItem });
                                }
                                else
                                {
                                    var childList = item[citem].ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                                    childList.Add(childAddItem);
                                    item[citem] = childList;
                                }
                            });
                        }
                        else
                        {
                            singleForm.ForEach(item => addItem.Add(item, dataItem[item]));

                            // 子表数据
                            childTableVModel.ForEach(item =>
                            {
                                var childAddItem = new Dictionary<string, object>();
                                tInfo.selectKey.Where(x => x.Contains(item)).ToList().ForEach(it =>
                                {
                                    if (dataItem.ContainsKey(it)) childAddItem.Add(it.Replace(item + "-", string.Empty), dataItem[it]);
                                });

                                addItem.Add(item, new List<Dictionary<string, object>> { childAddItem });
                            });

                            newData.Add(addItem);
                        }
                    }
                });
                resData = newData;
            }
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D1412);
        }

        resData.ForEach(items =>
        {
            foreach (var item in items)
            {
                var vmodel = tInfo.AllFieldsModel.FirstOrDefault(x => x.__vModel__.Equals(item.Key));
                if (vmodel != null && vmodel.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) && item.Value.IsNotEmptyOrNull())
                    items[item.Key] = string.Format("{0:" + vmodel.format + "} ", item.Value);
                else if (vmodel != null && vmodel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && item.Value.IsNotEmptyOrNull())
                {
                    var ctList = item.Value.ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                    ctList.ForEach(ctItems =>
                    {
                        foreach (var ctItem in ctItems)
                        {
                            var ctVmodel = tInfo.AllFieldsModel.FirstOrDefault(x => x.__vModel__.Equals(vmodel.__vModel__ + "-" + ctItem.Key));
                            if (ctVmodel != null && ctVmodel.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) && ctItem.Value.IsNotEmptyOrNull())
                                ctItems[ctItem.Key] = string.Format("{0:" + ctVmodel.format + "} ", ctItem.Value);
                        }
                    });
                    items[item.Key] = ctList;
                }
            }
        });

        // 返回结果
        return new { dataRow = resData, headerRow = headerRow };
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="list">数据集合.</param>
    /// <param name="vId">模板Id.</param>
    /// <returns>[成功列表,失败列表].</returns>
    public async Task<object[]> ImportMenuData(TemplateParsingBase tInfo, DataImportInput input, string vId = "")
    {
        var list = input.list;
        if (tInfo.ColumnData.complexHeaderList != null && tInfo.ColumnData.complexHeaderList.Count > 0 && !tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5))
        {
            var complexHeaderIdList = tInfo.ColumnData.complexHeaderList.Select(it => it.id).ToList();
            foreach (var item in list)
            {
                var addValue = new Dictionary<string, object>();
                foreach (var subItem in item)
                {
                    if (complexHeaderIdList.Contains(subItem.Key))
                    {
                        foreach (var newItem in subItem.Value.ToObject<List<Dictionary<string, object>>>())
                        {
                            foreach (var dicItem in newItem)
                            {
                                addValue[dicItem.Key] = dicItem.Value;
                            }
                        }
                    }
                }

                if (addValue.Count > 0)
                {
                    foreach (var addItem in addValue)
                    {
                        item[addItem.Key] = addItem.Value;
                    }
                }
            }
        }

        List<Dictionary<string, object>> userInputList = ImportFirstVerify(tInfo, list);
        List<FieldsModel> fieldsModelList = tInfo.AllFieldsModel.Where(x => tInfo.selectKey.Contains(x.__vModel__) || tInfo.ChildTableFieldsModelList.Select(xx => xx.__vModel__).Contains(x.__vModel__)).ToList();

        var successList = new List<Dictionary<string, object>>();
        var errorsList = new List<Dictionary<string, object>>();

        // 捞取控件解析数据
        var cData = await GetCDataList(tInfo.AllFieldsModel, new Dictionary<string, List<Dictionary<string, string>>>());
        var res = await ImportDataAssemble(fieldsModelList, userInputList, cData);
        res.Where(x => x.ContainsKey("errorsInfo")).ToList().ForEach(item => errorsList.Add(item));
        res.Where(x => !x.ContainsKey("errorsInfo")).ToList().ForEach(item => successList.Add(item));

        try
        {
            // 唯一验证已处理，入库前去掉.
            tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item => item.__config__.unique = false);
            _sqlSugarClient.BeginTran();
            foreach (var item in successList)
            {
                if (item.ContainsKey("Update_MainTablePrimary_Id"))
                {
                    string? mainId = item["Update_MainTablePrimary_Id"].ToString();
                    var haveTableSql = await _runService.GetUpdateSqlByTemplate(tInfo, new VisualDevModelDataUpInput() { data = item.ToJsonString() }, mainId);
                    foreach (var it in haveTableSql) await _databaseService.ExecuteSql(tInfo.DbLink, it); // 修改功能数据
                }
                else
                {
                    if ((tInfo.visualDevEntity?.EnableFlow.Equals(1)).ParseToBool())
                    {
                        await _flowTaskService.Create(new Common.Models.WorkFlow.FlowTaskSubmitModel() { formData = item, flowId = input.flowId, flowUrgent = 1, status = 1 });
                    }
                    else
                    {
                        string? mainId = SnowflakeIdHelper.NextId().ToString();
                        var haveTableSql = await _runService.GetCreateSqlByTemplate(tInfo, new VisualDevModelDataCrInput() { data = item.ToJsonString() }, mainId);

                        // 主表自增长Id.
                        if (haveTableSql.ContainsKey("MainTableReturnIdentity"))
                        {
                            mainId = haveTableSql["MainTableReturnIdentity"].First().First().Value.ToString();
                            haveTableSql.Remove("MainTableReturnIdentity");
                        }
                        foreach (var it in haveTableSql)
                            await _databaseService.ExecuteSql(tInfo.DbLink, it.Key, it.Value); // 新增功能数据
                    }
                }
            }

            _sqlSugarClient.CommitTran();
        }
        catch (Exception e)
        {
            _sqlSugarClient.RollbackTran();
            throw;
        }

        errorsList.ForEach(item =>
        {
            if (item.ContainsKey("errorsInfo") && item["errorsInfo"].IsNotEmptyOrNull()) item["errorsInfo"] = item["errorsInfo"].ToString().TrimStart(',').TrimEnd(',');
        });

        return new object[] { successList, errorsList };
    }

    /// <summary>
    /// Excel 转输出 Model.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="realList">数据列表.</param>
    /// <param name="excelName">导出文件名称.</param>
    /// <param name="firstColumns">手动输入第一行（合并主表列和各个子表列）.</param>
    /// <returns>dynamic.</returns>
    public async Task<dynamic> ExcelCreateModel(TemplateParsingBase tInfo, List<Dictionary<string, object>> realList, string excelName = null, Dictionary<string, int> firstColumns = null)
    {
        List<ExcelTemplateModel> templateList = new List<ExcelTemplateModel>();
        List<string> columnList = new List<string>();
        if (tInfo.ColumnData != null && tInfo.ColumnData.complexHeaderList != null && tInfo.ColumnData.complexHeaderList.Any())
        {
            List<ParamsModel> paramList = new List<ParamsModel>();
            tInfo.AllFieldsModel.ForEach(item =>
            {
                // 处理复杂表头
                if (tInfo.ColumnData.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item.__vModel__))))
                {
                    var comlex = tInfo.ColumnData.complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item.__vModel__)));

                    // 复杂表头格式 label 调整
                    var comlexLabel = string.Format("{0}@@{1}@@{2}@@{3}", comlex.id, comlex.fullName, comlex.align, item.__config__.label);
                    paramList.Add(new ParamsModel() { field = item.__vModel__, value = comlexLabel });
                }
                else
                {

                    paramList.Add(new ParamsModel() { field = item.__vModel__, value = item.__config__.label });
                }
            });
            firstColumns = GetCreateFirstColumnsHeader(tInfo.selectKey, realList, paramList).Item1;
        }
        try
        {
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = (excelName.IsNullOrEmpty() ? SnowflakeIdHelper.NextId().ToString() : excelName) + ".xls";
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (string? item in tInfo.selectKey)
            {
                var excelColumn = tInfo.AllFieldsModel.Find(t => t.__vModel__ == item);
                if (excelColumn != null && (excelColumn.__config__.jnpfKey == null || !excelColumn.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
                {
                    excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item, ExcelColumn = excelColumn.__config__.label });
                    columnList.Add(excelColumn.__config__.label);
                }
            }

            string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
            var fs = firstColumns == null ? ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList) : ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList, firstColumns);
            var flag = await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
            if (flag)
            {
                fs.Flush();
                fs.Close();
            }
            var name = excelconfig.FileName;
            var url = "/api/file/Download?encryption=" + DESCEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF");
            return new { name = name, url = url };
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 导入功能数据初步验证.
    /// </summary>
    private List<Dictionary<string, object>> ImportFirstVerify(TemplateParsingBase tInfo, List<Dictionary<string, object>> list)
    {
        var errorKey = ImportAssembleErrors.ErrorKey;
        var resList = ImportFirstVerifyHelpers.SeedWithEmptyErrors(list);
        var childTableList = ImportFirstVerifyHelpers.CollectChildTableVModels(tInfo.AllFieldsModel);

        #region 验证必填控件
        ImportFirstVerifyHelpers.ValidateRequired(resList, tInfo.AllFieldsModel, childTableList);
        #endregion

        #region 验证唯一
        var uniqueList = tInfo.AllFieldsModel.Where(x => x.__config__.unique).ToList();

        if (uniqueList.Any())
        {
            ImportFirstVerifyHelpers.ValidateBatchUnique(
                resList, tInfo.AllFieldsModel, childTableList, tInfo.dataType);

            // 表里的数据验证唯一
            List<string>? relationKey = new List<string>();
            List<string>? auxiliaryFieldList = tInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            auxiliaryFieldList.ForEach(tName =>
            {
                string? tableField = tInfo.AllTable.Find(tf => tf.table == tName)?.tableField;
                relationKey.Add(tInfo.MainTableName + "." + tInfo.MainPrimary + "=" + tName + "." + tableField);
            });

            resList.ForEach(allDataMap =>
            {
                List<string>? fieldList = new List<string>();
                var whereList = new List<IConditionalModel>();
                fieldList.Add(string.Format("{0}.{1}", tInfo.MainTableName, tInfo.MainPrimary));
                tInfo.SingleFormData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item =>
                {
                    fieldList.Add(string.Format("{0}.{1} {2}", item.__config__.tableName, item.superiorVModel.Split("_jnpf_").Last(), item.__vModel__));

                    if (allDataMap.ContainsKey(item.__vModel__) && allDataMap[item.__vModel__] != null)
                    {
                        whereList.Add(new ConditionalCollections()
                        {
                            ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                                {
                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                                    {
                                        FieldName = string.Format("{0}.{1}", item.__config__.tableName, item.superiorVModel.Split("_jnpf_").Last()),
                                        ConditionalType =allDataMap.ContainsKey(item.__vModel__) ? ConditionalType.Equal: ConditionalType.IsNullOrEmpty,
                                        FieldValue = allDataMap.ContainsKey(item.__vModel__) ? allDataMap[item.__vModel__].ToString() : string.Empty,
                                    })
                                }
                        });
                    }
                });

                var itemWhere = _repository.AsSugarClient().SqlQueryable<dynamic>("@").Where(whereList).ToSqlString();
                if (!itemWhere.Equals("@"))
                {
                    var whereStrList = new List<string>();
                    whereStrList.AddRange(relationKey);
                    whereStrList.Add(itemWhere.Split("WHERE").Last());
                    var querStr = string.Format(
                        "select {0} from {1} where {2}",
                        string.Join(",", fieldList),
                        auxiliaryFieldList.Any() ? tInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : tInfo.MainTableName,
                        string.Join(" and ", whereStrList)); // 多表， 联合查询

                    var res = _databaseService.GetSqlData(tInfo.DbLink, querStr).ToObject<List<Dictionary<string, string>>>();

                    if (res.Any())
                    {
                        var errorList = new List<string>();

                        res.ForEach(items =>
                        {
                            if (tInfo.dataType.Equals("2"))
                            {
                                if (items.Last().Value == null || items.Last().Value.Equals(allDataMap[items.Last().Key].ToString()))
                                {
                                    if (!allDataMap.ContainsKey("Update_MainTablePrimary_Id")) allDataMap.Add("Update_MainTablePrimary_Id", items[tInfo.MainPrimary]);
                                }
                            }
                            else
                            {
                                if (items.Last().Value == null || items.Last().Value.Equals(allDataMap[items.Last().Key].ToString()))
                                {
                                    var errorInfo = tInfo.SingleFormData.First(x => x.__vModel__.Equals(items.Last().Key))?.__config__.label + ": 值不能重复";
                                    if (allDataMap.ContainsKey(errorKey))
                                    {
                                        if (!allDataMap[errorKey].ToString().Contains(errorInfo)) allDataMap[errorKey] = allDataMap[errorKey] + "," + errorInfo;
                                    }
                                    else
                                    {
                                        allDataMap.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        });
                    }
                }
            });
        }

        #endregion

        resList.ForEach(item =>
        {
            if (item[errorKey].IsNullOrEmpty()) item.Remove(errorKey);
        });
        return resList;
    }

    /// <summary>
    /// 获取模板控件解析数据.
    /// </summary>
    /// <param name="tInfo"></param>
    /// <param name="resData"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, List<Dictionary<string, string>>>> GetCDataList(List<FieldsModel> listFieldsModel, Dictionary<string, List<Dictionary<string, string>>> resData)
    {
        foreach (var item in listFieldsModel.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList())
        {
            var addItem = new List<Dictionary<string, string>>();
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.COMSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var allDataList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1)
                                .Select(x => new OrganizeEntity { Id = x.Id, OrganizeIdTree = x.OrganizeIdTree, FullName = x.FullName }).ToListAsync();
                            var dataList = new List<OrganizeEntity>();
                            if (item.selectType.Equals("custom"))
                            {
                                item.ableIds = DynamicParameterConversion(item.ableIds);
                                dataList = allDataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            }
                            else
                            {
                                dataList = allDataList;
                            }
                            addItem = ImportAddressCacheHelpers.BuildOrganizeTreePairs(allDataList, dataList);
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ADDRESS:
                    {
                        if (!resData.ContainsKey(JnpfKeyConst.ADDRESS))
                        {
                            if (_cacheManager.Exists(ImportAddressCacheHelpers.CacheKey))
                            {
                                addItem = _cacheManager.Get(ImportAddressCacheHelpers.CacheKey).ToObject<List<Dictionary<string, string>>>();
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                            else
                            {
                                var dataList = await _repository.AsSugarClient().Queryable<ProvinceEntity>().Select(x => new ProvinceEntity { Id = x.Id, ParentId = x.ParentId, Type = x.Type, FullName = x.FullName }).ToListAsync();
                                addItem = ImportAddressCacheHelpers.BuildPairs(dataList);
                                _cacheManager.Set(ImportAddressCacheHelpers.CacheKey, addItem, TimeSpan.FromDays(7)); // 缓存七天
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.GROUPSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _repository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new GroupEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom"))
                            {
                                dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            }
                            addItem = ImportAddressCacheHelpers.BuildIdEncodePairs(dataList.Select(x => (x.Id, x.EnCode)));
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ROLESELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new RoleEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom"))
                            {
                                item.ableIds = DynamicParameterConversion(item.ableIds);
                                var relationIds = await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>()
                                    .Where(it => item.ableIds.Contains(it.OrganizeId) && it.ObjectType.Equals("Role"))
                                    .Select(it => it.ObjectId).ToListAsync();
                                item.ableIds.AddRange(relationIds);
                                dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            }
                            addItem = ImportAddressCacheHelpers.BuildIdEncodePairs(dataList.Select(x => (x.Id, x.EnCode)));
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.SWITCH:
                    {
                        ImportCacheBagHelpers.TryAdd(
                            resData,
                            item.__vModel__,
                            ImportCacheBagHelpers.BuildSwitchPairs(item.activeTxt, item.inactiveTxt));
                    }

                    break;
                case JnpfKeyConst.CHECKBOX:
                case JnpfKeyConst.SELECT:
                case JnpfKeyConst.RADIO:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var propsValue = string.Empty;
                            var propsLabel = string.Empty;
                            var children = string.Empty;
                            if (item.props != null && item.props != null)
                            {
                                propsValue = item.props.value;
                                propsLabel = item.props.label;
                                children = item.props.children;
                            }

                            if (item.__config__.dataType.Equals("static"))
                            {
                                if (item != null && item.options != null)
                                {
                                    ImportCacheBagHelpers.TryAdd(
                                        resData,
                                        item.__vModel__,
                                        ImportCacheBagHelpers.BuildStaticOptionPairs(item.options, propsValue, propsLabel));
                                }
                            }
                            else if (item.__config__.dataType.Equals("dictionary"))
                            {
                                var dictionaryDataList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();
                                ImportCacheBagHelpers.TryAdd(
                                    resData,
                                    item.__vModel__,
                                    ImportCacheBagHelpers.BuildDictionaryPairs(
                                        dictionaryDataList.Select(it => (it.Id, it.EnCode, it.FullName)),
                                        item.props.value,
                                        ImportCacheBagHelpers.DictionaryPropsMode.EncodeCaseInsensitive));
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                ImportCacheBagHelpers.TryAdd(resData, item.__vModel__, popDataList);
                            }
                        }
                    }
                    break;
                case JnpfKeyConst.TREESELECT:
                case JnpfKeyConst.CASCADER:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.__config__.dataType.Equals("static"))
                            {
                                if (item.options != null)
                                    resData.Add(item.__vModel__, GetStaticList(item));
                            }
                            else if (item.__config__.dataType.Equals("dictionary"))
                            {
                                var dictionaryDataList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.ParentId, a.EnCode, a.FullName, a.Description }).ToListAsync();

                                ImportCacheBagHelpers.TryAdd(
                                    resData,
                                    item.__vModel__,
                                    ImportCacheBagHelpers.BuildDictionaryPairs(
                                        dictionaryDataList.Select(it => (it.Id, it.EnCode, it.FullName)),
                                        item.props.value,
                                        ImportCacheBagHelpers.DictionaryPropsMode.EncodeCaseInsensitive));
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                ImportCacheBagHelpers.TryAdd(resData, item.__vModel__, popDataList);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.POPUPTABLESELECT:
                case JnpfKeyConst.POPUPSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var popDataList = await _formDataParsing.GetDynamicList(item);
                            ImportCacheBagHelpers.TryAdd(resData, item.__vModel__, popDataList);
                        }
                    }
                    break;

                case JnpfKeyConst.USERSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                var dataList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
                                dataList.ForEach(item =>
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    dictionary.Add(item.Id, item.Account);
                                    addItem.Add(dictionary);
                                });
                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.selectType.Equals("custom"))
                            {
                                var newAbleIds = new List<object>();
                                item.ableIds.ForEach(x => newAbleIds.Add(x.ParseToString().Split("--").FirstOrDefault()));
                                newAbleIds = DynamicParameterConversion(newAbleIds);
                                var userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                                    .WhereIF(item.ableIds.Any(), x => newAbleIds.Contains(x.UserId) || newAbleIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                var dataList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && userIdList.Contains(x.Id))
                                    .Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
                                dataList.ForEach(item =>
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    dictionary.Add(item.Id, item.Account);
                                    if (!addItem.Any(x => x.ContainsKey(item.Id))) addItem.Add(dictionary);
                                });
                                resData.Add(item.__vModel__, addItem);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.USERSSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                if (item.multiple)
                                {
                                    (await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.RealName, x.Account }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--user", item.RealName + "/" + item.Account);
                                        addItem.Add(user);
                                    });
                                    var dataList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null)
                                        .Select(x => new OrganizeEntity { Id = x.Id, OrganizeIdTree = x.OrganizeIdTree, FullName = x.FullName, EnCode = x.EnCode }).ToListAsync();
                                    dataList.ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--department", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);

                                        if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
                                        var orgNameList = new List<string>();
                                        item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
                                        {
                                            var org = dataList.Find(x => x.Id == it);
                                            if (org != null) orgNameList.Add(org.FullName);
                                        });
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(item.Id + "--company", string.Join("/", orgNameList));
                                        addItem.Add(dictionary);
                                    });
                                    (await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--role", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                    (await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--position", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                    (await _repository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--group", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                }
                                else
                                {
                                    var dataList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
                                    dataList.ForEach(item =>
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(item.Id + "--user", item.Account);
                                        if (!addItem.Any(x => x.ContainsKey(item.Id))) addItem.Add(dictionary);
                                    });
                                }
                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.selectType.Equals("custom"))
                            {
                                if (item.ableIds.Any())
                                {
                                    var newAbleIds = new List<object>();
                                    item.ableIds.ForEach(x => newAbleIds.Add(x.ParseToString().Split("--").FirstOrDefault()));
                                    newAbleIds = DynamicParameterConversion(newAbleIds);
                                    var userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => newAbleIds.Contains(x.UserId) || newAbleIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                    var dataList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => userIdList.Contains(x.Id)).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
                                    dataList.ForEach(item =>
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(item.Id + "--user", item.Account);
                                        if (!addItem.Any(x => x.ContainsKey(item.Id))) addItem.Add(dictionary);
                                    });
                                    resData.Add(item.__vModel__, addItem);
                                }
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.DEPSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                var dataList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).Select(x => new { x.Id, x.EnCode }).ToListAsync();
                                dataList.ForEach(item =>
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    dictionary.Add(item.Id, item.EnCode);
                                    addItem.Add(dictionary);
                                });
                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.selectType.Equals("custom"))
                            {
                                if (item.ableIds.Any())
                                {
                                    item.ableIds = DynamicParameterConversion(item.ableIds);
                                    if (item.ableIds.Any())
                                    {
                                        var listQuery = new List<ISugarQueryable<OrganizeEntity>>();
                                        item.ableIds.ForEach(x => listQuery.Add(_repository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.OrganizeIdTree.Contains(x.ToString()))));
                                        var dataList = await _repository.AsSugarClient().UnionAll(listQuery).Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.EnCode }).ToListAsync();
                                        dataList.ForEach(item =>
                                        {
                                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                            dictionary.Add(item.Id, item.EnCode);
                                            if (!addItem.Any(x => x.ContainsKey(item.Id))) addItem.Add(dictionary);
                                        });
                                        resData.Add(item.__vModel__, addItem);
                                    }
                                }
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.POSSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                var dataList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new PositionEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                                dataList.ForEach(item =>
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    dictionary.Add(item.Id, item.EnCode);
                                    addItem.Add(dictionary);
                                });
                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.selectType.Equals("custom"))
                            {
                                if (item.ableIds.Any())
                                {
                                    var newAbleIds = new List<object>();
                                    item.ableIds.ForEach(x => newAbleIds.Add(x.ParseToString().Split("--").FirstOrDefault()));
                                    newAbleIds = DynamicParameterConversion(newAbleIds);
                                    var dataList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && (newAbleIds.Contains(x.Id) || newAbleIds.Contains(x.OrganizeId)))
                                        .Select(x => new PositionEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                                    dataList.ForEach(item =>
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(item.Id, item.EnCode);
                                        addItem.Add(dictionary);
                                    });

                                    if (resData.ContainsKey(item.__vModel__))
                                    {
                                        var newAddItem = new List<Dictionary<string, string>>();
                                        foreach (var it in addItem)
                                        {
                                            var tempIt = it.FirstOrDefault().Value;
                                            if (tempIt.IsNotEmptyOrNull() && !resData[item.__vModel__].Any(x => x.ContainsValue(tempIt))) newAddItem.Add(it);
                                        }
                                        resData[item.__vModel__].AddRange(newAddItem);
                                    }
                                    else resData.Add(item.__vModel__, addItem);
                                }
                            }
                        }
                    }

                    break;
            }
        }

        listFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList().ForEach(async item =>
        {
            var res = await GetCDataList(item.__config__.children, resData);
            if (res.Any()) foreach (var it in res) if (!resData.ContainsKey(it.Key)) resData.Add(it.Key, it.Value);
        });

        return resData;
    }

    /// <summary>
    /// 导入数据组装.
    /// </summary>
    /// <param name="fieldsModelList">控件列表.</param>
    /// <param name="dataList">导入数据列表.</param>
    /// <param name="cDataList">控件解析缓存数据.</param>
    /// <returns></returns>
    private async Task<List<Dictionary<string, object>>> ImportDataAssemble(List<FieldsModel> fieldsModelList, List<Dictionary<string, object>> dataList, Dictionary<string, List<Dictionary<string, string>>> cDataList)
    {
        var errorKey = ImportAssembleErrors.ErrorKey;
        UserEntity? userInfo = _userManager.User;

        var resList = new List<Dictionary<string, object>>();
        foreach (var dataItems in dataList)
        {
            var newDataItems = dataItems.Copy();
            foreach (var item in dataItems)
            {
                var vModel = fieldsModelList.Find(x => x.__vModel__.Equals(item.Key));
                if (vModel == null) continue;
                var dicList = ImportFieldCacheLookup.Resolve(vModel, cDataList);
                if (vModel.separator.IsNullOrEmpty()) vModel.separator = "/";
                switch (vModel.__config__.jnpfKey)
                {
                    case JnpfKeyConst.DATE:
                        ImportDateTimeFieldAssembler.MapDate(
                            vModel, item.Key, item.Value, dataItems, newDataItems, ImportDateTimeSemantics.CodeGen);
                        break;

                    case JnpfKeyConst.TIME: // 时间选择
                        ImportDateTimeFieldAssembler.MapTime(
                            vModel, item.Key, item.Value, dataItems, newDataItems, ImportDateTimeSemantics.CodeGen);
                        break;
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.ADDRESS:
                        ImportPathSelectMapper.MapComOrAddress(
                            vModel, item.Key, item.Value, dicList, newDataItems,
                            clearWhenEmpty: true, allowMunicipalDistrictShortcut: true);
                        break;
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.SWITCH:
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.RADIO:
                        ImportOptionValueMapper.MapSelectLike(
                            vModel, item.Key, item.Value, dicList, newDataItems, clearWhenEmpty: true);
                        break;
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.USERSELECT:
                        if (item.Value.IsNotEmptyOrNull() && ImportOptionValueMapper.IsAllOrCustomSelectType(vModel))
                        {
                            ImportOptionValueMapper.MapOrgPathSelect(
                                vModel, item.Key, item.Value, dicList, newDataItems, clearWhenEmpty: true);
                        }
                        else
                        {
                            newDataItems[item.Key] = null;
                        }

                        break;
                    case JnpfKeyConst.USERSSELECT:
                        ImportPathSelectMapper.MapUsersSelect(
                            vModel, item.Key, item.Value, dicList, newDataItems);
                        break;
                    case JnpfKeyConst.TREESELECT:
                        ImportOptionValueMapper.MapSelectLike(
                            vModel, item.Key, item.Value, dicList, newDataItems, clearWhenEmpty: true);
                        break;
                    case JnpfKeyConst.POPUPTABLESELECT:
                    case JnpfKeyConst.POPUPSELECT:
                        ImportPopupValueMapper.Map(
                            vModel, item.Key, item.Value, dicList, newDataItems, clearWhenEmpty: true);
                        break;
                    case JnpfKeyConst.CASCADER:
                        ImportPathSelectMapper.MapCascader(
                            vModel, item.Key, item.Value, dicList, newDataItems, clearWhenEmpty: true);
                        break;
                    case JnpfKeyConst.TABLE:
                        await ImportChildTableAssembler.MapAsync(
                            vModel, item.Key, item.Value, cDataList, newDataItems, ImportDataAssemble, clearWhenEmpty: true);
                        break;
                    case JnpfKeyConst.RATE:
                        ImportNumericFieldAssembler.MapRate(
                            vModel, item.Key, item.Value, newDataItems, ImportNumericSemantics.CodeGen);
                        break;
                    case JnpfKeyConst.SLIDER:
                        ImportNumericFieldAssembler.MapSlider(
                            vModel, item.Key, item.Value, newDataItems, ImportNumericSemantics.CodeGen);
                        break;
                    case JnpfKeyConst.NUMINPUT:
                        ImportNumericFieldAssembler.MapNumberInput(
                            vModel, item.Key, item.Value, newDataItems, ImportNumericSemantics.CodeGen);
                        break;
                }
            }

            // 系统自动生成控件
            var systemCtx = new ImportSystemFieldContext(
                userInfo.Id, userInfo.OrganizeId, DateTime.Now);
            foreach (var item in dataItems)
            {
                if (newDataItems.ContainsKey(errorKey)) continue; // 如果存在错误信息 则 不生成
                var vModel = fieldsModelList.Find(x => x.__vModel__.Equals(item.Key));
                if (vModel == null) continue;
                var jnpfKey = vModel.__config__.jnpfKey;
                if (!ImportSystemFieldAssembler.IsSystemAutoKey(jnpfKey)) continue;

                if (ImportSystemFieldAssembler.TryMapStatic(jnpfKey, item.Key, newDataItems, systemCtx))
                    continue;

                if (jnpfKey.Equals(JnpfKeyConst.BILLRULE))
                {
                    string billNumber = await _billRuleService.GetBillNumber(vModel.__config__.rule);
                    ImportSystemFieldAssembler.MapBillRule(item.Key, billNumber, newDataItems);
                }
                else if (jnpfKey.Equals(JnpfKeyConst.CURRPOSITION))
                {
                    string? pid = await _repository.AsSugarClient().Queryable<UserEntity, PositionEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.PositionId))
                        .Where((a, b) => a.Id == userInfo.Id && a.DeleteMark == null).Select((a, b) => a.PositionId).FirstAsync();
                    ImportSystemFieldAssembler.MapCurrPosition(item.Key, pid, newDataItems);
                }
            }

            if (newDataItems.ContainsKey(errorKey))
            {
                if (dataItems.ContainsKey(errorKey)) dataItems[errorKey] = newDataItems[errorKey].ToString();
                else dataItems.Add(errorKey, newDataItems[errorKey]);
                resList.Add(dataItems);
            }
            else
            {
                resList.Add(newDataItems);
            }
        }

        return resList;
    }

    /// <summary>
    /// 处理静态数据.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetStaticList(FieldsModel model)
    {
        PropsBeanModel? props = model.props;
        List<OptionsModel>? optionList = GetTreeOptions(model.options, props);
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        foreach (OptionsModel? item in optionList)
        {
            Dictionary<string, string> option = new Dictionary<string, string>();
            option.Add(item.value, item.label);
            list.Add(option);
        }

        return list;
    }

    /// <summary>
    /// options无限级.
    /// </summary>
    /// <returns></returns>
    private List<OptionsModel> GetTreeOptions(List<Dictionary<string, object>> model, PropsBeanModel props)
    {
        List<OptionsModel> options = new List<OptionsModel>();
        foreach (object? item in model)
        {
            OptionsModel option = new OptionsModel();
            Dictionary<string, object>? dicObject = item.ToJsonString().ToObject<Dictionary<string, object>>();
            option.label = dicObject[props.label].ToString();
            option.value = dicObject[props.value].ToString();
            if (dicObject.ContainsKey(props.children))
            {
                List<Dictionary<string, object>>? children = dicObject[props.children].ToJsonString().ToObject<List<Dictionary<string, object>>>();
                options.AddRange(GetTreeOptions(children, props));
            }

            options.Add(option);
        }

        return options;
    }

    /// <summary>
    /// 获取动态无限级数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetDynamicInfiniteData(string data, PropsBeanModel props)
    {
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        string? value = props.value;
        string? label = props.label;
        string? children = props.children;
        foreach (JToken? info in JToken.Parse(data))
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic[info.Value<string>(value)] = info.Value<string>(label);
            list.Add(dic);
            if (info.Value<object>(children) != null && info.Value<object>(children).ToString() != string.Empty)
                list.AddRange(GetDynamicInfiniteData(info.Value<object>(children).ToString(), props));
        }

        return list;
    }

    /// <summary>
    /// 动态参数的转换.
    /// </summary>
    /// <param name="dynamicParameter"></param>
    /// <returns></returns>
    private List<object> DynamicParameterConversion(List<object> dynamicParameter)
    {
        var list = new List<object>();
        foreach (var item in dynamicParameter)
        {
            if (item.ToString().Contains("["))
            {
                var str = item.ToObject<List<string>>().LastOrDefault();
                list.AddRange(ReplaceParameter(str));
            }
            else
            {
                list.AddRange(ReplaceParameter(item.ToString()));
            }
        }
        return list;
    }

    /// <summary>
    /// 替换参数.
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    private List<string> ReplaceParameter(string parameter)
    {
        // 获取所有组织
        List<OrganizeEntity>? allOrgList = GetOrgListTreeName();
        var result = new List<string>();
        switch (parameter)
        {
            case "@currentOrg":
                result.Add(_userManager.User.OrganizeId);
                break;
            case "@currentOrgAndSubOrg":
                result.AddRange(allOrgList.TreeChildNode(_userManager.User.OrganizeId, t => t.Id, t => t.ParentId).Select(it => it.Id).ToList());
                break;
            case "@currentGradeOrg":
                if (_userManager.IsAdministrator)
                {
                    result.AddRange(allOrgList.Select(it => it.Id).ToList());
                }
                else
                {
                    result.AddRange(_userManager.DataScope.Select(x => x.organizeId).ToList());
                }
                break;
            default:
                result.Add(parameter);
                break;
        }
        return result;
    }

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    private List<OrganizeEntity> GetOrgListTreeName()
    {
        List<OrganizeEntity>? orgTreeNameList = new List<OrganizeEntity>();
        List<OrganizeEntity>? orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToList();
        orgList.ForEach(item =>
        {
            if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
            OrganizeEntity? newItem = item.Adapt<OrganizeEntity>();
            newItem.Id = item.Id;
            var orgNameList = new List<string>();
            item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
            {
                var org = orgList.Find(x => x.Id == it);
                if (org != null) orgNameList.Add(org.FullName);
            });
            newItem.Description = string.Join("/", orgNameList);
            orgTreeNameList.Add(newItem);
        });

        return orgTreeNameList;
    }

    /// <summary>
    /// 处理复杂表头.
    /// </summary>
    /// <returns></returns>
    private static List<ComplexHeaderModel> GetComplexHeaderList(List<ParamsModel> paramList, List<string> selectKey)
    {
        var complexHeaderList = new List<ComplexHeaderModel>();
        if (paramList != null && paramList.Any(x => x.value.Contains("@@")))
        {
            var pList = paramList.Copy();
            pList.Where(x => x.value.Contains("@@")).ToList().ForEach(it =>
            {
                var hList = it.value.Split("@@");
                if (!complexHeaderList.Any(x => x.id.Equals(hList.First())))
                {
                    var addItem = new ComplexHeaderModel()
                    {
                        id = hList[0],
                        fullName = hList[1],
                        align = hList[2],
                        childColumns = new List<string>()
                    };
                    addItem.childColumns = pList.Where(x => x.value.Contains(addItem.id + "@@" + addItem.fullName) && selectKey.Contains(x.field)).Select(x => x.field).ToList();
                    if (addItem.childColumns.Any()) complexHeaderList.Add(addItem);
                }

                it.value = hList.Last();
            });
        }

        return complexHeaderList;
    }

    /// <summary>
    /// 处理导入的复杂表头.
    /// </summary>
    /// <returns></returns>
    private static List<ParamsModel> GetParamsModelListByFirstData(List<Dictionary<string, object>> dataList)
    {
        var paramList = new List<ParamsModel>();
        if (dataList != null && dataList.Any())
        {
            if (dataList.First().Keys.Any(x => x.Contains("@@")))
            {
                // 从第一行捞取表头
                var firstDic = dataList.First();
                var newDic = new Dictionary<string, object>();
                foreach (var item in firstDic)
                {
                    if (item.Key.Contains("@@"))
                    {
                        var hList = item.Key.Split("@@");
                        newDic.Add(hList.Last(), item.Value);
                        var pmodel = hList.Last().Split("(");
                        paramList.Add(new ParamsModel() { field = pmodel.Last().Replace(")", ""), value = item.Key.Split("(").FirstOrDefault() });
                    }
                    else
                    {
                        newDic.Add(item.Key, item.Value);
                        var pmodel = item.Key.Split("(");
                        paramList.Add(new ParamsModel() { field = pmodel.Last().Replace(")", ""), value = item.Key.Split("(").FirstOrDefault() });
                    }
                }

                // 重新定义表头
                dataList[0] = newDic;
            }
            else
            {
                var firstDic = dataList.First();
                var newDic = new Dictionary<string, object>();
                foreach (var item in firstDic)
                {
                    newDic.Add(item.Key, item.Value);
                    var pmodel = item.Key.Split("(");
                    paramList.Add(new ParamsModel() { field = pmodel.Last().Replace(")", ""), value = item.Key.Split("(").FirstOrDefault() });
                }

            }
        }

        return paramList;
    }
}
