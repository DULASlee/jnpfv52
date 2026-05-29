using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：用户关系.
/// </summary>
public interface IUserRelationService
{
    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">用户ID.</param>
    /// <returns></returns>
    Task Delete(string id);

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="ids">对象ID组.</param>
    /// <param name="relationType">关系类型(岗位-Position;角色-Role;组织-Organize;分组-Group;).</param>
    /// <returns></returns>
    List<UserRelationEntity> CreateUserRelation(string userId, string ids, string relationType);

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="input">新增数据.</param>
    /// <returns></returns>
    Task Create(List<UserRelationEntity> input);

    /// <summary>
    /// 根据用户主键获取列表.
    /// </summary>
    /// <param name="userId">用户主键.</param>
    /// <returns></returns>
    Task<List<UserRelationEntity>> GetListByUserId(string userId);

    /// <summary>
    /// 获取用户.
    /// </summary>
    /// <param name="type">关系类型.</param>
    /// <param name="objId">对象ID.</param>
    /// <returns></returns>
    List<string> GetUserId(string type, string objId);

    /// <summary>
    /// 获取用户.
    /// </summary>
    /// <param name="type">关系类型.</param>
    /// <param name="objId">对象ID.</param>
    /// <returns></returns>
    List<string> GetUserId(List<string> objId, string type = null);

    /// <summary>
    /// 获取用户(分页).
    /// </summary>
    /// <param name="userIds">用户ID组.</param>
    /// <param name="objIds">对象ID组.</param>
    /// <param name="pageInputBase">分页参数.</param>
    /// <returns></returns>
    dynamic GetUserPage(UserConditionInput input, ref bool hasCandidates);

    /// <summary>
    /// 新用户组件获取人员.
    /// </summary>
    /// <param name="Ids"></param>
    /// <returns></returns>
    List<string> GetUserId(List<string> Ids);
}