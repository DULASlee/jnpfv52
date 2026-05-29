using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.ProjectGantt;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 项目计划
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "ProjectGantt", Order = 600)]
[Route("api/extend/[controller]")]
public class ProjectGanttService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<ProjectGanttEntity> _repository;

    public ProjectGanttService(ISqlSugarRepository<ProjectGanttEntity> repository)
    {
        _repository = repository;
    }

    #region GET

    /// <summary>
    /// 项目列表.
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] KeywordInput input)
    {
        var data = await _repository.AsQueryable().Where(x => x.Type == 1 && x.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToListAsync();
        var output = data.Adapt<List<ProjectGanttListOutput>>();
        await GetManagersInfo(output);
        return new { list = output };
    }

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="projectId">项目Id.</param>
    /// <returns></returns>
    [HttpGet("{projectId}/Task")]
    public async Task<dynamic> GetTaskList([FromQuery] KeywordInput input, string projectId)
    {
        var data = await _repository.AsQueryable()
            .Where(x => x.Type == 2 && x.ProjectId == projectId && x.DeleteMark == null)
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToListAsync();
        data.Add(await _repository.GetFirstAsync(x => x.Id == projectId));
        if (!string.IsNullOrEmpty(input.keyword))
        {
            data = data.TreeWhere(t => t.FullName.Contains(input.keyword), t => t.Id, t => t.ParentId);
        }
        var output = data.Adapt<List<ProjectGanttTaskListOutput>>();
        return new { list = output.ToTree() };
    }

    /// <summary>
    /// 任务树形.
    /// </summary>
    /// <param name="projectId">项目Id.</param>
    /// <returns></returns>
    [HttpGet("{projectId}/Task/Selector/{id}")]
    public async Task<dynamic> GetTaskTreeView(string projectId, string id)
    {
        var data = (await _repository.AsQueryable().Where(x => x.Type == 2 && x.ProjectId == projectId && x.DeleteMark == null).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync());
        data.Add(await _repository.GetFirstAsync(x => x.Id == projectId));
        if (!id.Equals("0"))
        {
            data.RemoveAll(x => x.Id == id);
        }
        var output = data.Adapt<List<ProjectGanttTaskTreeViewOutput>>();
        return new { list = output.ToTree() };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<ProjectGanttInfoOutput>();
        return data;
    }

    /// <summary>
    /// 项目任务信息.
    /// </summary>
    /// <param name="taskId">主键值.</param>
    /// <returns></returns>
    [HttpGet("Task/{taskId}")]
    public async Task<dynamic> GetTaskInfo(string taskId)
    {
        return (await _repository.GetFirstAsync(x => x.Id == taskId && x.DeleteMark == null)).Adapt<ProjectGanttTaskInfoOutput>();
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (await _repository.IsAnyAsync(x => x.ParentId != id && x.DeleteMark == null))
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity != null)
            {
                int isOk = await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
                if (isOk < 1)
                    throw Oops.Oh(ErrorCode.COM1002);
            }
            else
            {
                throw Oops.Oh(ErrorCode.COM1005);
            }
        }
        else
        {
            throw Oops.Oh(ErrorCode.D1007);
        }
    }

    /// <summary>
    /// 创建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ProjectGanttCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ProjectGanttEntity>();
        entity.Type = 1;
        entity.ParentId = "0";
        var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 编辑.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ProjectGanttUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ProjectGanttEntity>();
        var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 创建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("Task")]
    public async Task CreateTask([FromBody] ProjectGanttTaskCrInput input)
    {
        var entity = input.Adapt<ProjectGanttEntity>();
        entity.Type = 2;
        var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 编辑.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("Task/{id}")]
    public async Task UpdateTask(string id, [FromBody] ProjectGanttTaskUpInput input)
    {
        var entity = input.Adapt<ProjectGanttEntity>();
        var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1001);
    }
    #endregion

    #region PrivateMethod

    /// <summary>
    /// 项目参与人员.
    /// </summary>
    /// <param name="outputList"></param>
    /// <returns></returns>
    private async Task GetManagersInfo(List<ProjectGanttListOutput> outputList)
    {
        foreach (var output in outputList)
        {
            foreach (var id in output.managerIds.Split(","))
            {
                var managerInfo = new ManagersInfo();
                var userInfo = await _repository.AsSugarClient().Queryable<UserEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
                if (userInfo != null)
                {
                    managerInfo.account = userInfo.RealName + "/" + userInfo.Account;
                    managerInfo.headIcon = string.IsNullOrEmpty(userInfo.HeadIcon) ? string.Empty : "/api/file/Image/userAvatar/" + userInfo.HeadIcon;
                    output.managersInfo.Add(managerInfo);
                }
            }
        }
    }
    #endregion
}