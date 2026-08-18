using System.Threading;
using System.Threading.Tasks;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Ir;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio.VisualDev;

/// <summary>
/// P8-M01 IR→VisualDev 映射 API。
///
/// POST /api/studio/visualdev/map/{pipelineId} — 将 pipeline 的 FormPageIR + EventSpec 映射为 VisualDev formData JSON。
/// 缺口不 silent drop：每个未映射字段写 MappingGapReported IR 事件 + 返回 gaps 清单。
/// 三元组 R12：从 IR snapshot 隔离校验。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "VisualDevMapper", Order = 205)]
[Route("api/studio/visualdev")]
public class VisualDevMapperApiService : IDynamicApiController, ITransient
{
    private readonly IIr1ToVisualDevMapper _mapper;
    private readonly IIrEventStoreService _eventStore;
    private readonly IUserManager _userManager;

    public VisualDevMapperApiService(
        IIr1ToVisualDevMapper mapper,
        IIrEventStoreService eventStore,
        IUserManager userManager)
    {
        _mapper = mapper;
        _eventStore = eventStore;
        _userManager = userManager;
    }

    private string TenantId() => _userManager.TenantId ?? string.Empty;

    /// <summary>
    /// POST /api/studio/visualdev/map/{pipelineId}
    /// 将 pipeline 的 IR（FormPageIR + EventSpec）映射为 VisualDev formData JSON。
    /// 返回结果含 formDataJson + gaps；调用方可据此 POST /api/visualdev/Base 创建表单。
    /// </summary>
    [HttpPost("map/{pipelineId}")]
    public async Task<object> MapToVisualDev(long pipelineId, [FromBody] VisualDevMapInput? input, CancellationToken ct)
    {
        var tenantId = TenantId();

        // 解析 projectId（pipeline 对应的 project）— 从 IR snapshot 反查或入参
        var projectId = !string.IsNullOrEmpty(input?.ProjectId)
            ? input.ProjectId
            : pipelineId.ToString();  // greenfield 自锚定：projectId ≡ pipelineId

        // 执行映射
        var result = await _mapper.MapAsync(tenantId, projectId, pipelineId.ToString(), ct);

        // 缺口事件化（每个 gap 写 MappingGapReported IR 事件，不 silent drop）
        if (result.Gaps.Count > 0)
        {
            foreach (var gap in result.Gaps)
            {
                await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
                {
                    EventType = "mapping.gap_reported",
                    SkillId = "ir1-to-visualdev-mapper",
                    FragmentId = $"visualdev-gap:{gap.FieldId}",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        pipelineId,
                        fieldId = gap.FieldId,
                        label = gap.Label,
                        componentType = gap.ComponentType,
                        reason = gap.Reason,
                        mappedAt = System.DateTime.UtcNow,
                    }),
                }, ct);
            }
        }

        // 总览事件：VisualDevMappingCompleted（成功/部分成功均记录）
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = "visualdev.mapping_completed",
            SkillId = "ir1-to-visualdev-mapper",
            FragmentId = $"visualdev:{pipelineId}",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                pipelineId,
                mappedFieldCount = result.MappedFieldCount,
                gapCount = result.Gaps.Count,
                schemaValid = result.SchemaValid,
                fullName = result.FullName,
                enCode = result.EnCode,
            }),
        }, ct);

        return new
        {
            ok = true,
            formDataJson = result.FormDataJson,
            fullName = result.FullName,
            enCode = result.EnCode,
            type = result.Type,
            webType = result.WebType,
            mappedFieldCount = result.MappedFieldCount,
            gapCount = result.Gaps.Count,
            gaps = result.Gaps,
            schemaValid = result.SchemaValid,
            // 后续步骤提示
            nextStep = $"POST /api/visualdev/Base with formData + fullName + enCode（{result.Gaps.Count} 个缺口已事件化）",
        };
    }
}

public class VisualDevMapInput
{
    /// <summary>项目 ID（可选，默认 = pipelineId greenfield 自锚定）</summary>
    public string? ProjectId { get; set; }
}
