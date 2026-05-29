using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.DictionaryType;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 字典分类
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DictionaryType", Order = 202)]
[Route("api/system/[controller]")]
public class DictionaryTypeService : IDictionaryTypeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基本仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DictionaryTypeEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DictionaryTypeService"/>类型的新实例.
    /// </summary>
    public DictionaryTypeService(
        ISqlSugarRepository<DictionaryTypeEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        if (data.ParentId.Equals("-1"))
            data.ParentId = data.ZxDataType.ToString();

        return data.Adapt<DictionaryTypeInfoOutput>();
    }

    private  async Task<List<DictionaryTypeEntity>> GetCommonList() 
    {
        var data = await GetList();

        var businessDictionary = new DictionaryTypeEntity()
        {
            Id = "0",
            FullName = "系统自定义字典",
            ZxDataType =  (int)ZxDataTypeEnum.TenantSystem,
            Type = (int)ZxDataTypeEnum.TenantSystem,
            ParentId = "-1",
            Description = "租户应用系统自定义的参数",
        };
        var tenantDictionary = new DictionaryTypeEntity()
        {
            Id = "1",
            FullName = "平台通用字典",
            ZxDataType = (int)ZxDataTypeEnum.Tenant,
            Type = (int)ZxDataTypeEnum.TenantSystem,
            ParentId = "-1",
            Description = "租户平台应用所有系统自定义的参数",
        };

        var bizSystemDictionary = new DictionaryTypeEntity()
        {
            Id = "2",
            FullName = "系统通用字典",
            ZxDataType = (int)ZxDataTypeEnum.System,
            Type = (int)ZxDataTypeEnum.System,
            ParentId = "-1",
            Description = "业务系统定义的通用参数",
        };

        var frameworkDictionary = new DictionaryTypeEntity()
        {
            Id = "3",
            FullName = "开发逻辑字典",
            ZxDataType = (int)ZxDataTypeEnum.Framework,
            Type = (int)ZxDataTypeEnum.Framework,
            ParentId = "-1",
            Description = "系统开发应用的数据字典表，通常为固定不变，与开发业务逻辑相关的参数",
        };
         

        foreach (var item in data)
        {
            if (item.ZxDataType.Equals(0) && item.ParentId.Equals("-1"))
                item.ParentId = businessDictionary.Id;
            else if (item.ZxDataType.Equals(1) && item.ParentId.Equals("-1"))
                item.ParentId = tenantDictionary.Id;
            else if (item.ZxDataType.Equals(2) && item.ParentId.Equals("-1"))
                item.ParentId = bizSystemDictionary.Id;
            else if (item.ZxDataType.Equals(3) && item.ParentId.Equals("-1"))
                item.ParentId = frameworkDictionary.Id;
       
        }

        data.Add(businessDictionary);
        data.Add(tenantDictionary);
        data.Add(bizSystemDictionary);
        data.Add(frameworkDictionary); 

        return data;
    }

    /// <summary>
    /// 列表.
    /// </summary>
    [HttpGet("")]
    public async Task<dynamic> GetList_Api()
    {
        var data =await  GetCommonList();


        var output = data.Adapt<List<DictionaryTypeListOutput>>();

        return new { list = output.ToTree("-1") };
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        var list =await  GetCommonList();

        if (!id.Equals("0"))
            list.RemoveAll(x => x.Id == id);


        var output = list.Adapt<List<DictionaryTypeSelectorOutput>>();
        return new { list = output.ToTree("-1") };
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create_Api([FromBody] DictionaryTypeCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3001);
        var entity = input.Adapt<DictionaryTypeEntity>();



        DictionaryTypeEntity parentEntity = new DictionaryTypeEntity();
        if (entity.ParentId.Equals("0") || entity.ParentId.Equals("1") || entity.ParentId.Equals("2") || entity.ParentId.Equals("3"))
        {
            entity.ZxDataType = entity.ParentId.ParseToInt();
            entity.ParentId = "-1";
        }
        else
        {
            entity.ZxDataType = await _repository.AsQueryable()
                .Where(it => it.Id.Equals(entity.ParentId))
                .Select(it => it.ZxDataType)
                .FirstAsync();
        }

        ZxDataTypeEnum dataType = (ZxDataTypeEnum)entity.ZxDataType;
        switch (dataType)
        {
            case ZxDataTypeEnum.TenantSystem:
                entity.TenantId = _userManager.TenantId;
                entity.ZxSystemId = _userManager.BizSystemId;
                break;
            case ZxDataTypeEnum.Tenant:
                entity.TenantId = _userManager.TenantId;
                entity.ZxSystemId = _userManager.BizSystemId;
                break;
            case ZxDataTypeEnum.System:
                entity.TenantId = null;
                entity.ZxSystemId = _userManager.BizSystemId;
                break;
            case ZxDataTypeEnum.Framework:
                entity.TenantId = null;
                entity.ZxSystemId = null;
                break;
            default:
                break;
        }

        entity.EnabledMark = 1;
        entity.Type = entity.ZxDataType;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete_Api(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3000);
        if (await AllowDelete(id))
        {
            var isOk = await _repository.AsUpdateable().SetColumns(it => new DictionaryTypeEntity()
            {
                DeleteTime = DateTime.Now,
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId
            }).Where(x => x.Id == id).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1002);
        }
        else
        {
            throw Oops.Oh(ErrorCode.D3002);
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update_Api(string id, [FromBody] DictionaryTypeUpInput input)
    {
        string zxDataType = input.parentId;
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null && x.ZxDataType.ToString()== zxDataType) || await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null&& x.ZxDataType.ToString() == zxDataType))
            throw Oops.Oh(ErrorCode.D3001);
        var entity = input.Adapt<DictionaryTypeEntity>();
        DictionaryTypeEntity parentEntity = new DictionaryTypeEntity();
        if (entity.ParentId.Equals("0") || entity.ParentId.Equals("1") || entity.ParentId.Equals("2") || entity.ParentId.Equals("3") || entity.ParentId.Equals("4"))
        {
            entity.ZxDataType = entity.ParentId.ParseToInt();
            entity.ParentId = "-1";
        }
        else
        {
            parentEntity = await _repository.AsQueryable()
                .Where(it => it.Id.Equals(entity.ParentId)).FirstAsync();
            entity.ZxDataType = parentEntity.ZxDataType;
        }

        ZxDataTypeEnum dataType = (ZxDataTypeEnum)entity.ZxDataType;
        switch (dataType)
        {
            case ZxDataTypeEnum.TenantSystem:
                entity.TenantId = _userManager.TenantId;
                entity.ZxSystemId = _userManager.BizSystemId;
                break;
            case ZxDataTypeEnum.Tenant:
                entity.TenantId = _userManager.TenantId;
                //entity.ZxSystemId 不更新，首次添加时是什么就是什么
                break;
            case ZxDataTypeEnum.System:
                entity.TenantId = null;
                entity.ZxSystemId = _userManager.BizSystemId;
                break;
            case ZxDataTypeEnum.Framework:
                entity.TenantId = null;
                entity.ZxSystemId = null;
                break;
            default:
                break;
        }


        entity.EnabledMark = 1;
        entity.Type = entity.ZxDataType;

        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);


    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<DictionaryTypeEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => (x.Id == id || x.EnCode == id) && x.DeleteMark == null);
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<DictionaryTypeEntity>> GetList()
    {
        var query = _repository.AsQueryable().ClearFilter();

        var q1 = _repository.AsQueryable().ClearFilter().Where(aa => aa.ZxDataType == (int)ZxDataTypeEnum.Framework); //框架逻辑数据
        var q2 = _repository.AsQueryable().ClearFilter().Where(aa => aa.TenantId == _userManager.TenantId && aa.ZxSystemId == _userManager.BizSystemId && aa.ZxDataType == (int)ZxDataTypeEnum.TenantSystem); //租户与业务数据
        var q3 = _repository.AsQueryable().ClearFilter().Where(aa =>  aa.ZxSystemId == _userManager.BizSystemId && aa.ZxDataType == (int)ZxDataTypeEnum.System);//应用系统数据
        var q4 = _repository.AsQueryable().ClearFilter().Where(aa => aa.TenantId == _userManager.TenantId && aa.ZxDataType == (int)ZxDataTypeEnum.Tenant); //租户平台数据

        return await _repository.AsSugarClient().UnionAll(q1, q2, q3, q4).ClearFilter().
            Where(x => x.DeleteMark == null).
            OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 递归获取所有分类.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="typeList"></param>
    /// <returns></returns>
    [NonAction]
    public async Task GetListAllById(string id, List<DictionaryTypeEntity> typeList)
    {
        var entity = await GetInfo(id);
        if (entity != null)
        {
            typeList.Add(entity);
            if (await _repository.IsAnyAsync(x => x.ParentId == entity.Id && x.DeleteMark == null))
            {
                var list = await _repository.AsQueryable().Where(x => x.ParentId == entity.Id && x.DeleteMark == null).ToListAsync();
                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        await GetListAllById(item.Id, typeList);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 是否存在上级.
    /// </summary>
    /// <param name="Entities"></param>
    /// <returns></returns>
    public bool IsExistParent(List<DictionaryTypeEntity> Entities)
    {
        foreach (var item in Entities)
        {
            if (_repository.IsAny(x => x.Id == item.ParentId && x.DeleteMark == null))
                return true;
        }

        return false;
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 是否可以删除.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<bool> AllowDelete(string id)
    {
        var flag = true;
        if (await _repository.IsAnyAsync(o => o.ParentId.Equals(id) && o.DeleteMark == null))
            return false;
        if (await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().AnyAsync(p => p.DictionaryTypeId.Equals(id) && p.DeleteMark == null))
            return false;
        return flag;
    }

    #endregion
}