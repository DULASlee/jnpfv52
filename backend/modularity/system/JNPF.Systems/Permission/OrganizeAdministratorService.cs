using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.OrganizeAdministrator;
using JNPF.Systems.Entitys.Dto.Permission.OrganizeAdministrator;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 分级管理
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021.09.27.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "OrganizeAdministrator", Order = 166)]
[Route("api/permission/[controller]")]
public class OrganizeAdministratorService : IOrganizeAdministratorService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储 .
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeAdministratorEntity> _repository;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="OrganizeAdministratorService"/>类型的新实例.
    /// </summary>
    public OrganizeAdministratorService(
        ISqlSugarRepository<OrganizeAdministratorEntity> organizeAdministratorRepository,
        IUserManager userManager,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IOrganizeService organizeService)
    {
        _repository = organizeAdministratorRepository;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _organizeService = organizeService;
    }

    #region GET

    /// <summary>
    /// 拉取机构分级管理.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        // 处理组织树 名称
        List<OrganizeEntity>? orgTreeNameList = GetOrgListTreeName();

        // 获取有查看权限的组织
        var dataScope = _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>()
            .Where(it => it.UserId == _userManager.UserId && it.DeleteMark == null)
            .Where(it => it.ThisLayerSelect.Equals(1) || it.ThisLayerSelect.Equals(1) || it.SubLayerSelect.Equals(1)).ToList();

        // 性别字典类型
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);

        List<string>? orgIds = dataScope.Where(x => x.ThisLayerSelect.Equals(1)).Select(x => x.OrganizeId).ToList();
        if (dataScope.Any(x => x.SubLayerSelect.Equals(1)))
        {
            dataScope.Where(x => x.SubLayerSelect.Equals(1)).ToList().ForEach(item =>
            {
                var resList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => !o.Id.Equals(item.OrganizeId) && o.OrganizeIdTree.Contains(item.OrganizeId) && o.DeleteMark == null && o.EnabledMark == 1).Select(x => x.Id).ToList();
                orgIds.AddRange(resList);
            });
        }

        var userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals("Organize") && orgIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
        var organizeAdmin = await _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => !x.UserId.Equals(_userManager.UserId))
            .WhereIF(!_userManager.IsAdministrator, x => orgIds.Contains(x.OrganizeId) && userIdList.Contains(x.UserId))
            .Select(x => new { x.UserId, x.CreatorTime, x.ManagerGroup }).ToListAsync();

        var data = await _repository.AsSugarClient().Queryable<UserEntity>()
        .Where(x => organizeAdmin.Select(xx => xx.UserId).Contains(x.Id) && x.DeleteMark == null)
        .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.Account.Contains(input.keyword) || x.RealName.Contains(input.keyword) || x.MobilePhone.Contains(input.keyword))
        .Select(x => new OrganizeAdministratorListOutput()
        {
            id = x.Id,
            account = x.Account,
            gender = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == x.Gender).Select(d => d.FullName),
            realName = x.RealName,
            mobilePhone = x.MobilePhone,
            creatorTime = x.CreatorTime
        }).ToPagedListAsync(input.currentPage, input.pageSize);

        List<UserRelationEntity>? orgUserIdAll = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType.Equals("Organize") && data.list.Select(x => x.id).Contains(x.UserId)).ToListAsync();

        foreach (var item in data.list)
        {
            // 获取用户组织集合
            List<string>? roleOrgList = orgUserIdAll.Where(x => x.UserId == item.id).Select(x => x.ObjectId).ToList();
            item.organizeId = string.Join(" , ", orgTreeNameList.Where(x => roleOrgList.Contains(x.Id)).Select(x => x.Description));
            item.creatorTime = organizeAdmin.OrderBy(x => x.CreatorTime).ToList().Find(x => x.UserId.Equals(item.id)).CreatorTime;
            item.managerGroup = organizeAdmin.ToList().Find(x => x.UserId.Equals(item.id)).ManagerGroup;
        }

        data.list = data.list.OrderByDescending(x => x.creatorTime).ToList();
        return PageResult<OrganizeAdministratorListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector(string userId)
    {
        if (userId.IsNullOrEmpty()) return new OrganizeAdminSystemModuleSelectorOutput() { moduleIds = new List<string>(), systemIds = new List<string>() };
        if (userId.Equals(_userManager.UserId)) throw Oops.Oh(ErrorCode.D2304);
        if (await _repository.AsSugarClient().Queryable<UserEntity>().AnyAsync(x => x.Id.Equals(userId) && x.IsAdministrator.Equals(1))) throw Oops.Oh(ErrorCode.D2305);

        // 处理组织树 名称
        List<OrganizeEntity> orgTreeNameList = GetOrgListTreeName();

        // 获取当前有权限的组织
        var currList = await _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => orgTreeNameList.Select(xx => xx.Id).Contains(x.OrganizeId)).ToListAsync();
        var result = new List<OrganizeAdministratorSelectorOutput>();

        var managerGroup = await _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => x.UserId.Equals(userId)).Select(x => x.ManagerGroup).FirstAsync();

        // 管理员
        if (_userManager.IsAdministrator)
        {
            orgTreeNameList.ForEach(orgItem =>
            {
                var addItem = new OrganizeAdministratorSelectorOutput();

                addItem.id = orgItem.Id;
                addItem.fullName = orgItem.FullName;
                addItem.organizeId = orgItem.Id;
                addItem.parentId = orgItem.ParentId;
                addItem.category = orgItem.Category;
                addItem.icon = orgItem.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1";
                addItem.organizeIdTree = orgItem.OrganizeIdTree;

                var item = currList.FirstOrDefault(x => x.OrganizeId.Equals(orgItem.Id) && x.UserId.Equals(userId));
                if (item != null && userId != _userManager.UserId)
                {
                    addItem.thisLayerSelect = item.ThisLayerSelect;
                    addItem.thisLayerAdd = item.ThisLayerAdd;
                    addItem.thisLayerEdit = item.ThisLayerEdit;
                    addItem.thisLayerDelete = item.ThisLayerDelete;
                    addItem.subLayerSelect = item.SubLayerSelect;
                    addItem.subLayerAdd = item.SubLayerAdd;
                    addItem.subLayerEdit = item.SubLayerEdit;
                    addItem.subLayerDelete = item.SubLayerDelete;
                }
                else
                {
                    addItem.thisLayerSelect = 0;
                    addItem.thisLayerAdd = 0;
                    addItem.thisLayerEdit = 0;
                    addItem.thisLayerDelete = 0;
                    addItem.subLayerSelect = 0;
                    addItem.subLayerAdd = 0;
                    addItem.subLayerEdit = 0;
                    addItem.subLayerDelete = 0;
                }

                result.Add(addItem);
            });

            var resultTree = result.ToTree("-1");
            resultTree.ToObject<List<Dictionary<string, object>>>().ForEach(item =>
            {
                if (item.ContainsValue(-1))
                    foreach (var key in item.Where(x => x.Value.Equals(-1))) item.Remove(key.Key);
            });

            var resTree = await GetSystemAndModuleListByUserId(userId);
            resTree.orgAdminList = resultTree;
            resTree.managerGroup = managerGroup;

            return resTree;
        }

        currList = currList.WhereIF(userId.Equals(_userManager.UserId), x => x.UserId.Equals(_userManager.UserId))
            .WhereIF(!userId.Equals(_userManager.UserId), x => x.UserId == _userManager.UserId || x.UserId == userId).ToList();

        // 捞取编辑用户权限
        var userList = currList.Where(x => x.ThisLayerAdd.Equals(1) || x.ThisLayerEdit.Equals(1) || x.ThisLayerDelete.Equals(1) || x.ThisLayerSelect.Equals(1) ||
          x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).Where(x => x.UserId.Equals(userId)).ToList().Copy();
        userList.Where(x => x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).ToList().ForEach(item =>
        {
            var orgIds = orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId)).Select(x => x.Id).ToList();
            var subList = currList.Where(x => orgIds.Contains(x.OrganizeId) && x.UserId.Equals(_userManager.UserId)).ToList().Copy();

            subList.ForEach(it =>
            {
                var userIt = userList.Find(x => x.OrganizeId.Equals(it.OrganizeId));
                if (userIt == null)
                {
                    it.ThisLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                }
                else
                {
                    it.ThisLayerAdd = userIt.ThisLayerAdd.Equals(1) || userIt.ThisLayerAdd.Equals(3) ? userIt.ThisLayerAdd : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = userIt.ThisLayerEdit.Equals(1) || userIt.ThisLayerEdit.Equals(3) ? userIt.ThisLayerEdit : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = userIt.ThisLayerDelete.Equals(1) || userIt.ThisLayerDelete.Equals(3) ? userIt.ThisLayerDelete : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = userIt.ThisLayerSelect.Equals(1) || userIt.ThisLayerSelect.Equals(3) ? userIt.ThisLayerSelect : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = userIt.SubLayerAdd.Equals(1) || userIt.SubLayerAdd.Equals(3) ? userIt.SubLayerAdd : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = userIt.SubLayerEdit.Equals(1) || userIt.SubLayerEdit.Equals(3) ? userIt.SubLayerEdit : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = userIt.SubLayerDelete.Equals(1) || userIt.SubLayerDelete.Equals(3) ? userIt.SubLayerDelete : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = userIt.SubLayerSelect.Equals(1) || userIt.SubLayerSelect.Equals(3) ? userIt.SubLayerSelect : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    userList.Remove(userIt);
                }
            });

            userList.AddRange(subList);
        });

        // 捞取管理员权限
        var adminList = currList.Where(x => x.ThisLayerAdd.Equals(1) || x.ThisLayerEdit.Equals(1) || x.ThisLayerDelete.Equals(1) || x.ThisLayerSelect.Equals(1) ||
           x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).Where(x => x.UserId.Equals(_userManager.UserId)).ToList().Copy();
        adminList.Where(x => x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).ToList().ForEach(item =>
        {
            var orgIds = orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId)).Select(x => x.Id).ToList();
            var subList = currList.Where(x => orgIds.Contains(x.OrganizeId) && x.UserId.Equals(_userManager.UserId)).ToList().Copy();

            subList.ForEach(it =>
            {
                var adminIt = adminList.Find(x => x.OrganizeId.Equals(it.OrganizeId));
                if (adminIt == null)
                {
                    it.ThisLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                }
                else
                {
                    it.ThisLayerAdd = adminIt.ThisLayerAdd.Equals(1) || adminIt.ThisLayerAdd.Equals(3) ? adminIt.ThisLayerAdd : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = adminIt.ThisLayerEdit.Equals(1) || adminIt.ThisLayerEdit.Equals(3) ? adminIt.ThisLayerEdit : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = adminIt.ThisLayerDelete.Equals(1) || adminIt.ThisLayerDelete.Equals(3) ? adminIt.ThisLayerDelete : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = adminIt.ThisLayerSelect.Equals(1) || adminIt.ThisLayerSelect.Equals(3) ? adminIt.ThisLayerSelect : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = adminIt.SubLayerAdd.Equals(1) || adminIt.SubLayerAdd.Equals(3) ? adminIt.SubLayerAdd : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = adminIt.SubLayerEdit.Equals(1) || adminIt.SubLayerEdit.Equals(3) ? adminIt.SubLayerEdit : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = adminIt.SubLayerDelete.Equals(1) || adminIt.SubLayerDelete.Equals(3) ? adminIt.SubLayerDelete : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = adminIt.SubLayerSelect.Equals(1) || adminIt.SubLayerSelect.Equals(3) ? adminIt.SubLayerSelect : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    adminList.Remove(adminIt);
                }
            });

            adminList.AddRange(subList);
        });

        adminList.ForEach(item =>
        {
            var user = userList.Find(x => x.OrganizeId.Equals(item.OrganizeId));
            var orgItem = orgTreeNameList.Find(x => x.Id.Equals(item.OrganizeId));
            var resultItem = new OrganizeAdministratorSelectorOutput();
            resultItem.id = orgItem.Id;
            resultItem.organizeId = orgItem.Id;
            resultItem.fullName = orgItem.Description;
            resultItem.parentId = orgItem.ParentId;
            resultItem.category = orgItem.Category;
            resultItem.icon = orgItem.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1";
            resultItem.organizeIdTree = orgItem.OrganizeIdTree;

            if (user != null && userId != _userManager.UserId)
            {
                if (item.ThisLayerAdd.Equals(0) && user.ThisLayerAdd.Equals(0)) resultItem.thisLayerAdd = -1;
                else if ((item.ThisLayerAdd.Equals(1) || item.ThisLayerAdd.Equals(3)) && user.ThisLayerAdd.Equals(1)) resultItem.thisLayerAdd = 1;
                else if ((item.ThisLayerAdd.Equals(1) || item.ThisLayerAdd.Equals(3)) && user.ThisLayerAdd.Equals(0)) resultItem.thisLayerAdd = 0;
                else if (item.ThisLayerAdd.Equals(0) && user.ThisLayerAdd.Equals(1)) resultItem.thisLayerAdd = 2;
                else if (item.ThisLayerAdd.Equals(0) && user.ThisLayerAdd.Equals(3)) resultItem.thisLayerAdd = 3;

                if (item.ThisLayerEdit.Equals(0) && user.ThisLayerEdit.Equals(0)) resultItem.thisLayerEdit = -1;
                else if ((item.ThisLayerEdit.Equals(1) || item.ThisLayerEdit.Equals(3)) && user.ThisLayerEdit.Equals(1)) resultItem.thisLayerEdit = 1;
                else if ((item.ThisLayerEdit.Equals(1) || item.ThisLayerEdit.Equals(3)) && user.ThisLayerEdit.Equals(0)) resultItem.thisLayerEdit = 0;
                else if (item.ThisLayerEdit.Equals(0) && user.ThisLayerEdit.Equals(1)) resultItem.thisLayerEdit = 2;
                else if (item.ThisLayerEdit.Equals(0) && user.ThisLayerEdit.Equals(3)) resultItem.thisLayerEdit = 3;

                if (item.ThisLayerDelete.Equals(0) && user.ThisLayerDelete.Equals(0)) resultItem.thisLayerDelete = -1;
                else if ((item.ThisLayerDelete.Equals(1) || item.ThisLayerDelete.Equals(3)) && user.ThisLayerDelete.Equals(1)) resultItem.thisLayerDelete = 1;
                else if ((item.ThisLayerDelete.Equals(1) || item.ThisLayerDelete.Equals(3)) && user.ThisLayerDelete.Equals(0)) resultItem.thisLayerDelete = 0;
                else if (item.ThisLayerDelete.Equals(0) && user.ThisLayerDelete.Equals(1)) resultItem.thisLayerDelete = 2;
                else if (item.ThisLayerDelete.Equals(0) && user.ThisLayerDelete.Equals(3)) resultItem.thisLayerDelete = 3;

                if (item.ThisLayerSelect.Equals(0) && user.ThisLayerSelect.Equals(0)) resultItem.thisLayerSelect = -1;
                else if ((item.ThisLayerSelect.Equals(1) || item.ThisLayerSelect.Equals(3)) && user.ThisLayerSelect.Equals(1)) resultItem.thisLayerSelect = 1;
                else if ((item.ThisLayerSelect.Equals(1) || item.ThisLayerSelect.Equals(3)) && user.ThisLayerSelect.Equals(0)) resultItem.thisLayerSelect = 0;
                else if (item.ThisLayerSelect.Equals(0) && user.ThisLayerSelect.Equals(1)) resultItem.thisLayerSelect = 2;
                else if (item.ThisLayerSelect.Equals(0) && user.ThisLayerSelect.Equals(3)) resultItem.thisLayerSelect = 3;

                if (item.SubLayerAdd.Equals(0) && user.SubLayerAdd.Equals(0)) resultItem.subLayerAdd = -1;
                else if ((item.SubLayerAdd.Equals(1) || item.SubLayerAdd.Equals(3)) && user.SubLayerAdd.Equals(1)) resultItem.subLayerAdd = 1;
                else if ((item.SubLayerAdd.Equals(1) || item.SubLayerAdd.Equals(3)) && user.SubLayerAdd.Equals(0)) resultItem.subLayerAdd = 0;
                else if (item.SubLayerAdd.Equals(0) && user.SubLayerAdd.Equals(1)) resultItem.subLayerAdd = 2;
                else if (item.SubLayerAdd.Equals(0) && user.SubLayerAdd.Equals(3)) resultItem.subLayerAdd = 3;

                if (item.SubLayerEdit.Equals(0) && user.SubLayerEdit.Equals(0)) resultItem.subLayerEdit = -1;
                else if ((item.SubLayerEdit.Equals(1) || item.SubLayerEdit.Equals(3)) && user.SubLayerEdit.Equals(1)) resultItem.subLayerEdit = 1;
                else if ((item.SubLayerEdit.Equals(1) || item.SubLayerEdit.Equals(3)) && user.SubLayerEdit.Equals(0)) resultItem.subLayerEdit = 0;
                else if (item.SubLayerEdit.Equals(0) && user.SubLayerEdit.Equals(1)) resultItem.subLayerEdit = 2;
                else if (item.SubLayerEdit.Equals(0) && user.SubLayerEdit.Equals(3)) resultItem.subLayerEdit = 3;

                if (item.SubLayerDelete.Equals(0) && user.SubLayerDelete.Equals(0)) resultItem.subLayerDelete = -1;
                else if ((item.SubLayerDelete.Equals(1) || item.SubLayerDelete.Equals(3)) && user.SubLayerDelete.Equals(1)) resultItem.subLayerDelete = 1;
                else if ((item.SubLayerDelete.Equals(1) || item.SubLayerDelete.Equals(3)) && user.SubLayerDelete.Equals(0)) resultItem.subLayerDelete = 0;
                else if (item.SubLayerDelete.Equals(0) && user.SubLayerDelete.Equals(1)) resultItem.subLayerDelete = 2;
                else if (item.SubLayerDelete.Equals(0) && user.SubLayerDelete.Equals(3)) resultItem.subLayerDelete = 3;

                if (item.SubLayerSelect.Equals(0) && user.SubLayerSelect.Equals(0)) resultItem.subLayerSelect = -1;
                else if ((item.SubLayerSelect.Equals(1) || item.SubLayerSelect.Equals(3)) && user.SubLayerSelect.Equals(1)) resultItem.subLayerSelect = 1;
                else if ((item.SubLayerSelect.Equals(1) || item.SubLayerSelect.Equals(3)) && user.SubLayerSelect.Equals(0)) resultItem.subLayerSelect = 0;
                else if (item.SubLayerSelect.Equals(0) && user.SubLayerSelect.Equals(1)) resultItem.subLayerSelect = 2;
                else if (item.SubLayerSelect.Equals(0) && user.SubLayerSelect.Equals(3)) resultItem.subLayerSelect = 3;
            }
            else
            {
                resultItem.thisLayerAdd = item.ThisLayerAdd.Equals(1) || item.ThisLayerAdd.Equals(3) ? resultItem.thisLayerAdd = 0 : resultItem.thisLayerAdd = -1;
                resultItem.thisLayerEdit = item.ThisLayerEdit.Equals(1) || item.ThisLayerEdit.Equals(3) ? resultItem.thisLayerEdit = 0 : resultItem.thisLayerEdit = -1;
                resultItem.thisLayerDelete = item.ThisLayerDelete.Equals(1) || item.ThisLayerDelete.Equals(3) ? resultItem.thisLayerDelete = 0 : resultItem.thisLayerDelete = -1;
                resultItem.thisLayerSelect = item.ThisLayerSelect.Equals(1) || item.ThisLayerSelect.Equals(3) ? resultItem.thisLayerSelect = 0 : resultItem.thisLayerSelect = -1;
                resultItem.subLayerAdd = item.SubLayerAdd.Equals(1) || item.SubLayerAdd.Equals(3) ? resultItem.subLayerAdd = 0 : resultItem.subLayerAdd = -1;
                resultItem.subLayerEdit = item.SubLayerEdit.Equals(1) || item.SubLayerEdit.Equals(3) ? resultItem.subLayerEdit = 0 : resultItem.subLayerEdit = -1;
                resultItem.subLayerDelete = item.SubLayerDelete.Equals(1) || item.SubLayerDelete.Equals(3) ? resultItem.subLayerDelete = 0 : resultItem.subLayerDelete = -1;
                resultItem.subLayerSelect = item.SubLayerSelect.Equals(1) || item.SubLayerSelect.Equals(3) ? resultItem.subLayerSelect = 0 : resultItem.subLayerSelect = -1;
            }

            result.Add(resultItem);
        });

        userList.ForEach(userItem =>
        {
            var adminItem = adminList.Find(x => x.OrganizeId.Equals(userItem.OrganizeId));
            if (adminItem == null)
            {
                var orgItem = orgTreeNameList.Find(x => x.Id.Equals(userItem.OrganizeId));
                var resultItem = new OrganizeAdministratorSelectorOutput();
                resultItem.id = orgItem.Id;
                resultItem.organizeId = orgItem.Id;
                resultItem.fullName = orgItem.Description;
                resultItem.parentId = orgItem.ParentId;
                resultItem.category = orgItem.Category;
                resultItem.icon = orgItem.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1";
                resultItem.organizeIdTree = orgItem.OrganizeIdTree;

                if (userItem.ThisLayerAdd.Equals(0)) resultItem.thisLayerAdd = -1;
                else if (userItem.ThisLayerAdd.Equals(1)) resultItem.thisLayerAdd = 2;
                else if (userItem.ThisLayerAdd.Equals(3) || userItem.ThisLayerAdd.Equals(1)) resultItem.thisLayerAdd = 3;

                if (userItem.ThisLayerEdit.Equals(0)) resultItem.thisLayerEdit = -1;
                else if (userItem.ThisLayerEdit.Equals(1)) resultItem.thisLayerEdit = 2;
                else if (userItem.ThisLayerEdit.Equals(3) || userItem.ThisLayerEdit.Equals(1)) resultItem.thisLayerEdit = 3;

                if (userItem.ThisLayerDelete.Equals(0)) resultItem.thisLayerDelete = -1;
                else if (userItem.ThisLayerDelete.Equals(1)) resultItem.thisLayerDelete = 2;
                else if (userItem.ThisLayerDelete.Equals(3) || userItem.ThisLayerDelete.Equals(1)) resultItem.thisLayerDelete = 3;

                if (userItem.ThisLayerSelect.Equals(0)) resultItem.thisLayerSelect = -1;
                else if (userItem.ThisLayerSelect.Equals(1)) resultItem.thisLayerSelect = 2;
                else if (userItem.ThisLayerSelect.Equals(3) || userItem.ThisLayerSelect.Equals(1)) resultItem.thisLayerSelect = 3;

                if (userItem.SubLayerAdd.Equals(0)) resultItem.subLayerAdd = -1;
                else if (userItem.SubLayerAdd.Equals(1)) resultItem.subLayerAdd = 2;
                else if (userItem.SubLayerAdd.Equals(3) || userItem.SubLayerAdd.Equals(1)) resultItem.subLayerAdd = 3;

                if (userItem.SubLayerEdit.Equals(0)) resultItem.subLayerEdit = -1;
                else if (userItem.SubLayerEdit.Equals(1)) resultItem.subLayerEdit = 2;
                else if (userItem.SubLayerEdit.Equals(3) || userItem.SubLayerEdit.Equals(1)) resultItem.subLayerEdit = 3;

                if (userItem.SubLayerDelete.Equals(0)) resultItem.subLayerDelete = -1;
                else if (userItem.SubLayerDelete.Equals(1)) resultItem.subLayerDelete = 2;
                else if (userItem.SubLayerDelete.Equals(3) || userItem.SubLayerDelete.Equals(1)) resultItem.subLayerDelete = 3;

                if (userItem.SubLayerSelect.Equals(0)) resultItem.subLayerSelect = -1;
                else if (userItem.SubLayerSelect.Equals(1)) resultItem.subLayerSelect = 2;
                else if (userItem.SubLayerSelect.Equals(3) || userItem.SubLayerSelect.Equals(1)) resultItem.subLayerSelect = 3;

                if (!result.Any(x => x.organizeId.Equals(resultItem.organizeId))) result.Add(resultItem);
            }
        });

        // 组织断层处理
        result.Where(x => x.parentId != "-1").OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!result.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = result.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "-1";
                }
            }
            else
            {
                var pItem = result.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        var orgAdminList = result.ToTree("-1");

        orgAdminList.ToObject<List<Dictionary<string, object>>>().ForEach(item =>
        {
            if (item.ContainsValue(-1))
                foreach (var key in item.Where(x => x.Value.Equals(-1))) item.Remove(key.Key);
        });

        var res = await GetSystemAndModuleListByUserId(userId);
        res.orgAdminList = orgAdminList;
        res.managerGroup = managerGroup;

        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 更新机构分级管理.
    /// </summary>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update([FromBody] OrganizeAdminIsTratorUpInput input)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == input.organizeId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        List<string>? oldUserIds = await _repository.AsQueryable().Where(it => it.OrganizeId == input.organizeId && it.DeleteMark == null).Select(it => it.UserId).ToListAsync();

        // 计算旧用户数组与新用户数组差
        List<string>? addList = input.userId.Split(',').Except(oldUserIds).ToList();
        List<string>? editList = input.userId.Split(',').Intersect(oldUserIds).ToList();
        List<string>? delList = oldUserIds.Except(input.userId.Split(',')).ToList();

        // 创建新数据
        if (addList.Count > 0)
        {
            List<OrganizeAdministratorEntity>? addEntityList = new List<OrganizeAdministratorEntity>();
            addList.ForEach(it =>
            {
                OrganizeAdministratorEntity entity = input.Adapt<OrganizeAdministratorEntity>();
                entity.UserId = it;
                addEntityList.Add(entity);
            });
            await _repository.AsSugarClient().Insertable(addEntityList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        // 修改旧数据
        if (editList.Count > 0)
        {
            List<OrganizeAdministratorEntity>? editEntityList = await _repository.AsQueryable().Where(it => it.OrganizeId == input.organizeId && editList.Contains(it.UserId)).ToListAsync();
            editEntityList.ForEach(it =>
            {
                it.ThisLayerAdd = Convert.ToInt32(input.thisLayerAdd);
                it.ThisLayerEdit = Convert.ToInt32(input.thisLayerEdit);
                it.ThisLayerDelete = Convert.ToInt32(input.thisLayerDelete);
                it.SubLayerAdd = Convert.ToInt32(input.subLayerAdd);
                it.SubLayerEdit = Convert.ToInt32(input.subLayerEdit);
                it.SubLayerDelete = Convert.ToInt32(input.subLayerDelete);
                it.LastModifyTime = DateTime.Now;
                it.LastModifyUserId = _userManager.UserId;
            });
            await _repository.AsSugarClient().Updateable(editEntityList).UpdateColumns(it => new {
                it.ThisLayerAdd,
                it.ThisLayerEdit,
                it.ThisLayerDelete,
                it.SubLayerAdd,
                it.SubLayerEdit,
                it.SubLayerDelete,
                it.LastModifyTime,
                it.LastModifyUserId
            }).ExecuteCommandAsync();
        }

        // 删除旧数据
        if (delList.Count > 0)
        {
            await _repository.AsSugarClient().Updateable<OrganizeAdministratorEntity>().SetColumns(it => new OrganizeAdministratorEntity()
            {
                EnabledMark = 0,
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId
            }).Where(it => delList.Contains(it.UserId) && it.OrganizeId == input.organizeId).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 更新机构分级管理.
    /// </summary>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Save([FromBody] OrganizeAdminIsTratorUpInput input)
    {
        if (input.userId.Equals(_userManager.UserId)) throw Oops.Oh(ErrorCode.D2304);
        if (await _repository.AsSugarClient().Queryable<UserEntity>().AnyAsync(x => x.Id.Equals(input.userId) && x.IsAdministrator.Equals(1))) throw Oops.Oh(ErrorCode.D2305);

        // 处理组织树 名称
        List<OrganizeEntity> orgTreeNameList = GetOrgListTreeName();

        // 获取当前有权限的组织
        var currList = await _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => orgTreeNameList.Select(xx => xx.Id).Contains(x.OrganizeId)).ToListAsync();

        // 用户旧权限
        var oldList = currList.Where(x => x.ThisLayerAdd.Equals(1) || x.ThisLayerEdit.Equals(1) || x.ThisLayerDelete.Equals(1) || x.ThisLayerSelect.Equals(1) ||
          x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).Where(x => x.UserId.Equals(input.userId)).ToList().Copy();
        oldList.Where(x => x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).ToList().ForEach(item =>
        {
            var orgIds = orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId)).Select(x => x.Id).ToList();
            var subList = currList.Where(x => orgIds.Contains(x.OrganizeId) && x.UserId.Equals(_userManager.UserId)).ToList().Copy();

            subList.ForEach(it =>
            {
                var userIt = oldList.Find(x => x.OrganizeId.Equals(it.OrganizeId));
                if (userIt == null)
                {
                    it.ThisLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                }
                else
                {
                    it.ThisLayerAdd = userIt.ThisLayerAdd.Equals(1) ? 1 : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = userIt.ThisLayerEdit.Equals(1) ? 1 : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = userIt.ThisLayerDelete.Equals(1) ? 1 : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = userIt.ThisLayerSelect.Equals(1) ? 1 : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = userIt.SubLayerAdd.Equals(1) ? 1 : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = userIt.SubLayerEdit.Equals(1) ? 1 : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = userIt.SubLayerDelete.Equals(1) ? 1 : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = userIt.SubLayerSelect.Equals(1) ? 1 : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    oldList.Remove(userIt);
                }
            });

            oldList.AddRange(subList);
        });

        // 管理员权限
        var adminList = currList.Where(x => x.ThisLayerAdd.Equals(1) || x.ThisLayerEdit.Equals(1) || x.ThisLayerDelete.Equals(1) || x.ThisLayerSelect.Equals(1) ||
          x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).Where(x => x.UserId.Equals(_userManager.UserId)).ToList().Copy();
        adminList.Where(x => x.SubLayerAdd.Equals(1) || x.SubLayerEdit.Equals(1) || x.SubLayerDelete.Equals(1) || x.SubLayerSelect.Equals(1)).ToList().ForEach(item =>
        {
            var orgIds = orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId)).Select(x => x.Id).ToList();
            var subList = currList.Where(x => orgIds.Contains(x.OrganizeId) && x.UserId.Equals(_userManager.UserId)).ToList().Copy();

            subList.ForEach(it =>
            {
                var adminIt = adminList.Find(x => x.OrganizeId.Equals(it.OrganizeId));
                if (adminIt == null)
                {
                    it.ThisLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = item.SubLayerSelect.Equals(1) ? 3 : 0;
                }
                else
                {
                    it.ThisLayerAdd = adminIt.ThisLayerAdd.Equals(1) ? 1 : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.ThisLayerEdit = adminIt.ThisLayerEdit.Equals(1) ? 1 : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.ThisLayerDelete = adminIt.ThisLayerDelete.Equals(1) ? 1 : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.ThisLayerSelect = adminIt.ThisLayerSelect.Equals(1) ? 1 : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    it.SubLayerAdd = adminIt.SubLayerAdd.Equals(1) ? 1 : item.SubLayerAdd.Equals(1) ? 3 : 0;
                    it.SubLayerEdit = adminIt.SubLayerEdit.Equals(1) ? 1 : item.SubLayerEdit.Equals(1) ? 3 : 0;
                    it.SubLayerDelete = adminIt.SubLayerDelete.Equals(1) ? 1 : item.SubLayerDelete.Equals(1) ? 3 : 0;
                    it.SubLayerSelect = adminIt.SubLayerSelect.Equals(1) ? 1 : item.SubLayerSelect.Equals(1) ? 3 : 0;
                    oldList.Remove(adminIt);
                }
            });

            adminList.AddRange(subList);
        });

        var itemList = GetOrganizeAdministrators(input.orgAdminModel, input.userId, oldList, adminList, input.managerGroup);

        try
        {
            var firsCreateTime = await _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => x.UserId.Equals(input.userId)).Select(x => x.CreatorTime).FirstAsync();
            await _repository.AsSugarClient().Deleteable<OrganizeAdministratorEntity>().Where(x => x.UserId.Equals(input.userId)).ExecuteCommandAsync();
            if (itemList.Any()) await _repository.AsSugarClient().Insertable(itemList).ExecuteCommandAsync();
            if (input.systemIds.Any())
            {
                var systemList = new List<OrganizeAdministratorEntity>();
                input.systemIds.ForEach(item =>
                {
                    systemList.Add(new OrganizeAdministratorEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        OrganizeId = item,
                        OrganizeType = "System",
                        UserId = input.userId,
                        EnabledMark = 1,
                        ThisLayerAdd = 1,
                        ThisLayerDelete = 1,
                        ThisLayerEdit = 1,
                        ThisLayerSelect = 1
                        ,
                        CreatorTime = firsCreateTime != null ? firsCreateTime : DateTime.Now
                    });
                });
                await _repository.AsSugarClient().Insertable(systemList).ExecuteCommandAsync();
            }

            if (input.moduleIds.Any())
            {
                var moduleList = new List<OrganizeAdministratorEntity>();
                input.moduleIds.ForEach(item =>
                {
                    moduleList.Add(new OrganizeAdministratorEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        OrganizeId = item,
                        OrganizeType = "Module",
                        UserId = input.userId,
                        EnabledMark = 1,
                        ThisLayerAdd = 1,
                        ThisLayerDelete = 1,
                        ThisLayerEdit = 1,
                        ThisLayerSelect = 1,
                        CreatorTime = firsCreateTime != null ? firsCreateTime : DateTime.Now
                    });
                });
                await _repository.AsSugarClient().Insertable(moduleList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }

            await ForcedOffline(input.userId);
        }
        catch
        {
            if (input.id.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.D2300);
            else throw Oops.Oh(ErrorCode.D2301);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entityList = await _repository.GetListAsync(r => r.UserId == id && r.DeleteMark == null);
        _ = entityList ?? throw Oops.Oh(ErrorCode.D2302);

        await _repository.AsSugarClient().Deleteable(entityList).ExecuteCommandAsync();
        await ForcedOffline(id);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取用户数据范围.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserDataScopeModel>> GetUserDataScopeModel(string userId)
    {
        List<UserDataScopeModel> data = new List<UserDataScopeModel>();
        List<UserDataScopeModel> subData = new List<UserDataScopeModel>();
        List<UserDataScopeModel> inteList = new List<UserDataScopeModel>();
        List<OrganizeAdministratorEntity>? list = await _repository.AsQueryable().Where(it => it.UserId == userId && it.DeleteMark == null).ToListAsync();

        // 填充数据
        foreach (OrganizeAdministratorEntity? item in list)
        {
            if (item.SubLayerAdd.ParseToBool() || item.SubLayerEdit.ParseToBool() || item.SubLayerDelete.ParseToBool())
            {
                List<string>? subsidiary = await _organizeService.GetSubsidiary(item.OrganizeId);
                subsidiary.Remove(item.OrganizeId);
                subsidiary.ForEach(it =>
                {
                    subData.Add(new UserDataScopeModel()
                    {
                        organizeId = it,
                        Add = item.SubLayerAdd.ParseToBool(),
                        Edit = item.SubLayerEdit.ParseToBool(),
                        Delete = item.SubLayerDelete.ParseToBool()
                    });
                });
            }

            if (item.ThisLayerAdd.ParseToBool() || item.ThisLayerEdit.ParseToBool() || item.ThisLayerDelete.ParseToBool())
            {
                data.Add(new UserDataScopeModel()
                {
                    organizeId = item.OrganizeId,
                    Add = item.ThisLayerAdd.ParseToBool(),
                    Edit = item.ThisLayerEdit.ParseToBool(),
                    Delete = item.ThisLayerDelete.ParseToBool()
                });
            }
        }

        /* 比较数据
        所有分级数据权限以本级权限为主 子级为辅
        将本级数据与子级数据对比 对比出子级数据内组织ID存在本级数据的组织ID*/
        List<string>? intersection = data.Select(it => it.organizeId).Intersect(subData.Select(it => it.organizeId)).ToList();
        intersection.ForEach(it =>
        {
            UserDataScopeModel? parent = data.Find(item => item.organizeId == it);
            UserDataScopeModel? child = subData.Find(item => item.organizeId == it);
            bool add = false;
            bool edit = false;
            bool delete = false;
            if (parent.Add || child.Add) add = true;
            if (parent.Edit || child.Edit) edit = true;
            if (parent.Delete || child.Delete) delete = true;
            inteList.Add(new UserDataScopeModel()
            {
                organizeId = it,
                Add = add,
                Edit = edit,
                Delete = delete
            });
            data.Remove(parent);
            subData.Remove(child);
        });
        return data.Union(subData).Union(inteList).ToList();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    private List<OrganizeEntity> GetOrgListTreeName()
    {
        List<OrganizeEntity>? orgTreeNameList = new List<OrganizeEntity>();
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

        return orgTreeNameList;
    }

    /// <summary>
    /// 递归处理 获取所有组织权限，根据前端参数 orgAdminList.
    /// </summary>
    /// <param name="orgAdminList"></param>
    /// <param name="userId"></param>
    /// <param name="oldList"></param>
    /// <param name="adminList"></param>
    /// <param name="managerGroup"></param>
    /// <returns></returns>
    private List<OrganizeAdministratorEntity> GetOrganizeAdministrators(List<OrganizeAdminCrInput> orgAdminList, string userId, List<OrganizeAdministratorEntity> oldList, List<OrganizeAdministratorEntity> adminList, string managerGroup)
    {
        var oldInfo = _repository.AsQueryable().Where(x => x.UserId.Equals(userId)).First();
        if (oldInfo == null) oldInfo = new OrganizeAdministratorEntity() { CreatorTime = DateTime.Now, CreatorUserId = _userManager.UserId };
        if (!_userManager.IsAdministrator)
        {
            var addItems = new List<OrganizeAdminCrInput>();
            orgAdminList.ForEach(item =>
            {
                var oldItem = oldList.Find(x => x.OrganizeId.Equals(item.organizeId));
                var adminItem = adminList.Find(x => x.OrganizeId.Equals(item.organizeId));
                if (oldItem != null)
                {
                    if (adminItem != null)
                    {
                        item.thisLayerAdd = adminItem.ThisLayerAdd.Equals(1) ? item.thisLayerAdd : oldItem.ThisLayerAdd;
                        item.thisLayerDelete = adminItem.ThisLayerDelete.Equals(1) ? item.thisLayerDelete : oldItem.ThisLayerDelete;
                        item.thisLayerEdit = adminItem.ThisLayerEdit.Equals(1) ? item.thisLayerEdit : oldItem.ThisLayerEdit;
                        item.thisLayerSelect = adminItem.ThisLayerSelect.Equals(1) ? item.thisLayerSelect : oldItem.ThisLayerSelect;
                        item.subLayerAdd = adminItem.SubLayerAdd.Equals(1) ? item.subLayerAdd : oldItem.SubLayerAdd;
                        item.subLayerDelete = adminItem.SubLayerDelete.Equals(1) ? item.subLayerDelete : oldItem.SubLayerDelete;
                        item.subLayerEdit = adminItem.SubLayerEdit.Equals(1) ? item.subLayerEdit : oldItem.SubLayerEdit;
                        item.subLayerSelect = adminItem.SubLayerSelect.Equals(1) ? item.subLayerSelect : oldItem.SubLayerSelect;
                    }
                    else
                    {
                        item.thisLayerAdd = oldItem.ThisLayerAdd.Equals(1) || oldItem.ThisLayerAdd.Equals(2) ? 1 : item.thisLayerAdd;
                        item.thisLayerDelete = oldItem.ThisLayerDelete.Equals(1) || oldItem.ThisLayerDelete.Equals(2) ? 1 : item.thisLayerDelete;
                        item.thisLayerEdit = oldItem.ThisLayerEdit.Equals(1) || oldItem.ThisLayerEdit.Equals(2) ? 1 : item.thisLayerEdit;
                        item.thisLayerSelect = oldItem.ThisLayerSelect.Equals(1) || oldItem.ThisLayerSelect.Equals(2) ? 1 : item.thisLayerSelect;
                        item.subLayerAdd = oldItem.SubLayerAdd.Equals(1) || oldItem.SubLayerAdd.Equals(2) ? 1 : item.subLayerAdd;
                        item.subLayerDelete = oldItem.SubLayerDelete.Equals(1) || oldItem.SubLayerDelete.Equals(2) ? 1 : item.subLayerDelete;
                        item.subLayerEdit = oldItem.SubLayerEdit.Equals(1) || oldItem.SubLayerEdit.Equals(2) ? 1 : item.subLayerEdit;
                        item.subLayerSelect = oldItem.SubLayerSelect.Equals(1) || oldItem.SubLayerSelect.Equals(2) ? 1 : item.subLayerSelect;
                    }
                }
            });
            oldList.ForEach(item =>
            {
                if (!orgAdminList.Any(x => x.organizeId.Equals(item.OrganizeId)) && !adminList.Any(x => x.OrganizeId.Equals(item.OrganizeId)))
                {
                    addItems.Add(new OrganizeAdminCrInput()
                    {
                        userId = item.UserId,
                        organizeId = item.OrganizeId,
                        thisLayerAdd = item.ThisLayerAdd,
                        thisLayerDelete = item.ThisLayerDelete,
                        thisLayerEdit = item.ThisLayerEdit,
                        thisLayerSelect = item.ThisLayerSelect,
                        subLayerAdd = item.SubLayerAdd,
                        subLayerDelete = item.SubLayerDelete,
                        subLayerEdit = item.SubLayerEdit,
                        subLayerSelect = item.SubLayerSelect,
                    });
                }
            });
            orgAdminList.AddRange(addItems);
        }

        var resList = new List<OrganizeAdministratorEntity>();

        orgAdminList.ForEach(item =>
        {
            resList.Add(new OrganizeAdministratorEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                UserId = userId,
                ManagerGroup = managerGroup,
                OrganizeId = item.organizeId,
                CreatorTime = oldInfo.CreatorTime,
                CreatorUserId = oldInfo.CreatorUserId,
                ThisLayerSelect = item.thisLayerSelect.Equals(1) || item.thisLayerSelect.Equals(2) ? 1 : 0,
                ThisLayerAdd = (item.thisLayerSelect.Equals(1) || item.thisLayerSelect.Equals(2)) && (item.thisLayerAdd.Equals(1) || item.thisLayerAdd.Equals(2)) ? 1 : 0,
                ThisLayerDelete = (item.thisLayerSelect.Equals(1) || item.thisLayerSelect.Equals(2)) && (item.thisLayerDelete.Equals(1) || item.thisLayerDelete.Equals(2)) ? 1 : 0,
                ThisLayerEdit = (item.thisLayerSelect.Equals(1) || item.thisLayerSelect.Equals(2)) && (item.thisLayerEdit.Equals(1) || item.thisLayerEdit.Equals(2)) ? 1 : 0,
                SubLayerSelect = item.subLayerSelect.Equals(1) || item.subLayerSelect.Equals(2) ? 1 : 0,
                SubLayerAdd = (item.subLayerSelect.Equals(1) || item.subLayerSelect.Equals(2)) && (item.subLayerAdd.Equals(1) || item.subLayerAdd.Equals(2)) ? 1 : 0,
                SubLayerDelete = (item.subLayerSelect.Equals(1) || item.subLayerSelect.Equals(2)) && (item.subLayerDelete.Equals(1) || item.subLayerDelete.Equals(2)) ? 1 : 0,
                SubLayerEdit = (item.subLayerSelect.Equals(1) || item.subLayerSelect.Equals(2)) && (item.subLayerEdit.Equals(1) || item.subLayerEdit.Equals(2)) ? 1 : 0
            });

            if (item.children != null && item.children.Any())
            {
                var children = GetOrganizeAdministrators(item.children.ToObject<List<OrganizeAdminCrInput>>(), userId, oldList, adminList, managerGroup);
                resList.AddRange(children);
            }
        });

        return resList;
    }

    /// <summary>
    /// 获取应用系统和主系统菜单集合.
    /// </summary>
    /// <returns></returns>
    private async Task<OrganizeAdminSystemModuleSelectorOutput> GetSystemAndModuleListByUserId(string userId)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var res = new OrganizeAdminSystemModuleSelectorOutput() { moduleIds = new List<string>(), systemIds = new List<string>() };

        // 应用系统
        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
            .WhereIF(ignoreIds != null, x => !ignoreIds.Contains(x.Id))
            .Select<SystemSelectorOutput>().ToListAsync();

        // 主系统
        var mainSystem = systemList.First(x => x.enCode.Equals("mainSystem"));

        // 主系统菜单
        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1) && mainSystem.id.Equals(x.SystemId))
            .Where(x => !_userManager.CommonModuleEnCodeList.Contains(x.EnCode) && x.Category.Equals("Web")).OrderBy(x => x.SortCode)
            .WhereIF(ignoreIds != null, x => !ignoreIds.Contains(x.Id))
            .WhereIF(ignoreUrls != null, x => !ignoreUrls.Contains(x.UrlAddress))
            .Select<Entitys.Dto.Permission.OrganizeAdministrator.ModuleSelectorOutput>().ToListAsync();

        systemList = systemList.Where(x => !x.enCode.Equals("mainSystem")).ToList();

        if (!_userManager.IsAdministrator)
        {
            // 获取有权限的 Id.
            var sIdList = await _repository.AsQueryable()
                .Where(x => (x.UserId.Equals(userId) || x.UserId.Equals(_userManager.UserId)) && (x.OrganizeType.Equals("System") || x.OrganizeType.Equals("Module")))
                .Select(x => new { ObjectId = x.OrganizeId, Type = x.OrganizeType, x.UserId }).ToListAsync();

            var adminIdList = sIdList.Where(x => x.UserId.Equals(_userManager.UserId)).Select(x => x.ObjectId).ToList();
            var userIdList = sIdList.Where(x => x.UserId.Equals(userId)).Select(x => x.ObjectId).ToList();

            res.systemPermissionList = systemList.Where(x => adminIdList.Contains(x.id) || userIdList.Contains(x.id)).ToList();
            res.systemPermissionList.ForEach(item =>
            {
                if (adminIdList.Any(x => x.Equals(item.id)) && userIdList.Any(x => x.Equals(item.id)))
                {
                    res.systemIds.Add(item.id);
                }
                else if (!adminIdList.Any(x => x.Equals(item.id)) && userIdList.Any(x => x.Equals(item.id)))
                {
                    item.disabled = true;
                    res.systemIds.Add(item.id);
                }
            });

            res.modulePermissionList = moduleList.Where(x => adminIdList.Contains(x.id) || userIdList.Contains(x.id)).ToList();
            res.modulePermissionList.ForEach(item =>
            {
                if (adminIdList.Any(x => x.Equals(item.id)) && userIdList.Any(x => x.Equals(item.id)))
                {
                    res.moduleIds.Add(item.id);
                }
                else if (!adminIdList.Any(x => x.Equals(item.id)) && userIdList.Any(x => x.Equals(item.id)))
                {
                    item.disabled = true;
                    res.moduleIds.Add(item.id);
                }

                if (item.parentId.Equals("-1")) item.parentId = mainSystem.id;
            });
            if (sIdList.Any(x => x.ObjectId.Equals(mainSystem.id)))
            {
                res.modulePermissionList.Add(new Entitys.Dto.Permission.OrganizeAdministrator.ModuleSelectorOutput()
                {
                    id = mainSystem.id,
                    category = "Web",
                    enCode = mainSystem.enCode,
                    fullName = mainSystem.fullName,
                    icon = mainSystem.icon,
                    parentId = "-1"
                });
            }

            res.modulePermissionList = res.modulePermissionList.ToTree("-1");
        }
        else
        {
            var userIdList = await _repository.AsQueryable().Where(x => x.UserId.Equals(userId)).Select(x => x.OrganizeId).ToListAsync();

            systemList.ForEach(item =>
            {
                if (userIdList.Any(x => x.Equals(item.id))) res.systemIds.Add(item.id);
            });
            res.systemPermissionList = systemList;

            moduleList.ForEach(item => { if (item.parentId.Equals("-1")) item.parentId = mainSystem.id; });
            moduleList.Add(new Entitys.Dto.Permission.OrganizeAdministrator.ModuleSelectorOutput()
            {
                id = mainSystem.id,
                category = "Web",
                enCode = mainSystem.enCode,
                fullName = mainSystem.fullName,
                icon = mainSystem.icon,
                parentId = "-1"
            });
            moduleList.ForEach(item => { if (userIdList.Any(x => x.Equals(item.id))) res.moduleIds.Add(item.id); });
            res.modulePermissionList = moduleList.ToTree("-1");
        }

        return res;
    }

    /// <summary>
    /// 强制分管用户下线或切换应用.
    /// </summary>
    /// <param name="userId"></param>
    private async Task ForcedOffline(string userId)
    {
        var tenantId = _userManager.TenantId;
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        var allUserOnlineList = await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);

        if (allUserOnlineList != null)
        {
            var onlineUserList = allUserOnlineList.Where(x => x.userId == userId).ToList();
            foreach (var onlineUser in onlineUserList)
            {
                await _imHandler.SendMessageAsync(onlineUser.connectionId, new { method = "logout", msg = "权限已变更，请重新登录！" }.ToJsonString());

                // 删除在线用户ID
                allUserOnlineList.RemoveAll((x) => x.connectionId == onlineUser.connectionId);
                await _cacheManager.SetAsync(cacheKey, allUserOnlineList);

                // 删除用户登录信息缓存
                cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, onlineUser.userId);
                await _cacheManager.DelAsync(cacheKey);
            }
        }
    }

    #endregion
}