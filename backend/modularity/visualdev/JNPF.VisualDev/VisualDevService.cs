using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Manager;
using JNPF.Extensions;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Engine;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Release;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Query;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发基础 .
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Base", Order = 171)]
[Route("api/visualdev/[controller]")]
public class VisualDevService : IVisualDevService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _visualDevRepository;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _changeDataBase;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 在线开发运行服务.
    /// </summary>
    private readonly RunService _runService;

    /// <summary>
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 初始化一个<see cref="VisualDevService"/>类型的新实例.
    /// </summary>
    public VisualDevService(
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        IDataBaseManager changeDataBase,
        IUserManager userManager,
        IOptions<TenantOptions> tenantOptions,
        RunService runService,
        ISqlSugarClient context,
        ICacheManager cacheManager)
    {
        _visualDevRepository = visualDevRepository;
        _userManager = userManager;
        _runService = runService;
        _tenant = tenantOptions.Value;
        _changeDataBase = changeDataBase;
        _db = context.AsTenant();
        _cacheManager = cacheManager;
    }

    #region Get

    /// <summary>
    /// 获取功能列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("List")]
    public async Task<dynamic> GetIntegrateList([FromQuery] VisualDevListQueryInput input)
    {
        SqlSugarPagedList<VisualDevIntergrateListOutput>? data = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>()
           .WhereIF(!string.IsNullOrEmpty(input.keyword), it => it.FullName.Contains(input.keyword))
           .WhereIF(input.enableFlow != null, it => it.EnableFlow.Equals(input.enableFlow))
           .WhereIF(!string.IsNullOrEmpty(input.category), it => it.Category == input.category)
           .Where(it => it.DeleteMark == null && it.Type == 1 && it.WebType == 2)
           .OrderBy(it => it.SortCode, OrderByType.Asc)
           .OrderBy(it => it.CreatorTime, OrderByType.Desc)
           .OrderByIF(!input.keyword.IsNullOrEmpty(), it => it.LastModifyTime, OrderByType.Desc)
           .Select(it => new VisualDevIntergrateListOutput
           {
               id = it.Id,
               fullName = it.FullName,
               enCode = it.EnCode,
               enableFlow = it.EnableFlow,
           }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<VisualDevIntergrateListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取功能列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] VisualDevListQueryInput input)
    {
        // ── 引用表缓存加载（5 分钟 TTL，避免每次分页请求都全表扫描） ──
        // 注意：SqlSugar ISqlSugarClient 非线程安全，禁止并发查询（Task.Run / Task.WhenAll）。
        var tenantId = _userManager.TenantId ?? "default";
        var refClient = _visualDevRepository.AsSugarClient();
        var dict = await GetOrSetCacheAsync($"VisualDev_RefDict_{tenantId}",
            () => refClient.Queryable<DictionaryDataEntity>().Where(d => d.DeleteMark == null).ToListAsync());
        var users = await GetOrSetCacheAsync($"VisualDev_RefUsers_{tenantId}",
            () => refClient.Queryable<UserEntity>().Where(u => u.DeleteMark == null).ToListAsync());
        var systems = await GetOrSetCacheAsync($"VisualDev_RefSystems_{tenantId}",
            () => refClient.Queryable<SystemEntity>().Where(s => s.DeleteMark == null).ToListAsync());
        var modules = await GetOrSetCacheAsync($"VisualDev_RefModules_{tenantId}",
            () => refClient.Queryable<ModuleEntity>().Where(m => m.DeleteMark == null).ToListAsync());

        // ── 主查询：分页列表（不含子查询，单次 SQL 完成） ──
        SqlSugarPagedList<VisualDevListOutput>? data = await _visualDevRepository.AsSugarClient().Queryable<VisualDevEntity>()
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .WhereIF(!string.IsNullOrEmpty(input.category), a => a.Category == input.category)
            .WhereIF(!string.IsNullOrEmpty(input.parentId), a => a.ParentId == input.parentId && a.DeleteMark != null && a.Type == input.type)
            .WhereIF(string.IsNullOrEmpty(input.parentId), a => a.DeleteMark == null && a.Type == input.type)
            .WhereIF(input.isRelease.IsNotEmptyOrNull(), a => a.State == input.isRelease)
            .WhereIF(input.webType.Equals(1), a => a.EnableFlow.Equals(0) && !a.WebType.Equals(4))
            .WhereIF(input.webType.Equals(2), a => a.EnableFlow.Equals(1))
            .WhereIF(input.webType.Equals(4), a => a.WebType.Equals(4))
            .WhereIF(input.enableFlow.IsNotEmptyOrNull(), a => a.EnableFlow.Equals(input.enableFlow))
            .Where(a => a.DeleteMark == null && a.Type == input.type)
            .OrderBy(a => a.SortCode, OrderByType.Asc)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new VisualDevListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                state = a.State,
                type = a.Type,
                webType = a.WebType,
                tables = a.Tables,
                description = a.Description,
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                deleteMark = a.DeleteMark,
                sortCode = a.SortCode,
                parentId = a.Category,
                isRelease = a.State,
                enableFlow = a.EnableFlow,
                creatorUserId = a.CreatorUserId,
                lastModifyUserId = a.LastModifyUserId,
                platformRelease = a.PlatformRelease
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        // ── 内存拼接引用数据（O(1) 字典查找替代 O(n) 子查询） ──

        // 构建字典/用户查找映射
        var dictMap = dict.ToDictionary(d => d.Id, d => d.FullName);
        var userMap = users.ToDictionary(u => u.Id, u => $"{u.RealName}/{u.Account}");

        // 构建 module→路径名 查找（一次性递归预计算）
        var sysDict = systems.ToDictionary(s => s.Id, s => s.FullName);
        var modDict = modules.ToDictionary(m => m.Id);
        var modPathCache = new Dictionary<string, string>();
        string BuildModulePath(ModuleEntity m)
        {
            if (modPathCache.TryGetValue(m.Id, out var cached)) return cached;
            if (m.ParentId == "-1")
            {
                var sysName = sysDict.TryGetValue(m.SystemId ?? string.Empty, out var sn) ? sn : string.Empty;
                var path = string.IsNullOrEmpty(sysName) ? m.FullName : $"{sysName}/{m.FullName}";
                modPathCache[m.Id] = path;
                return path;
            }
            if (modDict.TryGetValue(m.ParentId ?? string.Empty, out var parent))
            {
                var parentPath = BuildModulePath(parent);
                var path = $"{parentPath}/{m.FullName}";
                modPathCache[m.Id] = path;
                return path;
            }
            modPathCache[m.Id] = m.FullName;
            return m.FullName;
        }
        // 预计算所有模块路径
        foreach (var mod in modules)
            BuildModulePath(mod);

        // 构建 module→visualDevIds 倒排索引（O(模块 JSON 字节数) 替代 O(模块数 × 行数) 的 Contains 扫描）
        var pcModules = modules.Where(m => m.Category == "Web").ToList();
        var appModules = modules.Where(m => m.Category == "App").ToList();
        string? ResolveModulePath(string moduleId) =>
            modPathCache.TryGetValue(moduleId, out var cachedPath) ? cachedPath : null;

        var dataList = data.list.ToList();
        var pcRefMap = VisualDevListRefIndex.BuildRefIndex(pcModules, dataList, ResolveModulePath);
        var appRefMap = VisualDevListRefIndex.BuildRefIndex(appModules, dataList, ResolveModulePath);

        // ── 填充每一行的引用数据 ──
        foreach (var item in data.list)
        {
            item.category = dictMap.TryGetValue(item.parentId ?? string.Empty, out var cat) ? cat : string.Empty;
            item.creatorUser = userMap.TryGetValue(item.creatorUserId ?? string.Empty, out var cu) ? cu : string.Empty;
            item.lastModifyUser = userMap.TryGetValue(item.lastModifyUserId ?? string.Empty, out var lu) ? lu : string.Empty;

            if (pcRefMap.TryGetValue(item.id, out var pcPaths) && pcPaths.Count > 0)
            {
                item.pcReleaseName = string.Join("；", pcPaths);
                item.pcIsRelease = 1;
            }
            if (appRefMap.TryGetValue(item.id, out var appPaths) && appPaths.Count > 0)
            {
                item.appReleaseName = string.Join("；", appPaths);
                item.appIsRelease = 1;
            }
        }

        return PageResult<VisualDevListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取功能列表下拉框.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] VisualDevSelectorInput input)
    {
        var webType = input.webType.IsNotEmptyOrNull() ? input.webType.Split(',').ToObject<List<int>>() : new List<int>();
        var output = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>()
            .Where(v => v.Type == input.type && v.DeleteMark == null)
            .WhereIF(webType.Any(), v => webType.Contains(v.WebType))
            .OrderBy(a => a.Category).OrderBy(a => a.SortCode)
            .Select(it => new VisualDevSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                SortCode = it.SortCode,
                parentId = it.Category
            }).ToListAsync();
        IEnumerable<string>? parentIds = output.Select(x => x.parentId).ToList().Distinct();
        List<VisualDevSelectorOutput>? pList = new List<VisualDevSelectorOutput>();
        List<DictionaryDataEntity>? parentData = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => parentIds.Contains(d.Id) && d.DeleteMark == null).OrderBy(x => x.SortCode).ToListAsync();
        foreach (DictionaryDataEntity? item in parentData)
        {
            VisualDevSelectorOutput? pData = item.Adapt<VisualDevSelectorOutput>();
            pData.parentId = "-1";
            pList.Add(pData);
        }

        return new { list = output.Union(pList).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        VisualDevEntity? data = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        var output = data.Adapt<VisualDevInfoOutput>();
        output.interfaceName = await _visualDevRepository.AsSugarClient().Queryable<DataInterfaceEntity>()
            .Where(it => it.DeleteMark == null && it.Id.Equals(output.interfaceId))
            .Select(it => it.FullName)
            .FirstAsync();
        return output;
    }

    /// <summary>
    /// 获取表单主表属性下拉框.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="filterType">1：过滤指定控件.</param>
    /// <returns></returns>
    [HttpGet("{id}/FormDataFields")]
    public async Task<dynamic> GetFormDataFields(string id, [FromQuery] int filterType)
    {
        var templateEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        TemplateParsingBase? tInfo = new TemplateParsingBase(templateEntity.Adapt<VisualDevEntity>()); // 解析模板
        //List<FieldsModel>? fieldsModels = tInfo.SingleFormData.FindAll(x => x.__vModel__.IsNotEmptyOrNull() && !JnpfKeyConst.RELATIONFORM.Equals(x.__config__.jnpfKey));
        //modify by harry //不需要过虑弹出类型，需要支持弹出框之间的数据传递
        List<FieldsModel>? fieldsModels = tInfo.SingleFormData.FindAll(x => x.__vModel__.IsNotEmptyOrNull());
        if (filterType.Equals(1))
        {
            fieldsModels = fieldsModels.FindAll(x => !JnpfKeyConst.UPLOADIMG.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.UPLOADFZ.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.MODIFYUSER.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.MODIFYTIME.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.LINK.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.BUTTON.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.ALERT.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.JNPFTEXT.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.BARCODE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.QRCODE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.TABLE.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.CREATEUSER.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CREATETIME.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.POPUPSELECT.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CURRORGANIZE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CURRPOSITION.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.IFRAME.Equals(x.__config__.jnpfKey)  );
            //modify by harry //不需要过虑弹出类型，需要支持弹出框之间的数据传递
            //fieldsModels = fieldsModels.Where(x => !JnpfKeyConst.POPUPSELECT.Equals(x.__config__.jnpfKey));
        }

        List<VisualDevFormDataFieldsOutput>? output = fieldsModels.Select(x => new VisualDevFormDataFieldsOutput()
        {
            label = x.__config__.label,
            vmodel = x.__vModel__
        }).ToList();
        return new { list = output };
    }

    /// <summary>
    /// 获取表单主表属性列表.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("{id}/FieldDataSelect")]
    public async Task<dynamic> GetFieldDataSelect(string id, [FromQuery] VisualDevDataFieldDataListInput input)
    {
        Dictionary<string, object> queryDic = new Dictionary<string, object>();

        if (input.relationField.IsNullOrEmpty() && input.columnOptions.IsNotEmptyOrNull()) input.relationField = input.columnOptions.Split(',').First();
        if (!string.IsNullOrWhiteSpace(input.columnOptions) && !string.IsNullOrWhiteSpace(input.keyword))
        {
            foreach (var item in input.columnOptions.Split(','))
            {
                queryDic.Add(item, input.keyword);
            }
        }
        VisualDevEntity? templateEntity = await GetInfoById(id, true); // 取数据
        TemplateParsingBase? tInfo = new TemplateParsingBase(templateEntity); // 解析模板

        //modify by harry 官方没有切换数据库异常
        tInfo.DbLink = await _runService.GetDbLink(templateEntity.DbLinkId);

        // 指定查询字段
        if (input.IsNotEmptyOrNull() && input.columnOptions.IsNotEmptyOrNull())
        {
            List<string>? showFieldList = input.columnOptions.Split(',').ToList(); // 显示的所有 字段
            //List<FieldsModel>? flist = new List<FieldsModel>();
            List<IndexGridFieldModel>? clist = new List<IndexGridFieldModel>();

            // 获取 调用 该功能表单 的功能模板
            FieldsModel? smodel = tInfo.FieldsModelList.Where(x => x.__vModel__ == input.relationField).First();
            smodel.searchType = 2;
            //flist.Add(smodel); // 添加 关联查询字段
            if (tInfo.ColumnData == null)
            {
                tInfo.ColumnData = new ColumnDesignModel()
                {
                    columnList = new List<IndexGridFieldModel>() { new IndexGridFieldModel() { prop = input.relationField, label = input.relationField } },
                    searchList = new List<IndexSearchFieldModel>() { smodel.Adapt<IndexSearchFieldModel>() }
                };
            }

            if (!tInfo.ColumnData.columnList.Where(x => x.prop == input.relationField).Any())
                tInfo.ColumnData.columnList.Add(new IndexGridFieldModel() { prop = input.relationField, label = input.relationField });
            //if (tInfo.ColumnData.defaultSidx.IsNotEmptyOrNull() && tInfo.FieldsModelList.Any(x => x.__vModel__ == tInfo.ColumnData?.defaultSidx))
            //    flist.Add(tInfo.FieldsModelList.Where(x => x.__vModel__ == tInfo.ColumnData?.defaultSidx).FirstOrDefault()); // 添加 关联排序字段

            //tInfo.FieldsModelList.ForEach(item =>
            //{
            //    if (showFieldList.Find(x => x == item.__vModel__) != null) flist.Add(item);
            //});
            clist.Add(tInfo.ColumnData.columnList.Where(x => x.prop == input.relationField).FirstOrDefault()); // 添加 关联查询字段
            if (tInfo.ColumnData.defaultSidx.IsNotEmptyOrNull() && tInfo.FieldsModelList.Any(x => x.__vModel__ == tInfo.ColumnData?.defaultSidx))
                clist.Add(tInfo.ColumnData.columnList.Where(x => x.prop == tInfo.ColumnData?.defaultSidx).FirstOrDefault()); // 添加 关联排序字段
            showFieldList.ForEach(item =>
            {
                if (!tInfo.ColumnData.columnList.Where(x => x.prop == item).Any())
                    clist.Add(new IndexGridFieldModel() { prop = item, label = item });
                else
                    clist.Add(tInfo.ColumnData.columnList.Find(x => x.prop == item));
            });

            //if (flist.Count > 0)
            //{
            //    tInfo.FormModel.fields = flist.Distinct().ToList();
            //    templateEntity.FormData = tInfo.FormModel.ToJsonString();
            //}

            if (clist.Count > 0)
            {
                tInfo.ColumnData.columnList = clist.Distinct().ToList();
                templateEntity.ColumnData = tInfo.ColumnData.ToJsonString();
            }
			
						//modify by harry 关键词查询支持
            if (input.keyword.IsNotEmptyOrNull())
            {
                queryDic.Clear();
                foreach (var item in clist)
                {
                    if (!queryDic.ContainsKey(item.__vModel__))
                        queryDic.Add(item.__vModel__, input.keyword);
                }
            }
        }

        // 获取值 无分页
        VisualDevModelListQueryInput listQueryInput = new VisualDevModelListQueryInput
        {
            queryJson = queryDic.ToJsonString(),
            currentPage = input.currentPage > 0 ? input.currentPage : 1,
            pageSize = input.pageSize > 0 ? input.pageSize : 20,
            dataType = "1",
            sidx = tInfo.ColumnData.defaultSidx,
            sort = tInfo.ColumnData.sort
        };

        //modify by harry 关键词
        listQueryInput.keyword = input.keyword;

        return await _runService.GetRelationFormList(templateEntity, listQueryInput, "List");
    }

    /// <summary>
    /// 回滚模板.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/RollbackTemplate")]
    public async Task RollbackTemplate(string id)
    {
        var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(id) && x.DeleteMark == null);
        if (vREntity == null) throw Oops.Oh(ErrorCode.D1415);
        VisualDevEntity? entity = vREntity.Adapt<VisualDevEntity>();
        entity.State = 1;
        await _visualDevRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建功能信息.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] VisualDevCrInput input)
    {
        VisualDevEntity? entity = input.Adapt<VisualDevEntity>();
        try
        {
            // 验证名称和编码是否重复
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Type == input.type && (x.FullName == input.fullName || x.EnCode == input.enCode))) throw Oops.Oh(ErrorCode.D1406);

            if (input.formData.IsNotEmptyOrNull())
            {
                TemplateParsingBase? tInfo = new TemplateParsingBase(entity); // 解析模板
                if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
                await VerifyPrimaryKeyPolicy(tInfo, entity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持
            }
            _db.BeginTran(); // 开启事务
            entity.State = 0;

            // 添加功能
            entity = await _visualDevRepository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

            // 同步流程相关
            if (entity.EnableFlow.Equals(1) && (!entity.Type.Equals(3) && !entity.Type.Equals(4)))
            {
                var fEntity = entity.Adapt<FlowFormEntity>();
                fEntity.PropertyJson = entity.FormData;
                fEntity.TableJson = entity.Tables;
                fEntity.DraftJson = fEntity.ToJsonString();
                fEntity.FlowType = 1;
                fEntity.FormType = 2;
                fEntity.FlowId = entity.Id;
                await _visualDevRepository.AsSugarClient().Insertable(fEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                await SaveFlowTemplate(entity);
            }

            await SyncField(entity);
            _db.CommitTran(); // 提交事务
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 设置主版本.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/MainVersion")]
    public async Task MainVersion(string id)
    {
        var entity=  await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id.Equals(id));
        if(!entity.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.D1416); // 已发布的模板  表不能为空.
        var parentEntity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id.Equals(entity.ParentId));
        if (!parentEntity.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.D1416); // 已发布的模板  表不能为空.
        parentEntity.FormData = entity.FormData;
        parentEntity.Tables = entity.Tables;
        parentEntity.ColumnData = entity.ColumnData;
        parentEntity.FullName = entity.FullName;
        parentEntity.EnCode = entity.EnCode;
        parentEntity.State = 2;//已修改
        await _visualDevRepository.AsSugarClient().Updateable(parentEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 流程表单 修改接口.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <param name="input">参数</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] VisualDevUpInput input)
    {
        VisualDevEntity? entity = input.Adapt<VisualDevEntity>();
        try
        {
            if (!input.webType.Equals(4) && await _visualDevRepository.AsQueryable().AnyAsync(x => x.Id.Equals(id) && x.State.Equals(1)) && (entity.Tables.IsNullOrEmpty() || entity.Tables.Equals("[]")))
                throw Oops.Oh(ErrorCode.D1416); // 已发布的模板  表不能为空.

            // 验证名称和编码是否重复
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Id != entity.Id && x.Type == input.type && (x.FullName == input.fullName || x.EnCode == input.enCode))) throw Oops.Oh(ErrorCode.D1406);

            var state = await _visualDevRepository.AsQueryable()
                .Where(it => it.DeleteMark == null && it.Id.Equals(id))
                .Select(it => it.State).FirstAsync();
            if (state == 1)
                entity.State = 2;

            if (input.formData.IsNotEmptyOrNull())
            {
                TemplateParsingBase? tInfo = new TemplateParsingBase(entity); // 解析模板
                if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
                await VerifyPrimaryKeyPolicy(tInfo, entity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持
            }

            _db.BeginTran(); // 开启事务

            //add by xudi 备份当前设置
            if (false)
            {
                var copyObj = JsonSerialization.JSON.Deserialize<VisualDevEntity>(JsonSerialization.JSON.Serialize(entity));
                copyObj.ParentId = entity.Id;
                copyObj.DeleteMark = 1;
                copyObj.Id = null;
                await _visualDevRepository.AsSugarClient().Insertable<VisualDevEntity>(copyObj).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
            //end 

            // 修改功能
            await _visualDevRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

            // 同步流程相关
            if (entity.EnableFlow.Equals(1) && (!entity.Type.Equals(3) && !entity.Type.Equals(4)))
            {
                var fEntity = await _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().FirstAsync(x => x.Id.Equals(id));
                if (fEntity != null)
                {
                    // EnabledMark=0 未发布，EnabledMark=1 已发布
                    if (fEntity.EnabledMark.Equals(0))
                    {
                        fEntity.FullName = entity.FullName;
                        fEntity.EnCode = entity.EnCode;
                        fEntity.TableJson = entity.Tables;
                        fEntity.PropertyJson = entity.FormData;
                        fEntity.DraftJson = fEntity.ToJsonString();
                    }
                    else
                    {
                        var dEntity = fEntity.Copy();
                        dEntity.TableJson = entity.Tables;
                        dEntity.PropertyJson = entity.FormData;
                        fEntity = new FlowFormEntity();
                        fEntity.DraftJson = dEntity.ToJsonString();
                        fEntity.Id = id;
                    }
                    fEntity.FlowType = 1;
                    fEntity.FormType = 2;
                    fEntity.FlowId = id;

                    await _visualDevRepository.AsSugarClient().Updateable(fEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                    await SaveFlowTemplate(entity);
                }
                else
                {
                    fEntity = entity.Adapt<FlowFormEntity>();
                    fEntity.PropertyJson = entity.FormData;
                    fEntity.TableJson = entity.Tables;
                    fEntity.DraftJson = fEntity.ToJsonString();
                    fEntity.FlowType = 1;
                    fEntity.FormType = 2;
                    fEntity.FlowId = id;
                    await _visualDevRepository.AsSugarClient().Insertable(fEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                    await SaveFlowTemplate(entity);
                }
            }

            await SyncField(entity);
            _db.CommitTran(); // 关闭事务
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 删除接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        await _visualDevRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

        // 同步删除线上版本
        var rEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (rEntity != null) await _visualDevRepository.AsSugarClient().Updateable(rEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

        // 同步删除流程表单
        var fEntity = await _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (fEntity != null)
        {
            await _visualDevRepository.AsSugarClient().Deleteable<FlowFormEntity>().Where(x => x.Id.Equals(fEntity.Id)).ExecuteCommandAsync();
            await _visualDevRepository.AsSugarClient().Deleteable<FlowFormRelationEntity>().Where(x => x.FormId.Equals(fEntity.FlowId)).ExecuteCommandAsync();

            // 功能流程存在发起的流程，流程设计不应该删除.
            if (!await _visualDevRepository.AsSugarClient().Queryable<FlowTaskEntity>().AnyAsync(ft => ft.TemplateId.Equals(fEntity.Id) && ft.DeleteMark == null))
            {
                await _visualDevRepository.AsSugarClient().Deleteable<FlowTemplateEntity>().Where(it => it.Id.Equals(fEntity.Id)).ExecuteCommandAsync();
                await _visualDevRepository.AsSugarClient().Deleteable<FlowTemplateJsonEntity>().Where(it => it.TemplateId.Equals(fEntity.Id)).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        string? random = new Random().NextLetterAndNumberString(5);
        VisualDevEntity? entity = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (entity.State.Equals(1) && (!entity.Type.Equals(3) && !entity.Type.Equals(4)))
        {
            var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
            entity = vREntity.Adapt<VisualDevEntity>();
            entity.State = 0;
        }

        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnCode += random;
        entity.State = 0;
        entity.Id = null; // 复制的数据需要把Id清空，否则会主键冲突错误
        entity.LastModifyUserId = null;
        entity.LastModifyTime = null;

        try
        {
            entity = await _visualDevRepository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteReturnEntityAsync();
        }
        catch
        {
            if (entity.FullName.Length >= 100 || entity.EnCode.Length >= 50) throw Oops.Oh(ErrorCode.D1403); // 数据长度超过 字段设定长度
            else throw;
        }

        // 同步流程相关
        if (entity.EnableFlow.Equals(1) && (!entity.Type.Equals(3) && !entity.Type.Equals(4)))
        {
            var fEntity = entity.Adapt<FlowFormEntity>();
            fEntity.PropertyJson = entity.FormData;
            fEntity.TableJson = entity.Tables;
            fEntity.DraftJson = fEntity.ToJsonString();
            fEntity.FlowType = 1;
            fEntity.FormType = 2;
            fEntity.FlowId = entity.Id;
            await _visualDevRepository.AsSugarClient().Insertable(fEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
            await SaveFlowTemplate(entity);
        }
    }

    /// <summary>
    /// 功能同步菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Release")]
    public async Task FuncToMenu(string id, [FromBody] VisualDevToMenuInput input)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id);
        var tInfo = new TemplateParsingBase(entity); // 解析模板

        if (entity.FormData.IsNullOrEmpty() && !entity.WebType.Equals(4)) throw Oops.Oh(ErrorCode.COM1013);
        if ((entity.WebType.Equals(2) || entity.WebType.Equals(4)) && entity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1014);

        var sysIdList = await _visualDevRepository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).Select(it => it.Id).ToListAsync();
        var moduleList = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId)).ToListAsync();

        var allModuleList = moduleList.Where(it => it.PropertyJson.Contains(id)).ToList();
        if (input.pc == 1)
        {
            if (!input.pcModuleParentId.Any() && !await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("Web") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);
        }
        if (input.app == 1)
        {
            if (!input.appModuleParentId.Any() && !await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("App") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);
        }

        var release = FuncToMenuReleasePlanner.BuildReleaseTargets(
            publishPc: input.pc == 1,
            publishApp: input.app == 1,
            pcModuleParentIds: input.pcModuleParentId,
            appModuleParentIds: input.appModuleParentId,
            sysIdList: sysIdList,
            modulesLinkedToFeature: allModuleList,
            allModules: moduleList);

        // 无表转有表
        if (!tInfo.IsHasTable && !entity.WebType.Equals(4))
        {
            string? mTableName = "mt" + entity.Id; // 主表名称
            VisualDevEntity? res = await NoTblToTable(entity, mTableName);
            if (res != null)
                await _visualDevRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            else
                throw Oops.Oh(ErrorCode.D1414);
            tInfo = new TemplateParsingBase(res); // 解析模板
            entity = res;
        }

        try
        {
            _db.BeginTran(); // 开启事务

            foreach (var platform in release)
            {
                foreach (var item in platform.Value)
                {
                    var data = item.First();

                    #region 旧的菜单、权限数据

                    var oldWebModule = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Category == "Web" && x.DeleteMark == null && x.PropertyJson.Contains(id) && x.SystemId.Equals(data.Key) && x.ParentId.Equals(data.Value));
                    var oldWebModuleButtonEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldWebModuleColumnEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldWebModuleFormEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();

                    var oldAppModule = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Category == "App" && x.DeleteMark == null && x.PropertyJson.Contains(id) && x.SystemId.Equals(data.Key) && x.ParentId.Equals(data.Value));
                    var oldAppModuleButtonEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldAppModuleColumnEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldAppModuleFormEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();

                    #endregion

                    var fullName = entity.FullName;
                    var enCode = entity.EnCode + new Random().NextLetterAndNumberString(5);

                    if ((platform.Key == "Web" && (input.pcModuleParentId.Contains(data.Value) || (data.Value.Equals("-1") && input.pcModuleParentId.Contains(data.Key))))
                        || (platform.Key == "App" && (input.appModuleParentId.Contains(data.Value) || (data.Value.Equals("-1") && input.appModuleParentId.Contains(data.Key)))))
                    {
                        if (_visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Any(x => x.FullName == fullName && x.ParentId == data.Value && x.SystemId == data.Key && x.Category == platform.Key && x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.COM1032);
                        if (_visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Any(x => x.EnCode == enCode && x.Category == platform.Key && x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.COM1031);
                    }

                    var moduleModel = new ModuleEntity();

                    // 数据视图
                    if (entity.WebType.Equals(4))
                    {
                        #region 菜单组装

                        moduleModel.Id = oldWebModule != null ? oldWebModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ModuleId = id;
                        moduleModel.ParentId = oldWebModule != null ? oldWebModule.ParentId : data.Value; // 父级菜单节点
                        moduleModel.Category = "Web";
                        moduleModel.FullName = oldWebModule != null ? oldWebModule.FullName : fullName;
                        moduleModel.EnCode = oldWebModule != null ? oldWebModule.EnCode : enCode;
                        moduleModel.Icon = oldWebModule != null ? oldWebModule.Icon : "icon-ym icon-ym-webForm";
                        moduleModel.UrlAddress = oldWebModule != null ? oldWebModule.UrlAddress : "model/" + moduleModel.EnCode;
                        moduleModel.Type = 3;
                        moduleModel.EnabledMark = 1;
                        moduleModel.IsColumnAuthorize = 1;
                        moduleModel.IsButtonAuthorize = 1;
                        moduleModel.IsFormAuthorize = 1;
                        moduleModel.IsDataAuthorize = 1;
                        moduleModel.SortCode = oldWebModule != null ? oldWebModule.SortCode : 999;
                        moduleModel.CreatorTime = DateTime.Now;
                        moduleModel.PropertyJson = (new { moduleId = id, iconBackgroundColor = string.Empty, isTree = 0 }).ToJsonString();
                        moduleModel.SystemId = oldWebModule != null ? oldWebModule.SystemId : data.Key;

                        #endregion

                        // 添加PC菜单
                        if (platform.Key == "Web")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                        }

                        // 添加App菜单
                        if (platform.Key == "App")
                        {
                            #region App菜单
                            moduleModel.Id = oldAppModule != null ? oldAppModule.Id : SnowflakeIdHelper.NextId();
                            moduleModel.ParentId = oldAppModule != null ? oldAppModule.ParentId : data.Value; // 父级菜单节点
                            moduleModel.Category = "App";
                            moduleModel.FullName = oldAppModule != null ? oldAppModule.FullName : fullName;
                            moduleModel.EnCode = oldAppModule != null ? oldAppModule.EnCode : enCode;
                            moduleModel.UrlAddress = oldAppModule != null ? oldAppModule.UrlAddress : "/pages/apply/dynamicModel/index?id=" + moduleModel.EnCode;
                            moduleModel.SystemId = oldAppModule != null ? oldAppModule.SystemId : data.Key;
                            moduleModel.SortCode = oldAppModule != null ? oldAppModule.SortCode : 999;
                            moduleModel.Icon = oldAppModule != null ? oldAppModule.Icon : "icon-ym icon-ym-webForm";

                            #endregion

                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                        }
                    }
                    else
                    {
                        ColumnDesignModel? columnData = new ColumnDesignModel();

                        // 列配置模型
                        if (!string.IsNullOrWhiteSpace(entity.ColumnData))
                        {
                            columnData = entity.ColumnData.ToObject<ColumnDesignModel>();

                            //add by harry 无列表时以下控件为null会有些异常
                            if (columnData.btnsList == null) columnData.btnsList = new List<ButtonConfigModel>();
                            if (columnData.columnBtnsList == null) columnData.columnBtnsList = new List<ButtonConfigModel>();
                            if (columnData.customBtnsList == null) columnData.customBtnsList = new List<ButtonConfigModel>();
                            if (columnData.columnList == null) columnData.columnList = new List<IndexGridFieldModel>();
                            if (columnData.defaultColumnList == null) columnData.defaultColumnList = new List<IndexGridFieldModel>();

                        }
                        else
                        {
                            columnData = new ColumnDesignModel()
                            {
                                btnsList = new List<ButtonConfigModel>(),
                                columnBtnsList = new List<ButtonConfigModel>(),
                                customBtnsList = new List<ButtonConfigModel>(),
                                columnList = new List<IndexGridFieldModel>(),
                                defaultColumnList = new List<IndexGridFieldModel>()
                            };
                        }

                        columnData.btnsList = columnData.btnsList?.Union(columnData.columnBtnsList).ToList();
                        if (columnData.customBtnsList != null && columnData.customBtnsList.Any()) columnData.btnsList = columnData.btnsList.Union(columnData.customBtnsList).ToList();

                        ColumnDesignModel? appColumnData = new ColumnDesignModel();

                        // App列配置模型
                        if (!string.IsNullOrWhiteSpace(entity.AppColumnData))
                        {
                            appColumnData = tInfo.AppColumnData;
                        }
                        else
                        {
                            appColumnData = new ColumnDesignModel()
                            {
                                btnsList = new List<ButtonConfigModel>(),
                                columnBtnsList = new List<ButtonConfigModel>(),
                                customBtnsList = new List<ButtonConfigModel>(),
                                columnList = new List<IndexGridFieldModel>(),
                                defaultColumnList = new List<IndexGridFieldModel>()
                            };
                        }

                        appColumnData.btnsList = appColumnData.btnsList.Union(appColumnData.columnBtnsList).ToList();
                        if (appColumnData.customBtnsList != null && appColumnData.customBtnsList.Any()) appColumnData.btnsList = appColumnData.btnsList.Union(appColumnData.customBtnsList).ToList();

                        #region 菜单组装
                        moduleModel.Id = oldWebModule != null ? oldWebModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ModuleId = id;
                        moduleModel.ParentId = oldWebModule != null ? oldWebModule.ParentId : data.Value; // 父级菜单节点
                        moduleModel.Category = "Web";
                        moduleModel.FullName = oldWebModule != null ? oldWebModule.FullName : fullName;
                        moduleModel.EnCode = oldWebModule != null ? oldWebModule.EnCode : enCode;
                        moduleModel.Icon = oldWebModule != null ? oldWebModule.Icon : "icon-ym icon-ym-webForm";
                        moduleModel.UrlAddress = oldWebModule != null ? oldWebModule.UrlAddress : "model/" + moduleModel.EnCode;
                        moduleModel.Type = 3;
                        moduleModel.EnabledMark = 1;
                        moduleModel.IsColumnAuthorize = 1;
                        moduleModel.IsButtonAuthorize = 1;
                        moduleModel.IsFormAuthorize = 1;
                        moduleModel.IsDataAuthorize = 1;
                        moduleModel.SortCode = oldWebModule != null ? oldWebModule.SortCode : 999;
                        moduleModel.CreatorTime = DateTime.Now;
                        moduleModel.PropertyJson = (new { moduleId = id, iconBackgroundColor = string.Empty, isTree = 0 }).ToJsonString();
                        moduleModel.SystemId = oldWebModule != null ? oldWebModule.SystemId : data.Key;
                        #endregion

                        #region 配置权限

                        // 按钮权限
                        var btnAuth = new List<ModuleButtonEntity>();
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_add", FullName = "新增", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_download", FullName = "导出", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_upload", FullName = "导入", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_batchRemove", FullName = "批量删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_edit", FullName = "编辑", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_remove", FullName = "删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_detail", FullName = "详情", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_batchPrint", FullName = "批量打印", ModuleId = moduleModel.Id });
                        columnData.customBtnsList?.ForEach(item =>
                        {
                            btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 1, SortCode = 0, ParentId = "-1", EnCode = item.value, FullName = item.label, ModuleId = moduleModel.Id });
                        });

                        columnData.btnsList?.ForEach(item =>
                        {
                            var aut = btnAuth.Find(x => x.EnCode == "btn_" + item.value);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        // 表单权限
                        var columnAuth = new List<ModuleColumnEntity>();
                        var fieldList = tInfo.AllFieldsModel;
                        var formAuth = new List<ModuleFormEntity>();

                        var ctList = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList();
                        var childTableIndex = new Dictionary<string, string>();
                        for (var i = 0; i < ctList.Count; i++) childTableIndex.Add(ctList[i].__vModel__, ctList[i].__config__.label + (i + 1));

                        fieldList = fieldList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList();
                        fieldList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item =>
                        {
                            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
                            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;
                            var ctName = item.__vModel__.Split("-");
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                                ChildTableKey = fRule.Equals(2) ? ctName.FirstOrDefault() : string.Empty,
                                FieldRule = fRule,
                                ModuleId = moduleModel.Id,
                                FullName = fRule.Equals(2) ? childTableIndex[item.__vModel__.Split('-').First()] + "-" + item.__config__.label : item.__config__.label,
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });
                        ctList.ForEach(item =>
                        {

                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = tInfo.MainTableName,
                                ChildTableKey = item.__vModel__,
                                FieldRule = 0,
                                ModuleId = moduleModel.Id,
                                FullName = childTableIndex[item.__vModel__],
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });

                        // 列表权限
                        columnData.defaultColumnList.ForEach(item =>
                        {
                            var itemModel = fieldList.FirstOrDefault(x => x.__config__.jnpfKey == item.__config__.jnpfKey && x.__vModel__ == item.prop);
                            if (itemModel != null)
                            {
                                var fRule = itemModel.__vModel__.Contains("_jnpf_") ? 1 : 0;
                                fRule = itemModel.__vModel__.ToLower().Contains("tablefield") && itemModel.__vModel__.Contains("-") ? 2 : fRule;
                                var ctName = item.__vModel__.Split("-");
                                columnAuth.Add(new ModuleColumnEntity()
                                {
                                    ParentId = "-1",
                                    EnCode = itemModel.__vModel__,
                                    BindTable = fRule.Equals(2) ? itemModel.__config__.relationTable : itemModel.__config__.tableName,
                                    ChildTableKey = fRule.Equals(2) ? itemModel.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                    FieldRule = fRule,
                                    ModuleId = moduleModel.Id,
                                    FullName = fRule.Equals(2) ? childTableIndex[itemModel.__vModel__.Split('-').First()] + "-" + itemModel.__config__.label : itemModel.__config__.label,
                                    EnabledMark = 0,
                                    SortCode = 0
                                });
                            }
                        });

                        columnData.columnList.ForEach(item =>
                        {
                            var aut = columnAuth.Find(x => x.EnCode == item.prop);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        #endregion

                        // 添加PC菜单和权限
                        if (platform.Key == "Web")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

                            #region 表单权限
                            if (columnData.useFormPermission)
                            {
                                if (!oldWebModuleFormEntity.Any())
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(formAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else
                                {
                                    var formAuthAddList = new List<ModuleFormEntity>();
                                    formAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleFormEntity.Any(x => x.EnCode == item.EnCode)) formAuthAddList.Add(item);
                                    });
                                    if (formAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(formAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldWebModuleFormEntity.ForEach(item =>
                                    {
                                        var it = formAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = 1; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleFormEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 按钮权限
                            if (columnData.useBtnPermission)
                            {
                                if (!oldWebModuleButtonEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(btnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var btnAuthAddList = new List<ModuleButtonEntity>();
                                    btnAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleButtonEntity.Any(x => x.EnCode == item.EnCode)) btnAuthAddList.Add(item);
                                    });
                                    if (btnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(btnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldWebModuleButtonEntity.ForEach(item =>
                                    {
                                        var it = btnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleButtonEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 列表权限
                            if (columnData.useColumnPermission)
                            {
                                if (!oldWebModuleColumnEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(columnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var columnAuthAddList = new List<ModuleColumnEntity>();
                                    columnAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleColumnEntity.Any(x => x.EnCode == item.EnCode)) columnAuthAddList.Add(item);
                                    });
                                    if (columnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(columnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldWebModuleColumnEntity.ForEach(item =>
                                    {
                                        var it = columnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleColumnEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 数据权限
                            if (columnData.useDataPermission)
                            {
                                if (!_visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => x.EnCode.Equals("jnpf_alldata") && x.ModuleId == moduleModel.Id && x.DeleteMark == null).Any())
                                {
                                    // 全部数据权限方案
                                    var AllDataAuthScheme = new ModuleDataAuthorizeSchemeEntity()
                                    {
                                        FullName = "全部数据",
                                        EnCode = "jnpf_alldata",
                                        AllData = 1,
                                        ConditionText = string.Empty,
                                        ConditionJson = string.Empty,
                                        ModuleId = moduleModel.Id
                                    };
                                    await _visualDevRepository.AsSugarClient().Insertable(AllDataAuthScheme).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                                }

                                // 创建用户和所属组织权限方案
                                // 只添加 主表控件的数据权限
                                var fList = fieldList.Where(x => !x.__vModel__.Contains("_jnpf_") && x.__vModel__.IsNotEmptyOrNull() && x.__config__.visibility.Contains("pc"))
                                    .Where(x => x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER || x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE).ToList();

                                var authList = await MenuMergeDataAuth(moduleModel.Id, fList);
                                await MenuMergeDataAuthScheme(moduleModel.Id, authList, fList);
                            }
                            #endregion
                        }

                        #region App菜单、 权限组装
                        moduleModel.Id = oldAppModule != null ? oldAppModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ParentId = oldAppModule != null ? oldAppModule.ParentId : data.Value; // 父级菜单节点
                        moduleModel.Category = "App";
                        moduleModel.FullName = oldAppModule != null ? oldAppModule.FullName : fullName;
                        moduleModel.EnCode = oldAppModule != null ? oldAppModule.EnCode : enCode;
                        moduleModel.UrlAddress = oldAppModule != null ? oldAppModule.UrlAddress : "/pages/apply/dynamicModel/index?id=" + moduleModel.EnCode;
                        moduleModel.SystemId = oldAppModule != null ? oldAppModule.SystemId : data.Key;
                        moduleModel.SortCode = oldAppModule != null ? oldAppModule.SortCode : 999;
                        moduleModel.Icon = oldAppModule != null ? oldAppModule.Icon : "icon-ym icon-ym-webForm";

                        btnAuth = new List<ModuleButtonEntity>();
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_add", FullName = "新增", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_edit", FullName = "编辑", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_remove", FullName = "删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_detail", FullName = "详情", ModuleId = moduleModel.Id });
                        appColumnData.customBtnsList.ForEach(item =>
                        {
                            btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 1, SortCode = 0, ParentId = "-1", EnCode = item.value, FullName = item.label, ModuleId = moduleModel.Id });
                        });
                        appColumnData.btnsList.ForEach(item =>
                        {
                            var aut = btnAuth.Find(x => x.EnCode == "btn_" + item.value);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        formAuth.Clear();
                        fieldList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item =>
                        {
                            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
                            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                                ChildTableKey = fRule.Equals(2) ? item.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                FieldRule = fRule,
                                ModuleId = moduleModel.Id,
                                FullName = fRule.Equals(2) ? childTableIndex[item.__vModel__.Split('-').First()] + "-" + item.__config__.label : item.__config__.label,
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });
                        ctList.ForEach(item =>
                        {

                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = tInfo.MainTableName,
                                ChildTableKey = item.__vModel__,
                                FieldRule = 0,
                                ModuleId = moduleModel.Id,
                                FullName = childTableIndex[item.__vModel__],
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });

                        columnAuth.Clear();
                        appColumnData.defaultColumnList.ForEach(item =>
                        {
                            var itemModel = fieldList.FirstOrDefault(x => x.__config__.jnpfKey == item.__config__.jnpfKey && x.__vModel__ == item.prop);
                            if (itemModel != null)
                            {
                                var fRule = itemModel.__vModel__.Contains("_jnpf_") ? 1 : 0;
                                fRule = itemModel.__vModel__.ToLower().Contains("tablefield") && itemModel.__vModel__.Contains("-") ? 2 : fRule;
                                columnAuth.Add(new ModuleColumnEntity()
                                {
                                    ParentId = "-1",
                                    EnCode = itemModel.__vModel__,
                                    BindTable = fRule.Equals(2) ? itemModel.__config__.relationTable : itemModel.__config__.tableName,
                                    ChildTableKey = fRule.Equals(2) ? itemModel.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                    FieldRule = fRule,
                                    ModuleId = moduleModel.Id,
                                    FullName = fRule.Equals(2) ? childTableIndex[itemModel.__vModel__.Split('-').First()] + "-" + itemModel.__config__.label : itemModel.__config__.label,
                                    EnabledMark = 0,
                                    SortCode = 0
                                });
                            }
                        });
                        appColumnData.columnList.ForEach(item =>
                        {
                            var aut = columnAuth.Find(x => x.EnCode == item.prop);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        columnAuth.ForEach(item => { item.ModuleId = moduleModel.Id; });
                        #endregion

                        // 添加App菜单和权限
                        if (platform.Key == "App")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

                            #region 表单权限
                            if (appColumnData.useFormPermission)
                            {
                                if (!oldAppModuleFormEntity.Any())
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(formAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else
                                {
                                    var formAuthAddList = new List<ModuleFormEntity>();
                                    formAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleFormEntity.Any(x => x.EnCode == item.EnCode)) formAuthAddList.Add(item);
                                    });
                                    if (formAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(formAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldAppModuleFormEntity.ForEach(item =>
                                    {
                                        var it = formAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = 1; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleFormEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 按钮权限
                            if (appColumnData.useBtnPermission)
                            {
                                if (!oldAppModuleButtonEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(btnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var btnAuthAddList = new List<ModuleButtonEntity>();
                                    btnAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleButtonEntity.Any(x => x.EnCode == item.EnCode)) btnAuthAddList.Add(item);
                                    });
                                    if (btnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(btnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldAppModuleButtonEntity.ForEach(item =>
                                    {
                                        var it = btnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleButtonEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 列表权限
                            if (appColumnData.useColumnPermission)
                            {
                                if (!oldAppModuleColumnEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(columnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var columnAuthAddList = new List<ModuleColumnEntity>();
                                    columnAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleColumnEntity.Any(x => x.EnCode == item.EnCode)) columnAuthAddList.Add(item);
                                    });
                                    if (columnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(columnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldAppModuleColumnEntity.ForEach(item =>
                                    {
                                        var it = columnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleColumnEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 数据权限
                            if (appColumnData.useDataPermission)
                            {
                                // 全部数据权限
                                if (!_visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => x.EnCode.Equals("jnpf_alldata") && x.ModuleId == moduleModel.Id && x.DeleteMark == null).Any())
                                {
                                    // 全部数据权限方案
                                    var AllDataAuthScheme = new ModuleDataAuthorizeSchemeEntity()
                                    {
                                        FullName = "全部数据",
                                        EnCode = "jnpf_alldata",
                                        AllData = 1,
                                        ConditionText = string.Empty,
                                        ConditionJson = string.Empty,
                                        ModuleId = moduleModel.Id
                                    };
                                    await _visualDevRepository.AsSugarClient().Insertable(AllDataAuthScheme).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                                }

                                // 创建用户和所属组织权限方案
                                // 只添加 主表控件的数据权限
                                var fList = fieldList.Where(x => !x.__vModel__.Contains("_jnpf_") && x.__vModel__.IsNotEmptyOrNull() && x.__config__.visibility.Contains("app"))
                                    .Where(x => x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER || x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE).ToList();

                                var authList = await MenuMergeDataAuth(moduleModel.Id, fList);
                                await MenuMergeDataAuthScheme(moduleModel.Id, authList, fList);
                            }
                            #endregion
                        }
                    }
                }
            }

            // 修改功能发布状态
            await _visualDevRepository.AsUpdateable().SetColumns(it => new VisualDevEntity { State = 1, PlatformRelease = input.platformRelease }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();

            // 线上版本
            if (!await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().AnyAsync(x => x.Id.Equals(id)))
            {
                var vReleaseEntity = entity.Adapt<VisualDevReleaseEntity>();
                await _visualDevRepository.AsSugarClient().Insertable(vReleaseEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

                // 同步添加流程表单
                if (entity.EnableFlow.Equals(1))
                {
                    if (!_visualDevRepository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Any(x => x.TemplateId.Equals(entity.Id))) throw Oops.Oh(ErrorCode.D1421);
                    var fEntity = entity.Adapt<FlowFormEntity>();
                    fEntity.TableJson = entity.Tables;
                    fEntity.FlowType = 1;
                    fEntity.FormType = 2;
                    fEntity.EnabledMark = 1;
                    fEntity.PropertyJson = entity.FormData;
                    fEntity.DraftJson = fEntity.ToJsonString();
                    await _visualDevRepository.AsSugarClient().Updateable(fEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                    await _visualDevRepository.AsSugarClient().Updateable<FlowTemplateEntity>().SetColumns(x => x.EnabledMark == 1).Where(it => it.Id == entity.Id).ExecuteCommandHasChangeAsync();
                }
            }
            else
            {
                var vReleaseEntity = entity.Adapt<VisualDevReleaseEntity>();
                await _visualDevRepository.AsSugarClient().Updateable(vReleaseEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

                if (entity.EnableFlow.Equals(1))
                {
                    if (!_visualDevRepository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Any(x => x.TemplateId.Equals(entity.Id))) throw Oops.Oh(ErrorCode.D1421);
                    var fEntity = entity.Adapt<FlowFormEntity>();
                    fEntity.TableJson = entity.Tables;
                    fEntity.FlowType = 1;
                    fEntity.FormType = 2;
                    fEntity.EnabledMark = 1;
                    fEntity.PropertyJson = entity.FormData;
                    fEntity.DraftJson = fEntity.ToJsonString();
                    if (!await _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().AnyAsync(x => x.Id.Equals(id)))
                        await _visualDevRepository.AsSugarClient().Insertable(fEntity).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnEntityAsync();
                    else
                        await _visualDevRepository.AsSugarClient().Updateable(fEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

                    await _visualDevRepository.AsSugarClient().Updateable<FlowTemplateEntity>().SetColumns(x => x.EnabledMark == 1).Where(it => it.Id == entity.Id).ExecuteCommandHasChangeAsync();
                }
            }

            _db.CommitTran(); // 关闭事务
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw;
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="isGetRelease">是否获取发布版本.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<VisualDevEntity> GetInfoById(string id, bool isGetRelease = false)
    {
        if (isGetRelease)
        {
            var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>()
                .ClearFilter<IZxSystemFilter>() //开发模式下显示菜单找不到模块
                .FirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (vREntity != null && vREntity.EnableFlow == 1 && _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.Id.Equals(id)))
            {
                vREntity.FlowId = await _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().Where(x => x.Id.Equals(id)).Select(x => x.FlowId).FirstAsync();
                if (vREntity.FlowId.IsNotEmptyOrNull())
                {
                    if (!_visualDevRepository.AsSugarClient().Queryable<FlowTemplateEntity>().Where(x => x.Id.Equals(vREntity.FlowId) && x.EnabledMark.Equals(1)).Any())
                        vREntity.EnableFlow = -1;
                }
            }
            return vREntity.Adapt<VisualDevEntity>();
        }
        else
        {
            //查看历史纪录，需要去掉 && x.DeleteMark == null 这个条件
            var vEntity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id);
            //var vEntity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (_visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.Id.Equals(id)))
                vEntity.FlowId = await _visualDevRepository.AsSugarClient().Queryable<FlowFormEntity>().Where(x => x.Id.Equals(id)).Select(x => x.FlowId).FirstAsync();
            return vEntity.Adapt<VisualDevEntity>();
        }
    }

    /// <summary>
    /// 新增导入数据.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [NonAction]
    public async Task CreateImportData(VisualDevEntity input, int type)
    {
        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(input.Id))) errorList.Add("ID");
        if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(input.EnCode))) errorList.Add("编码");
        if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(input.FullName))) errorList.Add("名称");

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
                input.Id = SnowflakeIdHelper.NextId();
                input.FullName = string.Format("{0}.副本{1}", input.FullName, random);
                input.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        input.EnabledMark = 0;
        input.State = 0;
        input.DbLinkId = "0";
        input.Create();
        input.CreatorUserId = _userManager.UserId;
        input.LastModifyTime = null;
        input.LastModifyUserId = null;
        try
        {
            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(input).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }

        // 流程表单
        if (input.EnableFlow.Equals(1) && !input.Type.Equals(3) && !input.Type.Equals(4))
        {
            var fEntity = input.Adapt<FlowFormEntity>();
            fEntity.PropertyJson = input.FormData;
            fEntity.TableJson = input.Tables;
            fEntity.DraftJson = fEntity.ToJsonString();
            fEntity.FlowType = 1;
            fEntity.FormType = 2;
            fEntity.FlowId = input.Id;
            try
            {
                await _visualDevRepository.AsSugarClient().Insertable(fEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                await SaveFlowTemplate(input);
            }
            catch (Exception ex)
            {
                throw Oops.Oh(ErrorCode.COM1020, ex.Message);
            }
        }
    }

    /// <summary>
    /// 功能模板 无表 转 有表.
    /// </summary>
    /// <param name="vEntity">功能实体.</param>
    /// <param name="mainTableName">主表名称.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<VisualDevEntity> NoTblToTable(VisualDevEntity vEntity, string mainTableName)
    {
        var dbtype = _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
        var isUpper = false; // 是否大写
        if (dbtype.ToLower().Equals("oracle") || dbtype.ToLower().Equals("dm") || dbtype.ToLower().Equals("dm8")) isUpper = true;
        else isUpper = false;

        // Oracle和Dm数据库 表名全部大写, 其他全部小写
        mainTableName = isUpper ? mainTableName.ToUpper() : mainTableName.ToLower();

        FormDataModel formModel = vEntity.FormData.ToObjectOld<FormDataModel>();
        List<FieldsModel>? fieldsModelList = TemplateAnalysis.AnalysisTemplateData(formModel.fields);

        #region 创表信息组装

        List<DbTableAndFieldModel>? addTableList = new List<DbTableAndFieldModel>(); // 表集合

        // 主表信息
        DbTableAndFieldModel? mainInfo = new DbTableAndFieldModel();
        mainInfo.table = mainTableName;
        mainInfo.tableName = vEntity.FullName;
        mainInfo.FieldList = FieldsModelToTableFile(fieldsModelList, formModel.primaryKeyPolicy == 2, isUpper);

        var inteAssistant = isUpper ? "F_INTE_ASSISTANT" : "f_inte_assistant";
        mainInfo.FieldList.Add(new DbTableFieldModel() { field = inteAssistant, fieldName = "集成助手数据标识", dataType = "int", dataLength = "1", allowNull = 1 });

        if (vEntity.EnableFlow.Equals(1))
        {
            var flowTaskId = isUpper ? "F_FLOW_TASK_ID" : "f_flow_task_id";
            var flowId = isUpper ? "F_FLOW_ID" : "f_flow_id";

            mainInfo.FieldList.Add(new DbTableFieldModel() { field = flowTaskId, fieldName = "流程任务Id", dataType = "varchar", dataLength = "50", allowNull = 1 });
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = flowId, fieldName = "流程引擎Id", dataType = "varchar", dataLength = "50", allowNull = 1 });
        }
        if (formModel.logicalDelete)
        {
            var deleteMark = isUpper ? "F_DELETE_MARK" : "f_delete_mark";
            var deleteUserId = isUpper ? "F_DELETE_USER_ID" : "f_delete_user_id";
            var deleteTime = isUpper ? "F_DELETE_TIME" : "f_delete_time";

            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteMark, fieldName = "删除标识", dataType = "int", dataLength = "1", allowNull = 1 });
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteUserId, fieldName = "删除用户", dataType = "varchar", dataLength = "50", allowNull = 1 });
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteTime, fieldName = "删除时间", dataType = "datetime", dataLength = "50", allowNull = 1 });
        }
        if (_tenant.MultiTenancy && _userManager.CurrentTenantInformation.type.Equals(1))
        {
            var tenantId = isUpper ? "F_TENANT_ID" : "f_tenant_id";

            mainInfo.FieldList.Add(new DbTableFieldModel() { field = tenantId, fieldName = "租户Id", dataType = "varchar", dataLength = "50", allowNull = 1 });
        }

        // 子表信息
        Dictionary<string, string>? childTableDic = new Dictionary<string, string>();
        fieldsModelList.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList().ForEach(item =>
        {
            DbTableAndFieldModel? childTInfo = new DbTableAndFieldModel();
            childTInfo.table = "ct" + SnowflakeIdHelper.NextId();
            childTInfo.table = isUpper ? childTInfo.table.ToUpper() : childTInfo.table.ToLower();
            childTableDic.Add(item.__vModel__, childTInfo.table);
            childTInfo.tableName = vEntity.FullName + "_子表";
            childTInfo.FieldList = FieldsModelToTableFile(item.__config__.children, formModel.primaryKeyPolicy == 2, isUpper);

            var foreignId = isUpper ? "F_FOREIGN_ID" : "f_foreign_id";
            childTInfo.FieldList.Add(new DbTableFieldModel() { dataLength = "50", allowNull = 1, dataType = "varchar", field = foreignId, fieldName = vEntity.FullName + "_关联外键" });

            addTableList.Add(childTInfo);
        });

        #endregion

        #region 修改功能模板 有表改无表
        List<TableModel>? modelTableList = new List<TableModel>();

        // 处理主表
        TableModel? mainTable = new TableModel();
        mainTable.fields = new List<EntityFieldModel>();
        mainTable.table = mainInfo.table;
        mainTable.tableName = mainInfo.tableName;
        mainTable.typeId = "1";
        mainInfo.FieldList.ForEach(item => // 表字段
        {
            EntityFieldModel? etFieldModel = new EntityFieldModel();
            etFieldModel.DataLength = item.dataLength;
            etFieldModel.PrimaryKey = 1;
            etFieldModel.DataType = item.dataType;
            etFieldModel.Field = item.field;
            etFieldModel.FieldName = item.fieldName;
            mainTable.fields.Add(etFieldModel);
        });

        // 处理子表
        addTableList.ForEach(item =>
        {
            TableModel? childInfo = new TableModel();
            childInfo.fields = new List<EntityFieldModel>();
            childInfo.table = item.table;
            childInfo.tableName = item.tableName;
            childInfo.tableField = isUpper ? "F_FOREIGN_ID" : "f_foreign_id"; // 关联外键
            childInfo.relationField = isUpper ? "F_ID" : "f_id"; // 关联主键
            childInfo.typeId = "0";
            item.FieldList.ForEach(it => // 子表字段
            {
                EntityFieldModel? etFieldModel = new EntityFieldModel();
                etFieldModel.DataLength = it.dataLength;
                etFieldModel.PrimaryKey = it.primaryKey.ParseToInt();
                etFieldModel.DataType = it.dataType;
                etFieldModel.Field = it.field;
                etFieldModel.FieldName = it.fieldName;
                childInfo.fields.Add(etFieldModel);
            });
            modelTableList.Add(childInfo);
        });
        modelTableList.Add(mainTable);

        #region 给控件绑定 tableName、relationTable 属性

        // 用字典反序列化， 避免多增加不必要的属性
        Dictionary<string, object>? dicFormModel = vEntity.FormData.ToObjectOld<Dictionary<string, object>>();
        List<Dictionary<string, object>>? dicFieldsModelList = dicFormModel.FirstOrDefault(x => x.Key == "fields").Value.ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();

        // 给控件绑定 tableName
        FieldBindTable(dicFieldsModelList, childTableDic, mainTableName);

        #endregion

        dicFormModel["fields"] = dicFieldsModelList; // 修改表单控件
        vEntity.FormData = dicFormModel.ToJsonString(); // 修改模板
        vEntity.Tables = modelTableList.ToJsonString(); // 修改模板涉及表

        addTableList.Add(mainInfo);

        #endregion

        try
        {
            _db.BeginTran(); // 执行事务
            var link = await _runService.GetDbLink(vEntity.DbLinkId);
            foreach (DbTableAndFieldModel? item in addTableList)
            {
                bool res = _changeDataBase.Create(link, item, item.FieldList);
                if (!res) throw null;
            }

            if (await _visualDevRepository.IsAnyAsync(x => x.Id.Equals(vEntity.Id)))
                await _visualDevRepository.AsUpdateable(vEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            else
                await _visualDevRepository.AsInsertable(vEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            _db.CommitTran(); // 提交事务

            return vEntity;
        }
        catch (Exception)
        {
            _db.RollbackTran(); // 回滚事务
            return null;
        }
    }

    #endregion

    #region Private

    /// <summary>
    /// 组件转换表字段.
    /// </summary>
    /// <param name="fmList">表单列表.</param>
    /// <param name="isIdentity">主键是否自增长.</param>
    /// <param name="isUpper">是否大写.</param>
    /// <returns></returns>
    [NonAction]
    private List<DbTableFieldModel> FieldsModelToTableFile(List<FieldsModel> fmList, bool isIdentity, bool isUpper)
    {
        List<DbTableFieldModel>? fieldList = new List<DbTableFieldModel>(); // 表字段
        List<FieldsModel>? mList = fmList.Where(x => x.__config__.jnpfKey.IsNotEmptyOrNull())
            .Where(x => x.__config__.jnpfKey != JnpfKeyConst.QRCODE && x.__config__.jnpfKey != JnpfKeyConst.BARCODE && x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList(); // 非存储字段
        fieldList.Add(new DbTableFieldModel()
        {
            primaryKey = true,
            dataType = isIdentity ? "int" : "varchar",
            dataLength = "50",
            identity = isIdentity,
            field = isUpper ? "F_ID" : "f_id",
            fieldName = "主键"
        });

        foreach (var item in mList)
        {
            // 不生成数据库字段(控件类型为：展示数据)，关联表单、弹窗选择、计算公式.
            if ((item.__config__.jnpfKey == JnpfKeyConst.RELATIONFORMATTR || item.__config__.jnpfKey == JnpfKeyConst.POPUPATTR || item.__config__.jnpfKey == JnpfKeyConst.CALCULATE) && item.isStorage.Equals(0))
                continue;
            DbTableFieldModel? field = new DbTableFieldModel();
            field.field = item.__vModel__;
            field.fieldName = item.__config__.label;
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.NUMINPUT:
                case JnpfKeyConst.CALCULATE:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = item.precision.IsNullOrEmpty() ? 15 : item.precision;
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.TIME:
                    field.dataType = "varchar";
                    field.dataLength = "50";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.MODIFYTIME:
                    field.dataType = "DateTime";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.EDITOR:
                case JnpfKeyConst.UPLOADFZ:
                case JnpfKeyConst.UPLOADIMG:
                case JnpfKeyConst.SIGN:
                    field.dataType = "longtext";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.RATE:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = 1;
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.SLIDER:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = 15;
                    field.allowNull = 1;
                    break;
                default:
                    field.dataType = "varchar";
                    field.dataLength = "500";
                    field.allowNull = 1;
                    break;
            }

            if (field.field.IsNotEmptyOrNull()) fieldList.Add(field);
        }

        return fieldList;
    }

    /// <summary>
    /// 组装菜单 数据权限 字段管理数据.
    /// </summary>
    /// <param name="menuId">菜单ID.</param>
    /// <param name="fields">功能模板控件集合.</param>
    /// <returns></returns>
    private async Task<List<ModuleDataAuthorizeEntity>> MenuMergeDataAuth(string menuId, List<FieldsModel> fields)
    {
        // 旧的自动生成的 字段管理
        List<ModuleDataAuthorizeEntity>? oldDataAuth = await _visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeEntity>()
            .Where(x => x.ModuleId == menuId && x.DeleteMark == null && x.SortCode.Equals(-9527))
            .Where(x => (x.ConditionText == "@organizationAndSuborganization" && x.ConditionSymbol == "in") || (x.ConditionText == "@organizeId" && x.ConditionSymbol == "==")
            || (x.ConditionText == "@userAndSubordinates" && x.ConditionSymbol == "in") || (x.ConditionText == "@userId" && x.ConditionSymbol == "==")
            || (x.ConditionText == "@branchManageOrganize" && x.ConditionSymbol == "in"))
            .ToListAsync();

        List<ModuleDataAuthorizeEntity>? authList = new List<ModuleDataAuthorizeEntity>(); // 字段管理
        List<ModuleDataAuthorizeEntity>? noDelData = new List<ModuleDataAuthorizeEntity>(); // 记录未删除

        // 当前用户
        FieldsModel? item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER);
        if (item != null)
        {
            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;

            // 新增
            if (!oldDataAuth.Any(x => x.EnCode == item.__vModel__ && x.ConditionText == "@userId"))
            {
                authList.Add(new ModuleDataAuthorizeEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ConditionSymbol = "==", // 条件符号
                    Type = "varchar", // 字段类型
                    FullName = item.__config__.label, // 字段说明
                    ConditionText = "@userId", // 条件内容（当前用户）
                    EnabledMark = 1,
                    SortCode = -9527,
                    FieldRule = fRule, // 主表/副表/子表
                    EnCode = fRule.Equals(1) ? item.__vModel__.Split("jnpf_").LastOrDefault() : item.__vModel__,
                    BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                    ModuleId = menuId
                });
            }

            if (!oldDataAuth.Any(x => x.EnCode == item.__vModel__ && x.ConditionText == "@userAndSubordinates"))
            {
                authList.Add(new ModuleDataAuthorizeEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ConditionSymbol = "in", // 条件符号
                    Type = "varchar", // 字段类型
                    FullName = item.__config__.label, // 字段说明
                    ConditionText = "@userAndSubordinates", // 条件内容（当前用户及下属）
                    EnabledMark = 1,
                    SortCode = -9527,
                    FieldRule = fRule, // 主表/副表/子表
                    EnCode = fRule.Equals(1) ? item.__vModel__.Split("jnpf_").LastOrDefault() : item.__vModel__,
                    BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                    ModuleId = menuId
                });
            }

            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.EnCode != item.__vModel__ && (x.ConditionText == "@userId" || x.ConditionText == "@userAndSubordinates")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();

            noDelData = oldDataAuth.Except(delData).ToList(); // 记录未删除
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.ConditionText == "@userId" || x.ConditionText == "@userAndSubordinates").ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        // 所属组织
        item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE);
        if (item != null)
        {
            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;

            // 新增
            if (!oldDataAuth.Any(x => x.EnCode == item.__vModel__ && x.ConditionText == "@organizeId"))
            {
                authList.Add(new ModuleDataAuthorizeEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ConditionSymbol = "==", // 条件符号
                    Type = "varchar", // 字段类型
                    FullName = item.__config__.label, // 字段说明
                    ConditionText = "@organizeId", // 条件内容（当前组织）
                    EnabledMark = 1,
                    SortCode = -9527,
                    FieldRule = fRule, // 主表/副表/子表
                    EnCode = fRule.Equals(1) ? item.__vModel__.Split("jnpf_").LastOrDefault() : item.__vModel__,
                    BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                    ModuleId = menuId
                });
            }

            if (!oldDataAuth.Any(x => x.EnCode == item.__vModel__ && x.ConditionText == "@organizationAndSuborganization"))
            {
                authList.Add(new ModuleDataAuthorizeEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ConditionSymbol = "in", // 条件符号
                    Type = "varchar", // 字段类型
                    FullName = item.__config__.label, // 字段说明
                    ConditionText = "@organizationAndSuborganization", // 条件内容（当前组织及组织）
                    EnabledMark = 1,
                    SortCode = -9527,
                    FieldRule = fRule, // 主表/副表/子表
                    EnCode = fRule.Equals(1) ? item.__vModel__.Split("jnpf_").LastOrDefault() : item.__vModel__,
                    BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                    ModuleId = menuId
                });
            }

            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.EnCode != item.__vModel__ && (x.ConditionText == "@organizeId" || x.ConditionText == "@organizationAndSuborganization")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();

            noDelData = oldDataAuth.Except(delData).ToList(); // 记录未删除
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.ConditionText == "@organizeId" || x.ConditionText == "@organizationAndSuborganization").ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        // 当前分管组织
        item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE);
        if (item != null)
        {
            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;

            // 新增
            if (!oldDataAuth.Any(x => x.EnCode == item.__vModel__ && x.ConditionText == "@branchManageOrganize"))
            {
                authList.Add(new ModuleDataAuthorizeEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ConditionSymbol = "in", // 条件符号
                    Type = "varchar", // 字段类型
                    FullName = item.__config__.label, // 字段说明
                    ConditionText = "@branchManageOrganize", // 条件内容（当前分管组织）
                    EnabledMark = 1,
                    SortCode = -9527,
                    FieldRule = fRule, // 主表/副表/子表
                    EnCode = fRule.Equals(1) ? item.__vModel__.Split("jnpf_").LastOrDefault() : item.__vModel__,
                    BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                    ModuleId = menuId
                });
            }

            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.EnCode != item.__vModel__ && (x.ConditionText == "@branchManageOrganize")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();

            noDelData = oldDataAuth.Except(delData).ToList(); // 记录未删除
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeEntity>? delData = oldDataAuth.Where(x => x.ConditionText == "@branchManageOrganize").ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        if (authList.Any()) await _visualDevRepository.AsSugarClient().Insertable(authList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (noDelData.Any()) authList.AddRange(noDelData);
        return authList.Any() ? authList : oldDataAuth;
    }

    /// <summary>
    /// 组装菜单 数据权限 方案管理数据.
    /// </summary>
    /// <param name="menuId">菜单ID.</param>
    /// <param name="authList">字段管理列表.</param>
    /// <param name="fields">功能模板控件集合.</param>
    /// <returns></returns>
    private async Task MenuMergeDataAuthScheme(string menuId, List<ModuleDataAuthorizeEntity> authList, List<FieldsModel> fields)
    {
        // 旧的自动生成的 方案管理
        List<ModuleDataAuthorizeSchemeEntity>? oldDataAuthScheme = await _visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(x => x.ModuleId == menuId && x.DeleteMark == null && x.SortCode.Equals(-9527))
            .Where(x => x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@userId\"")
            || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@userAndSubordinates\"")
            || x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@organizeId\"")
            || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@organizationAndSuborganization\"")
            || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@branchManageOrganize\""))
            .ToListAsync();

        List<ModuleDataAuthorizeSchemeEntity>? authSchemeList = new List<ModuleDataAuthorizeSchemeEntity>(); // 方案管理

        // 当前用户
        FieldsModel? item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER);
        var condJson = new AuthorizeModuleResourceConditionModelInput()
        {
            logic = "and",
            groups = new List<AuthorizeModuleResourceConditionItemModelInput>() { new AuthorizeModuleResourceConditionItemModelInput() { id = "", bindTable = "", field = "", fieldRule = 0, value = "", type = "varchar", op = "==" } }
        };

        if (item != null)
        {
            ModuleDataAuthorizeEntity? model = authList.FirstOrDefault(x => x.EnCode == item.__vModel__ && x.ConditionText.Equals("@userId"));

            if (model != null)
            {
                condJson.groups.First().id = model.Id;
                condJson.groups.First().bindTable = model.BindTable;
                condJson.groups.First().field = item.__vModel__;
                condJson.groups.First().fieldRule = model.FieldRule.ParseToInt();
                condJson.groups.First().value = "@userId";
                condJson.groups.First().conditionText = "@userId";

                // 新增
                if (!oldDataAuthScheme.Any(x => x.ConditionText == "【{" + item.__config__.label + "} {等于} {@userId}】"))
                {
                    authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                    {
                        FullName = "当前用户",
                        EnCode = SnowflakeIdHelper.NextId(),
                        SortCode = -9527,
                        ConditionText = "【{" + item.__config__.label + "} {等于} {@userId}】",
                        ConditionJson = new List<AuthorizeModuleResourceConditionModelInput>() { condJson }.ToJsonStringOld(),
                        ModuleId = menuId,
                        MatchLogic = "and"
                    });
                }

                model = authList.FirstOrDefault(x => x.EnCode == item.__vModel__ && x.ConditionText.Equals("@userAndSubordinates"));
                condJson.groups.First().id = model.Id;
                condJson.groups.First().op = "in";
                condJson.groups.First().value = "@userAndSubordinates";
                condJson.groups.First().conditionText = "@userAndSubordinates";
                if (!oldDataAuthScheme.Any(x => x.ConditionText == "【{" + item.__config__.label + "} {包含任意一个} {@userAndSubordinates}】"))
                {
                    authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                    {
                        FullName = "当前用户及下属",
                        EnCode = SnowflakeIdHelper.NextId(),
                        SortCode = -9527,
                        ConditionText = "【{" + item.__config__.label + "} {包含任意一个} {@userAndSubordinates}】",
                        ConditionJson = new List<AuthorizeModuleResourceConditionModelInput>() { condJson }.ToJsonStringOld(),
                        ModuleId = menuId,
                        MatchLogic = "and"
                    });
                }

                // 删除
                //List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme.Where(x => x.EnCode != item.__vModel__
                //&& (x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@userId\"") || x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@userAraSubordinates\""))).ToList();
                //await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
            else
            {
                // 删除
                List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                    .Where(x => x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@userId\"") || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@userAndSubordinates\"")).ToList();
                await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                .Where(x => x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@userId\"") || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@userAndSubordinates\"")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        // 当前组织
        item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE);
        if (item != null)
        {
            ModuleDataAuthorizeEntity? model = authList.FirstOrDefault(x => x.EnCode == item.__vModel__ && x.ConditionText.Equals("@organizeId"));

            if (model != null)
            {
                condJson.groups.First().id = model.Id;
                condJson.groups.First().bindTable = model.BindTable;
                condJson.groups.First().field = item.__vModel__;
                condJson.groups.First().fieldRule = model.FieldRule.ParseToInt();
                condJson.groups.First().op = "==";
                condJson.groups.First().value = "@organizeId";
                condJson.groups.First().conditionText = "@organizeId";

                // 新增
                if (!oldDataAuthScheme.Any(x => x.ConditionText == "【{" + item.__config__.label + "} {等于} {@organizeId}】"))
                {
                    authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                    {
                        FullName = "当前组织",
                        EnCode = SnowflakeIdHelper.NextId(),
                        SortCode = -9527,
                        ConditionText = "【{" + item.__config__.label + "} {等于} {@organizeId}】",
                        ConditionJson = new List<AuthorizeModuleResourceConditionModelInput>() { condJson }.ToJsonStringOld(),
                        ModuleId = menuId,
                        MatchLogic = "and"
                    });
                }

                model = authList.FirstOrDefault(x => x.EnCode == item.__vModel__ && x.ConditionText.Equals("@organizationAndSuborganization"));
                condJson.groups.First().id = model.Id;
                condJson.groups.First().op = "in";
                condJson.groups.First().value = "@organizationAndSuborganization";
                condJson.groups.First().conditionText = "@organizationAndSuborganization";
                if (!oldDataAuthScheme.Any(x => x.ConditionText == "【{" + item.__config__.label + "} {包含任意一个} {@organizationAndSuborganization}】"))
                {
                    authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                    {
                        FullName = "当前组织及子组织",
                        EnCode = SnowflakeIdHelper.NextId(),
                        SortCode = -9527,
                        ConditionText = "【{" + item.__config__.label + "} {包含任意一个} {@organizationAndSuborganization}】",
                        ConditionJson = new List<AuthorizeModuleResourceConditionModelInput>() { condJson }.ToJsonStringOld(),
                        ModuleId = menuId,
                        MatchLogic = "and"
                    });
                }

                // 删除
                //List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme.Where(x => x.EnCode != item.__vModel__
                //&& (x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@organizeId\"") || x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@organizationAndSuborganization\""))).ToList();
                //await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
            else
            {
                // 删除
                List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                    .Where(x => x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@organizeId\"") || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@organizationAndSuborganization\"")).ToList();
                await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                .Where(x => x.ConditionJson.Contains("\"op\":\"==\",\"value\":\"@organizeId\"") || x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@organizationAndSuborganization\"")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        // 当前分管组织
        item = fields.FirstOrDefault(x => x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE);
        if (item != null)
        {
            ModuleDataAuthorizeEntity? model = authList.FirstOrDefault(x => x.EnCode == item.__vModel__ && x.ConditionText.Equals("@branchManageOrganize"));

            if (model != null)
            {
                condJson.groups.First().id = model.Id;
                condJson.groups.First().bindTable = model.BindTable;
                condJson.groups.First().field = item.__vModel__;
                condJson.groups.First().fieldRule = model.FieldRule.ParseToInt();
                condJson.groups.First().op = "in";
                condJson.groups.First().value = "@branchManageOrganize";
                condJson.groups.First().conditionText = "@branchManageOrganize";

                // 新增
                if (!oldDataAuthScheme.Any(x => x.ConditionText == "【{" + item.__config__.label + "} {包含任意一个} {@branchManageOrganize}】"))
                {
                    authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                    {
                        FullName = "当前分管组织",
                        EnCode = SnowflakeIdHelper.NextId(),
                        SortCode = -9527,
                        ConditionText = "【{" + item.__config__.label + "} {包含任意一个} {@branchManageOrganize}】",
                        ConditionJson = new List<AuthorizeModuleResourceConditionModelInput>() { condJson }.ToJsonStringOld(),
                        ModuleId = menuId,
                        MatchLogic = "and"
                    });
                }

                // 删除
                //List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme.Where(x => x.EnCode != item.__vModel__
                //&& (x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@branchManageOrganize\"") || x.ConditionJson.Contains("\"op\":\"Equal\",\"value\":\"@branchManageOrganizeAndSub\""))).ToList();
                //await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
            else
            {
                // 删除
                List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                    .Where(x => x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@branchManageOrganize\"")).ToList();
                await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
            }
        }
        else
        {
            // 删除
            List<ModuleDataAuthorizeSchemeEntity>? delData = oldDataAuthScheme
                .Where(x => x.ConditionJson.Contains("\"op\":\"in\",\"value\":\"@branchManageOrganize\"")).ToList();
            await _visualDevRepository.AsSugarClient().Deleteable(delData).ExecuteCommandAsync();
        }

        if (authSchemeList.Any()) await _visualDevRepository.AsSugarClient().Insertable(authSchemeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 无限递归 给控件绑定tableName (绕过 布局控件).
    /// </summary>
    private void FieldBindTable(List<Dictionary<string, object>> dicFieldsModelList, Dictionary<string, string> childTableDic, string tableName)
    {
        foreach (var item in dicFieldsModelList)
        {
            var obj = item["__config__"].ToObject<Dictionary<string, object>>();

            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE)) obj["tableName"] = childTableDic[item["__vModel__"].ToString()];
            else if (obj.ContainsKey("tableName")) obj["tableName"] = tableName;

            // 关联表单属性和弹窗属性
            if (obj.ContainsKey("jnpfKey") && (obj["jnpfKey"].Equals(JnpfKeyConst.RELATIONFORMATTR) || obj["jnpfKey"].Equals(JnpfKeyConst.POPUPATTR)))
            {
                string relationField = Convert.ToString(item["relationField"]);
                string? rField = relationField.ReplaceRegex(@"_jnpfTable_(\w+)", string.Empty);
                item["relationField"] = string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", tableName, "1");
            }

            // 子表控件
            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var cList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                foreach (var child in cList)
                {
                    var cObj = child["__config__"].ToObject<Dictionary<string, object>>();
                    if (cObj.ContainsKey("relationTable")) cObj["relationTable"] = childTableDic[item["__vModel__"].ToString()];
                    else cObj.Add("relationTable", childTableDic[item["__vModel__"].ToString()]);

                    if (cObj.ContainsKey("tableName")) cObj["tableName"] = obj["tableName"];

                    // 关联表单属性和弹窗属性
                    if (cObj.ContainsKey("jnpfKey") && (cObj["jnpfKey"].Equals(JnpfKeyConst.RELATIONFORMATTR) || cObj["jnpfKey"].Equals(JnpfKeyConst.POPUPATTR)))
                    {
                        string relationField = Convert.ToString(child["relationField"]);
                        string? rField = relationField.ReplaceRegex(@"_jnpfTable_(\w+)", string.Empty);
                        if (child.ContainsKey("relationField")) child["relationField"] = string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", cObj["tableName"], "0");
                        else child.Add("relationField", string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", cObj["tableName"], "0"));
                    }

                    child["__config__"] = cObj;
                }

                obj["children"] = cList;
            }

            // 递归
            if (obj.ContainsKey("children") && !obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var fmList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                FieldBindTable(fmList, childTableDic, tableName);
                obj["children"] = fmList;
            }

            item["__config__"] = obj;
        }
    }

    /// <summary>
    /// 验证主键策略 数据库表是否支持.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="dbLinkId">数据库连接id.</param>
    private async Task VerifyPrimaryKeyPolicy(TemplateParsingBase tInfo, string dbLinkId)
    {
        if (tInfo.IsHasTable)
        {
            DbLinkEntity link = await _runService.GetDbLink(dbLinkId);
            tInfo.AllTable.ForEach(item =>
            {
                List<DbTableFieldModel>? tableList = _changeDataBase.GetFieldList(link, item.table); // 获取主表所有列
                var mainPrimary = tableList.Find(t => t.primaryKey); // 主表主键
                if (mainPrimary == null)
                {
                    if (item.table.ToLower().StartsWith("sys_"))
                    {
                        //自定义视图
                        return;
                    }
                    throw Oops.Oh(ErrorCode.D1409, "主键为空", item.table);
                }

                if (tInfo.FormModel.primaryKeyPolicy.Equals(2) && !mainPrimary.identity)
                {
                    throw Oops.Oh(ErrorCode.D1409, "自增长ID,没有自增标识", item.table);
                }
                if (tInfo.FormModel.primaryKeyPolicy.Equals(1) && !(mainPrimary.dataType.ToLower().Equals("string") || mainPrimary.dataType.ToLower().Equals("varchar") || mainPrimary.dataType.ToLower().Equals("nvarchar")))
                    throw Oops.Oh(ErrorCode.D1409, "雪花ID", item.table);
            });
        }
    }

    /// <summary>
    /// 同步到流程相关.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task SaveFlowTemplate(VisualDevEntity input)
    {
        if (!(await _visualDevRepository.AsSugarClient().Queryable<FlowTemplateEntity>().AnyAsync(x => x.Id.Equals(input.Id))))
        {
            if (await _visualDevRepository.AsSugarClient().Queryable<FlowTemplateEntity>().AnyAsync(x => (x.EnCode == input.EnCode || x.FullName == input.FullName) && x.DeleteMark == null))
                throw Oops.Oh(ErrorCode.COM1004);

            var dictionaryTypeId = await _visualDevRepository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => x.DeleteMark == null && x.EnCode == "WorkFlowCategory").Select(x => x.Id).FirstAsync();
            var flowTypeId = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(x => x.DeleteMark == null && x.DictionaryTypeId.Equals(dictionaryTypeId)).Select(x => x.Id).FirstAsync();

            var inputType = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(x => x.DeleteMark == null && x.Id.Equals(input.Category)).FirstAsync();
            var inputTypeId = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(x => x.DeleteMark == null && x.EnCode.Equals(inputType.EnCode) && x.DictionaryTypeId.Equals(dictionaryTypeId)).Select(it => it.Id).FirstAsync();
            if (inputTypeId.IsNotEmptyOrNull()) flowTypeId = inputTypeId;

            var flowTemplateEntity = input.Adapt<FlowTemplateEntity>();
            flowTemplateEntity.EnabledMark = 0;
            flowTemplateEntity.Type = 1;
            flowTemplateEntity.Category = flowTypeId;
            //flowTemplateEntity.IconBackground = "#008cff";.
            //flowTemplateEntity.Icon = "icon-ym icon-ym-node";

            var result = await _visualDevRepository.AsSugarClient().Insertable(flowTemplateEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
            if (result == null)
                throw Oops.Oh(ErrorCode.COM1005);
        }
    }

    /// <summary>
    /// 同步业务字段.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private async Task SyncField(VisualDevEntity entity)
    {
        if (entity.Tables.IsNotEmptyOrNull() && !entity.Tables.Equals("[]") && entity.FormData.IsNotEmptyOrNull())
        {
            TemplateParsingBase? tInfo = new TemplateParsingBase(entity); // 解析模板
            DbLinkEntity link = await _runService.GetDbLink(entity.DbLinkId);
            tInfo.DbLink = link;
            await _runService.SyncField(tInfo);
        }
    }

    /// <summary>
    /// 缓存优先读取（5 分钟 TTL），避免每次分页请求都全表扫描引用表。
    /// 并发首查串行化（缓存击穿防护），避免重复全表扫描（SqlSugar 非线程安全）。
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _refCacheGates = new();

    private async Task<List<T>> GetOrSetCacheAsync<T>(string key, Func<Task<List<T>>> factory, int ttlMinutes = 5)
    {
        var cached = _cacheManager.Get<List<T>>(key);
        if (cached != null) return cached;

        var gate = _refCacheGates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            cached = _cacheManager.Get<List<T>>(key);
            if (cached != null) return cached;

            var result = await factory();
            _cacheManager.Set(key, result, TimeSpan.FromMinutes(ttlMinutes));
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    #endregion
}
