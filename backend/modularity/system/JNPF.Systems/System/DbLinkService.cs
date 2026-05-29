using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.DbLink;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据连接
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataSource", Order = 205)]
[Route("api/system/[controller]")]
public class DbLinkService : IDbLinkService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DbLinkEntity> _repository;

    /// <summary>
    /// 数据字典服务.
    /// </summary>
    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 初始化一个<see cref="DbLinkService"/>类型的新实例.
    /// </summary>
    public DbLinkService(
        ISqlSugarRepository<DbLinkEntity> repository,
        IDictionaryDataService dictionaryDataService,
        IDataBaseManager dataBaseManager)
    {
        _repository = repository;
        _dictionaryDataService = dictionaryDataService;
        _dataBaseManager = dataBaseManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] DbLinkListInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<DbLinkEntity, UserEntity, UserEntity>(
            (a, b, c) => new JoinQueryInfos(JoinType.Left, a.CreatorUserId == b.Id, JoinType.Left, a.LastModifyUserId == c.Id))
                .Where((a, b, c) => a.DeleteMark == null)
                .WhereIF(input.dbType.IsNotEmptyOrNull(), (a, b, c) => a.DbType == input.dbType)
                .WhereIF(input.keyword.IsNotEmptyOrNull(), (a, b, c) => a.FullName.Contains(input.keyword) || a.Host.Contains(input.keyword))
                .Select((a, b, c) => new DbLinkListOutput()
                {
                    id = a.Id,
                    creatorTime = a.CreatorTime,
                    creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                    dbType = a.DbType,
                    //enabledMark = a.EnabledMark,
                    fullName = a.FullName,
                    host = a.Host,
                    lastModifyTime = a.LastModifyTime,
                    lastModifyUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                    port = a.Port.ToString(),
                    sortCode = a.SortCode
                }).Distinct().MergeTable().OrderBy((a) => a.sortCode).OrderBy((a) => a.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<DbLinkListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 下拉框列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] string type)
    {
        var data = (await GetList()).Adapt<List<DbLinkSelectorOutput>>();

        // 数据库分类
        var dbTypeList = (await _dictionaryDataService.GetList("dbType")).FindAll(x => x.EnabledMark == 1);
        var output = new List<DbLinkSelectorOutput>();
        if (type.IsNullOrEmpty())
        {
            output.Add(new DbLinkSelectorOutput()
            {
                id = "-2",
                parentId = "0",
                fullName = "",
                num = data.FindAll(x => x.parentId == null).Count
            });
            // 获取选项
            var dbOptions = App.GetOptions<ConnectionStringsOptions>();
            var defaultConnection = dbOptions.DefaultConnectionConfig;
            var defaultDBType = defaultConnection.DbType.ToString();
            if (defaultDBType.Equals("Kdbndp"))
            {
                defaultDBType = "KingbaseES";
            }
            if (defaultDBType.Equals("Dm"))
            {
                defaultDBType = "DM";
            }
            output.Add(new DbLinkSelectorOutput()
            {
                id = "0",
                parentId = "-2",
                fullName = "默认数据库",
                dbType = defaultDBType,
                num = 1
            });
        }

        output.Add(new DbLinkSelectorOutput()
        {
            id = "-1",
            parentId = "0",
            fullName = "未分类",
            num = data.FindAll(x => x.parentId == null).Count
        });

        foreach (var item in dbTypeList)
        {
            var index = data.FindAll(x => x.dbType.Equals(item.EnCode)).Count;
            if (index > 0)
            {
                output.Add(new DbLinkSelectorOutput()
                {
                    id = item.Id,
                    fullName = item.FullName
                });
            }
        }

        return new { list = output.Union(data).ToList().ToTree() };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        var output = data.Adapt<DbLinkInfoOutput>();

        var oracleParam = data.OracleParam?.ToObject<OracleParamModel>();
        if (oracleParam != null)
        {
            output.oracleService = oracleParam.oracleService;
            output.oracleExtend = oracleParam.oracleExtend;
            output.oracleLinkType = oracleParam.oracleLinkType;
            output.oracleRole = oracleParam.oracleRole;
        }

        return output;
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await GetInfo(id);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 创建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create_Api([FromBody] DbLinkCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<DbLinkEntity>();
        entity.OracleParam = new OracleParamModel()
        {
            oracleExtend = input.oracleExtend,
            oracleRole = input.oracleRole,
            oracleLinkType = input.oracleLinkType,
            oracleService = input.oracleService
        }.ToJsonString();

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
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
    public async Task Update_Api(string id, [FromBody] DbLinkUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<DbLinkEntity>();
        entity.OracleParam = new OracleParamModel()
        {
            oracleExtend = input.oracleExtend,
            oracleRole = input.oracleRole,
            oracleLinkType = input.oracleLinkType,
            oracleService = input.oracleService
        }.ToJsonString();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 测试连接.
    /// </summary>
    /// <param name="input">实体对象.</param>
    [HttpPost("Actions/Test")]
    public void TestDbConnection([FromBody] DbLinkActionsTestInput input)
    {
        var entity = input.Adapt<DbLinkEntity>();
        entity.Id = input.id.Equals("0") ? SnowflakeIdHelper.NextId() : input.id;
        entity.OracleParam = new OracleParamModel()
        {
            oracleExtend = input.oracleExtend,
            oracleRole = input.oracleRole,
            oracleLinkType = input.oracleLinkType,
            oracleService = input.oracleService
        }.ToJsonString();

        if (!_dataBaseManager.IsConnection(entity))
            throw Oops.Oh(ErrorCode.D1507);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<DbLinkListOutput>> GetList()
    {
        return await _repository.AsSugarClient().Queryable<DbLinkEntity, UserEntity, UserEntity, DictionaryDataEntity, DictionaryTypeEntity>(
            (a, b, c, d, e) => new JoinQueryInfos(
                JoinType.Left, a.CreatorUserId == b.Id,
                JoinType.Left, a.LastModifyUserId == c.Id,
                JoinType.Left, a.DbType == d.EnCode && d.DeleteMark == null,
                JoinType.Left, d.DictionaryTypeId == e.Id && e.EnCode == "dbType"))
                .Where((a, b, c) => a.DeleteMark == null)
                .OrderBy((a, b, c) => a.SortCode).OrderBy((a, b, c) => a.CreatorTime, OrderByType.Desc).
                Select((a, b, c, d) => new DbLinkListOutput()
                {
                    id = a.Id,
                    parentId = d.Id == null ? "-1" : d.Id,
                    creatorTime = a.CreatorTime,
                    creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                    dbType = a.DbType,
                    //enabledMark = a.EnabledMark,
                    fullName = a.FullName,
                    host = a.Host,
                    lastModifyTime = a.LastModifyTime,
                    lastModifyUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                    port = SqlFunc.ToString(a.Port),
                    sortCode = a.SortCode
                }).Distinct().ToListAsync();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<DbLinkEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
    }
    #endregion
}