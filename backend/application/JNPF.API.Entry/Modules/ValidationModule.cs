using FluentValidation;
using FluentValidation.AspNetCore;
using JNPF.API.Entry.Validators;
using JNPF.Modules;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// FluentValidation 验证模块 — 替换裸 throw 为声明式验证.
/// </summary>
[JNPF.Modules.DependsOn(typeof(JsonSettingsModule))]
public class ValidationModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddFluentValidationAutoValidation(config =>
        {
            config.DisableDataAnnotationsValidation = false;
        });

        services.AddFluentValidationClientsideAdapters();

        services.AddValidatorsFromAssemblyContaining<UserCrInputValidator>();
    }
}
