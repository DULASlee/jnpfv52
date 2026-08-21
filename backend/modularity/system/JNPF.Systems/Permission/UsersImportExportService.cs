using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Text.RegularExpressions;
using System.Threading;

namespace JNPF.Systems;

/// <summary>
///  业务实现：用户信息导入导出（CR-20260819-01 自 UsersService 剥离）.
///  路由契约：类级特性与 UsersService 完全一致，[controller] 仍解析为 users，
///  六个端点路径 /api/permission/users/{action} 逐条不变（仅移动，行为零变更）.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Users", Order = 163)]
[Route("api/permission/[controller]")]
public class UsersImportExportService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

    /// <summary>
    /// 机构表服务.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户关系表服务.
    /// </summary>
    private readonly IUserRelationService _userRelationService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 初始化一个<see cref="UsersImportExportService"/>类型的新实例.
    /// </summary>
    public UsersImportExportService(
        ISqlSugarRepository<UserEntity> userRepository,
        IOrganizeService organizeService,
        IUserRelationService userRelationService,
        ICacheManager cacheManager,
        IFileManager fileService,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager,
        ITenantManager tenantManager)
    {
        _repository = userRepository;
        _organizeService = organizeService;
        _userRelationService = userRelationService;
        _cacheManager = cacheManager;
        _fileManager = fileService;
        _tenant = tenantOptions.Value;
        _userManager = userManager;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 导出Excel.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("ExportData")]
    public async Task<dynamic> ExportData([FromQuery] UserExportDataInput input, CancellationToken cancellationToken = default)
    {
        // 获取分级管理组织
        var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).Distinct().ToList();

        // 处理组织树 名称
        List<OrganizeEntity>? orgTreeNameList = _organizeService.GetOrgListTreeName();

        #region 获取组织层级

        List<string>? childOrgIds = new List<string>();
        if (input.organizeId.IsNotEmptyOrNull())
        {
            childOrgIds.Add(input.organizeId);

            // 根据组织Id 获取所有子组织Id集合
            childOrgIds.AddRange(orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToList());
            childOrgIds = childOrgIds.Distinct().ToList();
        }

        #endregion

        // 用户信息列表
        List<UserListImportDataInput>? userList = new List<UserListImportDataInput>();
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "certificateType" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity1 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Education" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity3 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Nation" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity4 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Rank" && x.DeleteMark == null && x.EnabledMark == 1);
        var query = _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && !a.Account.Equals("admin"))
            .WhereIF(input.enabledMark != null, a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.gender != null, a => a.Gender.Equals(input.gender))
            .WhereIF(childOrgIds.Any(), a => SqlFunc.Subqueryable<UserRelationEntity>().Where(x => childOrgIds.Contains(x.ObjectId) && x.UserId.Equals(a.Id)).Any())
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .WhereIF(!_userManager.IsAdministrator, a => SqlFunc.Subqueryable<UserRelationEntity>().Where(x => dataScope.Contains(x.ObjectId) && x.UserId.Equals(a.Id)).Any())
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new UserListImportDataInput()
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                birthday = SqlFunc.ToString(a.Birthday),
                certificatesNumber = a.CertificatesNumber,
                managerId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.ManagerId && u.DeleteMark == null && u.EnabledMark == 1).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                organizeId = a.OrganizeId, // 组织结构
                positionId = a.PositionId, // 岗位
                roleId = a.RoleId, // 多角色
                certificatesType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity.Id && d.Id == a.CertificatesType && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                education = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity1.Id && d.Id == a.Education && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                gender = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == a.Gender && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                nation = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity3.Id && d.Id == a.Nation && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                description = a.Description,
                entryDate = SqlFunc.ToString(a.EntryDate),
                email = a.Email,
                enabledMark = SqlFunc.IF(a.EnabledMark.Equals(0)).Return("禁用").ElseIF(a.EnabledMark.Equals(1)).Return("启用").End("锁定"),
                mobilePhone = a.MobilePhone,
                nativePlace = a.NativePlace,
                postalAddress = a.PostalAddress,
                telePhone = a.TelePhone,
                urgentContacts = a.UrgentContacts,
                urgentTelePhone = a.UrgentTelePhone,
                landline = a.Landline,
                ranks = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity4.Id && d.Id == a.Ranks && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                sortCode = a.SortCode.ToString()
            });
        if (input.dataType.Equals("0"))
        {
            userList = (await query.ToPagedListAsync(input.currentPage, input.pageSize)).list.ToList();
        }
        else
        {
            userList = await query.ToListAsync();
        }

        userList.ForEach(item =>
        {
            if (item.birthday.IsNotEmptyOrNull()) item.birthday = Convert.ToDateTime(item.birthday).ToString("yyyy-MM-dd HH:mm:ss");
            if (item.entryDate.IsNotEmptyOrNull()) item.entryDate = Convert.ToDateTime(item.entryDate).ToString("yyyy-MM-dd HH:mm:ss");
        });

        List<PositionEntity>? plist = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.EnabledMark == 1 && it.DeleteMark == null).ToListAsync(); // 获取所有岗位
        List<RoleEntity>? rlist = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(it => it.EnabledMark == 1 && it.DeleteMark == null).ToListAsync(); // 获取所有角色

        // 获取用户组织关联数据
        var userRelation = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType == "Organize" || x.ObjectType == "Position").Where(x => userList.Select(xx => xx.id).Contains(x.UserId))
            .Select(x => new { x.ObjectId, x.ObjectType, x.UserId }).ToListAsync();

        // 转换 组织结构 和 岗位(多岗位)
        foreach (UserListImportDataInput? item in userList)
        {
            // 获取用户组织关联数据
            List<string>? orgRelList = userRelation.Where(x => x.ObjectType == "Organize" && x.UserId == item.id).Select(x => x.ObjectId).ToList();

            if (orgRelList.Any())
            {
                List<OrganizeEntity>? oentityList = orgTreeNameList.Where(x => orgRelList.Contains(x.Id)).ToList();
                if (oentityList.Any())
                {
                    List<string>? userOrgList = new List<string>();
                    oentityList.ForEach(oentity => userOrgList.Add(oentity.Description));
                    item.organizeId = string.Join(";", userOrgList);
                }
            }
            else
            {
                item.organizeId = string.Empty;
            }

            // 获取用户岗位关联
            List<string>? posRelList = userRelation.Where(x => x.ObjectType == "Position" && x.UserId == item.id).Select(x => x.ObjectId).ToList();
            if (posRelList.Any())
                item.positionId = string.Join(";", plist.Where(x => posRelList.Contains(x.Id)).Select(x => x.FullName + "/" + x.EnCode).ToList());
            else
                item.positionId = string.Empty;

            // 角色
            if (item.roleId.IsNotEmptyOrNull())
            {
                List<string>? ridList = item.roleId.Split(',').ToList();
                item.roleId = string.Join(";", rlist.Where(x => ridList.Contains(x.Id)).Select(x => x.FullName).ToList());
            }
        }

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("用户信息_{0:yyyyMMddhhmmss}.xls", DateTime.Now);
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (KeyValuePair<string, string> item in GetUserInfoFieldToTitle(input.selectKey.Split(',').ToList()))
        {
            string? column = item.Key;
            string? excelColumn = item.Value;
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = column, ExcelColumn = excelColumn });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var fs = ExcelExportHelper<UserListImportDataInput>.ExportMemoryStream(userList, excelconfig);
        var flag = await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
        if (flag)
        {
            fs.Flush();
            fs.Close();
        }

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESCEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("TemplateDownload")]
    public async Task<dynamic> TemplateDownload(CancellationToken cancellationToken = default)
    {
        // 初始化 一条空数据 
        List<UserListImportDataInput>? dataList = new List<UserListImportDataInput>() { new UserListImportDataInput() { } };

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = "用户信息导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var userInfoFields = GetUserInfoFieldToTitle();
        userInfoFields.Remove("errorsInfo");
        foreach (KeyValuePair<string, string> item in userInfoFields)
        {
            string? column = item.Key;
            string? excelColumn = item.Value;
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = column, ExcelColumn = excelColumn });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        if (!(await _fileManager.ExistsFile(addPath)))
        {
            var stream = ExcelExportHelper<UserListImportDataInput>.ToStream(dataList, excelconfig);
            await _fileManager.UploadFileByType(stream, FileVariable.TemporaryFilePath, excelconfig.FileName);
        }
        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESCEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task<dynamic> Uploader(IFormFile file, CancellationToken cancellationToken = default)
    {
        var _filePath = _fileManager.GetPathByType(string.Empty);
        var _fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + SnowflakeIdHelper.NextId() + Path.GetExtension(file.FileName);
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, _filePath, _fileName);
        return new { name = _fileName, url = string.Format("/api/File/Image/{0}/{1}", string.Empty, _fileName) };
    }

    /// <summary>
    /// 导入预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImportPreview")]
    public async Task<dynamic> ImportPreview(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            Dictionary<string, string>? FileEncode = GetUserInfoFieldToTitle();

            string? filePath = FileVariable.TemporaryFilePath;
            string? savePath = Path.Combine(filePath, fileName);

            // 得到数据
            var sr = await _fileManager.GetFileStream(savePath);
            global::System.Data.DataTable? excelData = ExcelImportHelper.ToDataTable(savePath, sr);
            foreach (object? item in excelData.Columns)
            {
                excelData.Columns[item.ToString()].ColumnName = FileEncode.Where(x => x.Value == item.ToString()).FirstOrDefault().Key;
            }

            if (excelData.Rows.Count > 0) excelData.Rows.RemoveAt(0);

            // 返回结果
            return new { dataRow = excelData };
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1801);
        }
    }

    /// <summary>
    /// 导出错误报告.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ExportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData([FromBody] UserImportDataInput list, CancellationToken cancellationToken = default)
    {
        list.list.ForEach(it => it.errorsInfo = string.Empty);
        object[]? res = await ImportUserData(list.list);

        // 错误数据
        List<UserListImportDataInput>? errorlist = res.Last() as List<UserListImportDataInput>;

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("错误报告_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmss"));
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (KeyValuePair<string, string> item in GetUserInfoFieldToTitle())
        {
            string? column = item.Key;
            string? excelColumn = item.Value;
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = column, ExcelColumn = excelColumn });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        ExcelExportHelper<UserListImportDataInput>.Export(errorlist, excelconfig, addPath);

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESCEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData([FromBody] UserImportDataInput list, CancellationToken cancellationToken = default)
    {
        list.list.ForEach(x => x.errorsInfo = string.Empty);
        object[]? res = await ImportUserData(list.list);
        List<UserEntity>? addlist = res.First() as List<UserEntity>;
        List<UserListImportDataInput>? errorlist = res.Last() as List<UserListImportDataInput>;
        return new UserImportResultOutput() { snum = addlist.Count, fnum = errorlist.Count, failResult = errorlist, resultType = errorlist.Count < 1 ? 0 : 1 };
    }

    #region 私有方法

    /// <summary>
    /// 用户信息 字段对应 列名称.
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, string> GetUserInfoFieldToTitle(List<string> fields = null)
    {
        Dictionary<string, string>? res = new Dictionary<string, string>();
        res.Add("account", "账户");
        res.Add("realName", "姓名");
        res.Add("gender", "性别");
        res.Add("email", "电子邮箱");
        res.Add("organizeId", "所属组织");
        res.Add("managerId", "直属主管");
        res.Add("positionId", "岗位");
        res.Add("ranks", "职级");
        res.Add("roleId", "角色");
        res.Add("sortCode", "排序");
        res.Add("enabledMark", "状态");
        res.Add("description", "说明");
        res.Add("nation", "民族");
        res.Add("nativePlace", "籍贯");
        res.Add("entryDate", "入职时间");
        res.Add("certificatesType", "证件类型");
        res.Add("certificatesNumber", "证件号码");
        res.Add("education", "文化程度");
        res.Add("birthday", "出生年月");
        res.Add("telePhone", "办公电话");
        res.Add("landline", "办公座机");
        res.Add("mobilePhone", "手机号码");
        res.Add("urgentContacts", "紧急联系");
        res.Add("urgentTelePhone", "紧急电话");
        res.Add("postalAddress", "通讯地址");
        res.Add("errorsInfo", "异常原因");

        if (fields == null || !fields.Any()) return res;

        Dictionary<string, string>? result = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> item in res)
        {
            if (fields.Contains(item.Key))
                result.Add(item.Key, item.Value);
        }

        return result;
    }

    /// <summary>
    /// 导入用户数据函数.
    /// </summary>
    /// <param name="list">list.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> ImportUserData(List<UserListImportDataInput> list)
    {
        List<UserListImportDataInput> userInputList = list;

        #region 初步排除错误数据

        if (userInputList == null || userInputList.Count() < 1)
            throw Oops.Oh(ErrorCode.D5019);

        var regex = new Regex("^[a-z0-9A-Z\u4e00-\u9fa5]+$");

        // 必填字段验证 (账号，姓名，性别，所属组织)
        List<UserListImportDataInput>? errorList = new List<UserListImportDataInput>();
        userInputList.ForEach(item =>
        {
            if (item.account.IsNullOrWhiteSpace() && !item.errorsInfo.Contains("账号不能为空"))
            {
                item.errorsInfo += "账号不能为空;";
                errorList.Add(item);
            }
            if (item.realName.IsNullOrWhiteSpace() && !item.errorsInfo.Contains("姓名不能为空"))
            {
                item.errorsInfo += "姓名不能为空;";
                errorList.Add(item);
            }
            if (item.gender.IsNullOrWhiteSpace() && !item.errorsInfo.Contains("性别不能为空"))
            {
                item.errorsInfo += "性别不能为空;";
                errorList.Add(item);
            }
            if (item.organizeId.IsNullOrWhiteSpace() && !item.errorsInfo.Contains("所属组织不能为空"))
            {
                item.errorsInfo += "所属组织不能为空;";
                errorList.Add(item);
            }
            if (item.account.IsNotEmptyOrNull() && !regex.IsMatch(item.account) && !item.errorsInfo.Contains("账号不能含有特殊符号"))
            {
                item.errorsInfo += "账号不能含有特殊符号;";
                errorList.Add(item);
            }
        });

        // 上传重复的账号
        userInputList.ForEach(item =>
        {
            if (userInputList.Count(x => x.account == item.account) > 1)
            {
                var errorItems = userInputList.Where(x => x.account == item.account).ToList();
                errorItems.Remove(errorItems.First());
                errorItems.ForEach(item => item.errorsInfo = "账号已存在;");
                errorList.AddRange(errorItems);
            }
        });

        errorList = errorList.Distinct().ToList();
        userInputList = userInputList.Except(errorList).ToList();

        // 用户账号 (匹配直属主管 和 验证重复账号)
        List<UserEntity>? _userRepositoryList = await _repository.AsQueryable().Where(it => it.DeleteMark == null).Select(it => new UserEntity() { Id = it.Id, Account = it.Account }).ToListAsync();

        // 已存在的账号
        List<UserEntity>? repeat = _userRepositoryList.Where(u => userInputList.Select(x => x.account).Contains(u.Account)).ToList();

        // 已存在的账号 列入 错误列表
        if (repeat.Any())
        {
            var addList = userInputList.Where(u => repeat.Select(x => x.Account).Contains(u.account)).ToList();
            addList.ForEach(item => item.errorsInfo = "账号已存在;");
            errorList.AddRange(addList);
        }

        userInputList = userInputList.Except(errorList).ToList();

        #endregion

        List<UserEntity>? userList = new List<UserEntity>();

        #region 预处理关联表数据

        // 组织机构
        List<OrganizeEntity>? _organizeServiceList = await _organizeService.GetListAsync();
        Dictionary<string, string>? organizeDic = new Dictionary<string, string>();

        _organizeServiceList.ForEach(item =>
        {
            if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
            var orgNameList = new List<string>();
            item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
            {
                var org = _organizeServiceList.Find(x => x.Id == it);
                if (org != null) orgNameList.Add(org.FullName);
            });
            organizeDic.Add(item.Id, string.Join("/", orgNameList));
        });

        List<PositionEntity>? _positionRepositoryList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).ToListAsync(); // 岗位
        List<RoleEntity>? _roleRepositoryList = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).ToListAsync(); // 角色

        DictionaryTypeEntity? typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "963255a34ea64a2584c5d1ba269c1fe6" || x.EnCode == "sex") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _genderList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 性别

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "b6cd65a763fa45eb9fe98e5057693e40" || x.EnCode == "Nation") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _nationList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 民族

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "7866376d5f694d4d851c7164bd00ebfc" || x.EnCode == "certificateType") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? certificateTypeList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 证件类型

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "6a6d6fb541b742fbae7e8888528baa16" || x.EnCode == "Education") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? educationList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 文化程度

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "485719133245509" || x.EnCode == "Rank") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? ranksList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 职级

        #endregion

        // 处理多租户限定账号额度
        var maxAccountCount = 0;
        var addCount = 0;
        if (_tenant.MultiTenancy)
        {
            var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);
            if (tenatInfo.accountNum != 0)
                maxAccountCount = (int)(tenatInfo.accountNum - await _repository.AsQueryable().CountAsync(x => x.DeleteMark == null));
        }

        // 用户关系数据
        List<UserRelationEntity>? userRelationList = new List<UserRelationEntity>();
        foreach (UserListImportDataInput? item in userInputList)
        {
            addCount++;
            List<string>? orgIds = new List<string>(); // 多组织 , 号隔开
            List<string>? posIds = new List<string>(); // 多岗位 , 号隔开

            UserEntity? uentity = new UserEntity();
            uentity.Id = SnowflakeIdHelper.NextId();
            if (string.IsNullOrEmpty(uentity.HeadIcon)) uentity.HeadIcon = "001.png";
            uentity.Secretkey = Guid.NewGuid().ToString();

            var defaultPassWord = await _repository.AsSugarClient().Queryable<SysConfigEntity>()
                .Where(it => it.Key.Equals("newUserDefaultPassword"))
                .Select(it => it.Value)
                .FirstAsync();
            uentity.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt(defaultPassWord) + uentity.Secretkey); // 初始化密码
            uentity.ManagerId = _userRepositoryList.Find(x => x.Account == item.managerId?.Split('/').LastOrDefault())?.Id; // 寻找主管

            // 寻找角色
            if (item.roleId.IsNotEmptyOrNull() && item.roleId.Split(";").Any())
                uentity.RoleId = string.Join(",", _roleRepositoryList.Where(r => item.roleId.Split(";").Contains(r.FullName)).Select(x => x.Id).ToList());

            // 寻找组织
            var errorOrgList = new List<string>();
            string[]? userOidList = item.organizeId?.Split(";");
            if (userOidList != null && userOidList.Any())
            {
                foreach (string? oinfo in userOidList)
                {
                    if (organizeDic.ContainsValue(oinfo)) orgIds.Add(organizeDic.Where(x => x.Value == oinfo).FirstOrDefault().Key);
                    else errorOrgList.Add(oinfo);
                }
            }
            else
            {
                // 如果未找到组织，列入错误列表
                item.errorsInfo = "找不到该所属组织;";
                errorList.Add(item);
                continue;
            }

            // 存在未找到组织，列入错误列表
            if (errorOrgList.Any())
            {
                item.errorsInfo = string.Format("找不到该所属组织({0});", string.Join("、", errorOrgList));
                errorList.Add(item);
                continue;
            }

            // 性别
            if (!_genderList.Any(x => x.FullName == item.gender))
            {
                item.errorsInfo = "找不到该性别;";
                errorList.Add(item);
                continue;
            }

            // 寻找岗位
            item.positionId?.Split(';').ToList().ForEach(it =>
            {
                string[]? pinfo = it.Split("/");
                string? pid = _positionRepositoryList.Find(x => x.FullName == pinfo.FirstOrDefault() && x.EnCode == pinfo.LastOrDefault())?.Id;
                if (pid.IsNotEmptyOrNull()) posIds.Add(pid); // 多岗位
            });

            uentity.Gender = _genderList.Find(x => x.FullName == item.gender).EnCode;
            uentity.Nation = _nationList.Find(x => x.FullName == item.nation)?.Id; // 民族
            uentity.Education = educationList.Find(x => x.FullName == item.education)?.Id; // 文化程度
            uentity.CertificatesType = certificateTypeList.Find(x => x.FullName == item.certificatesType)?.Id; // 证件类型
            uentity.Ranks = ranksList.Find(x => x.FullName == item.ranks)?.Id; // 职级
            uentity.Account = item.account;
            uentity.Birthday = item.birthday.IsNotEmptyOrNull() ? item.birthday.ParseToDateTime() : null;
            uentity.CertificatesNumber = item.certificatesNumber;
            uentity.CreatorUserId = _userManager.UserId;
            uentity.CreatorTime = DateTime.Now;
            uentity.Description = item.description;
            uentity.Email = item.email;
            switch (item.enabledMark)
            {
                case "禁用":
                    uentity.EnabledMark = 0;
                    break;
                case "启用":
                    uentity.EnabledMark = 1;
                    break;
                case "锁定":
                    uentity.EnabledMark = 2;
                    break;
                default:
                    uentity.EnabledMark = 0;
                    break;
            }

            uentity.EntryDate = item.entryDate.IsNotEmptyOrNull() ? item.entryDate.ParseToDateTime() : null;
            uentity.Landline = item.landline;
            uentity.MobilePhone = item.mobilePhone;
            uentity.NativePlace = item.nativePlace;
            uentity.PostalAddress = item.postalAddress;
            uentity.RealName = item.realName;
            uentity.SortCode = item.sortCode.ParseToInt();
            uentity.TelePhone = item.telePhone;
            uentity.UrgentContacts = item.urgentContacts;
            uentity.UrgentTelePhone = item.urgentTelePhone;
            uentity.OrganizeId = orgIds.FirstOrDefault();

            // 岗位多组织 匹配
            var opIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId)).Select(x => x.Id).ToListAsync();
            posIds = opIds.Intersect(posIds).ToList();

            if (uentity.OrganizeId.IsNotEmptyOrNull())
            {
                List<UserRelationEntity>? roleList = _userRelationService.CreateUserRelation(uentity.Id, uentity.RoleId, "Role"); // 角色关系
                List<UserRelationEntity>? positionList = _userRelationService.CreateUserRelation(uentity.Id, string.Join(",", posIds), "Position"); // 岗位关系
                List<UserRelationEntity>? organizeList = _userRelationService.CreateUserRelation(uentity.Id, string.Join(",", orgIds), "Organize"); // 组织关系
                userRelationList.AddRange(positionList);
                userRelationList.AddRange(roleList);
                userRelationList.AddRange(organizeList);
            }

            if (_tenant.MultiTenancy && maxAccountCount != 0)
            {
                if (maxAccountCount >= addCount)
                {
                    userList.Add(uentity);
                }
                else
                {
                    item.errorsInfo = "用户额度已达到上限";
                    errorList.Add(item);
                }
            }
            else
            {
                userList.Add(uentity);
            }
        }

        if (userList.Any())
        {
            try
            {
                // 新增用户记录
                UserEntity? newEntity = await _repository.AsInsertable(userList).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

                // 批量新增用户关系
                if (userRelationList.Count > 0) await _userRelationService.Create(userRelationList);

            }
            catch (Exception)
            {
                userInputList.ForEach(item => item.errorsInfo = "系统异常");
                errorList.AddRange(userInputList);
                userInputList = new List<UserListImportDataInput>();
            }
        }

        foreach (var item in errorList) if (item.errorsInfo.Contains(";")) item.errorsInfo = item.errorsInfo.TrimEnd(';');
        return new object[] { userList, errorList };
    }

    #endregion
}
