using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.DataInterfaceVarate;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 数据接口变量
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataInterfaceVariate", Order = 204)]
[Route("api/system/[controller]")]
public class DataInterfaceVariateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<DataInterfaceVariateEntity> _repository;
    private readonly IFileManager _fileManager;
    private readonly IUserManager _userManager;

    public DataInterfaceVariateService(
        ISqlSugarRepository<DataInterfaceVariateEntity> repository,
        IFileManager fileManager,
        IUserManager userManager)
    {
        _repository = repository;
        _fileManager = fileManager;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="interfaceId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("{interfaceId}")]
    public async Task<dynamic> GetList(string interfaceId, [FromQuery] KeywordInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>()
             .Where(a => a.InterfaceId == interfaceId && a.DeleteMark == null)
             .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword))
             .OrderBy(a => a.SortCode).OrderByDescending(a => a.CreatorTime).OrderByIF(input.keyword.IsNotEmptyOrNull(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new DataInterfaceVariateOutput
            {
                id = a.Id,
                interfaceId = a.InterfaceId,
                fullName = a.FullName,
                value = a.Value,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime
            }).ToListAsync();
        return new { list = list };
    }

    /// <summary>
    /// 下拉列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var output = new List<DataInterfaceVariateTreeOutput>();
        var dataInterfaceList = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>()
            .Where(x => x.IsPostposition == 1 && x.EnabledMark == 1 && x.DeleteMark == null)
            .Select(x => new DataInterfaceVariateTreeOutput
            {
                id = x.Id,
                fullName = x.FullName,
                type = 0,
                parentId = "0"
            }).ToListAsync();
        var dataInterfaceVariateList = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .Select(x => new DataInterfaceVariateTreeOutput
            {
                id = x.Id,
                fullName = x.FullName,
                type = 1,
                parentId = x.InterfaceId
            }).ToListAsync();
        output = dataInterfaceList.Union(dataInterfaceVariateList).ToList().ToTree();
        return output;
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Info")]
    public async Task<dynamic> Info(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<DataInterfaceVariateInput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var jsonStr = entity.ToJsonString();
        return await _fileManager.Export(jsonStr, entity.FullName, ExportFileType.ffa);
    }
    #endregion

    #region Post

    /// <summary>
    /// 添加接口.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] DataInterfaceVariateInput input)
    {
        if (input.fullName.Contains("@"))
            throw Oops.Oh(ErrorCode.xg1006);
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.IO0007);
        var entity = input.Adapt<DataInterfaceVariateEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DataInterfaceVariateInput input)
    {
        if (input.fullName.Contains("@"))
            throw Oops.Oh(ErrorCode.xg1006);
        if (await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.IO0007);
        var entity = input.Adapt<DataInterfaceVariateEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(IFormFile file)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.ffa.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        var data = josn.ToObject<DataInterfaceVariateEntity>();
        if (data == null)
            throw Oops.Oh(ErrorCode.D3006);
        data.CreatorTime = DateTime.Now;
        data.CreatorUserId = _userManager.UserId;
        data.LastModifyUserId = null;
        data.LastModifyTime = null;
        var isOk = await _repository.AsSugarClient().Storageable(data).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.D3008);
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
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        if (entity.FullName.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }
    #endregion
}
