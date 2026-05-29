using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using Mapster;
using SqlSugar;

namespace JNPF.Common.CodeGen.DataParsing;

public class ControlParsing : ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 模板表单列表数据解析.
    /// </summary>
    private readonly FormDataParsing _formDataParsing;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public ControlParsing(
        IUserManager userManager,
        FormDataParsing formDataParsing,
        ISqlSugarRepository<UserEntity> repositoryRepository,
        IDataInterfaceService dataInterfaceService,
        ICacheManager cacheManager)
    {
        _userManager = userManager;
        _repository = repositoryRepository;
        _cacheManager = cacheManager;
        _formDataParsing = formDataParsing;
        _dataInterfaceService = dataInterfaceService;
    }

    /// <summary>
    /// 解析控件数据.
    /// </summary>
    /// <param name="oldDatas">原数据集合.</param>
    /// <param name="vModelStr">解析的字段集合 多个需以 ,号隔开.</param>
    /// <param name="jnpfKeyConst">控件类型 (JnpfKeyConst).</param>
    /// <param name="tenantId">租户Id.</param>
    /// <param name="vModelAttr">控件属性 (vModel ,属性字符串).</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <param name="userOrigin">请求端类型 pc , app.</param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, object>>> GetParsDataList(
        List<Dictionary<string, object>> oldDatas,
        string vModelStr,
        string jnpfKeyConst,
        string tenantId,
        List<FieldsModel>? vModelAttr = null,
        bool isInlineEditor = false,
        string userOrigin = "pc")
    {
        var vModels = new Dictionary<string, object>();
        var vModelList = vModelStr.Split(',');
        oldDatas.ForEach(items =>
        {
            foreach (var item in items)
            {
                if (vModelList.Contains(item.Key) && !vModels.ContainsKey(item.Key)) vModels.Add(item.Key, jnpfKeyConst);

                // 子表
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctOldDatas = item.Value.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                    ctOldDatas.ForEach(ctItems =>
                    {
                        foreach (var ctItem in ctItems)
                        {
                            if ((vModelList.Contains(item.Key + "-" + ctItem.Key) || vModelList.Contains(item.Key + "-" + ctItem.Key + "_name")) && !vModels.ContainsKey(item.Key + "-" + ctItem.Key))
                                vModels.Add(item.Key + "-" + ctItem.Key, jnpfKeyConst);
                        }
                    });
                }
            }
        });

        return await GetParsDataByList(vModelAttr, oldDatas, vModels, tenantId, isInlineEditor, userOrigin);
    }

    /// <summary>
    /// 获取解析数据.
    /// </summary>
    /// <param name="fields">模板集合.</param>
    /// <param name="oldDatas">原数据集合.</param>
    /// <param name="vModels">需解析的字段 (字段名,JnpfKeyConst/子表dictionary).</param>
    /// <param name="tenantId">租户Id.</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <param name="userOrigin">请求端类型 pc , app.</param>
    /// <param name="isChildTable">是否子表控件.</param>
    /// <returns></returns>
    private async Task<List<Dictionary<string, object>>> GetParsDataByList(
        List<FieldsModel> fields,
        List<Dictionary<string, object>> oldDatas,
        Dictionary<string, object> vModels,
        string tenantId,
        bool isInlineEditor,
        string userOrigin,
        bool isChildTable = false)
    {
        var cacheData = await GetCaCheData(vModels, tenantId);
        var usersselectDatas = cacheData.Where(t => t.Key.Equals(CommonConst.CodeGenDynamic + "_usersSelect_" + tenantId)).FirstOrDefault().Value.ToObject<Dictionary<string, string>>(); // 用户组件

        foreach (var items in oldDatas)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items.ToList()[i];

                if (vModels.Any(x => x.Key.Equals(item.Key)) && items[item.Key] != null)
                {
                    FieldsModel model = fields.Any(x => x.__vModel__.Equals(item.Key)) ? fields.Find(x => x.__vModel__.Equals(item.Key)) : (fields.Any(x => x.__vModel__.Equals(item.Key.Replace("_name", string.Empty))) ? fields.Find(x => x.__vModel__.Equals(item.Key.Replace("_name", string.Empty))) : new FieldsModel());
                    model.separator = ",";
                    var jnpfKey = vModels.FirstOrDefault(x => x.Key.Equals(item.Key)).Value;
                    switch (jnpfKey)
                    {
                        case JnpfKeyConst.USERSSELECT:
                            {
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                var itemValue = item.Value;
                                if (itemValue == null && items.ContainsKey(item.Key.Replace("_name", string.Empty))) itemValue = items[item.Key.Replace("_name", string.Empty)];
                                if (itemValue != null)
                                {
                                    var vList = new List<string>();
                                    if (itemValue.ToString().Contains("[")) vList = itemValue.ToString().ToObject<List<string>>();
                                    else vList.Add(itemValue.ToString());
                                    var itemValues = new List<string>();
                                    vList.ForEach(it =>
                                    {
                                        if (usersselectDatas.Any(x => x.Key.Contains(it))) itemValues.Add(usersselectDatas.First(x => x.Key.Contains(it)).Value);
                                    });
                                    if (itemValues.Any()) items[item.Key] = string.Join(",", itemValues);
                                }
                            }

                            break;
                        case JnpfKeyConst.POPUPSELECT: // 弹窗选择
                            {
                                if (model.interfaceId.IsNullOrEmpty()) continue;
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                List<Dictionary<string, string>> popupselectDataList = new List<Dictionary<string, string>>();

                                // 获取远端数据
                                var dynamic = await _dataInterfaceService.GetInfo(model.interfaceId);
                                var redisName = CommonConst.CodeGenDynamic + "_" + model.interfaceId + "_" + tenantId;
                                if (_cacheManager.Exists(redisName))
                                {
                                    popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                }
                                else
                                {
                                    popupselectDataList = await _formDataParsing.GetDynamicList(model);
                                    _cacheManager.Set(redisName, popupselectDataList.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                    popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                }

                                switch (dynamic.Type)
                                {
                                    case 1: // SQL数据
                                        {
                                            var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (specificData != null)
                                            {
                                                // 要用模板的 “显示字段 - relationField”来展示数据
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = specificData[model.relationField];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = specificData[fItem];
                                            }
                                        }
                                        break;
                                    case 2: // 静态数据
                                        {
                                            var vara = popupselectDataList.Where(a => a.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (vara != null)
                                            {
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = vara[items[item.Key].ToString()];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = vara[fItem];
                                            }
                                        }
                                        break;
                                    case 3: // Api数据
                                        {
                                            var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (specificData != null)
                                            {
                                                // 要用模板的 “显示字段 - relationField”来展示数据
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = specificData[model.relationField];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = specificData[fItem];
                                            }
                                        }
                                        break;
                                }
                            }

                            break;

                        case JnpfKeyConst.RELATIONFORM: // 关联表单
                            {
                                if (model.modelId.IsNullOrEmpty()) continue;
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                List<Dictionary<string, object>> relationFormDataList = new List<Dictionary<string, object>>();

                                var redisName = CommonConst.CodeGenDynamic + "_" + model.modelId + "_" + tenantId;
                                if (_cacheManager.Exists(redisName))
                                {
                                    relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                }
                                else
                                {
                                    // 根据可视化功能ID获取该模板全部数据
                                    var relationFormModel = await _repository.AsSugarClient().Queryable<VisualDevEntity>().FirstAsync(v => v.Id == model.modelId);
                                    var newFieLdsModelList = relationFormModel.FormData.ToObject<FormDataModel>().fields.FindAll(x => model.relationField.Equals(x.__vModel__));
                                    VisualDevModelListQueryInput listQueryInput = new VisualDevModelListQueryInput
                                    {
                                        dataType = "1",
                                        pageSize = 999999
                                    };

                                    Scoped.Create((_, scope) =>
                                    {
                                        var services = scope.ServiceProvider;
                                        var _runService = App.GetService<IRunService>(services);
                                        var res = _runService.GetRelationFormList(relationFormModel, listQueryInput).WaitAsync(TimeSpan.FromMinutes(2)).Result;
                                        _cacheManager.Set(redisName, res.list.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                    });
                                    var cacheStr = _cacheManager.Get(redisName);
                                    if (cacheStr.IsNotEmptyOrNull()) relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                }

                                var relationFormRealData = relationFormDataList.Where(it => it["id"].Equals(items[item.Key])).FirstOrDefault();
                                if (relationFormRealData != null && relationFormRealData.Count > 0)
                                {
                                    items[model.__vModel__ + "_id"] = relationFormRealData["id"];
                                    items[item.Key] = relationFormRealData.ContainsKey(model.relationField) ? relationFormRealData[model.relationField] : string.Empty;

                                    // 关联表单属性
                                    if (model.relational.IsNotEmptyOrNull())
                                        foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = relationFormRealData[fItem];
                                }
                                else
                                {
                                    items[item.Key] = string.Empty;
                                }
                            }

                            break;
                    }
                }

                // 子表
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctList = item.Value.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                    var ctVModels = new Dictionary<string, object>();
                    foreach (var ctItem in vModels.Where(x => x.Key.Contains(item.Key)).ToList())
                    {
                        ctVModels.Add(ctItem.Key.Split("-").LastOrDefault(), ctItem.Value);
                        var ctFields = fields.Find(x => x.__vModel__.Equals(ctItem.Key.Split("-").FirstOrDefault())).__config__.children;
                        if (ctList.Any()) items[item.Key] = await GetParsDataByList(ctFields, ctList, ctVModels, tenantId, isInlineEditor, userOrigin, true);
                    }
                }
            }
        }

        return oldDatas;
    }

    /// <summary>
    /// 获取解析数据缓存.
    /// </summary>
    /// <param name="vModels"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, object>> GetCaCheData(Dictionary<string, object> vModels, string tenantId)
    {
        var res = new Dictionary<string, object>();

        if (vModels.Where(x => x.Value.Equals(JnpfKeyConst.USERSSELECT)).Any())
        {
            string? userCacheKey = CommonConst.CodeGenDynamic + "_usersSelect_" + tenantId;
            if (_cacheManager.Exists(userCacheKey))
            {
                res.Add(userCacheKey, _cacheManager.Get(userCacheKey).ToObject<Dictionary<string, object>>());
            }
            else
            {
                var addList = new Dictionary<string, string>();
                (await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.RealName, x.Account }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--user", item.RealName ));
                (await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    addList.Add(item.Id + "--company", item.FullName);
                    addList.Add(item.Id + "--department", item.FullName);
                });
                (await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--role", item.FullName));
                (await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--position", item.FullName));
                (await _repository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--group", item.FullName));

                // 缓存5分钟
                _cacheManager.Set(userCacheKey, addList, TimeSpan.FromMinutes(5));
                res.Add(userCacheKey, addList);
            }
        }

        return res;
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, string values)
    {
        if (values.IsNotEmptyOrNull()) return GetUsersSelectQueryWhere(key, new List<string>() { values });
        else return new List<IConditionalModel>();
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, string values, bool isMultiple = false)
    {
        return GetUsersSelectQueryWhere(key, values);
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, List<string> values)
    {
        var conModels = new List<IConditionalModel>();
        if (values.IsNullOrEmpty() || !values.Any()) return conModels;
        values = values.Where(x => x != null).ToList();
        if (values.IsNullOrEmpty() || !values.Any()) return conModels;
        var userIds = values.Select(x => x.Replace("--user", string.Empty)).ToList();
        var rIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => userIds.Contains(x.UserId)).Select(x => new { x.ObjectId, x.ObjectType }).ToList();
        var objIdList = values;
        rIdList.ForEach(x =>
        {
            if (x.ObjectType.Equals("Organize"))
            {
                objIdList.Add(x.ObjectId + "--company");
                objIdList.Add(x.ObjectId + "--department");
            }
            else
            {
                objIdList.Add(x.ObjectId + "--" + x.ObjectType.ToLower());
            }
        });

        var whereList = new List<KeyValuePair<WhereType, ConditionalModel>>();
        for (var i = 0; i < objIdList.Count(); i++)
        {
            if (i == 0)
            {
                whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                {
                    FieldName = key,
                    ConditionalType = ConditionalType.Like,
                    FieldValue = objIdList[i]
                }));
            }
            else
            {
                whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                {
                    FieldName = key,
                    ConditionalType = ConditionalType.Like,
                    FieldValue = objIdList[i]
                }));
            }
        }

        whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
        {
            FieldName = key,
            ConditionalType = ConditionalType.IsNot,
            FieldValue = null
        }));
        whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
        {
            FieldName = key,
            ConditionalType = ConditionalType.IsNot,
            FieldValue = string.Empty
        }));
        conModels.Add(new ConditionalCollections() { ConditionalList = whereList });
        return conModels;
    }

    /// <summary>
    /// 生成查询多选条件.
    /// </summary>
    /// <param name="key">数据库列名称.</param>
    /// <param name="list"></param>
    /// <returns></returns>
    public List<IConditionalModel> GenerateMultipleSelectionCriteriaForQuerying(string key, List<string>? list)
    {
        var conModels = new List<IConditionalModel>();
        var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
        for (int i = 0; i < list?.Count; i++)
        {
            var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
            {
                FieldName = key,
                ConditionalType = ConditionalType.Like,
                FieldValue = list[i]
            });
            addItems.Add(add);
        }
        if (addItems?.Count > 0)
            conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
        return conModels;
    }

    #region 解析组织数据

    /// <summary>
    /// 所有组织.
    /// </summary>
    public List<OrganizeEntity> AllOrganizeList
    {
        get
        {
            if (_allOrganizeList == null) _allOrganizeList = GetOrgListTreeName();
            return _allOrganizeList;
        }
    }
    private List<OrganizeEntity> _allOrganizeList { get; set; }

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    private List<OrganizeEntity> GetOrgListTreeName()
    {
        List<OrganizeEntity>? orgTreeNameList = new List<OrganizeEntity>();

        string? userCacheKey = CommonConst.CodeGenDynamic + "_AllOrganize";
        if (_cacheManager.Exists(userCacheKey))
        {
            orgTreeNameList = _cacheManager.Get(userCacheKey).ToObject<List<OrganizeEntity>>();
        }
        else
        {
            orgTreeNameList = new List<OrganizeEntity>();
            List<OrganizeEntity>? orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToList();
            orgList.ForEach(item =>
            {
                if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
                OrganizeEntity? newItem = item.Adapt<OrganizeEntity>();
                newItem.Id = item.Id;
                var orgNameList = new List<string>();
                item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
                {
                    var org = orgList.Find(x => x.Id == it);
                    if (org != null) orgNameList.Add(org.FullName);
                });
                newItem.Description = string.Join("/", orgNameList);
                orgTreeNameList.Add(newItem);
            });

            // 缓存5分钟
            _cacheManager.Set(userCacheKey, orgTreeNameList, TimeSpan.FromMinutes(5));
        }

        return orgTreeNameList;
    }

    /// <summary>
    /// 解析所属组织.
    /// </summary>
    /// <param name="showLevel">部门 last(返回单个组织名称) , 组织 all(返回组织树名称).</param>
    /// <param name="oIds">组织Id</param>
    /// <returns></returns>
    public string GetCurrOrganizeName(string showLevel, string oIds)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            var idList = new List<string>();
            try
            {
                idList = oIds.ToObject<List<string>>();
            }
            catch
            {
                idList = oIds.ToString().ToObject<List<string>>();
            }
            if (showLevel.Equals("last"))
            {
                var oList = AllOrganizeList.Where(x => (idList.Contains(x.Id) || idList.Contains(x.EnCode)) && x.Category.Equals("department")).Select(x => x.FullName).ToList();
                res = oList.LastOrDefault();
            }
            else if (showLevel.Equals("all"))
            {
                var oList = AllOrganizeList.Where(x => idList.Contains(x.Id) || idList.Contains(x.EnCode)).Select(x => x.FullName).ToList();
                res = string.Join("/", oList);
            }
        }

        return res;
    }

    /// <summary>
    /// 解析组织.
    /// </summary>
    /// <param name="isMultiple">是否多选.</param>
    /// <param name="oIds">组织Id.</param>
    /// <returns></returns>
    public string GetOrganizeName(bool isMultiple, object oIds)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            if (isMultiple)
            {
                var idList = new List<List<string>>();
                try
                {
                    idList = oIds.ToObject<List<List<string>>>();
                }
                catch
                {
                    idList = oIds.ToString().ToObject<List<List<string>>>();
                }
                var resList = new List<string>();
                foreach (var item in idList) resList.Add(AllOrganizeList.Where(x => x.Id == item.LastOrDefault() || x.EnCode == item.LastOrDefault()).Select(x => x.Description).FirstOrDefault());
                res = string.Join(",", resList);
            }
            else
            {
                var idList = new List<string>();
                try
                {
                    idList = oIds.ToObject<List<string>>();
                }
                catch
                {
                    idList = oIds.ToString().ToObject<List<string>>();
                }
                res = AllOrganizeList.Where(x => x.Id == idList.LastOrDefault() || x.EnCode == idList.LastOrDefault()).Select(x => x.Description).FirstOrDefault();
            }
        }

        return res;
    }

    /// <summary>
    /// 解析部门.
    /// </summary>
    /// <param name="isMultiple">是否多选.</param>
    /// <param name="oIds">组织Id.</param>
    /// <param name="singleParsing">单选也解析.</param>
    /// <returns></returns>
    public string GetDepartmentName(bool isMultiple, object oIds, bool singleParsing = false)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            if (isMultiple)
            {
                var idList = new List<string>();
                try
                {
                    idList = oIds.ToObject<List<string>>();
                }
                catch
                {
                    idList = oIds.ToString().ToObject<List<string>>();
                }

                var resList = new List<string>();
                foreach (var item in idList) resList.Add(AllOrganizeList.Where(x => x.Id == item || x.EnCode == item).Select(x => x.FullName).FirstOrDefault());
                res = string.Join(",", resList);
            }
            else if (singleParsing)
            {
                res = AllOrganizeList.Where(x => x.Id == oIds.ToString() || x.EnCode == oIds.ToString()).Select(x => x.FullName).FirstOrDefault();
            }
            else
            {
                res = oIds.ToString();
            }
        }

        return res;
    }

    #endregion
}