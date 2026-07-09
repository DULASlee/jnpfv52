using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Ir;

public interface IIrSchemaValidator
{
    void Validate(string eventType, string payload);
}

/// <summary>
/// IR Schema 校验器（阶段九 P9-S1 升级）。
///
/// P1-B08 原版：只校验 SkeletonCreated，其余 fragment 直接 return（零校验）。
/// P9-S1 升级：对所有关键 fragment 按契约校验，不符合则 Oops.Bah 拒绝（触发 re-prompt）。
///
/// 校验力度：只做"结构完整性"校验（非空/必填字段/JSON 合法），
/// 不做"语义正确性"校验（那是 Gate 和测试验收的职责）。
/// </summary>
public sealed class IrSchemaValidator : IIrSchemaValidator, ITransient
{
    private readonly ILogger<IrSchemaValidator> _logger;

    public IrSchemaValidator(ILogger<IrSchemaValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Validate(string eventType, string payload)
    {
        _logger.LogDebug("Schema校验 eventType={EventType} payloadLen={Len}",
            eventType, payload?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(payload))
        {
            // 非关键事件允许空 payload
            if (!IsCriticalEvent(eventType))
            {
                _logger.LogDebug("非关键事件 {EventType} 允许空 payload，跳过校验", eventType);
                return;
            }
            _logger.LogWarning("校验失败 eventType={EventType}: payload 不能为空", eventType);
            throw Oops.Bah($"{eventType} payload 不能为空");
        }

        try
        {
            switch (eventType)
            {
                case IrEventTypes.SkeletonCreated:
                    ValidateSkeleton(payload);
                    break;

                case IrEventTypes.DDLStabilized:
                    ValidateDdl(payload);
                    break;

                case IrEventTypes.UIDesignStabilized:
                    ValidateFormPage(payload);
                    break;

                case IrEventTypes.EventSpecConfirmed:
                    ValidateEventSpec(payload);
                    break;

                case IrEventTypes.ArchitectureDecisionRecorded:
                    ValidateArchitecture(payload);
                    break;

                // 非关键事件（FragmentStabilized / CodeGenerated 等）不强制校验
                default:
                    break;
            }
        }
        catch (JNPF.FriendlyException.AppFriendlyException)
        {
            throw; // Oops.Bah 业务异常直接抛出
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "校验失败 eventType={EventType}: JSON 解析异常", eventType);
            throw Oops.Bah($"{eventType} payload JSON 解析失败: {ex.Message}");
        }
    }

    /// <summary>是否关键事件（需要强校验）</summary>
    private static bool IsCriticalEvent(string eventType) =>
        eventType is IrEventTypes.SkeletonCreated
            or IrEventTypes.DDLStabilized
            or IrEventTypes.UIDesignStabilized
            or IrEventTypes.EventSpecConfirmed;

    // ─── 各 fragment 校验 ───

    private static void ValidateSkeleton(string payload)
    {
        // 用契约 Parse + Validate（双向兼容解析 + 结构校验）
        var skeleton = SkeletonPayload.Parse(payload);
        skeleton.Validate();
    }

    private static void ValidateDdl(string payload)
    {
        var ddl = DdlPayload.Parse(payload);
        ddl.Validate();
    }

    private static void ValidateFormPage(string payload)
    {
        var formPage = FormPagePayload.Parse(payload);
        formPage.Validate();
    }

    private static void ValidateEventSpec(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("eventId", out var idEl) || idEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(idEl.GetString()))
        {
            throw Oops.Bah("EventSpec 缺少 eventId");
        }

        // confirmedFields 非空（至少有主键字段）
        if (!root.TryGetProperty("confirmedFields", out var fieldsEl)
            || fieldsEl.ValueKind != JsonValueKind.Array
            || fieldsEl.GetArrayLength() == 0)
        {
            throw Oops.Bah("EventSpec 缺少非空 confirmedFields");
        }
    }

    private static void ValidateArchitecture(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        // modules 非空（唯一强校验，与原 ArchitectSkillService 一致）
        if (!root.TryGetProperty("modules", out var modEl)
            || modEl.ValueKind != JsonValueKind.Array
            || modEl.GetArrayLength() == 0)
        {
            throw Oops.Bah("Architecture 缺少非空 modules 数组");
        }
    }
}
