using JNPF.Common.Configuration;
using JNPF.Common.Core.Job;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Permission;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Net.Http.Headers;
using System.Text;

namespace JNPF.Schedule;

/// <summary>
/// JNPF-HTTP 请求作业处理程序.
/// </summary>
[SuppressSniffer]
public class JNPFHttpJob : IJob
{
    /// <summary>
    /// <see cref="HttpClient"/> 创建工厂.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 日志服务.
    /// </summary>
    private readonly ILogger<JNPFHttpJob> _logger;

    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="serviceProvider">服务提供器.</param>
    /// <param name="context"><see cref="ISqlSugarClient"/> 客户端.</param>
    /// <param name="httpClientFactory"><see cref="HttpClient"/> 创建工厂.</param>
    /// <param name="logger">日志服务.</param>
    public JNPFHttpJob(
        IServiceProvider serviceProvider,
        ISqlSugarClient context,
        IHttpClientFactory httpClientFactory,
        ITenantManager tenantManager,
        ILogger<JNPFHttpJob> logger)
    {
        _serviceProvider = serviceProvider;
        _sqlSugarClient = (SqlSugarScope)context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 具体处理逻辑.
    /// </summary>
    /// <param name="context">作业执行前上下文.</param>
    /// <param name="stoppingToken">取消任务 Token.</param>
    /// <returns><see cref="Task"/></returns>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var jobDetail = context.JobDetail;

        // 解析 HTTP 请求参数，键名称为类名
        var httpJobMessage = Penetrates.Deserialize<JNPFHttpJobMessage>(jobDetail.GetProperty<string>(nameof(JNPFHttpJob)));

        // 空检查
        if (httpJobMessage == null || string.IsNullOrWhiteSpace(httpJobMessage.RequestUri))
        {
            return;
        }

        // 创建请求客户端
        using var httpClient = _httpClientFactory.CreateClient(); // CreateClient 可以传入一个字符串进行全局配置 Client

        // 添加请求报文头 User-Agent
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.81 Safari/537.36 Edg/104.0.1293.47");

        // 获取到 用户信息
        if (KeyVariable.MultiTenancy)
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, httpJobMessage.TenantId);
        }

        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(httpJobMessage.UserId));

        // 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, httpJobMessage.TenantId);

        // 添加请求报文头 Authorization
        httpClient.DefaultRequestHeaders.Add("Authorization", toKen);

        // 创建请求对象
        var httpRequestMessage = new HttpRequestMessage(httpJobMessage.HttpMethod, httpJobMessage.RequestUri);

        // 添加请求报文体，默认只支持发送 application/json 类型
        if (httpJobMessage.HttpMethod != HttpMethod.Get
            && httpJobMessage.HttpMethod != HttpMethod.Head
            && !string.IsNullOrWhiteSpace(httpJobMessage.Body))
        {
            var stringContent = new StringContent(httpJobMessage.Body, Encoding.UTF8);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpRequestMessage.Content = stringContent;
        }

        // 添加请求头
        if (httpJobMessage.Headers != null && httpJobMessage.Headers.Count > 0)
        {
            foreach (var (name, value) in httpJobMessage.Headers)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(name, value);
            }
        }

        // 发送请求
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, stoppingToken);

        // 确保请求成功
        if (httpJobMessage.EnsureSuccessStatusCode)
        {
            httpResponseMessage = httpResponseMessage.EnsureSuccessStatusCode();
        }

        // 解析返回值
        var bodyString = await httpResponseMessage.Content.ReadAsStringAsync(stoppingToken);

        // 输出日志
        _logger.LogInformation($"Received HTTP response body with a length of <{bodyString.Length}> output as follows - {(int)httpResponseMessage.StatusCode}{Environment.NewLine}{bodyString}");

        // 设置本次执行结果
        context.Result = Penetrates.Serialize(new {
            httpResponseMessage.StatusCode,
            Body = bodyString
        });
    }
}