using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Models.User;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.Permission.User;
using JNPF.Systems.Entitys.Dto.Role;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Dto.UserRelation;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;

namespace JNPF.Systems;

/// <summary>
///  业务实现：用户信息.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Users", Order = 163)]
[Route("api/permission/[controller]")]
public class UsersService : IUsersService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

    /// <summary>
    /// 流程相关.
    /// </summary>
    private readonly IFlowTaskRepository _flowTaskRepository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="UsersService"/>类型的新实例.
    /// </summary>
    public UsersService(
        ISqlSugarRepository<UserEntity> userRepository,
        IFlowTaskRepository flowTaskRepository,
        IUserManager userManager)
    {
        _repository = userRepository;
        _flowTaskRepository = flowTaskRepository;
        _userManager = userManager;
    }

    #region POST

    /// <summary>
    /// 保存工作交接.
    /// </summary>
    /// <param name="input">主键.</param>
    /// <returns></returns>
    [HttpPost("workHandover")]
    public async Task SaveWorkHandover([FromBody] UserWorkHandoverInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (input.toId.Equals(input.fromId)) throw Oops.Oh(ErrorCode.D5027);
            if (await _repository.IsAnyAsync(x => x.Id.Equals(input.toId) && x.IsAdministrator.Equals(1))) throw Oops.Oh(ErrorCode.D5028);

            // 记录 被交接人Id
            int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
            {
                HandoverUserId = input.fromId,
                LastModifyUserId = _userManager.UserId,
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.Id == input.toId).ExecuteCommandAsync();

            // 交接权限组
            if (input.permissionList != null && input.permissionList.Any())
            {
                var pList = await _repository.AsSugarClient().Queryable<PermissionGroupEntity>().Where(x => input.permissionList.Contains(x.Id)).ToListAsync();
                pList.ForEach(item =>
                {
                    var itemPList = item.PermissionMember?.Split(',').ToList();
                    itemPList.Add(input.toId + "--user");
                    itemPList.Remove(input.fromId + "--user");
                    item.PermissionMember = string.Join(",", itemPList);
                });

                await _repository.AsSugarClient().Updateable(pList).UpdateColumns(it => new { it.PermissionMember }).ExecuteCommandAsync();
            }

            // 待办
            if (input.waitList != null && input.waitList.Any()) _flowTaskRepository.SaveWorkHandover(input.toId, input.waitList, 1, input.fromId);

            // 负责流程
            if (input.flowList != null && input.flowList.Any()) _flowTaskRepository.SaveWorkHandover(input.toId, input.flowList, 2, input.fromId);

        }
        catch (Exception e)
        {
            throw e;
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    [NonAction]
    public UserEntity GetInfoByUserId(string userId)
    {
        return _repository.GetFirst(u => u.Id == userId && u.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<UserEntity> GetInfoByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetFirstAsync(u => u.Id == userId && u.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetList(CancellationToken cancellationToken = default)
    {
        return await _repository.AsQueryable().Where(u => u.DeleteMark == null).ToListAsync();
    }

    /// <summary>
    /// 获取用户信息 根据用户账户.
    /// </summary>
    /// <param name="account">用户账户.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<UserEntity> GetInfoByAccount(string account, CancellationToken cancellationToken = default)
    {
        return await _repository.GetFirstAsync(u => u.Account == account && u.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户信息 根据登录信息.
    /// </summary>
    /// <param name="account">用户账户.</param>
    /// <param name="password">用户密码.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<UserEntity> GetInfoByLogin(string account, string password, CancellationToken cancellationToken = default)
    {
        return await _repository.GetFirstAsync(u => u.Account == account && u.Password == password && u.DeleteMark == null);
    }

    /// <summary>
    /// 根据用户姓名获取用户ID.
    /// </summary>
    /// <param name="realName">用户姓名.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<string> GetUserIdByRealName(string realName, CancellationToken cancellationToken = default)
    {
        return (await _repository.GetFirstAsync(u => u.RealName == realName && u.DeleteMark == null)).Id;
    }

    /// <summary>
    /// 获取用户名.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否显示账号.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<string> GetUserName(string userId, bool isAccount = true, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(x => x.Id == userId && x.DeleteMark == null);
        if (entity.IsNullOrEmpty()) return string.Empty;
        return isAccount ? entity.RealName + "/" + entity.Account : entity.RealName;
    }

    /// <summary>
    /// 获取当前用户岗位信息.
    /// </summary>
    /// <param name="PositionIds"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<PositionInfoModel>> GetPosition(string organizeId, CancellationToken cancellationToken = default)
    {
        return await _repository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals("Position"))).Where((a, b) => a.OrganizeId.Equals(organizeId) && b.UserId.Equals(_userManager.UserId)).Select(a => new PositionInfoModel { id = a.Id, name = a.FullName }).ToListAsync();
    }

    /// <summary>
    /// 表达式获取用户.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<UserEntity> GetUserByExp(Expression<Func<UserEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _repository.GetFirstAsync(expression);
    }

    /// <summary>
    /// 表达式获取用户列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _repository.AsQueryable().Where(expression).ToListAsync();
    }

    /// <summary>
    /// 表达式获取指定字段的用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <param name="select">select 选择字段表达式.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression, Expression<Func<UserEntity, UserEntity>> select, CancellationToken cancellationToken = default)
    {
        return await _repository.AsQueryable().Where(expression).Select(select).ToListAsync();
    }

    #endregion
}
