using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Database;
using JNPF.Systems.Entitys.Dto.DataSync;
using JNPF.Systems.Entitys.Dto.System.DataSync;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据同步
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataSync", Order = 209)]
[Route("api/system/[controller]")]
public class DataSyncService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    public readonly ISqlSugarRepository<DbLinkEntity> _repository;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DataSyncService"/>类型的新实例.
    /// </summary>
    public DataSyncService(
        ISqlSugarRepository<DbLinkEntity> repository,
        IDataBaseManager dataBaseManager,
        IDbLinkService dbLinkService,
        IUserManager userManager)
    {
        _repository = repository;
        _dataBaseManager = dataBaseManager;
        _dbLinkService = dbLinkService;
        _userManager = userManager;
    }

    /// <summary>
    /// 同步判断.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<int> Estimate([FromBody] DbSyncActionsExecuteInput input)
    {
        var linkFrom = await _dbLinkService.GetInfo(input.dbConnectionFrom) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var linkTo = await _dbLinkService.GetInfo(input.dbConnectionTo) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

        if (!IsNullDataByTable(linkFrom, input.dbTable))
        {
            // 初始表有数据
            return 1;
        }
        else if (!_dataBaseManager.IsAnyTable(linkTo, input.dbTable))
        {
            // 目的表不存在
            return 2;
        }
        else if (IsNullDataByTable(linkTo, input.dbTable))
        {
            // 目的表有数据
            return 3;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// 执行同步.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("Actions/Execute")]
    [UnitOfWork]
    public async Task Execute([FromBody] DbSyncActionsExecuteInput input)
    {
        var linkFrom = await _dbLinkService.GetInfo(input.dbConnectionFrom) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var linkTo = await _dbLinkService.GetInfo(input.dbConnectionTo) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (linkFrom.DbType == linkTo.DbType)
        {
            input.convertRuleMap = null;
        }
        _dataBaseManager.SyncTable(linkFrom, linkTo, input.dbTable, input.type, input.convertRuleMap);
        if (!await ImportTableData(linkFrom, linkTo, input.dbTable))
            throw Oops.Oh(ErrorCode.COM1006);
    }

    /// <summary>
    /// 执行同步批量.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("Actions/batchExecute")]
    [UnitOfWork]
    public async Task<dynamic> batchExecute([FromBody] DbSyncActionsExecuteInput input)
    {
        var result = new Dictionary<string, object>();
        var linkFrom = await _dbLinkService.GetInfo(input.dbConnectionFrom) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var linkTo = await _dbLinkService.GetInfo(input.dbConnectionTo) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (linkFrom.DbType == linkTo.DbType)
        {
            input.convertRuleMap = null;
        }
        if (input.dbTableList.Any())
        {
            foreach (var item in input.dbTableList)
            {
                input.dbTable = item;
                var type = await Estimate(input);
                _dataBaseManager.SyncTable(linkFrom, linkTo, item, type, input.convertRuleMap);
                var flag = await ImportTableData(linkFrom, linkTo, item);
                if (flag)
                {
                    result.Add(item, 1);
                }
                else
                {
                    result.Add(item, 0);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 同步判断.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("Actions/checkDbLink")]
    public async Task<dynamic> checkDbLink([FromBody] DbSyncActionsExecuteInput input)
    {
        var linkFrom = await _dbLinkService.GetInfo(input.dbConnectionFrom) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var linkTo = await _dbLinkService.GetInfo(input.dbConnectionTo) ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (linkFrom.DbType.ToLower() == linkTo.DbType.ToLower() && linkFrom.Host == linkTo.Host && linkFrom.Port == linkTo.Port && linkFrom.UserName == linkTo.UserName && linkFrom.Password == linkTo.Password && linkFrom.ServiceName == linkTo.ServiceName)
            throw Oops.Oh(ErrorCode.D1516);
        var output = new DbSyncOutput();
        output.checkDbFlag = _dataBaseManager.IsConnection(linkFrom) && _dataBaseManager.IsConnection(linkTo);
        if (!output.checkDbFlag)
            throw Oops.Oh(ErrorCode.D1507);
        var tables = _dataBaseManager.GetDBTableList(linkFrom);
        output.tableList = tables.Adapt<List<DatabaseTableListOutput>>().OrderBy(x => x.table).ToList();
        output.convertRuleMap = GetFieldType(linkFrom.DbType, linkTo.DbType);
        return output;
    }

    #region PrivateMethod

    /// <summary>
    /// 判断表中是否有数据.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="table"></param>
    /// <returns></returns>
    private bool IsNullDataByTable(DbLinkEntity entity, string table)
    {
        var data = _dataBaseManager.GetData(entity, table);
        if (data.Rows.Count > 0)
            return true;

        return false;
    }

    /// <summary>
    /// 批量写入.
    /// </summary>
    /// <param name="linkFrom">数据库连接 From.</param>
    /// <param name="linkTo">数据库连接To.</param>
    /// <param name="table"></param>
    private async Task<bool> ImportTableData(DbLinkEntity linkFrom, DbLinkEntity linkTo, string table)
    {
        try
        {
            // 取同步数据
            var syncData = _dataBaseManager.GetData(linkFrom, table);

            // 插入同步数据
            return await _dataBaseManager.SyncData(linkTo, syncData, table);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取数据库类型.
    /// </summary>
    /// <param name="dbTypeForm"></param>
    /// <param name="dbTypeTo"></param>
    /// <returns></returns>
    private Dictionary<string, List<string>> GetFieldType(string dbTypeForm, string dbTypeTo)
    {
        var fieldTypeList = new Dictionary<string, List<string>>();
        switch (dbTypeForm.ToLower())
        {
            case "sqlserver":
                fieldTypeList.Add("nvarchar", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("int", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("datetime", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("decimal", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("bigint", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("nvarchar(max)", GetDataTypeList(5, dbTypeTo));
                break;
            case "mysql":
                fieldTypeList.Add("varchar", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("int", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("datetime", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("decimal", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("bigint", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("longtext", GetDataTypeList(5, dbTypeTo));
                break;
            case "oracle":
                fieldTypeList.Add("varchar2", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("number", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("date", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("number", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("number", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("clob", GetDataTypeList(5, dbTypeTo));
                break;
            case "dm":
                fieldTypeList.Add("varchar", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("int", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("datetime", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("decimal", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("bigint", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("text", GetDataTypeList(5, dbTypeTo));
                break;
            case "postgresql":
                fieldTypeList.Add("nvarchar", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("int4", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("timestamp", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("numeric", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("int8", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("text", GetDataTypeList(5, dbTypeTo));
                break;
            case "kingbasees":
                fieldTypeList.Add("nvarchar", GetDataTypeList(0, dbTypeTo));
                fieldTypeList.Add("integer", GetDataTypeList(1, dbTypeTo));
                fieldTypeList.Add("date", GetDataTypeList(2, dbTypeTo));
                fieldTypeList.Add("numeric", GetDataTypeList(3, dbTypeTo));
                fieldTypeList.Add("bigint", GetDataTypeList(4, dbTypeTo));
                fieldTypeList.Add("text", GetDataTypeList(5, dbTypeTo));
                break;
        }
        return fieldTypeList;
    }

    private List<string> GetDataTypeList(int type, string dbType)
    {
        var list = new List<string>();
        switch (dbType.ToLower())
        {
            case "sqlserver":
                switch (type)
                {
                    case 0:
                        list.Add("nvarchar");
                        break;
                    case 1:
                        list.Add("int");
                        break;
                    case 2:
                        list.Add("datetime");
                        break;
                    case 3:
                        list.Add("decimal");
                        break;
                    case 4:
                        list.Add("bigint");
                        break;
                    case 5:
                        list.Add("nvarchar(max)");
                        break;
                }
                break;
            case "mysql":
                switch (type)
                {
                    case 0:
                        list.Add("varchar");
                        break;
                    case 1:
                        list.Add("int");
                        break;
                    case 2:
                        list.Add("datetime");
                        break;
                    case 3:
                        list.Add("decimal");
                        break;
                    case 4:
                        list.Add("bigint");
                        break;
                    case 5:
                        list.Add("longtext");
                        break;
                }
                break;
            case "oracle":
                switch (type)
                {
                    case 0:
                        list.Add("varchar2");
                        break;
                    case 1:
                        list.Add("number");
                        break;
                    case 2:
                        list.Add("date");
                        break;
                    case 3:
                        list.Add("number");
                        break;
                    case 4:
                        list.Add("number");
                        break;
                    case 5:
                        list.Add("clob");
                        break;
                }
                break;
            case "dm":
                switch (type)
                {
                    case 0:
                        list.Add("varchar");
                        break;
                    case 1:
                        list.Add("int");
                        break;
                    case 2:
                        list.Add("datetime");
                        break;
                    case 3:
                        list.Add("decimal");
                        break;
                    case 4:
                        list.Add("bigint");
                        break;
                    case 5:
                        list.Add("text");
                        break;
                }
                break;
            case "postgresql":
                switch (type)
                {
                    case 0:
                        list.Add("nvarchar");
                        break;
                    case 1:
                        list.Add("int4");
                        break;
                    case 2:
                        list.Add("timestamp");
                        break;
                    case 3:
                        list.Add("numeric");
                        break;
                    case 4:
                        list.Add("int8");
                        break;
                    case 5:
                        list.Add("text");
                        break;
                }
                break;
            case "kingbasees":
                switch (type)
                {
                    case 0:
                        list.Add("nvarchar");
                        break;
                    case 1:
                        list.Add("integer");
                        break;
                    case 2:
                        list.Add("date");
                        break;
                    case 3:
                        list.Add("numeric");
                        break;
                    case 4:
                        list.Add("bigint");
                        break;
                    case 5:
                        list.Add("text");
                        break;
                }
                break;
        }
        return list;
    }
    #endregion
}