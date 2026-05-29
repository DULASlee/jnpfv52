using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.SensitiveDetection;
using JNPF.Systems.Entitys.Dto.PrintDev;
using JNPF.Systems.Entitys.Dto.System.PrintDev;
using JNPF.Systems.Entitys.Model.PrintDev;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;
using System.Text;

namespace JNPF.Systems;

/// <summary>
/// 打印模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "PrintDev", Order = 200)]
[Route("api/system/[controller]")]
public class PrintDevService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PrintDevEntity> _repository;

    /// <summary>
    /// 数据字典服务.
    /// </summary>
    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 脱敏词汇提供器.
    /// </summary>
    private readonly ISensitiveDetectionProvider _sensitiveDetectionProvider;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 初始化一个<see cref="PrintDevService"/>类型的新实例.
    /// </summary>
    public PrintDevService(
        ISqlSugarRepository<PrintDevEntity> repository,
        IDictionaryDataService dictionaryDataService,
        IFileManager fileManager,
        IDataBaseManager dataBaseManager,
        IUserManager userManager,
        IDbLinkService dbLinkService,
        ISensitiveDetectionProvider sensitiveDetectionProvider,
        IDataInterfaceService dataInterfaceService)
    {
        _repository = repository;
        _dictionaryDataService = dictionaryDataService;
        _dbLinkService = dbLinkService;
        _fileManager = fileManager;
        _dataBaseManager = dataBaseManager;
        _userManager = userManager;
        _sensitiveDetectionProvider = sensitiveDetectionProvider;
        _dataInterfaceService = dataInterfaceService;
    }

    #region Get

    /// <summary>
    /// 列表(分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList_Api([FromQuery] PrintDevListInput input)
    {
        string systemId = _userManager.BizSystemId;
        var list = await _repository.AsSugarClient().Queryable<PrintDevEntity, UserEntity, UserEntity, DictionaryDataEntity>((a, b, c, d) =>
         new JoinQueryInfos(JoinType.Left, b.Id == a.CreatorUserId, JoinType.Left, c.Id == a.LastModifyUserId, JoinType.Left, a.Category == d.EnCode))
            .Where((a, b, c, d) => a.DeleteMark == null && d.DeleteMark == null && d.ZxSystemId == systemId)
     
            .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a, b, c, d) => new PrintDevListOutput
            {
                category = d.FullName,
                id = a.Id,
                fullName = a.FullName,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                lastModifyTime = a.LastModifyTime,
                lastModifyUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                sortCode = a.SortCode,
                type = a.Type,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<PrintDevListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetList_Api([FromQuery] string type)
    {
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "printDev" && x.DeleteMark == null);
        var list = await _repository.AsSugarClient().Queryable<PrintDevEntity, UserEntity, UserEntity, DictionaryDataEntity>((a, b, c, d) => new JoinQueryInfos(JoinType.Left, b.Id == a.CreatorUserId, JoinType.Left, c.Id == a.LastModifyUserId, JoinType.Left, a.Category == d.EnCode))
            .Where((a, b, c, d) => a.DeleteMark == null && d.DictionaryTypeId == dictionaryTypeEntity.Id && a.EnabledMark == 1).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .WhereIF(type.IsNotEmptyOrNull(), (a) => a.Type == type.ParseToInt())
            .Select((a, b, c, d) => new PrintDevListTreeOutput
            {
                category = a.Category,
                id = a.Id,
                fullName = a.FullName,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                lastModifyTime = a.LastModifyTime,
                lastModifyUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                sortCode = a.SortCode,
                type = a.Type,
                parentId = d.Id,
            }).ToListAsync();

        // 数据库分类
        var dbTypeList = (await _dictionaryDataService.GetList("printDev")).FindAll(x => x.EnabledMark == 1);
        var result = new List<PrintDevListTreeOutput>();
        foreach (var item in dbTypeList)
        {
            var index = list.FindAll(x => x.category.Equals(item.EnCode)).Count;
            if (index > 0)
            {
                result.Add(new PrintDevListTreeOutput()
                {
                    id = item.Id,
                    parentId = "0",
                    fullName = item.FullName,
                    num = index
                });
            }
        }

        return new { list = result.OrderBy(x => x.sortCode).Union(list).ToList().ToTree() };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = (await GetInfo(id)).Adapt<PrintDevInfoOutput>();

        var interfaceData = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.Id.Equals(data.interfaceId)).FirstAsync();
        data.interfaceName = interfaceData?.FullName;
        data.fieldJson = interfaceData?.FieldJson;

        return data;
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var importModel = await GetInfo(id);
        var jsonStr = importModel.ToJsonString();
        return await _fileManager.Export(jsonStr, importModel.FullName, ExportFileType.bp);
    }

    /// <summary>
    /// 表单字段.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Fields")]
    public async Task<dynamic> GetFields([FromBody] PrintDevFieldsQuery input)
    {
        if (input.sqlTemplate.IsNotEmptyOrNull() && !(await _sensitiveDetectionProvider.VaildedAsync(input.sqlTemplate.ToUpper())))
            throw Oops.Oh(ErrorCode.xg1005);

        var link = await _dbLinkService.GetInfo(input.dbLinkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (!_dataBaseManager.IsConnection(tenantLink)) throw Oops.Oh(ErrorCode.D1507);

        var parameter = new List<SugarParameter>()
        {
            new SugarParameter("@formId", null)
        };
        var sqlList = input.sqlTemplate.ToList<PrintDevSqlModel>();
        var output = new List<PrintDevFieldModel>();
        var index = 0;
        foreach (var item in sqlList)
        {
            if (item.sql.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.COM1005);
            var dataTable = _dataBaseManager.GetSqlData(tenantLink, item.sql, false, parameter.ToArray());

            var fieldModel = new PrintDevFieldModel()
            {
                id = index == 0 ? "headTable" : "childrenDataTable" + (index - 1),
                parentId = "struct",
                fullName = index == 0 ? "主表" : "子表" + (index - 1),
            };
            output.Add(fieldModel);
            output.AddRange(GetFieldModels(dataTable, fieldModel));
            ++index;
        }
        return output.ToTree("struct");
    }

    /// <summary>
    /// 模板数据.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Data")]
    public async Task<dynamic> GetData([FromQuery] PrintDevSqlDataQuery input)
    {
        PrintDevDataOutput output = await GetPrintDevDataOutput(input.id, input.formId);

        var dic = output.printData.ToObject<Dictionary<string, string>>();
        StringBuilder sb = new StringBuilder(output.printTemplate.ToString());
        
        foreach (var key in dic.Keys)
        {
            sb = sb.Replace("{" + key + "}", dic[key]?.ToString());
        }
        output.printTemplate = sb.ToString();
        return output;
    }

    /// <summary>
    /// 模板数据.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("BatchData")]
    public async Task<dynamic> GetBatchData([FromQuery] PrintDevSqlDataQuery input)
    {
        var output = new List<PrintDevDataOutput>();
        foreach (var formId in input.formId.Split(','))
        {
            var data = await GetPrintDevDataOutput(input.id, formId);
            output.Add(data);
        }
        return output;
    }

    /// <summary>
    /// 模板列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("getListOptions")]
    public async Task<dynamic> GetListOptions([FromBody] PrintDevSqlDataQuery input)
    {
        return _repository.GetList(x => input.ids.Contains(x.Id)).Select(x => new { id = x.Id, fullName = x.FullName }).ToList();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create_Api([FromBody] PrintDevCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if (input.sqlTemplate.IsNotEmptyOrNull() && !await _sensitiveDetectionProvider.VaildedAsync(input.sqlTemplate.ToUpper()))
            throw Oops.Oh(ErrorCode.xg1005);
        var entity = input.Adapt<PrintDevEntity>();
        entity.EnabledMark = 1;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new PrintDevEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update_Api(string id, [FromBody] PrintDevUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if (input.sqlTemplate.IsNotEmptyOrNull() && !await _sensitiveDetectionProvider.VaildedAsync(input.sqlTemplate.ToUpper()))
            throw Oops.Oh(ErrorCode.xg1005);
        var entity = input.Adapt<PrintDevEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 修改状态.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState_Api(string id)
    {
        var isOk = await _repository.AsSugarClient().Updateable<BillRuleEntity>().SetColumns(it => new BillRuleEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await GetInfo(id);
        var random = new Random().NextLetterAndNumberString(5).ToLower();
        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnabledMark = 0;
        entity.EnCode += random;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
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
        if (!fileType.ToLower().Equals(ExportFileType.bp.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        PrintDevEntity? model;
        try
        {
            model = josn.ToObject<PrintDevEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (model == null || model.SqlTemplate.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.D3006);

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(model.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(model.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(model.FullName))) errorList.Add("名称");

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
                model.Id = SnowflakeIdHelper.NextId();
                model.FullName = string.Format("{0}.副本{1}", model.FullName, random);
                model.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        model.Create();
        model.EnabledMark = 0;
        model.CreatorUserId = _userManager.UserId;
        model.LastModifyTime = null;
        model.LastModifyUserId = null;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(model).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
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
    /// 信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<PrintDevEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 模板数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="formId"></param>
    /// <returns></returns>
    private async Task<PrintDevDataOutput> GetPrintDevDataOutput(string id, string formId)
    {
        var output = new PrintDevDataOutput();
        var entity = await GetInfo(id);
        if (entity == null)
            throw Oops.Oh(ErrorCode.D9010);
        var link = await _dbLinkService.GetInfo(entity.DbLinkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var parameter = new List<SugarParameter>()
        {
            new SugarParameter("@formId", formId)
        };

        var dic = new Dictionary<string, object>();
        if (entity.sourceType == 0)
        {
            var sqlList = entity.SqlTemplate.ToList<PrintDevSqlModel>();
            for (int i = 0; i < sqlList.Count; i++)
            {
                var dataTable = _dataBaseManager.GetSqlData(tenantLink, sqlList[i].sql, false, parameter.ToArray());
                if (dataTable.Rows.Count > 0)
                {
                    if (i == 0)
                    {
                        dic = DateConver(DataTableToDicList(dataTable)).FirstOrDefault();
                    }
                    else
                    {
                        dic.Add("childrenDataTable" + (i - 1), DateConver(DataTableToDicList(dataTable)));
                    }
                }
                else
                {
                    var columnsDic = new Dictionary<string, object>();
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        columnsDic.Add(dataTable.Columns[j].ColumnName, "");
                    }
                    if (i == 0)
                    {
                        dic = columnsDic;
                    }
                    else
                    {
                        dic.Add("childrenDataTable" + (i - 1), new List<Dictionary<string, object>> { columnsDic });
                    }
                }
            }
        }
        else
        {
            var parameterDic = new Dictionary<string, object>();
            parameterDic.Add("@formId", formId);
            var input = new DataInterfacePreviewInput
            {
                paramList = entity.ParameterJson.ToObject<List<DataInterfaceParameter>>(),
                systemParamter = parameterDic,
                tenantId = _userManager.TenantId,
            };
            var res = await _dataInterfaceService.GetResponseByType(entity.InterfaceId, 3, input);

            if (res.ToJsonString().Contains('['))
                dic = res.ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
            else
                dic = res.ToObject<Dictionary<string, object>>();
        }

        output.printData = dic;
        output.printTemplate = entity.PrintTemplate;
        output.pageParam = entity.PageParam;
        output.operatorRecordList = await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>()
            .Where(a => a.TaskId == formId)
            .Select(a => new PrintDevDataModel()
            {
                id = a.Id,
                handleId = a.HandleId,
                handleOpinion = a.HandleOpinion,
                handleStatus = a.HandleStatus,
                nodeCode = a.NodeCode,
                handleTime = a.HandleTime,
                nodeName = a.NodeName,
                signImg = a.SignImg,
                taskId = a.TaskId,
                operatorId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                status = a.Status,
                taskNodeId = a.TaskNodeId,
                taskOperatorId = a.TaskOperatorId,
            }).ToListAsync();
        return output;
    }

    /// <summary>
    /// 获取字段模型.
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="printDevFieldModel">父级</param>
    /// <returns></returns>
    private List<PrintDevFieldModel> GetFieldModels(DataTable dt, PrintDevFieldModel printDevFieldModel)
    {
        var models = new List<PrintDevFieldModel>();
        foreach (var item in dt.Columns)
        {
            models.Add(new PrintDevFieldModel()
            {
                id = item.ToString(),
                fullName = item.ToString(),
                parentId = printDevFieldModel.id
            });
        }

        return models;
    }

    /// <summary>
    /// DataTable转DicList.
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    private List<Dictionary<string, object>> DataTableToDicList(DataTable dt)
    {
        return dt.AsEnumerable().Select(
                row => dt.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column])).ToList();
    }

    /// <summary>
    /// 动态表单时间格式转换.
    /// </summary>
    /// <param name="diclist"></param>
    /// <returns></returns>
    private List<Dictionary<string, object>> DateConver(List<Dictionary<string, object>> diclist)
    {
        foreach (var item in diclist)
        {
            foreach (var dic in item.Keys)
            {
                if (item[dic] is DateTime)
                {
                    item[dic] = item[dic].ToString() + " ";
                }

                if (item[dic] is decimal)
                {
                    item[dic] = item[dic].ToString();
                }
            }
        }

        return diclist;
    }
    #endregion
}