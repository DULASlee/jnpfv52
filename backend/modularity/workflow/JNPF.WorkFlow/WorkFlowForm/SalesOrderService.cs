using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.WorkFlow.Entitys;
using JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.SalesOrder;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.WorkFlowForm;

/// <summary>
/// 销售订单
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowForm", Name = "SalesOrder", Order = 532)]
[Route("api/workflow/Form/[controller]")]
public class SalesOrderService : ISalesOrderService, IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<SalesOrderEntity> _sqlSugarRepository;
    private readonly ICacheManager _cacheManager;
    private readonly IFileManager _fileManager;
    private readonly IUserManager _userManager;

    public SalesOrderService(
        ISqlSugarRepository<SalesOrderEntity> sqlSugarRepository,
        ICacheManager cacheManager,
        IFileManager fileManager,
        IUserManager userManager)
    {
        _sqlSugarRepository = sqlSugarRepository;
        _cacheManager = cacheManager;
        _fileManager = fileManager;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        if ("0".Equals(id)) return null;
        var data = (await _sqlSugarRepository.GetFirstAsync(x => x.Id == id)).Adapt<SalesOrderInfoOutput>();
        data.entryList = (await _sqlSugarRepository.AsSugarClient().Queryable<SalesOrderEntryEntity>().Where(x => x.SalesOrderId == id).ToListAsync()).Adapt<List<EntryListItem>>();
        return data;
    }

    #endregion

    #region POST

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{id}")]
    public async Task Save(string id, [FromBody] SalesOrderInput input)
    {
        input.id = id;
        var entity = input.Adapt<SalesOrderEntity>();
        var entityList = input.entryList.Adapt<List<SalesOrderEntryEntity>>();
        entity.Id = id;
        if (_sqlSugarRepository.IsAny(x => x.Id == id))
        {
            if (entityList.IsNotEmptyOrNull())
            {
                foreach (var item in entityList)
                {
                    item.Id = SnowflakeIdHelper.NextId();
                    item.SalesOrderId = entity.Id;
                    item.SortCode = entityList.IndexOf(item);
                }
            }
            await _sqlSugarRepository.AsSugarClient().Deleteable<SalesOrderEntryEntity>(x => x.SalesOrderId == id).ExecuteCommandAsync();
            await _sqlSugarRepository.AsSugarClient().Insertable(entityList).ExecuteCommandAsync();
            await _sqlSugarRepository.UpdateAsync(entity);
            if (entity.FileJson.IsNotEmptyOrNull() && entity.FileJson.IsNotEmptyOrNull())
            {
                foreach (var item in entity.FileJson.ToList<AnnexModel>())
                {
                    if (item.IsNotEmptyOrNull() && item.FileType == "delete")
                    {
                        await _fileManager.DeleteFile(Path.Combine(FileVariable.SystemFilePath, item.FileName));
                    }
                }
            }
        }
        else
        {
            if (entityList.IsNotEmptyOrNull())
            {
                foreach (var item in entityList)
                {
                    item.Id = SnowflakeIdHelper.NextId();
                    item.SalesOrderId = entity.Id;
                    item.SortCode = entityList.IndexOf(item);
                }
            }
            await _sqlSugarRepository.AsSugarClient().Insertable(entityList).ExecuteCommandAsync();
            await _sqlSugarRepository.InsertAsync(entity);
            _cacheManager.Del(string.Format("{0}{1}_{2}", CommonConst.CACHEKEYBILLRULE, _userManager.TenantId, _userManager.UserId + "WF_LeaveApplyNo"));
        }
    }
    #endregion
}