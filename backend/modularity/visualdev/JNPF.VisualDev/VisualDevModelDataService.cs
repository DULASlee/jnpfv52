using JNPF.Common.Configuration;
using JNPF.Extensions;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发基础.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "OnlineDev", Order = 172)]
[Route("api/visualdev/[controller]")]
public class VisualDevModelDataService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _visualDevRepository;  // 在线开发功能实体

    /// <summary>
    /// 可视化开发基础.
    /// </summary>
    private readonly IVisualDevService _visualDevService;

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
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 工作流.
    /// </summary>
    private readonly IFlowTaskService _flowTaskService;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 初始化一个<see cref="VisualDevModelDataService"/>类型的新实例.
    /// </summary>
    public VisualDevModelDataService(
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        IVisualDevService visualDevService,
        RunService runService,
        FormDataParsing formDataParsing,
        IDbLinkService dbLinkService,
        IDataInterfaceService dataInterfaceService,
        IUserManager userManager,
        IDataBaseManager databaseService,
        IBillRullService billRuleService,
        ICacheManager cacheManager,
        IFlowTaskService flowTaskService,
        IFileManager fileManager,
        ISqlSugarClient context,
        IOrganizeService organizeService,
        IEventPublisher eventPublisher)
    {
        _visualDevRepository = visualDevRepository;
        _visualDevService = visualDevService;
        _databaseService = databaseService;
        _dbLinkService = dbLinkService;
        _runService = runService;
        _formDataParsing = formDataParsing;
        _billRuleService = billRuleService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _flowTaskService = flowTaskService;
        _fileManager = fileManager;
        _dataInterfaceService = dataInterfaceService;
        _db = context.AsTenant();
        _organizeService = organizeService;
        _eventPublisher = eventPublisher;
    }

    #region Get

    /// <summary>
    /// 获取列表表单配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="type">1 线上版本, 0 草稿版本.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/Config")]
    public async Task<dynamic> GetData(string modelId, string type)
    {
        if (type.IsNullOrEmpty()) type = "1";
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId, type.Equals("1"));
        if (data == null) throw Oops.Bah(ErrorCode.COM1018, "该表单已删除");
        if (data.EnableFlow.Equals(-1) && data.FlowId.IsNotEmptyOrNull()) throw Oops.Bah(ErrorCode.COM1018, "该功能配置的流程已停用!");
        if (data.EnableFlow.Equals(1) && data.FlowId.IsNullOrWhiteSpace()) throw Oops.Bah(ErrorCode.COM1018, "该流程功能未绑定流程!");
        if (data.WebType.Equals(1) && data.FormData.IsNullOrWhiteSpace()) throw Oops.Bah(ErrorCode.COM1018, "该模板内表单内容为空，无法预览!");
        else if (data.WebType.Equals(2) && data.ColumnData.IsNullOrWhiteSpace()) throw Oops.Bah(ErrorCode.COM1018, "该模板内列表内容为空，无法预览!");
        return GetVisualDevModelDataConfig(data);
    }

    ///获取历史版本数据
    [HttpGet("{modelId}/HistoryConfig")]
    [NonUnify]
    public async Task<dynamic> GetHistoryData(string modelId, string type)
    {
        if (type.IsNullOrEmpty()) type = "1";
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId, type.Equals("1"));

        if (data == null) return new { code = 400, msg = "该表单已删除" };

        if (data.ParentId.IsNotEmptyOrNull())
        {
            VisualDevEntity? parentData = await _visualDevService.GetInfoById(data.ParentId, type.Equals("1"));
            data.FlowId = parentData.FlowId;
            data.EnableFlow = parentData.EnableFlow;
        }
        if (data.EnableFlow.Equals(-1) && data.FlowId.IsNotEmptyOrNull()) return new { code = 400, msg = "该功能配置的流程已停用!" };
        if (data.EnableFlow.Equals(1) && data.FlowId.IsNullOrWhiteSpace()) return new { code = 400, msg = "该流程功能未绑定流程!" };
        if (data.WebType.Equals(1) && data.FormData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内表单内容为空，无法预览!" };
        else if (data.WebType.Equals(2) && data.ColumnData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内列表内容为空，无法预览!" };
        return new { code = 200, data = GetVisualDevModelDataConfig(data) };
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

        // 有表
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = (await _runService.GetHaveTableInfo(id, templateEntity)).ToJsonString() };
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

        // 有表
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = await _runService.GetHaveTableInfoDetails(id, templateEntity) };
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
            var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == modelId && v.DeleteMark == null);
            templateEntity = vREntity.Adapt<VisualDevEntity>();
            templateEntity.State = 0;
        }
        string? jsonStr = templateEntity.ToJsonString();
        return await _fileManager.Export(jsonStr, templateEntity.FullName, ExportFileType.vdd);
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
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.vdd.ToString())) throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
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
    [UnifySerializerSetting("special")]
    public async Task<dynamic> List(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        return await _runService.GetListResult(templateEntity, input);
    }

    /// <summary>
    /// 外链获取数据列表.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="input">分页查询条件.</param>
    /// <returns></returns>
    [HttpPost("{modelId}/ListLink")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> ListLink(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (templateEntity == null) throw Oops.Oh(ErrorCode.D1420);
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

        return new { id = id };
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
    /// 修改数据（集成助手）.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    [HttpPut("batchUpdate/{modelId}")]
    public async Task BatchUpdate(string modelId, [FromBody] VisualDevModelDataUpInput visualdevModelDataUpForm)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        await _runService.BatchUpdate(visualdevModelDataUpForm.idList, templateEntity, visualdevModelDataUpForm);
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
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables)) await _runService.DelHaveTableInfo(id, templateEntity);
    }

    /// <summary>
    /// 删除集成助手数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpDelete("DelInteAssistant/{modelId}")]
    public async Task DelInteAssistant(string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables)) await _runService.DelInteAssistant(templateEntity);
    }

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("batchDelete/{modelId}")]
    [UnitOfWork]
    public async Task BatchDelete(string modelId, [FromBody] VisualDevModelDataBatchDelInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables)) await _runService.BatchDelHaveTableData(input.ids, templateEntity, input);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/ExportData")]
    public async Task<dynamic> ExportData(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (input.dataType == "1")
        {
            input.pageSize = 99999999;
            input.currentPage = 1;
        }
        PageResult<Dictionary<string, object>>? pageList = await _runService.GetListResult(templateEntity, input);

        // 如果是 分组表格 模板
        ColumnDesignModel? columnData = templateEntity.ColumnData.ToObject<ColumnDesignModel>(); // 列配置模型
        if (columnData.type == 3)
        {
            List<Dictionary<string, object>>? newValueList = new List<Dictionary<string, object>>();
            pageList.list.ForEach(item =>
            {
                List<Dictionary<string, object>>? tt = item["children"].ToJsonString().ToObject<List<Dictionary<string, object>>>();
                newValueList.AddRange(tt);
            });
            pageList.list = newValueList;
        }

        // 导出当前选择数据
        var selectList = new List<Dictionary<string, object>>();
        if (input.dataType == "2" && input.selectIds.Any())
        {
            foreach (var item in pageList.list)
            {
                if (item.ContainsKey("id") && input.selectIds.Contains(item["id"]))
                    selectList.Add(item);
            }
            pageList.list = selectList;
            pageList.pagination.total = selectList.Count;
        }

        var templateInfo = new TemplateParsingBase(templateEntity);
        var res = GetCreateFirstColumnsHeader(input.selectKey, pageList.list, templateInfo.AllFieldsModel, templateInfo.ColumnData);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        var newResultList = new List<Dictionary<string, object>>();

        // 行内编辑
        if (templateInfo.ColumnData.type.Equals(4))
        {
            resultList.ForEach(row =>
            {
                foreach (var data in row) if (data.Key.Contains("_name") && row.ContainsKey(data.Key.Replace("_name", string.Empty))) row[data.Key.Replace("_name", string.Empty)] = data.Value;
            });
        }

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

        if (!newResultList.Any())
        {
            var dic = new Dictionary<string, object>();
            dic.Add("id", "id");
            foreach (var item in input.selectKey) dic.Add(item, string.Empty);
            newResultList.Add(dic);
        }

        var menuName = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.Id.Equals(input.menuId)).Select(it => it.FullName).FirstAsync();
        var excelName = string.Format("{0}_{1}", menuName, DateTime.Now.ToString("yyyyMMddHHmmss"));
        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any() ? await ExcelCreateModel(templateInfo.AllFieldsModel, newResultList, input.selectKey, excelName, firstColumns)
            : await ExcelCreateModel(templateInfo.AllFieldsModel, newResultList, input.selectKey, excelName);
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{modelId}/TemplateDownload")]
    public async Task<dynamic> TemplateDownload(string modelId)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);

        if (tInfo.selectKey == null || !tInfo.selectKey.Any()) throw Oops.Oh(ErrorCode.D1411);

        // 初始化 一条空数据
        List<Dictionary<string, object>>? dataList = new List<Dictionary<string, object>>();

        // 赋予默认值
        var dicItem = new Dictionary<string, object>();
        tInfo.AllFieldsModel.Where(x => tInfo.selectKey.Contains(x.__vModel__)).ToList().ForEach(item =>
        {
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.CREATEUSER:
                case JnpfKeyConst.MODIFYUSER:
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.MODIFYTIME:
                case JnpfKeyConst.CURRORGANIZE:
                case JnpfKeyConst.CURRPOSITION:
                case JnpfKeyConst.CURRDEPT:
                case JnpfKeyConst.BILLRULE:
                    dicItem.Add(item.__vModel__, "系统自动生成");
                    break;
                case JnpfKeyConst.COMSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:引迈信息/产品部,引迈信息/技术部" : "例:引迈信息/技术部");
                    break;
                case JnpfKeyConst.DEPSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:产品部/部门编码,技术部/部门编码" : "例:技术部/部门编码");
                    break;
                case JnpfKeyConst.POSSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:技术经理/岗位编码,技术员/岗位编码" : "例:技术员/岗位编码");
                    break;
                case JnpfKeyConst.USERSSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:引迈信息/产品部,产品部/部门编码,技术经理/岗位编码,研发人员/角色编码,A分组/分组编码,张三/账号" : "例:李四/账号");
                    break;
                case JnpfKeyConst.USERSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:张三/账号,李四/账号" : "例:张三/账号");
                    break;
                case JnpfKeyConst.ROLESELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:研发人员/角色编码,测试人员/角色编码" : "例:研发人员/角色编码");
                    break;
                case JnpfKeyConst.GROUPSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "例:A分组/分组编码,B分组/分组编码" : "例:A分组/分组编码");
                    break;
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.TIME:
                    dicItem.Add(item.__vModel__, string.Format("例:{0}", item.format));
                    break;
                case JnpfKeyConst.ADDRESS:
                    switch (item.level)
                    {
                        case 0:
                            dicItem.Add(item.__vModel__, item.multiple ? "例:福建省,广东省" : "例:福建省");
                            break;
                        case 1:
                            dicItem.Add(item.__vModel__, item.multiple ? "例:福建省/莆田市,广东省/广州市" : "例:福建省/莆田市");
                            break;
                        case 2:
                            dicItem.Add(item.__vModel__, item.multiple ? "例:福建省/莆田市/城厢区,广东省/广州市/荔湾区" : "例:福建省/莆田市/城厢区");
                            break;
                        case 3:
                            dicItem.Add(item.__vModel__, item.multiple ? "例:福建省/莆田市/城厢区/霞林街道,广东省/广州市/荔湾区/沙面街道" : "例:福建省/莆田市/城厢区/霞林街道");
                            break;
                    }
                    break;
                default:
                    dicItem.Add(item.__vModel__, string.Empty);
                    break;
            }
        });
        dicItem.Add("id", "id");
        dataList.Add(dicItem);

        var cData = await GetCDataList(tInfo.AllFieldsModel, new Dictionary<string, List<Dictionary<string, string>>>());

        var excelName = string.Format("{0} 导入模板_{1}", tInfo.FullName, SnowflakeIdHelper.NextId());
        var res = GetCreateFirstColumnsHeader(tInfo.selectKey, dataList, tInfo.AllFieldsModel, tInfo.ColumnData);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any() ? await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, excelName, firstColumns)
            : await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, excelName);
    }

    /// <summary>
    /// 模板与数据下载. 
    /// </summary>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/TemplateDataDownload")]
    public async Task<dynamic> TemplateDataDownload(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (input.dataType == "1")
        {
            input.pageSize = 99999999;
            input.currentPage = 1;
        }
        PageResult<Dictionary<string, object>>? pageList = await _runService.GetListResult(templateEntity, input);


        var tInfo = await GetUploaderTemplateInfoAsync(modelId);

        if (tInfo.selectKey == null || !tInfo.selectKey.Any()) throw Oops.Oh(ErrorCode.D1411);

        // 初始化 一条空数据
        List<Dictionary<string, object>>? dataList = new List<Dictionary<string, object>>();

        var cData = await GetCDataList(tInfo.AllFieldsModel, new Dictionary<string, List<Dictionary<string, string>>>());

        var excelName = string.Format("{0} 导入模板_{1}", tInfo.FullName, SnowflakeIdHelper.NextId());
        var res = GetCreateFirstColumnsHeader(tInfo.selectKey, pageList.list, tInfo.AllFieldsModel, tInfo.ColumnData);
        var firstColumns = res.Item1;
        var resultList = res.Item2;

        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any() ? await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, excelName, firstColumns)
            : await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, excelName);
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task<dynamic> Uploader(IFormFile file)
    {
        var _filePath = _fileManager.GetPathByType(string.Empty);
        var _fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + SnowflakeIdHelper.NextId() + Path.GetExtension(file.FileName);
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, _filePath, _fileName);
        return new { name = _fileName, url = string.Format("/api/File/Image/{0}/{1}", string.Empty, _fileName) };
    }

    /// <summary>
    /// 导入预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{modelId}/ImportPreview")]
    public async Task<dynamic> ImportPreview(string modelId, string fileName)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);

        var resData = new List<Dictionary<string, object>>();
        var headerRow = new List<dynamic>();

        var isChildTable = tInfo.selectKey.Any(x => tInfo.ChildTableFields.ContainsKey(x));

        // 复杂表头
        if (!tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5) && tInfo.ColumnData.complexHeaderList.Any())
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
                excelData.Columns[item.ToString()].ColumnName = FileEncode.Where(x => x.__config__.label == item.ToString()).FirstOrDefault().__vModel__;
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
                        if ((item.Key.Contains("tablefield") || item.Key.Contains("tableField")) && item.Key.Contains("-"))
                        {
                            var childVModel = item.Key.Split("-").First();
                            if (!headerRow.Any(x => x.id.Equals(childVModel)))
                            {
                                var child = new List<dynamic>();
                                hRow.Where(x => x.Key.Contains(childVModel)).ToList().ForEach(x =>
                                {
                                    child.Add(new { id = x.Key.Replace(childVModel + "-", string.Empty), fullName = x.Value.ToString().Replace(string.Format("({0})", x.Key), string.Empty) });
                                });
                                headerRow.Add(new { id = childVModel, fullName = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(childVModel)).__config__.label.Replace(string.Format("({0})", childVModel), string.Empty), jnpfKey = "table", children = child });
                            }
                        }
                        else if (tInfo.ColumnData.complexHeaderList.Count > 0 && tInfo.ColumnData.complexHeaderList.Any(it => it.childColumns.Contains(item.Key)))
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
                                headerRow.Add(new { id = complexHeaderModel.id, fullName = complexHeaderModel.fullName, jnpfKey = "complexHeader", children = child });
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
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1410);
        }

        try
        {
            // 带子表字段数据导入
            if (isChildTable)
            {
                var newData = new List<Dictionary<string, object>>();
                var singleForm = tInfo.selectKey.Where(x => !x.Contains("tablefield") && !x.Contains("tableField")).ToList();

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
                            tInfo.selectKey.Where(x => x.Contains(item)).ToList().ForEach(it =>
                            {
                                if (dataItem.ContainsKey(it))
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
                                    childAddItem.Add(it.Replace(citem + "-", string.Empty), dataItem[it]);
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
                                    if (dataItem.ContainsKey(it))
                                        childAddItem.Add(it.Replace(item + "-", string.Empty), dataItem[it]);
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
                                ctItems[ctItem.Key] = string.Format("{0:" + vmodel.format + "} ", ctItem.Value);
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
    /// 导入数据的错误报告.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/ImportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData(string modelId, [FromBody] VisualDevImportDataInput list)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);
        //object[]? res = await ImportMenuData(tInfo, list.list, tInfo.visualDevEntity);

        // 错误数据
        tInfo.selectKey.Add("errorsInfo");
        tInfo.AllFieldsModel.Add(new FieldsModel() { __vModel__ = "errorsInfo", __config__ = new ConfigModel() { label = "异常原因" } });
        for (var i = 0; i < list.list.Count(); i++) list.list[i].Add("id", i);

        var result = GetCreateFirstColumnsHeader(tInfo.selectKey, list.list, tInfo.AllFieldsModel);
        var firstColumns = result.Item1;
        var resultList = result.Item2;

        _cacheManager.Set(string.Format("{0} 导入错误报告.xls", tInfo.FullName), string.Empty);
        return firstColumns.Any()
            ? await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, string.Format("{0} 导入错误报告", tInfo.FullName), firstColumns)
            : await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, string.Format("{0} 导入错误报告", tInfo.FullName));
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData(string modelId, [FromBody] VisualDevImportDataInput list)
    {
        if (list.flowId.IsNotEmptyOrNull())
        {
            foreach (var item in list.list)
            {
                item.Add("f_flow_id", list.flowId);
            }
        }

        var tInfo = await GetUploaderTemplateInfoAsync(modelId);
        object[]? res = await ImportMenuData(tInfo, list, tInfo.visualDevEntity);
        var addlist = res.First() as List<Dictionary<string, object>>;
        var errorlist = res.Last() as List<Dictionary<string, object>>;
        var result = new VisualDevImportDataOutput()
        {
            snum = addlist.Count,
            fnum = errorlist.Count,
            failResult = errorlist,
            resultType = errorlist.Count < 1 ? 0 : 1
        };

        return result;
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// Excel 转输出 Model.
    /// </summary>
    /// <param name="fieldList">控件集合.</param>
    /// <param name="realList">数据列表.</param>
    /// <param name="keys"></param>
    /// <param name="excelName">导出文件名称.</param>
    /// <param name="firstColumns">手动输入第一行（合并主表列和各个子表列）.</param>
    /// <returns>VisualDevModelDataExportOutput.</returns>
    public async Task<VisualDevModelDataExportOutput> ExcelCreateModel(List<FieldsModel> fieldList, List<Dictionary<string, object>> realList, List<string> keys, string excelName = null, Dictionary<string, int> firstColumns = null)
    {
        VisualDevModelDataExportOutput output = new VisualDevModelDataExportOutput();
        try
        {
            List<string> columnList = new List<string>();
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = (excelName.IsNullOrEmpty() ? SnowflakeIdHelper.NextId() : excelName) + ".xls";
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (string? item in keys)
            {
                FieldsModel? excelColumn = fieldList.Find(t => t.__vModel__ == item);
                if (excelColumn != null)
                {
                    var columnModel = new ExcelColumnModel() { Column = item, ExcelColumn = excelColumn.__config__.label };

                    //关于关联表单 table等考虑是没有必要实现，下拉类型已经支持数据字典表和远端数据，批量更新的数据完全可以使用下拉控件替换这类控件
                    if (excelColumn.__config__.jnpfKey == JnpfKeyConst.RELATIONFORM)
                    {
                        if (excelColumn.options != null)
                        {
                            // 从每个字典中取出所有的值，并将它们合并成一个 IEnumerable<object>
                            List<string> columnOptions = new List<string>();
                            foreach (var option in excelColumn.options)
                            {
                                string key = excelColumn.relationField;
                                if (option.ContainsKey(key) && option[key] != null)
                                {
                                    columnOptions.Add(option[key].ToString());
                                }
                            }

                            columnModel.Options = columnOptions.ToArray();
                        }
                    }

                    else if (excelColumn.options != null)
                    {
                        // 从每个字典中取出所有的值，并将它们合并成一个 IEnumerable<object>
                        List<string> columnOptions = new List<string>();
                        foreach (var option in excelColumn.options)
                        {
                            //远端数据
                            if (excelColumn.__config__.dataType == "dynamic")
                            {
                                string key = excelColumn.props.label;
                                columnOptions.Add(option[key].ToString());
                            }
                            else
                            {
                                string key = option.Keys.ToList()[0];
                                columnOptions.Add(option[key].ToString());
                            }
                        }

                        columnModel.Options = columnOptions.ToArray();
                    }

                    //下拉类型的都转换为下拉选择
                    if (excelColumn.__config__.jnpfKey == JnpfKeyConst.RELATIONFORM
                        || excelColumn.__config__.jnpfKey == JnpfKeyConst.CHECKBOX
                        || excelColumn.__config__.jnpfKey == JnpfKeyConst.SELECT
                        || excelColumn.__config__.jnpfKey == JnpfKeyConst.TREESELECT
                        || excelColumn.__config__.jnpfKey == JnpfKeyConst.POPUPSELECT
                        || excelColumn.__config__.jnpfKey == JnpfKeyConst.POPUPTABLESELECT
                        )
                    {
                        if (columnModel.Options == null)
                            columnModel.IsOptionFromList = true;
                    }

                    excelconfig.ColumnModel.Add(columnModel);
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
        catch (Exception e)
        {
            throw e;
        }
    }

    /// <summary>
    /// 组装导出带子表得数据,返回 第一个合并行标头,第二个导出数据.
    /// </summary>
    /// <param name="selectKey">导出选择列.</param>
    /// <param name="realList">原数据集合.</param>
    /// <param name="fieldList">控件列表.</param>
    /// <param name="columnDesignModel"></param>
    /// <returns>第一行标头 , 导出数据.</returns>
    public (Dictionary<string, int>, List<Dictionary<string, object>>) GetCreateFirstColumnsHeader(List<string> selectKey, List<Dictionary<string, object>> realList, List<FieldsModel> fieldList, ColumnDesignModel columnDesignModel = null)
    {
        // 是否有复杂表头
        var isComplexHeader = false;
        if (!columnDesignModel.type.Equals(3) && !columnDesignModel.type.Equals(5) && columnDesignModel.complexHeaderList.Any())
        {
            foreach (var item in columnDesignModel.complexHeaderList)
            {
                foreach (var subItem in item.childColumns)
                {
                    if (selectKey.Contains(subItem))
                    {
                        isComplexHeader = true;
                        break;
                    }
                }
            }
        }

        selectKey.ForEach(item =>
        {
            realList.ForEach(it =>
            {
                if (!it.ContainsKey(item)) it.Add(item, string.Empty);
            });
        });

        var addItemList = new List<Dictionary<int, Dictionary<string, object>>>();
        var num = 0;
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
                        var dicItem = new Dictionary<int, Dictionary<string, object>>();
                        dicItem.Add(num + 1, newRealItem);
                        addItemList.Add(dicItem);
                    }
                }
            }

            num++;
        });
        for (int i = 0; i < addItemList.Count; i++)
        {
            var dic = addItemList[i].FirstOrDefault();
            realList.Insert(dic.Key + i, dic.Value);
        }

        var resultList = new List<Dictionary<string, object>>();

        realList.ForEach(newRealItem =>
        {
            if (!resultList.Any(x => x["id"].Equals(newRealItem["id"]))) resultList.AddRange(realList.Where(x => x["id"].Equals(newRealItem["id"])).ToList());
        });

        var firstColumns = new Dictionary<string, int>();
        if (selectKey.Any(x => x.Contains("-") && x.ToLower().Contains("tablefield")) || isComplexHeader)
        {
            var empty = string.Empty;
            var keyList = selectKey.Select(x => x.Split("-").First()).Distinct().ToList();

            var complexHeaderField = new List<string>();
            var lastName = "jnpf-singlefield";
            foreach (var item in keyList)
            {
                if (item.ToLower().Contains("tablefield"))
                {
                    var title = fieldList.FirstOrDefault(x => x.__vModel__.Equals(item))?.__config__.label;
                    firstColumns.Add(title + empty, selectKey.Count(x => x.Contains(item)));
                    empty += " ";
                }
                else if (!complexHeaderField.Contains(item))
                {
                    var flag = false;
                    foreach (var ch in columnDesignModel.complexHeaderList)
                    {
                        if (ch.childColumns.Contains(item))
                        {
                            var columns = new List<string>();
                            foreach (var sk in selectKey)
                            {
                                if (ch.childColumns.Contains(sk)) columns.Add(sk);
                            }

                            // 调整 selectKey 顺序
                            var index = selectKey.IndexOf(item);
                            foreach (var col in columns)
                            {
                                selectKey.Remove(col);
                                selectKey.Insert(index, col);
                                index++;
                            }

                            complexHeaderField.AddRange(columns);
                            flag = true;
                            lastName = ch.fullName;
                            firstColumns[ch.fullName] = columns.Count;

                            break;
                        }
                    }

                    // 字段没在复杂表头
                    if (!flag)
                    {
                        if (lastName.Contains("jnpf-singlefield"))
                        {
                            if (firstColumns.ContainsKey("jnpf-singlefield" + empty))
                                firstColumns[lastName]++;
                            else
                                firstColumns.Add("jnpf-singlefield" + empty, 1);
                        }
                        else
                        {
                            empty += " ";
                            lastName = "jnpf-singlefield" + empty;
                            firstColumns.Add("jnpf-singlefield" + empty, 1);
                        }
                    }
                }
            }
        }

        return (firstColumns, resultList);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取导出模板信息.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    private async Task<TemplateParsingBase> GetUploaderTemplateInfoAsync(string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        var tInfo = new TemplateParsingBase(templateEntity);
        tInfo.DbLink = await _dbLinkService.GetInfo(templateEntity.DbLinkId);
        if (tInfo.DbLink == null) tInfo.DbLink = _databaseService.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName); // 当前数据库连接
        var tableList = _databaseService.GetFieldList(tInfo.DbLink, tInfo.MainTableName); // 获取主表所有列
        var mainPrimary = tableList.Find(t => t.primaryKey); // 主表主键
        if (mainPrimary == null || mainPrimary.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1402); // 主表未设置主键
        tInfo.MainPrimary = mainPrimary.field;
        tInfo.AllFieldsModel = tInfo.AllFieldsModel.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.COLORPICKER)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT)
        //&& !x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM) //modify by harry
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORMATTR)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.QRCODE)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.BARCODE)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE)).ToList();
        tInfo.AllFieldsModel.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item => item.__config__.label = string.Format("{0}({1})", item.__config__.label, item.__vModel__));
        return tInfo;
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="input"></param>
    /// <param name="tEntity">开发实体.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> ImportMenuData(TemplateParsingBase tInfo, VisualDevImportDataInput input, VisualDevEntity tEntity = null)
    {
        if (tInfo.ColumnData.complexHeaderList.Count > 0 && !tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5))
        {
            var complexHeaderIdList = tInfo.ColumnData.complexHeaderList.Select(it => it.id).ToList();
            foreach (var item in input.list)
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

        List<Dictionary<string, object>> userInputList = ImportFirstVerify(tInfo, input.list);
        List<FieldsModel> fieldsModelList = tInfo.AllFieldsModel.Where(x => tInfo.selectKey.Contains(x.__vModel__)).ToList();

        var successList = new List<Dictionary<string, object>>();
        var errorsList = new List<Dictionary<string, object>>();

        // 捞取控件解析数据
        var cData = await GetCDataList(tInfo.AllFieldsModel, new Dictionary<string, List<Dictionary<string, string>>>());
        var res = await ImportDataAssemble(tInfo.AllFieldsModel, userInputList, cData);
        res.Where(x => x.ContainsKey("errorsInfo")).ToList().ForEach(item => errorsList.Add(item));
        res.Where(x => !x.ContainsKey("errorsInfo")).ToList().ForEach(item => successList.Add(item));

        // 唯一验证已处理，入库前去掉.
        tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item => item.__config__.unique = false);

        var eventList = new List<object>();
        foreach (var item in successList)
        {
            if (item.ContainsKey("Update_MainTablePrimary_Id"))
            {
                string? mainId = item["Update_MainTablePrimary_Id"].ToString();
                var haveTableSql = await _runService.GetUpdateSqlByTemplate(tInfo, new VisualDevModelDataUpInput() { data = item.ToJsonString() }, mainId);
                foreach (var it in haveTableSql) await _databaseService.ExecuteSql(tInfo.DbLink, it); // 修改功能数据

                var eventData = item.Copy();
                eventData.Remove("Update_MainTablePrimary_Id");
                eventList.Add(new { id = mainId, data = eventData });
            }
            else
            {
                if (tInfo.visualDevEntity.EnableFlow.Equals(1))
                {
                    await _flowTaskService.Create(new Common.Models.WorkFlow.FlowTaskSubmitModel() { formData = item, flowId = input.flowId, flowUrgent = 1, status = 1 });
                }
                else
                {
                    string? mainId = SnowflakeIdHelper.NextId();
                    var haveTableSql = await _runService.GetCreateSqlByTemplate(tInfo, new VisualDevModelDataCrInput() { data = item.ToJsonString() }, mainId);

                    // 主表自增长Id.
                    if (haveTableSql.ContainsKey("MainTableReturnIdentity")) haveTableSql.Remove("MainTableReturnIdentity");
                    foreach (var it in haveTableSql)
                        await _databaseService.ExecuteSql(tInfo.DbLink, it.Key, it.Value); // 新增功能数据

                    eventList.Add(new { id = mainId, data = item });
                }
            }
        }

        errorsList.ForEach(item =>
        {
            if (item.ContainsKey("errorsInfo") && item["errorsInfo"].IsNotEmptyOrNull()) item["errorsInfo"] = item["errorsInfo"].ToString().TrimStart(',').TrimEnd(',');
        });

        // 添加集成助手`事件触发`导入事件
        if (input.isInteAssis)
        {
            await _eventPublisher.PublishAsync(new InteEventSource("Inte:CreateInte", _userManager.UserId, _userManager.TenantId, new InteAssiEventModel
            {
                ModelId = tEntity.Id,
                Data = eventList.ToJsonString(),
                TriggerType = 4,
            }));
        }

        return new object[] { successList, errorsList };
    }

    /// <summary>
    /// 导入功能数据初步验证.
    /// </summary>
    private List<Dictionary<string, object>> ImportFirstVerify(TemplateParsingBase tInfo, List<Dictionary<string, object>> list)
    {
        var errorKey = "errorsInfo";
        var resList = new List<Dictionary<string, object>>();
        list.ForEach(item =>
        {
            var addItem = item.Copy();
            addItem.Add(errorKey, string.Empty);
            resList.Add(addItem);
        });

        #region 验证必填控件
        var childTableList = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();
        var requiredList = tInfo.AllFieldsModel.Where(x => x.__config__.required).ToList();
        var VModelList = requiredList.Select(x => x.__vModel__).ToList();

        if (VModelList.Any())
        {
            var newResList = new List<Dictionary<string, object>>();
            resList.ForEach(items =>
            {
                var newItems = items.Copy();
                foreach (var item in items)
                {
                    if (item.Value.IsNullOrEmpty() && VModelList.Contains(item.Key))
                    {
                        var errorInfo = requiredList.Find(x => x.__vModel__.Equals(item.Key)).__config__.label + ": 值不能为空";
                        if (newItems.ContainsKey(errorKey)) newItems[errorKey] = newItems[errorKey] + "," + errorInfo;
                        else newItems.Add(errorKey, errorInfo);
                    }

                    // 子表
                    if (childTableList.Contains(item.Key))
                    {
                        item.Value.ToObject<List<Dictionary<string, object>>>().ForEach(childItems =>
                        {
                            foreach (var childItem in childItems)
                            {
                                if (childItem.Value.IsNullOrEmpty() && VModelList.Contains(item.Key + "-" + childItem.Key))
                                {
                                    var errorInfo = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key)).__config__.children.Find(x => x.__vModel__.Equals(item.Key + "-" + childItem.Key)).__config__.label + ": 值不能为空";
                                    if (newItems.ContainsKey(errorKey)) newItems[errorKey] = newItems[errorKey] + "," + errorInfo;
                                    else newItems.Add(errorKey, errorInfo);
                                }
                            }
                        });
                    }
                }
                newResList.Add(newItems);
            });
            resList = newResList;
        }
        #endregion

        #region 验证唯一
        var uniqueList = tInfo.AllFieldsModel.Where(x => x.__config__.unique).ToList();
        VModelList = uniqueList.Select(x => x.__vModel__).ToList();

        if (uniqueList.Any())
        {
            resList.ForEach(items =>
            {
                foreach (var item in items)
                {
                    if (VModelList.Contains(item.Key))
                    {
                        var vlist = new List<Dictionary<string, object>>();
                        resList.Where(x => x.ContainsKey(item.Key) && x.ContainsValue(item.Value)).ToList().ForEach(it =>
                        {
                            foreach (var dic in it)
                            {
                                if (dic.Value != null && item.Value != null && dic.Key.Equals(item.Key) && dic.Value.Equals(item.Value))
                                {
                                    vlist.Add(it);
                                    break;
                                }
                            }
                        });

                        if (vlist.Count > 1)
                        {
                            for (var i = 1; i < vlist.Count; i++)
                            {
                                var errorInfo = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key)).__config__.label + ": 值不能重复";
                                items[errorKey] = items[errorKey] + "," + errorInfo;
                            }
                        }
                    }

                    // 子表
                    var updateItemCList = new List<Dictionary<string, object>>();
                    var ctItemErrors = new List<string>();
                    if (childTableList.Contains(item.Key))
                    {
                        var itemCList = item.Value.ToObject<List<Dictionary<string, object>>>();
                        itemCList.ForEach(childItems =>
                        {
                            if (tInfo.dataType.Equals("2"))
                            {
                                foreach (var childItem in childItems)
                                {
                                    var uniqueKey = item.Key + "-" + childItem.Key;
                                    if (VModelList.Contains(uniqueKey))
                                    {
                                        var vlist = itemCList.Where(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)).ToList();
                                        if (!updateItemCList.Any(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)))
                                            updateItemCList.Add(vlist.Last());
                                    }
                                }
                            }
                            else
                            {
                                foreach (var childItem in childItems)
                                {
                                    var uniqueKey = item.Key + "-" + childItem.Key;
                                    if (VModelList.Contains(uniqueKey) && childItem.Value != null)
                                    {
                                        var vlist = itemCList.Where(x => x.ContainsKey(childItem.Key) && x.ContainsValue(childItem.Value)).ToList();
                                        if (vlist.Count > 1)
                                        {
                                            for (var i = 1; i < vlist.Count; i++)
                                            {
                                                var errorTxt = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(uniqueKey)).__config__.label + ": 值不能重复";
                                                if (!ctItemErrors.Any(x => x.Equals(errorTxt))) ctItemErrors.Add(errorTxt);
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }

                    if (tInfo.dataType.Equals("2") && updateItemCList.Any()) items[item.Key] = updateItemCList;
                    if (ctItemErrors.Any())
                    {
                        items[errorKey] = items[errorKey].IsNullOrEmpty() ? string.Join(",", ctItemErrors) : items[errorKey] + "," + string.Join(",", ctItemErrors);
                    }
                }
            });

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
                var uniqueList = new List<string>();
                tInfo.SingleFormData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item =>
                {
                    uniqueList.Add(item.__vModel__);
                    fieldList.Add(string.Format("{0}.{1} {2}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last(), item.__vModel__));
                    if (allDataMap.ContainsKey(item.__vModel__) && allDataMap[item.__vModel__] != null)
                    {
                        whereList.Add(new ConditionalCollections()
                        {
                            ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                        {
                            FieldName = string.Format("{0}.{1}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last()),
                            ConditionalType =allDataMap.ContainsKey(item.__vModel__) ? ConditionalType.Equal: ConditionalType.IsNullOrEmpty,
                            FieldValue = allDataMap.ContainsKey(item.__vModel__) ? allDataMap[item.__vModel__].ToString() : string.Empty,
                        })
                    }
                        });
                    }
                });

                var itemWhere = _visualDevRepository.AsSugarClient().SqlQueryable<dynamic>("@").Where(whereList).ToSqlString();
                if (!itemWhere.Equals("@"))
                {
                    var relationList = new List<string>();
                    var whereStr = string.Empty;
                    relationList.AddRange(relationKey);
                    if (relationList.Count > 0)
                    {
                        var whereRelation = string.Join(" and ", relationList);
                        whereStr = string.Format("({0}) and {1}", whereRelation, itemWhere.Split("WHERE").Last());
                    }
                    else
                    {
                        whereStr = itemWhere.Split("WHERE").Last();
                    }

                    var querStr = string.Format(
                        "select {0} from {1} where {2}",
                        string.Join(",", fieldList),
                        auxiliaryFieldList.Any() ? tInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : tInfo.MainTableName,
                        whereStr); // 多表， 联合查询
                    var res = _databaseService.GetSqlData(tInfo.DbLink, querStr).ToObject<List<Dictionary<string, string>>>();

                    if (res.Any())
                    {
                        var errorList = new List<string>();
                        var mainId = string.Empty;
                        var uniqueKey = string.Empty;
                        foreach (var item in uniqueList)
                        {
                            var list = res.FindAll(x => x[item].IsNotEmptyOrNull());
                            if (list.Any(x => x.Any(xx => xx.Key.Equals(item) && xx.Value.Equals(allDataMap[item].ToString()))))
                            {
                                if (mainId.IsNotEmptyOrNull() && !mainId.Equals(list.Find(x => x.Any(xx => xx.Key.Equals(item) && xx.Value.Equals(allDataMap[item].ToString())))[tInfo.MainPrimary]))
                                {
                                    allDataMap[errorKey] = "存在重复数据";
                                }
                                else
                                {
                                    mainId = list.Find(x => x.Any(xx => xx.Key.Equals(item) && xx.Value.Equals(allDataMap[item].ToString())))[tInfo.MainPrimary];
                                    uniqueKey = item;
                                }
                            }
                        }

                        if (tInfo.dataType.Equals("2"))
                        {
                            if (mainId.IsNotEmptyOrNull() && !allDataMap.ContainsKey("Update_MainTablePrimary_Id"))
                                allDataMap.Add("Update_MainTablePrimary_Id", mainId);
                        }
                        else
                        {
                            if (mainId.IsNotEmptyOrNull())
                            {
                                var errorInfo = tInfo.SingleFormData.First(x => x.__vModel__.Equals(uniqueKey))?.__config__.label + ": 值不能重复";
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
                            var allDataList = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1)
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
                            dataList.ForEach(item =>
                            {
                                if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
                                var orgNameList = new List<string>();
                                item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
                                {
                                    var org = allDataList.Find(x => x.Id == it);
                                    if (org != null) orgNameList.Add(org.FullName);
                                });
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.OrganizeIdTree, string.Join("/", orgNameList));
                                addItem.Add(dictionary);
                            });

                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ADDRESS:
                    {
                        string? addCacheKey = "Import_Address";

                        if (!resData.ContainsKey(JnpfKeyConst.ADDRESS))
                        {
                            if (_cacheManager.Exists(addCacheKey))
                            {
                                addItem = _cacheManager.Get(addCacheKey).ToObject<List<Dictionary<string, string>>>();
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                            else
                            {
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<ProvinceEntity>().Select(x => new ProvinceEntity { Id = x.Id, ParentId = x.ParentId, Type = x.Type, FullName = x.FullName }).ToListAsync();

                                // 处理省市区树
                                dataList.Where(x => x.Type == "1").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = item.FullName;
                                    item.Description = item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "2").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
                                    item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "3").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
                                    item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "4").ToList().ForEach(item =>
                                {
                                    ProvinceEntity? it = dataList.Find(x => x.Id == item.ParentId);
                                    if (it != null)
                                    {
                                        item.QuickQuery = it.QuickQuery + "/" + item.FullName;
                                        item.Description = it.Description + "," + item.Id;
                                        Dictionary<string, string> address = new Dictionary<string, string>();
                                        address.Add(item.Description, item.QuickQuery);
                                        addItem.Add(address);
                                    }
                                });
                                dataList.ForEach(it =>
                                {
                                    if (it.Description.IsNotEmptyOrNull())
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.Description, it.QuickQuery);
                                        addItem.Add(dictionary);
                                    }
                                });

                                var noTypeList = dataList.Where(x => x.Type.IsNullOrWhiteSpace()).ToList();
                                foreach (var it in noTypeList)
                                {
                                    it.QuickQuery = GetAddressByPList(noTypeList, it);
                                    it.Description = GetAddressIdByPList(noTypeList, it);
                                }
                                foreach (var it in noTypeList)
                                {
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(it.Description, it.QuickQuery);
                                    addItem.Add(address);
                                }

                                _cacheManager.Set(addCacheKey, addItem, TimeSpan.FromDays(7)); // 缓存七天
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.GROUPSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new GroupEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom"))
                            {
                                dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            }
                            dataList.ForEach(item =>
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.Id, item.EnCode);
                                addItem.Add(dictionary);
                            });
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ROLESELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new RoleEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom"))
                            {
                                item.ableIds = DynamicParameterConversion(item.ableIds);
                                var relationIds = await _visualDevRepository.AsSugarClient().Queryable<OrganizeRelationEntity>()
                                    .Where(it => item.ableIds.Contains(it.OrganizeId) && it.ObjectType.Equals("Role"))
                                    .Select(it => it.ObjectId).ToListAsync();
                                item.ableIds.AddRange(relationIds);
                                dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            }
                            dataList.ForEach(item =>
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.Id, item.EnCode);
                                addItem.Add(dictionary);
                            });
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.SWITCH:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            dictionary.Add("1", item.activeTxt);
                            addItem.Add(dictionary);
                            Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                            dictionary2.Add("0", item.inactiveTxt);
                            addItem.Add(dictionary2);
                            resData.Add(item.__vModel__, addItem);
                        }
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
                            if (item.props != null)
                            {
                                propsValue = item.props.value;
                                propsLabel = item.props.label;
                                children = item.props.children;
                            }

                            if (item.__config__.dataType.Equals("static"))
                            {
                                if (item != null && item.options != null)
                                {
                                    item.options.ForEach(option =>
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(option[propsValue].ToString(), option[propsLabel].ToString());
                                        addItem.Add(dictionary);
                                    });
                                    resData.Add(item.__vModel__, addItem);
                                }
                            }
                            else if (item.__config__.dataType.Equals("dictionary"))
                            {
                                var dictionaryDataList = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();

                                foreach (var it in dictionaryDataList)
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    if (propsValue.Equals("id")) dictionary.Add(it.Id, it.FullName);
                                    if (propsValue.Equals("enCode")) dictionary.Add(it.EnCode, it.FullName);
                                    addItem.Add(dictionary);
                                }

                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                resData.Add(item.__vModel__, popDataList);
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
                                var dictionaryDataList = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();
                                if (item.props.value.ToLower().Equals("encode"))
                                {
                                    foreach (var it in dictionaryDataList)
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.EnCode, it.FullName);
                                        addItem.Add(dictionary);
                                    }
                                }
                                else
                                {
                                    foreach (var it in dictionaryDataList)
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.Id, it.FullName);
                                        addItem.Add(dictionary);
                                    }
                                }

                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                resData.Add(item.__vModel__, popDataList);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.POPUPTABLESELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var popDataList = await _formDataParsing.GetDynamicList(item);
                            resData.Add(item.__vModel__, popDataList);
                        }
                    }
                    break;

                case JnpfKeyConst.USERSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
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
                                var userIdList = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                                    .WhereIF(item.ableIds.Any(), x => newAbleIds.Contains(x.UserId) || newAbleIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && userIdList.Contains(x.Id))
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
                                    (await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.RealName, x.Account }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--user", item.RealName + "/" + item.Account);
                                        addItem.Add(user);
                                    });
                                    var dataList = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null)
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
                                    (await _visualDevRepository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--role", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                    (await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--position", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                    (await _visualDevRepository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName, x.EnCode }).ToListAsync()).ForEach(item =>
                                    {
                                        Dictionary<string, string> user = new Dictionary<string, string>();
                                        user.Add(item.Id + "--group", item.FullName + "/" + item.EnCode);
                                        addItem.Add(user);
                                    });
                                }
                                else
                                {
                                    var dataList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
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
                                    var userIdList = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => newAbleIds.Contains(x.UserId) || newAbleIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                    var dataList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => userIdList.Contains(x.Id)).Select(x => new UserEntity() { Id = x.Id, Account = x.Account }).ToListAsync();
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
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).Select(x => new { x.Id, x.EnCode }).ToListAsync();
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
                                    var listQuery = new List<ISugarQueryable<OrganizeEntity>>();
                                    item.ableIds.ForEach(x => listQuery.Add(_visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.OrganizeIdTree.Contains(x.ToString()))));
                                    var dataList = await _visualDevRepository.AsSugarClient().UnionAll(listQuery).Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.EnCode }).ToListAsync();
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

                    break;
                case JnpfKeyConst.POSSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.selectType.Equals("all"))
                            {
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new PositionEntity() { Id = x.Id, EnCode = x.EnCode }).ToListAsync();
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
                                    var dataList = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && (newAbleIds.Contains(x.Id) || newAbleIds.Contains(x.OrganizeId)))
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
                                    else
                                    {
                                        resData.Add(item.__vModel__, addItem);
                                    }
                                }
                            }
                        }
                    }

                    break;

                case JnpfKeyConst.RELATIONFORM:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            //暂时只获取缓存数据，因为查询界面就会生成缓存数据
                            var redisName = CommonConst.VISUALDEV + _userManager.TenantId + "_" + item.__config__.jnpfKey + "_" + item.__config__.renderKey;

                            if (_cacheManager.Exists(redisName))
                            {
                                var relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                resData.Add(item.__vModel__, relationFormDataList);

                                item.options = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
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
        var errorKey = "errorsInfo";
        var resList = new List<Dictionary<string, object>>();
        foreach (var dataItems in dataList)
        {
            var newDataItems = dataItems.Copy();
            foreach (var item in dataItems)
            {
                var vModel = fieldsModelList.Find(x => x.__vModel__.Equals(item.Key));
                if (vModel == null) continue;
                var dicList = new List<Dictionary<string, string>>();
                if (cDataList.ContainsKey(vModel.__config__.jnpfKey)) dicList = cDataList[vModel.__config__.jnpfKey];
                if ((dicList == null || !dicList.Any()) && cDataList.ContainsKey(vModel.__vModel__)) dicList = cDataList[vModel.__vModel__];

                switch (vModel.__config__.jnpfKey)
                {
                    case JnpfKeyConst.DATE:
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                // 判断格式是否正确
                                var value = DateTime.ParseExact(item.Value.ToString().TrimEnd(), vModel.format, System.Globalization.CultureInfo.CurrentCulture);
                                if (vModel.__config__.startTimeRule)
                                {
                                    var minDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                                    switch (vModel.__config__.startTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.startTimeValue.IsNotEmptyOrNull())
                                                    minDate = vModel.__config__.startTimeValue.TimeStampToDateTime();
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.startRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.startRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.startRelationField] == null)
                                                    {
                                                        minDate = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        var data = dataItems[vModel.__config__.startRelationField].ToString();
                                                        minDate = data.TrimEnd().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minDate = minDate.AddYears(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minDate = minDate.AddMonths(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minDate = minDate.AddDays(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minDate = minDate.AddYears(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minDate = minDate.AddMonths(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minDate = minDate.AddDays(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (minDate > value && !minDate.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = vModel.__config__.label + ": 日期选择值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                if (vModel.__config__.endTimeRule)
                                {
                                    var maxDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                                    switch (vModel.__config__.endTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.endTimeValue.IsNotEmptyOrNull())
                                                    maxDate = vModel.__config__.endTimeValue.TimeStampToDateTime();
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.endRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.endRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.endRelationField] == null)
                                                    {
                                                        maxDate = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        var data = dataItems[vModel.__config__.endRelationField].ToString();
                                                        maxDate = data.TrimEnd().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxDate = maxDate.AddYears(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxDate = maxDate.AddMonths(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxDate = maxDate.AddDays(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxDate = maxDate.AddYears(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxDate = maxDate.AddMonths(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxDate = maxDate.AddDays(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (maxDate < value && !maxDate.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = vModel.__config__.label + ": 日期选择值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                newDataItems[item.Key] = value.ParseToUnixTime();
                            }
                        }
                        catch
                        {
                            var errorInfo = vModel.__config__.label + ": 值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }

                        break;
                    case JnpfKeyConst.TIME: // 时间选择
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                var value = DateTime.ParseExact(item.Value.ToString().TrimEnd(), vModel.format, System.Globalization.CultureInfo.CurrentCulture);
                                if (vModel.__config__.startTimeRule)
                                {
                                    var minTime = value;
                                    switch (vModel.__config__.startTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.startTimeValue.IsNotEmptyOrNull())
                                                    minTime = DateTime.Parse(vModel.__config__.startTimeValue);
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.startRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.startRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.startRelationField] == null)
                                                    {
                                                        minTime = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        minTime = dataItems[vModel.__config__.startRelationField].ToString().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minTime = minTime.AddHours(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minTime = minTime.AddMinutes(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minTime = minTime.AddSeconds(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minTime = minTime.AddHours(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minTime = minTime.AddMinutes(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minTime = minTime.AddSeconds(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (minTime > value && !minTime.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = vModel.__config__.label + ": 时间选择值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                if (vModel.__config__.endTimeRule)
                                {
                                    var maxTime = value;
                                    switch (vModel.__config__.endTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.endTimeValue.IsNotEmptyOrNull())
                                                    maxTime = DateTime.Parse(vModel.__config__.endTimeValue);
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.endRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.endRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.endRelationField] == null)
                                                    {
                                                        maxTime = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        maxTime = dataItems[vModel.__config__.endRelationField].ToString().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxTime = maxTime.AddHours(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxTime = maxTime.AddMinutes(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxTime = maxTime.AddSeconds(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxTime = maxTime.AddHours(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxTime = maxTime.AddMinutes(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxTime = maxTime.AddSeconds(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (maxTime < value && !maxTime.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = vModel.__config__.label + ": 时间选择值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            var errorInfo = vModel.__config__.label + ": 值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }

                        break;
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.ADDRESS:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(it =>
                                    {
                                        if (vModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || (it.Count(x => x == '/') == vModel.level))
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it)).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().FirstOrDefault();
                                                addList.Add(value.Key.Split(",").ToList());
                                            }
                                            else
                                            {
                                                var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                            }
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (vModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || (item.Value?.ToString().Count(x => x == '/') == vModel.level))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(item.Value?.ToString())).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().FirstOrDefault();
                                            newDataItems[item.Key] = value.Key.Split(",").ToList();
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    }
                                    else
                                    {
                                        var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.SWITCH:
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.RADIO:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple || vModel.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX))
                                {
                                    var addList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(it =>
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.USERSELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull() && (vModel.selectType.IsNullOrEmpty() || vModel.selectType.Equals("all") || vModel.selectType.Equals("custom")))
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(it =>
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it.Split("/").Last())).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it.Split("/").Last())).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString().Split("/").Last())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString().Split("/").Last())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                            else newDataItems[item.Key] = null;
                        }

                        break;
                    case JnpfKeyConst.USERSSELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull() && (vModel.selectType.IsNullOrEmpty() || vModel.selectType.Equals("all") || vModel.selectType.Equals("custom")))
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(it =>
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it.Split("/").Last())).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it.Split("/").Last())).FirstOrDefault().LastOrDefault();
                                                addList.Add(value.Key);
                                            }
                                            else
                                            {
                                                var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                            }
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        if (dicList.Where(x => x.ContainsValue(item.Value.ToString().Split("/").Last())).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString().Split("/").Last())).FirstOrDefault().LastOrDefault();
                                            newDataItems[item.Key] = value.Key;
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    }
                                }
                            }
                            else newDataItems[item.Key] = null;
                        }

                        break;
                    case JnpfKeyConst.TREESELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(it =>
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.CASCADER:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addsList = new List<object>();
                                    item.Value.ToString().Split(",").ToList().ForEach(its =>
                                    {
                                        var txtList = its.Split(vModel.separator).ToList();

                                        var add = new List<object>();
                                        txtList.ForEach(it =>
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it)).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                                add.Add(value.Key);
                                            }
                                            else
                                            {
                                                var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                            }
                                        });
                                        addsList.Add(add);
                                    });
                                    newDataItems[item.Key] = addsList;
                                }
                                else
                                {
                                    var txtList = item.Value.ToString().Split(vModel.separator).ToList();

                                    var addList = new List<object>();
                                    txtList.ForEach(it =>
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = vModel.__config__.label + ": 值无法匹配";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    });
                                    newDataItems[item.Key] = addList;
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.TABLE:
                        {
                            if (item.Value != null)
                            {
                                var valueList = item.Value.ToObject<List<Dictionary<string, object>>>();
                                var newValueList = new List<Dictionary<string, object>>();
                                valueList.ForEach(it =>
                                {
                                    var addValue = new Dictionary<string, object>();
                                    foreach (var value in it) addValue.Add(vModel.__vModel__ + "-" + value.Key, value.Value);
                                    newValueList.Add(addValue);
                                });

                                var res = await ImportDataAssemble(vModel.__config__.children, newValueList, cDataList);
                                if (res.Any(x => x.ContainsKey(errorKey)))
                                {
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + res.FirstOrDefault(x => x.ContainsKey(errorKey))[errorKey].ToString();
                                    else newDataItems.Add(errorKey, res.FirstOrDefault(x => x.ContainsKey(errorKey))[errorKey].ToString());
                                    res.Remove(res.FirstOrDefault(x => x.ContainsKey(errorKey)));
                                }

                                var result = new List<Dictionary<string, object>>();
                                res.ForEach(it =>
                                {
                                    var addValue = new Dictionary<string, object>();
                                    foreach (var value in it) addValue.Add(value.Key.Replace(vModel.__vModel__ + "-", string.Empty), value.Value);
                                    result.Add(addValue);
                                });
                                newDataItems[item.Key] = result;
                            }
                        }
                        break;
                    case JnpfKeyConst.RATE:
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            try
                            {
                                var value = double.Parse(item.Value.ToString());

                                if (value < 0) throw new Exception(string.Empty);

                                if (vModel.allowHalf)
                                {
                                    if (value % 0.5 != 0)
                                        throw new Exception(string.Empty);
                                }
                                else
                                {
                                    if (value % 1 != 0)
                                        throw new Exception(string.Empty);
                                }

                                if (vModel.count != null && vModel.count < value)
                                {
                                    var errorInfo = vModel.__config__.label + ": 评分超过设置的最大值";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                }
                            }
                            catch
                            {
                                var errorInfo = vModel.__config__.label + ": 值不正确";
                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                else newDataItems.Add(errorKey, errorInfo);
                            }
                        }
                        break;
                    case JnpfKeyConst.SLIDER:
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            try
                            {
                                var value = decimal.Parse(item.Value.ToString());
                                if (vModel.max != null)
                                {
                                    if (vModel.max < value)
                                    {
                                        var errorInfo = vModel.__config__.label + ": 滑块超过设置的最大值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                                if (vModel.min != null)
                                {
                                    if (vModel.min > value)
                                    {
                                        var errorInfo = vModel.__config__.label + ": 滑块超过设置的最小值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                            catch
                            {
                                var errorInfo = vModel.__config__.label + ": 值不正确";
                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                else newDataItems.Add(errorKey, errorInfo);
                            }
                        }
                        break;
                    case JnpfKeyConst.NUMINPUT:
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            try
                            {
                                var value = decimal.Parse(item.Value.ToString());
                                if (vModel.max != null)
                                {
                                    if (vModel.max < value)
                                    {
                                        var errorInfo = vModel.__config__.label + ": 数字输入超过设置的最大值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                                if (vModel.min != null)
                                {
                                    if (vModel.min > value)
                                    {
                                        var errorInfo = vModel.__config__.label + ": 数字输入超过设置的最小值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                            catch
                            {
                                var errorInfo = vModel.__config__.label + ": 值不正确";
                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                else newDataItems.Add(errorKey, errorInfo);
                            }
                        }
                        break;
                }
            }

            // 系统自动生成控件
            foreach (var item in dataItems)
            {
                if (newDataItems.ContainsKey(errorKey)) continue; // 如果存在错误信息 则 不生成
                var vModel = fieldsModelList.Find(x => x.__vModel__.Equals(item.Key));
                if (vModel == null) continue;

                switch (vModel.__config__.jnpfKey)
                {
                    case JnpfKeyConst.BILLRULE:
                        string billNumber = await _billRuleService.GetBillNumber(vModel.__config__.rule);
                        if (!"单据规则不存在".Equals(billNumber)) newDataItems[item.Key] = billNumber;
                        else newDataItems[item.Key] = string.Empty;

                        break;
                    case JnpfKeyConst.MODIFYUSER:
                        newDataItems[item.Key] = string.Empty;
                        break;
                    case JnpfKeyConst.CREATEUSER:
                        newDataItems[item.Key] = _userManager.UserId;
                        break;
                    case JnpfKeyConst.MODIFYTIME:
                        newDataItems[item.Key] = string.Empty;
                        break;
                    case JnpfKeyConst.CREATETIME:
                        newDataItems[item.Key] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                        break;
                    case JnpfKeyConst.CURRPOSITION:
                        string? pid = await _visualDevRepository.AsSugarClient().Queryable<UserEntity, PositionEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.PositionId))
                            .Where((a, b) => a.Id == _userManager.UserId && a.DeleteMark == null).Select((a, b) => a.PositionId).FirstAsync();
                        if (pid.IsNotEmptyOrNull()) newDataItems[item.Key] = pid;
                        else newDataItems[item.Key] = string.Empty;

                        break;
                    case JnpfKeyConst.CURRORGANIZE:
                        if (_userManager.User.OrganizeId != null) newDataItems[item.Key] = _userManager.User.OrganizeId;
                        else newDataItems[item.Key] = string.Empty;
                        break;
                }
            }

            if (fieldsModelList.Any(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique) && dataItems.ContainsKey("f_flow_id") && dataItems.ContainsKey("Update_MainTablePrimary_Id"))
            {
                var mainId = dataItems["Update_MainTablePrimary_Id"].ToString();
                var taskFlowStatus = await _visualDevRepository.AsSugarClient().Queryable<FlowTaskEntity>().Where(it => it.Id.Equals(mainId)).Select(it => it.Status).FirstAsync();
                if (taskFlowStatus.IsNotEmptyOrNull() && !taskFlowStatus.Equals(0))
                {
                    dataItems.Add(errorKey, "已发起流程，导入失败");
                    resList.Add(dataItems);
                    continue;
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
    /// 递归获取手动添加的省市区,名称处理成树形结构.
    /// </summary>
    /// <param name="addressEntityList"></param>
    private string GetAddressByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.FullName;
        }
        else
        {
            var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
            if (pItem != null) pEntity.QuickQuery = GetAddressByPList(addressEntityList, pItem) + "/" + pEntity.FullName;
            else pEntity.QuickQuery = pEntity.FullName;
            return pEntity.QuickQuery;
        }
    }

    /// <summary>
    /// 递归获取手动添加的省市区,Id处理成树形结构.
    /// </summary>
    /// <param name="addressEntityList"></param>
    private string GetAddressIdByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.Id;
        }
        else
        {
            var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
            if (pItem != null) pEntity.Id = GetAddressIdByPList(addressEntityList, pItem) + "," + pEntity.Id;
            else pEntity.Id = pEntity.Id;
            return pEntity.Id;
        }
    }

    /// <summary>
    /// 处理模板默认值.
    /// 用户选择 , 部门选择 , 岗位选择 , 角色选择 , 分组选择 ， 用户组件.
    /// </summary>
    /// <param name="config">模板.</param>
    /// <returns></returns>
    private VisualDevModelDataConfigOutput GetVisualDevModelDataConfig(VisualDevEntity config)
    {
        if (config.WebType.Equals(4)) return config.Adapt<VisualDevModelDataConfigOutput>();
        var tInfo = new TemplateParsingBase(config);
        if (tInfo.AllFieldsModel.Any(x => (x.__config__.defaultCurrent) && (x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.DEPSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.ROLESELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.GROUPSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSSELECT))))
        {
            var userId = _userManager.UserId;
            var depId = _visualDevRepository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
                .Where((a, b) => a.Id.Equals(_userManager.UserId) && b.Category.Equals("department")).Select((a, b) => a.OrganizeId).First();
            var posIds = _visualDevRepository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.ObjectId && b.ObjectType.Equals("Position")))
                .Where((a, b) => b.UserId.Equals(_userManager.UserId) && a.OrganizeId.Equals(_userManager.User.OrganizeId)).Select(a => a.Id).ToList();
            var roleIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals("Role")).Select(it => it.ObjectId).ToList();
            var groupIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals("Group")).Select(it => it.ObjectId).ToList();

            var allUserRelationList = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => new UserRelationEntity() { UserId = x.UserId, ObjectId = x.ObjectId }).ToList();

            var configData = config.FormData.ToObject<Dictionary<string, object>>();
            var columnList = configData["fields"].ToObject<List<Dictionary<string, object>>>();
            _runService.FieldBindDefaultValue(ref columnList, userId, depId, posIds, roleIds, groupIds, allUserRelationList);
            configData["fields"] = columnList;
            config.FormData = configData.ToJsonString();

            configData = config.ColumnData.ToObject<Dictionary<string, object>>();
            var searchList = configData["searchList"].ToObject<List<Dictionary<string, object>>>();
            columnList = configData["columnList"].ToObject<List<Dictionary<string, object>>>();
            _runService.FieldBindDefaultValue(ref searchList, userId, depId, posIds, roleIds, groupIds, allUserRelationList);
            _runService.FieldBindDefaultValue(ref columnList, userId, depId, posIds, roleIds, groupIds, allUserRelationList);
            configData["searchList"] = searchList;
            configData["columnList"] = columnList;
            config.ColumnData = configData.ToJsonString();

            configData = config.AppColumnData.ToObject<Dictionary<string, object>>();
            searchList = configData["searchList"].ToObject<List<Dictionary<string, object>>>();
            columnList = configData["columnList"].ToObject<List<Dictionary<string, object>>>();
            _runService.FieldBindDefaultValue(ref searchList, userId, depId, posIds, roleIds, groupIds, allUserRelationList);
            _runService.FieldBindDefaultValue(ref columnList, userId, depId, posIds, roleIds, groupIds, allUserRelationList);
            configData["searchList"] = searchList;
            configData["columnList"] = columnList;
            config.AppColumnData = configData.ToJsonString();
        }

        return config.Adapt<VisualDevModelDataConfigOutput>();
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
        List<OrganizeEntity>? allOrgList = _organizeService.GetOrgListTreeName();
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

    #endregion
}