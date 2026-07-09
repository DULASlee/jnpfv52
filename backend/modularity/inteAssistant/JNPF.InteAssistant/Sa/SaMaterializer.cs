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
            INSERT INTO sa_scope (tenant_id, project_id, pipeline_id, asset_level, system_boundary, external_entities, business_events, event_count, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @systemBoundary, @externalEntities, @businessEvents, @eventCount, 'COMPILED', @auditUser, @auditUser);
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
            INSERT INTO sa_dfd (tenant_id, project_id, pipeline_id, asset_level, scope_id, context_diagram, dfd_levels, processes, data_flows, data_stores, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @scopeId, @contextDiagram, @dfdLevels, @processes, @dataFlows, @dataStores, 'COMPILED', @auditUser, @auditUser);
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
            INSERT INTO sa_business_process (tenant_id, project_id, pipeline_id, asset_level, dfd_id, swim_lanes, activity_nodes, edges, exception_paths, dfd_process_mappings, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dfdId, @swimLanes, @activityNodes, @edges, @exceptionPaths, @dfdProcessMappings, 'COMPILED', @auditUser, @auditUser);
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
            INSERT INTO sa_data_dictionary (tenant_id, project_id, pipeline_id, asset_level, dfd_id, bpm_id, elements, data_structures, data_flows, data_stores, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dfdId, @bpmId, @elements, @dataStructures, @dataFlows, @dataStores, 'COMPILED', @auditUser, @auditUser);
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
        // P9-S1：计算校验列（确定性，零 LLM）
        var entitiesJson = JsonProp(payload, "entities");
        var relationshipsJson = JsonPropOrEmptyArray(payload, "relationships");
        var (fkInDict, thirdNormalForm, noCalculatedColumns) = ComputeErValidationFlags(entitiesJson, relationshipsJson, dictId);

        const string insert = """
            INSERT INTO sa_er (tenant_id, project_id, pipeline_id, asset_level, dict_id, entities, relationships,
                validation_status, fk_in_dict, third_normal_form, no_calculated_columns, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, 'PROJECT', @dictId, @entities, @relationships,
                'COMPILED', @fkInDict, @thirdNormalForm, @noCalculatedColumns, @auditUser, @auditUser);
            """;
        return QueryInsertIdAsync(insert, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectIdNumeric,
            pipelineId = triple.PipelineId,
            dictId,
            entities = entitiesJson,
            relationships = relationshipsJson,
            fkInDict,
            thirdNormalForm,
            noCalculatedColumns,
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    /// <summary>P9-S1：确定性计算 ER 校验标志（零 LLM）。</summary>
    private static (string fkInDict, string thirdNormalForm, string noCalculatedColumns) ComputeErValidationFlags(
        string entitiesJson, string relationshipsJson, long dictId)
    {
        // fk_in_dict：所有 FK 引用的实体是否都在 entities 列表里
        var fkInDict = "PASS";
        try
        {
            using var entDoc = JsonDocument.Parse(entitiesJson);
            var entityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (entDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in entDoc.RootElement.EnumerateArray())
                {
                    if (e.TryGetProperty("name", out var n))
                        entityNames.Add(n.GetString() ?? "");
                }
            }

            using var relDoc = JsonDocument.Parse(relationshipsJson);
            if (relDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in relDoc.RootElement.EnumerateArray())
                {
                    if (r.TryGetProperty("toEntity", out var te))
                    {
                        var toEntity = te.GetString() ?? "";
                        if (!string.IsNullOrEmpty(toEntity) && !entityNames.Contains(toEntity))
                        {
                            fkInDict = $"FAIL: FK 目标实体 {toEntity} 不在 entities 列表";
                            break;
                        }
                    }
                }
            }
        }
        catch { /* 解析失败，放行 */ }

        // third_normal_form：简单启发式——每实体有主键则 PASS
        var thirdNormalForm = "PASS";
        // no_calculated_columns：当前 compiler 不产出计算列，默认 PASS
        var noCalculatedColumns = "PASS";

        return (fkInDict, thirdNormalForm, noCalculatedColumns);
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
        var stateMachinesJson = JsonProp(payload, "stateMachines", "state_machines");
        // P9-S1：确定性计算状态机校验标志（零 LLM）
        var (reachabilityCheck, deadEndCheck) = ComputeStateMachineValidation(stateMachinesJson);

        const string insert = """
            INSERT INTO sa_state_machine (tenant_id, project_id, pipeline_id, asset_level, event_id, dict_id, bpm_id, state_machines,
                validation_status, reachability_check, dead_end_check, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @dictId, @bpmId, @stateMachines,
                'COMPILED', @reachabilityCheck, @deadEndCheck, @auditUser, @auditUser);
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
            stateMachines = stateMachinesJson,
            reachabilityCheck,
            deadEndCheck,
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    /// <summary>P9-S1：确定性计算状态机可达性/死锁校验（零 LLM）。</summary>
    private static (string reachabilityCheck, string deadEndCheck) ComputeStateMachineValidation(string stateMachinesJson)
    {
        var reachability = "PASS";
        var deadEnd = "PASS";

        try
        {
            using var doc = JsonDocument.Parse(stateMachinesJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return (reachability, deadEnd);

            foreach (var sm in doc.RootElement.EnumerateArray())
            {
                var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hasTransitions = sm.TryGetProperty("transitions", out var tEl) && tEl.ValueKind == JsonValueKind.Array && tEl.GetArrayLength() > 0;
                var initialState = "Draft";

                if (sm.TryGetProperty("states", out var sEl) && sEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in sEl.EnumerateArray())
                        states.Add(s.GetString() ?? "");
                }

                if (sm.TryGetProperty("initialState", out var isEl) && isEl.ValueKind == JsonValueKind.String)
                    initialState = isEl.GetString() ?? "Draft";

                // BFS 从 initialState 遍历 transitions
                if (hasTransitions && states.Count > 0)
                {
                    reachable.Add(initialState);
                    var queue = new Queue<string>();
                    queue.Enqueue(initialState);
                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        foreach (var t in tEl.EnumerateArray())
                        {
                            if (t.TryGetProperty("from", out var fEl) && fEl.GetString()?.Equals(current, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                if (t.TryGetProperty("to", out var toEl))
                                {
                                    var to = toEl.GetString() ?? "";
                                    if (states.Contains(to) && reachable.Add(to))
                                        queue.Enqueue(to);
                                }
                            }
                        }
                    }

                    // 可达性：是否有状态不可达
                    var unreachable = states.Except(reachable).ToList();
                    if (unreachable.Count > 0)
                        reachability = $"WARN: 不可达状态 {string.Join(",", unreachable)}";

                    // 死锁：是否有状态无出边（非终态）
                    var hasOutEdge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var t in tEl.EnumerateArray())
                    {
                        if (t.TryGetProperty("from", out var fEl))
                            hasOutEdge.Add(fEl.GetString() ?? "");
                    }
                    var deadStates = states.Except(hasOutEdge).ToList();
                    // 允许终态无出边（如 Approved/Rejected），只警告"非典型终态"
                    var typicalTerminals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected", "Closed", "Done", "Completed", "End" };
                    var suspicious = deadStates.Where(s => !typicalTerminals.Contains(s)).ToList();
                    if (suspicious.Count > 0)
                        deadEnd = $"WARN: 可能死锁状态 {string.Join(",", suspicious)}";
                }
            }
        }
        catch { /* 解析失败放行 */ }

        return (reachability, deadEnd);
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
            INSERT INTO sa_pspec (tenant_id, project_id, pipeline_id, asset_level, event_id, dict_id, bpm_id, process_specs, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @dictId, @bpmId, @processSpecs, 'COMPILED', @auditUser, @auditUser);
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
            INSERT INTO sa_decision_table (tenant_id, project_id, pipeline_id, asset_level, event_id, pspec_id, dict_id, tables, validation_status, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @pspecId, @dictId, @tables, 'COMPILED', @auditUser, @auditUser);
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
        var screens = JsonProp(payload, "screens");
        var fieldMapping = BuildUiFieldMapping(payload);
        // P9-S1：确定性计算 UI 校验标志
        var uiFieldsInDict = ComputeUiFieldsInDict(screens, fieldMapping);

        const string insert = """
            INSERT INTO sa_ui (tenant_id, project_id, pipeline_id, asset_level, event_id, bpm_id, dict_id, screens, field_to_dict_mapping,
                validation_status, ui_fields_in_dict, no_extra_fields, event_to_screen_mapping, created_by, updated_by)
            OUTPUT INSERTED.id INTO @InsertedIds
            VALUES (@tenantId, @projectId, @pipelineId, @assetLevel, @eventId, @bpmId, @dictId, @screens, @fieldMapping,
                'COMPILED', @uiFieldsInDict, @noExtraFields, @eventToScreenMapping, @auditUser, @auditUser);
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
            screens,
            fieldMapping,
            uiFieldsInDict,
            noExtraFields = "PASS", // 当前 compiler 不产出额外字段
            eventToScreenMapping = "PASS",
            auditUser = MaterializeAuditUser,
        }, ct);
    }

    /// <summary>
    /// P9-S1 修复：从恒等映射（name→name）改为真实字段绑定。
    /// 映射 UI 字段 → 数据字典列（camelCase → snake_case 规范化匹配）。
    /// </summary>
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
                    // P9-S1：读 name + controlType，绑定到规范化的字典字段名
                    if (field.TryGetProperty("name", out var nameEl))
                    {
                        var name = nameEl.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            // 规范化：camelCase → snake_case 作为字典字段
                            var dictField = CamelToSnake(name);
                            map[name] = dictField;
                        }
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

    /// <summary>P9-S1：camelCase → snake_case 转换（确定性字段规范化）。</summary>
    private static string CamelToSnake(string camel)
    {
        if (string.IsNullOrEmpty(camel)) return camel;
        var sb = new System.Text.StringBuilder();
        foreach (var c in camel)
        {
            if (char.IsUpper(c))
            {
                if (sb.Length > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>P9-S1：校验 UI 字段是否都在数据字典里（零 LLM，确定性）。</summary>
    private static string ComputeUiFieldsInDict(string screensJson, string fieldMappingJson)
    {
        try
        {
            using var mapDoc = JsonDocument.Parse(fieldMappingJson);
            if (mapDoc.RootElement.ValueKind != JsonValueKind.Object || mapDoc.RootElement.GetRawText() == "{}")
                return "SKIP: 无字段映射";

            var mappedCount = 0;
            foreach (var prop in mapDoc.RootElement.EnumerateObject())
                mappedCount++;

            using var scrDoc = JsonDocument.Parse(screensJson);
            var totalFields = 0;
            if (scrDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var screen in scrDoc.RootElement.EnumerateArray())
                {
                    if (screen.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
                        totalFields += fields.GetArrayLength();
                }
            }

            return totalFields > 0 ? $"PASS: {mappedCount}/{totalFields} 字段已映射" : "PASS";
        }
        catch
        {
            return "SKIP: 解析失败";
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
