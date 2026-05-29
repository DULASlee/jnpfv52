using JNPF.Common.Const;
using JNPF.Common.Dtos.OAuth;
using JNPF.Common.Enums;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using JNPF.UnifyResult;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.Common.Core.Manager.Tenant;

/// <summary>
/// 租户管理.
/// </summary>
public class TenantManager : ITenantManager, ITransient
{
    /// <summary>
    /// 数据库配置选项.
    /// </summary>
    private readonly SqlSugar.ConnectionStringsOptions _connectionStrings;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 获取或设置请求头部信息.
    /// </summary>
    public Dictionary<string, object> Headers { get; set; }

    public TenantManager(IOptions<SqlSugar.ConnectionStringsOptions> connectionOptions, IOptions<TenantOptions> tenantOptions, ICacheManager cacheManager, IDataBaseManager dataBaseManager)
    {
        _connectionStrings = connectionOptions.Value;
        _tenant = tenantOptions.Value;
        Headers = GetHeaders();
        _cacheManager = cacheManager;
        _dataBaseManager = dataBaseManager;
    }

    /// <summary>
    /// 多租户切换.
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public ConnectionConfigOptions ChangTenant(ISqlSugarClient sqlSugarClient, TenantInterFaceOutput input)
    {
        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        ConnectionConfigOptions options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);

        switch (input.type)
        {
            case 1:
                if (input.dotnet == null) throw Oops.Oh(ErrorCode.D1025);
                options = JNPFTenantExtensions.GetLinkToOrdinary(input.tenantId, defaultConnection.DBName, input.dotnet);
                break;
            case 2:
                if (input.linkList == null || !input.linkList.Any()) throw Oops.Oh(ErrorCode.D1025);
                options = JNPFTenantExtensions.GetLinkToCustom(input.tenantId, input.linkList);
                break;
            default:
                if (input.dotnet == null) throw Oops.Oh(ErrorCode.D1025);
                options = JNPFTenantExtensions.GetLinkToOrdinary(input.tenantId, input.dotnet);
                break;
        }

        sqlSugarClient.AsTenant().AddConnection(JNPFTenantExtensions.GetConfig(options));
        sqlSugarClient.AsTenant().ChangeDatabase(input.tenantId);
        if (input.type == 1 && !"default".Equals(input.tenantId))
        {
            sqlSugarClient.QueryFilter.Clear();
            sqlSugarClient.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == input.dotnet);
            sqlSugarClient.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    entityInfo.SetValue(input.dotnet);
                }
                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    entityInfo.SetValue(input.dotnet);
                }
                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.DeleteByObject)
                {
                    entityInfo.SetValue(input.dotnet);
                }
            };
        }
        return options;
    }

    /// <summary>
    /// 多租户切换.
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<TenantInterFaceOutput> ChangTenant(ISqlSugarClient sqlSugarClient, string tenantId, bool isCache = true)
    {
        if (await _cacheManager.ExistsAsync(CommonConst.GLOBALTENANT) && isCache)
        {
            var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(tenantId));
            if (tenantCache != null)
            {
                sqlSugarClient = _dataBaseManager.GetTenantSqlSugarClient(tenantId, tenantCache);
                return new TenantInterFaceOutput
                {
                    accountNum = tenantCache.accountNum,
                    dotnet = tenantCache.tenantName,
                    tenantId = tenantCache.TenantId,
                    tenantName = tenantCache.tenantName,
                    validTime = tenantCache.validTime,
                    domain = tenantCache.domain,
                    type = tenantCache.type,
                    moduleIdList = tenantCache.moduleIdList,
                    urlAddressList = tenantCache.urlAddressList,
                    unitInfoJson = tenantCache.unitInfoJson,
                    userInfoJson = tenantCache.userInfoJson,
                    options = tenantCache.connectionConfig
                };
            }
        }
        var tenantInfo = await GetTenant(tenantId);
        tenantInfo.options = ChangTenant(sqlSugarClient, tenantInfo);
        return tenantInfo;
    }

    /// <summary>
    /// 获取多租户信息.
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<TenantInterFaceOutput> GetTenant(string tenantId)
    {
        var interFace = string.Format("{0}{1}", _tenant.MultiTenancyDBInterFace, tenantId);
        var response = await interFace.SetHeaders(Headers).GetAsStringAsync();
        var result = response.ToObject<RESTfulResult<TenantInterFaceInfo>>();
        if (result.code != 200) throw Oops.Oh(result.msg).WithData(result.data);
        if (result.data.db_names == null && result.data.db_names.dotnet == null && result.data.db_names.linkList == null) throw Oops.Oh(ErrorCode.D1025);
        result.data.db_names.tenantId = tenantId;
        result.data.db_names.wl_qrcode = result.data.wl_qrcode;
        return result.data.db_names;
    }

    /// <summary>
    /// 获取生成请求头部信息.
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, object> GetHeaders()
    {
        var headers = new Dictionary<string, object>();
        headers.Add("X-Forwarded-For", NetHelper.Ip);

        return headers;
    }
}
