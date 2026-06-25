using JNPF.Common.Core.Manager;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 业务实现：AI 调用日志
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "AiCallLog", Order = 177)]
[Route("api/InteAssistant/[controller]")]
public class AiCallLogService : IAiCallLogService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储
    /// </summary>
    private readonly ISqlSugarRepository<AiCallLogEntity> _repository;

    /// <summary>
    /// 用户管理
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="AiCallLogService"/>类型的新实例
    /// </summary>
    public AiCallLogService(
        ISqlSugarRepository<AiCallLogEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取 AI 调用日志列表
    /// </summary>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] AiCallLogListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(input.model), it => it.Model == input.model)
            .WhereIF(input.statusCode != null, it => it.StatusCode == input.statusCode)
            .WhereIF(input.startTime != null, it => it.CreatorTime >= input.startTime)
            .WhereIF(input.endTime != null, it => it.CreatorTime <= input.endTime)
            .Where(it => it.DeleteMark == null)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new AiCallLogListOutput
            {
                id = it.Id,
                model = it.Model,
                promptTokens = it.PromptTokens,
                completionTokens = it.CompletionTokens,
                latencyMs = it.LatencyMs,
                statusCode = it.StatusCode,
                creatorTime = it.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id.Equals(it.CreatorUserId)).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<AiCallLogListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取 AI 调用日志详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsQueryable().FirstAsync(it => it.Id == id && it.DeleteMark == null);
        return data.Adapt<AiCallLogInfoOutput>();
    }

    #endregion
}
