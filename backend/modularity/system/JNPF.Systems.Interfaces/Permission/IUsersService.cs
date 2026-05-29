using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Permission;
using System.Linq.Expressions;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：用户信息.
/// </summary>
public interface IUsersService
{
    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    UserEntity GetInfoByUserId(string userId);

    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<UserEntity> GetInfoByUserIdAsync(string userId);

    /// <summary>
    /// 根据用户账户.
    /// </summary>
    /// <param name="account">用户账户.</param>
    /// <returns></returns>
    Task<UserEntity> GetInfoByAccount(string account);

    /// <summary>
    /// 获取用户信息 根据登录信息.
    /// </summary>
    /// <param name="account">用户账户.</param>
    /// <param name="password">用户密码.</param>
    /// <returns></returns>
    Task<UserEntity> GetInfoByLogin(string account, string password);

    /// <summary>
    /// 根据用户姓名获取用户ID.
    /// </summary>
    /// <param name="realName">用户姓名.</param>
    /// <returns></returns>
    Task<string> GetUserIdByRealName(string realName);

    /// <summary>
    /// 获取用户名.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否显示账号.</param>
    /// <returns></returns>
    Task<string> GetUserName(string userId, bool isAccount = true);

    /// <summary>
    /// 获取用户列表.
    /// </summary>
    Task<List<UserEntity>> GetList();

    /// <summary>
    /// 用户岗位.
    /// </summary>
    /// <param name="organizeId"></param>
    /// <returns></returns>
    Task<List<PositionInfoModel>> GetPosition(string organizeId);

    /// <summary>
    /// 表达式获取用户.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <returns></returns>
    Task<UserEntity> GetUserByExp(Expression<Func<UserEntity, bool>> expression);

    /// <summary>
    /// 表达式获取用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <returns></returns>
    Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression);

    /// <summary>
    /// 表达式获取指定字段的用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <param name="select">select 选择字段表达式.</param>
    /// <returns></returns>
    Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression, Expression<Func<UserEntity, UserEntity>> select);
}