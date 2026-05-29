using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.Dto.BillRule;
using JNPF.Systems.Entitys.Dto.System.BillRule;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text;

namespace JNPF.Systems;

/// <summary>
/// 单据规则.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "BillRule", Order = 200)]
[Route("api/system/[controller]")]
public class BillRuleService : IBillRullService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<BillRuleEntity> _repository;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="BillRuleService"/>类型的新实例.
    /// </summary>
    public BillRuleService(
        ISqlSugarRepository<BillRuleEntity> repository,
        ICacheManager cacheManager,
        IUserManager userManager,
        IFileManager fileManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] BillRuleListQueryInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<BillRuleEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.CreatorUserId))
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1)
            .WhereIF(!string.IsNullOrEmpty(input.categoryId), a => a.Category == input.categoryId)
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a, b) => new BillRuleListOutput
            {
                id = a.Id,
                lastModifyTime = a.LastModifyTime,
                enabledMark = a.EnabledMark,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                fullName = a.FullName,
                enCode = a.EnCode,
                outputNumber = a.OutputNumber,
                digit = a.Digit,
                startNumber = a.StartNumber,
                sortCode = a.SortCode,
                creatorTime = a.CreatorTime
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<BillRuleListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取单据规则列表(带分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    [OperateLog("单据规则", "查询")]
    public async Task<dynamic> GetList([FromQuery] BillRuleListQueryInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<BillRuleEntity, UserEntity, DictionaryDataEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.CreatorUserId == b.Id, JoinType.Left, a.Category == c.Id))
            .Where(a => a.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.categoryId), a => a.Category == input.categoryId)
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select((a, b, c) => new BillRuleListOutput
            {
                id = a.Id,
                lastModifyTime = a.LastModifyTime,
                enabledMark = a.EnabledMark,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                fullName = a.FullName,
                enCode = a.EnCode,
                outputNumber = a.OutputNumber,
                digit = a.Digit,
                startNumber = a.StartNumber,
                sortCode = a.SortCode,
                creatorTime = a.CreatorTime,
                category = c.FullName
            }).MergeTable()
            .OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<BillRuleListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取单据流水号（工作流程调用）.
    /// </summary>
    /// <param name="enCode">编码.</param>
    /// <returns></returns>
    [HttpGet("BillNumber/{enCode}")]
    public async Task<dynamic> GetBillNumber(string enCode)
    {
        return await GetBillNumber(enCode, true);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<BillRuleInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.FullName, ExportFileType.bb);
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    [OperateLog("单据规则", "新增")]
    public async Task Create([FromBody] BillRuleCrInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<BillRuleEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [OperateLog("单据规则", "修改")]
    public async Task Update(string id, [FromBody] BillRuleUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<BillRuleEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [OperateLog("单据规则", "删除")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        if (!string.IsNullOrEmpty(entity.OutputNumber))
            throw Oops.Oh(ErrorCode.BR0001);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 修改单据规则状态.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new BillRuleEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bb.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        BillRuleEntity? data;
        try
        {
            data = josn.ToObject<BillRuleEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (data == null) throw Oops.Oh(ErrorCode.D3006);

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(data.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(data.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(data.FullName))) errorList.Add("名称");

        if (errorList.Any())
        {
            if (type.Equals(0))
            {
                var error = string.Join("、", errorList);
                errorMsgList.Add(string.Format("{0}重复", error));
            }
            else
            {
                var random = new Random().NextLetterAndNumberString(5);
                data.Id = SnowflakeIdHelper.NextId();
                data.FullName = string.Format("{0}.副本{1}", data.FullName, random);
                data.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        data.Create();
        data.EnabledMark = 0;
        data.OutputNumber = null;
        data.CreatorUserId = _userManager.UserId;
        data.LastModifyTime = null;
        data.LastModifyUserId = null;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(data).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取流水号.
    /// </summary>
    /// <param name="enCode">流水编码.</param>
    /// <param name="isCache">是否缓存：每个用户会自动占用一个流水号，这个刷新页面也不会跳号.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<string> GetBillNumber(string enCode, bool isCache = false)
    {
        string cacheKey = string.Format("{0}{1}_{2}", CommonConst.CACHEKEYBILLRULE, _userManager.TenantId, _userManager.UserId + enCode);
        string strNumber = string.Empty;
        if (isCache)
        {
            if (!_cacheManager.Exists(cacheKey))
            {
                strNumber = await GetNumber(enCode).WaitAsync(new TimeSpan(0, 0, 5));
                await _cacheManager.SetAsync(cacheKey, strNumber, new TimeSpan(0, 3, 0));
            }
            else
            {
                strNumber = await _cacheManager.GetAsync(cacheKey);
            }
        }
        else
        {
            strNumber = await GetNumber(enCode).WaitAsync(new TimeSpan(0, 0, 5));
            _cacheManager.Set(cacheKey, strNumber, new TimeSpan(0, 3, 0));
        }

        return strNumber;
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取流水号.
    /// </summary>
    /// <param name="enCode"></param>
    /// <returns></returns>
    private async Task<string> GetNumber(string enCode)
    {
        StringBuilder strNumber = new StringBuilder();
        //copynew有错误
        var entity = await _repository.AsQueryable().FirstAsync(m => m.EnCode == enCode && m.DeleteMark == null);
        if (entity != null)
        {
            // 拼接前缀
            strNumber.Append(entity.Prefix);

            switch (entity.Type)
            {
                case 1: // 时间格式

                    // 处理隔天流水号归0
                    if (entity.OutputNumber != null)
                    {
                        var serialDate = string.Empty;
                        try
                        {
                            serialDate = entity.OutputNumber.Remove(entity.OutputNumber.Length - (int)entity.Digit - entity.Suffix.Length).Substring(entity.Prefix.Length);
                        }
                        catch (Exception)
                        {
                            entity.ThisNumber = 0;
                        }

                        var thisDate = entity.DateFormat == "no" ? string.Empty : DateTime.Now.ToString(entity.DateFormat.Replace("YYYY", "yyyy").Replace("DD", "dd").Replace("SSS", "fff"));
                        if (serialDate != thisDate)
                        {
                            entity.ThisNumber = 0;
                        }
                        else
                        {
                            entity.ThisNumber++;
                        }
                    }
                    else
                    {
                        entity.ThisNumber = 0;
                    }

                    if (entity.DateFormat != "no")
                        strNumber.Append(DateTime.Now.ToString(entity.DateFormat.Replace("YYYY", "yyyy").Replace("DD", "dd").Replace("SSS", "fff"))); // 日期格式

                    var number = int.Parse(entity.StartNumber) + entity.ThisNumber;
                    strNumber.Append(number.ToString().PadLeft((int)entity.Digit, '0')); // 流水号

                    break;
                case 2: // 随机数编号
                    var random = entity.RandomType.Equals(1) ? new Random().NextNumberString(entity.RandomDigit.Value) : new Random().NextLetterAndNumberString(entity.RandomDigit.Value);
                    strNumber.Append(random);
                    break;
                case 3: // UUID
                    strNumber.Append(Guid.NewGuid().ToString());
                    break;
            }

            // 拼接后缀
            strNumber.Append(entity.Suffix);
            entity.OutputNumber = strNumber.ToString();

            // 更新流水号
            await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        }
        else
        {
            strNumber.Append("单据规则不存在");
        }

        return strNumber.ToString();
    }

    #endregion
}