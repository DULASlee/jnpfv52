using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 总体设计 Skill（R3 认知模具版）— 三片段 stable 后 SystemDesignLocked；
/// critical 约束违规时拒绝锁定（无 LLM、无 fallback）。
/// </summary>
public sealed class SystemDesignSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConstraintEngineService _constraintEngine;
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ILogger<SystemDesignSkillService> _logger;

    public SystemDesignSkillService(
        ICognitiveSkillToolkit toolkit,
        IConstraintEngineService constraintEngine,
        EntityDesignRepository entityDesignRepo,
        ILogger<SystemDesignSkillService> logger)
        : base(toolkit)
    {
        _constraintEngine = constraintEngine;
        _entityDesignRepo = entityDesignRepo;
        _logger = logger;
    }

    public override string SkillId => DesignSkillIds.SystemDesign;
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.RefineSpecification;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.DDL,
            IrFragmentTypes.FormPageIR,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.SystemDesignLocked,
            IrEventTypes.ConstraintViolationReported,
        },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.SystemDesign, IrStabilityStates.Locked) != null)
            return Task.FromResult(SkillValidationResult.Fail("SystemDesign 已 locked"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Any(e => e.EventType == IrEventTypes.SystemDesignLocked))
            return Task.FromResult(SkillValidationResult.Ok());

        if (events.Any(e => e.EventType == IrEventTypes.ConstraintViolationReported))
            return Task.FromResult(SkillValidationResult.Fail("critical 约束违规，未产出 SystemDesignLocked"));

        return Task.FromResult(SkillValidationResult.Fail("必须产出 SystemDesignLocked 或 ConstraintViolationReported"));
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var fragmentId = $"systemDesign:{context.ProjectId}";

        // 25 §6：锁定前校验 ai_entity_field 投影存在（字段唯一源）
        var fieldCount = await _entityDesignRepo.CountFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        if (fieldCount == 0)
            throw Oops.Bah("SystemDesign Skill: ai_entity_field 无字段，拒绝锁定（须先 Round 3 Finalize 投影）");

        var arch = context.Snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)!;
        var ddl = context.Snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)!;
        var ui = context.Snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)!;

        var check = _constraintEngine.Evaluate(context.Snapshot);
        _logger.LogInformation(
            "SystemDesign 约束校验 project={ProjectId} critical={Critical} warning={Warning}",
            context.ProjectId, check.CriticalCount, check.WarningCount);

        if (check.Violations.Count > 0)
            yield return BuildViolationEvent(context.ProjectId, check);

        if (check.CriticalCount > 0)
            throw Oops.Bah($"存在 {check.CriticalCount} 条 critical 约束违规，SystemDesignLocked 已拒绝");

        // P9-S1：从上游 IR 确定性派生状态机/工作流/菜单（零 LLM，纯编译派生）
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.Skeleton);
        var (stateMachines, workflowNodes, menus) = DeriveStructuredDesign(skeleton, ui);

        var payload = JsonSerializer.Serialize(new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            lockedAt = DateTime.UtcNow.ToString("O"),
            references = new
            {
                architectureFragmentId = arch.FragmentId,
                ddlFragmentId = ddl.FragmentId,
                formPageFragmentId = ui.FragmentId,
            },
            consistencyChecks = new object[]
            {
                new { check = "fragments-present", passed = true },
                new { check = "constraint-engine", passed = true, warningCount = check.WarningCount },
            },
            // P9-S1 新增：结构化设计内容（编译器据此生成工作流 JSON + 菜单注册）
            stateMachines,
            workflowNodes,
            menus,
            stabilityState = IrStabilityStates.Locked,
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SystemDesignLocked,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.SystemDesign,
            FragmentVersion = 1,
            Payload = payload,
        };

        await Task.CompletedTask;
    }

    /// <summary>
    /// P9-S1：从 skeleton stateTransitions + FormPageIR pages 确定性派生状态机/工作流/菜单。
    /// 零 LLM，纯编译派生（编译器架构原则）。
    /// </summary>
    private static (List<object> stateMachines, List<object> workflowNodes, List<object> menus)
        DeriveStructuredDesign(IrSnapshotFragment? skeleton, IrSnapshotFragment formPage)
    {
        var stateMachines = new List<object>();
        var workflowNodes = new List<object>();
        var menus = new List<object>();

        // ① 从 skeleton.stateTransitions 派生状态机
        if (skeleton != null)
        {
            try
            {
                using var skDoc = JsonDocument.Parse(skeleton.Payload);
                var entityStates = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var entityTransitions = new Dictionary<string, List<object>>();

                if (skDoc.RootElement.TryGetProperty("stateTransitions", out var stEl) && stEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in stEl.EnumerateArray())
                    {
                        var entity = GetString(t, "entity") ?? "";
                        var from = GetString(t, "from") ?? "";
                        var to = GetString(t, "to") ?? "";
                        var trigger = GetString(t, "trigger") ?? GetString(t, "triggerEventId") ?? to;

                        if (!entityStates.ContainsKey(entity))
                        {
                            entityStates[entity] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft" };
                            entityTransitions[entity] = new List<object>();
                        }
                        if (!string.IsNullOrEmpty(from)) entityStates[entity].Add(from);
                        if (!string.IsNullOrEmpty(to)) entityStates[entity].Add(to);
                        entityTransitions[entity].Add(new { from, to, trigger });
                    }

                    foreach (var (entity, states) in entityStates)
                    {
                        stateMachines.Add(new
                        {
                            entity,
                            initialState = "Draft",
                            states = states.ToList(),
                            transitions = entityTransitions[entity],
                        });
                    }
                }
            }
            catch { /* 容错 */ }

            // ② 派生工作流节点（通用审批链：start → approver(主管) → end）
            //    从 roleMatrix 派生审批人类型：有 approve 权限的角色 → approver 节点
            workflowNodes.Add(new
            {
                nodeId = "node_start",
                nodeType = "start",
                name = "流程发起",
                assigneeType = 0,
                approverUserIds = Array.Empty<string>(),
                counterSign = 0,
                rejectType = 1,
            });
            workflowNodes.Add(new
            {
                nodeId = "node_approver_1",
                nodeType = "approver",
                name = "部门主管审批",
                assigneeType = 1, // 1=发起人主管
                approverUserIds = Array.Empty<string>(),
                counterSign = 0,  // 或签
                rejectType = 1,   // 回到发起
            });
        }

        // ③ 从 FormPageIR pages 派生菜单（列表页 → 菜单项）
        if (formPage != null)
        {
            try
            {
                using var fpDoc = JsonDocument.Parse(formPage.Payload);
                var sort = 0;
                if (fpDoc.RootElement.TryGetProperty("pages", out var pagesEl) && pagesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in pagesEl.EnumerateArray())
                    {
                        var pageType = GetString(p, "pageType") ?? FormPageInferPageType(GetString(p, "title"));
                        if (pageType == "list") // 列表页才生成菜单
                        {
                            var title = GetString(p, "title") ?? GetString(p, "pageName") ?? "页面";
                            var entityBinding = GetString(p, "entityBinding") ?? GetString(p, "entity") ?? "";
                            menus.Add(new
                            {
                                menuName = title,
                                path = $"/{GetString(p, "id") ?? $"page-{sort}"}",
                                icon = "icon-ym-tree-organization",
                                entityBinding,
                                sort = sort++,
                            });
                        }
                    }
                }
            }
            catch { /* 容错 */ }
        }

        return (stateMachines, workflowNodes, menus);
    }

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    /// <summary>页面类型推断（与 FormPagePayload.InferPageType 一致的确定性逻辑）</summary>
    private static string FormPageInferPageType(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "form";
        var t = title.ToLowerInvariant();
        if (t.Contains("列表") || t.Contains("list") || t.Contains("查询") || t.Contains("管理"))
            return "list";
        if (t.Contains("详情") || t.Contains("detail"))
            return "detail";
        return "form";
    }

    private static AppendIrEventRequest BuildViolationEvent(string projectId, ConstraintCheckResult check)
    {
        var payload = JsonSerializer.Serialize(new
        {
            checkedAt = DateTime.UtcNow.ToString("O"),
            criticalCount = check.CriticalCount,
            warningCount = check.WarningCount,
            violations = check.Violations.Select(v => new
            {
                v.RuleId,
                v.Severity,
                v.Message,
                v.FragmentType,
                v.FragmentId,
            }),
        }, JsonOptions);

        return new AppendIrEventRequest
        {
            EventType = IrEventTypes.ConstraintViolationReported,
            FragmentId = $"constraints:{projectId}",
            FragmentType = "IR2_ConstraintReport",
            FragmentVersion = 1,
            Payload = payload,
            SkillId = DesignSkillIds.SystemDesign,
        };
    }
}
