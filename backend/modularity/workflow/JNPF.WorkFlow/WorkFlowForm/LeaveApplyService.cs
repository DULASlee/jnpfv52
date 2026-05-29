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
using JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.LeaveApply;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.WorkFlowForm;

/// <summary>
/// 请假申请
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowForm", Name = "LeaveApply", Order = 516)]
[Route("api/workflow/Form/[controller]")]
public class LeaveApplyService : ILeaveApplyService, IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<LeaveApplyEntity> _sqlSugarRepository;
    private readonly ICacheManager _cacheManager;
    private readonly IFileManager _fileManager;
    private readonly IUserManager _userManager;

    public LeaveApplyService(
        ISqlSugarRepository<LeaveApplyEntity> sqlSugarRepository,
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
        return (await _sqlSugarRepository.GetFirstAsync(x => x.Id == id)).Adapt<LeaveApplyInfoOutput>();
    }
    #endregion

    #region POST

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="id">表单信息.</param>
    /// <param name="input">表单信息.</param>
    /// <returns></returns>
    [HttpPost("{id}")]
    public async Task Save(string id, [FromBody] LeaveApplyInput input)
    {
        input.id = id;
        var entity = input.Adapt<LeaveApplyEntity>();
        entity.Id = id;
        if (_sqlSugarRepository.IsAny(x => x.Id == id))
        {
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
            await _sqlSugarRepository.InsertAsync(entity);
            _cacheManager.Del(string.Format("{0}{1}_{2}", CommonConst.CACHEKEYBILLRULE, _userManager.TenantId, _userManager.UserId + "WF_LeaveApplyNo"));
        }
    }
    #endregion
}
