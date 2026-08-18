using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Extensions;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发APP基础.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "App", Order = 175)]
[Route("api/visualdev/OnlineDev/[controller]")]
public class VisualdevModelAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 可视化开发基础.
    /// </summary>
    private readonly IVisualDevService _visualDevService;

    /// <summary>
    /// 在线开发运行服务.
    /// </summary>
    private readonly IRunService _runService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 初始化一个<see cref="VisualdevModelAppService"/>类型的新实例.
    /// </summary>
    public VisualdevModelAppService(
        IVisualDevService visualDevService,
        IRunService runService,
        IUserManager userManager,
        IFileManager fileManager)
    {
        _visualDevService = visualDevService;
        _runService = runService;
        _userManager = userManager;
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// 获取列表表单配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="type">1 线上版本, 0 草稿版本.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/Config")]
    [NonUnify]
    public async Task<dynamic> GetData(string modelId, string type)
    {
        if (type.IsNullOrEmpty()) type = "1";
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId, type.Equals("1"));
        if (data == null) return new { code = 400, msg = "未找到该模板!" };
        if (data.EnableFlow.Equals(1) && data.FlowId.IsNullOrWhiteSpace()) return new { code = 400, msg = "该流程功能未绑定流程!" };
        if (data.WebType.Equals(1) && data.FormData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内表单内容为空，无法预览!" };
        else if (data.WebType.Equals(2) && data.ColumnData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内列表内容为空，无法预览!" };
        return new { code = 200, data = data.Adapt<VisualDevModelDataConfigOutput>() };
    }

    /// <summary>
    /// 获取列表配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/ColumnData")]
    public async Task<dynamic> GetColumnData(string modelId)
    {
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId);
        return new { columnData = data.ColumnData };
    }

    /// <summary>
    /// 获取列表配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/FormData")]
    public async Task<dynamic> GetFormData(string modelId)
    {
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId);
        return new { formData = data.FormData };
    }

    /// <summary>
    /// 获取数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpGet("{modelId}/{id}")]
    public async Task<dynamic> GetInfo(string id, string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true); // 模板实体
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = (await _runService.GetHaveTableInfo(id, templateEntity)).ToJsonString() }; // 有表
        else
            return null;
    }

    /// <summary>
    /// 获取详情.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpGet("{modelId}/{id}/DataChange")]
    public async Task<dynamic> GetDetails(string id, string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true); // 模板实体

        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = await _runService.GetHaveTableInfoDetails(id, templateEntity) }; // 有表
        else
            return null;
    }

    #endregion

    #region Post

    /// <summary>
    /// 功能导出.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId); // 模板实体
        if (templateEntity.State.Equals(1))
        {
            var vREntity = await _visualDevService.GetInfoById(modelId, true);
            templateEntity = vREntity.Adapt<VisualDevEntity>();
            templateEntity.State = 0;
        }
        string? jsonStr = templateEntity.ToJsonString();
        return await _fileManager.Export(jsonStr, templateEntity.FullName, ExportFileType.va);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    [UnitOfWork]
    public async Task ActionsImport(IFormFile file, int type)
    {
        string? fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.va.ToString())) throw Oops.Oh(ErrorCode.D3006);
        string? josn = _fileManager.Import(file);
        VisualDevEntity? templateEntity;
        try
        {
            templateEntity = josn.ToObject<VisualDevEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }

        if (templateEntity == null || templateEntity.Type.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D3006);
        else if (templateEntity.Type != 1) throw Oops.Oh(ErrorCode.D3009);
        await _visualDevService.CreateImportData(templateEntity, type);
    }

    /// <summary>
    /// 获取数据列表.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="input">分页查询条件.</param>
    /// <returns></returns>
    [HttpPost("{modelId}/List")]
    public async Task<dynamic> List(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        return await _runService.GetListResult(templateEntity, input);
    }

    /// <summary>
    /// 创建数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="visualdevModelDataCrForm"></param>
    /// <returns></returns>
    [HttpPost("{modelId}")]
    public async Task<dynamic> Create(string modelId, [FromBody] VisualDevModelDataCrInput visualdevModelDataCrForm)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        string id = await _runService.Create(templateEntity, visualdevModelDataCrForm);

        return new { id = id }; // 有表
    }

    /// <summary>
    /// 修改数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="id"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    [HttpPut("{modelId}/{id}")]
    public async Task Update(string modelId, string id, [FromBody] VisualDevModelDataUpInput visualdevModelDataUpForm)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        await _runService.Update(id, templateEntity, visualdevModelDataUpForm);
    }

    /// <summary>
    /// 删除数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpDelete("{modelId}/{id}")]
    public async Task Delete(string id, string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            await _runService.DelHaveTableInfo(id, templateEntity);
    }

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("batchDelete/{modelId}")]
    public async Task BatchDelete(string modelId, [FromBody] VisualDevModelDataBatchDelInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            await _runService.BatchDelHaveTableData(input.ids, templateEntity, input);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/ExportData")]
    public async Task<dynamic> ExportData(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        PageResult<Dictionary<string, object>>? pageList = await _runService.GetListResult(templateEntity, input);

        #region 如果是 分组表格 模板

        ColumnDesignModel? ColumnData = templateEntity.ColumnData.ToObject<ColumnDesignModel>(); // 列配置模型
        if (ColumnData.type == 3)
        {
            List<Dictionary<string, object>>? newValueList = new List<Dictionary<string, object>>();
            pageList.list.ForEach(item =>
            {
                List<Dictionary<string, object>>? tt = item["children"].ToJsonString().ToObject<List<Dictionary<string, object>>>();
                newValueList.AddRange(tt);
            });
            pageList.list = newValueList;
        }
        #endregion

        List<Dictionary<string, object>> realList = pageList.list.Copy();
        var templateInfo = new TemplateParsingBase(templateEntity);
        var res = GetCreateFirstColumnsHeader(input.selectKey, realList, templateInfo.AllFieldsModel);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        var newResultList = new List<Dictionary<string, object>>();
        resultList.ForEach(row =>
        {
            foreach (var item in input.selectKey)
            {
                if (row[item].IsNotEmptyOrNull())
                {
                    newResultList.Add(row);
                    break;
                }
            }
        });
        return firstColumns.Any() ? await ExcelCreateModel(templateInfo, resultList, input.selectKey, null, firstColumns) : await ExcelCreateModel(templateInfo, resultList, input.selectKey);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// Excel 转输出 Model.
    /// </summary>
    /// <param name="templateInfo">模板.</param>
    /// <param name="realList">数据列表.</param>
    /// <param name="keys"></param>
    /// <param name="excelName">导出文件名称.</param>
    /// <param name="firstColumns">手动输入第一行（合并主表列和各个子表列）.</param>
    /// <returns>VisualDevModelDataExportOutput.</returns>
    public async Task<VisualDevModelDataExportOutput> ExcelCreateModel(TemplateParsingBase templateInfo, List<Dictionary<string, object>> realList, List<string> keys, string excelName = null, Dictionary<string, int> firstColumns = null)
    {
        List<ExcelTemplateModel> templateList = new List<ExcelTemplateModel>();
        VisualDevModelDataExportOutput output = new VisualDevModelDataExportOutput();
        List<string> columnList = new List<string>();
        try
        {
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = (excelName.IsNullOrEmpty() ? SnowflakeIdHelper.NextId() : excelName) + ".xls";
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (string? item in keys)
            {
                FieldsModel? excelColumn = templateInfo.AllFieldsModel.Find(t => t.__vModel__ == item);
                if (excelColumn != null)
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
            output.name = excelconfig.FileName;
            output.url = "/api/file/Download?encryption=" + DESCEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF");
            return output;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 组装导出带子表得数据,返回 第一个合并行标头,第二个导出数据.
    /// </summary>
    /// <param name="selectKey">导出选择列.</param>
    /// <param name="realList">原数据集合.</param>
    /// <param name="fieldList">控件列表.</param>
    /// <returns>第一行标头 , 导出数据.</returns>
    public (Dictionary<string, int>, List<Dictionary<string, object>>) GetCreateFirstColumnsHeader(List<string> selectKey, List<Dictionary<string, object>> realList, List<FieldsModel> fieldList)
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
                    var ctList = item.Value.ToObject<List<Dictionary<string, object>>>();
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
                    var title = fieldList.FirstOrDefault(x => x.__vModel__.Equals(item))?.__config__.label;
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
    #endregion
}
