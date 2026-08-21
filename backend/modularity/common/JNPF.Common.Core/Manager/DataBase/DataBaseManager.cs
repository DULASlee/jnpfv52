using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Dtos.DataBase;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.VisualDev;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Database;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;
using System.Dynamic;
using System.Net.Http;
using System.Text;

namespace JNPF.Common.Core.Manager;

/// <summary>
/// 实现切换数据库后操作.
/// </summary>
public class DataBaseManager : IDataBaseManager, ITransient
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 作用域工厂（战役 0.1.2：替代构造注入 Scoped IUserManager，消除 Singleton
    /// 消费链 Captive Dependency 违规；用法对齐 DbJobPersistence 标准模式）.
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 默认数据库配置.
    /// </summary>
    private readonly DbConnectionConfig defaultConnectionConfig;

    /// <summary>
    /// 按需解析当前请求的 UserManager 并提取所需值（scope 内完成读取，
    /// BizSystemId 内部访问仓储，不可把实例带出 scope；
    /// 后台/无 HttpContext 场景返回 null，与原构造注入在 Singleton 链下拿到空上下文的行为等价）.
    /// </summary>
    private T? ResolveUserManagerValue<T>(Func<IUserManager, T> selector) where T : class
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetService<IUserManager>();
        return userManager == null ? null : selector(userManager);
    }

    /// <summary>
    /// 构造函数.
    /// </summary>
    public DataBaseManager(
        IOptions<SqlSugar.ConnectionStringsOptions> connectionOptions,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<TenantOptions> tenantOptions,
        ISqlSugarClient context,
        ICacheManager cacheManager)
    {
        _sqlSugarClient = (SqlSugarScope)context;
        _serviceScopeFactory = serviceScopeFactory;
        _tenant = tenantOptions.Value;
        _cacheManager = cacheManager;
        defaultConnectionConfig = connectionOptions.Value.DefaultConnectionConfig;
    }

    #region 公共

    /// <summary>
    /// 数据库切换.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <returns>切库后的SqlSugarClient.</returns>
    public SqlSugarScope ChangeDataBase(DbLinkEntity link)
    {
        if (link != null && _sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString() != link.Id)
        {
            if (_sqlSugarClient.AsTenant().IsAnyConnection(link.Id))
            {
                _sqlSugarClient.ChangeDatabase(link.Id);
            }
            else
            {
                _sqlSugarClient.AddConnection(new SqlSugar.ConnectionConfig()
                {
                    ConfigId = link.Id,
                    DbType = ToDbType(link.DbType),
                    ConnectionString = ToConnectionString(link),
                    InitKeyType = InitKeyType.Attribute,
                    IsAutoCloseConnection = true
                });

                _sqlSugarClient.Ado.CommandTimeOut = 30;

                _sqlSugarClient.Aop.OnLogExecuting = (sql, pars) =>
                {
                    if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.White;
                    if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.Blue;

                    // 在控制台输出sql语句
                    Console.WriteLine("【" + DateTime.Now + "——执行SQL】\r\n" + UtilMethods.GetSqlString(_sqlSugarClient.CurrentConnectionConfig.DbType, sql, pars) + "\r\n");
                };

                _sqlSugarClient.Aop.OnError = (ex) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    var pars = _sqlSugarClient.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));
                    Console.WriteLine("【" + DateTime.Now + "——错误SQL】\r\n" + UtilMethods.GetSqlString(_sqlSugarClient.CurrentConnectionConfig.DbType, ex.Sql, (SugarParameter[])ex.Parametres) + "\r\n");
                };

                if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle)
                {
                    _sqlSugarClient.Aop.OnExecutingChangeSql = (sql, pars) =>
                    {
                        if (pars != null)
                        {
                            foreach (var item in pars)
                            {
                                // 如果是DbTppe=string设置成OracleDbType.Nvarchar2
                                item.IsNvarchar2 = true;
                            }
                        }
                        return new KeyValuePair<string, SugarParameter[]>(sql, pars);
                    };
                }
                _sqlSugarClient.ChangeDatabase(link.Id);
            }
        }
        return _sqlSugarClient;
    }

    /// <summary>
    /// 获取租户SqlSugarClient客户端.
    /// </summary>
    /// <param name="tenantId">租户id.</param>
    /// <returns></returns>
    public ISqlSugarClient GetTenantSqlSugarClient(string tenantId, GlobalTenantCacheModel globalTenantCache = null)
    {
        var tenant = globalTenantCache ?? GetGlobalTenantCache(tenantId);
        if (!_sqlSugarClient.AsTenant().IsAnyConnection(tenantId))
        {
            _sqlSugarClient.AddConnection(new SqlSugar.ConnectionConfig()
            {
                ConfigId = tenant.TenantId,
                DbType = tenant.connectionConfig.ConfigList.FirstOrDefault<DBConnectionConfig>().dbType,
                ConnectionString = tenant.connectionConfig.ConfigList.FirstOrDefault<DBConnectionConfig>().connectionStr,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true
            });
        }
        _sqlSugarClient.ChangeDatabase(tenantId);

        if (tenant != null && !"default".Equals(tenantId) && tenant.type == 1)
        {
            _sqlSugarClient.QueryFilter.Clear();
            _sqlSugarClient.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == tenant.connectionConfig.IsolationField);
            _sqlSugarClient.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (!typeof(ITenantFilter).IsAssignableFrom(entityInfo.EntityValue.GetType()))
                {
                    return;
                }

                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    entityInfo.SetValue(tenant.connectionConfig.IsolationField);
                }
                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    entityInfo.SetValue(tenant.connectionConfig.IsolationField);
                }
                if (entityInfo.PropertyName == "TenantId" && entityInfo.OperationType == DataFilterType.DeleteByObject)
                {
                    entityInfo.SetValue(tenant.connectionConfig.IsolationField);
                }
            };
        }

        //add by harry 
        //string systemId = null;
        var bizSystemId = ResolveUserManagerValue(u => u.BizSystemId);
        if (_tenant.MultiSystem && bizSystemId.IsNotEmptyOrNull()) //tenant.MultiSystem
        {
            string systemConst = "ZxSystemId";
            string systemId = bizSystemId;
            _sqlSugarClient.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == systemId);
            _sqlSugarClient.QueryFilter.Clear<IZxSystemFilter>(); //首次移除时出现异常，故前面先添加一个
            _sqlSugarClient.QueryFilter.AddTableFilter<IZxSystemFilter>(it => it.ZxSystemId == systemId);

            _sqlSugarClient.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (!typeof(IZxSystemFilter).IsAssignableFrom(entityInfo.EntityValue.GetType()))
                {
                    return;
                }

                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    entityInfo.SetValue(systemId);
                }
                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    entityInfo.SetValue(systemId);
                }
                if (entityInfo.PropertyName == systemConst && entityInfo.OperationType == DataFilterType.DeleteByObject)
                {
                    entityInfo.SetValue(systemId);
                }
            };
        }
        //end add

        _sqlSugarClient.Ado.CommandTimeOut = 30;

        _sqlSugarClient.Aop.OnLogExecuting = (sql, pars) =>
        {
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Green;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.White;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Blue;

            // 在控制台输出sql语句
            Console.WriteLine("【" + DateTime.Now + "——执行SQL】\r\n" + UtilMethods.GetSqlString(_sqlSugarClient.CurrentConnectionConfig.DbType, sql, pars) + "\r\n");
        };

        _sqlSugarClient.Aop.OnError = (ex) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            var pars = _sqlSugarClient.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));
            Console.WriteLine("【" + DateTime.Now + "——错误SQL】\r\n" + UtilMethods.GetSqlString(_sqlSugarClient.CurrentConnectionConfig.DbType, ex.Sql, (SugarParameter[])ex.Parametres) + "\r\n");
        };

        if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle)
        {
            _sqlSugarClient.Aop.OnExecutingChangeSql = (sql, pars) =>
            {
                if (pars != null)
                {
                    foreach (var item in pars)
                    {
                        // 如果是DbTppe=string设置成OracleDbType.Nvarchar2
                        item.IsNvarchar2 = true;
                    }
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };
        }
        return _sqlSugarClient;
    }

    /// <summary>
    /// 获取多租户Link.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="tenantName">租户数据库.</param>
    /// <returns></returns>
    public DbLinkEntity GetTenantDbLink(string tenantId = "", string tenantName = "")
    {
        var entity = new DbLinkEntity
        {
            Id = tenantId.IsNullOrEmpty() ? defaultConnectionConfig.ConfigId.ToString() : tenantId,
            DbType = defaultConnectionConfig.DbType.ToString(),
            Host = defaultConnectionConfig.Host,
            Port = defaultConnectionConfig.Port,
            UserName = defaultConnectionConfig.UserName,
            Password = defaultConnectionConfig.Password
        };
        switch (entity.DbType.ToLower())
        {
            case "oracle":
            case "dm8":
            case "dm":
            case "kdbndp":
            case "kingbasees":
            case "postgresql":
                entity.ServiceName = defaultConnectionConfig.DBName;
                entity.DbSchema = tenantName.IsNullOrEmpty() ? defaultConnectionConfig.DBSchema : tenantName;
                break;
            default:
                entity.ServiceName = tenantName.IsNullOrEmpty() ? defaultConnectionConfig.DBName : tenantName;
                break;
        }
        return entity;
    }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    private GlobalTenantCacheModel GetGlobalTenantCache(string tenantId)
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        return _cacheManager.Get<List<GlobalTenantCacheModel>>(cacheKey).Find(it => it.TenantId.Equals(tenantId));
    }
    #endregion

    #region Sql

    /// <summary>
    /// 执行增删改sql.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="strSql">sql语句.</param>
    /// <param name="isCopyNew">是否CopyNew.</param>
    /// <param name="parameters">参数.</param>
    /// <returns></returns>
    public async Task<int> ExecuteSql(DbLinkEntity link, string strSql, bool isCopyNew = false, params SugarParameter[] parameters)
    {
        _sqlSugarClient = ChangeDataBase(link);

        if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle)
            strSql = strSql.TrimEnd(';');

        int flag = isCopyNew ? await _sqlSugarClient.CopyNew().Ado.ExecuteCommandAsync(strSql, parameters) : await _sqlSugarClient.Ado.ExecuteCommandAsync(strSql, parameters);

        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 执行Sql(新增、修改).
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <param name="dicList">数据.</param>
    /// <param name="primaryField">主键字段.</param>
    /// <returns></returns>
    public async Task<int> ExecuteSql(DbLinkEntity link, string table, List<Dictionary<string, object>> dicList, string primaryField = "")
    {

        _sqlSugarClient = ChangeDataBase(link);

        int flag = 0;
        if (string.IsNullOrEmpty(primaryField))
            foreach (var item in dicList) flag = await _sqlSugarClient.Insertable(item).AS(table).ExecuteCommandAsync();
        else
            foreach (var item in dicList) flag = await _sqlSugarClient.Updateable(item).AS(table).WhereColumns(primaryField).ExecuteCommandAsync();

        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 执行Sql 新增 并返回自增长Id.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <param name="dicList">数据.</param>
    /// <returns>id.</returns>
    public int ExecuteReturnIdentity(DbLinkEntity link, string table, List<Dictionary<string, object>> dicList)
    {

        _sqlSugarClient = ChangeDataBase(link);

        int id = _sqlSugarClient.Insertable(dicList).AS(table).ExecuteReturnIdentity();

        ChangeDefaultDatabase(link);
        return id;
    }

    /// <summary>
    /// 查询sql.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="strSql">sql语句.</param>
    /// <param name="isCopyNew">是否CopyNew.</param>
    /// <param name="parameters">参数.</param>
    /// <returns></returns>
    public DataTable GetSqlData(DbLinkEntity link, string strSql, bool isCopyNew = false, params SugarParameter[] parameters)
    {
        try
        {
            _sqlSugarClient = ChangeDataBase(link);

            if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle)
                strSql = strSql.Replace(";", string.Empty);

            var data = isCopyNew ? _sqlSugarClient.CopyNew().Ado.GetDataTable(strSql, parameters) : _sqlSugarClient.Ado.GetDataTable(strSql, parameters);

            ChangeDefaultDatabase(link);
            return data;
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.D1511);
        }
    }

    /// <summary>
    /// 条件动态过滤.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="strSql">sql语句.</param>
    /// <returns>条件是否成立.</returns>
    public bool WhereDynamicFilter(DbLinkEntity link, string strSql)
    {
        _sqlSugarClient = ChangeDataBase(link);
        var flag = _sqlSugarClient.Ado.SqlQuery<dynamic>(strSql).Count > 0;
        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 执行统计sql.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="strSql">sql语句.</param>
    /// <param name="isCopyNew">是否CopyNew.</param>
    /// <param name="parameters">参数.</param>
    public int GetCount(DbLinkEntity link, string strSql, bool isCopyNew = false, params SugarParameter[] parameters)
    {

        _sqlSugarClient = ChangeDataBase(link);

        if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle)
            strSql = strSql.Replace(";", string.Empty);

        var count = isCopyNew ? _sqlSugarClient.CopyNew().Ado.GetInt(strSql, parameters) : _sqlSugarClient.CopyNew().Ado.GetInt(strSql, parameters);

        ChangeDefaultDatabase(link);
        return count;
    }

    /// <summary>
    /// 使用存储过程.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="stored">存储过程名称.</param>
    /// <param name="parameters">参数.</param>
    public void UseStoredProcedure(DbLinkEntity link, string stored, List<SugarParameter> parameters)
    {

        _sqlSugarClient = ChangeDataBase(link);

        _sqlSugarClient.Ado.UseStoredProcedure().GetDataTable(stored, parameters);

        ChangeDefaultDatabase(link);
    }

    /// <summary>
    /// 获取数据表分页(SQL语句).
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="dbSql">数据SQL.</param>
    /// <param name="pageIndex">页数.</param>
    /// <param name="pageSize">条数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetDataTablePage(DbLinkEntity link, string dbSql, int pageIndex, int pageSize)
    {

        _sqlSugarClient = ChangeDataBase(link);

        RefAsync<int> totalNumber = 0;
        var list = await _sqlSugarClient.SqlQueryable<object>(dbSql).ToDataTablePageAsync(pageIndex, pageSize, totalNumber);
        var data = PageResult<dynamic>.SqlSugarPageResult(new SqlSugarPagedList<dynamic>()
        {
            list = ToDynamicList(list),
            pagination = new Pagination()
            {
                CurrentPage = pageIndex,
                PageSize = pageSize,
                Total = totalNumber
            }
        });
        ChangeDefaultDatabase(link);
        return data;
    }

    /// <summary>
    /// 获取数据表分页(实体).
    /// </summary>
    /// <typeparam name="TEntity">T.</typeparam>
    /// <param name="link">数据连接.</param>
    /// <param name="pageIndex">页数.</param>
    /// <param name="pageSize">条数.</param>
    /// <returns></returns>
    public async Task<List<TEntity>> GetDataTablePage<TEntity>(DbLinkEntity link, int pageIndex, int pageSize)
    {
        _sqlSugarClient = ChangeDataBase(link);
        var data = await _sqlSugarClient.Queryable<TEntity>().ToPageListAsync(pageIndex, pageSize);
        ChangeDefaultDatabase(link);
        return data;
    }

    /// <summary>
    /// 根据链接获取分页数据.
    /// </summary>
    /// <returns></returns>
    public PageResult<Dictionary<string, object>> GetInterFaceData(DbLinkEntity link, string strSql, VisualDevModelListQueryInput pageInput, MainBeltViceQueryModel columnDesign, List<IConditionalModel> dataPermissions, Dictionary<string, string> outColumnName = null)
    {
        _sqlSugarClient = ChangeDataBase(link);

        try
        {
            int total = 0;

            if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle) strSql = strSql.Replace(";", string.Empty);

            var sidx = new List<OrderByModel>();
            if (pageInput.sidx.IsNotEmptyOrNull())
            {
                foreach (var item in pageInput.sidx.Split(",").ToList())
                {
                    if (item[0].ToString().Equals("-"))
                    {
                        var itemName = item.Remove(0, 1);
                        sidx.Add(new OrderByModel()
                        {
                            FieldName = itemName,
                            OrderByType = OrderByType.Desc
                        });
                    }
                    else
                    {
                        sidx.Add(new OrderByModel()
                        {
                            FieldName = item,
                            OrderByType = OrderByType.Asc
                        });
                    }
                }
            }

            var dataRuleJson = new List<IConditionalModel>();
            if (pageInput.dataRuleJson.IsNotEmptyOrNull()) dataRuleJson = _sqlSugarClient.Utilities.JsonToConditionalModels(pageInput.dataRuleJson);

            var querJson = new List<IConditionalModel>();
            if (pageInput.queryJson.IsNotEmptyOrNull()) querJson = _sqlSugarClient.Utilities.JsonToConditionalModels(pageInput.queryJson);

            var superQueryJson = new List<IConditionalModel>();
            if (pageInput.superQueryJson.IsNotEmptyOrNull()) superQueryJson = _sqlSugarClient.Utilities.JsonToConditionalModels(pageInput.superQueryJson);
            // var sql = _sqlSugarClient.SqlQueryable<object>(strSql)
            // .Where(dataRuleJson).Where(querJson).Where(superQueryJson, true).Where(dataPermissions).ToSqlString();
            DataTable dt = _sqlSugarClient.SqlQueryable<object>(strSql)
                .Where(dataRuleJson).Where(querJson).Where(superQueryJson, true).Where(dataPermissions)
                .OrderBy(sidx)
                .ToDataTablePage(pageInput.currentPage, pageInput.pageSize, ref total);

            // 如果有字段别名 替换 ColumnName
            if (outColumnName != null && outColumnName.Count > 0)
            {
                var resultKey = string.Empty;
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    //modify by harry 字段名已存在则跳过，主要可能为id重复
                    string columnName = outColumnName.TryGetValue(dt.Columns[i].ColumnName.ToUpper(), out resultKey) == true ? outColumnName[dt.Columns[i].ColumnName.ToUpper()] : dt.Columns[i].ColumnName.ToUpper();
                    if (dt.Columns.Contains(columnName)) continue;
                    dt.Columns[i].ColumnName = columnName;
                }
            }

            var data = new PageResult<Dictionary<string, object>>()
            {
                pagination = new PageResult()
                {
                    currentPage = pageInput.currentPage,
                    pageSize = pageInput.pageSize,
                    total = total
                },
                list = dt.ToJsonStringOld().ToObject<List<Dictionary<string, string>>>().ToObject<List<Dictionary<string, object>>>()
            };

            ChangeDefaultDatabase(link);

            return data;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region 数据库
    /// <summary>
    /// 创建表.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableModel">表对象.</param>
    /// <param name="tableFieldList">字段对象.</param>
    /// <returns></returns>
    public bool Create(DbLinkEntity link, DbTableModel tableModel, List<DbTableFieldModel> tableFieldList)
    {

        _sqlSugarClient = ChangeDataBase(link);
        try
        {
            CreateTable(tableModel, tableFieldList);
            ChangeDefaultDatabase(link);
            return true;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 删除表.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <returns></returns>
    public bool Delete(DbLinkEntity link, string table)
    {

        _sqlSugarClient = ChangeDataBase(link);
        try
        {
            _sqlSugarClient.DbMaintenance.DropTable(table);
            ChangeDefaultDatabase(link);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 修改表.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="oldTable">原数据.</param>
    /// <param name="tableModel">表对象.</param>
    /// <param name="tableFieldList">字段对象.</param>
    /// <returns></returns>
    public bool Update(DbLinkEntity link, string oldTable, DbTableModel tableModel, List<DbTableFieldModel> tableFieldList)
    {

        _sqlSugarClient = ChangeDataBase(link);
        try
        {
            _sqlSugarClient.DbMaintenance.DropTable(oldTable);
            CreateTable(tableModel, tableFieldList);
            ChangeDefaultDatabase(link);
            return true;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// sqlsugar建表.
    /// </summary>
    /// <param name="tableModel">表.</param>
    /// <param name="tableFieldList">字段.</param>
    private void CreateTable(DbTableModel tableModel, List<DbTableFieldModel> tableFieldList)
    {
        var cloumnList = tableFieldList.Adapt<List<DbColumnInfo>>();
        DelDataLength(cloumnList);
        var isOk = _sqlSugarClient.DbMaintenance.CreateTable(tableModel.table, cloumnList);
        _sqlSugarClient.DbMaintenance.AddTableRemark(tableModel.table, tableModel.tableName);
        //mysql不需要单独添加字段注释
        if (_sqlSugarClient.CurrentConnectionConfig.DbType != SqlSugar.DbType.MySql)
        {
            foreach (var item in cloumnList)
            {
                _sqlSugarClient.DbMaintenance.AddColumnRemark(item.DbColumnName, tableModel.table, item.ColumnDescription);
            }
        }
    }

    /// <summary>
    /// sqlsugar添加表字段.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableInfo">表信息.</param>
    /// <param name="tableFieldList">表字段.</param>
    public void AddTableColumn(DbLinkEntity link, TableInfoOutput tableInfo, List<DbTableFieldModel> tableFieldList)
    {
        try
        {

            _sqlSugarClient = ChangeDataBase(link);
            var cloumnList = tableFieldList.Adapt<List<DbColumnInfo>>();
            DelDataLength(cloumnList);
            foreach (var item in cloumnList)
            {
                _sqlSugarClient.DbMaintenance.AddColumn(tableInfo.table, item);
                if (_sqlSugarClient.CurrentConnectionConfig.DbType != SqlSugar.DbType.MySql)
                    _sqlSugarClient.DbMaintenance.AddColumnRemark(item.DbColumnName, tableInfo.table, item.ColumnDescription);
            }
            _sqlSugarClient.DbMaintenance.DeleteTableRemark(tableInfo.table);
            _sqlSugarClient.DbMaintenance.AddTableRemark(tableInfo.table, tableInfo.tableName);
            if (!tableInfo.table.Equals(tableInfo.newTable))
                _sqlSugarClient.DbMaintenance.RenameTable(tableInfo.table, tableInfo.newTable);
            ChangeDefaultDatabase(link);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// sqlsugar添加表字段.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableName">表名.</param>
    /// <param name="tableFieldList">表字段.</param>
    public void AddTableColumn(DbLinkEntity link, string tableName, List<DbTableFieldModel> tableFieldList)
    {
        try
        {
            _sqlSugarClient = ChangeDataBase(link);
            var cloumnList = tableFieldList.Adapt<List<DbColumnInfo>>();
            DelDataLength(cloumnList);
            foreach (var item in cloumnList)
            {
                _sqlSugarClient.DbMaintenance.AddColumn(tableName, item);
                if (_sqlSugarClient.CurrentConnectionConfig.DbType != SqlSugar.DbType.MySql)
                    _sqlSugarClient.DbMaintenance.AddColumnRemark(item.DbColumnName, tableName, item.ColumnDescription);
            }
            ChangeDefaultDatabase(link);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 表是否存在.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <returns></returns>
    public bool IsAnyTable(DbLinkEntity link, string table)
    {
        //moidify by harry 不检查视图
        if (table.ToLower().StartsWith("sys_")) return true;

        _sqlSugarClient = ChangeDataBase(link);

        var flag = _sqlSugarClient.DbMaintenance.IsAnyTable(table, false);
        ChangeDefaultDatabase(link);

        return flag;
    }

    /// <summary>
    /// 表是否存在数据.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <returns></returns>
    public bool IsAnyData(DbLinkEntity link, string table)
    {

        _sqlSugarClient = ChangeDataBase(link);

        var flag = _sqlSugarClient.Queryable<dynamic>().AS(table).Any();
        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 表字段是否存在.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="table">表名.</param>
    /// <param name="column">表字段名.</param>
    /// <returns></returns>
    public bool IsAnyColumn(DbLinkEntity link, string table, string column)
    {

        _sqlSugarClient = ChangeDataBase(link);

        var flag = _sqlSugarClient.DbMaintenance.IsAnyColumn(table, column, false);

        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 获取表字段列表.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableName">表名.</param>
    /// <returns>TableFieldListModel.</returns>
    public List<DbTableFieldModel> GetFieldList(DbLinkEntity? link, string? tableName)
    {
        _sqlSugarClient = ChangeDataBase(link);

        var list = _sqlSugarClient.DbMaintenance.GetColumnInfosByTableName(tableName, false);

        ChangeDefaultDatabase(link);
        return list.Adapt<List<DbTableFieldModel>>();
    }

    /// <summary>
    /// 获取表数据.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableName">表名.</param>
    /// <returns></returns>
    public DataTable GetData(DbLinkEntity link, string tableName)
    {

        _sqlSugarClient = ChangeDataBase(link);
        var data = _sqlSugarClient.Queryable<dynamic>().AS(tableName).ToDataTable();
        ChangeDefaultDatabase(link);
        return data;
    }

    /// <summary>
    /// 获取表信息.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="tableName">表名.</param>
    /// <returns></returns>
    public DatabaseTableInfoOutput GetDataBaseTableInfo(DbLinkEntity link, string tableName)
    {

        _sqlSugarClient = ChangeDataBase(link);

        var data = new DatabaseTableInfoOutput()
        {
            tableInfo = _sqlSugarClient.DbMaintenance.GetTableInfoList(false).Find(m => m.Name == tableName).Adapt<TableInfoOutput>(),
            tableFieldList = _sqlSugarClient.DbMaintenance.GetColumnInfosByTableName(tableName, false).Adapt<List<TableFieldOutput>>()
        };

        data.tableFieldList = ViewDataTypeConversion(data.tableFieldList, _sqlSugarClient.CurrentConnectionConfig.DbType);

        ChangeDefaultDatabase(link);

        return data;
    }

    /// <summary>
    /// 获取数据库表信息.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <returns></returns>
    public List<DatabaseTableListOutput> GetDBTableList(DbLinkEntity link)
    {

        _sqlSugarClient = ChangeDataBase(link);

        var dbType = link.DbType;
        var sql = DBTableSql(dbType);
        var data = new List<DatabaseTableListOutput>();
        if ("postgresql".Equals(dbType.ToLower()))
        {
            data = _sqlSugarClient.Ado.SqlQuery<dynamic>(sql).Select(x => new DatabaseTableListOutput { table = x.f_table, tableName = x.f_tablename, sum = x.f_sum }).ToList();
        }
        else
        {
            var modelList = _sqlSugarClient.Ado.SqlQuery<DynamicDbTableModel>(sql).ToList();
            data = modelList.Select(x => new DatabaseTableListOutput { table = x.F_TABLE, tableName = x.F_TABLENAME, sum = x.F_SUM.ParseToInt() }).ToList();
        }
        ChangeDefaultDatabase(link);
        return data;
    }

    /// <summary>
    /// 获取数据库表信息.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="isView">视图.</param>
    /// <returns></returns>
    public List<DbTableInfo> GetTableInfos(DbLinkEntity link, bool isView = false)
    {

        _sqlSugarClient = ChangeDataBase(link);

        var data = isView ? _sqlSugarClient.DbMaintenance.GetViewInfoList(false) : _sqlSugarClient.DbMaintenance.GetTableInfoList(false);
        ChangeDefaultDatabase(link);
        return data;
    }

    /// <summary>
    /// 同步数据.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <param name="dt">数据.</param>
    /// <param name="table">表名.</param>
    /// <returns></returns>
    public async Task<bool> SyncData(DbLinkEntity link, DataTable dt, string table)
    {

        _sqlSugarClient = ChangeDataBase(link);

        List<Dictionary<string, object>> dic = _sqlSugarClient.Utilities.DataTableToDictionaryList(dt); // 5.0.23版本支持
        var isOk = await _sqlSugarClient.Insertable(dic).AS(table).ExecuteCommandAsync();
        ChangeDefaultDatabase(link);
        return isOk > 0;
    }

    /// <summary>
    /// 同步表操作.
    /// </summary>
    /// <param name="linkFrom">原数据库.</param>
    /// <param name="linkTo">目前数据库.</param>
    /// <param name="table">表名称.</param>
    /// <param name="type">操作类型.</param>
    /// <param name="fieldType">数据类型.</param>
    public void SyncTable(DbLinkEntity linkFrom, DbLinkEntity linkTo, string table, int type, Dictionary<string, string> fieldType)
    {
        try
        {
            switch (type)
            {
                case 2:
                    {
                        if (linkFrom != null)
                            _sqlSugarClient = ChangeDataBase(linkFrom);
                        var columns = _sqlSugarClient.DbMaintenance.GetColumnInfosByTableName(table, false);
                        if (linkTo != null)
                            _sqlSugarClient = ChangeDataBase(linkTo);
                        DelDataLength(columns, fieldType);
                        _sqlSugarClient.DbMaintenance.CreateTable(table, columns);
                    }
                    break;
                case 3:
                    {
                        if (linkTo != null)
                            _sqlSugarClient = ChangeDataBase(linkTo);
                        _sqlSugarClient.DbMaintenance.TruncateTable(table);
                    }
                    break;
            }
            ChangeDefaultDatabase(linkFrom);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// 测试数据库连接.
    /// </summary>
    /// <param name="link">数据连接.</param>
    /// <returns></returns>
    public bool IsConnection(DbLinkEntity link)
    {
        _sqlSugarClient = ChangeDataBase(link);
        var flag = _sqlSugarClient.Ado.IsValidConnection();
        ChangeDefaultDatabase(link);
        return flag;
    }

    /// <summary>
    /// 视图数据类型转换.
    /// </summary>
    /// <param name="fields">字段数据.</param>
    /// <param name="databaseType">数据库类型.</param>
    public List<TableFieldOutput> ViewDataTypeConversion(List<TableFieldOutput> fields, SqlSugar.DbType databaseType)
    {
        foreach (var item in fields)
        {
            item.dataType = item.dataType.ToLower();
            switch (item.dataType)
            {
                case "string":
                    {
                        item.dataType = "varchar";
                        if (item.dataLength.ParseToInt() > 2000)
                        {
                            item.dataType = "text";
                            item.dataLength = "50";
                        }
                    }

                    break;
                case "single":
                    item.dataType = "decimal";
                    break;
                case "int32":
                    item.dataType = "int";
                    break;
                case "int64":
                    item.dataType = "bigint";
                    break;
            }
        }

        return fields;
    }

    /// <summary>
    /// 转换数据库类型.
    /// </summary>
    /// <param name="dbType">数据库类型.</param>
    /// <returns></returns>
    public SqlSugar.DbType ToDbType(string dbType)
    {
        switch (dbType.ToLower())
        {
            case "sqlserver":
                return SqlSugar.DbType.SqlServer;
            case "mysql":
                return SqlSugar.DbType.MySql;
            case "oracle":
                return SqlSugar.DbType.Oracle;
            case "dm8":
            case "dm":
                return SqlSugar.DbType.Dm;
            case "kdbndp":
            case "kingbasees":
                return SqlSugar.DbType.Kdbndp;
            case "postgresql":
                return SqlSugar.DbType.PostgreSQL;
            default:
                throw Oops.Oh(ErrorCode.D1505);
        }
    }

    /// <summary>
    /// 转换连接字符串.
    /// </summary>
    /// <param name="dbLinkEntity">数据连接.</param>
    /// <returns></returns>
    public string ToConnectionString(DbLinkEntity dbLinkEntity)
    {
        switch (dbLinkEntity.DbType.ToLower())
        {
            case "sqlserver":
                return string.Format("Data Source={0},{4};Initial Catalog={1};User ID={2};Password={3};MultipleActiveResultSets=true", dbLinkEntity.Host, dbLinkEntity.ServiceName, dbLinkEntity.UserName, dbLinkEntity.Password, dbLinkEntity.Port);
            case "oracle":
                var oracleParam = dbLinkEntity.OracleParam.ToObject<OracleParamModel>();
                if (oracleParam.oracleExtend)
                {
                    return string.Format("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVER = DEDICATED)(SERVICE_NAME={2})));User Id={3};Password={4}", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), oracleParam.oracleService, dbLinkEntity.UserName, dbLinkEntity.Password);
                }
                else
                {
                    return string.Format("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVER = DEDICATED)(SERVICE_NAME=ORCL)));User Id={2};Password={3}", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), dbLinkEntity.UserName, dbLinkEntity.Password);
                }

            case "mysql":
                return string.Format("server={0};port={1};database={2};user={3};password={4};AllowLoadLocalInfile=true", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), dbLinkEntity.ServiceName, dbLinkEntity.UserName, dbLinkEntity.Password);
            case "dm8":
            case "dm":
                return string.Format("server={0};port={1};database={2};User Id={3};PWD={4}", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), dbLinkEntity.ServiceName, dbLinkEntity.UserName, dbLinkEntity.Password);
            case "kdbndp":
            case "kingbasees":
                return string.Format("server={0};port={1};database={2};UID={3};PWD={4}", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), dbLinkEntity.ServiceName, dbLinkEntity.UserName, dbLinkEntity.Password);
            case "postgresql":
                return string.Format("server={0};port={1};Database={2};User Id={3};Password={4};searchpath={5};", dbLinkEntity.Host, dbLinkEntity.Port.ToString(), dbLinkEntity.ServiceName, dbLinkEntity.UserName, dbLinkEntity.Password, dbLinkEntity.DbSchema);
            default:
                throw Oops.Oh(ErrorCode.D1505);
        }
    }

    /// <summary>
    /// 将DataTable 转换成 List<dynamic>
    /// reverse 反转：控制返回结果中是只存在 FilterField 指定的字段,还是排除.
    /// [flase 返回FilterField 指定的字段]|[true 返回结果剔除 FilterField 指定的字段]
    /// FilterField  字段过滤，FilterField 为空 忽略 reverse 参数；返回DataTable中的全部数
    /// </summary>
    /// <param name="table">DataTable</param>
    /// <param name="reverse">
    /// 反转：控制返回结果中是只存在 FilterField 指定的字段,还是排除.
    /// [flase 返回FilterField 指定的字段]|[true 返回结果剔除 FilterField 指定的字段]
    /// </param>
    /// <param name="FilterField">字段过滤，FilterField 为空 忽略 reverse 参数；返回DataTable中的全部数据</param>
    /// <returns>List<dynamic></dynamic></returns>
    public static List<dynamic> ToDynamicList(DataTable table, bool reverse = true, params string[] FilterField)
    {
        var modelList = new List<dynamic>();
        foreach (DataRow row in table.Rows)
        {
            dynamic model = new ExpandoObject();
            var dict = (IDictionary<string, object>)model;
            foreach (DataColumn column in table.Columns)
            {
                if (FilterField.Length != 0)
                {
                    if (reverse)
                    {
                        if (!FilterField.Contains(column.ColumnName))
                        {
                            dict[column.ColumnName] = row[column];
                        }
                    }
                    else
                    {
                        if (FilterField.Contains(column.ColumnName))
                        {
                            dict[column.ColumnName] = row[column];
                        }
                    }
                }
                else
                {
                    dict[column.ColumnName.ToLower()] = row[column];
                }
            }

            modelList.Add(model);
        }

        return modelList;
    }

    /// <summary>
    /// 数据库表SQL.
    /// </summary>
    /// <param name="dbType">数据库类型.</param>
    /// <returns></returns>
    private string DBTableSql(string dbType)
    {
        StringBuilder sb = new StringBuilder();
        switch (dbType.ToLower())
        {
            case "sqlserver":
                sb.Append(@"SELECT s.Name F_TABLE, Convert(nvarchar(max), tbp.value) as F_TABLENAME, b.ROWS F_SUM FROM sysobjects s LEFT JOIN sys.extended_properties as tbp ON s.id = tbp.major_id and tbp.minor_id = 0 AND ( tbp.Name = 'MS_Description' OR tbp.Name is null ) LEFT JOIN sysindexes AS b ON s.id = b.id WHERE s.xtype IN('U') AND (b.indid IN (0, 1))");
                break;
            case "oracle":
                sb.Append(@"SELECT table_name F_TABLE , (select COMMENTS from user_tab_comments where t.table_name=table_name ) as F_TABLENAME, T.NUM_ROWS F_SUM from user_tables t where table_name!='HELP' AND table_name NOT LIKE '%$%' AND table_name NOT LIKE 'LOGMNRC_%' AND table_name!='LOGMNRP_CTAS_PART_MAP' AND table_name!='LOGMNR_LOGMNR_BUILDLOG' AND table_name!='SQLPLUS_PRODUCT_PROFILE'");
                break;
            case "mysql":
                sb.Append(@"select TABLE_NAME as F_TABLE,TABLE_ROWS as F_SUM ,TABLE_COMMENT as F_TABLENAME from information_schema.tables where TABLE_SCHEMA=(select database()) AND TABLE_TYPE='BASE TABLE'");
                break;
            case "dm8":
            case "dm":
                sb.Append(@"SELECT table_name F_TABLE , (select COMMENTS from user_tab_comments where t.table_name=table_name ) as F_TABLENAME, T.NUM_ROWS F_SUM from user_tables t where table_name!='HELP' AND table_name NOT LIKE '%$%' AND table_name NOT LIKE 'LOGMNRC_%' AND table_name!='LOGMNRP_CTAS_PART_MAP' AND table_name!='LOGMNR_LOGMNR_BUILDLOG' AND table_name!='SQLPLUS_PRODUCT_PROFILE'");
                break;
            case "kdbndp":
            case "kingbasees":
                sb.Append(@"select a.relname F_TABLE,a.n_live_tup F_SUM,b.description F_TABLENAME from sys_stat_user_tables a left outer join sys_description b on a.relid = b.objoid where a.schemaname='public' and b.objsubid='0'");
                break;
            case "postgresql":
                sb.Append(@"select cast(relname as varchar) as F_TABLE,cast(reltuples as int) as F_SUM, cast(obj_description(relfilenode,'pg_class') as varchar) as F_TABLENAME from pg_class c inner join pg_namespace n on n.oid = c.relnamespace and nspname='public' inner join pg_tables z on z.tablename=c.relname where relkind = 'r' and relname not like 'pg_%' and relname not like 'sql_%' and schemaname='public' order by relname");
                break;
            default:
                throw new Exception("不支持");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 删除列长度(SqlSugar除了字符串其他不需要类型长度).
    /// </summary>
    /// <param name="dbColumnInfos"></param>
    /// <param name="dataTypeDic"></param>
    private void DelDataLength(List<DbColumnInfo> dbColumnInfos, Dictionary<string, string> dataTypeDic = null)
    {
        foreach (var item in dbColumnInfos)
        {
            if (item.IsIdentity)
            {
                if ("int".Equals(item.DataType.ToLower()) || "bigint".Equals(item.DataType.ToLower()))
                {
                    if (_sqlSugarClient.CurrentConnectionConfig.DbType.Equals(SqlSugar.DbType.Oracle) || _sqlSugarClient.CurrentConnectionConfig.DbType.Equals(SqlSugar.DbType.Kdbndp) || _sqlSugarClient.CurrentConnectionConfig.DbType.Equals(SqlSugar.DbType.PostgreSQL))
                    {
                        throw Oops.Oh(ErrorCode.D1519);
                    }
                }
                else
                {
                    throw Oops.Oh(ErrorCode.D1518);
                }
            }

            if (item.DataType.ToLower() != "varchar" && item.DataType.ToLower() != "nvarchar" && item.DataType.ToLower() != "decimal")
                item.Length = 0;
            if (item.DataType.ToLower() != "varchar" && item.DataType.ToLower() != "nvarchar" && item.DataType.ToLower() != "decimal")
                item.DecimalDigits = 0;
            if (dataTypeDic == null)
            {
                item.DataType = DataTypeConversion(item.DataType.ToLower(), _sqlSugarClient.CurrentConnectionConfig.DbType);
            }
            else
            {
                if (dataTypeDic.ContainsKey(item.DataType.ToLower()))
                {
                    item.DataType = dataTypeDic[item.DataType.ToLower().Replace("(默认)", string.Empty)];
                }
            }
        }
    }

    /// <summary>
    /// 数据库数据类型转换.
    /// </summary>
    /// <param name="dataType">数据类型.</param>
    /// <param name="databaseType">数据库类型</param>
    /// <returns></returns>
    private string DataTypeConversion(string dataType, SqlSugar.DbType databaseType)
    {
        if (databaseType.Equals(SqlSugar.DbType.Oracle))
        {
            switch (dataType)
            {
                case "text":
                case "longtext":
                    return "CLOB";
                case "decimal":
                    return "DECIMAL";
                case "datetime":
                    return "TIMESTAMP";
                case "bigint":
                    return "NUMBER";
                default:
                    return dataType.ToUpper();
            }
        }
        else if (databaseType.Equals(SqlSugar.DbType.Dm))
        {
            switch (dataType)
            {
                case "text":
                case "longtext":
                    return "CLOB";
                case "datetime":
                    return "TIMESTAMP";
                default:
                    return dataType.ToUpper();
            }
        }
        else if (databaseType.Equals(SqlSugar.DbType.Kdbndp))
        {
            switch (dataType)
            {
                case "int":
                    return "INT4";
                case "datetime":
                    return "DATE";
                case "bigint":
                    return "INT8";
                case "longtext":
                    return "TEXT";
                default:
                    return dataType.ToUpper();
            }
        }
        else if (databaseType.Equals(SqlSugar.DbType.PostgreSQL))
        {
            switch (dataType)
            {
                case "varchar":
                    return "varchar";
                case "int":
                    return "INT4";
                case "datetime":
                    return "timestamp";
                case "decimal":
                    return "DECIMAL";
                case "bigint":
                    return "INT8";
                case "text":
                case "longtext":
                    return "TEXT";
                default:
                    return dataType;
            }
        }
        else if (databaseType.Equals(SqlSugar.DbType.SqlServer))
        {
            switch (dataType)
            {
                case "longtext":
                    return "nvarchar(max)";
                default:
                    return dataType;
            }
        }
        else
        {
            return dataType;
        }
    }

    /// <summary>
    /// 切换默认库.
    /// </summary>
    /// <param name="linkEntity"></param>
    private void ChangeDefaultDatabase(DbLinkEntity linkEntity)
    {
        if (KeyVariable.MultiTenancy)
        {
            var userTenantId = ResolveUserManagerValue(u => u.TenantId);
            if (userTenantId.IsNotEmptyOrNull())
            {
                _sqlSugarClient.ChangeDatabase(userTenantId);
            }
            else
            {
                _sqlSugarClient.ChangeDatabase(linkEntity.Id);
            }
        }
        else
        {
            _sqlSugarClient.ChangeDatabase(defaultConnectionConfig.ConfigId);
        }
    }
    #endregion
}