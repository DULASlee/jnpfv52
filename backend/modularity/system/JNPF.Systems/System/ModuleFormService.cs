using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.ModuleForm;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 功能表单.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "ModuleForm", Order = 212)]
[Route("api/system/[controller]")]
public class ModuleFormService : IModuleFormService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表单表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ModuleFormEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleFormService"/>类型的新实例.
    /// </summary>
    public ModuleFormService(
        ISqlSugarRepository<ModuleFormEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能主键 .</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{moduleId}/Fields")]
    public async Task<dynamic> GetList(string moduleId, [FromQuery] KeywordInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<ModuleFormEntity, ModuleEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.ModuleId == b.Id))
                .Where((a, b) => a.ModuleId == moduleId && a.DeleteMark == null && b.DeleteMark == null)
                .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.EnCode.Contains(input.keyword) || a.FullName.Contains(input.keyword))
                .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
                .Select((a, b) => new ModuleFormListOutput()
                {
                    id = a.Id,
                    fullName = a.FullName,
                    enabledMark = a.EnabledMark,
                    description = a.Description,
                    moduleId = a.ModuleId,
                    fieldRule = a.FieldRule,
                    bindTable = a.BindTable,
                    sortCode = a.SortCode,
                    enCode = SqlFunc.IF(a.FieldRule == 1 && !SqlFunc.IsNullOrEmpty(a.BindTable)).Return(a.EnCode.Replace("jnpf_" + a.BindTable + "_jnpf_", ""))
                    .ElseIF(b.Type == 3 && a.FieldRule == 1).Return(a.EnCode.Replace(a.BindTable + ".", ""))
                     .ElseIF(a.FieldRule == 2).Return(a.EnCode.Replace(a.ChildTableKey + "-", "")).End(a.EnCode),
                }).ToListAsync();
        return new { list = list };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (data.FieldRule == 2 && data.ChildTableKey.IsNotEmptyOrNull())
            data.EnCode = data.EnCode.Replace(data.ChildTableKey + "-", string.Empty);
        var menu = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id == data.ModuleId && x.DeleteMark == null);
        if (menu.IsNotEmptyOrNull() && data.BindTable.IsNotEmptyOrNull() && data.FieldRule == 1)
        {
            // 代码生成
            if (menu.Type == 2)
            {
                data.EnCode = data.EnCode.Replace("jnpf_" + data.BindTable + "_jnpf_", string.Empty);
            }
            // 在线开发
            if (menu.Type == 3)
            {
                data.EnCode = data.EnCode.Replace(data.BindTable + ".", string.Empty);
            }
        }
        return data.Adapt<ModuleFormListOutput>();
    }

    /// <summary>
    /// 字段列表.
    /// </summary>
    /// <param name="moduleId">菜单id.</param>
    /// <returns></returns>
    [HttpGet("{moduleId}/FieldList")]
    public async Task<dynamic> GetFieldList(string moduleId)
    {
        var moduleEntity = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id == moduleId && x.DeleteMark == null);
        var visualDevId = moduleEntity.PropertyJson.ToObject<JObject>()["moduleId"].ToString();
        var visualDevEntity = await _repository.AsSugarClient().Queryable<VisualDevEntity>().FirstAsync(x => x.Id == visualDevId && x.DeleteMark == null);
        var columnList = visualDevEntity.ColumnData.ToObject<ColumnDesignModel>().columnList;
        var formDataModel = visualDevEntity.FormData.ToObject<FormDataModel>();
        var fields = formDataModel.fields.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList();
        var childFieldList = fields.Select(x => new { field = x.__vModel__, fieldName = x.__config__.label }).ToList();
        return columnList.Select(x => new { field = x.prop, fieldName = x.label }).Union(childFieldList).ToList();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ModuleFormCrInput input)
    {
        var entity = input.Adapt<ModuleFormEntity>();
        if (entity.FieldRule == 2 && input.childTableKey.IsNotEmptyOrNull())
            entity.EnCode = input.childTableKey + "-" + entity.EnCode;
        var menu = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id == input.moduleId && x.DeleteMark == null);
        if (menu.IsNotEmptyOrNull() && entity.BindTable.IsNotEmptyOrNull() && entity.FieldRule == 1)
        {
            // 代码生成
            if (menu.Type == 2)
            {
                entity.EnCode = "jnpf_" + input.bindTable + "_jnpf_" + entity.EnCode;
            }
            // 在线开发
            if (menu.Type == 3)
            {
                entity.EnCode = "jnpf_" + input.bindTable + "_jnpf_" + entity.EnCode;
            }
        }
        if (await _repository.IsAnyAsync(x => x.EnCode.Equals(entity.EnCode) && x.DeleteMark == null && x.ModuleId == input.moduleId))
            throw Oops.Oh(ErrorCode.COM1004);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ModuleFormUpInput input)
    {
        var entity = input.Adapt<ModuleFormEntity>();
        if (entity.FieldRule == 2 && input.childTableKey.IsNotEmptyOrNull())
            entity.EnCode = input.childTableKey + "-" + entity.EnCode;
        var menu = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id == input.moduleId && x.DeleteMark == null);
        if (menu.IsNotEmptyOrNull() && entity.BindTable.IsNotEmptyOrNull() && entity.FieldRule == 1)
        {
            // 代码生成
            if (menu.Type == 2)
            {
                entity.EnCode = "jnpf_" + input.bindTable + "_jnpf_" + entity.EnCode;
            }
            // 在线开发
            if (menu.Type == 3)
            {
                entity.EnCode = "jnpf_" + input.bindTable + "_jnpf_" + entity.EnCode;
            }
        }
        if (await _repository.IsAnyAsync(x => x.EnCode.Equals(entity.EnCode) && x.DeleteMark == null && x.ModuleId == input.moduleId && x.Id != entity.Id))
            throw Oops.Oh(ErrorCode.COM1004);
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1007);
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleFormEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 批量新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("Actions/Batch")]
    [UnitOfWork]
    public async Task BatchCreate([FromBody] ModuleFormActionsBatchInput input)
    {
        var entitys = new List<ModuleFormEntity>();
        var menu = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id == input.moduleId && x.DeleteMark == null);
        foreach (var item in input.formJson)
        {
            var entity = input.Adapt<ModuleFormEntity>();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.EnabledMark = 1;
            entity.CreatorUserId = _userManager.UserId;
            entity.EnCode = item.enCode;
            entity.FullName = item.fullName;
            entity.BindTable = item.bindTable;
            entity.FieldRule = item.fieldRule;
            entity.ChildTableKey = item.childTableKey;
            entity.SortCode = 0;
            if (entity.FieldRule == 2 && item.childTableKey.IsNotEmptyOrNull())
                entity.EnCode = item.childTableKey + "-" + entity.EnCode;
            if (menu.IsNotEmptyOrNull() && entity.BindTable.IsNotEmptyOrNull() && entity.FieldRule == 1)
            {
                // 代码生成
                if (menu.Type == 2)
                {
                    entity.EnCode = "jnpf_" + input.bindTable + "_jnpf_" + entity.EnCode;
                }
                // 在线开发
                if (menu.Type == 3)
                {
                    entity.EnCode = input.bindTable + "." + item.enCode;
                }
            }
            if (await _repository.IsAnyAsync(x => x.EnCode.Equals(entity.EnCode) && x.DeleteMark == null && x.ModuleId == input.moduleId))
                throw Oops.Oh(ErrorCode.COM1004);
            entitys.Add(entity);
        }

        var newDic = await _repository.AsInsertable(entitys).ExecuteReturnEntityAsync();
        _ = newDic ?? throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 更新字段状态.
    /// </summary>
    /// <param name="id">id</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleFormEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId"></param>
    /// <returns></returns>
    public async Task<List<ModuleFormEntity>> GetList(string? moduleId = default)
    {
        return await _repository.AsQueryable().Where(x => x.ModuleId == moduleId && x.DeleteMark == null).ToListAsync();
    }

    /// <summary>
    /// 获取用户表单列表.
    /// <param name="moduleIdList">功能ids.</param>
    /// </summary>
    [NonAction]
    public async Task<List<ModuleFormOutput>> GetUserModuleFormList(List<string> moduleIdList)
    {
        var output = new List<ModuleFormOutput>();
        if (!_userManager.IsAdministrator)
        {
            if (moduleIdList != null && moduleIdList.Any())
            {
                var forms = await _repository.AsQueryable().Where(a => moduleIdList.Contains(a.ModuleId)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(q => q.SortCode).ToListAsync();
                output.AddRange(forms.Adapt<List<ModuleFormOutput>>());
            }

            var roles = _userManager.PermissionGroup;
            if (roles.Any())
            {
                var items = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, roles).Where(a => a.ItemType == "form").GroupBy(it => new { it.ItemId }).Select(a => a.ItemId).ToListAsync();
                var forms = await _repository.AsQueryable().Where(a => items.Contains(a.Id)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select<ModuleFormEntity>().OrderBy(q => q.SortCode).ToListAsync();
                output.AddRange(forms.Adapt<List<ModuleFormOutput>>());
            }
        }
        else
        {
            var forms = await _repository.AsQueryable().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select<ModuleFormEntity>().OrderBy(q => q.SortCode).ToListAsync();
            output = forms.Adapt<List<ModuleFormOutput>>();
        }

        return output;
    }

    #endregion
}