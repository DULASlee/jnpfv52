using JNPF.Systems.Entitys.Permission;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：部门管理.
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 获取部门列表.
    /// </summary>
    /// <returns></returns>
    Task<List<OrganizeEntity>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 部门名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetDepName(string id);

    /// <summary>
    /// 公司名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetComName(string id);

    /// <summary>
    /// 公司结构树.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetOrganizeNameTree(string id);

    /// <summary>
    /// 公司id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetCompanyId(string id);

    /// <summary>
    /// 获取公司下所有部门.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<List<OrganizeEntity>> GetCompanyAllDep(string id, CancellationToken cancellationToken = default);
}