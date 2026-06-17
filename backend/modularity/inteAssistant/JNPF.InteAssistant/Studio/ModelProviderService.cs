using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text;
using System.Text.Json;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 模型供应商配置服务
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "ModelProvider", Order = 210)]
[Route("api/studio/pipeline")]
public class ModelProviderService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public ModelProviderService(
        ISqlSugarClient db,
        IUserManager userManager,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
    }

    #region CRUD

    /// <summary>
    /// 供应商列表
    /// </summary>
    [HttpGet("providers")]
    public async Task<dynamic> GetList()
    {
        var list = await _db.Queryable<ModelProviderEntity>()
            .Where(x => !x.F_DeleteMark)
            .OrderBy(x => x.F_Priority)
            .OrderByDescending(x => x.F_CreatorTime)
            .ToListAsync();

        var safeList = list.Select(x => new
        {
            id = x.F_Id,
            providerCode = x.F_ProviderCode,
            name = x.F_Name,
            baseUrl = x.F_BaseUrl,
            apiKeyMasked = MaskApiKey(x.F_ApiKey),
            defaultModel = x.F_DefaultModel,
            maxTokens = x.F_MaxTokens,
            temperature = x.F_Temperature,
            status = x.F_Status,
            priority = x.F_Priority,
            enabled = x.F_Enabled,
            description = x.F_Description,
            lastTestTime = x.F_LastTestTime,
            lastTestResult = x.F_LastTestResult,
            creatorTime = x.F_CreatorTime
        }).ToList();

        return new { items = safeList, total = safeList.Count };
    }

    /// <summary>
    /// 供应商详情
    /// </summary>
    [HttpGet("providers/{id}")]
    public async Task<dynamic> GetDetail(long id)
    {
        var entity = await _db.Queryable<ModelProviderEntity>()
            .Where(x => x.F_Id == id && !x.F_DeleteMark)
            .FirstAsync();

        if (entity == null) throw new Exception("供应商不存在");

        return new
        {
            id = entity.F_Id,
            providerCode = entity.F_ProviderCode,
            name = entity.F_Name,
            baseUrl = entity.F_BaseUrl,
            apiKeyMasked = MaskApiKey(entity.F_ApiKey),
            defaultModel = entity.F_DefaultModel,
            maxTokens = entity.F_MaxTokens,
            temperature = entity.F_Temperature,
            status = entity.F_Status,
            priority = entity.F_Priority,
            enabled = entity.F_Enabled,
            description = entity.F_Description
        };
    }

    /// <summary>
    /// 创建供应商
    /// </summary>
    [HttpPost("providers")]
    public async Task<long> Create([FromBody] ProviderCreateInput input)
    {
        var exists = await _db.Queryable<ModelProviderEntity>()
            .AnyAsync(x => x.F_ProviderCode == input.ProviderCode && !x.F_DeleteMark);
        if (exists) throw new Exception($"供应商编码 {input.ProviderCode} 已存在");

        var entity = new ModelProviderEntity
        {
            F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            F_ProviderCode = input.ProviderCode,
            F_Name = input.Name,
            F_BaseUrl = input.BaseUrl,
            F_ApiKey = input.ApiKey,
            F_DefaultModel = input.DefaultModel,
            F_MaxTokens = input.MaxTokens ?? 1000000,
            F_Temperature = input.Temperature ?? 0.7m,
            F_Status = "testing",
            F_Priority = input.Priority,
            F_Enabled = true,
            F_Description = input.Description,
            F_CreatorTime = DateTime.Now,
            F_CreatorUserId = long.TryParse(_userManager.UserId, out var uid) ? uid : null
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.F_Id;
    }

    /// <summary>
    /// 更新供应商
    /// </summary>
    [HttpPut("providers/{id}")]
    public async Task Update(long id, [FromBody] ProviderUpdateInput input)
    {
        var entity = await _db.Queryable<ModelProviderEntity>()
            .Where(x => x.F_Id == id && !x.F_DeleteMark)
            .FirstAsync();
        if (entity == null) throw new Exception("供应商不存在");

        entity.F_Name = input.Name ?? entity.F_Name;
        entity.F_BaseUrl = input.BaseUrl ?? entity.F_BaseUrl;

        if (!string.IsNullOrEmpty(input.ApiKey) && input.ApiKey != "***********")
            entity.F_ApiKey = input.ApiKey;

        entity.F_DefaultModel = input.DefaultModel ?? entity.F_DefaultModel;
        entity.F_MaxTokens = input.MaxTokens ?? entity.F_MaxTokens;
        entity.F_Temperature = input.Temperature ?? entity.F_Temperature;
        entity.F_Priority = input.Priority ?? entity.F_Priority;
        entity.F_Description = input.Description ?? entity.F_Description;
        entity.F_ModifyTime = DateTime.Now;
        entity.F_ModifyUserId = long.TryParse(_userManager.UserId, out var uid) ? uid : null;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除供应商
    /// </summary>
    [HttpDelete("providers/{id}")]
    public async Task Delete(long id)
    {
        await _db.Updateable<ModelProviderEntity>()
            .SetColumns(x => x.F_DeleteMark, true)
            .SetColumns(x => x.F_ModifyTime, DateTime.Now)
            .Where(x => x.F_Id == id)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 启用/禁用
    /// </summary>
    [HttpPut("providers/{id}/toggle")]
    public async Task Toggle(long id)
    {
        var entity = await _db.Queryable<ModelProviderEntity>()
            .Where(x => x.F_Id == id && !x.F_DeleteMark)
            .FirstAsync();
        if (entity == null) throw new Exception("供应商不存在");

        entity.F_Enabled = !entity.F_Enabled;
        entity.F_ModifyTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    #endregion

    #region 测试连接

    /// <summary>
    /// 测试供应商连接
    /// </summary>
    [HttpPost("providers/{id}/test")]
    public async Task<dynamic> TestConnection(long id)
    {
        var entity = await _db.Queryable<ModelProviderEntity>()
            .Where(x => x.F_Id == id && !x.F_DeleteMark)
            .FirstAsync();
        if (entity == null) throw new Exception("供应商不存在");

        var start = DateTime.Now;
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var requestBody = new
            {
                model = entity.F_DefaultModel,
                messages = new[] { new { role = "user", content = "ping" } },
                max_tokens = 50,
                temperature = 0.1
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{entity.F_BaseUrl}/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {entity.F_ApiKey}");
            request.Content = content;

            var response = await client.SendAsync(request);
            var latency = (DateTime.Now - start).TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                entity.F_Status = "healthy";
                entity.F_LastTestTime = DateTime.Now;
                entity.F_LastTestResult = $"✅ 连接成功 | HTTP {(int)response.StatusCode} | 延迟 {latency:F0}ms";

                await _db.Updateable(entity)
                    .UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult })
                    .ExecuteCommandAsync();

                return new { success = true, message = entity.F_LastTestResult, latency, httpStatus = (int)response.StatusCode, model = entity.F_DefaultModel, provider = entity.F_ProviderCode };
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                entity.F_Status = "degraded";
                entity.F_LastTestTime = DateTime.Now;
                entity.F_LastTestResult = $"❌ 连接失败 | HTTP {(int)response.StatusCode}";

                await _db.Updateable(entity)
                    .UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult })
                    .ExecuteCommandAsync();

                return new { success = false, message = entity.F_LastTestResult, latency, httpStatus = (int)response.StatusCode };
            }
        }
        catch (TaskCanceledException)
        {
            entity.F_Status = "offline";
            entity.F_LastTestTime = DateTime.Now;
            entity.F_LastTestResult = "❌ 连接超时 (30s)";

            await _db.Updateable(entity)
                .UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult })
                .ExecuteCommandAsync();

            return new { success = false, message = "连接超时 (30s)", latency = 30000d };
        }
        catch (Exception ex)
        {
            entity.F_Status = "offline";
            entity.F_LastTestTime = DateTime.Now;
            entity.F_LastTestResult = $"❌ 连接异常: {ex.Message}";

            await _db.Updateable(entity)
                .UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult })
                .ExecuteCommandAsync();

            return new { success = false, message = ex.Message, latency = (DateTime.Now - start).TotalMilliseconds };
        }
    }

    #endregion

    #region 内部方法

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return "未配置";
        if (apiKey.Length <= 8) return "****";
        return apiKey.Substring(0, 4) + "****" + apiKey.Substring(apiKey.Length - 4);
    }

    #endregion
}

#region DTO

public class ProviderCreateInput
{
    public string ProviderCode { get; set; }
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
    public string DefaultModel { get; set; }
    public long? MaxTokens { get; set; }
    public decimal? Temperature { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; }
}

public class ProviderUpdateInput
{
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
    public string DefaultModel { get; set; }
    public long? MaxTokens { get; set; }
    public decimal? Temperature { get; set; }
    public int? Priority { get; set; }
    public string Description { get; set; }
}

#endregion
