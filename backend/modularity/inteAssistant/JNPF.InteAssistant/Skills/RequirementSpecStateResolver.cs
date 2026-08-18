using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Ir;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 从 IR 事件序 + 02 交付物组装 <see cref="RequirementSpecSnapshot"/>（P4 阶段 1：纯读，无投影依赖）。
/// </summary>
public sealed class RequirementSpecStateResolver : IRequirementSpecStateResolver, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IIrEventStoreService _eventStore;
    private readonly IRequirementSpecMarkdownReader _markdownReader;
    private readonly IPipelineS2ProgressStore? _progressStore;
    private readonly ILogger<RequirementSpecStateResolver> _logger;

    public RequirementSpecStateResolver(
        IIrEventStoreService eventStore,
        IRequirementSpecMarkdownReader markdownReader,
        ILogger<RequirementSpecStateResolver> logger,
        IPipelineS2ProgressStore? progressStore = null)
    {
        _eventStore = eventStore;
        _markdownReader = markdownReader;
        _progressStore = progressStore;
        _logger = logger;
    }

    public Task<RequirementSpecSnapshot> ResolveAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct = default)
        => ResolveAsync(tenantId, projectId, pipelineId, includeFormalMarkdown: false, ct);

    public async Task<RequirementSpecSnapshot> ResolveAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        bool includeFormalMarkdown,
        CancellationToken ct = default)
    {
        var pipelineKey = pipelineId.ToString();
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineKey, ct);
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineKey, ct);

        var progressRow = _progressStore != null
            ? await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct)
            : null;

        var inferredPhase = InferPhase(events, ResolveWorkingText(snapshots, events, pipelineId));
        var version = progressRow?.SpecVersion > 0
            ? progressRow.SpecVersion
            : InferSpecVersion(events);
        var workingText = ResolveWorkingText(snapshots, events, pipelineId);

        RequirementSpecPhase phase;
        S2PipelineStage? pipelineStage = null;
        var clarRound = 0;
        var awaitingUser = false;
        var hasProgressRow = progressRow != null;

        if (progressRow != null)
        {
            phase = (RequirementSpecPhase)progressRow.SpecPhase;
            pipelineStage = (S2PipelineStage)progressRow.PipelineStage;
            clarRound = progressRow.ClarRound;
            awaitingUser = progressRow.AwaitingUser;

            if (phase != inferredPhase)
            {
                _logger.LogWarning(
                    "SpecResolver drift pipeline={PipelineId} rowPhase={RowPhase} inferredPhase={Inferred} stage={Stage}",
                    pipelineId, phase, inferredPhase, pipelineStage);
            }
        }
        else
        {
            phase = inferredPhase;
        }

        string? formalMarkdown = null;
        string? contentHash = progressRow?.ContentHash;
        int? contentLength = progressRow?.ContentLength;
        string? blockReason = null;

        if (phase >= RequirementSpecPhase.Rendered)
        {
            var (exists, markdown, hash, length) =
                await _markdownReader.TryReadFormalAsync(tenantId, projectId, pipelineId, ct);

            if (exists && markdown != null)
            {
                var gate = FormalSpecGate.Validate(markdown);
                if (gate.IsValid)
                {
                    contentHash = hash ?? contentHash;
                    contentLength = length;
                    if (includeFormalMarkdown)
                        formalMarkdown = markdown;
                }
                else if (phase == RequirementSpecPhase.Rendered
                         || phase == RequirementSpecPhase.Confirmed
                         || phase == RequirementSpecPhase.PmReviewed)
                {
                    blockReason = "正式版说明书格式校验失败：" + string.Join("；", gate.Violations);
                    _logger.LogWarning(
                        "SpecResolver 02 非 formal pipeline={PipelineId} phase={Phase} violations={V}",
                        pipelineId, phase, string.Join("|", gate.Violations));
                }
            }
            else if (phase >= RequirementSpecPhase.Rendered && phase < RequirementSpecPhase.Finalized)
            {
                blockReason = "正式版说明书文件缺失，请刷新后重试";
            }
        }

        var canUserConfirm = phase == RequirementSpecPhase.Rendered
                             && contentHash != null
                             && blockReason == null;
        var canUserFeedback = phase == RequirementSpecPhase.Rendered && blockReason == null;
        var canFinalize = (phase == RequirementSpecPhase.Confirmed || phase == RequirementSpecPhase.PmReviewed)
                          && contentHash != null
                          && blockReason == null;

        return new RequirementSpecSnapshot
        {
            Phase = phase,
            PipelineStage = pipelineStage,
            ClarRound = clarRound,
            AwaitingUser = awaitingUser,
            HasProgressRow = hasProgressRow,
            Version = version,
            ContentHash = contentHash,
            ContentLength = contentLength,
            FormalMarkdown = formalMarkdown,
            WorkingText = phase is RequirementSpecPhase.Absent or RequirementSpecPhase.Refining or RequirementSpecPhase.Superseded
                ? workingText
                : null,
            CanUserConfirm = canUserConfirm,
            CanUserFeedback = canUserFeedback,
            CanFinalize = canFinalize,
            BlockReason = blockReason,
        };
    }

    internal static RequirementSpecPhase InferPhase(IReadOnlyList<IrEventDto> events, string? workingText)
    {
        if (HasFinalizedAnalysis(events))
            return RequirementSpecPhase.Finalized;

        if (HasEvent(events, IrEventTypes.RequirementSpecPmReviewed))
            return RequirementSpecPhase.PmReviewed;

        if (HasEvent(events, IrEventTypes.RequirementSpecConfirmed))
            return RequirementSpecPhase.Confirmed;

        if (IsSupersededAfterRendered(events))
            return RequirementSpecPhase.Superseded;

        if (HasEvent(events, IrEventTypes.RequirementSpecRendered))
            return RequirementSpecPhase.Rendered;

        if (!string.IsNullOrWhiteSpace(workingText)
            || HasEvent(events, IrEventTypes.RequirementRefined)
            || HasEvent(events, IrEventTypes.RequirementEnhanced))
            return RequirementSpecPhase.Refining;

        return RequirementSpecPhase.Absent;
    }

    private static bool IsSupersededAfterRendered(IReadOnlyList<IrEventDto> events)
    {
        var renderedIdx = LastEventIndex(events, IrEventTypes.RequirementSpecRendered);
        if (renderedIdx < 0) return false;
        var supersededIdx = LastEventIndex(events, IrEventTypes.RequirementSpecSuperseded);
        return supersededIdx > renderedIdx;
    }

    private static bool HasFinalizedAnalysis(IReadOnlyList<IrEventDto> events)
    {
        foreach (var evt in events)
        {
            if (!string.Equals(evt.EventType, IrEventTypes.AnalysisCompleted, StringComparison.Ordinal))
                continue;
            if (TryReadBool(evt.PayloadPreview, "finalized"))
                return true;
        }

        return false;
    }

    private static int InferSpecVersion(IReadOnlyList<IrEventDto> events)
    {
        var max = 0;
        foreach (var evt in events)
        {
            if (!IsSpecLifecycleEvent(evt.EventType))
                continue;
            var v = TryReadInt(evt.PayloadPreview, "specVersion");
            if (v.HasValue)
                max = Math.Max(max, v.Value);
        }

        if (max > 0) return max;
        return CountEvents(events, IrEventTypes.RequirementSpecRendered);
    }

    private static bool IsSpecLifecycleEvent(string? eventType) =>
        eventType is IrEventTypes.RequirementSpecRendered
            or IrEventTypes.RequirementSpecConfirmed
            or IrEventTypes.RequirementSpecPmReviewed
            or IrEventTypes.RequirementSpecSuperseded;

    private static string? ResolveWorkingText(
        IReadOnlyList<IrFragmentSnapshotDto> snapshots,
        IReadOnlyList<IrEventDto> events,
        long pipelineId)
    {
        var preferredFragmentId = RequirementSpecConstants.WorkingRequirementFragmentId(pipelineId);
        foreach (var snap in snapshots)
        {
            if (snap.FragmentType != IrFragmentTypes.Requirement) continue;
            if (!string.Equals(snap.FragmentId, preferredFragmentId, StringComparison.Ordinal)
                && snap.FragmentId?.StartsWith("requirement:", StringComparison.Ordinal) != true)
                continue;
            var text = ExtractTextFromPayload(snap.Payload);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        foreach (var eventType in new[] { IrEventTypes.RequirementRefined, IrEventTypes.RequirementEnhanced })
        {
            var payload = events.LastOrDefault(e => e.EventType == eventType)?.PayloadPreview;
            var text = ExtractTextFromPayloadJson(payload);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        return null;
    }

    private static string? ExtractTextFromPayload(object? payload)
    {
        if (payload == null) return null;
        if (payload is string s)
            return ExtractTextFromPayloadJson(s);
        try
        {
            return ExtractTextFromPayloadJson(JsonSerializer.Serialize(payload, JsonOptions));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractTextFromPayloadJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch (JsonException) { /* ignore */ }

        return null;
    }

    private static bool TryReadBool(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.True)
                return true;
        }
        catch (JsonException) { /* ignore */ }

        return false;
    }

    private static int? TryReadInt(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(property, out var el) && el.TryGetInt32(out var v))
                return v;
        }
        catch (JsonException) { /* ignore */ }

        return null;
    }

    private static bool HasEvent(IReadOnlyList<IrEventDto> events, string eventType)
        => events.Any(e => string.Equals(e.EventType, eventType, StringComparison.Ordinal));

    private static int CountEvents(IReadOnlyList<IrEventDto> events, string eventType)
        => events.Count(e => string.Equals(e.EventType, eventType, StringComparison.Ordinal));

    private static int LastEventIndex(IReadOnlyList<IrEventDto> events, string eventType)
    {
        for (var i = events.Count - 1; i >= 0; i--)
        {
            if (string.Equals(events[i].EventType, eventType, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }
}
