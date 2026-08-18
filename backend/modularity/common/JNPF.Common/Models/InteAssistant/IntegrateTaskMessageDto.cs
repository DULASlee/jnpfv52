using JNPF.DependencyInjection;

namespace JNPF.Common.Models.InteAssistant;

/// <summary>
/// Message-side view of an integrate task for WeChat mini-program push.
/// Keeps Message.Interfaces free of JNPF.InteAssistant.Entitys ProjectReference (W4+).
/// </summary>
[SuppressSniffer]
public class IntegrateTaskMessageDto
{
    /// <summary>
    /// Task payload JSON (BASE_INTEGRATE_TASK.F_DATA).
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// Template design JSON (BASE_INTEGRATE_TASK.F_TEMPLATE_JSON).
    /// </summary>
    public string TemplateJson { get; set; }

    /// <summary>
    /// Map payload fields without a compile-time entity dependency.
    /// Caller decides nullability of the DTO (null entity → do not call).
    /// </summary>
    public static IntegrateTaskMessageDto From(string? data, string? templateJson) =>
        new()
        {
            Data = data,
            TemplateJson = templateJson,
        };
}
