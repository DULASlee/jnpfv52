using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Dto.FlowForm;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程设计.
/// </summary>
[ApiDescriptionSettings(Tag = "FlowForm", Name = "Form", Order = 301)]
[Route("api/flowForm/Form")]
public class FlowFormService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<FlowFormEntity> _repository;
    private readonly IRunService _runService;
    private readonly IVisualDevService _visualDevService;
    private readonly IUserManager _userManager;
    private readonly IFileManager _fileManager;
    private readonly IDataBaseManager _dataBaseManager;
    private readonly ITenant _db;

    public FlowFormService(
        ISqlSugarRepository<FlowFormEntity> repository,
        IRunService runService,
        IVisualDevService visualDevService,
        IUserManager userManager,
        IFileManager fileManager,
        IDataBaseManager dataBaseManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _runService = runService;
        _visualDevService = visualDevService;
        _userManager = userManager;
        _fileManager = fileManager;
        _dataBaseManager = dataBaseManager;
        _db = context.AsTenant();
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] FlowFormListInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && !(a.FormType == 2 && a.FlowType == 1))
            .WhereIF(input.isRelease.IsNotEmptyOrNull(), a => a.State == input.isRelease)
            .WhereIF(input.formType.IsNotEmptyOrNull(), a => a.FormType == input.formType)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select(a => new FlowFormListOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                formType = a.FormType,
                flowType = a.FlowType,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                isRelease = a.State
            }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowFormListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var entity = GetInfo(id);
        var res = entity.Adapt<FlowFormListOutput>();
        var a = entity.DraftJson?.ToObject<FlowFormEntity>();
        var draft = a.Adapt<FlowFormListOutput>();
        res.propertyJson = entity.PropertyJson;
        res.draftJson = draft != null && draft.propertyJson.IsNotEmptyOrNull() ? draft?.propertyJson : entity.PropertyJson;
        return res;
    }

    /// <summary>
    /// 列表(下拉).
    /// </summary>
    /// <param name="type">(预留字段).</param>
    /// <returns></returns>
    [HttpGet("Select")]
    public async Task<dynamic> Select([FromQuery] CommonInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowFormEntity>()
            .Where(a => a.DeleteMark == null && a.EnabledMark.Equals(1) && a.FlowType == input.flowType)
            .WhereIF(input.flowType == 1, a => a.FormType != 2)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select(a => new FlowFormListOutput
            {
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(it => it.EnCode.Equals(a.Category)).Select(it => it.FullName),
                id = a.Id,
                description = a.Description,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                formType = a.FormType,
                flowType = a.FlowType,
                sortCode = a.SortCode,
                isQuote = SqlFunc.IsNullOrEmpty(a.FlowId) ? 0 : 1
            }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowFormListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var info = GetInfo(id);
        info.DraftJson = info.ToJsonString();
        var jsonStr = info.ToJsonString();
        return await _fileManager.Export(jsonStr, info.FullName, ExportFileType.fff);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = GetInfo(id);
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        entity.EnabledMark = 0;
        entity.State = 0;
        //entity.TableJson = "[]";
        entity.FlowId = null;
        entity.DraftJson = entity.ToJsonString();
        entity.CreatorUserId = _userManager.UserId;
        entity.CreatorTime = DateTime.Now;
        entity.LastModifyUserId = null;
        entity.LastModifyTime = null;
        var result = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        _ = result ?? throw Oops.Oh(ErrorCode.WF0002);
    }

    /// <summary>
    /// 根据表单Id 获取流程信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("GetFormById/{id}")]
    public async Task<dynamic> GetFormById(string id)
    {
        var model = new { id = string.Empty, enCode = string.Empty, enabledMark = 0 };
        var form = GetInfo(id);
        if (form == null) throw Oops.Oh(ErrorCode.COM1019);

        if (form != null && form.FlowId.IsNotEmptyOrNull())
            model = _repository.AsSugarClient().Queryable<FlowTemplateEntity>().Where(x => x.Id.Equals(form.FlowId)).Select(x => new { id = x.Id, enCode = x.EnCode, enabledMark = (int)x.EnabledMark }).First();
        if (model == null || model.id.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1016);

        if (form.FlowType == 1 && form.FormType == 1 && model.enabledMark != 1)
        {
            // 代码生成的功能流程需要判断流程是否启用。
            throw Oops.Oh(ErrorCode.COM1017);
        }
        return model;
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
        var flowFormEntity = GetInfo(id);
        if (flowFormEntity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        if (await _repository.AsSugarClient().Queryable<FlowFormRelationEntity>().Where(x => x.FormId.Equals(id)).AnyAsync() || flowFormEntity.FlowId.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.COM1012);

        var isOk = await _repository.AsSugarClient().Updateable(flowFormEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        await _repository.AsSugarClient().Deleteable<FlowFormRelationEntity>().Where(x => x.FormId.Equals(id)).ExecuteCommandAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] FlowFormInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.FormType.Equals(input.formType) && x.FlowType.Equals(input.flowType) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);

        var flowFormEntity = input.Adapt<FlowFormEntity>();
        flowFormEntity.Id = SnowflakeIdHelper.NextId();
        flowFormEntity.EnabledMark = 0;
        flowFormEntity.PropertyJson = flowFormEntity.DraftJson;
        flowFormEntity.DraftJson = flowFormEntity.ToJsonString();
        if (input.formType.Equals(2) && input.draftJson.IsNotEmptyOrNull())
        {
            TemplateParsingBase? tInfo = new TemplateParsingBase(flowFormEntity.PropertyJson, flowFormEntity.TableJson); // 解析模板
            if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
            await VerifyPrimaryKeyPolicy(tInfo, flowFormEntity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持

        }

        var result = await _repository.AsSugarClient().Insertable(flowFormEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

        _ = result ?? throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("")]
    public async Task Update([FromBody] FlowFormInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != input.id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.FormType.Equals(input.formType) && x.FlowType.Equals(input.flowType) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if (await _repository.AsQueryable().AnyAsync(x => x.Id.Equals(input.id) && x.EnabledMark.Equals(1) && x.FormType == 2))
        {
            if (input.tableJson.IsNullOrWhiteSpace() || input.tableJson.Equals("[]"))
                throw Oops.Oh(ErrorCode.D1416); // 已发布的模板  表不能为空.
            input.enabledMark = 1;
        }

        var isOk = false;
        var fEntity = input.Adapt<FlowFormEntity>();
        if (fEntity != null)
        {
            if (input.formType.Equals(2) && input.draftJson.IsNotEmptyOrNull())
            {
                TemplateParsingBase? tInfo = new TemplateParsingBase(fEntity.DraftJson, fEntity.TableJson); // 解析模板
                if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
                await VerifyPrimaryKeyPolicy(tInfo, fEntity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持
            }

            // EnabledMark=0 未发布，EnabledMark=1 已发布
            if (fEntity.EnabledMark.Equals(0))
            {
                fEntity.PropertyJson = fEntity.DraftJson;
                fEntity.DraftJson = fEntity.ToJsonString();

                // 修改流程表单
                isOk = await _repository.AsSugarClient().Updateable(fEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            }
            else
            {
                var nEntity = _repository.AsSugarClient().Queryable<FlowFormEntity>().Where(x => x.Id.Equals(input.id)).First();

                if (nEntity.State.Equals(1))
                    nEntity.State = 2;

                fEntity.PropertyJson = fEntity.DraftJson;
                nEntity.DraftJson = fEntity.ToJsonString();
                nEntity.FullName = fEntity.FullName;
                nEntity.EnCode = fEntity.EnCode;
                nEntity.SortCode = fEntity.SortCode;
                nEntity.Description = fEntity.Description;
                nEntity.TableJson = fEntity.TableJson;
                nEntity.DbLinkId = fEntity.DbLinkId;
                nEntity.AppUrlAddress = fEntity.AppUrlAddress;
                nEntity.UrlAddress = fEntity.UrlAddress;
                nEntity.InterfaceUrl = fEntity.InterfaceUrl;

                // 修改流程表单
                isOk = await _repository.AsSugarClient().Updateable(nEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            }

        }

        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.fff.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        FlowFormEntity? model;
        try
        {
            model = josn.ToObject<FlowFormEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (model == null)
            throw Oops.Oh(ErrorCode.D3006);
        await ImportData(model, type);
    }

    /// <summary>
    /// 发布.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="isRelease"></param>
    /// <returns></returns>
    [HttpPost("Release/{id}")]
    public async Task Release(string id, [FromQuery] int isRelease)
    {
        var entity = GetInfo(id);

        // 0 回滚表单 , 1 发布表单.
        if (isRelease.Equals(0))
        {
            if (entity == null) throw Oops.Oh(ErrorCode.COM1005);
            var isOk = await _repository.AsSugarClient().Updateable<FlowFormEntity>().SetColumns(it => it.DraftJson == entity.ToJsonString()).SetColumns(it => it.State == 1).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
            if (!isOk) throw Oops.Oh(ErrorCode.COM1003);
        }
        else
        {
            if (entity == null || entity.PropertyJson.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1013);
            var newEntity = entity.DraftJson?.ToObject<FlowFormEntity>();
            if (newEntity == null)
            {
                newEntity = entity;
            }
            entity.TableJson = newEntity.TableJson;
            entity.PropertyJson = newEntity.PropertyJson;
            if (entity.FormType == 2)
            {
                // 无表转有表
                if (entity.TableJson.IsNullOrEmpty() || entity.TableJson == "[]")
                {
                    // 主表名称
                    var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
                    var mTableName = "wf_" + entity.EnCode + "_" + random;
                    if (mTableName.Length > 24) mTableName = "wf_" + SnowflakeIdHelper.NextId();
                    var devEntity = new VisualDev.Entitys.VisualDevEntity()
                    {
                        Id = entity.Id,
                        State = 1,
                        WebType = 3,
                        EnableFlow = 1,
                        FullName = entity.FullName,
                        EnCode = entity.EnCode,
                        FormData = entity.PropertyJson,
                        DbLinkId = entity.DbLinkId
                    };
                    var res = await _visualDevService.NoTblToTable(devEntity, mTableName);
                    if (res == null) throw Oops.Oh(ErrorCode.COM1000);
                    entity.TableJson = res.Tables;
                    entity.PropertyJson = res.FormData;
                    //await _repository.AsSugarClient().Insertable(res).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                }
                //else if (await _repository.AsSugarClient().Queryable<VisualDev.Entitys.VisualDevEntity>().Where(x => x.Id.Equals(id)).AnyAsync())
                //await _repository.AsSugarClient().Updateable<VisualDev.Entitys.VisualDevEntity>().SetColumns(it => it.IsRelease == 1).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();

            }

            var dbLink = await _runService.GetDbLink(newEntity.DbLinkId);
            var MainTable = newEntity.TableJson.ToList<TableModel>().Find(m => m.typeId.Equals("1")); // 主表
            if (MainTable != null)
            {
                if (!_dataBaseManager.IsAnyColumn(dbLink, MainTable?.table, "f_flow_task_id"))
                {
                    var pFieldList = new List<DbTableFieldModel>() { new DbTableFieldModel() { field = "f_flow_task_id", fieldName = "流程任务Id", dataType = "varchar", dataLength = "50", allowNull = 1 } };
                    _dataBaseManager.AddTableColumn(dbLink, MainTable?.table, pFieldList);
                }
                if (!_dataBaseManager.IsAnyColumn(dbLink, MainTable?.table, "f_flow_id"))
                {
                    var pFieldList = new List<DbTableFieldModel>() { new DbTableFieldModel() { field = "f_flow_id", fieldName = "流程引擎Id", dataType = "varchar", dataLength = "50", allowNull = 1 } };
                    _dataBaseManager.AddTableColumn(dbLink, MainTable?.table, pFieldList);
                }
            }

            entity.DraftJson = entity.ToJsonString();
            entity.EnabledMark = 1;
            entity.State = 1;
            var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(x => x.LastModify()).ExecuteCommandHasChangeAsync();

            if (!isOk) throw Oops.Oh(ErrorCode.COM1003);
        }
    }

    /// <summary>
    /// 回滚.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("Stop/{id}")]
    public async Task Stop(string id)
    {
        var entity = GetInfo(id);
    }
    #endregion

    #region PrivateMethod

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [NonAction]
    private FlowFormEntity GetInfo(string id)
    {
        return _repository.GetFirst(a => a.Id == id && a.DeleteMark == null);
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="entity">导入实例.</param>
    /// <param name="type"></param>
    /// <returns></returns>
    private async Task ImportData(FlowFormEntity entity, int type)
    {
        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(entity.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(entity.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(entity.FullName))) errorList.Add("名称");

        if (errorList.Any())
        {
            if (type.Equals(0))
            {
                var error = string.Join("、", errorList);
                errorMsgList.Add(string.Format("{0}重复", error));
            }
            else
            {
                var random = new Random().NextLetterAndNumberString(5);
                entity.Id = SnowflakeIdHelper.NextId();
                entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
                entity.EnCode += random;
                entity.FlowId = null;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        entity.State = 0;
        entity.EnabledMark = 0;
        entity.DbLinkId = "0";
        entity.Create();
        entity.CreatorUserId = _userManager.UserId;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(entity).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    /// <summary>
    /// 验证主键策略 数据库表是否支持.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="dbLinkId">数据库连接id.</param>
    private async Task VerifyPrimaryKeyPolicy(TemplateParsingBase tInfo, string dbLinkId)
    {
        if (tInfo.IsHasTable)
        {
            DbLinkEntity link = await _runService.GetDbLink(dbLinkId);
            tInfo.AllTable.ForEach(item =>
            {
                var tableList = _dataBaseManager.GetFieldList(link, item.table); // 获取主表所有列
                var mainPrimary = tableList.Find(t => t.primaryKey); // 主表主键
                if (mainPrimary == null) throw Oops.Oh(ErrorCode.D1409, "主键为空", item.table);

                if (tInfo.FormModel.primaryKeyPolicy.Equals(2) && !mainPrimary.identity)
                {
                    throw Oops.Oh(ErrorCode.D1409, "自增长ID,没有自增标识", item.table);
                }
                if (tInfo.FormModel.primaryKeyPolicy.Equals(1) && !(mainPrimary.dataType.ToLower().Equals("string") || mainPrimary.dataType.ToLower().Equals("varchar") || mainPrimary.dataType.ToLower().Equals("nvarchar")))
                    throw Oops.Oh(ErrorCode.D1409, "雪花ID", item.table);
            });
            _dataBaseManager.ChangeDataBase(_dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName));
        }
    }

    #endregion
}
