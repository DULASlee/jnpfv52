using IGeekFan.AspNetCore.Knife4jUI;
using JNPF.API.Entry.Handlers;
using JNPF.API.Entry.Infrastructure;
using JNPF.Common.Cache;
using JNPF.Common.Core;
using JNPF.Common.Core.Filter;
using JNPF.Common.Core.Handlers;
using JNPF.EventHandler;
using JNPF.SpecificationDocument;
using JNPF.UnifyResult;
using JNPF.VirtualFileServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;
using SqlSugar;

namespace JNPF.API.Entry;

public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // SqlSugar
        services.SqlSugarConfigure();

        // Jwt处理程序
        services.AddJwt<JwtHandler>(enableGlobalAuthorize: true, jwtBearerConfigure: options =>
        {
            // 实现 JWT 身份验证过程控制
            options.Events = new JwtBearerEvents
            {
                // 添加读取 Token 的方式
                OnMessageReceived = context =>
                {
                    var httpContext = context.HttpContext;

                    // 判断请求是否包含 token 参数，如果有就设置给 Token
                    if (httpContext.Request.Query.ContainsKey("token"))
                    {
                        var token = httpContext.Request.Query["token"].ToString();

                        switch (token.StartsWith("Bearer") || token.StartsWith("bearer"))
                        {
                            case true:
                                token = token.Replace("Bearer", string.Empty).Replace("bearer", string.Empty);
                                break;
                        }

                        // 去除头部空格
                        token = token.TrimStart();
                        switch (token.StartsWith("%20"))
                        {
                            case true:
                                token = token.Replace("%20", string.Empty);
                                break;
                        }

                        // 设置 Token
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },

                // Token 验证通过处理
                OnTokenValidated = context => Task.CompletedTask,
            };
        });

        // 跨域
        services.AddCorsAccessor();

        // 注册远程请求
        services.AddRemoteRequest();

        // 任务队列
        services.AddTaskQueue();

        // 任务调度
        services.AddSchedule(options => options.AddPersistence<DbJobPersistence>());

        services.AddConfigurableOptions<CacheOptions>();
        services.AddConfigurableOptions<EventBusOptions>();
        services.AddConfigurableOptions<SqlSugar.ConnectionStringsOptions>();
        services.AddConfigurableOptions<TenantOptions>();

        services.AddControllers()
        .AddMvcFilter<RequestActionFilter>()
        .AddInjectWithUnifyResult<RESTfulResultProvider>()
        .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null)
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = 64;

                options.SerializerSettings.Converters.AddDateTimeTypeConverters();

                options.SerializerSettings.Converters.AddClayConverters();

                // 格式化JSON文本
                options.SerializerSettings.Formatting = Formatting.Indented;

                // 默认命名规则
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();

                // 设置时区为 Utc
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

                // 格式化json输出的日期格式
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                // 忽略空值
                //options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

                // 忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        services.AddUnifyJsonOptions("special", new JsonSerializerSettings
        {
            MaxDepth = 64,

            // 默认命名规则
            ContractResolver = new DefaultContractResolver(),

            // 设置时区为 Utc
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,

            // 格式化json输出的日期格式
            DateFormatString = "yyyy-MM-dd HH:mm:ss",

            // 忽略循环引用
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        services.AddUnifyJsonOptions("datainterfaceSpecial", new JsonSerializerSettings
        {
            MaxDepth = 64,

            // 默认命名规则
            ContractResolver = new DefaultContractResolver(),

            // 设置时区为 Utc
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,

            // 格式化json输出的日期格式
            DateFormatString = "yyyy-MM-dd HH:mm:ss",

            // 忽略空值
            NullValueHandling = NullValueHandling.Ignore,

            // 忽略循环引用
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        // 配置Nginx转发获取客户端真实IP
        // 注1：如果负载均衡不是在本机通过 Loopback 地址转发请求的，一定要加上options.KnownNetworks.Clear()和options.KnownProxies.Clear()
        // 注2：如果设置环境变量 ASPNETCORE_FORWARDEDHEADERS_ENABLED 为 True，则不需要下面的配置代码
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // 注册EventBus服务
        services.AddEventBus(options =>
        {
            var config = App.GetOptions<EventBusOptions>();

            if (config.EventBusType != EventBusType.Memory)
            {
                switch (config.EventBusType)
                {
                    case EventBusType.RabbitMQ:
                        // 创建连接工厂
                        var factory = new RabbitMQ.Client.ConnectionFactory
                        {
                            // 设置主机名
                            HostName = config.HostName,

                            // 用户名
                            UserName = config.UserName,

                            // 密码
                            Password = config.Password,
                        };

                        // 创建默认内存通道事件源对象，可自定义队列路由key，比如这里是 eventbus
                        var rbmqEventSourceStorer = new RabbitMQEventSourceStorer(factory, "eventbus", 3000);

                        // 替换默认事件总线存储器
                        options.ReplaceStorer(serviceProvider =>
                        {
                            return rbmqEventSourceStorer;
                        });
                        break;
                }
            }

            options.UseUtcTimestamp = false;

            // 启用事件日志以便排查事件处理异常
            options.LogEnabled = true;

            // 事件执行器（失败重试）
            options.AddExecutor<RetryEventHandlerExecutor>();
        });

        // 视图引擎
        services.AddViewEngine();

        // 脱敏词汇检测
        services.AddSensitiveDetection();

        // WebSocket服务
        services.AddWebSocketManager();

        // 微信
        services.AddSenparcGlobalServices(App.Configuration) // Senparc.CO2NET 全局注册
                    .AddSenparcWeixinServices(App.Configuration); // Senparc.Weixin 注册（如果使用Senparc.Weixin SDK则添加）
        services.AddSession();

        // 使用本地缓存必须添加
        // services.AddMemoryCache();

        // 日志监听
        // services.AddMonitorLogging(options =>
        //{
        //    options.IgnorePropertyNames = new[] { "Byte" };
        //    options.IgnorePropertyTypes = new[] { typeof(byte[]) };
        //});

        services.OSSServiceConfigure();

        services.AddHttpContextAccessor();

        // Swagger 文档缓存（须在 AddSwaggerGen 之后注册）
        services.AddCachingSwaggerProvider();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
    {
        // 添加状态码拦截中间件
        app.UseUnifyResultStatusCodes();

        // app.UseHttpsRedirection(); // 强制https
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = FS.GetFileExtensionContentTypeProvider()
        });

        #region 微信

        IRegisterService register = RegisterService.Start(senparcSetting.Value).UseSenparcGlobal();//启动 CO2NET 全局注册，必须！
        register.UseSenparcWeixin(senparcWeixinSetting.Value, senparcSetting.Value);//微信全局注册,必须！

        #endregion

        app.UseWebSockets();

        // TraceId - must be early in pipeline
        app.UseMiddleware<TraceIdMiddleware>();

        app.UseRouting();

        app.UseCorsAccessor();

        app.UseAuthentication();
        app.UseAuthorization();

        // 任务调度看板
        app.UseScheduleUI();

        app.UseKnife4UI(options =>
        {
            options.RoutePrefix = "newapi";  // 配置 Knife4UI 路由地址，现在是 /newapi
            foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
            {
                options.SwaggerEndpoint("/" + groupInfo.RouteTemplate, groupInfo.Title);
            }
        });

        app.UseInject(string.Empty);

        app.MapWebSocketManager("/api/message/websocket", serviceProvider.GetService<IMHandler>());

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        // 启动时预热 Swagger，避免 Knife4UI 首次请求超过 20s 超时
        serviceProvider.WarmupSwagger();
    }
}