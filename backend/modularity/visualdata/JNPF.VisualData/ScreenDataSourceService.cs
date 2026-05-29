using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenDataSource;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：大屏.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "db", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenDataSourceService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 客户端初始化.
    /// </summary>
    public ISqlSugarClient _sqlSugarClient;

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDBEntity> _visualDBRepository;

    /// <summary>
    /// 初始化一个<see cref="ScreenDataSourceService"/>类型的新实例.
    /// </summary>
    public ScreenDataSourceService(
        ISqlSugarRepository<VisualDBEntity> visualDBRepository,
        ISqlSugarClient context)
    {
        _visualDBRepository = visualDBRepository;
        _sqlSugarClient = context;
    }

    #region Get

    /// <summary>
    /// 分页.
    /// </summary>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<dynamic> GetList([FromQuery] ScreenDataSourceListQueryInput input)
    {
        SqlSugarPagedList<ScreenDataSourceListOutput>? data = await _visualDBRepository.AsQueryable().Where(v => v.IsDeleted == 0)
            .Select(v => new ScreenDataSourceListOutput { id = v.Id, name = v.Name, driverClass = v.DriverClass, password = v.Password, remark = v.Remark, url = v.Url, username = v.UserName, isDeleted = v.IsDeleted })
            .ToPagedListAsync(input.current, input.size);
        return new { current = data.pagination.CurrentPage, pages = data.pagination.Total / data.pagination.PageSize, records = data.list, size = data.pagination.PageSize, total = data.pagination.Total };
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetInfo(string id)
    {
        VisualDBEntity? entity = await _visualDBRepository.AsQueryable().FirstAsync(v => v.Id == id && v.IsDeleted == 0);
        return entity.Adapt<ScreenDataSourceInfoOutput>();
    }

    /// <summary>
    /// 下拉数据源.
    /// </summary>
    /// <returns></returns>
    [HttpGet("db-list")]
    public async Task<dynamic> GetDBList()
    {
        return await _visualDBRepository.AsQueryable().Select(v => new ScreenDataSourceSeleectOutput { id = v.Id, name = v.Name, driverClass = v.DriverClass }).ToListAsync();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增与修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("submit")]
    public async Task Submit([FromBody] ScreenDataSourceUpInput input)
    {
        VisualDBEntity? entity = input.Adapt<VisualDBEntity>();
        int isOk = 0;
        if (input.id == null)
            isOk = await _visualDBRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        else
            isOk = await _visualDBRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenDataSourceCrInput input)
    {
        VisualDBEntity? entity = input.Adapt<VisualDBEntity>();
        int isOk = await _visualDBRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenDataSourceUpInput input)
    {
        VisualDBEntity? entity = input.Adapt<VisualDBEntity>();
        int isOk = await _visualDBRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpPost("remove")]
    public async Task Delete(string ids)
    {
        VisualDBEntity? entity = await _visualDBRepository.AsQueryable().FirstAsync(v => v.Id == ids && v.IsDeleted == 0);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _visualDBRepository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.IsDeleted }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 数据源测试连接.
    /// </summary>
    /// <returns></returns>
    [HttpPost("db-test")]
    public dynamic Test([FromBody] ScreenDataSourceUpInput input)
    {
        if (input.id == null)
            input.id = SnowflakeIdHelper.NextId();

        _sqlSugarClient.AsTenant().AddConnection(new ConnectionConfig()
        {
            ConfigId = input.id,
            DbType = ToDbTytpe(input.driverClass),
            ConnectionString = ToConnectionString(input.driverClass, input.url, input.username, input.password),
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        _sqlSugarClient = _sqlSugarClient.AsTenant().GetConnectionScope(input.id);
        if (_sqlSugarClient.Ado.IsValidConnection())
            return Task.FromResult(true);
        else
            throw Oops.Oh(ErrorCode.D1507);
    }

    /// <summary>
    /// 动态执行SQL.
    /// </summary>
    /// <returns></returns>
    [HttpPost("dynamic-query")]
    public async Task<dynamic> Query([FromBody] ScreenDataSourceDynamicQueryInput input)
    {
        if (input.sql.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1513);
        if (!string.IsNullOrWhiteSpace(input.id))
        {
            VisualDBEntity? entity = await _visualDBRepository.AsQueryable().FirstAsync(v => v.Id == input.id && v.IsDeleted == 0);
            _sqlSugarClient.AsTenant().AddConnection(new ConnectionConfig()
            {
                ConfigId = input.id,
                DbType = ToDbTytpe(entity.DriverClass),
                ConnectionString = ToConnectionString(entity.DriverClass, entity.Url, entity.UserName, entity.Password),
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true
            });
            _sqlSugarClient = _sqlSugarClient.AsTenant().GetConnectionScope(input.id);

            try
            {
                return await _sqlSugarClient.Ado.GetDataTableAsync(input.sql);
            }
            catch (Exception ex)
            {
                throw Oops.Oh(ErrorCode.D1512, ex.Message);
            }
        }

        return Task.FromResult(true);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 转换数据库类型.
    /// </summary>
    /// <param name="dbType"></param>
    /// <returns></returns>
    private SqlSugar.DbType ToDbTytpe(string dbType)
    {
        switch (dbType)
        {
            case "org.postgresql.Driver":
                return SqlSugar.DbType.PostgreSQL;
            case "com.mysql.cj.jdbc.Driver":
                return SqlSugar.DbType.MySql;
            case "oracle.jdbc.OracleDriver":
                return SqlSugar.DbType.Oracle;
            case "com.microsoft.sqlserver.jdbc.SQLServerDriver":
                return SqlSugar.DbType.SqlServer;
            default:
                throw Oops.Oh(ErrorCode.D1505);
        }
    }

    /// <summary>
    /// 转换连接字符串.
    /// </summary>
    /// <returns></returns>
    private string ToConnectionString(string driverClass, string url, string name, string password)
    {
        switch (driverClass)
        {
            case "org.postgresql.Driver":
                return string.Format(url, name, password);
            case "com.mysql.cj.jdbc.Driver":
                return string.Format(url, name, password);
            case "oracle.jdbc.OracleDriver":
                return string.Format(url, name, password);
            case "com.microsoft.sqlserver.jdbc.SQLServerDriver":
                return string.Format(url, name, password);
            default:
                throw Oops.Oh(ErrorCode.D1505);
        }
    }

    #endregion
}
