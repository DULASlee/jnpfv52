using JNPF.Common.Core.Manager;
using JNPF.Common.CodeGen.DataParsing;
using JNPF.Common.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;


using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using JNPF.ZxDev.Entitys;
using JNPF.Common.Helper;
using System.Data;
using JNPF.Common.Configuration;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.Text;
using JNPF.ZxDev.Entitys.Dto.Config;
using JNPF.Message.Interfaces;
using JNPF.Common.Dtos.Message;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Aop.Api.Domain;
using Attribute = System.Attribute;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using JNPF.Common.Const;
using Microsoft.Extensions.Options;
using JNPF.RemoteRequest.Extensions;

namespace JNPF.ZxDev;

/// <summary>
/// 业务实现：系统配置信息.
/// </summary>
[ApiDescriptionSettings("ZxDev", Tag = "ZxSystem", Name = "ZxSystem", Order = 200)]
[Route("api/ZxDev/[controller]")]
public class ZxSystemController : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SysConfig> _repository;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 数据接口服务.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;


    /// <summary>
    /// 通用数据解析.
    /// </summary>
    private readonly ControlParsing _controlParsing;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;


    /// <summary>
    /// 客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 初始化一个<see cref="ConfigService"/>类型的新实例.
    /// </summary>
    public ZxSystemController(
        ISqlSugarRepository<SysConfig> repository,
        IDataInterfaceService dataInterfaceService,
        IDataBaseManager dataBaseManager,
        ISqlSugarClient context,
        ICacheManager cacheManager,
        ControlParsing controlParsing,
             IOptions<TenantOptions> tenantOptions,
        IUserManager userManager)
    {
        _repository = repository;
        _dataBaseManager = dataBaseManager;
        _sqlSugarClient = (SqlSugarScope)context;
        _dataInterfaceService = dataInterfaceService;
        _cacheManager = cacheManager;
        _controlParsing = controlParsing;
        _userManager = userManager;
        _tenant = tenantOptions.Value;
    }

    /// <summary>
    /// 新建系统配置信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("ChangeSystemId")]
    public async Task ChangeSystemId(string oldId,string newId)
    {
        if (oldId == ClaimConst.MainSystemId) Oops.Oh("系统开发模式应用Id不可修改！");

        _sqlSugarClient.BeginTran();

        // 获取所有包含 f_system_id 字段的表
        var tables = _sqlSugarClient.Ado.SqlQuery<string>(@"
                SELECT DISTINCT TABLE_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE COLUMN_NAME = 'f_system_id'
            ").ToList();

        foreach (var table in tables)
        {
            var sql = $"UPDATE {table} SET f_system_id = @newId WHERE f_system_id = @oldId";
            _sqlSugarClient.Ado.ExecuteCommand(sql, new { newId, oldId });
        }

        tables = tables = _sqlSugarClient.Ado.SqlQuery<string>(@"
                SELECT DISTINCT TABLE_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE COLUMN_NAME = 'f_zx_system_id'
            ").ToList();

        foreach (var table in tables)
        {
            var sql = $"UPDATE {table} SET f_zx_system_id = @newId WHERE f_zx_system_id = @oldId";
            _sqlSugarClient.Ado.ExecuteCommand(sql, new { newId, oldId });
        }

        _sqlSugarClient.CommitTran();

    }


    [HttpPost("SubmitSystem")]
    public async Task SubmitSystem(string Id) 
    {
        var systemEntity = _sqlSugarClient.Queryable<SystemEntity>().Single(aa => aa.Id == Id);
        var appDbLink = _sqlSugarClient.Queryable<DbLinkEntity>().Single(aa => aa.DeleteMark == null);

        string connStr = JNPFTenantExtensions.ToConnectionString(new DbConnectionConfig()
        {
            DBName = appDbLink.FullName,
            DbType = SqlSugar.DbType.SqlServer,
            Host = appDbLink.Host,
            Password = appDbLink.Password,
            Port = appDbLink.Port ?? 0,
        });

        SystemAppCheckModel appCheckModel = new SystemAppCheckModel()
        {
            AccountId = _userManager.UserId,
            SystemId = Id,
            SystemName = systemEntity.FullName,
            FwDbConfig = _sqlSugarClient.CurrentConnectionConfig.ConnectionString,
            AppDbConfig = connStr,
        };


        var postUrl = _tenant.MultiTenancyDBInterFace + "socials";
        var result = (await postUrl.SetHeaders(new Dictionary<string, object> {
                        { "X-Forwarded-For", NetHelper.Ip}
                    }).SetBody(appCheckModel).PostAsStringAsync()).ToObject<Dictionary<string, string>>();

    }

}
