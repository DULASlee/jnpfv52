using System.Diagnostics;
using JNPF.Common.Const;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Models.User;
using JNPF.Common.Net;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Entity.Permission;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SqlSugar;
using System.Security.Claims;

namespace JNPF.Common.Core.Manager;

/// <summary>
/// 当前登录用户.
/// </summary>
public class UserManager : IUserManager, IScoped
{
    /// <summary>
    /// 用户表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 当前Http请求.
    /// </summary>
    private readonly HttpContext _httpContext;

    /// <summary>
    /// 用户Claim主体.
    /// </summary>
    private readonly ClaimsPrincipal _user;

    /// <summary>
    /// 初始化一个<see cref="UserManager"/>类型的新实例.
    /// </summary>
    /// <param name="repository">用户仓储.</param>
    /// <param name="cacheManager">缓存管理.</param>
    public UserManager(
        ISqlSugarRepository<UserEntity> repository,
        ICacheManager cacheManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _httpContext = App.HttpContext;
        _user = _httpContext?.User;
    }

    /// <summary>
    /// 用户信息.
    /// </summary>
    public UserEntity User
    {
        get
        {
            if (_userEntity == null) _userEntity = _repository.GetSingle(u => u.Id == UserId);
            return _userEntity;
        }
    }
    private UserEntity _userEntity { get; set; }

    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId
    {
        get => _user.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
    }

    /// <summary>
    /// 获取用户角色.
    /// </summary>
    public List<string> Roles
    {
        get
        {
            if (_roles == null)
            {
                var user = _repository.GetSingle(u => u.Id == UserId);
                _roles = GetUserRoleIds(user.RoleId, user.OrganizeId);
            }

            return _roles;
        }
    }
    private List<string> _roles { get; set; }

    /// <summary>
    /// 用户权限组Ids.
    /// </summary>
    public List<string> PermissionGroup
    {
        get
        {
            if (_permissionGroup == null) _permissionGroup = GetPermissionGroupIds();
            return _permissionGroup;
        }
    }
    private List<string> _permissionGroup { get; set; }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单Ids.
    /// </summary>
    public List<string>? TenantIgnoreModuleIdList
    {
        get => GetGlobalTenantCache().Where(x => x.TenantId.Equals(TenantId)).FirstOrDefault()?.moduleIdList;
    }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单urlList.
    /// </summary>
    public List<string>? TenantIgnoreUrlAddressList
    {
        get
        {
            var urlAddressList = GetGlobalTenantCache().Where(x => x.TenantId.Equals(TenantId)).FirstOrDefault()?.urlAddressList;
            if (TenantIgnoreModuleIdList.IsNotEmptyOrNull() && urlAddressList == null) return new List<string>();
            return urlAddressList;
        }
    }

    /// <summary>
    /// 用户账号.
    /// </summary>
    public string Account
    {
        get => _user.FindFirst(ClaimConst.CLAINMACCOUNT)?.Value;
    }

    /// <summary>
    /// 用户昵称.
    /// </summary>
    public string RealName
    {
        get => _user.FindFirst(ClaimConst.CLAINMREALNAME)?.Value;
    }

    public string BizSystemId 
    {
        get
        {
            if (this._user == null) return "";

            if (this.UserId.IsNullOrEmpty()) return "";

            string configId = _repository.AsSugarClient().CurrentConnectionConfig.ConfigId.ToString();
            string systemId = _cacheManager.Get<string>(configId + this.UserId + "_devSystemId");
            if (systemId.IsNullOrEmpty()) 
            {
                systemId = this.User.SystemId;
                _cacheManager.Set(configId+this.UserId + "_devSystemId", systemId);
            }

            return systemId;
            //return _user.FindFirst(ClaimConst.ZXSYSTEMID)?.Value;
             //涉及切换问题，该信息是登陆时生成token，如果切换系统，token中的zxsystem并不会改变
        }
    }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId
    {
        get => _user.FindFirst(ClaimConst.TENANTID)?.Value;
    }

    /// <summary>
    /// 租户数据库名称.
    /// </summary>
    public string TenantDbName
    {
        get
        {
            var tenant = GetGlobalTenantCache(TenantId);
            if (tenant == null) return null;
            else return tenant.connectionConfig.ConfigList.FirstOrDefault().ServiceName;
        }
    }

    /// <summary>
    /// 当前用户 token.
    /// </summary>
    public string ToKen
    {
        get => string.IsNullOrEmpty(App.HttpContext?.Request.Headers["Authorization"]) ? App.HttpContext?.Request.Query["token"] : App.HttpContext?.Request.Headers["Authorization"];
    }

    /// <summary>
    /// 是否是管理员.
    /// </summary>
    public bool IsAdministrator
    {
        get => _user.FindFirst(ClaimConst.CLAINMADMINISTRATOR)?.Value == ((int)AccountType.Administrator).ToString();
    }

    /// <summary>
    /// 当前租户配置.
    /// </summary>
    public GlobalTenantCacheModel CurrentTenantInformation
    {
        get => GetGlobalTenantCache(TenantId);
    }

    /// <summary>
    /// 当前用户下属.
    /// </summary>
    public List<string> Subordinates
    {
        get
        {
            if (_subordinates == null) _subordinates = GetSubordinates(UserId).ToList();
            return _subordinates;
        }
    }
    private List<string> _subordinates { get; set; }

    /// <summary>
    /// 当前用户及下属.
    /// </summary>
    public List<string> CurrentUserAndSubordinates
    {
        get
        {
            if (_currentUserAndSubordinates == null)
            {
                _currentUserAndSubordinates = new List<string> { UserId };
                _currentUserAndSubordinates.AddRange(GetSubordinates(UserId).ToList());
            }
            return _currentUserAndSubordinates;
        }
    }
    private List<string> _currentUserAndSubordinates { get; set; }

    /// <summary>
    /// 当前组织及子组织.
    /// </summary>
    public List<string> CurrentOrganizationAndSubOrganizations
    {
        get
        {
            if (_currentOrganizationAndSubOrganizations == null)
            {
                _currentOrganizationAndSubOrganizations = new List<string> { User.OrganizeId };
                _currentOrganizationAndSubOrganizations.AddRange(GetSubsidiary(User.OrganizeId, IsAdministrator).ToObject<List<string>>());
            }
            return _currentOrganizationAndSubOrganizations;
        }
    }
    private List<string> _currentOrganizationAndSubOrganizations { get; set; }

    /// <summary>
    /// 当前用户子组织.
    /// </summary>
    public List<string> CurrentUserSubOrganization
    {
        get
        {
            return GetSubsidiary(User.OrganizeId, IsAdministrator).ToObject<List<string>>();
        }
    }

    /// <summary>
    /// 获取用户的数据范围.
    /// </summary>
    public List<UserDataScopeModel> DataScope
    {
        get
        {
            if (_dataScope == null) _dataScope = GetUserDataScope(UserId);
            return _dataScope;
        }
    }

    private List<UserDataScopeModel> _dataScope { get; set; }

    /// <summary>
    /// 获取请求端类型 pc 、 app.
    /// </summary>
    public string UserOrigin
    {
        get => _httpContext?.Request.Headers["jnpf-origin"] ?? "pc";
    }

    /// <summary>
    /// 获取请求vue版本
    /// 3-vue3,其他-vue2.
    /// </summary>
    public int VueVersion
    {

        get => (_httpContext?.Request.Headers["vue-version"].FirstOrDefault()).ParseToInt();
    }

    /// <summary>
    /// 获取公用菜单 编码 .
    /// </summary>
    /// <returns></returns>
    public List<string> CommonModuleEnCodeList
    {
        get
        {
            return new List<string>()
            {
                "workFlow.addFlow", "workFlow.flowLaunch", "workFlow.entrust", "workFlow", "workFlow.flowTodo","workFlow.flowDone","workFlow.flowCirculate"
            };
        }
    }

    /// <summary>
    /// 获取用户登录信息.
    /// </summary>
    /// <returns></returns>
    public async Task<UserInfoModel> GetUserInfo()
    {
        var __swTotal = Stopwatch.StartNew();
        UserAgent userAgent = new UserAgent(_httpContext);
        var data = new UserInfoModel();
        var userCache = string.Format("{0}:{1}:{2}", TenantId, CommonConst.CACHEKEYUSER, UserId);
        var __sw = Stopwatch.StartNew();
        var userDataScope = GetUserDataScope(UserId);
        Console.WriteLine($"[P0-1-TIMING] GetUserDataScope: {__sw.ElapsedMilliseconds}ms");

        var ipAddress = NetHelper.Ip;
        var ipAddressName = await NetHelper.GetLocation(ipAddress);
        var sysConfigInfo = await _repository.AsSugarClient().Queryable<SysConfigEntity>().FirstAsync(s => s.Category.Equals("SysConfig") && s.Key.ToLower().Equals("tokentimeout"));
        data = await _repository.AsQueryable().Where(it => it.Id == UserId)
           .Select(a => new UserInfoModel
           {
               userId = a.Id,
               headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
               userAccount = a.Account,
               userName = a.RealName,
               gender = a.Gender,
               organizeId = a.OrganizeId,
               departmentId = a.OrganizeId,
               departmentName = SqlFunc.Subqueryable<OrganizeEntity>().Where(o => o.Id == a.OrganizeId && o.Category.Equals("department")).Select(o => o.FullName),
               organizeName = SqlFunc.Subqueryable<OrganizeEntity>().Where(o => o.Id == a.OrganizeId).Select(o => o.OrganizeIdTree),
               managerId = a.ManagerId,
               isAdministrator = SqlFunc.IIF(a.IsAdministrator == 1, true, false),
               portalId = a.PortalId,
               positionId = a.PositionId,
               roleId = a.RoleId,
               prevLoginTime = a.PrevLogTime,
               prevLoginIPAddress = SqlFunc.IIF(a.PrevLogIP != null, a.PrevLogIP, "127.0.0.1"),
               landline = a.Landline,
               telePhone = a.TelePhone,
               manager = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.ManagerId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
               mobilePhone = a.MobilePhone,
               email = a.Email,
               birthday = a.Birthday,
               systemId = a.SystemId,
               appSystemId = a.AppSystemId,
               signImg = SqlFunc.Subqueryable<SignImgEntity>().Where(a => a.CreatorUserId == UserId && a.IsDefault == 1).Select(a => a.SignImg),
               changePasswordDate = a.ChangePasswordDate,
               loginTime = DateTime.Now,
           }).FirstAsync();

        if (data != null && data.organizeName.IsNotEmptyOrNull())
        {
            var orgIdTree = data?.organizeName?.Split(',');
            data.organizeIdList = orgIdTree.ToList();
            var organizeName = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => orgIdTree.Contains(x.Id)).OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).Select(x => x.FullName).ToListAsync();
            data.organizeName = string.Join("/", organizeName);
        }
        else
        {
            data.organizeName = data.departmentName;
        }
        data.prevLogin = (await _repository.AsSugarClient().Queryable<SysConfigEntity>().FirstAsync(x => x.Category.Equals("SysConfig") && x.Key.ToLower().Equals("lastlogintimeswitch"))).Value.ParseToInt();
        data.loginIPAddress = ipAddress;
        data.loginIPAddressName = ipAddressName;
        data.prevLoginIPAddressName = await NetHelper.GetLocation(data.prevLoginIPAddress);
        data.loginPlatForm = userAgent.RawValue;
        data.subsidiary = await GetSubsidiaryAsync(data.organizeId, data.isAdministrator);
        data.subordinates = await this.GetSubordinatesAsync(UserId);

        if (data.positionId.IsNotEmptyOrNull())
        {
            var positionIdList = await GetPosition(data.organizeId);
            if (positionIdList.Select(it => it.id).Contains(data.positionId))
            {
                var mainPosition = positionIdList.Find(it => it.id.Equals(data.positionId));
                positionIdList.Remove(mainPosition);
                positionIdList.Insert(0, mainPosition);
            }
            data.positionIds = positionIdList;
        }
        else
        {
            data.positionIds = new List<PositionInfoModel>();
        }

        data.positionName = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.Id.Equals(data.positionId)).Select(it => it.FullName).FirstAsync();

        var roleList = GetUserRoleIds(data.roleId, data.organizeId);
        __sw.Restart();
        data.roleName = await GetRoleNameByIds(string.Join(",", roleList));
        Console.WriteLine($"[P0-1-TIMING] GetRoleNameByIds: {__sw.ElapsedMilliseconds}ms");
        data.roleIds = roleList.ToArray();
        data.groupIds = await _repository.AsSugarClient().Queryable<GroupEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals("Group"))).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null && b.UserId.Equals(data.userId)).Select((a, b) => b.ObjectId).ToListAsync();
        data.groupNames = await _repository.AsSugarClient().Queryable<GroupEntity>().Where(it => data.groupIds.Contains(it.Id)).Select(x => x.FullName).ToListAsync();
        data.overdueTime = TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble());
        data.dataScope = userDataScope;
        data.tenantId = TenantId;

        var currSysId = UserOrigin.Equals("pc") ? User.SystemId : User.AppSystemId;
        data.workflowEnabled = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.Id.Equals(currSysId) && it.DeleteMark == null).Select(it => it.WorkflowEnabled).FirstAsync();

        // 根据系统配置过期时间自动过期
        await SetUserInfo(userCache, data, TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble()));

        __swTotal.Stop();
        Console.WriteLine($"[P0-1-TIMING] GetUserInfo total: {__swTotal.ElapsedMilliseconds}ms");

        return data;
    }

    public Loginer TheLoginer
    {
        get
        {
            return new Loginer()
            {
                DataSetID = "",
                DBName = "",
                Account = "",
                ID = this.UserId,
                AccountName = "王京",

            };
        }
    }
 

    /// <summary>
    /// 获取用户数据范围.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    private List<UserDataScopeModel> GetUserDataScope(string userId)
    {
        List<UserDataScopeModel> data = new List<UserDataScopeModel>();
        List<UserDataScopeModel> subData = new List<UserDataScopeModel>();
        List<UserDataScopeModel> inteList = new List<UserDataScopeModel>();

        // 一次性加载所有启用组织，避免循环内重复全表扫描
        var allOrganizes = _repository.AsSugarClient().Queryable<OrganizeEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1)).ToList();

        // 填充数据
        foreach (var item in _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>()
            .Where(it => it.UserId == userId && it.DeleteMark == null).ToList())
        {
            if (item.SubLayerSelect.ParseToBool() || item.SubLayerAdd.ParseToBool() || item.SubLayerEdit.ParseToBool() || item.SubLayerDelete.ParseToBool())
            {
                var subsidiary = GetSubsidiaryFromCache(allOrganizes, item.OrganizeId, false).ToList();
                subsidiary.Remove(item.OrganizeId);
                subsidiary.ToList().ForEach(it =>
                {
                    subData.Add(new UserDataScopeModel()
                    {
                        organizeId = it,
                        Add = item.SubLayerAdd.ParseToBool(),
                        Edit = item.SubLayerEdit.ParseToBool(),
                        Delete = item.SubLayerDelete.ParseToBool(),
                        Select = item.SubLayerSelect.ParseToBool()
                    });
                });
            }

            if (item.ThisLayerSelect.ParseToBool() || item.ThisLayerAdd.ParseToBool() || item.ThisLayerEdit.ParseToBool() || item.ThisLayerDelete.ParseToBool())
            {
                data.Add(new UserDataScopeModel()
                {
                    organizeId = item.OrganizeId,
                    organizeType = item.OrganizeType,
                    Add = item.ThisLayerAdd.ParseToBool(),
                    Edit = item.ThisLayerEdit.ParseToBool(),
                    Delete = item.ThisLayerDelete.ParseToBool(),
                    Select = item.ThisLayerSelect.ParseToBool()
                });
            }
        }

        /* 比较数据
        所有分级数据权限以本级权限为主 子级为辅
        将本级数据与子级数据对比 对比出子级数据内组织ID存在本级数据的组织ID*/
        var intersection = data.Select(it => it.organizeId).Intersect(subData.Select(it => it.organizeId)).ToList();
        intersection.ForEach(it =>
        {
            var parent = data.Find(item => item.organizeId == it);
            var child = subData.Find(item => item.organizeId == it);
            var add = false;
            var edit = false;
            var delete = false;
            var select = false;
            if (parent.Add || child.Add) add = true;
            if (parent.Edit || child.Edit) edit = true;
            if (parent.Delete || child.Delete) delete = true;
            if (parent.Select || child.Select) select = true;
            inteList.Add(new UserDataScopeModel()
            {
                organizeId = it,
                Add = add,
                Edit = edit,
                Delete = delete,
                Select = select
            });
            data.Remove(parent);
            subData.Remove(child);
        });
        return data.Union(subData).Union(inteList).ToList();
    }

    /// <summary>
    /// 获取数据条件.
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <param name="tableNumber">联表编号.</param>
    /// <returns></returns>
    public async Task<List<IConditionalModel>> GetConditionAsync<T>(string moduleId, string primaryKey = "f_id", bool isDataPermissions = true, string tableNumber = "")
        where T : new()
    {
        var conModels = new List<IConditionalModel>();
        if (IsAdministrator) return conModels;
        var dataScope = DataScope.Select(x => x.organizeId).ToList();
        if (_repository.AsSugarClient().Queryable<ModuleEntity>().Any(x => dataScope.Contains(x.SystemId) && x.Id.Equals(moduleId))) return conModels; // 分级管理全部放开
        var roles = PermissionGroup;
        var roleAuthorizeList = _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => roles.Contains(x.ObjectId) && x.ItemType == "resource").Select(a => new { a.ItemId, a.ObjectId }).ToList();
        if (!isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = string.Format("{0}{1}", tableNumber, primaryKey), ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
            return conModels;
        }
        else if (roleAuthorizeList.Count == 0 && isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = string.Format("{0}{1}", tableNumber, primaryKey), ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
            return conModels;
        }

        var resourceList = _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().In(it => it.Id, roleAuthorizeList.Select(x => x.ItemId).ToList()).Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToList();

        if (resourceList.Any(x => x.AllData == 1 || "jnpf_alldata".Equals(x.EnCode)))
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>() {
                            new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = string.Format("{0}{1}", tableNumber, primaryKey), ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                        }
            });
        }
        else
        {
            var allList = new List<object>(); // 构造任何层级的条件
            var resultList = new List<object>();
            foreach (var roleId in roles)
            {
                var isCurrentRole = true;
                var roleList = new List<object>();
                foreach (var item in resourceList.Where(x => roleAuthorizeList.Where(xx => xx.ObjectId.Equals(roleId)).Select(x => x.ItemId).Contains(x.Id)).ToList())
                {
                    var groupsList = new List<object>();
                    foreach (var conditionItem in item.ConditionJson.ToList<AuthorizeModuleResourceConditionModel>())
                    {
                        var conditionalList = new List<object>();
                        foreach (var fieldItem in conditionItem.Groups)
                        {
                            var itemField = string.Format("{0}{1}", tableNumber, fieldItem.Field);
                            var itemValue = fieldItem.Value;
                            fieldItem.Op = ReplaceOp(fieldItem.Op);
                            var itemMethod = (QueryType)System.Enum.Parse(typeof(QueryType), fieldItem.Op);

                            var cmodel = GetConditionalModel(itemMethod, itemField, User.OrganizeId);
                            if (itemMethod.Equals(QueryType.Equal)) cmodel.ConditionalType = ConditionalType.Like;
                            if (itemMethod.Equals(QueryType.NotEqual)) cmodel.ConditionalType = ConditionalType.NoLike;
                            switch (itemValue)
                            {
                                case "@userId": // 当前用户
                                    {
                                        switch (conditionItem.Logic)
                                        {
                                            case "and":
                                                conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = UserId, ConditionalType = (int)cmodel.ConditionalType } });

                                                break;
                                            case "or":
                                                conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = UserId, ConditionalType = (int)cmodel.ConditionalType } });

                                                break;
                                        }
                                    }

                                    break;
                                case "@userAraSubordinates": // 当前用户集下属
                                    {
                                        var ids = new List<string>() { UserId };
                                        ids.AddRange(Subordinates);
                                        for (int i = 0; i < ids.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                switch (conditionItem.Logic)
                                                {
                                                    case "and":
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                        break;
                                                    case "or":
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                else
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                            }
                                            isCurrentRole = false;
                                        }
                                    }

                                    break;
                                case "@organizeId": // 当前组织
                                    {
                                        if (!string.IsNullOrEmpty(User.OrganizeId))
                                        {
                                            switch (conditionItem.Logic)
                                            {
                                                case "and":
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = User.OrganizeId, ConditionalType = (int)cmodel.ConditionalType } });
                                                    break;
                                                case "or":
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = User.OrganizeId, ConditionalType = (int)cmodel.ConditionalType } });
                                                    break;
                                            }
                                        }
                                    }

                                    break;
                                case "@organizationAndSuborganization": // 当前组织及子组织
                                    {
                                        if (!string.IsNullOrEmpty(User.OrganizeId))
                                        {
                                            var ids = CurrentOrganizationAndSubOrganizations;
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                    }

                                    break;

                                case "@branchManageOrganize": // 当前分管组织
                                    {
                                        var ids = DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList();
                                        if (ids.Any())
                                        {
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                        else
                                        {
                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = "jnpf", ConditionalType = (int)ConditionalType.Equal } });
                                        }
                                    }

                                    break;

                                case "@branchManageOrganizeAndSub": // 当前分管组织及子组织
                                    {
                                        var ids = new List<string>();
                                        DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList()
                                            .ForEach(item => ids.AddRange(_repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(item)).Select(x => x.Id).ToList()));

                                        if (ids.Any())
                                        {
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                        else
                                        {
                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = "jnpf", ConditionalType = (int)ConditionalType.Equal } });
                                        }
                                    }

                                    break;

                                default:
                                    {
                                        if (itemValue.IsNotEmptyOrNull())
                                        {
                                            var defCmodel = GetConditionalModel(itemMethod, itemField, itemValue.ToString(), fieldItem.Type);
                                            if (defCmodel.ConditionalType.Equals(ConditionalType.In)) defCmodel.ConditionalType = ConditionalType.Like;
                                            if (defCmodel.ConditionalType.Equals(ConditionalType.NotIn)) defCmodel.ConditionalType = ConditionalType.NoLike;
                                            switch (conditionItem.Logic)
                                            {
                                                case "and":
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = (int)defCmodel.ConditionalType } });
                                                    break;
                                                case "or":
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = (int)defCmodel.ConditionalType } });
                                                    break;
                                            }
                                        }

                                    }

                                    break;
                            }
                            if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNullOrEmpty } });
                        }

                        if (conditionalList.Any())
                        {
                            var firstItem = conditionalList.First().ToObject<dynamic>();
                            firstItem.Key = 0;
                            conditionalList[0] = firstItem;
                            groupsList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = conditionalList } });
                        }
                    }

                    if (groupsList.Any()) roleList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = groupsList } });
                    isCurrentRole = false;
                }

                if (roleList.Any()) allList.Add(new { Key = (int)WhereType.Or, Value = new { ConditionalList = roleList } });
            }

            if (allList.Any()) resultList.Add(new { ConditionalList = allList });

            if (resultList.Any()) conModels.AddRange(_repository.AsSugarClient().Utilities.JsonToConditionalModels(resultList.ToJsonString()));
        }

        if (resourceList.Count == 0 || !Roles.Any())
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = string.Format("{0}{1}", tableNumber, primaryKey), ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
        }

        return conModels;
    }

    /// <summary>
    /// 获取数据条件.
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <returns></returns>
    public async Task<List<IConditionalModel>> GetDataConditionAsync<T>(string moduleId, string primaryKey, bool isDataPermissions = true)
        where T : new()
    {
        var conModels = new List<IConditionalModel>();
        if (IsAdministrator) return conModels;
        var dataScope = DataScope.Select(x => x.organizeId).ToList();
        if (_repository.AsSugarClient().Queryable<ModuleEntity>().Any(x => dataScope.Contains(x.SystemId) && x.Id.Equals(moduleId))) return conModels; // 分级管理全部放开
        var roles = PermissionGroup;
        var roleAuthorizeList = _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => roles.Contains(x.ObjectId) && x.ItemType == "resource").Select(a => new { a.ItemId, a.ObjectId }).ToList();
        if (!isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
            return conModels;
        }
        else if (roleAuthorizeList.Count == 0 && isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
            return conModels;
        }

        var resourceList = _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().In(it => it.Id, roleAuthorizeList).Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToList();

        if (resourceList.Any(x => x.AllData == 1 || x.EnCode.Equals("jnpf_alldata")))
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>() {
                            new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                        }
            });
        }
        else
        {
            var allList = new List<object>(); // 构造任何层级的条件
            var resultList = new List<object>();
            foreach (var roleId in roles)
            {
                var isCurrentRole = true;
                var roleList = new List<object>();
                foreach (var item in resourceList)
                {
                    var groupsList = new List<object>();
                    foreach (var conditionItem in item.ConditionJson.ToList<AuthorizeModuleResourceConditionModel>())
                    {
                        var conditionalList = new List<object>();
                        foreach (var fieldItem in conditionItem.Groups)
                        {
                            var itemField = string.IsNullOrEmpty(fieldItem.BindTable) ? fieldItem.Field : string.Format("{0}.{1}", fieldItem.BindTable, fieldItem.Field);
                            var itemValue = fieldItem.Value;
                            fieldItem.Op = ReplaceOp(fieldItem.Op);
                            var itemMethod = (QueryType)System.Enum.Parse(typeof(QueryType), fieldItem.Op);

                            var cmodel = GetConditionalModel(itemMethod, itemField, User.OrganizeId);
                            if (itemMethod.Equals(QueryType.Equal)) cmodel.ConditionalType = ConditionalType.Like;
                            if (itemMethod.Equals(QueryType.NotEqual)) cmodel.ConditionalType = ConditionalType.NoLike;
                            switch (itemValue)
                            {
                                case "@userId": // 当前用户
                                    {
                                        switch (conditionItem.Logic)
                                        {
                                            case "and":
                                                conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = UserId, ConditionalType = (int)cmodel.ConditionalType } });

                                                break;
                                            case "or":
                                                conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = UserId, ConditionalType = (int)cmodel.ConditionalType } });

                                                break;
                                        }
                                    }

                                    break;
                                case "@userAraSubordinates": // 当前用户集下属
                                    {
                                        var ids = new List<string>() { UserId };
                                        ids.AddRange(Subordinates);
                                        for (int i = 0; i < ids.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                switch (conditionItem.Logic)
                                                {
                                                    case "and":
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                        break;
                                                    case "or":
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                else
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                            }
                                            isCurrentRole = false;
                                        }
                                    }

                                    break;
                                case "@organizeId": // 当前组织
                                    {
                                        if (!string.IsNullOrEmpty(User.OrganizeId))
                                        {
                                            switch (conditionItem.Logic)
                                            {
                                                case "and":
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = User.OrganizeId, ConditionalType = (int)cmodel.ConditionalType } });
                                                    break;
                                                case "or":
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = User.OrganizeId, ConditionalType = (int)cmodel.ConditionalType } });
                                                    break;
                                            }
                                        }
                                    }

                                    break;
                                case "@organizationAndSuborganization": // 当前组织及子组织
                                    {
                                        if (!string.IsNullOrEmpty(User.OrganizeId))
                                        {
                                            var ids = CurrentOrganizationAndSubOrganizations;
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                    }

                                    break;

                                case "@branchManageOrganize": // 当前分管组织
                                    {
                                        var ids = DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList();
                                        if (ids.Any())
                                        {
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                        else
                                        {
                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = "jnpf", ConditionalType = (int)ConditionalType.Equal } });
                                        }
                                    }

                                    break;

                                case "@branchManageOrganizeAndSub": // 当前分管组织及子组织
                                    {
                                        var ids = new List<string>();
                                        DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList()
                                            .ForEach(item => ids.AddRange(_repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(item)).Select(x => x.Id).ToList()));

                                        if (ids.Any())
                                        {
                                            for (int i = 0; i < ids.Count; i++)
                                            {
                                                if (i == 0)
                                                {
                                                    switch (conditionItem.Logic)
                                                    {
                                                        case "and":
                                                            conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                        case "or":
                                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });

                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                                        conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                    else
                                                        conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = ids[i], ConditionalType = (int)cmodel.ConditionalType } });
                                                }
                                                isCurrentRole = false;
                                            }
                                        }
                                        else
                                        {
                                            conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = "jnpf", ConditionalType = (int)ConditionalType.Equal } });
                                        }
                                    }

                                    break;

                                default:
                                    {
                                        if (itemValue.IsNotEmptyOrNull())
                                        {
                                            var defCmodel = GetConditionalModel(itemMethod, itemField, itemValue.ToString(), fieldItem.Type);
                                            if (defCmodel.ConditionalType.Equals(ConditionalType.In)) defCmodel.ConditionalType = ConditionalType.Like;
                                            if (defCmodel.ConditionalType.Equals(ConditionalType.NotIn)) defCmodel.ConditionalType = ConditionalType.NoLike;
                                            switch (conditionItem.Logic)
                                            {
                                                case "and":
                                                    conditionalList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = (int)defCmodel.ConditionalType } });
                                                    break;
                                                case "or":
                                                    conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = (int)defCmodel.ConditionalType } });
                                                    break;
                                            }
                                        }

                                    }

                                    break;
                            }
                            if (itemMethod.Equals(QueryType.NotEqual) || itemMethod.Equals(QueryType.NotIncluded))
                                conditionalList.Add(new { Key = (int)WhereType.Or, Value = new { FieldName = itemField, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNullOrEmpty } });
                        }

                        if (conditionalList.Any())
                        {
                            var firstItem = conditionalList.First().ToObject<dynamic>();
                            firstItem.Key = 0;
                            conditionalList[0] = firstItem;
                            groupsList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = conditionalList } });
                        }
                    }

                    if (groupsList.Any()) roleList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = groupsList } });
                    isCurrentRole = false;
                }

                if (roleList.Any()) allList.Add(new { Key = (int)WhereType.Or, Value = new { ConditionalList = roleList } });
            }

            if (allList.Any()) resultList.Add(new { ConditionalList = allList });

            if (resultList.Any()) conModels.AddRange(_repository.AsSugarClient().Utilities.JsonToConditionalModels(resultList.ToJsonString()));
        }

        if (resourceList.Count == 0)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
            });
        }

        return conModels;
    }

    /// <summary>
    /// 获取代码生成数据条件 .
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <returns></returns>
    public async Task<List<CodeGenAuthorizeModuleResourceModel>> GetCodeGenAuthorizeModuleResource<T>(string moduleId, string primaryKey, int primaryKeyPolicy, bool isDataPermissions = true)
        where T : new()
    {
        var codeGenConditional = new List<CodeGenAuthorizeModuleResourceModel>();

        // 获取所有数据权限的 表名
        var resourceList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToListAsync();

        var allTableName = new List<string>();
        foreach (var resourceItem in resourceList)
        {
            if (resourceItem != null && resourceItem.ConditionJson != null && resourceItem.ConditionJson.Any())
            {
                var items = resourceItem.ConditionJson.ToList<AuthorizeModuleResourceConditionModel>();
                items.ForEach(it => allTableName.AddRange(it.Groups.Select(x => x.BindTable)));
            }
        }

        var condList = await GetCondition<object>(primaryKey, moduleId, isDataPermissions, primaryKeyPolicy.Equals(2));

        var minTable = GetIConditionalModelListByTableName(JsonConvert.DeserializeObject<List<IConditionalModel>>(JsonConvert.SerializeObject(condList)), null);

        if (minTable.Any())
        {
            codeGenConditional.Add(new CodeGenAuthorizeModuleResourceModel()
            {
                conditionalModel = minTable,
                TableName = string.Empty,
                FieldRule = 0
            });
        }

        foreach (var tName in allTableName.Distinct().ToList())
        {
            var tNameConditional = GetIConditionalModelListByTableName(JsonConvert.DeserializeObject<List<IConditionalModel>>(JsonConvert.SerializeObject(condList)), tName);

            if (tNameConditional.Any())
            {
                codeGenConditional.Add(new CodeGenAuthorizeModuleResourceModel()
                {
                    conditionalModel = tNameConditional,
                    TableName = tName,
                    FieldRule = -1
                });
            }
        }

        return codeGenConditional;
    }
    private List<IConditionalModel> GetIConditionalModelListByTableName(List<IConditionalModel> cList, string tableName)
    {
        for (int i = 0; i < cList.Count; i++)
        {
            if (cList[i] is ConditionalTree)
            {
                var newItem = (ConditionalTree)cList[i];
                for (int j = 0; j < newItem.ConditionalList.Count; j++)
                {
                    var value = GetIConditionalModelListByTableName(new List<IConditionalModel>() { newItem.ConditionalList[j].Value }, tableName);
                    if (value != null && value.Any())
                    {
                        if (newItem.ConditionalList[j].Equals(newItem.ConditionalList.FirstOrDefault()))
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(newItem.ConditionalList[j].Key, value.First());
                        else
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(newItem.ConditionalList[j].Key, value.First());
                    }
                    else
                    {
                        newItem.ConditionalList.RemoveAt(j);
                        j--;
                    }
                }

                if (newItem.ConditionalList.Any())
                {
                    cList[i] = newItem;
                }
                else
                {
                    cList.RemoveAt(i);
                    i--;
                }
            }
            else if (cList[i] is ConditionalCollections)
            {
                var newItemList = (ConditionalCollections)cList[i];

                for (int j = 0; j < newItemList.ConditionalList.Count; j++)
                {
                    if ((tableName.IsNullOrEmpty() && newItemList.ConditionalList[j].Value.FieldName.Contains(".")) || tableName.IsNotEmptyOrNull() && !newItemList.ConditionalList[j].Value.FieldName.Contains(tableName + "."))
                    {
                        newItemList.ConditionalList.RemoveAt(j);
                    }
                    else
                    {
                        newItemList.ConditionalList[j].Value.FieldName = newItemList.ConditionalList[j].Value.FieldName.Split(".").Last();
                    }
                }
                if (newItemList.ConditionalList.Any()) cList[i] = newItemList;
                else cList.RemoveAt(i);
            }
            else if (cList[i] is ConditionalModel)
            {
                var newItem = (ConditionalModel)cList[i];
                if ((tableName.IsNullOrEmpty() && newItem.FieldName.Contains(".")) || tableName.IsNotEmptyOrNull() && !newItem.FieldName.Contains(tableName + "."))
                {
                    cList.RemoveAt(i);
                }
                else
                {
                    newItem.FieldName = newItem.FieldName.Split(".").Last();
                    cList[i] = newItem;
                }
            }
        }

        return cList;
    }

    /// <summary>
    /// 获取代码生成数据条件.
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <returns></returns>
    public async Task<List<CodeGenAuthorizeModuleResourceModel>> GetCodeGenAuthorizeModuleResource<T>(string moduleId, string primaryKey, bool isDataPermissions = true)
        where T : new()
    {
        var codeGenConditional = new List<CodeGenAuthorizeModuleResourceModel>()
        {
            new CodeGenAuthorizeModuleResourceModel
            {
                FieldRule = 0,
                conditionalModel = new List<IConditionalModel>()
            }
        };
        if (IsAdministrator) return codeGenConditional; // 管理员全部放开
        var dataScope = DataScope.Select(x => x.organizeId).ToList();
        if (_repository.AsSugarClient().Queryable<ModuleEntity>().Any(x => dataScope.Contains(x.SystemId) && x.Id.Equals(moduleId))) return codeGenConditional; // 分级管理全部放开

        var roles = PermissionGroup;
        var items = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => roles.Contains(x.ObjectId) && x.ItemType == "resource")
            .GroupBy(x => new { x.ItemId }).Select(a => a.ItemId).ToListAsync();

        switch (isDataPermissions)
        {
            case true:
                // 开启权限 但是没有权限资源.
                switch (items.Count)
                {
                    case 0:
                        codeGenConditional.Find(it => it.FieldRule.Equals(0)).conditionalModel.Add(new ConditionalCollections()
                        {
                            ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                            {
                                new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                            }
                        });
                        break;
                }

                break;
            default:
                // 未开启数据权限
                codeGenConditional.Find(it => it.FieldRule.Equals(0)).conditionalModel.Add(new ConditionalCollections()
                {
                    ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
                });
                break;
        }

        var resourceList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().In(it => it.Id, items).Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToListAsync();

        // 权限资源是否为全部数据.
        switch (resourceList?.Any(x => x.AllData == 1 || x.EnCode.Equals("jnpf_alldata")))
        {
            case true:
                codeGenConditional.Find(it => it.FieldRule.Equals(0)).conditionalModel.Add(new ConditionalCollections()
                {
                    ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                            new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                    }
                });
                break;
            case false:
                switch (resourceList.Count)
                {
                    case 0:
                        codeGenConditional.Find(it => it.FieldRule.Equals(0)).conditionalModel.Add(new ConditionalCollections()
                        {
                            ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                            {
                                new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.Equal, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) })
                            }
                        });
                        break;
                    default:
                        codeGenConditional = new List<CodeGenAuthorizeModuleResourceModel>();

                        var allList = new List<object>(); // 构造任何层级的条件
                        var resultList = new List<object>();
                        var codeGenConditionalObject = new List<CodeGenAuthorizeModuleResource>();
                        foreach (var item in resourceList)
                        {
                            var groupsList = new List<object>();
                            var fieldRule = 0;
                            var tableName = string.Empty;
                            foreach (var conditionItem in item.ConditionJson.ToList<AuthorizeModuleResourceConditionModel>())
                            {
                                var conditionalList = new List<object>();
                                foreach (var fieldItem in conditionItem.Groups)
                                {
                                    fieldRule = fieldItem.FieldRule;
                                    tableName = string.IsNullOrEmpty(fieldItem.BindTable) ? fieldItem.Field.Split('.').First() : fieldItem.BindTable;
                                    if (!codeGenConditionalObject.Any(it => it.FieldRule == fieldRule && it.TableName == tableName))
                                    {
                                        codeGenConditionalObject.Add(new CodeGenAuthorizeModuleResource()
                                        {
                                            FieldRule = fieldRule,
                                            TableName = tableName,
                                            conditionalModel = new List<object>()
                                        });
                                    }

                                    var itemField = fieldRule == 0 ? fieldItem.Field : (string.IsNullOrEmpty(fieldItem.BindTable) ? fieldItem.Field.Split('.').Last() : fieldItem.Field);
                                    var itemValue = new object();
                                    switch (fieldItem.Value)
                                    {
                                        case "@userId":
                                            itemValue = UserId;
                                            break;
                                        case "@userAraSubordinates":
                                            itemValue = CurrentUserAndSubordinates.ToJsonString();
                                            break;
                                        case "@organizeId":
                                            var organizeTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                                .Where(it => it.Id.Equals(User.OrganizeId))
                                                .Select(it => it.OrganizeIdTree)
                                                .FirstAsync();
                                            if (organizeTree.IsNotEmptyOrNull())
                                                itemValue = organizeTree.Split(",").ToJsonString();
                                            break;
                                        case "@organizationAndSuborganization":
                                            var oList = new List<string>();
                                            foreach (var organizeId in CurrentOrganizationAndSubOrganizations)
                                            {
                                                var oTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                                    .Where(it => it.Id.Equals(organizeId))
                                                    .Select(it => it.OrganizeIdTree)
                                                    .FirstAsync();
                                                if (oTree.IsNotEmptyOrNull())
                                                    oList.Add(oTree.Split(",").ToJsonString());
                                            }
                                            itemValue = oList.ToJsonString();
                                            break;
                                        case "@branchManageOrganize":
                                            var bList = new List<string>();
                                            var dataScopeList = DataScope.Select(x => x.organizeId).ToList();
                                            if (dataScopeList.Any())
                                            {
                                                foreach (var organizeId in dataScopeList)
                                                {
                                                    var oTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                                        .Where(it => it.Id.Equals(organizeId))
                                                        .Select(it => it.OrganizeIdTree)
                                                        .FirstAsync();
                                                    if (oTree.IsNotEmptyOrNull())
                                                        bList.Add(oTree.Split(",").ToJsonString());
                                                }
                                                itemValue = bList.ToJsonString();
                                            }
                                            else
                                            {
                                                //分管组织为什么要这个？加上这个只有没有分管会导致所有数据查不了
                                                itemValue = "jnpfNullList";
                                            }
                                            break;
                                        default:
                                            if (fieldItem.Value.IsNotEmptyOrNull() && fieldItem.Value.ToString().Contains("["))
                                                itemValue = fieldItem.Value.ToString().Replace("\r\n", "").Replace(" ", "");
                                            else
                                                itemValue = fieldItem.Value;
                                            break;
                                    }

                                    fieldItem.Op = ReplaceOp(fieldItem.Op);
                                    var itemMethod = (QueryType)System.Enum.Parse(typeof(QueryType), fieldItem.Op);
                                    var cmodel = GetConditionalModel(itemMethod, itemField, User.OrganizeId);

                                    var between = new List<string>();
                                    if (itemMethod.Equals(QueryType.Between))
                                        between = itemValue.ToString().ToObject<List<string>>();

                                    if (itemValue.IsNotEmptyOrNull())
                                    {
                                        switch (fieldItem.Type)
                                        {
                                            case "datetime":
                                                if (itemMethod.Equals(QueryType.Between))
                                                {
                                                    var startTime = between[0].TimeStampToDateTime();
                                                    var endTime = between[1].TimeStampToDateTime();
                                                    between[0] = startTime.ToString();
                                                    between[1] = endTime.ToString();
                                                }
                                                else
                                                {
                                                    itemValue = itemValue.ToString().TimeStampToDateTime().ToString();
                                                }
                                                break;
                                        }
                                    }

                                    switch (itemMethod)
                                    {
                                        case QueryType.Equal:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                                            break;
                                        case QueryType.NotEqual:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoEqual } });
                                            break;
                                        case QueryType.Included:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Like } });
                                            break;
                                        case QueryType.NotIncluded:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoLike } });
                                            break;
                                        case QueryType.GreaterThan:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThan } });
                                            break;
                                        case QueryType.GreaterThanOrEqual:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThanOrEqual } });
                                            break;
                                        case QueryType.LessThan:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThan } });
                                            break;
                                        case QueryType.LessThanOrEqual:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThanOrEqual } });
                                            break;
                                        case QueryType.Between:
                                            if (between.IsNotEmptyOrNull())
                                            {
                                                conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[0], ConditionalType = ConditionalType.GreaterThanOrEqual } });
                                                conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[1], ConditionalType = ConditionalType.LessThanOrEqual } });
                                                continue;
                                            }
                                            break;
                                        case QueryType.Null:
                                            if (fieldItem.Type.Equals("double") || fieldItem.Type.Equals("int") || fieldItem.Type.Equals("bigint"))
                                                conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.EqualNull } });
                                            else
                                                conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNullOrEmpty } });
                                            break;
                                        case QueryType.NotNull:
                                            conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNot } });
                                            break;
                                        case QueryType.In:
                                        case QueryType.NotIn:
                                            if (itemValue != null && itemValue.ToString().Contains("["))
                                            {
                                                var ids = new List<string>();
                                                if (itemValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[["))
                                                {
                                                    foreach (var valueList in itemValue.ToString().ToObject<List<List<string>>>())
                                                    {
                                                        var id = valueList.ToJsonString();
                                                        ids.Add(id);
                                                    }
                                                }
                                                else
                                                {
                                                    ids = itemValue.ToString().ToObject<List<string>>();
                                                }

                                                for (var i = 0; i < ids.Count; i++)
                                                {
                                                    var it = ids[i];
                                                    var conditionWhereType = WhereType.And;
                                                    if (itemMethod.Equals(QueryType.In)) conditionWhereType = i.Equals(0) && conditionItem.Logic.Equals("and") ? WhereType.And : WhereType.Or;
                                                    else conditionWhereType = i.Equals(0) && conditionItem.Logic.Equals("or") ? WhereType.Or : WhereType.And;

                                                    conditionalList.Add(new { Key = (int)conditionWhereType, Value = new { FieldName = itemField, FieldValue = it, ConditionalType = itemMethod.Equals(QueryType.In) ? ConditionalType.Like : ConditionalType.NoLike } });
                                                }

                                                if (itemMethod.Equals(QueryType.NotIn))
                                                {
                                                    conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = "null", ConditionalType = ConditionalType.IsNot } });
                                                    conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNot } });
                                                }

                                                continue;
                                            }
                                            else
                                            {
                                                conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                                            }
                                            break;
                                    }

                                    codeGenConditionalObject.Find(it => it.FieldRule == fieldRule && it.TableName.Equals(tableName)).conditionalModel.AddRange(conditionalList);
                                }

                                if (codeGenConditionalObject.Any())
                                {
                                    var firstItem = codeGenConditionalObject.Find(it => it.FieldRule.Equals(fieldRule) && it.TableName.Equals(tableName)).conditionalModel.First().ToObject<dynamic>();
                                    firstItem.Key = 0;
                                    conditionalList[0] = firstItem;
                                    groupsList.Add(new { Key = (int)WhereType.And, Value = new { ConditionalList = codeGenConditionalObject.Find(it => it.FieldRule == fieldRule && it.TableName.Equals(tableName)).conditionalModel } });
                                    codeGenConditionalObject.Find(it => it.FieldRule.Equals(fieldRule) && it.TableName.Equals(tableName)).conditionalModel = groupsList;
                                    groupsList = new List<object>();
                                }
                            }

                            if (codeGenConditionalObject.Any())
                            {
                                allList.Add(new { Key = (int)WhereType.And, Value = new { ConditionalList = codeGenConditionalObject.Find(it => it.FieldRule.Equals(fieldRule) && it.TableName.Equals(tableName)).conditionalModel } });
                                codeGenConditionalObject.Find(it => it.FieldRule.Equals(fieldRule) && it.TableName.Equals(tableName)).conditionalModel = allList;
                                allList = new List<object>();
                            }
                        }

                        if (codeGenConditionalObject.Any())
                        {
                            foreach (var conditional in codeGenConditionalObject)
                            {
                                resultList.Add(new { ConditionalList = conditional.conditionalModel });
                                conditional.conditionalModel = resultList;
                                resultList = new List<object>();
                            }
                        }

                        if (codeGenConditionalObject.Any())
                        {
                            foreach (var conditional in codeGenConditionalObject)
                            {
                                if (!codeGenConditional.Any(it => it.FieldRule == conditional.FieldRule && it.TableName.Equals(conditional.TableName)))
                                {
                                    codeGenConditional.Add(new CodeGenAuthorizeModuleResourceModel
                                    {
                                        FieldRule = conditional.FieldRule,
                                        TableName = conditional.TableName,
                                        conditionalModel = new List<IConditionalModel>()
                                    });
                                }
                                codeGenConditional.Find(it => it.FieldRule.Equals(conditional.FieldRule) && it.TableName.Equals(conditional.TableName)).conditionalModel = _repository.AsSugarClient().Utilities.JsonToConditionalModels(conditional.conditionalModel.ToJsonString());
                            }
                        }
                        break;
                }
                break;
        }

        return codeGenConditional.FindAll(it => it.conditionalModel.Count > 0);
    }

    /// <summary>
    /// 获取数据条件(在线开发专用) .
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <returns></returns>
    public async Task<List<IConditionalModel>> GetCondition<T>(string primaryKey, string moduleId, bool isDataPermissions, bool primaryKeyPolicy)
        where T : new()
    {
        var primaryWhere = new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(string)) });
        if (primaryKeyPolicy) primaryWhere = new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) });

        var conModels = new List<IConditionalModel>();
        if (IsAdministrator) return conModels; // 管理员全部放开
        var dataScope = DataScope.Select(x => x.organizeId).ToList();
        if (_repository.AsSugarClient().Queryable<ModuleEntity>().Any(x => dataScope.Contains(x.SystemId) && x.Id.Equals(moduleId))) return conModels; // 分级管理全部放开

        var roles = PermissionGroup;
        var roleAuthorizeList = _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => roles.Contains(x.ObjectId) && x.ItemType == "resource").Select(a => new { a.ItemId, a.ObjectId }).ToList();

        if (!isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                {
                    primaryWhere
                }
            });
            return conModels;
        }
        else if (roleAuthorizeList.Count == 0 && isDataPermissions)
        {
            primaryWhere.Value.ConditionalType = ConditionalType.Equal;
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                {
                    primaryWhere
                }
            });
            return conModels;
        }

        var resourceList = _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().In(it => it.Id, roleAuthorizeList.Select(x => x.ItemId).ToList()).Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToList();

        if (resourceList.Any(x => x.AllData == 1 || x.EnCode.Equals("jnpf_alldata")))
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>() {
                    primaryWhere
                }
            });
        }
        else
        {
            var allList = new List<object>(); // 构造任何层级的条件
            var resultList = new List<object>();
            foreach (var roleId in PermissionGroup)
            {
                var isCurrentRole = true;
                var roleList = new List<object>();
                foreach (var item in resourceList.Where(x => roleAuthorizeList.Where(xx => xx.ObjectId.Equals(roleId)).Select(x => x.ItemId).Contains(x.Id)).ToList())
                {
                    var conditionItemWhere = item.MatchLogic;
                    var groupsList = new List<object>();
                    foreach (var conditionItem in item.ConditionJson.ToList<AuthorizeModuleResourceConditionModel>())
                    {
                        var conditionalList = new List<object>();
                        foreach (var fieldItem in conditionItem.Groups)
                        {
                            var itemField = fieldItem.BindTable.IsNullOrWhiteSpace() ? fieldItem.Field : string.Format("{0}.{1}", fieldItem.BindTable, fieldItem.Field);
                            var itemValue = new object();
                            switch (fieldItem.Value)
                            {
                                case "@userId":
                                    itemValue = UserId;
                                    break;
                                case "@userAraSubordinates":
                                    itemValue = CurrentUserAndSubordinates.ToJsonString();
                                    break;
                                case "@organizeId":
                                    // todo 生产环境查询有问题。
                                    var organizeTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                        .Where(it => it.Id.Equals(User.OrganizeId))
                                        .Select(it => it.OrganizeIdTree)
                                        .FirstAsync();
                                    if (organizeTree.IsNotEmptyOrNull())
                                    {
                                        itemValue = Newtonsoft.Json.JsonConvert.SerializeObject(organizeTree.Split(","));
                                    }
                                    break;
                                case "@organizationAndSuborganization":
                                    var oList = new List<string>();
                                    foreach (var organizeId in CurrentOrganizationAndSubOrganizations)
                                    {
                                        var oTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                            .Where(it => it.Id.Equals(organizeId))
                                            .Select(it => it.OrganizeIdTree)
                                            .FirstAsync();
                                        if (oTree.IsNotEmptyOrNull())
                                            oList.Add(oTree.Split(",").ToJsonString());
                                    }
                                    itemValue = oList.ToJsonString();
                                    break;
                                case "@branchManageOrganize":
                                    var bList = new List<string>();
                                    var dataScopeList = DataScope.Select(x => x.organizeId).ToList();
                                    if (dataScopeList.Any())
                                    {
                                        foreach (var organizeId in dataScopeList)
                                        {
                                            var oTree = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                                .Where(it => it.Id.Equals(organizeId))
                                                .Select(it => it.OrganizeIdTree)
                                                .FirstAsync();
                                            if (oTree.IsNotEmptyOrNull())
                                                bList.Add(oTree.Split(",").ToJsonString());
                                        }
                                        itemValue = bList.ToJsonString();
                                    }
                                    else
                                    {
                                        //分管组织为什么要这个？加上这个只有没有分管会导致所有数据查不了
                                        itemValue = "jnpfNullList";
                                    }
                                    break;
                                default:
                                    if (fieldItem.Value.IsNotEmptyOrNull() && fieldItem.Value.ToString().Contains("["))
                                        itemValue = fieldItem.Value.ToString().Replace("\r\n", "").Replace(" ", "");
                                    else
                                        itemValue = fieldItem.Value;
                                    break;
                            }
                            fieldItem.Op = ReplaceOp(fieldItem.Op);
                            var itemMethod = (QueryType)System.Enum.Parse(typeof(QueryType), fieldItem.Op);

                            var cmodel = GetConditionalModel(itemMethod, itemField, User.OrganizeId);

                            var between = new List<string>();
                            if (itemMethod.Equals(QueryType.Between))
                                between = itemValue.ToString().ToObject<List<string>>();

                            if (itemValue.IsNotEmptyOrNull())
                            {
                                switch (fieldItem.Type)
                                {
                                    case "datetime":
                                        if (itemMethod.Equals(QueryType.Between))
                                        {
                                            var startTime = between[0].TimeStampToDateTime();
                                            var endTime = between[1].TimeStampToDateTime();
                                            between[0] = startTime.ToString();
                                            between[1] = endTime.ToString();
                                        }
                                        else
                                        {
                                            itemValue = itemValue.ToString().TimeStampToDateTime().ToString();
                                        }
                                        break;
                                }
                            }

                            switch (itemMethod)
                            {
                                case QueryType.Equal:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                                    break;
                                case QueryType.NotEqual:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoEqual } });
                                    break;
                                case QueryType.Included:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Like } });
                                    break;
                                case QueryType.NotIncluded:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.NoLike } });
                                    break;
                                case QueryType.GreaterThan:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThan } });
                                    break;
                                case QueryType.GreaterThanOrEqual:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.GreaterThanOrEqual } });
                                    break;
                                case QueryType.LessThan:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThan } });
                                    break;
                                case QueryType.LessThanOrEqual:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.LessThanOrEqual } });
                                    break;
                                case QueryType.Between:
                                    if (between.IsNotEmptyOrNull())
                                    {
                                        conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[0], ConditionalType = ConditionalType.GreaterThanOrEqual } });
                                        conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = between[1], ConditionalType = ConditionalType.LessThanOrEqual } });
                                        continue;
                                    }
                                    break;
                                case QueryType.Null:
                                    if (fieldItem.Type.Equals("double") || fieldItem.Type.Equals("int") || fieldItem.Type.Equals("bigint"))
                                        conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.EqualNull } });
                                    else
                                        conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNullOrEmpty } });
                                    break;
                                case QueryType.NotNull:
                                    conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.IsNot } });
                                    break;
                                case QueryType.In:
                                case QueryType.NotIn:
                                    if (itemValue != null && itemValue.ToString().Contains('['))
                                    {
                                        var ids = new List<string>();
                                        foreach (var valueList in itemValue.ToString().ToObject<List<string>>())
                                        {
                                            if (valueList.Contains('['))
                                            {
                                                var value = valueList.ToObject<List<string>>();
                                                ids.AddRange(value);
                                            }
                                            else
                                            {
                                                ids.Add(valueList);
                                            }
                                        }

                                        for (var i = 0; i < ids.Count; i++)
                                        {
                                            var it = ids[i];
                                            var conditionWhereType = WhereType.And;
                                            if (itemMethod.Equals(QueryType.In)) conditionWhereType = i.Equals(0) && conditionItem.Logic.Equals("and") ? WhereType.And : WhereType.Or;
                                            else conditionWhereType = i.Equals(0) && conditionItem.Logic.Equals("or") ? WhereType.Or : WhereType.And;

                                            conditionalList.Add(new { Key = (int)conditionWhereType, Value = new { FieldName = itemField, FieldValue = it, ConditionalType = itemMethod.Equals(QueryType.In) ? ConditionalType.Like : ConditionalType.NoLike } });
                                        }

                                        if (itemMethod.Equals(QueryType.NotIn))
                                        {
                                            conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = "null", ConditionalType = ConditionalType.IsNot } });
                                            conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNot } });
                                        }

                                        continue;
                                    }
                                    else
                                    {
                                        conditionalList.Add(new { Key = conditionItem.Logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { FieldName = itemField, FieldValue = itemValue, ConditionalType = ConditionalType.Equal } });
                                    }
                                    break;
                            }
                        }

                        if (conditionalList.Any())
                        {
                            var firstItem = conditionalList.First().ToObject<dynamic>();
                            firstItem.Key = 0;
                            conditionalList[0] = firstItem;
                            groupsList.Add(new { Key = conditionItemWhere.Equals("or") ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = conditionalList } });
                        }
                    }

                    if (groupsList.Any()) roleList.Add(new { Key = isCurrentRole ? (int)WhereType.Or : (int)WhereType.And, Value = new { ConditionalList = groupsList } });
                    isCurrentRole = false;
                }

                if (roleList.Any()) allList.Add(new { Key = (int)WhereType.Or, Value = new { ConditionalList = roleList } });
            }

            if (allList.Any()) resultList.Add(new { ConditionalList = allList });

            if (resultList.Any()) conModels.AddRange(_repository.AsSugarClient().Utilities.JsonToConditionalModels(resultList.ToJsonString()));
        }

        if (resourceList.Count == 0)
        {
            primaryWhere.Value.ConditionalType = ConditionalType.Equal;
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        primaryWhere
                    }
            });
        }

        return conModels;
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="isAdmin">是否管理员.</param>
    /// <returns></returns>
    private async Task<string[]> GetSubsidiaryAsync(string organizeId, bool isAdmin)
    {
        var data = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1)).ToListAsync();
        if (!isAdmin)
            data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);

        return data.Select(m => m.Id).ToArray();
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="isAdmin">是否管理员.</param>
    /// <returns></returns>
    private string[] GetSubsidiary(string organizeId, bool isAdmin)
    {
        var data = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1)).ToList();
        if (!isAdmin)
            data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);

        return data.Select(m => m.Id).ToArray();
    }

    /// <summary>
    /// 从已加载的组织列表中获取下属机构（避免重复查库）.
    /// </summary>
    private string[] GetSubsidiaryFromCache(List<OrganizeEntity> allOrganizes, string organizeId, bool isAdmin)
    {
        var data = allOrganizes;
        if (!isAdmin)
            data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);

        return data.Select(m => m.Id).ToArray();
    }

    /// <summary>
    /// 获取下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <returns></returns>
    private async Task<string[]> GetSubordinatesAsync(string managerId)
    {
        List<string> data = new List<string>();
        var userIds = await _repository.AsQueryable().Where(m => m.ManagerId == managerId && m.DeleteMark == null).Select(m => m.Id).ToListAsync();
        data.AddRange(userIds);

        // 关闭无限级我的下属
        // data.AddRange(await GetInfiniteSubordinats(userIds.ToArray()));
        return data.ToArray();
    }

    /// <summary>
    /// 获取下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <returns></returns>
    private string[] GetSubordinates(string managerId)
    {
        List<string> data = new List<string>();
        var userIds = _repository.AsQueryable().Where(m => m.ManagerId == managerId && m.DeleteMark == null).Select(m => m.Id).ToList();
        data.AddRange(userIds);

        // 关闭无限级我的下属
        // data.AddRange(await GetInfiniteSubordinats(userIds.ToArray()));
        return data.ToArray();
    }

    /// <summary>
    /// 获取下属无限极.
    /// </summary>
    /// <param name="parentIds"></param>
    /// <returns></returns>
    private async Task<List<string>> GetInfiniteSubordinats(string[] parentIds)
    {
        List<string> data = new List<string>();
        if (parentIds.ToList().Count > 0)
        {
            var userIds = await _repository.AsQueryable().In(it => it.ManagerId, parentIds).Where(it => it.DeleteMark == null).OrderBy(it => it.SortCode).Select(it => it.Id).ToListAsync();
            data.AddRange(userIds);
            data.AddRange(await GetInfiniteSubordinats(userIds.ToArray()));
        }

        return data;
    }

    /// <summary>
    /// 获取当前用户岗位信息.
    /// </summary>
    /// <param name="PositionIds"></param>
    /// <returns></returns>
    private async Task<List<PositionInfoModel>> GetPosition(string organizeId)
    {
        return await _repository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals("Position"))).Where((a, b) => a.OrganizeId.Equals(organizeId) && b.UserId.Equals(UserId)).Select(a => new PositionInfoModel { id = a.Id, name = a.FullName }).ToListAsync();
    }

    /// <summary>
    /// 获取条件模型.
    /// </summary>
    /// <returns></returns>
    private ConditionalModel GetConditionalModel(QueryType expressType, string fieldName, string fieldValue, string dataType = "string")
    {
        switch (expressType)
        {
            // 模糊
            case QueryType.Contains:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Like, FieldValue = fieldValue };

            // 等于
            case QueryType.Equal:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue };
                }

            // 不等于
            case QueryType.NotEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue };
                }

            // 小于
            case QueryType.LessThan:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue };
                }

            // 小于等于
            case QueryType.LessThanOrEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue };
                }

            // 大于
            case QueryType.GreaterThan:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue };
                }

            // 大于等于
            case QueryType.GreaterThanOrEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue };
                }

            // 包含
            case QueryType.In:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.In, FieldValue = fieldValue };
            case QueryType.Included:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Like, FieldValue = fieldValue };
            // 不包含
            case QueryType.NotIn:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NotIn, FieldValue = fieldValue };
            case QueryType.NotIncluded:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoLike, FieldValue = fieldValue };
        }

        return new ConditionalModel();
    }

    /// <summary>
    /// 获取角色名称 根据 角色Ids.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<string> GetRoleNameByIds(string ids)
    {
        if (ids.IsNullOrEmpty())
            return string.Empty;

        var idList = ids.Split(",").ToList();
        var nameList = await _repository.AsSugarClient().Queryable<RoleEntity>()
            .Where(x => idList.Contains(x.Id) && x.DeleteMark == null && x.EnabledMark == 1)
            .Select(x => x.FullName).ToListAsync();

        return string.Join(",", nameList);
    }

    /// <summary>
    /// 根据角色Ids和组织Id 获取组织下的角色以及全局角色.
    /// </summary>
    /// <param name="roleIds">角色Id集合.</param>
    /// <param name="organizeId">组织Id.</param>
    /// <returns></returns>
    public List<string> GetUserRoleIds(string roleIds, string organizeId)
    {
        if (roleIds.IsNotEmptyOrNull())
        {
            var userRoleIds = roleIds.Split(",");

            // 当前组织下的角色Id 集合
            var roleList = _repository.AsSugarClient().Queryable<OrganizeRelationEntity>()
                .Where(x => x.OrganizeId == organizeId && x.ObjectType == "Role" && userRoleIds.Contains(x.ObjectId)).Select(x => x.ObjectId).ToList();

            // 全局角色Id 集合
            var gRoleList = _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => userRoleIds.Contains(x.Id) && x.GlobalMark == 1)
                .Where(r => r.EnabledMark == 1 && r.DeleteMark == null).Select(x => x.Id).ToList();

            roleList.AddRange(gRoleList); // 组织角色 + 全局角色

            return roleList;
        }
        else
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 用户权限组Ids.
    /// </summary>
    /// <returns></returns>
    public List<string> GetPermissionGroupIds()
    {
        var res = GetPermissionByCurrentOrgId(UserId, User.OrganizeId);

        // 如果当前组织没有任何权限组 则切换所属组织
        if (!DataScope.Any(x => x.organizeType.IsNotEmptyOrNull()) && (!res.Any() || !_repository.AsSugarClient().Queryable<AuthorizeEntity>().Any(a => res.Contains(a.ObjectId) && a.ItemType == "system")))
        {
            var orgIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.UserId.Equals(UserId) && x.ObjectType.Equals("Organize") && !x.ObjectId.Equals(User.OrganizeId)).Select(x => x.ObjectId).ToList();
            if (orgIds != null && orgIds.Any())
            {
                foreach (var item in orgIds)
                {
                    res = GetPermissionByCurrentOrgId(UserId, item);
                    if (res.Any() && _repository.AsSugarClient().Queryable<AuthorizeEntity>().Any(a => res.Contains(a.ObjectId) && a.ItemType == "system"))
                    {
                        _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => x.OrganizeId == item).Where(x => x.Id.Equals(UserId)).ExecuteCommand();

                        // 获取切换组织 Id 下的所有岗位
                        var pList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.OrganizeId == item).Select(x => x.Id).ToList();

                        // 获取切换组织的 岗位，如果该组织没有岗位则为空
                        var idList = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                            .Where(x => x.UserId == UserId && pList.Contains(x.ObjectId) && x.ObjectType == "Position").Select(x => x.ObjectId).ToList();
                        User.PositionId = idList.FirstOrDefault() == null ? string.Empty : idList.FirstOrDefault();
                        break;
                    }
                }
            }
        }

        return res;
    }
    public List<string> GetPermissionByCurrentOrgId(string userId, string orgId)
    {
        // 当前用户所属组织下的 部门、角色、岗位
        var orgIdList = new List<string>() { orgId };
        var posIdList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => orgIdList.Contains(x.OrganizeId) && x.DeleteMark == null).Select(x => x.Id).ToList();
        var roleIdList = _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => orgIdList.Contains(x.OrganizeId) && x.ObjectType.Equals("Role")).Select(x => x.ObjectId).ToList();
        var groupIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals("Group")).Select(x => x.ObjectId).ToList();
        orgIdList.AddRange(posIdList);
        orgIdList.AddRange(roleIdList);
        orgIdList.AddRange(groupIdList);
        var objIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgIdList.Contains(x.ObjectId) && x.UserId.Equals(userId)).Select(x => x.ObjectId).ToList();
        var roleGMIds = _repository.AsSugarClient().Queryable<UserRelationEntity, RoleEntity>((u, r) => new JoinQueryInfos(JoinType.Left, u.ObjectId == r.Id && r.DeleteMark == null))
            .Where((u, r) => u.UserId.Equals(userId) && u.ObjectType.Equals("Role") && r.GlobalMark.Equals(1)).Select((u, r) => u.ObjectId).ToList();
        objIdList.AddRange(roleGMIds);
        objIdList.Add(userId);

        // 查询业务平台权限
        var querList = LinqExpression.Or<PermissionGroupEntity>();
        objIdList.ForEach(item => querList = querList.Or(x => x.PermissionMember.Contains(item)));
        return _repository.AsSugarClient().Queryable<PermissionGroupEntity>().Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1)).Where(querList).Select(x => x.Id).ToList();
    }

    /// <summary>
    /// 获取当前用户所有 权限组.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<string> GetPermissionByUserId(string userId)
    {
        // 当前用户所属组织下的 部门、角色、岗位
        var objIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(userId)).Select(x => x.ObjectId).ToList();
        objIdList.Add(userId);

        // 查询业务平台权限
        var querList = LinqExpression.Or<PermissionGroupEntity>();
        objIdList.ForEach(item => querList = querList.Or(x => x.PermissionMember.Contains(item)));
        return _repository.AsSugarClient().Queryable<PermissionGroupEntity>().Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1)).Where(querList).Select(x => x.Id).ToList();
    }

    /// <summary>
    /// 获取用户已授权的资源ID集合，用于路由级权限匹配.
    /// 查询 BASE_AUTHORIZE WHERE ObjectId IN (用户角色IDs + 用户ID).
    /// </summary>
    public async Task<HashSet<string>> GetAuthorizedResourceIdsAsync(string userId)
    {
        var objectIds = new List<string>(PermissionGroup ?? new List<string>()) { userId };
        objectIds = objectIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

        if (objectIds.Count == 0)
            return new HashSet<string>();

        var itemIds = await _repository.AsSugarClient()
            .Queryable<AuthorizeEntity>()
            .Where(a => objectIds.Contains(a.ObjectId)
                && (a.ItemType == "module" || a.ItemType == "button" || a.ItemType == "system"))
            .Select(a => a.ItemId)
            .ToListAsync();

        return new HashSet<string>(itemIds.Where(id => !string.IsNullOrEmpty(id)));
    }

    /// <summary>
    /// 会否存在用户缓存.
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <returns></returns>
    private async Task<bool> ExistsUserInfo(string cacheKey)
    {
        return await _cacheManager.ExistsAsync(cacheKey);
    }

    /// <summary>
    /// 保存用户登录信息.
    /// </summary>
    /// <param name="cacheKey">key.</param>
    /// <param name="userInfo">用户信息.</param>
    /// <param name="timeSpan">过期时间.</param>
    /// <returns></returns>
    private async Task<bool> SetUserInfo(string cacheKey, UserInfoModel userInfo, TimeSpan timeSpan)
    {
        return await _cacheManager.SetAsync(cacheKey, userInfo, timeSpan);
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

    /// <summary>
    /// 获取用户登录信息.
    /// </summary>
    /// <param name="cacheKey">key.</param>
    /// <returns></returns>
    private async Task<UserInfoModel> GetUserInfo(string cacheKey)
    {
        return (await _cacheManager.GetAsync(cacheKey)).Adapt<UserInfoModel>();
    }

    /// <summary>
    /// 获取用户名称.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否带账号.</param>
    /// <returns></returns>
    public string GetUserName(string userId, bool isAccount = true)
    {
        UserEntity? entity = _repository.GetFirst(x => x.Id == userId && x.DeleteMark == null);
        if (entity.IsNullOrEmpty()) return string.Empty;
        return isAccount ? entity.RealName + "/" + entity.Account : entity.RealName;
    }

    /// <summary>
    /// 获取用户名称.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否带账号.</param>
    /// <returns></returns>
    public async Task<string> GetUserNameAsync(string userId, bool isAccount = true)
    {
        UserEntity? entity = await _repository.GetFirstAsync(x => x.Id == userId && x.DeleteMark == null);
        if (entity.IsNullOrEmpty()) return string.Empty;
        return isAccount ? entity.RealName + "/" + entity.Account : entity.RealName;
    }

    /// <summary>
    /// 获取管理员用户id.
    /// </summary>
    public string GetAdminUserId()
    {
        var user = _repository.AsSugarClient().Queryable<UserEntity>().First(x => x.Account == "admin" && x.DeleteMark == null);
        if (user.IsNotEmptyOrNull()) return user.Id;
        return string.Empty;
    }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    public List<GlobalTenantCacheModel> GetGlobalTenantCache()
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        var list = _cacheManager.Get<List<GlobalTenantCacheModel>>(cacheKey);
        return list != null ? list : new List<GlobalTenantCacheModel>();
    }

    /// <summary>
    /// 转换条件符号.
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    private string ReplaceOp(string op)
    {
        switch (op)
        {
            case "==":
                op = "Equal";
                break;
            case "between":
                op = "Between";
                break;
            case ">":
                op = "GreaterThan";
                break;
            case "<":
                op = "LessThan";
                break;
            case "<>":
                op = "NotEqual";
                break;
            case ">=":
                op = "GreaterThanOrEqual";
                break;
            case "<=":
                op = "LessThanOrEqual";
                break;
            case "like":
                op = "Included";
                break;
            case "notLike":
                op = "NotIncluded";
                break;
            case "in":
                op = "In";
                break;
            case "notIn":
                op = "NotIn";
                break;
            case "null":
                op = "Null";
                break;
            case "notNull":
                op = "NotNull";
                break;

        }
        return op;
    }
}