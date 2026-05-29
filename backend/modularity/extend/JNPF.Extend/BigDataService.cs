using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.BigData;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 大数据测试
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "BigData", Order = 600)]
[Route("api/extend/[controller]")]
public class BigDataService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<BigDataEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="BigDataService"/>类型的新实例.
    /// </summary>
    public BigDataService(ISqlSugarRepository<BigDataEntity> repository)
    {
        _repository = repository;
    }

    #region GET

    /// <summary>
    /// 列表
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var queryWhere = LinqExpression.And<BigDataEntity>();
        if (!string.IsNullOrEmpty(input.keyword))
            queryWhere = queryWhere.And(m => m.FullName.Contains(input.keyword) || m.EnCode.Contains(input.keyword));
        var list = await _repository.AsQueryable().Where(queryWhere).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<BigDataListOutput>()
        {
            list = list.list.Adapt<List<BigDataListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<BigDataListOutput>.SqlSugarPageResult(pageList);
    }
    #endregion

    #region POST

    /// <summary>
    /// 新建
    /// </summary>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create()
    {
        var list = await _repository.GetListAsync();
        var code = 0;
        if (list.Count > 0)
        {
            code = list.Select(x => x.EnCode).ToList().Max().ParseToInt();
        }
        var index = code == 0 ? 10000001 : code;
        if (index > 11500001)
            throw Oops.Oh(ErrorCode.Ex0001);
        List<BigDataEntity> entityList = new List<BigDataEntity>();
        for (int i = 0; i < 10000; i++)
        {
            entityList.Add(new BigDataEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                EnCode = index.ToString(),
                FullName = "测试大数据" + index,
                CreatorTime = DateTime.Now,
            });
            index++;
        }
        Blukcopy(entityList);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 大数据批量插入.
    /// </summary>
    /// <param name="entityList"></param>
    private void Blukcopy(List<BigDataEntity> entityList)
    {
        try
        {
            var storageable = _repository.AsSugarClient().Storageable(entityList).SplitInsert(x => true).ToStorage();
            switch (_repository.AsSugarClient().CurrentConnectionConfig.DbType)
            {
                case DbType.Dm:
                case DbType.Kdbndp:
                    storageable.AsInsertable.ExecuteCommand();
                    break;
                case DbType.Oracle:
                    _repository.AsSugarClient().Storageable(entityList).ToStorage().BulkCopy();
                    break;
                default:
                    _repository.AsSugarClient().Fastest<BigDataEntity>().BulkCopy(entityList);
                    break;
            }
        }
        catch (Exception)
        {
            throw;
        }

    }

    #endregion
}