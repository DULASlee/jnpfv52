using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Dto.FlowTemplate;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Repository;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程设计.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowTemplate", Name = "FlowTemplate", Order = 301)]
[Route("api/workflow/Engine/[controller]")]
public class FlowTemplateService : IFlowTemplateService, IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<FlowTemplateEntity> _repository;
    private readonly IFlowTaskRepository _flowTaskRepository;
    private readonly IDictionaryDataService _dictionaryDataService;
    private readonly IUserManager _userManager;
    private readonly IFileManager _fileManager;
    private readonly ITenant _db;

    public FlowTemplateService(
        ISqlSugarRepository<FlowTemplateEntity> repository,
        IFlowTaskRepository flowTaskRepository,
        IDictionaryDataService dictionaryDataService,
        IUserManager userManager,
        IFileManager fileManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _flowTaskRepository = flowTaskRepository;
        _dictionaryDataService = dictionaryDataService;
        _userManager = userManager;
        _fileManager = fileManager;
        _db = context.AsTenant();
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] FlowTemplateListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowTemplateEntity, FlowVisibleEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.FlowId))
            .Where(a => a.DeleteMark == null)
            .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type == input.type.ParseToInt())
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark == input.enabledMark)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select(a => new FlowTemplateListOutput
            {
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.Id == a.Category).Select(d => d.FullName),
                id = a.Id,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                lastModifyUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                sortCode = a.SortCode,
                type = a.Type,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表(树形).
    /// </summary>
    /// <returns></returns>
    [HttpGet("ListAll")]
    public async Task<dynamic> GetListAll()
    {
        var list1 = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1)
            .Select(a => new FlowTemplateTreeOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                parentId = a.Category,
                category = a.Category,
                type = a.Type,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderBy(t => t.lastModifyTime, OrderByType.Desc).ToListAsync();
        if (list1.Any())
        {
            var dicDataInfo = await _dictionaryDataService.GetInfo(list1.FirstOrDefault().parentId);
            var dicDataList = await _dictionaryDataService.GetList(dicDataInfo.DictionaryTypeId);
            var list2 = new List<FlowTemplateTreeOutput>();
            foreach (var item in dicDataList)
            {
                list2.Add(new FlowTemplateTreeOutput()
                {
                    fullName = item.FullName,
                    parentId = "0",
                    id = item.Id,
                    num = list1.FindAll(x => x.category == item.EnCode).Count
                });
            }

            var output = list1.Union(list2).ToList().ToTree();
            return new { list = output };
        }
        else
        {
            return new { list = new List<FlowTemplateTreeOutput>() };
        }
    }

    /// <summary>
    /// 所属流程列表(树形).
    /// </summary>
    /// <returns></returns>
    [HttpGet("TreeList")]
    public async Task<dynamic> GetTreeList()
    {
        var flowTemplateList = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1)
            .Select(a => new FlowTemplateTreeOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                parentId = a.Category,
                category = a.Category,
                type = a.Type,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderBy(t => t.lastModifyTime, OrderByType.Desc).ToListAsync();
        List<FlowTemplateTreeOutput> output = new List<FlowTemplateTreeOutput>();
        if (flowTemplateList.Any())
        {
            var dicDataInfo = await _dictionaryDataService.GetInfo(flowTemplateList.FirstOrDefault().parentId);
            var dicDataList = await _dictionaryDataService.GetList(dicDataInfo.DictionaryTypeId);
            var dicList1 = new List<FlowTemplateTreeOutput>();
            var dicList2 = new List<FlowTemplateTreeOutput>();
            foreach (var item in dicDataList)
            {
                if (flowTemplateList.Any(x => x.type == 0 && x.category == item.Id))
                {
                    dicList1.Add(new FlowTemplateTreeOutput()
                    {
                        fullName = item.FullName,
                        parentId = "1",
                        id = item.Id,
                        disabled = true
                    });
                }
                if (flowTemplateList.Any(x => x.type == 1 && x.category == item.Id))
                {
                    dicList2.Add(new FlowTemplateTreeOutput()
                    {
                        fullName = item.FullName,
                        parentId = "2",
                        id = item.Id,
                        disabled = true
                    });
                }
            }
            var flowTemplateList1 = flowTemplateList.FindAll(x => x.type == 0);
            var flowTemplateList2 = flowTemplateList.FindAll(x => x.type == 1);
            if (flowTemplateList1.Any())
            {
                dicList1.Add(new FlowTemplateTreeOutput
                {
                    id = "1",
                    parentId = "0",
                    fullName = "发起流程",
                    disabled = true
                });
                var list1 = flowTemplateList1.Union(dicList1).ToList().ToTree();
                output = output.Union(list1).ToList();
            }
            if (flowTemplateList2.Any())
            {
                dicList2.Add(new FlowTemplateTreeOutput
                {
                    id = "2",
                    parentId = "0",
                    fullName = "功能流程",
                    disabled = true
                });
                var list2 = flowTemplateList2.Union(dicList2).ToList().ToTree();
                output = output.Union(list2).ToList();
            }
        }
        return new { list = output };
    }

    /// <summary>
    /// 列表(分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("PageListAll")]
    public async Task<dynamic> GetListPageAll([FromQuery] FlowTemplateListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.Type == 0 && a.EnabledMark == 1)
            .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select(a => new FlowTemplateListOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                type = a.Type,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 流程版本列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{id}/FlowJsonList")]
    public async Task<dynamic> GetFlowJsonList(string id, [FromQuery] FlowTemplateListQuery input)
    {
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNullOrEmpty())
        {
            var pageList = new SqlSugarPagedList<FlowTemplateJsonInfoOutput>()
            {
                list = new List<FlowTemplateJsonInfoOutput>(),
                pagination = new Pagination()
                {
                    CurrentPage = input.currentPage,
                    PageSize = input.pageSize,
                    Total = 0
                }
            };
            return PageResult<FlowTemplateJsonInfoOutput>.SqlSugarPageResult(pageList);
        }
        var whereLambda = LinqExpression.And<FlowTemplateJsonEntity>();
        whereLambda = whereLambda.And(x => x.DeleteMark == null && x.TemplateId == flowJosnEntity.TemplateId && x.GroupId == flowJosnEntity.GroupId);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.CreatorTime, input.startTime, input.endTime));
        }
        if (input.keyword.IsNotEmptyOrNull())
        {
            whereLambda = whereLambda.And(x => x.Version.Contains(input.keyword));
        }
        var list = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>()
            .Where(whereLambda)
            .Select((a) => new FlowTemplateJsonInfoOutput
            {
                id = a.Id,
                fullName = a.FullName,
                version = a.Version,
                enabledMark = a.EnabledMark,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                flowTemplateJson = a.FlowTemplateJson,
            }).MergeTable().OrderBy(a => a.enabledMark, OrderByType.Desc).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateJsonInfoOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        //var output = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<FlowTemplateInfoOutput>();
        //output.onlineDev = _repository.AsSugarClient().Queryable<VisualDevEntity>().Any(x => x.Id == id && x.DeleteMark == null);
        //var flowJsonList = await _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == id && x.EnabledMark == 1 && x.DeleteMark == null).Select(x => new { id = x.Id, flowId = x.Id, fullName = x.FullName, flowTemplateJson = x.FlowTemplateJson }).ToListAsync();
        //output.flowTemplateJson = flowJsonList.ToJsonString();
        //return output;
        var output = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<FlowTemplateInfoOutput>();
        var flowFormEntity = _repository.AsSugarClient().Queryable<FlowFormEntity>().First(x => x.FlowId == id && x.FlowType == 1 && x.FormType == 2 && x.DeleteMark == null);
        if (flowFormEntity.IsNotEmptyOrNull())
        {
            output.onlineDev = true;
            output.onlineFormId = flowFormEntity.Id;
        }
        var flowJsonList = new List<FlowJsonInfo>();
        var flowTemplateJsonEntityList = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == id && x.EnabledMark == 1 && x.DeleteMark == null).OrderBy(x => x.SortCode).ToList();
        foreach (var item in flowTemplateJsonEntityList)
        {
            var flowJosn = new FlowJsonInfo();
            flowJosn.id = item.Id;
            flowJosn.flowId = item.Id;
            flowJosn.fullName = item.FullName;
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == item.GroupId && x.DeleteMark == null).Select(x => x.Id).ToList();
            flowJosn.isDelete = await _flowTaskRepository.AnyFlowTask(x => x.DeleteMark == null && flowIds.Contains(x.FlowId));
            flowJosn.flowTemplateJson = item.FlowTemplateJson?.ToObjectOld<FlowTemplateJsonModel>();
            flowJsonList.Add(flowJosn);
        }
        output.flowTemplateJson = flowJsonList.ToJsonString();
        return output;
    }

    /// <summary>
    /// 列表(子流程选择流程).
    /// </summary>
    /// <param name="type">(预留字段).</param>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> ListSelect([FromQuery] int type)
    {
        var list1 = await GetOutList(0);
        if (type.IsEmpty())
            list1 = list1.FindAll(x => x.formType == type);
        var dicDataInfo = await _dictionaryDataService.GetInfo(list1.First().parentId);
        var dicDataList = (await _dictionaryDataService.GetList(dicDataInfo.DictionaryTypeId)).FindAll(x => x.EnabledMark == 1);
        var list2 = new List<FlowTemplateTreeOutput>();
        foreach (var item in dicDataList)
        {
            var index = list1.FindAll(x => x.category == item.EnCode).Count;
            if (index > 0)
            {
                list2.Add(new FlowTemplateTreeOutput()
                {
                    fullName = item.FullName,
                    parentId = "0",
                    id = item.Id,
                    num = index
                });
            }
        }

        var output = list1.Union(list2).ToList().ToTree();
        return new { list = output };
    }

    /// <summary>
    /// 列表(子流程选择流程).
    /// </summary>
    /// <param name="type">(预留字段).</param>
    /// <returns></returns>
    [HttpGet("PageChildListAll")]
    public async Task<dynamic> PageChildListAll([FromQuery] FlowTemplateListQuery input)
    {
        var list = _repository.AsSugarClient().Queryable<FlowTemplateEntity, FlowTemplateJsonEntity>((a, b) => new JoinQueryInfos(JoinType.Right, a.Id == b.TemplateId))
              .Where((a, b) => a.DeleteMark == null && a.EnabledMark == 1 && b.DeleteMark == null && b.EnabledMark == 1)
              .WhereIF(input.flowType.IsNotEmptyOrNull(), a => a.Type == input.flowType)
              .WhereIF(input.keyword.IsNotEmptyOrNull(), (a, b) => b.FullName.Contains(input.keyword))
              .Select((a, b) => new FlowTemplateJsonInfoOutput
              {
                  id = b.Id,
                  fullName = b.FullName,
                  creatorTime = a.CreatorTime,
                  lastModifyTime = a.LastModifyTime,
                  sortCode = a.SortCode,
                  flowName = a.FullName,
                  flowType = a.Type,
              }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
              .OrderBy(a => a.lastModifyTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateJsonInfoOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var importModel = new FlowTemplateImportOutput();
        importModel.flowTemplate = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        importModel.flowTemplateJson = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == id && x.EnabledMark == 1 && x.DeleteMark == null).ToList();
        var flowJsonIds = importModel.flowTemplateJson.Select(x => x.Id).ToList();
        importModel.visibleList = await _repository.AsSugarClient().Queryable<FlowVisibleEntity>().Where(x => flowJsonIds.Contains(x.FlowId)).ToListAsync();
        importModel.formRelationList = await _repository.AsSugarClient().Queryable<FlowFormRelationEntity>().Where(x => x.FlowId == id).ToListAsync();
        var jsonStr = importModel.ToJsonString();
        return await _fileManager.Export(jsonStr, importModel.flowTemplate.FullName, ExportFileType.ffe);
    }

    /// <summary>
    /// 表单主表属性.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}/FormInfo")]
    public async Task<dynamic> GetFormInfo(string id)
    {
        var flowJson = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == id && x.DeleteMark == null);
        if (flowJson.IsNullOrEmpty()) return null;
        var formId = flowJson.FlowTemplateJson.ToObject<FlowTemplateJsonModel>().properties.ToObject<StartProperties>().formId;
        var formEntity = _repository.AsSugarClient().Queryable<FlowFormEntity>().First(x => x.Id == formId && x.DeleteMark == null);
        return formEntity.Adapt<FlowFormModel>();
    }

    /// <summary>
    /// 流程协管列表.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}/assistList")]
    public async Task<dynamic> GetAssistList(string id)
    {
        var output = await _repository.AsSugarClient().Queryable<FlowVisibleEntity>().Where(a => a.FlowId == id && a.Type == 2).Select(a => a.OperatorId).ToListAsync();
        return new { list = output };
    }

    /// <summary>
    /// 委托流程列表(所有流程).
    /// </summary>
    /// <returns></returns>
    [HttpGet("getflowAll")]
    public async Task<dynamic> GetFlowAll([FromQuery] FlowTemplateListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
               .Where(a => a.DeleteMark == null && a.EnabledMark == 1)
               .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
               .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
               .Select(a => new FlowTemplateListOutput
               {
                   id = a.Id,
                   category = a.Category,
                   enCode = a.EnCode,
                   fullName = a.FullName,
                   sortCode = a.SortCode,
                   creatorTime = a.CreatorTime,
                   lastModifyTime = a.LastModifyTime,
                   type = a.Type,
               }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
               .OrderBy(a => a.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 委托流程列表(所有流程).
    /// </summary>
    /// <returns></returns>
    [HttpPost("getflowList")]
    public async Task<dynamic> GetflowList([FromBody] List<string> input)
    {
        return (await _repository.GetListAsync(x => input.Contains(x.Id))).Adapt<List<FlowTemplateListOutput>>();
    }

    /// <summary>
    /// 通过编码获取流程id.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getFlowIdByCode/{code}")]
    public async Task<dynamic> GetflowCode(string code)
    {
        var entity = await _repository.GetFirstAsync(x => x.EnCode == code && x.DeleteMark == null);
        if (entity == null) Oops.Oh(ErrorCode.COM1005);
        return entity.Id;
    }

    /// <summary>
    /// 发起流程列表(下拉).
    /// </summary>
    /// <returns></returns>
    [HttpGet("FlowJsonList/{teplateId}")]
    public async Task<dynamic> GetFlowJsonList(string teplateId, string type)
    {
        var flowJsonList = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == teplateId && x.EnabledMark == 1 && x.DeleteMark == null).OrderBy(x => x.SortCode).ToList().Adapt<List<FlowTemplateJsonInfoOutput>>();
        if (!flowJsonList.Any()) throw Oops.Oh(ErrorCode.COM1016);
        if (!_userManager.IsAdministrator && type == "1")
        {
            // 判断当前用户是否有发起权限
            foreach (var item in flowJsonList.FindAll(x => x.visibleType == 1))
            {
                // 指定发起人.
                var visibleUserList = await _repository.AsSugarClient().Queryable<FlowVisibleEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.OperatorId == b.ObjectId || a.OperatorId == b.UserId))
                    .Where((a, b) => a.FlowId == item.id && a.Type == 1).Select((a, b) => b.UserId).Distinct().ToListAsync();
                if (!visibleUserList.Contains(_userManager.UserId))
                {
                    flowJsonList.Remove(item);
                }
            }
        }
        if (!flowJsonList.Any()) throw Oops.Oh(ErrorCode.WF0049);
        return flowJsonList;
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var flowTemplateEntity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (flowTemplateEntity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var flowTemplateJsonIdList = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == id).Select(x => x.Id).ToList();
        if (await _flowTaskRepository.AnyFlowTask(x => x.DeleteMark == null && flowTemplateJsonIdList.Contains(x.FlowId)))
            throw Oops.Oh(ErrorCode.WF0037);
        if (_repository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.Id == id))
            throw Oops.Oh(ErrorCode.WF0052);
        _db.BeginTran();
        await _repository.AsSugarClient().Deleteable<FlowVisibleEntity>(a => flowTemplateJsonIdList.Contains(a.FlowId)).ExecuteCommandHasChangeAsync();
        _repository.AsSugarClient().Deleteable<FlowTemplateJsonEntity>(a => flowTemplateJsonIdList.Contains(a.Id)).ExecuteCommandHasChange();
        await _repository.AsSugarClient().Deleteable<FlowFormRelationEntity>(a => a.FlowId == id).ExecuteCommandHasChangeAsync();
        await _repository.AsSugarClient().Updateable<FlowFormEntity>().SetColumns(x => x.FlowId == null).Where(x => x.FlowId == id).ExecuteCommandAsync();
        var isOk = await _repository.AsUpdateable(flowTemplateEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        _db.CommitTran();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] FlowTemplateInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var flowTemplateEntity = input.Adapt<FlowTemplateEntity>();
        var result = await Create(flowTemplateEntity, input.flowTemplateJson);
        _ = result ?? throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<dynamic> Update(string id, [FromBody] FlowTemplateInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var flowTemplateEntity = input.Adapt<FlowTemplateEntity>();
        var result = await Update(flowTemplateEntity, input.flowTemplateJson, input.onlineFormId);
        if (result.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1001);
        return result;
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        entity.CreatorUserId = _userManager.UserId;
        var flowJsonList = new List<FlowJsonInfo>();
        var flowTemplateJsonEntityList = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == id && x.EnabledMark == 1 && x.DeleteMark == null).ToList();
        foreach (var item in flowTemplateJsonEntityList)
        {
            var flowJosn = new FlowJsonInfo();
            flowJosn.id = item.Id;
            flowJosn.flowId = item.Id;
            flowJosn.fullName = item.FullName;
            flowJosn.isDelete = await _flowTaskRepository.AnyFlowTask(x => x.DeleteMark == null && x.FlowId == item.Id);
            flowJosn.flowTemplateJson = item.FlowTemplateJson?.ToObject<FlowTemplateJsonModel>();
            flowJsonList.Add(flowJosn);
        }
        var result = await Create(entity, flowJsonList.ToJsonString());
        _ = result ?? throw Oops.Oh(ErrorCode.WF0002);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(string type, IFormFile file)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.ffe.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        FlowTemplateImportOutput model;
        try
        {
            model = josn.ToObject<FlowTemplateImportOutput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (model == null)
            throw Oops.Oh(ErrorCode.D3006);
        await ImportDataRepeat(model, type);
        await ImportData(model);
    }

    /// <summary>
    /// 发布.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("Release/{id}")]
    public async Task Release(string id)
    {
        var entity = await GetInfo(id);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var flowJson = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.TemplateId == id && x.EnabledMark == 1 && x.DeleteMark == null);
        if (flowJson.IsNotEmptyOrNull() && flowJson.FlowTemplateJson.IsNotEmptyOrNull())
        {
            var isOk = await _repository.AsSugarClient().Updateable<FlowTemplateEntity>().SetColumns(it => it.EnabledMark == 1).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1003);
        }
        else
        {
            throw Oops.Oh(ErrorCode.WF0038);
        }
    }

    /// <summary>
    /// 停止.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("Stop/{id}")]
    public async Task Stop(string id)
    {
        var entity = await GetInfo(id);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsSugarClient().Updateable<FlowTemplateEntity>().SetColumns(it => it.EnabledMark == 0).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 设置主版本.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/MainVersion")]
    public async Task MainVersion(string id)
    {
        foreach (var item in id.Split(","))
        {
            var jsonInfo = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == item && x.DeleteMark == null);
            _repository.AsSugarClient().Updateable<FlowTemplateJsonEntity>().SetColumns(it => new FlowTemplateJsonEntity()
            {
                EnabledMark = 0,
                LastModifyUserId = _userManager.UserId,
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.EnabledMark == 1 && it.TemplateId == jsonInfo.TemplateId && it.GroupId == jsonInfo.GroupId).ExecuteCommandHasChange();
            var isOk = _repository.AsSugarClient().Updateable<FlowTemplateJsonEntity>().SetColumns(it => new FlowTemplateJsonEntity()
            {
                EnabledMark = 1,
                LastModifyUserId = _userManager.UserId,
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.Id == item && it.TemplateId == jsonInfo.TemplateId && it.GroupId == jsonInfo.GroupId).ExecuteCommandHasChange();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1003);
        }
    }

    /// <summary>
    /// 删除版本.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}/FlowJson")]
    public async Task DeleteFlowJson(string id)
    {
        if (await _flowTaskRepository.AnyFlowTask(x => x.DeleteMark == null && x.FlowId == id))
            throw Oops.Oh(ErrorCode.WF0037);
        var jsonInfo = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == id && x.DeleteMark == null);
        var launchFormIdList = new List<string>();
        GetFormIdList(jsonInfo.FlowTemplateJson.ToObject<FlowTemplateJsonModel>(), launchFormIdList);
        await _repository.AsSugarClient().Deleteable<FlowFormRelationEntity>(a => a.FlowId == id && launchFormIdList.Contains(a.FormId)).ExecuteCommandHasChangeAsync();
        var isOk = _repository.AsSugarClient().Updateable<FlowTemplateJsonEntity>().SetColumns(it => new FlowTemplateJsonEntity() { DeleteMark = 1, DeleteTime = SqlFunc.GetDate(), DeleteUserId = _userManager.UserId }).Where(it => it.Id == id).ExecuteCommandHasChange();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 流程协管.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("assist")]
    public async Task SaveAssist([FromBody] FlowTemplateAssistQuery input)
    {
        var visibleList = new List<FlowVisibleEntity>();
        var flowTemplateEntity = await _repository.GetFirstAsync(x => x.Id == input.templateId && x.DeleteMark == null);
        foreach (var item in input.list)
        {
            var visibleEntity = new FlowVisibleEntity()
            {
                OperatorId = item,
                FlowId = input.templateId,
                Type = 2
            };
            visibleList.Add(visibleEntity);
        }
        await _repository.AsSugarClient().Deleteable<FlowVisibleEntity>(a => a.FlowId == input.templateId && a.Type == 2).ExecuteCommandHasChangeAsync();
        if (visibleList.Any())
        {
            await _repository.AsSugarClient().Insertable(visibleList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 发起列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<FlowTemplateTreeOutput>> GetFlowFormList(int flowType, string userId = null)
    {
        var list = await GetOutList(flowType);
        var IsAdministrator = false;
        if (userId.IsNullOrEmpty())
        {
            userId = _userManager.UserId;
            IsAdministrator = _userManager.User.IsAdministrator == 0;
        }
        else
        {
            IsAdministrator = _repository.AsSugarClient().Queryable<UserEntity>().Any(x => x.Id == userId && x.IsAdministrator == 0);
        }

        if (IsAdministrator)
        {
            var data = await GetVisibleFlowList(userId, flowType);
            data = data.Union(list.FindAll(x => x.visibleType == 0)).ToList();

            return data;
        }
        else
        {
            return list;
        }
    }
    #endregion

    #region PrivateMethod

    /// <summary>
    /// 新增流程.
    /// </summary>
    /// <param name="entity">流程实例.</param>
    /// <param name="visibleList">可见范围.</param>
    /// <returns></returns>
    [NonAction]
    private async Task<FlowTemplateEntity> Create(FlowTemplateEntity entity, string flowTemplateJson)
    {
        try
        {
            _db.BeginTran();
            var formIdList = new List<string>();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.EnabledMark = 0;
            if (flowTemplateJson.IsNotEmptyOrNull())
            {
                var index = 0;
                foreach (var item in flowTemplateJson.ToObjectOld<List<FlowJsonInfo>>())
                {
                    var sendConfigIdList = new List<string>();
                    var flowTemplateJsonEntity = new FlowTemplateJsonEntity();
                    flowTemplateJsonEntity.Id = SnowflakeIdHelper.NextId();
                    flowTemplateJsonEntity.TemplateId = entity.Id;
                    flowTemplateJsonEntity.Version = "1";
                    flowTemplateJsonEntity.FlowTemplateJson = item.flowTemplateJson.ToJsonString();
                    flowTemplateJsonEntity.FullName = item.fullName;
                    flowTemplateJsonEntity.SortCode = index++;
                    flowTemplateJsonEntity.GroupId = SnowflakeIdHelper.NextId();
                    GetSendConfigIdList(item.flowTemplateJson, sendConfigIdList);
                    flowTemplateJsonEntity.SendConfigIds = sendConfigIdList.ToJsonString();
                    var visibleList = GetFlowEngineVisibleList(item.flowTemplateJson.ToJsonString());
                    flowTemplateJsonEntity.VisibleType = visibleList.Count == 0 ? 0 : 1;
                    foreach (var visible in visibleList)
                    {
                        visible.FlowId = flowTemplateJsonEntity.Id;
                        visible.SortCode = visibleList.IndexOf(visible);
                        visible.Type = 1;
                    }
                    if (visibleList.Count > 0)
                        await _repository.AsSugarClient().Insertable(visibleList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    await _repository.AsSugarClient().Insertable(flowTemplateJsonEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                    #region 功能流程则要回写到对应的表单表中
                    if (entity.Type == 1)
                    {
                        var formId = item.flowTemplateJson.properties.ToJsonString().ToObjectOld<StartProperties>()?.formId;
                        if (formIdList.Any() && !formIdList.Contains(formId))
                        {
                            throw Oops.Oh(ErrorCode.WF0043);
                        }
                        else
                        {
                            formIdList.Add(formId);
                        }
                        if (_repository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.Id == formId && x.FlowId != entity.Id && !SqlFunc.IsNullOrEmpty(x.FlowId)))
                            throw Oops.Oh(ErrorCode.WF0043);
                        await _repository.AsSugarClient().Updateable<FlowFormEntity>().SetColumns(x => x.FlowId == entity.Id).Where(x => x.Id == formId).ExecuteCommandAsync();
                    }
                    else
                    {
                        var launchFormIdList = new List<string>();
                        GetFormIdList(item.flowTemplateJson, launchFormIdList);
                        foreach (var launchFormId in launchFormIdList)
                        {
                            var formRelationEntity = new FlowFormRelationEntity
                            {
                                Id = SnowflakeIdHelper.NextId(),
                                FlowId = entity.Id,
                                FormId = launchFormId
                            };
                            await _repository.AsSugarClient().Insertable(formRelationEntity).ExecuteReturnEntityAsync();
                        }
                    }
                    #endregion
                }
            }
            var result = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
            if (result == null)
                throw Oops.Oh(ErrorCode.COM1005);
            _db.CommitTran();
            return result;
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 修改流程.
    /// </summary>
    /// <param name="entity">流程实体.</param>
    /// <param name="visibleList">可见范围.</param>
    /// <returns></returns>
    [NonAction]
    private async Task<object> Update(FlowTemplateEntity entity, string flowTemplateJson, string onlineFormId)
    {
        try
        {
            _db.BeginTran();
            var flag = false;
            var isMainVersionId = new List<string>();
            var formIdList = new List<string>();
            var groupIdList = new List<string>(); // 未删除的分组id.
            if (flowTemplateJson.IsNotEmptyOrNull())
            {
                var index = 0;
                foreach (var item in flowTemplateJson.ToObjectOld<List<FlowJsonInfo>>())
                {
                    var flowTemplateJsonEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == item.id && x.DeleteMark == null);
                    var visibleList = GetFlowEngineVisibleList(item.flowTemplateJson.ToJsonString());
                    if (flowTemplateJsonEntity.IsNullOrEmpty())
                    {
                        var sendConfigIdList = new List<string>();
                        flowTemplateJsonEntity = new FlowTemplateJsonEntity();
                        flowTemplateJsonEntity.Id = SnowflakeIdHelper.NextId();
                        flowTemplateJsonEntity.TemplateId = entity.Id;
                        flowTemplateJsonEntity.Version = "1";
                        flowTemplateJsonEntity.FlowTemplateJson = item.flowTemplateJson.ToJsonString();
                        flowTemplateJsonEntity.FullName = item.fullName;
                        flowTemplateJsonEntity.SortCode = index++;
                        flowTemplateJsonEntity.VisibleType = visibleList.Count == 0 ? 0 : 1;
                        flowTemplateJsonEntity.GroupId = SnowflakeIdHelper.NextId();
                        GetSendConfigIdList(item.flowTemplateJson, sendConfigIdList);
                        flowTemplateJsonEntity.SendConfigIds = sendConfigIdList.ToJsonString();
                        foreach (var visible in visibleList)
                        {
                            visible.FlowId = flowTemplateJsonEntity.Id;
                            visible.SortCode = visibleList.IndexOf(visible);
                            visible.Type = 1;
                        }
                        if (visibleList.Count > 0)
                            await _repository.AsSugarClient().Insertable(visibleList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        await _repository.AsSugarClient().Insertable(flowTemplateJsonEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                        groupIdList.Add(flowTemplateJsonEntity.GroupId);
                    }
                    else
                    {
                        // 清空就模板关联表单数据
                        if (entity.Type == 1)
                        {
                            string oldFormId = flowTemplateJsonEntity.FlowTemplateJson.ToObjectOld<FlowTemplateJsonModel>().properties.ToJsonString().ToObjectOld<StartProperties>().formId;
                            await _repository.AsSugarClient().Updateable<FlowFormEntity>().SetColumns(x => x.FlowId == null).Where(x => x.Id == oldFormId).ExecuteCommandAsync();
                        }
                        else
                        {
                            await _repository.AsSugarClient().Deleteable<FlowFormRelationEntity>().Where(x => x.FlowId == entity.Id).ExecuteCommandAsync();
                        }
                        if (!groupIdList.Contains(flowTemplateJsonEntity.GroupId))
                        {
                            groupIdList.Add(flowTemplateJsonEntity.GroupId);
                        }
                        var isCreate = flowTemplateJsonEntity.FlowTemplateJson.Equals(item.flowTemplateJson.ToJsonString());//流程json是否变更
                        if (!isCreate)
                        {
                            var sendConfigIdList = new List<string>();
                            GetSendConfigIdList(item.flowTemplateJson, sendConfigIdList);
                            flowTemplateJsonEntity.SendConfigIds = sendConfigIdList.ToJsonString();
                        }
                        flowTemplateJsonEntity.FlowTemplateJson = item.flowTemplateJson.ToJsonString();
                        flowTemplateJsonEntity.SortCode = index++;
                        flowTemplateJsonEntity.FullName = item.fullName;
                        flowTemplateJsonEntity.VisibleType = visibleList.Count == 0 ? 0 : 1;
                        if ((await _flowTaskRepository.AnyFlowTask(x => x.DeleteMark == null && x.FlowId == flowTemplateJsonEntity.Id)) && !isCreate)
                        {
                            flag = true;
                            await _repository.AsSugarClient().Updateable(flowTemplateJsonEntity).UpdateColumns(it => new { it.FullName, it.SortCode }).ExecuteCommandHasChangeAsync();
                            flowTemplateJsonEntity.Version = (_repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == entity.Id && x.FullName == item.fullName).Select(x => SqlFunc.AggregateMax(SqlFunc.ToInt64(x.Version))).First().ParseToInt() + 1).ToString();
                            flowTemplateJsonEntity.Id = SnowflakeIdHelper.NextId();
                            flowTemplateJsonEntity.EnabledMark = 0;
                            flowTemplateJsonEntity.LastModifyTime = null;
                            flowTemplateJsonEntity.LastModifyUserId = null;
                            await _repository.AsSugarClient().Insertable(flowTemplateJsonEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                            isMainVersionId.Add(flowTemplateJsonEntity.Id);
                        }
                        else
                        {
                            await _repository.AsSugarClient().Updateable(flowTemplateJsonEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
                            await _repository.AsSugarClient().Deleteable<FlowVisibleEntity>(a => a.FlowId == flowTemplateJsonEntity.Id).ExecuteCommandHasChangeAsync();
                        }
                    }
                    foreach (var visible in visibleList)
                    {
                        visible.FlowId = flowTemplateJsonEntity.Id;
                        visible.SortCode = visibleList.IndexOf(visible);
                        visible.Type = 1;
                    }
                    if (visibleList.Count > 0)
                        await _repository.AsSugarClient().Insertable(visibleList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    if (entity.Type == 1)
                    {
                        var formId = item.flowTemplateJson.properties.ToJsonString().ToObjectOld<StartProperties>()?.formId;
                        if (formIdList.Any() && !formIdList.Contains(formId))
                        {
                            throw Oops.Oh(ErrorCode.WF0017);
                        }
                        else
                        {
                            formIdList.Add(formId);
                        }
                        if ((_repository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.Id == formId && x.FlowId != entity.Id && !SqlFunc.IsNullOrEmpty(x.FlowId))) && formId != onlineFormId)
                            throw Oops.Oh(ErrorCode.WF0043);
                        await _repository.AsSugarClient().Updateable<FlowFormEntity>().SetColumns(x => x.FlowId == entity.Id).Where(x => x.Id == formId).ExecuteCommandAsync();
                    }
                    else
                    {
                        // 发起流程表单id
                        var launchFormIdList = new List<string>();
                        GetFormIdList(item.flowTemplateJson, launchFormIdList);
                        foreach (var launchFormId in launchFormIdList)
                        {
                            var formRelationEntity = new FlowFormRelationEntity
                            {
                                Id = SnowflakeIdHelper.NextId(),
                                FlowId = entity.Id,
                                FormId = launchFormId
                            };
                            await _repository.AsSugarClient().Insertable(formRelationEntity).ExecuteReturnEntityAsync();
                        }
                    }
                }
                // 删除未使用的流程
                _repository.AsSugarClient().Deleteable<FlowTemplateJsonEntity>().Where(x => x.TemplateId == entity.Id && !groupIdList.Contains(x.GroupId)).ExecuteCommand();
            }
            var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            _db.CommitTran();
            return new { isMainVersion = flag, id = string.Join(",", isMainVersionId) };
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 解析流程可见参数.
    /// </summary>
    /// <param name="josnStr"></param>
    /// <returns></returns>
    private List<FlowVisibleEntity> GetFlowEngineVisibleList(string josnStr)
    {
        var output = new List<FlowVisibleEntity>();
        if (josnStr.IsNotEmptyOrNull())
        {
            // 发起节点属性.
            var pro = josnStr.ToObject<FlowTemplateJsonModel>().properties.ToJsonString().ToObjectOld<StartProperties>();
            if (pro.initiator.Any())
            {
                foreach (var item in pro.initiator)
                {
                    var strList = item.Split("--").ToList();
                    output.Add(new FlowVisibleEntity() { OperatorId = strList[0], OperatorType = strList[1] });
                }
            }
        }
        return output;
    }

    /// <summary>
    /// 获取当前用户可见流程.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    private async Task<List<FlowTemplateTreeOutput>> GetVisibleFlowList(string userId, int flowType)
    {
        return _repository.AsSugarClient().Queryable<FlowVisibleEntity, UserRelationEntity, FlowTemplateJsonEntity, FlowTemplateEntity>((a, b, c, d) => new JoinQueryInfos(JoinType.Left, a.OperatorId == b.ObjectId, JoinType.Left, a.FlowId == c.Id, JoinType.Left, c.TemplateId == d.Id))
            .Where((a, b, c, d) => (a.OperatorId == userId || b.UserId == userId) && c.DeleteMark == null && c.EnabledMark == 1 && d.Type == flowType)
            .Select((a, b, c, d) => new FlowTemplateTreeOutput
            {
                category = d.Category,
                id = c.Id,
                description = d.Description,
                creatorTime = d.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == d.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                enCode = d.EnCode,
                enabledMark = d.EnabledMark,
                fullName = d.FullName,
                icon = d.Icon,
                iconBackground = d.IconBackground,
                lastModifyTime = d.LastModifyTime,
                lastModifyUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == c.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                sortCode = a.SortCode,
                type = d.Type,
                visibleType = c.VisibleType,
                parentId = d.Category,
                templateId = a.Id
            }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
             .OrderBy(a => a.lastModifyTime, OrderByType.Desc).ToList();
    }

    /// <summary>
    /// 流程列表.
    /// </summary>
    /// <returns></returns>
    private async Task<List<FlowTemplateTreeOutput>> GetOutList(int flowType)
    {
        return _repository.AsSugarClient().Queryable<FlowTemplateEntity, FlowTemplateJsonEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.TemplateId))
             .Where((a, b) => a.DeleteMark == null && a.EnabledMark == 1 && a.Type == flowType && b.DeleteMark == null && b.EnabledMark == 1)
             .Select((a, b) => new FlowTemplateTreeOutput
             {
                 category = a.Category,
                 id = a.Id,
                 description = a.Description,
                 creatorTime = a.CreatorTime,
                 creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                 enCode = a.EnCode,
                 enabledMark = a.EnabledMark,
                 fullName = a.FullName,
                 icon = a.Icon,
                 iconBackground = a.IconBackground,
                 lastModifyTime = a.LastModifyTime,
                 lastModifyUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                 sortCode = a.SortCode,
                 type = a.Type,
                 visibleType = b.VisibleType,
                 parentId = a.Category,
                 templateId = a.Id
             }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
             .OrderBy(a => a.lastModifyTime, OrderByType.Desc).ToList();
    }

    /// <summary>
    /// 导入验证.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private async Task ImportDataRepeat(FlowTemplateImportOutput model, string type)
    {
        var errorMainList = new List<string>();
        var errorChild1List = new List<string>();
        var errorChild2List = new List<string>();
        var errorChild3List = new List<string>();
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        if (await _repository.IsAnyAsync(x => x.Id == model.flowTemplate.Id && x.DeleteMark == null))
        {
            errorMainList.Add("ID");
        }
        if (await _repository.IsAnyAsync(x => x.EnCode == model.flowTemplate.EnCode && x.DeleteMark == null))
        {
            errorMainList.Add("编码");
        }
        if (await _repository.IsAnyAsync(x => x.FullName == model.flowTemplate.FullName && x.DeleteMark == null))
        {
            errorMainList.Add("名称");
        }

        var jsonList = await _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Select(x => new FlowTemplateJsonEntity { Id = x.Id, FullName = x.FullName, DeleteMark = x.DeleteMark }).ToListAsync();
        var jsonIds = jsonList.Select(x => x.Id).ToList().Intersect(model.flowTemplateJson.Select(x => x.Id)).ToList();
        if (jsonIds.Any())
        {
            var str = string.Format("ID({0})", string.Join("、", jsonIds));
            errorChild1List.Add(str);
        }

        var jsonNames = model.flowTemplateJson.GroupBy(x => x.FullName).Where(x => x.Count() > 1).Select(g => g.Key).ToList();
        if (jsonNames.Any())
        {
            var str = string.Format("名称({0})", string.Join("、", jsonNames));
            errorChild1List.Add(str);
        }
        var visibleList = await _repository.AsSugarClient().Queryable<FlowVisibleEntity>().Select(x => x.Id).ToListAsync();
        var visibleIds = visibleList.Intersect(model.visibleList.Select(x => x.Id)).ToList();
        if (visibleIds.Any())
        {
            var str = string.Format("ID({0})", string.Join("、", visibleIds));
            errorChild2List.Add(str);
        }

        var fromRelationList = await _repository.AsSugarClient().Queryable<FlowFormRelationEntity>().Select(x => x.Id).ToListAsync();
        var fromRelationIds = visibleList.Intersect(model.formRelationList.Select(x => x.Id)).ToList();
        if (fromRelationIds.Any())
        {
            var str = string.Format("ID({0})", string.Join("、", fromRelationIds));
            errorChild3List.Add(str);
        }

        if (type == "0")
        {
            var error = string.Empty;
            if (errorMainList.Any())
            {
                error += string.Format("{0}重复；", string.Join("、", errorMainList));
            }
            if (errorChild1List.Any())
            {
                error += string.Format("flowTemplateJson：{0}重复；", string.Join("、", errorChild1List));
            }
            if (errorChild2List.Any())
            {
                error += string.Format("visibleList：{0}重复；", string.Join("、", errorChild2List));
            }
            if (errorChild3List.Any())
            {
                error += string.Format("formRelationList：{0}重复；", string.Join("、", errorChild3List));
            }
            if (error.IsNotEmptyOrNull())
            {
                throw Oops.Oh(ErrorCode.COM1018, error);
            }
        }
        else
        {
            if (errorMainList.Any())
            {
                model.flowTemplate.Id = SnowflakeIdHelper.NextId();
                model.flowTemplate.EnCode = string.Format("{0}{1}", model.flowTemplate.EnCode, random);
                model.flowTemplate.FullName = string.Format("{0}.副本{1}", model.flowTemplate.FullName, random);
            }
            jsonNames.Clear();
            foreach (var item in model.flowTemplateJson)
            {
                var snowId = SnowflakeIdHelper.NextId();
                foreach (var visible in model.visibleList)
                {
                    if (visible.FlowId == item.Id)
                    {
                        visible.FlowId = snowId;
                    }
                }
                if (jsonNames.Contains(item.FullName))
                {
                    item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                }
                jsonNames.Add(item.FullName);
                item.Id = snowId;
            }
            foreach (var item in model.visibleList)
            {
                item.Id = SnowflakeIdHelper.NextId();
            }
            foreach (var item in model.formRelationList)
            {
                item.Id = SnowflakeIdHelper.NextId();
            }
        }
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="model">导入实例.</param>
    /// <param name="type">识别重复(0-跳过 1-追加).</param>
    /// <returns></returns>
    private async Task ImportData(FlowTemplateImportOutput model)
    {
        try
        {
            _db.BeginTran();

            model.flowTemplate.CreatorTime = DateTime.Now;
            model.flowTemplate.CreatorUserId = _userManager.UserId;
            model.flowTemplate.LastModifyTime = null;
            model.flowTemplate.LastModifyUserId = null;
            model.flowTemplate.EnabledMark = 0;
            await _repository.AsSugarClient().Storageable(model.flowTemplate).ExecuteCommandAsync();

            model.flowTemplateJson.ForEach(x =>
            {
                x.TemplateId = model.flowTemplate.Id;
                x.CreatorTime = DateTime.Now;
                x.CreatorUserId = _userManager.UserId;
                x.LastModifyTime = null;
                x.LastModifyUserId = null;
            });

            model.visibleList.ForEach(x =>
            {
                x.CreatorTime = DateTime.Now;
                x.CreatorUserId = _userManager.UserId;
                x.LastModifyTime = null;
                x.LastModifyUserId = null;
            });

            model.formRelationList.ForEach(x =>
            {
                x.FlowId = model.flowTemplate.Id;
                x.CreatorTime = DateTime.Now;
                x.CreatorUserId = _userManager.UserId;
                x.LastModifyTime = null;
                x.LastModifyUserId = null;
            });
            await _repository.AsSugarClient().Storageable(model.flowTemplateJson).ExecuteCommandAsync();

            await _repository.AsSugarClient().Storageable(model.visibleList).ExecuteCommandAsync();

            await _repository.AsSugarClient().Storageable(model.formRelationList).ExecuteCommandAsync();

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D3006);
        }
    }

    /// <summary>
    /// 递归模板获取formid.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="formIdList"></param>
    private void GetFormIdList(FlowTemplateJsonModel template, List<string> formIdList)
    {
        if (template.IsNotEmptyOrNull() && !template.type.Equals("subFlow") && !template.type.Equals("timer"))
        {
            if (template.properties["formId"].ToString().IsNotEmptyOrNull())
            {
                formIdList.Add(template.properties["formId"].ToString());
            }
            if (template.childNode.IsNotEmptyOrNull())
            {
                GetFormIdList(template.childNode, formIdList);
            }
        }
    }

    /// <summary>
    /// 递归模板获取消息配置Id.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="sendConfigIdList"></param>
    private void GetSendConfigIdList(FlowTemplateJsonModel template, List<string> sendConfigIdList)
    {
        if (template.IsNotEmptyOrNull() && !template.type.Equals("timer"))
        {
            var msgProperties = template.properties.ToJsonString().ToObject<MsgProperties>();
            sendConfigIdList.Add(msgProperties.approveMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.rejectMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.copyMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.overTimeMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.noticeMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.launchMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.waitMsgConfig.msgId);
            sendConfigIdList.Add(msgProperties.waitMsgConfig.msgId);
            sendConfigIdList.Remove("");
            sendConfigIdList = sendConfigIdList.Distinct().ToList();
            if (template.childNode.IsNotEmptyOrNull())
            {
                GetFormIdList(template.childNode, sendConfigIdList);
            }
        }
    }
    #endregion
}