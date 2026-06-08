using JNPF.Common.Core.Filter;
using JNPF.Modules;
using JNPF.UnifyResult;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// JSON 序列化 + MVC 控制器 + 限流模块.
/// </summary>
public class JsonSettingsModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // MVC 控制器 + JSON 序列化
        services.AddControllers()
            .AddMvcFilter<RequestActionFilter>()
            .AddInjectWithUnifyResult<RESTfulResultProvider>()
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null)
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = 64;
                options.SerializerSettings.Converters.AddDateTimeTypeConverters();
                options.SerializerSettings.Converters.AddClayConverters();
                options.SerializerSettings.Formatting = Formatting.Indented;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        // 特殊 JSON 选项
        services.AddUnifyJsonOptions("special", new JsonSerializerSettings
        {
            MaxDepth = 64,
            ContractResolver = new DefaultContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        services.AddUnifyJsonOptions("datainterfaceSpecial", new JsonSerializerSettings
        {
            MaxDepth = 64,
            ContractResolver = new DefaultContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }
}
