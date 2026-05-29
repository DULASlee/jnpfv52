using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.DictionaryData;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 字典数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DictionaryData", Order = 203)]
[Route("api/system/[controller]")]
public class DictionaryDataService : IDictionaryDataService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DictionaryDataEntity> _repository;

    /// <summary>
    /// 字典类型服务.
    /// </summary>
    private readonly IDictionaryTypeService _dictionaryTypeService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化一个<see cref="DictionaryDataService"/>类型的新实例.
    /// </summary>
    public DictionaryDataService(
        ISqlSugarRepository<DictionaryDataEntity> repository,
        IDictionaryTypeService dictionaryTypeService,
        IFileManager fileManager,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager)
    {
        _repository = repository;
        _dictionaryTypeService = dictionaryTypeService;
        _fileManager = fileManager;
        _userManager = userManager;
        _tenant = tenantOptions.Value;
    }

    #region GET

    /// <summary>
    /// 获取数据字典列表.
    /// </summary>
    /// <param name="dictionaryTypeId">分类id.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{dictionaryTypeId}")]
    public async Task<dynamic> GetList_Api(string dictionaryTypeId, [FromQuery] DictionaryDataListQuery input)
    {
        var data = new List<DictionaryDataEntity>();
        if (!dictionaryTypeId.Equals("0") && !dictionaryTypeId.Equals("1") && !dictionaryTypeId.Equals("2") && !dictionaryTypeId.Equals("3"))
        {
            var entity = await _dictionaryTypeService.GetInfo(dictionaryTypeId);
            var query = _repository.AsQueryable();

            if (entity.ZxDataType == (int)ZxDataTypeEnum.TenantSystem || entity.ZxDataType == (int)ZxDataTypeEnum.Tenant)
            {
                if (_userManager.TenantId == "default")
                {
                    query = query.Where(aa => aa.TenantId == _userManager.TenantId || aa.TenantId == null);
                }
                else
                {
                    query = query.Where(aa => aa.TenantId == _userManager.TenantId);
                }
            }

            if (entity.ZxDataType == (int)ZxDataTypeEnum.TenantSystem || entity.ZxDataType == (int)ZxDataTypeEnum.System)
            {
                query = query.Where(aa => aa.ZxSystemId == _userManager.BizSystemId);
            }

            data = await query
                .Where(d => d.DictionaryTypeId == entity.Id && d.DeleteMark == null)
                .OrderBy(o => o.SortCode)
                .OrderBy(o => o.CreatorTime, OrderByType.Desc)
                .OrderByIF(input.keyword.IsNotEmptyOrNull(), o => o.LastModifyTime, OrderByType.Desc)
                .ToListAsync();
        }

        if ("1".Equals(input.isTree))
        {
            var treeList = data.Adapt<List<DictionaryDataTreeOutput>>();
            if (!string.IsNullOrEmpty(input.keyword))
                treeList = treeList.TreeWhere(t => t.enCode.Contains(input.keyword) || t.fullName.Contains(input.keyword), t => t.id, t => t.parentId);
            return new { list = treeList.ToTree() };
        }
        else
        {
            if (!string.IsNullOrEmpty(input.keyword))
                data = data.FindAll(t => t.EnCode.Contains(input.keyword) || t.FullName.Contains(input.keyword));
            var list = data.Adapt<List<DictionaryDataNotTreeOutput>>();
            return new { list = list };
        }
    }

    /// <summary>
    /// 获取所有数据字典列表(分类+内容).
    /// </summary>
    /// <returns></returns>
    [HttpGet("All")]
    public async Task<dynamic> GetListAll()
    {
        var dictionaryData = await _repository.AsQueryable().Where(d => d.DeleteMark == null && d.EnabledMark == 1)
            .OrderBy(o => o.SortCode).OrderBy(o => o.CreatorTime, OrderByType.Desc).OrderBy(o => o.LastModifyTime, OrderByType.Desc).ToListAsync();
        var dictionaryType = await _dictionaryTypeService.GetList();
        var data = dictionaryType.Adapt<List<DictionaryDataAllListOutput>>();
        data.ForEach(dataall =>
        {
            if (dataall.isTree == 1)
                dataall.dictionaryList = dictionaryData.FindAll(d => d.DictionaryTypeId == dataall.id).Adapt<List<DictionaryDataTreeOutput>>().ToTree();
            else
                dataall.dictionaryList = dictionaryData.FindAll(d => d.DictionaryTypeId == dataall.id).Adapt<List<DictionaryDataListOutput>>();
        });
        return new { list = data };

    }

    /// <summary>
    /// 获取字典分类下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{dictionaryTypeId}/Selector/{id}")]
    public async Task<dynamic> GetSelector(string dictionaryTypeId, string id, string isTree)
    {
        var output = new List<DictionaryDataSelectorOutput>();
        var typeEntity = await _dictionaryTypeService.GetInfo(dictionaryTypeId);

        // 顶级节点
        var dataEntity = typeEntity.Adapt<DictionaryDataSelectorOutput>();
        dataEntity.id = "0";
        dataEntity.parentId = "-1";
        output.Add(dataEntity);
        if ("1".Equals(isTree))
        {
            var dataList = (await GetList(dictionaryTypeId, false)).Adapt<List<DictionaryDataSelectorOutput>>();
            if (!id.Equals("0"))
                dataList.RemoveAll(x => x.id == id);
            output = output.Union(dataList).ToList();
            return new { list = output.ToTree("-1") };
        }

        return new { list = output };
    }

    /// <summary>
    /// 获取字典数据下拉框列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{dictionaryTypeId}/Data/Selector")]
    public async Task<dynamic> GetDataSelector(string dictionaryTypeId)
    {
        try
        {
            var isTree = (await _dictionaryTypeService.GetInfo(dictionaryTypeId)).IsTree;
            var datalist = await GetList(dictionaryTypeId);
            if (isTree == 1)
            {
                var treeList = datalist.Adapt<List<DictionaryDataSelectorDataOutput>>();
                var typeEntity = await _dictionaryTypeService.GetInfo(dictionaryTypeId);

                // 顶级节点
                var dataEntity = typeEntity.Adapt<DictionaryDataSelectorDataOutput>();
                dataEntity.id = "0";
                dataEntity.parentId = "-1";
                treeList.Add(dataEntity);
                treeList = treeList.ToTree();

                return new { list = treeList };
            }
            else
            {
                var treeList = datalist.Adapt<List<DictionaryDataSelectorDataNotTreeOutput>>();
                return new { list = treeList };
            }
        }
        catch (Exception)
        {
            return new { list = new List<object>() };
        }
    }

    /// <summary>
    /// 获取数据字典信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}/Info")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        return data.Adapt<DictionaryDataInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var output = new DictionaryDataExportInput();
        await _dictionaryTypeService.GetListAllById(id, output.list);
        foreach (var item in output.list)
        {
            var modelList = await GetList(item.Id, false);
            output.modelList = output.modelList.Union(modelList).ToList();
        }

        var jsonStr = output.ToJsonString();
        return await _fileManager.Export(jsonStr, (await _dictionaryTypeService.GetInfo(id)).FullName, ExportFileType.bdd);
    }

    #endregion

    #region Post

    /// <summary>
    /// 添加.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Creater([FromBody] DictionaryDataCrInput input)
    {
        var typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().First(aa => aa.Id == input.dictionaryTypeId);

        var entity = input.Adapt<DictionaryDataEntity>();
        entity.TenantId = typeEntity.TenantId;
        entity.ZxSystemId = typeEntity.ZxSystemId;
        entity.ZxDataType = typeEntity.ZxDataType;

        await CheckDataAsync(typeEntity, input);

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);

    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DictionaryDataUpInput input)
    {
        var typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().First(aa => aa.Id == input.dictionaryTypeId);

        var entity = input.Adapt<DictionaryDataEntity>();
        entity.TenantId = typeEntity.TenantId;
        entity.ZxSystemId = typeEntity.ZxSystemId;
        entity.ZxDataType = typeEntity.ZxDataType;

        await CheckDataAsync(typeEntity, input);

        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    private async Task CheckDataAsync(DictionaryTypeEntity typeEntity, DictionaryDataCrInput input) 
    {
        ZxDataTypeEnum type = (ZxDataTypeEnum)typeEntity.ZxDataType;
        if (type ==  ZxDataTypeEnum.TenantSystem)
        {
            if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == _userManager.TenantId && x.ZxSystemId == _userManager.BizSystemId)
                || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == _userManager.TenantId && x.ZxSystemId == _userManager.BizSystemId))
                throw Oops.Oh(ErrorCode.D3003);
        }
        else if (type == ZxDataTypeEnum.Tenant)
        {
            if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == _userManager.TenantId)
                || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == _userManager.TenantId))
                throw Oops.Oh(ErrorCode.D3003);

        }
        else if (type ==  ZxDataTypeEnum.System)
        {
            if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == null && x.ZxSystemId == _userManager.BizSystemId)
                || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == null && x.ZxSystemId == _userManager.BizSystemId))
                throw Oops.Oh(ErrorCode.D3003);

        }
        else if (type ==  ZxDataTypeEnum.Framework)
        {
            if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == null && x.ZxSystemId == null)
                || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DictionaryTypeId == input.dictionaryTypeId && x.DeleteMark == null && x.TenantId == null && x.ZxSystemId == null))
                throw Oops.Oh(ErrorCode.D3003);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3004);
        if (await _repository.IsAnyAsync(o => o.ParentId.Equals(id) && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3002);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new DictionaryDataEntity()
        {
            DeleteTime = DateTime.Now,
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId
        }).Where(x => x.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 更新字典状态.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new DictionaryDataEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 0, 1, 0),
            LastModifyTime = DateTime.Now,
            LastModifyUserId = _userManager.UserId
        }).Where(x => x.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.D1506);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    [UnitOfWork]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bdd.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        DictionaryDataExportInput? model;
        try
        {
            model = josn.ToObject<DictionaryDataExportInput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (model == null || model.list.Count == 0)
            throw Oops.Oh(ErrorCode.D3006);
        if (model.list.Find(x => x.ParentId == "-1") == null && !_dictionaryTypeService.IsExistParent(model.list))
            throw Oops.Oh(ErrorCode.D3007);

        var errorMsgList = new List<string>();
        await ImportData(model, type, errorMsgList);
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join("；", errorMsgList));
    }

    #endregion

    #region PulicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="dictionaryTypeId">类别主键.</param>
    /// <param name="enabledMark">是否过滤启用状态.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<DictionaryDataEntity>> GetList(string dictionaryTypeId, bool enabledMark = true)
    {
        if (dictionaryTypeId.Equals("0") || dictionaryTypeId.Equals("1"))
        {
            return new List<DictionaryDataEntity>();
        }
        else
        {
            var entity = await _dictionaryTypeService.GetInfo(dictionaryTypeId);
            return await _repository.AsQueryable().Where(d => d.DictionaryTypeId == entity.Id && d.DeleteMark == null).WhereIF(enabledMark, d => d.EnabledMark == 1)
                .OrderBy(o => o.SortCode).OrderBy(o => o.CreatorTime, OrderByType.Desc).OrderBy(o => o.LastModifyTime, OrderByType.Desc).ToListAsync();
        }
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<DictionaryDataEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="inputList"></param>
    /// <param name="type"></param>
    /// <param name="errorMsgList"></param>
    /// <returns></returns>
    private async Task ImportData(DictionaryDataExportInput inputList, int type, List<string> errorMsgList)
    {
        try
        {
            // 替换新id
            var idDic = new Dictionary<string, string>();

            var typeDic = new Dictionary<string, string>();
            foreach (var item in inputList.list)
            {
                var mainIsExist = false;
                var random = new Random().NextLetterAndNumberString(5);
                item.Create();
                item.CreatorUserId = _userManager.UserId;
                item.LastModifyTime = null;
                item.LastModifyUserId = null;
                if (await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                {
                    if (typeDic.ContainsKey("ID"))
                        typeDic["ID"] = string.Format("{0}、{1}", typeDic["ID"], item.Id);
                    else
                        typeDic.Add("ID", item.Id);
                    mainIsExist = true;
                }
                if (await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(item.EnCode)))
                {
                    if (typeDic.ContainsKey("编码"))
                        typeDic["编码"] = string.Format("{0}、{1}", typeDic["编码"], item.EnCode);
                    else
                        typeDic.Add("编码", item.EnCode);
                    mainIsExist = true;
                }
                if (await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(item.FullName)))
                {
                    if (typeDic.ContainsKey("名称"))
                        typeDic["名称"] = string.Format("{0}、{1}", typeDic["名称"], item.FullName);
                    else
                        typeDic.Add("名称", item.FullName);
                    mainIsExist = true;
                }

                if (!mainIsExist)
                {
                    var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                    await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                    await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                }
                else if (mainIsExist && type.Equals(1))
                {
                    var id = SnowflakeIdHelper.NextId();
                    idDic[item.Id] = id;
                    item.Id = id;
                    if (idDic.ContainsKey(item.ParentId)) item.ParentId = idDic[item.ParentId];
                    item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                    item.EnCode += random;
                    await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                }
            }

            // 组装主表提示语
            if (type.Equals(0) && typeDic.Any())
            {
                var typeMsg = new List<string>();
                foreach (var item in typeDic)
                    typeMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                errorMsgList.Add(string.Format("{0}重复", string.Join("、", typeMsg)));
            }

            var dataDic = new Dictionary<string, string>();
            foreach (var item in inputList.modelList)
            {
                var isExist = false; // 是否出现重复
                var random = new Random().NextLetterAndNumberString(5);
                item.Create();
                item.CreatorUserId = _userManager.UserId;
                item.LastModifyTime = null;
                item.LastModifyUserId = null;
                if (idDic.ContainsKey(item.DictionaryTypeId)) item.DictionaryTypeId = idDic[item.DictionaryTypeId];

                if (type.Equals(0))
                {
                    if (await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                    {
                        if (dataDic.ContainsKey("ID"))
                            dataDic["ID"] = string.Format("{0}、{1}", dataDic["ID"], item.Id);
                        else
                            dataDic.Add("ID", item.Id);
                        isExist = true;
                    }
                }
                else
                {
                    item.Id = SnowflakeIdHelper.NextId();
                }
                if (await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().AnyAsync(it => it.DeleteMark == null && it.DictionaryTypeId.Equals(item.DictionaryTypeId) && it.EnCode.Equals(item.EnCode)))
                {
                    if (dataDic.ContainsKey("编码"))
                        dataDic["编码"] = string.Format("{0}、{1}", dataDic["编码"], item.EnCode);
                    else
                        dataDic.Add("编码", item.EnCode);
                    isExist = true;
                }
                if (await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().AnyAsync(it => it.DeleteMark == null && it.DictionaryTypeId.Equals(item.DictionaryTypeId) && it.FullName.Equals(item.FullName)))
                {
                    if (dataDic.ContainsKey("名称"))
                        dataDic["名称"] = string.Format("{0}、{1}", dataDic["名称"], item.FullName);
                    else
                        dataDic.Add("名称", item.FullName);
                    isExist = true;
                }

                if (!isExist) // 子表不重复
                {
                    var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                    await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                    await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                }
                else if (isExist && type.Equals(1)) // 追加时，子表重复
                {
                    item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                    item.EnCode += random;
                    await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                }
            }

            // 组装子表提示语
            if (type.Equals(0) && dataDic.Any())
            {
                var dataMsg = new List<string>();
                foreach (var item in dataDic)
                    dataMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                errorMsgList.Add(string.Format("modelList：{0}重复", string.Join("、", dataMsg)));
            }
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    #endregion
}