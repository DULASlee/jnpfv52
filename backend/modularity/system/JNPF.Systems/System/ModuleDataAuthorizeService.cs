using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.ModuleDataAuthorize;
using JNPF.Systems.Entitys.Dto.ModuleDataAuthorizeScheme;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据权限
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "ModuleDataAuthorize", Order = 214)]
[Route("api/system/[controller]")]
public class ModuleDataAuthorizeService : IModuleDataAuthorizeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ModuleDataAuthorizeEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleDataAuthorizeService"/>类型的新实例.
    /// </summary>
    public ModuleDataAuthorizeService(
        ISqlSugarRepository<ModuleDataAuthorizeEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{moduleId}/List")]
    public async Task<dynamic> GetList(string moduleId, [FromQuery] KeywordInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeEntity, ModuleEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.ModuleId == b.Id))
            .Where((a, b) => a.ModuleId == moduleId && a.DeleteMark == null && b.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.EnCode.Contains(input.keyword) || a.FullName.Contains(input.keyword))
            .OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderBy(a => a.SortCode)
            .Select((a, b) => new ModuleDataAuthorizeListOutput()
            {
                id = a.Id,
                fullName = a.FullName,
                type = a.Type,
                conditionSymbol = a.ConditionSymbol,
                conditionText = a.ConditionText,
                bindTable = a.BindTable,
                fieldRule = a.FieldRule,
                format = a.Format,
                enCode = SqlFunc.IF(b.Type == 2 && a.FieldRule == 1 && !SqlFunc.IsNullOrEmpty(a.BindTable)).Return(a.EnCode.Replace("jnpf_" + a.BindTable + "_jnpf_", ""))
                .ElseIF(b.Type == 3 && a.FieldRule == 1).Return(a.EnCode.Replace(a.BindTable + ".", ""))
                 .ElseIF(a.FieldRule == 2).Return(a.EnCode.Replace(a.ChildTableKey + "-", "")).End(a.EnCode),
            }).ToListAsync();

        foreach (var item in list)
        {
            var symbolNameList = new List<string>();
            foreach (var subItem in item.conditionSymbol.Split(",").ToList()) symbolNameList.Add(getConditionSymbolName(subItem));
            item.conditionSymbolName = string.Join(",", symbolNameList);
            item.conditionName = getConditionName(item.conditionText);
        }

        return new { list = list };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
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
        return data.Adapt<ModuleDataAuthorizeInfoOutput>();
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
        var tInfo = new TemplateParsingBase(visualDevEntity);
        return tInfo.SingleFormData.Where(x => x.__vModel__.IsNotEmptyOrNull()).Select(x => new { field = x.__vModel__, fieldName = x.__config__.label });
    }
    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ModuleDataAuthorizeCrInput input)
    {
        var entity = input.Adapt<ModuleDataAuthorizeEntity>();
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
                entity.EnCode = input.bindTable + "." + input.enCode;
            }
        }
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
    public async Task Update(string id, [FromBody] ModuleDataAuthorizeUpInput input)
    {
        var entity = input.Adapt<ModuleDataAuthorizeEntity>();
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
                entity.EnCode = input.bindTable + "." + input.enCode;
            }
        }
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
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        await AnyScheme(id);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleDataAuthorizeEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleDataAuthorizeEntity>> GetList(string? moduleId = default)
    {
        return await _repository.AsQueryable().Where(x => x.DeleteMark == null).WhereIF(!moduleId.IsNullOrEmpty(), it => it.ModuleId == moduleId).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 方案中是否存在字段.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task AnyScheme(string id)
    {
        var moduleDataAuthorizeEntity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var schemeEntityList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => x.ModuleId == moduleDataAuthorizeEntity.ModuleId && x.DeleteMark == null).ToListAsync();
        var ids = new List<string>();
        foreach (var item in schemeEntityList)
        {
            if (item.ConditionJson.IsNotEmptyOrNull() && !item.ConditionJson.Equals("[]"))
            {
                var conditionJson = item.ConditionJson.ToObject<List<ConditionJsonItem>>();
                if (conditionJson.Any(x => x.groups.Select(x => x.id).Contains(id)))
                    throw Oops.Oh(ErrorCode.D4010, item.FullName);
            }
        }
    }
    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取条件名称.
    /// </summary>
    /// <param name="conditionText"></param>
    /// <returns></returns>
    private string getConditionName(string conditionText)
    {
        switch (conditionText)
        {
            case "input":
                conditionText = "任意文本";
                break;
            case "@organizeId":
                conditionText = "当前组织";
                break;
            case "@organizationAndSuborganization":
                conditionText = "当前组织及子组织";
                break;
            case "@userId":
                conditionText = "当前用户";
                break;
            case "@userAndSubordinates":
                conditionText = "当前用户及下属";
                break;
            case "@branchManageOrganize":
                conditionText = "当前分管组织";
                break;
            case "@branchManageOrganizeAndSub":
                conditionText = "当前分管组织及子组织";
                break;
            case "datePicker":
                conditionText = "日期选择";
                break;
            case "inputNumber":
                conditionText = "数字输入";
                break;
            case "organizeSelect":
                conditionText = "组织选择";
                break;
            case "depSelect":
                conditionText = "部门选择";
                break;
            case "posSelect":
                conditionText = "岗位选择";
                break;
            case "roleSelect":
                conditionText = "角色选择";
                break;
            case "groupSelect":
                conditionText = "分组选择";
                break;
            case "userSelect":
                conditionText = "用户选择";
                break;
        }

        return conditionText;
    }

    /// <summary>
    /// 获取条件符号名称.
    /// </summary>
    /// <param name="conditionSymbol"></param>
    /// <returns></returns>
    private string getConditionSymbolName(string conditionSymbol)
    {
        switch (conditionSymbol)
        {
            case "==":
                conditionSymbol = "等于";
                break;
            case "between":
                conditionSymbol = "介于";
                break;
            case "<>":
                conditionSymbol = "不等于";
                break;
            case ">=":
                conditionSymbol = "大于等于";
                break;
            case "<=":
                conditionSymbol = "小于等于";
                break;
            case ">":
                conditionSymbol = "大于";
                break;
            case "<":
                conditionSymbol = "小于";
                break;
            case "notIn":
                conditionSymbol = "不包含任意一个";
                break;
            case "in":
                conditionSymbol = "包含任意一个";
                break;
            case "notNull":
                conditionSymbol = "不为空";
                break;
            case "null":
                conditionSymbol = "为空";
                break;
            case "notLike":
                conditionSymbol = "不包含";
                break;
            case "like":
                conditionSymbol = "包含";
                break;
            case "and":
                conditionSymbol = "并且";
                break;
            case "or":
                conditionSymbol = "或者";
                break;
        }

        return conditionSymbol;
    }

    #endregion
}