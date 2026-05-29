using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Permission;
using JNPF.WorkFlow.Entitys.Dto.FlowComment;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程评论.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowEngine", Name = "FlowComment", Order = 304)]
[Route("api/workflow/Engine/[controller]")]
public class FlowCommentService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<FlowCommentEntity> _repository;
    private readonly IUserManager _userManager;

    public FlowCommentService(ISqlSugarRepository<FlowCommentEntity> repository, IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] FlowCommentListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowCommentEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.CreatorUserId == b.Id))
            .Where((a, b) => a.TaskId == input.taskId && a.DeleteMark == null)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select((a, b) => new FlowCommentListOutput()
            {
                id = a.Id,
                taskId = a.TaskId,
                text = a.Text,
                image = a.Image,
                file = a.File,
                creatorUserId = b.Id,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                creatorUserHeadIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
                isDel = SqlFunc.IIF(a.CreatorUserId == _userManager.UserId, true, false),
                lastModifyTime = a.LastModifyTime,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowCommentListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<FlowCommentInfoOutput>();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] FlowCommentCrInput input)
    {
        var entity = input.Adapt<FlowCommentEntity>();
        var isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] FlowCommentUpInput input)
    {
        var entity = input.Adapt<FlowCommentEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }
    #endregion
}
