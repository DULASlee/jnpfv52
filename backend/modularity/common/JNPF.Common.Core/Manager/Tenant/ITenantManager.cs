using JNPF.Common.Dtos.OAuth;
using SqlSugar;

namespace JNPF.Common.Core.Manager.Tenant;

public interface ITenantManager
{
    /// <summary>
    /// 获取或设置请求头部信息.
    /// </summary>
    Dictionary<string, object> Headers { get; set; }

    /// <summary>
    /// 多租户切换.
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    ConnectionConfigOptions ChangTenant(ISqlSugarClient sqlSugarClient, TenantInterFaceOutput input);

    /// <summary>
    /// 多租户切换.
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="tenantId"></param>
    /// <param name="isCache"></param>
    /// <returns></returns>
    Task<TenantInterFaceOutput> ChangTenant(ISqlSugarClient sqlSugarClient, string tenantId, bool isCache = true);

    /// <summary>
    /// 获取多租户信息.
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<TenantInterFaceOutput> GetTenant(string tenantId);
}
