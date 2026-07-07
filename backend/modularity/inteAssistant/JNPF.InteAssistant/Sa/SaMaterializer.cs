using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Runtime;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Sa;

public interface ISaMaterializer
{
    Task<SaMaterializeResult> MaterializeAsync(
        PipelineTriple triple,
        SaNineViewCompileResult bundle,
        CancellationToken ct = default);
}

public sealed class SaMaterializeResult
{
    public int ScopeId { get; init; }
    public int DictId { get; init; }
    public int EventCount { get; init; }
    public int DurationMs { get; init; }
}

/// <summary>
/// 将 SaNineViewCompiler bundle 物化到 sa_* 九表（SqlSugar 直连 JNPF 主库，三元组落库）。
/// sa-service 仅负责 LLM 九步编排，不得写业务库。
/// </summary>
public sealed class SaMaterializer : ISaMaterializer, ITransient
{
    private const string MaterializeAuditUser = "jnpf-materialize";

    private static readonly (string Table, string Trigger)[] SaVersionTriggers =
    [
        ("sa_scope", "trg_sa_scope_version"),
        ("sa_dfd", "trg_sa_dfd_version"),
        ("sa_business_process", "trg_sa_bpm_version"),
        ("sa_data_dictionary", "trg_sa_dict_version"),
        ("sa_pspec", "trg_sa_pspec_version"),
        ("sa_decision_table", "trg_sa_dt_version"),
        ("sa_er", "trg_sa_er_version"),
        ("sa_state_machine", "trg_sa_std_version"),
        ("sa_ui", "trg_sa_ui_version"),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISqlSugarClient _db;
    private readonly ILogger<SaMaterializer> _logger;

    public SaMaterializer(ISqlSugarClient db, ILogger<SaMaterializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SaMaterializeResult> MaterializeAsync(
        PipelineTriple triple,
        SaNineViewCompileResult bundle,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var steps = bundle.ProjectSteps;
        var events = bundle.EventResults;

        try
        {
            await _db.Ado.BeginTranAsync();
            await SetSaVersionTriggersAsync(enabled: false, ct);

        var scopeJson = BuildScopePayload(steps, events);
        var scopeId = await InsertScopeAsync(triple, scopeJson, ct);

        var dfdId = await InsertDfdAsync(triple, scopeId, GetStep(steps, SaStepNames.AggregateDesign, "dfd"), ct);
        var bpmId = await InsertBpmAsync(triple, dfdId, GetStep(steps, SaStepNames.EventCatalog, "bpm"), ct);
        var dictId = await InsertDictAsync(triple, dfdId, bpmId, GetStep(steps, SaStepNames.CommandQuery, "dict"), ct);

        await InsertErAsync(triple, dictId, GetStep(steps, SaStepNames.DataModel, "er"), ct);
        await InsertStateMachineAsync(
            triple, dictId, bpmId, GetStep(steps, SaStepNames.UISpec, "stateMachine"), eventId: null, assetLevel: "PROJECT", ct);

        for (var i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            var eventIndex = i + 1;
            var assetLevel = evt.Complexity == "complex" ? "PROCESS" : "EVENT";
            long? pspecId = null;

            var pspecStep = GetEventStep(evt, SaStepNames.IntegrationPoints);
            if (HasPayload(pspecStep))
                pspecId = await InsertPspecAsync(triple, dictId, bpmId, pspecStep, eventIndex, assetLevel, ct);

            var dtStep = GetEventStep(evt, SaStepNames.WorkflowSpec);
            if (HasPayload(dtStep))
            {
                pspecId ??= await InsertStubPspecAsync(triple, dictId, bpmId, evt.EventName, eventIndex, assetLevel, ct);
                await InsertDecisionTableAsync(triple, dictId, pspecId.Value, dtStep, eventIndex, assetLevel, ct);
            }

            var uiStep = GetEventStep(evt, SaStepNames.DeliveryChecklist);
            if (HasPayload(uiStep))
                await InsertUiAsync(triple, dictId, bpmId, uiStep, eventIndex, assetLevel, ct);
        }

        await SetSaVersionTriggersAsync(enabled: true, ct);
        await _db.Ado.CommitTranAsync();
        sw.Stop();
        _logger.LogInformation(
            "SA 物化完成 pipeline={PipelineId} scope={ScopeId} dict={DictId} events={Events} {Ms}ms",
            triple.PipelineId, scopeId, dictId, events.Count, sw.ElapsedMilliseconds);

        return new SaMaterializeResult
        {
            ScopeId = (int)scopeId,
            DictId = (int)dictId,
            EventCount = events.Count,
            DurationMs = (int)sw.ElapsedMilliseconds,
        };
        }
        catch
        {
            try { await SetSaVersionTriggersAsync(enabled: true, ct); } catch { /* best effort */ }
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task SetSaVersionTriggersAsync(bool enabled, CancellationToken ct)
    {
        var verb = enabled ? "ENABLE" : "DISABLE";
        foreach (var (table, trigger) in SaVersionTriggers)
            await ExecuteNonQueryAsync($"{verb} TRIGGER {trigger} ON {table}", ct);
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken ct)
    {
        var conn = _db.Ado.Connection as SqlConnection
            ?? throw new InvalidOperationException("SA 物化需要 SqlConnection 事务连接");
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = _db.Ado.Transaction as SqlTransaction;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private sealed record ScopePayload(string SystemBoundary, string ExternalEntities, string BusinessEvents, int EventCount);

    private static ScopePayload BuildScopePayload(
        IReadOnlyDictionary<string, object> steps,
        IReadOnlyList<SaEventResult> events)
    {
        var raw = GetStep(steps, SaStepNames.DomainModel, "scope");
        if (raw != null && TryReadProperty(raw, "businessEvents", out var existing) && existing.ValueKind == JsonValueKind.Array && existing.GetArrayLength() > 0)
        {
            return new ScopePayload(
                ToJson(GetPropertyOrDefault(raw, "systemBoundary", new { inScope = Array.Empty<string>(), outOfScope = Array.Empty<string>() })),
                ToJson(GetPropertyOrDefault(raw, "externalEntities", Array.Empty<object>())),
                existing.GetRawText(),
                existing.GetArrayLength());
        }

        var businessEvents = events.Select((e, i) => new
        {
            id = i + 1,
            irEventId = e.EventId,
            name = e.EventName,
            description = e.EventName,
            complexity = e.Complexity ?? "simple",
        }).ToList();

        return new ScopePayload(
            ToJson(GetPropertyOrDefault(raw, "systemBoundary", new { inScope = Array.Empty<string>(), outOfScope = Array.Empty<string>() })),
            ToJson(GetPropertyOrDefault(raw, "externalEntities", Array.Empty<object>())),
            JsonSerializer.Serialize(businessEvents, JsonOptions),
            businessEvents.Count);
    }

    private async Task<long> QueryInsertIdAsync(string insertStatement, object param, CancellationToken ct)
    {
        // 单 batch + 同一 SqlConnection：SET 与 INSERT 不拆批，满足 SCD 触发器 QUOTED_IDENTIFIER 要求
        var sql = $"""
            SET ANSI_NULLS ON;
            SET ANSI_PADDING ON;
            SET ANSI_WARNINGS ON;
            SET ARITHABORT ON;
            SET CONCAT_NULL_YIELDS_NULL ON;
            SET QUOTED_IDENTIFIER ON;
            SET NUMERIC_ROUNDABORT OFF;
            DECLARE @InsertedIds TABLE (id BIGINT);
            {insertStatement}
            SELECT TOP 1 id FROM @InsertedIds;
            """;

        var conn = _db.Ado.Connection as SqlConnection
            ?? throw new InvalidOperationException("SA 物化需要 SqlConnection 事务连接");
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = _db.Ado.Transaction as SqlTransaction;
        cmd.CommandText = sql;
        BindSqlParameters(cmd, param);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(scalar);
    }

    private static void BindSqlParameters(SqlCommand cmd, object param)
    {
        if (param is IDictionary<string, object?> dict)
        {
            foreach (var (key, value) in dict)
                cmd.Parameters.AddWithValue("@" + key, value ?? DBNull.Value);
            return;
        }

        foreach (var prop in param.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(param) ?? DBNull.Value);
    }

    private async Task<long> InsertScopeAsync(PipelineTriple triple, ScopePayload scope, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_scope (tenant_id, project_id, pipeline_id, asset_level, system_boundary, external_entities, business_events, event_count, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @systemBoundary, @externalEntities, @businessEvents, @eventCount, @auditUser, @auditUser);
            """;

        return await QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            systemBoundary = scope.SystemBoundary,
            externalEntities = scope.ExternalEntities,
            businessEvents = scope.BusinessEvents,
            eventCount = scope.EventCount,
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private async Task<long> InsertDfdAsync(PipelineTriple triple, long scopeId, object? payload, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_dfd (tenant_id, project_id, pipeline_id, asset_level, scope_id, context_diagram, dfd_levels, processes, data_flows, data_stores, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @scopeId, @contextDiagram, @dfdLevels, @processes, @dataFlows, @dataStores, @auditUser, @auditUser);
            """;
        return await QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            scopeId,
            contextDiagram = JsonProp(payload, "contextDiagram", "context_diagram"),
            dfdLevels = JsonProp(payload, "dfdLevels", "dfd_levels"),
            processes = JsonProp(payload, "processes"),
            dataFlows = JsonProp(payload, "dataFlows", "data_flows"),
            dataStores = JsonProp(payload, "dataStores", "data_stores"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private async Task<long> InsertBpmAsync(PipelineTriple triple, long dfdId, object? payload, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_business_process (tenant_id, project_id, pipeline_id, asset_level, dfd_id, swim_lanes, activity_nodes, edges, exception_paths, dfd_process_mappings, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dfdId, @swimLanes, @activityNodes, @edges, @exceptionPaths, @dfdProcessMappings, @auditUser, @auditUser);
            """;
        return await QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            dfdId,
            swimLanes = JsonProp(payload, "swimLanes", "swim_lanes"),
            activityNodes = JsonProp(payload, "activityNodes", "activity_nodes"),
            edges = JsonProp(payload, "edges"),
            exceptionPaths = JsonPropOrEmptyArray(payload, "exceptionPaths", "exception_paths"),
            dfdProcessMappings = JsonProp(payload, "dfdProcessMappings", "dfd_process_mappings"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private async Task<long> InsertDictAsync(
        PipelineTriple triple,
        long dfdId,
        long bpmId,
        object? payload,
        CancellationToken ct)
    {
        var dataStores = JsonProp(payload, "dataStores", "data_stores");
        const string insert = """
            INSERT INTO sa_data_dictionary (tenant_id, project_id, pipeline_id, asset_level, dfd_id, bpm_id, elements, data_structures, data_flows, data_stores, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dfdId, @bpmId, @elements, @dataStructures, @dataFlows, @dataStores, @auditUser, @auditUser);
            """;
        return await QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            dfdId,
            bpmId,
            elements = JsonProp(payload, "elements"),
            dataStructures = dataStores,
            dataFlows = JsonPropOrEmptyArray(payload, "dataFlows", "data_flows"),
            dataStores,
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private Task InsertErAsync(PipelineTriple triple, long dictId, object? payload, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_er (tenant_id, project_id, pipeline_id, asset_level, dict_id, entities, relationships, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dictId, @entities, @relationships, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            dictId,
            entities = JsonProp(payload, "entities"),
            relationships = JsonPropOrEmptyArray(payload, "relationships"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private Task InsertStateMachineAsync(
        PipelineTriple triple,
        long dictId,
        long bpmId,
        object? payload,
        long? eventId,
        string assetLevel,
        CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_state_machine (tenant_id, project_id, pipeline_id, asset_level, event_id, dict_id, bpm_id, state_machines, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @dictId, @bpmId, @stateMachines, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            assetLevel,
            eventId = (object?)eventId ?? DBNull.Value,
            dictId,
            bpmId,
            stateMachines = JsonProp(payload, "stateMachines", "state_machines"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private Task<long> InsertPspecAsync(
        PipelineTriple triple,
        long dictId,
        long bpmId,
        object? payload,
        long eventId,
        string assetLevel,
        CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_pspec (tenant_id, project_id, pipeline_id, asset_level, event_id, dict_id, bpm_id, process_specs, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @dictId, @bpmId, @processSpecs, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            assetLevel,
            eventId,
            dictId,
            bpmId,
            processSpecs = JsonProp(payload, "processSpecs", "process_specs"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private Task<long> InsertStubPspecAsync(
        PipelineTriple triple,
        long dictId,
        long bpmId,
        string eventName,
        long eventId,
        string assetLevel,
        CancellationToken ct)
    {
        var stub = JsonSerializer.Serialize(new
        {
            processSpecs = new[]
            {
                new
                {
                    id = $"PS-stub-{eventId}",
                    name = eventName,
                    input = new[] { eventName },
                    output = new[] { "结果" },
                    validation = "auto-stub",
                    algorithm = "stub",
                },
            },
        }, JsonOptions);
        return InsertPspecAsync(triple, dictId, bpmId, stub, eventId, assetLevel, ct);
    }

    private Task InsertDecisionTableAsync(
        PipelineTriple triple,
        long dictId,
        long pspecId,
        object? payload,
        long eventId,
        string assetLevel,
        CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_decision_table (tenant_id, project_id, pipeline_id, asset_level, event_id, pspec_id, dict_id, tables, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @pspecId, @dictId, @tables, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            assetLevel,
            eventId,
            pspecId,
            dictId,
            tables = JsonProp(payload, "tables"),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private Task InsertUiAsync(
        PipelineTriple triple,
        long dictId,
        long bpmId,
        object? payload,
        long eventId,
        string assetLevel,
        CancellationToken ct)
    {
        const string insert = """
            INSERT INTO sa_ui (tenant_id, project_id, pipeline_id, asset_level, event_id, bpm_id, dict_id, screens, field_to_dict_mapping, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @bpmId, @dictId, @screens, @fieldMapping, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            assetLevel,
            eventId,
            bpmId,
            dictId,
            screens = JsonProp(payload, "screens"),
            fieldMapping = BuildUiFieldMapping(payload),
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    private static string BuildUiFieldMapping(object? payload)
    {
        var screens = JsonProp(payload, "screens");
        try
        {
            using var doc = JsonDocument.Parse(screens);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return "{}";
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var screen in doc.RootElement.EnumerateArray())
            {
                if (!screen.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var field in fields.EnumerateArray())
                {
                    if (field.TryGetProperty("name", out var nameEl))
                    {
                        var name = nameEl.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                            map[name] = name;
                    }
                }
            }

            return map.Count == 0 ? "{}" : JsonSerializer.Serialize(map, JsonOptions);
        }
        catch
        {
            return "{}";
        }
    }

    private static string JsonProp(object? raw, params string[] names)
    {
        if (raw == null) return "{}";
        if (raw is string s && s.TrimStart().StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(s);
                return ExtractJsonProperty(doc.RootElement, names) ?? s;
            }
            catch
            {
                return s;
            }
        }

        if (raw is JsonElement je)
            return ExtractJsonProperty(je, names) ?? je.GetRawText();

        var json = JsonSerializer.Serialize(raw, JsonOptions);
        using (var doc = JsonDocument.Parse(json))
            return ExtractJsonProperty(doc.RootElement, names) ?? json;
    }

    private static string JsonPropOrEmptyArray(object? raw, params string[] names)
    {
        var value = JsonProp(raw, names);
        return string.IsNullOrWhiteSpace(value) || value == "{}" ? "[]" : value;
    }

    private static string? ExtractJsonProperty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop))
                return prop.GetRawText();
        }

        return null;
    }

    private static object? GetStep(IReadOnlyDictionary<string, object> steps, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (steps.TryGetValue(key, out var value))
                return value;
        }

        return null;
    }

    private static object? GetEventStep(SaEventResult evt, string stepName)
        => evt.Steps.TryGetValue(stepName, out var step) ? step : null;

    private static bool HasPayload(object? step)
    {
        if (step == null) return false;

        if (step is JsonElement je)
        {
            if (je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return false;
            if (je.ValueKind == JsonValueKind.Object && !je.EnumerateObject().Any()) return false;
            if (je.TryGetProperty("note", out var note) && note.GetString()?.Contains("无独立", StringComparison.Ordinal) == true)
                return false;
            if (je.TryGetProperty("processSpecs", out var specs) && specs.ValueKind == JsonValueKind.Array && specs.GetArrayLength() == 0)
                return false;
            if (je.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array && tables.GetArrayLength() == 0)
                return false;
            return true;
        }

        var json = JsonSerializer.Serialize(step, JsonOptions);
        if (json is "{}" or "null") return false;
        return true;
    }

    private static string ToJson(object? value)
    {
        if (value == null) return "{}";
        if (value is JsonElement je) return je.GetRawText();
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static object GetPropertyOrDefault(object raw, string name, object fallback)
    {
        if (raw is JsonElement je && je.TryGetProperty(name, out var prop))
            return prop;
        return fallback;
    }

    private static bool TryReadProperty(object raw, string name, out JsonElement value)
    {
        if (raw is JsonElement je && je.TryGetProperty(name, out value))
            return true;
        value = default;
        return false;
    }
}
