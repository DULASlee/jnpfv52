using System.Collections.Generic;
using System.Text.Json;

namespace JNPF.InteAssistant.Entitys.Ir.Contracts;

/// <summary>
/// IR2_SystemDesign 完整契约（阶段九 P9-S1）。
///
/// 修复缺口：
///   ⑦ SystemDesignLocked 从"锁回执"升级为携带状态机/工作流/菜单的结构化设计
///   - 工作流：审批节点 + 流转条件（编译器据此生成 FlowTemplateJsonModel）
///   - 菜单：层级结构（编译器据此生成菜单注册）
///
/// 向后兼容：保留 references + consistencyChecks（旧字段），新增 structured 内容。
/// </summary>
public sealed class SystemDesignPayload
{
    // ─── 旧字段（向后兼容）───
    public string LockedAt { get; set; } = "";
    public SystemDesignReferences? References { get; set; }
    public List<ConsistencyCheck> ConsistencyChecks { get; set; } = new();

    // ─── 新增结构化内容 ───

    /// <summary>状态机定义（每实体一个）—— 编译器据此生成工作流状态</summary>
    public List<StateMachineDefinition> StateMachines { get; set; } = new();

    /// <summary>工作流节点定义 —— 编译器据此生成 FlowTemplateJsonModel JSON 树</summary>
    public List<WorkflowNodeDefinition> WorkflowNodes { get; set; } = new();

    /// <summary>菜单层级 —— 编译器据此生成菜单注册</summary>
    public List<MenuDefinition> Menus { get; set; } = new();

    public static SystemDesignPayload Parse(string payloadJson)
    {
        var payload = new SystemDesignPayload();
        if (string.IsNullOrWhiteSpace(payloadJson)) return payload;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            payload.LockedAt = GetString(root, "lockedAt") ?? "";

            // 旧格式 references
            if (root.TryGetProperty("references", out var refEl))
            {
                payload.References = new SystemDesignReferences
                {
                    ArchitectureFragmentId = GetString(refEl, "architectureFragmentId"),
                    DdlFragmentId = GetString(refEl, "ddlFragmentId"),
                    FormPageFragmentId = GetString(refEl, "formPageFragmentId"),
                };
            }

            // 旧格式 consistencyChecks
            if (root.TryGetProperty("consistencyChecks", out var ccEl) && ccEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in ccEl.EnumerateArray())
                {
                    payload.ConsistencyChecks.Add(new ConsistencyCheck
                    {
                        Check = GetString(c, "check") ?? "",
                        Passed = ReadBool(c, "passed"),
                        WarningCount = ReadInt(c, "warningCount"),
                    });
                }
            }

            // 新增：stateMachines
            if (root.TryGetProperty("stateMachines", out var smEl) && smEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var sm in smEl.EnumerateArray())
                {
                    var machine = new StateMachineDefinition
                    {
                        Entity = GetString(sm, "entity") ?? "",
                        InitialState = GetString(sm, "initialState") ?? "Draft",
                    };
                    if (sm.TryGetProperty("states", out var sEl) && sEl.ValueKind == JsonValueKind.Array)
                        machine.States = ParseStringArray(sEl);
                    if (sm.TryGetProperty("transitions", out var tEl) && tEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in tEl.EnumerateArray())
                        {
                            machine.Transitions.Add(new StateTransitionDef
                            {
                                From = GetString(t, "from") ?? "",
                                To = GetString(t, "to") ?? "",
                                Trigger = GetString(t, "trigger") ?? "",
                                Condition = GetString(t, "condition"),
                            });
                        }
                    }
                    payload.StateMachines.Add(machine);
                }
            }

            // 新增：workflowNodes（审批链）
            if (root.TryGetProperty("workflowNodes", out var wnEl) && wnEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var n in wnEl.EnumerateArray())
                {
                    var node = new WorkflowNodeDefinition
                    {
                        NodeId = GetString(n, "nodeId") ?? "",
                        NodeType = GetString(n, "nodeType") ?? "approver", // start/approver/condition/end
                        Name = GetString(n, "name") ?? "",
                        // 审批人类型：1=发起人主管, 6=指定人
                        AssigneeType = ReadInt(n, "assigneeType") ?? 1,
                        ApproverUserIds = ParseStringArray(n.GetPropertySafe("approvers")),
                        CounterSign = ReadInt(n, "counterSign") ?? 0, // 0=或签, 1=会签, 2=依次
                        RejectType = ReadInt(n, "rejectType") ?? 1,    // 1=回到发起, 2=回到上一步
                    };
                    if (n.TryGetProperty("condition", out var condEl))
                    {
                        node.Condition = new WorkflowCondition
                        {
                            Field = GetString(condEl, "field") ?? "",
                            Operator = GetString(condEl, "operator") ?? "==",
                            Value = GetString(condEl, "value") ?? "",
                        };
                    }
                    payload.WorkflowNodes.Add(node);
                }
            }

            // 新增：menus
            if (root.TryGetProperty("menus", out var menuEl) && menuEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in menuEl.EnumerateArray())
                {
                    payload.Menus.Add(ParseMenu(m));
                }
            }
        }
        catch { /* 容错 */ }

        return payload;
    }

    private static MenuDefinition ParseMenu(JsonElement m)
    {
        var menu = new MenuDefinition
        {
            MenuName = GetString(m, "menuName") ?? GetString(m, "name") ?? "",
            Path = GetString(m, "path") ?? "",
            Icon = GetString(m, "icon") ?? "icon-ym-tree-organization",
            EntityBinding = GetString(m, "entityBinding") ?? GetString(m, "entity"),
            Sort = ReadInt(m, "sort") ?? 0,
        };
        if (m.TryGetProperty("children", out var chEl) && chEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in chEl.EnumerateArray())
                menu.Children.Add(ParseMenu(c));
        }
        return menu;
    }

    public void Validate()
    {
        // SystemDesign 可以为空（向后兼容旧 pipeline），不强制校验
    }

    internal static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }
    internal static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.True;
    }
    internal static int? ReadInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : null;
    }
    internal static List<string> ParseStringArray(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String) return new() { el.GetString() ?? "" };
        if (el.ValueKind != JsonValueKind.Array) return new();
        return el.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
    }
}

public sealed class SystemDesignReferences
{
    public string? ArchitectureFragmentId { get; set; }
    public string? DdlFragmentId { get; set; }
    public string? FormPageFragmentId { get; set; }
}

public sealed class ConsistencyCheck
{
    public string Check { get; set; } = "";
    public bool? Passed { get; set; }
    public int? WarningCount { get; set; }
}

/// <summary>状态机定义（编译器据此生成工作流状态流转）</summary>
public sealed class StateMachineDefinition
{
    public string Entity { get; set; } = "";
    public string InitialState { get; set; } = "Draft";
    public List<string> States { get; set; } = new();
    public List<StateTransitionDef> Transitions { get; set; } = new();
}

public sealed class StateTransitionDef
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string? Condition { get; set; }
}

/// <summary>工作流节点（编译器据此生成 FlowTemplateJsonModel JSON 树）</summary>
public sealed class WorkflowNodeDefinition
{
    public string NodeId { get; set; } = "";
    /// <summary>节点类型：start / approver / condition / end</summary>
    public string NodeType { get; set; } = "approver";
    public string Name { get; set; } = "";
    /// <summary>审批人来源：1=发起人主管, 6=指定人</summary>
    public int AssigneeType { get; set; } = 1;
    public List<string> ApproverUserIds { get; set; } = new();
    /// <summary>会签方式：0=或签, 1=会签, 2=依次审批</summary>
    public int CounterSign { get; set; }
    /// <summary>驳回方式：1=回到发起, 2=回到上一步</summary>
    public int RejectType { get; set; } = 1;
    /// <summary>条件节点专用</summary>
    public WorkflowCondition? Condition { get; set; }
}

public sealed class WorkflowCondition
{
    public string Field { get; set; } = "";
    public string Operator { get; set; } = "==";
    public string Value { get; set; } = "";
}

/// <summary>菜单定义（编译器据此生成菜单注册）</summary>
public sealed class MenuDefinition
{
    public string MenuName { get; set; } = "";
    public string Path { get; set; } = "";
    public string Icon { get; set; } = "icon-ym-tree-organization";
    public string? EntityBinding { get; set; }
    public int Sort { get; set; }
    public List<MenuDefinition> Children { get; set; } = new();
}

/// <summary>JsonElement 扩展（安全属性访问）</summary>
internal static class JsonElementExtensions
{
    public static JsonElement GetPropertySafe(this JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var prop) ? prop : default;
    }
}
