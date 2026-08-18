using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// 将预分析模型（IR-0 / 需求分析书机器层）确定性编译为 SA 九步 IR 视图。
/// 不调用 LLM；与 sa-service Agent 产出同形，供 EventSpecAssembler / 物化 Job 消费。
/// </summary>
public interface ISaNineViewCompiler
{
    SaNineViewCompileResult Compile(PreAnalysisModel model);

    SaNineViewCompileResult CompileFromSkeletonJson(
        string skeletonJson,
        string? requirementSummary = null,
        string? pipelineTitle = null);
}

public sealed class SaNineViewCompiler : ISaNineViewCompiler, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<SaNineViewCompiler> _logger;

    public SaNineViewCompiler(ILogger<SaNineViewCompiler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SaNineViewCompileResult CompileFromSkeletonJson(
        string skeletonJson,
        string? requirementSummary = null,
        string? pipelineTitle = null)
    {
        var model = PreAnalysisModel.ParseFromSkeletonJson(skeletonJson, requirementSummary)
            .ResolveIdentity(pipelineTitle, requirementSummary);
        return Compile(model);
    }

    public SaNineViewCompileResult Compile(PreAnalysisModel model)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("SA九步编译开始 events={EventCount} entities={EntityCount}",
            model.BusinessEvents.Count, model.EntityDrafts.Count);

        if (model.BusinessEvents.Count == 0)
        {
            _logger.LogWarning("编译失败: PreAnalysisModel 无 businessEvents");
            throw Oops.Bah("PreAnalysisModel 无 businessEvents，无法编译九步视图");
        }

        var scope = CompileDomainModel(model);
        var dfd = CompileAggregateDesign(model);
        var bpm = CompileEventCatalog(model);
        var dict = CompileCommandQuery(model);
        var er = CompileDataModel(model);
        var std = CompileUiSpec(model);

        var projectSteps = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [SaStepNames.DomainModel] = scope,
            [SaStepNames.AggregateDesign] = dfd,
            [SaStepNames.EventCatalog] = bpm,
            [SaStepNames.CommandQuery] = dict,
            [SaStepNames.DataModel] = er,
            [SaStepNames.UISpec] = std,
        };

        var eventResults = model.BusinessEvents.Select(evt =>
        {
            var steps = new Dictionary<string, object>(projectSteps, StringComparer.Ordinal)
            {
                [SaStepNames.DeliveryChecklist] = CompileDeliveryChecklist(model, evt),
            };

            if (IsComplex(evt.ComplexityHint))
            {
                _logger.LogDebug("事件 {EventId}({EventName}) 标记为 complex，生成 IntegrationPoints + WorkflowSpec",
                    evt.EventId, evt.EventName);
                steps[SaStepNames.IntegrationPoints] = CompileIntegrationPoints(model, evt);
                steps[SaStepNames.WorkflowSpec] = CompileWorkflowSpec(model, evt);
            }
            else
            {
                steps[SaStepNames.IntegrationPoints] = EmptyProcessSpec(evt);
                steps[SaStepNames.WorkflowSpec] = EmptyDecisionTable(evt);
            }

            return new SaEventResult
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                Complexity = evt.ComplexityHint,
                Steps = steps,
            };
        }).ToList();

        // ── 收集假设项（C# 编译器的确定性推导）─────────────────────────
        var assumptions = CollectAssumptions(model);

        sw.Stop();
        var hash = ComputeBundleHash(projectSteps, eventResults);

        _logger.LogInformation("SA九步编译完成 耗时={Elapsed}ms hash={Hash} assumptions={AssumptionCount}",
            sw.ElapsedMilliseconds, hash, assumptions.Count);

        return new SaNineViewCompileResult
        {
            Source = model,
            ProjectSteps = projectSteps,
            EventResults = eventResults,
            CompileDurationMs = (int)sw.ElapsedMilliseconds,
            BundleHash = hash,
            Assumptions = assumptions,
        };
    }

    private static object CompileDomainModel(PreAnalysisModel model)
    {
        var inScope = model.BusinessEvents.Select(e => e.EventName).Distinct().ToList();
        if (inScope.Count == 0 && !string.IsNullOrWhiteSpace(model.RequirementSummary))
            inScope.Add(model.RequirementSummary.Length > 80
                ? model.RequirementSummary[..80]
                : model.RequirementSummary);

        return new
        {
            systemBoundary = new
            {
                inScope,
                outOfScope = Array.Empty<string>(),
            },
            externalEntities = InferExternalEntities(model),
            businessEvents = model.BusinessEvents.Select(e => new
            {
                id = e.Index,
                irEventId = e.EventId,
                name = e.EventName,
                description = e.Description ?? e.EventName,
                complexity = e.ComplexityHint,
            }).ToList(),
            eventCount = model.BusinessEvents.Count,
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileAggregateDesign(PreAnalysisModel model)
    {
        var processes = model.BusinessEvents.Select((e, i) => new
        {
            id = $"P{i + 1}",
            name = e.EventName,
            inputFlows = new[] { $"{e.EventName}请求" },
            outputFlows = new[] { $"{e.EventName}结果" },
            parentId = "P0",
        }).ToList();

        var dataStores = model.EntityDrafts.Select(d => new
        {
            name = ToSnakeUpper(d.TableName ?? d.EntityName),
        }).ToList();

        if (dataStores.Count == 0)
            dataStores.Add(new { name = "BUSINESS_DATA" });

        var dataFlows = processes
            .SelectMany(p => p.inputFlows.Concat(p.outputFlows))
            .Distinct()
            .Select(n => new { name = n })
            .ToList();

        return new
        {
            contextDiagram = new
            {
                processName = model.SystemName ?? SaCompilerDefaults.DefaultSystemName,
                inboundFlows = processes.Select(p => new { from = "用户", dataName = p.inputFlows[0] }).ToList(),
                outboundFlows = processes.Select(p => new { to = "用户", dataName = p.outputFlows[0] }).ToList(),
            },
            dfdLevels = new
            {
                level0 = new[] { new { id = "P0", name = model.SystemName ?? SaCompilerDefaults.DefaultSystemNameShort } },
                level1 = new Dictionary<string, object>
                {
                    ["P0"] = processes.Select(p => new { id = p.id, name = p.name }).ToList(),
                },
            },
            processes,
            dataFlows,
            dataStores,
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileEventCatalog(PreAnalysisModel model)
    {
        var nodes = model.BusinessEvents.Select((e, i) => new
        {
            id = $"A{i + 1}",
            name = e.EventName,
            lane = "业务",
            eventId = e.EventId,
        }).ToList();

        var edges = new List<object>();
        foreach (var e in model.BusinessEvents)
        {
            foreach (var dep in e.DependsOn)
            {
                var upstream = model.BusinessEvents.FirstOrDefault(x => x.EventId == dep);
                if (upstream == null) continue;
                edges.Add(new
                {
                    from = $"A{upstream.Index}",
                    to = $"A{e.Index}",
                    label = "dependsOn",
                });
            }
        }

        return new
        {
            swimLanes = new[] { new { id = "L1", name = "业务" } },
            activityNodes = nodes,
            edges,
            exceptionPaths = Array.Empty<object>(),
            dfdProcessMappings = nodes.ToDictionary(
                n => n.id,
                n => $"P{n.id.TrimStart('A')}",
                StringComparer.Ordinal),
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileCommandQuery(PreAnalysisModel model)
    {
        var elements = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entityNames = model.EntityDrafts.Select(e => e.EntityName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in model.EntityDrafts)
        {
            foreach (var field in entity.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Name) || !seen.Add(field.Name))
                    continue;

                // Fix-6: FK 推导统一走 EntityRelationInferenceService
                var isFK = EntityRelationInferenceService.IsForeignKey(field);
                var refEntity = EntityRelationInferenceService.ResolveRefEntity(field, entityNames, model.EntityDrafts);

                elements.Add(new
                {
                    name = ToSnakeLower(field.Name),
                    type = MapSqlType(field.Type),
                    isRequired = field.Required || field.IsPrimaryKey,
                    isFK,
                    refEntity,
                });
            }
        }

        if (elements.Count == 0)
        {
            elements.Add(new
            {
                name = "id",
                type = "BIGINT",
                isRequired = true,
                isFK = false,
                refEntity = (string?)null,
            });
        }

        return new
        {
            elements,
            dataFlows = Array.Empty<object>(),
            dataStores = model.EntityDrafts.Select(d => new
            {
                name = ToSnakeUpper(d.TableName ?? d.EntityName),
                fields = d.Fields.Select(f => new { name = ToSnakeLower(f.Name), type = MapSqlType(f.Type) }).ToList(),
            }).ToList(),
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileDataModel(PreAnalysisModel model)
    {
        if (model.EntityDrafts.Count == 0)
        {
            return new
            {
                entities = new[]
                {
                    new
                    {
                        name = "BusinessEntity",
                        tableName = "BUSINESS_ENTITY",
                        columns = new[]
                        {
                            new { name = "id", type = "BIGINT", dataType = "BIGINT", isPK = true, isFK = false, refTable = (string?)null },
                        },
                    },
                },
                relationships = Array.Empty<object>(),
                source = "SaNineViewCompiler",
            };
        }

        // P9-S1：构建实体名索引（用于 FK refTable 解析）
        var entityNames = model.EntityDrafts.Select(e => e.EntityName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var entities = model.EntityDrafts.Select(d => new
        {
            name = d.EntityName,
            tableName = ToSnakeUpper(d.TableName ?? d.EntityName),
                columns = d.Fields.Select(f => new
                {
                    name = ToSnakeLower(f.Name),
                    type = MapSqlType(f.Type),
                    dataType = MapSqlType(f.Type),
                    isPK = f.IsPrimaryKey,
                    // Fix-6: FK 推导统一走 EntityRelationInferenceService
                    isFK = EntityRelationInferenceService.IsForeignKey(f),
                    refTable = EntityRelationInferenceService.ResolveRefEntity(f, entityNames, model.EntityDrafts),
                }).ToList(),
        }).ToList();

        // P9-S1 核心修复：从 EntityDraft.Relations 派生真实关系（不再写死 relationships=[]）
        var relationships = new List<object>();
        foreach (var entity in model.EntityDrafts)
        {
            foreach (var rel in entity.Relations)
            {
                relationships.Add(new
                {
                    fromEntity = entity.EntityName,
                    fromField = ToSnakeLower(rel.FromField),
                    toEntity = rel.ToEntity,
                    toField = ToSnakeLower(rel.ToField),
                    type = rel.RelationType,
                });
            }
        }

        // 兜底：若 Relations 为空，从字段 FK 推导派生（统一走 EntityRelationInferenceService）
        if (relationships.Count == 0)
        {
            foreach (var entity in model.EntityDrafts)
            {
                foreach (var field in entity.Fields)
                {
                    if (EntityRelationInferenceService.IsForeignKey(field))
                    {
                        var guessed = EntityRelationInferenceService.GuessRefEntity(field.Name, model.EntityDrafts);
                        if (!string.IsNullOrEmpty(guessed))
                        {
                            var targetEntity = model.EntityDrafts.FirstOrDefault(e =>
                                string.Equals(e.EntityName, guessed, StringComparison.OrdinalIgnoreCase));
                            relationships.Add(new
                            {
                                fromEntity = entity.EntityName,
                                fromField = ToSnakeLower(field.Name),
                                toEntity = guessed,
                                toField = EntityRelationInferenceService.ResolveToField(field.References, targetEntity),
                                type = "many-to-one",
                            });
                        }
                    }
                }
            }
        }

        return new
        {
            entities,
            relationships,  // P9-S1：不再 Array.Empty
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileUiSpec(PreAnalysisModel model)
    {
        var stateMachines = new List<object>();

        foreach (var entity in model.EntityDrafts)
        {
            var related = model.StateTransitions
                .Where(t => string.Equals(t.Entity, entity.EntityName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft" };
            foreach (var t in related)
            {
                if (!string.IsNullOrWhiteSpace(t.From)) states.Add(t.From);
                if (!string.IsNullOrWhiteSpace(t.To)) states.Add(t.To);
            }

            // P9-S1 修复：只有当 StateTransitions 为空（IR 未声明状态转换）且有 status 字段时，
            // 才用通用审批语义默认（Draft→Submitted→Approved/Rejected），非硬编码请假专用。
            if (related.Count == 0 && entity.Fields.Any(f => f.Name.Contains("status", StringComparison.OrdinalIgnoreCase)))
            {
                states.Add("Submitted");
                states.Add("Approved");
                states.Add("Rejected");
            }

            stateMachines.Add(new
            {
                entity = entity.EntityName,
                states = states.ToList(),
                transitions = related.Select(t => new
                {
                    from = t.From,
                    to = t.To,
                    trigger = t.TriggerEventId ?? t.To,
                }).ToList(),
            });
        }

        if (stateMachines.Count == 0)
        {
            stateMachines.Add(new
            {
                entity = "BusinessEntity",
                states = new[] { "Draft", "Active", "Closed" },
                transitions = Array.Empty<object>(),
            });
        }

        return new { stateMachines, source = "SaNineViewCompiler" };
    }

    private static object CompileIntegrationPoints(PreAnalysisModel model, PreAnalysisBusinessEvent evt)
    {
        var rules = RulesForEvent(model, evt.EventId);
        var specs = rules.Select((r, i) => new
        {
            id = $"PS-{evt.EventId}-{i + 1}",
            name = r.Description.Length > 64 ? r.Description[..64] : r.Description,
            input = new[] { evt.EventName },
            output = new[] { "处理结果" },
            validation = r.Description,
            algorithm = $"按规则 {r.RuleId} 执行",
        }).ToList();

        if (specs.Count == 0)
        {
            specs.Add(new
            {
                id = $"PS-{evt.EventId}",
                name = evt.EventName,
                input = new[] { evt.EventName },
                output = new[] { "处理结果" },
                validation = evt.Description ?? evt.EventName,
                algorithm = "标准业务处理",
            });
        }

        return new { processSpecs = specs, source = "SaNineViewCompiler" };
    }

    private static object CompileWorkflowSpec(PreAnalysisModel model, PreAnalysisBusinessEvent evt)
    {
        var rules = RulesForEvent(model, evt.EventId);
        var conditions = rules.Select((r, i) => new
        {
            name = $"cond_{i + 1}",
            @operator = "eq",
            value = r.Description,
        }).ToList();

        if (conditions.Count == 0)
        {
            conditions.Add(new { name = "default", @operator = "always", value = "true" });
        }

        // P9-S1 修复：actionIndex 不再用无意义的 i%2。
        // 语义：所有条件满足 → actionIndex=0（执行）；有任一不满足 → actionIndex=1（拒绝/转人工）
        // 默认规则：第一个规则=执行（条件全 true），第二个规则=拒绝（兜底）
        var ruleList = new List<object>
        {
            new
            {
                conditionMask = Enumerable.Repeat(true, conditions.Count).ToArray(), // 所有条件满足
                actionIndex = 0, // → 执行
            },
            new
            {
                conditionMask = Enumerable.Repeat(false, conditions.Count)
                    .Select((v, i) => i == 0 ? false : true).ToArray(), // 任一条件不满足
                actionIndex = 1, // → 拒绝
            },
        };

        return new
        {
            tables = new[]
            {
                new
                {
                    id = $"DT-{evt.EventId}",
                    conditions,
                    actions = new[] { new { name = "执行" }, new { name = "拒绝" } },
                    rules = ruleList,
                    hasDefaultRule = true, // P9-S1：标记有默认规则
                },
            },
            source = "SaNineViewCompiler",
        };
    }

    private static object CompileDeliveryChecklist(PreAnalysisModel model, PreAnalysisBusinessEvent evt)
    {
        var dict = CompileCommandQuery(model);
        var dictJson = JsonSerializer.Serialize(dict, JsonOptions);
        using var doc = JsonDocument.Parse(dictJson);
        var elements = doc.RootElement.TryGetProperty("elements", out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().ToList()
            : new List<JsonElement>();

        var fields = elements.Take(12).Select((e, i) =>
        {
            var name = e.TryGetProperty("name", out var n) ? n.GetString() ?? $"field_{i}" : $"field_{i}";
            var type = e.TryGetProperty("type", out var t) ? t.GetString() ?? "NVARCHAR(255)" : "NVARCHAR(255)";
            var required = e.TryGetProperty("isRequired", out var r) && r.ValueKind == JsonValueKind.True;
            return new
            {
                name,
                type,
                required,
                controlType = MapControlType(type, name),
            };
        }).ToList();

        if (fields.Count == 0)
        {
            fields.Add(new { name = "id", type = "BIGINT", required = true, controlType = "NumberInput" });
        }

        return new
        {
            screens = new[]
            {
                new
                {
                    id = "1",
                    name = $"{evt.EventName}表单",
                    dataFlow = $"{evt.EventName}数据",
                    bpmNodeId = $"A{evt.Index}",
                    fields,
                },
            },
            source = "SaNineViewCompiler",
        };
    }

    private static object EmptyProcessSpec(PreAnalysisBusinessEvent evt) => new
    {
        processSpecs = Array.Empty<object>(),
        note = $"simple/medium 事件 {evt.EventId} 无独立 PSpec",
        source = "SaNineViewCompiler",
    };

    private static object EmptyDecisionTable(PreAnalysisBusinessEvent evt) => new
    {
        tables = Array.Empty<object>(),
        note = $"simple/medium 事件 {evt.EventId} 无独立判定表",
        source = "SaNineViewCompiler",
    };

    private static IReadOnlyList<PreAnalysisBusinessRule> RulesForEvent(PreAnalysisModel model, string eventId)
    {
        var scoped = model.BusinessRules
            .Where(r => string.IsNullOrWhiteSpace(r.ScopeEventId)
                || string.Equals(r.ScopeEventId, eventId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (scoped.Count > 0)
            return scoped;

        var evt = model.BusinessEvents.FirstOrDefault(e => e.EventId == eventId);
        if (evt == null)
            return Array.Empty<PreAnalysisBusinessRule>();

        return new[]
        {
            new PreAnalysisBusinessRule
            {
                RuleId = $"R-{eventId}",
                ScopeEventId = eventId,
                Description = evt.Description ?? evt.EventName,
            },
        };
    }

    private static IReadOnlyList<object> InferExternalEntities(PreAnalysisModel model)
    {
        var list = new List<object> { new { name = "用户", type = "user", description = "系统使用者" } };
        if (model.RequirementSummary?.Contains("AD", StringComparison.OrdinalIgnoreCase) == true
            || model.BusinessEvents.Any(e => e.EventName.Contains("AD", StringComparison.OrdinalIgnoreCase)))
        {
            list.Add(new { name = "AD目录", type = "system", description = "企业账号认证" });
        }

        return list;
    }

    private static string MapSqlType(string draftType)
    {
        var lower = draftType.ToLowerInvariant();
        return SaCompilerDefaults.SqlTypeMap.TryGetValue(lower, out var sqlType)
            ? sqlType
            : draftType.Contains('(') ? draftType : SaCompilerDefaults.DefaultSqlType;
    }

    private static string MapControlType(string sqlType, string fieldName)
    {
        if (fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
            || fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            return "Select";

        return sqlType.ToUpperInvariant() switch
        {
            var t when t.StartsWith("NVARCHAR(MAX)", StringComparison.Ordinal) => "Textarea",
            var t when t.StartsWith("NVARCHAR", StringComparison.Ordinal) => "Input",
            "DATETIME" => "DatePicker",
            "BIT" => "Switch",
            "INT" or "BIGINT" or "DECIMAL(18,2)" => "NumberInput",
            _ => "Input",
        };
    }

    private static string ToSnakeLower(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private static string ToSnakeUpper(string name) => ToSnakeLower(name).ToUpperInvariant();

    private static bool IsComplex(string complexity) =>
        string.Equals(complexity, "complex", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 收集编译器确定性推导产生的假设项（安全网 2：标记从 Round 1 开始）。
    /// 当前收集：FK 命名推导、外部实体推断。后续可扩展更多推导类型。
    /// </summary>
    private List<Assumption> CollectAssumptions(PreAnalysisModel model)
    {
        var list = new List<Assumption>();

        // 1. FK 命名推导：字段以 Id 结尾、非主键、无显式 References 声明 → 编译器猜测外键
        foreach (var entity in model.EntityDrafts)
        {
            foreach (var field in entity.Fields)
            {
                if (!field.IsPrimaryKey
                    && field.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrEmpty(field.References))
                {
                    var guessed = EntityRelationInferenceService.GuessRefEntity(field.Name, model.EntityDrafts);
                    var refText = !string.IsNullOrEmpty(guessed)
                        ? $"推断 {entity.EntityName}.{field.Name} → {guessed}.id（命名以 Id 结尾，无显式 References）"
                        : $"字段 {entity.EntityName}.{field.Name} 推断为外键（命名以 Id 结尾但无法确定引用实体）";
                    list.Add(new Assumption(
                        entity.EntityName,
                        "DataModel",
                        refText,
                        SaCompilerDefaults.DefaultForeignKeyConfidence));
                    _logger.LogDebug("假设项 FK推导: {Text}", refText);
                }
            }
        }

        // 2. 外部实体推断：根据需求关键词推断外部系统
        if (model.RequirementSummary?.Contains("AD", StringComparison.OrdinalIgnoreCase) == true
            || model.BusinessEvents.Any(e => e.EventName.Contains("AD", StringComparison.OrdinalIgnoreCase)))
        {
            const string adText = "推断外部实体'AD目录'（检测到 AD 关键词）";
            list.Add(new Assumption("ALL", "DomainModel", adText, SaCompilerDefaults.DefaultExternalEntityConfidence));
            _logger.LogDebug("假设项 外部实体: {Text}", adText);
        }

        // 3. 实体间关系推导：Relations 为空时，从 EndsWith("Id") 字段猜测关系
        foreach (var entity in model.EntityDrafts)
        {
            if (entity.Relations.Count == 0)
            {
                foreach (var field in entity.Fields)
                {
                    if (!field.IsPrimaryKey
                        && field.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        var guessed = EntityRelationInferenceService.GuessRefEntity(field.Name, model.EntityDrafts);
                        if (!string.IsNullOrEmpty(guessed))
                        {
                            var relText = $"推断关系 {entity.EntityName} → {guessed}（从字段命名推导，实体无显式 Relations 声明）";
                            list.Add(new Assumption(entity.EntityName, "DataModel", relText, 0.5m));
                            _logger.LogDebug("假设项 关系推导: {Text}", relText);
                        }
                    }
                }
            }
        }

        return list;
    }

    private static string ComputeBundleHash(
        IReadOnlyDictionary<string, object> projectSteps,
        IReadOnlyList<SaEventResult> events)
    {
        var json = JsonSerializer.Serialize(new { projectSteps, events }, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }
}

/// <summary>与 SaStepMapping.IrStepOrder 一致的常量。</summary>
public static class SaStepNames
{
    public const string DomainModel = "DomainModel";
    public const string AggregateDesign = "AggregateDesign";
    public const string EventCatalog = "EventCatalog";
    public const string CommandQuery = "CommandQuery";
    public const string IntegrationPoints = "IntegrationPoints";
    public const string WorkflowSpec = "WorkflowSpec";
    public const string UISpec = "UISpec";
    public const string DataModel = "DataModel";
    public const string DeliveryChecklist = "DeliveryChecklist";

    public static readonly string[] All =
    {
        DomainModel, AggregateDesign, EventCatalog, CommandQuery,
        IntegrationPoints, WorkflowSpec, UISpec, DataModel, DeliveryChecklist,
    };
}
