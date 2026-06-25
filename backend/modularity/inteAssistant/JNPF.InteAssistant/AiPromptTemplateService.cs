using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 业务实现：AI Prompt 模板
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "AiPromptTemplate", Order = 178)]
[Route("api/InteAssistant/[controller]")]
public class AiPromptTemplateService : IAiPromptTemplateService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储
    /// </summary>
    private readonly ISqlSugarRepository<AiPromptTemplateEntity> _repository;

    /// <summary>
    /// 用户管理
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="AiPromptTemplateService"/>类型的新实例
    /// </summary>
    public AiPromptTemplateService(
        ISqlSugarRepository<AiPromptTemplateEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取 Prompt 模板列表
    /// </summary>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] AiPromptTemplateListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(input.category), it => it.Category == input.category)
            .WhereIF(input.isActive != null, it => it.IsActive == input.isActive)
            .WhereIF(!string.IsNullOrEmpty(input.name), it => it.Name.Contains(input.name))
            .Where(it => it.DeleteMark == null)
            .OrderBy(it => it.Category, OrderByType.Asc)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new AiPromptTemplateListOutput
            {
                id = it.Id,
                name = it.Name,
                category = it.Category,
                version = it.Version,
                isActive = it.IsActive,
                creatorTime = it.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id.Equals(it.CreatorUserId)).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                lastModifyTime = it.LastModifyTime,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<AiPromptTemplateListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取 Prompt 模板详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsQueryable().FirstAsync(it => it.Id == id && it.DeleteMark == null);
        return data.Adapt<AiPromptTemplateInfoOutput>();
    }

    /// <summary>
    /// 按分类获取模板列表（不分页，供流水线调用）
    /// </summary>
    [HttpGet("Category/{category}")]
    public async Task<List<AiPromptTemplateListOutput>> GetByCategory(string category)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.Category == category && it.DeleteMark == null && it.IsActive == 1)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new AiPromptTemplateListOutput
            {
                id = it.Id,
                name = it.Name,
                category = it.Category,
                version = it.Version,
                isActive = it.IsActive,
                creatorTime = it.CreatorTime,
                creatorUser = null,
                lastModifyTime = it.LastModifyTime,
            }).ToListAsync();
        return data;
    }

    /// <summary>
    /// 按名称获取当前激活版本（供流水线加载 prompt）
    /// </summary>
    [HttpGet("Active/{name}")]
    public async Task<dynamic> GetActiveByName(string name)
    {
        var data = await _repository.AsQueryable()
            .FirstAsync(it => it.Name == name && it.DeleteMark == null && it.IsActive == 1);
        if (data == null)
            throw Oops.Bah("未找到激活的 Prompt 模板：" + name);
        return data.Adapt<AiPromptTemplateInfoOutput>();
    }

    #endregion

    #region POST

    /// <summary>
    /// 创建 Prompt 模板
    /// </summary>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] AiPromptTemplateCrInput input)
    {
        if (await _repository.IsAnyAsync(it => it.DeleteMark == null && it.Name == input.name && it.Version == input.version))
            throw Oops.Bah($"Prompt 模板 \"{input.name}\" 版本 {input.version} 已存在");

        var entity = input.Adapt<AiPromptTemplateEntity>();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        var result = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (result < 1)
            throw Oops.Oh(ErrorCode.COM1000);
        return entity.Id;
    }

    /// <summary>
    /// 更新 Prompt 模板
    /// </summary>
    [HttpPut("{id}")]
    public async Task<dynamic> Update(string id, [FromBody] AiPromptTemplateUpInput input)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1007);

        input.Adapt(entity);
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        var result = await _repository.AsUpdateable(entity).UpdateColumns(it => new
        {
            it.Name,
            it.Category,
            it.Template,
            it.Version,
            it.IsActive,
            it.LastModifyTime,
            it.LastModifyUserId
        }).ExecuteCommandHasChangeAsync();
        if (!result)
            throw Oops.Oh(ErrorCode.COM1001);
        return id;
    }

    /// <summary>
    /// 删除 Prompt 模板
    /// </summary>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1007);

        var result = await _repository.AsUpdateable()
            .SetColumns(it => new AiPromptTemplateEntity
            {
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId,
            })
            .Where(it => it.Id == id && it.DeleteMark == null)
            .ExecuteCommandHasChangeAsync();
        if (!result)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
