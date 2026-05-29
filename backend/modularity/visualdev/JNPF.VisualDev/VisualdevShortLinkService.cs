using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Net;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发外链.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "ShortLink", Order = 175)]
[Route("api/visualdev/[controller]")]
public class VisualdevShortLinkService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevShortLinkEntity> _repository;

    private readonly RunService _runService;

    /// <summary>
    /// 可视化开发基础.
    /// </summary>
    private readonly IVisualDevService _visualDevService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private SqlSugarScope _sqlSugarClient;

    /// <summary>
    /// 初始化一个<see cref="VisualdevModelAppService"/>类型的新实例.
    /// </summary>
    public VisualdevShortLinkService(
        ISqlSugarRepository<VisualDevShortLinkEntity> repository,
        IUserManager userManager,
        ITenantManager tenantManager,
        IVisualDevService visualDevService,
        RunService runService,
        IOptions<TenantOptions> tenantOptions,
        ISqlSugarClient sqlSugarClient)
    {
        _repository = repository;
        _visualDevService = visualDevService;
        _runService = runService;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _tenant = tenantOptions.Value;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
    }

    private readonly string _shortLinkKey = App.GetConfig<AppOptions>("JNPF_App", true).AesKey;

    #region Get

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        var res = new VisualDevShortLinkInfoOutput() { id = id };
        if (data != null)
        {
            res = data.Adapt<VisualDevShortLinkInfoOutput>();
            res.alreadySave = true;
        }
        res.formLink = GetUrl(id, "form");
        res.columnLink = GetUrl(id, "list");

        return res;
    }

    /// <summary>
    /// 获取外链配置.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("getConfig/{id}")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetConfig(string id, [FromQuery] VisualDevShortLinkInput model)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(model.encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        var info = await _repository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        var res = info.Adapt<VisualdevShortLinkConfigOutput>();
        res.formLink = GetUrl(id, "form");
        res.columnLink = GetUrl(id, "list");
        return res;
    }

    /// <summary>
    /// 外链请求入口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("trigger/{id}")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task GetLink(string id, [FromQuery] VisualDevShortLinkInput model)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(model.encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        var link = string.Empty;
        var decValue = AESEncryption.AesDecrypt(model.encryption, _shortLinkKey);
        var modelDic = decValue.ToObject<VisualDevShortLinkInput>();
        var entity = await _repository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (entity != null)
        {
            UserAgent userAgent = new UserAgent(App.HttpContext);
            if (!userAgent.IsMobileDevice) link = entity.RealPcLink;
            else link = entity.RealAppLink;
        }
        else
        {
            throw Oops.Oh(ErrorCode.D1420);
        }

        var parame = new Dictionary<string, object>();
        parame.Add("modelId", id);
        parame.Add("type", modelDic.type);
        parame.Add("tenantId", modelDic.tenantId);
        var encryption = AESEncryption.AesEncrypt(parame.ToJsonString(), _shortLinkKey);
        link += "&encryption=" + encryption;
        App.HttpContext.Response.Redirect(link);
    }

    /// <summary>
    /// 获取列表表单配置JSON.
    /// </summary>
    [HttpGet("{modelId}/Config")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetConfig(string modelId, string type, string encryption)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        var data = await _visualDevService.GetInfoById(modelId);
        if (data == null) throw Oops.Oh(ErrorCode.D1420);

        if (data == null) return new { code = 400, msg = "未找到该模板!" };
        else if (data.WebType.Equals(1) && data.FormData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内表单内容为空，无法预览!" };
        else if (data.WebType.Equals(2) && data.ColumnData.IsNullOrWhiteSpace()) return new { code = 400, msg = "该模板内列表内容为空，无法预览!" };
        return data.Adapt<VisualdevShortLinkFormConfigOutput>();
    }

    /// <summary>
    /// 获取数据信息(带转换数据).
    /// </summary>
    [HttpGet("{modelId}/{id}/DataChange")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> InfoWithDataChange(string modelId, string id, string encryption)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId); // 模板实体
        templateEntity.isShortLink = true;
        // 有表
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = await _runService.GetHaveTableInfoDetails(id, templateEntity, false, visualDevShortLinkInput.tenantId) };
        else
            return null;
    }

    #endregion

    #region Post

    /// <summary>
    /// 修改外链信息.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("")]
    public async Task SaveOrUpdate([FromBody] VisualdevShortLinkFormInput data)
    {
        var entity = data.ToObject<VisualDevShortLinkEntity>();
        var info = await _repository.AsQueryable().FirstAsync(v => v.Id == data.id && v.DeleteMark == null);
        if (info != null)
        {
            entity.LastModifyTime = DateTime.Now;
            entity.LastModifyUserId = _userManager.UserId;
        }
        else
        {
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
        }

        // 本地url地址
        var pcAddress = App.Configuration["Message:DoMainPc"];
        var appAddress = App.Configuration["Message:DoMainApp"];
        var pcLink = pcAddress + "/formShortLink?modelId=" + data.id;
        var appLink = appAddress + "/pages/formShortLink/index?modelId=" + data.id;
        entity.RealPcLink = pcLink;
        entity.RealAppLink = appLink;
        entity.UserId = _userManager.UserId;
        entity.TenantId = _userManager.TenantId;

        var stor = _repository.AsSugarClient().Storageable(entity).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
        await stor.AsInsertable.ExecuteCommandAsync(); // 执行插入
        await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 密码验证.
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    [HttpPost("checkPwd")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task CheckPwd([FromBody] VisualDevShortLinkPwdInput form)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(form.encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        var info = await _repository.AsQueryable().FirstAsync(v => v.Id == form.id && v.DeleteMark == null);
        var flag = false;

        if (CommonConst.OnlineDevData_State_Enable.Equals(info.FormPassUse) && form.type.Equals(0))
        {
            if (MD5Encryption.Encrypt(info.FormPassword).Equals(form.password))
            {
                flag = true;
            }
        }
        else if (CommonConst.OnlineDevData_State_Enable.Equals(info.ColumnPassUse) && form.type.Equals(1))
        {
            if (MD5Encryption.Encrypt(info.ColumnPassword).Equals(form.password))
            {
                flag = true;
            }
        }

        if (!flag)
        {
            throw Oops.Oh(ErrorCode.D1418);
        }
    }

    /// <summary>
    /// 外链数据列表.
    /// </summary>
    [HttpPost("{modelId}/ListLink")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> ListLink(string modelId, string encryption, [FromBody] VisualDevModelListQueryInput input)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId);
        templateEntity.isShortLink = true;
        return await _runService.GetListResult(templateEntity, input, "List", visualDevShortLinkInput.tenantId);
    }

    /// <summary>
    /// 添加数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="encryption"></param>
    /// <param name="visualdevModelDataCrForm"></param>
    /// <returns></returns>
    [HttpPost("{modelId}")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task Create(string modelId, string encryption, [FromBody] VisualDevModelDataCrInput visualdevModelDataCrForm)
    {
        var visualDevShortLinkInput = AESEncryption.AesDecrypt(encryption, _shortLinkKey).ToObject<VisualDevShortLinkInput>();
        await InitConnectionConfig(visualDevShortLinkInput);

        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId);
        templateEntity.isShortLink = true;
        await _runService.Create(templateEntity, visualdevModelDataCrForm, visualDevShortLinkInput.tenantId);

    }
    #endregion

    #region PrivateMethod

    private string GetUrl(string id, string type)
    {
        // 前端PC外网能访问的地址(域名)
        var localAddress = App.Configuration["Message:ApiDoMain"];

        // 拼接url地址
        var url = localAddress + "/api/visualdev/ShortLink/trigger/" + id + "?encryption=";

        var parame = new Dictionary<string, object>();
        parame.Add("type", type);
        if (_tenant.MultiTenancy) parame.Add("tenantId", _userManager.TenantId);

        // 参数加密
        var encryption = AESEncryption.AesEncrypt(parame.ToJsonString(), _shortLinkKey);
        url += encryption;
        return url;
    }

    private async Task InitConnectionConfig(VisualDevShortLinkInput visualDevShortLinkInput)
    {
        if (visualDevShortLinkInput.tenantId.IsNotEmptyOrNull() && _tenant.MultiTenancy)
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, visualDevShortLinkInput.tenantId);
        }
    }

    #endregion
}