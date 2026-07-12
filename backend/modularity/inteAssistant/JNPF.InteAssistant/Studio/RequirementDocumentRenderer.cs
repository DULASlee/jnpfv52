using System.Linq;
using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 28 号 §4：需求分析规格说明书渲染器。
/// 纯确定性 C# 产出，无 LLM 调用，&lt;2 秒。
/// 输入：PreAnalysisModel + DddProjectionResult + ConsistencyFinding + QualityScore → Markdown 文档。
/// </summary>
public interface IRequirementDocumentRenderer
{
    /// <summary>渲染完整的需求分析规格说明书。</summary>
    /// <param name="triple">三元组</param>
    /// <param name="compileResult">SA 编译器输出（含 PreAnalysisModel）</param>
    /// <param name="dddProjection">DDD 五视角投影</param>
    /// <param name="entityFields">实体字段投影</param>
    /// <param name="consistencyFindings">一致性检查发现列表</param>
    /// <param name="qualityScore">质量评分</param>
    /// <param name="roundNumber">需求分析轮次（1/2/3）</param>
    /// <param name="clarificationAnswers">三轮澄清作答附录（有则写，无则空节）</param>
    /// <param name="ct">取消令牌</param>
    string Render(
        PipelineTriple triple,
        SaNineViewCompileResult compileResult,
        DddProjectionResult dddProjection,
        EntityDesignProjection entityFields,
        IReadOnlyList<ConsistencyFinding> consistencyFindings,
        QualityScore qualityScore,
        int roundNumber,
        IReadOnlyList<ClarificationAnswerAppendix>? clarificationAnswers = null,
        CancellationToken ct = default);
}

/// <summary>02 附录：用户澄清作答摘要。</summary>
public sealed record ClarificationAnswerAppendix(string Stage, int Round, string AnswersText);

public sealed class RequirementDocumentRenderer : IRequirementDocumentRenderer, ITransient
{
    private readonly ILogger<RequirementDocumentRenderer> _logger;

    public RequirementDocumentRenderer(ILogger<RequirementDocumentRenderer> logger)
    {
        _logger = logger;
    }

    public string Render(
        PipelineTriple triple,
        SaNineViewCompileResult compileResult,
        DddProjectionResult dddProjection,
        EntityDesignProjection entityFields,
        IReadOnlyList<ConsistencyFinding> consistencyFindings,
        QualityScore qualityScore,
        int roundNumber,
        IReadOnlyList<ClarificationAnswerAppendix>? clarificationAnswers = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var sb = new StringBuilder();
        // 企业可用：表头禁止「—」；身份在渲染前强制补全
        var model = compileResult.Source.ResolveIdentity(
            pipelineTitle: null,
            requirementText: compileResult.Source.RequirementSummary);

        RenderCover(sb, model, triple, roundNumber, qualityScore);
        RenderPendingConfirmations(sb, dddProjection);
        RenderSection1Overview(sb, model, compileResult, entityFields, dddProjection);
        RenderSection2BusinessEvents(sb, model, compileResult);
        RenderSection3DddEnhancement(sb, dddProjection);
        RenderSection4DataModel(sb, entityFields);
        RenderSection5Consistency(sb, consistencyFindings);
        RenderSection6Quality(sb, qualityScore);
        RenderSection7OutOfScope(sb, compileResult);
        RenderSection8FailureCompensation(sb, model, compileResult);
        RenderSection9AcceptancePoints(sb, model);
        RenderAppendices(sb, model, compileResult, clarificationAnswers);
        RenderConfirmCta(sb);

        _logger.LogInformation("需求分析规格说明书渲染完成，{totalChars:N0} 字符", sb.Length);
        return sb.ToString();
    }

    // ──────────────────── 封面 ────────────────────

    private static void RenderCover(
        StringBuilder sb, PreAnalysisModel model, PipelineTriple triple, int roundNumber, QualityScore qualityScore)
    {
        sb.AppendLine("# 需求分析规格说明书");
        sb.AppendLine();
        sb.AppendLine("| 属性 | 值 |");
        sb.AppendLine("|------|-----|");
        // ResolveIdentity 已保证非空；禁止回退到「—」
        sb.AppendLine($"| 项目名称 | {Esc(model.SystemName!)} |");
        sb.AppendLine($"| 需求概要 | {Esc(model.RequirementSummary!)} |");
        sb.AppendLine($"| 租户 ID | {Esc(triple.TenantId)} |");
        sb.AppendLine($"| 项目 ID | {Esc(triple.ProjectId)} |");
        sb.AppendLine($"| Pipeline ID | {triple.PipelineId} |");
        sb.AppendLine($"| 需求分析轮次 | Round {roundNumber} |");
        sb.AppendLine($"| 质量综合评分 | {qualityScore.TotalScore:F1} |");
        sb.AppendLine($"| 生成时间 | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine($"| Schema 版本 | {Esc(model.SchemaVersion)} |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    /// <summary>DDD 低置信度待确认（SG2-T11 / SG2-E2）：必须显式列出，禁止静默 30%。</summary>
    private static void RenderPendingConfirmations(StringBuilder sb, DddProjectionResult dddProjection)
    {
        if (dddProjection.PendingConfirmations.Count == 0)
            return;

        sb.AppendLine("## ⚠ 待确认事项（DDD 低置信度）");
        sb.AppendLine();
        sb.AppendLine("> 以下项置信度 &lt; 50%，须业务确认后方可作为设计/生成依据。");
        sb.AppendLine();
        var i = 1;
        foreach (var item in dddProjection.PendingConfirmations)
        {
            sb.AppendLine($"{i}. {Esc(item)}");
            i++;
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ──────────────────── §1 系统概述 ────────────────────

    private static void RenderSection1Overview(
        StringBuilder sb, PreAnalysisModel model, SaNineViewCompileResult compileResult,
        EntityDesignProjection entityFields, DddProjectionResult dddProjection)
    {
        sb.AppendLine("## §1 系统概述");
        sb.AppendLine();

        // 1.1 基本信息
        sb.AppendLine("### 1.1 基本信息");
        sb.AppendLine();
        sb.AppendLine($"**系统名称：** {Esc(model.SystemName ?? "未指定")}");
        sb.AppendLine();
        sb.AppendLine($"**需求概要：** {Esc(model.RequirementSummary ?? "未提供")}");
        sb.AppendLine();
        sb.AppendLine("**编译元数据：**");
        sb.AppendLine($"- 编译耗时：{compileResult.CompileDurationMs}ms");
        sb.AppendLine($"- Bundle Hash：`{Esc(compileResult.BundleHash)}`");
        sb.AppendLine($"- 假设项数量：{compileResult.Assumptions.Count}");
        sb.AppendLine();

        // 1.2 规模统计
        var entityCount = entityFields.Fields.Select(f => f.EntityName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var fieldCount = entityFields.Fields.Count;
        var tableCount = entityFields.TableNames().Count;

        sb.AppendLine("### 1.2 规模统计");
        sb.AppendLine();
        sb.AppendLine("| 维度 | 数量 |");
        sb.AppendLine("|------|------|");
        sb.AppendLine($"| 业务事件 | {model.BusinessEvents.Count} |");
        sb.AppendLine($"| 实体（PreAnalysis） | {model.EntityDrafts.Count} |");
        sb.AppendLine($"| 实体（投影后） | {entityCount} |");
        sb.AppendLine($"| 字段总数 | {fieldCount} |");
        sb.AppendLine($"| 数据表 | {tableCount} |");
        sb.AppendLine($"| 业务规则 | {model.BusinessRules.Count} |");
        sb.AppendLine($"| 状态转换 | {model.StateTransitions.Count} |");
        sb.AppendLine($"| 角色 | {model.RoleMatrix?.Roles.Count ?? 0} |");
        sb.AppendLine();

        // 1.3 DDD 概览
        sb.AppendLine("### 1.3 DDD 增强概览");
        sb.AppendLine();
        sb.AppendLine("| 视角 | 置信度 |");
        sb.AppendLine("|------|--------|");
        sb.AppendLine($"| 领域模型 | {dddProjection.DomainModel.Confidence:P0} |");
        sb.AppendLine($"| 聚合设计 | {dddProjection.AggregateDesign.Confidence:P0} |");
        sb.AppendLine($"| 事件目录 | {dddProjection.EventCatalog.Confidence:P0} |");
        sb.AppendLine($"| CQRS | {dddProjection.Cqrs.Confidence:P0} |");
        sb.AppendLine($"| 集成点 | {dddProjection.Integration.Confidence:P0} |");
        sb.AppendLine($"| **总体置信度** | **{dddProjection.OverallConfidence:P0}** |");
        sb.AppendLine();
    }

    // ──────────────────── §2 业务事件分析 ────────────────────

    private static void RenderSection2BusinessEvents(
        StringBuilder sb, PreAnalysisModel model, SaNineViewCompileResult compileResult)
    {
        sb.AppendLine("## §2 业务事件分析");
        sb.AppendLine();

        var resultMap = compileResult.EventResults
            .ToDictionary(e => e.EventId, StringComparer.OrdinalIgnoreCase);

        foreach (var evt in model.BusinessEvents)
        {
            resultMap.TryGetValue(evt.EventId, out var evtResult);

            sb.AppendLine($"### 2.{evt.Index} {Esc(evt.EventName)}");
            sb.AppendLine();
            sb.AppendLine("| 属性 | 值 |");
            sb.AppendLine("|------|-----|");
            sb.AppendLine($"| EventId | `{Esc(evt.EventId)}` |");
            sb.AppendLine("| 阶段 | MVP |");
            sb.AppendLine($"| 复杂度 | {Esc(evt.ComplexityHint)} |");
            sb.AppendLine($"| 编译步骤数 | {evtResult?.Steps?.Count ?? 0} |");
            if (evtResult?.Error != null)
                sb.AppendLine($"| ⚠️ 编译错误 | {Esc(evtResult.Error)} |");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(evt.Description))
            {
                sb.AppendLine($"**描述：** {Esc(evt.Description)}");
                sb.AppendLine();
            }

            if (evt.DependsOn.Count > 0)
            {
                sb.AppendLine("**依赖事件：** " + string.Join(", ", evt.DependsOn.Select(d => $"`{Esc(d)}`")));
                sb.AppendLine();
            }

            // 关联业务规则
            var relatedRules = model.BusinessRules
                .Where(r => string.Equals(r.ScopeEventId, evt.EventId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (relatedRules.Count > 0)
            {
                sb.AppendLine("**关联业务规则：**");
                sb.AppendLine();
                foreach (var rule in relatedRules)
                {
                    sb.AppendLine($"- `{Esc(rule.RuleId)}`：{Esc(rule.Description)}");
                }
                sb.AppendLine();
            }

            // 关联状态转换
            var relatedTransitions = model.StateTransitions
                .Where(t => string.Equals(t.TriggerEventId, evt.EventId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (relatedTransitions.Count > 0)
            {
                sb.AppendLine("**触发状态转换：**");
                sb.AppendLine();
                foreach (var tx in relatedTransitions)
                {
                    sb.AppendLine($"- `{Esc(tx.Entity)}`：{Esc(tx.From)} → {Esc(tx.To)}");
                }
                sb.AppendLine();
            }

            // 关联假设
            var relatedAssumptions = compileResult.Assumptions
                .Where(a => string.Equals(a.EventId, evt.EventId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (relatedAssumptions.Count > 0)
            {
                sb.AppendLine("**编译假设：**");
                sb.AppendLine();
                foreach (var a in relatedAssumptions)
                {
                    sb.AppendLine($"- [{a.SourceStep}] {Esc(a.Text)}（置信度 {a.Confidence:P0}）");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }
    }

    // ──────────────────── §3 DDD 增强分析 ────────────────────

    private static void RenderSection3DddEnhancement(StringBuilder sb, DddProjectionResult dddProjection)
    {
        sb.AppendLine("## §3 DDD 增强分析");
        sb.AppendLine();

        // 3.1 领域模型
        var dm = dddProjection.DomainModel;
        sb.AppendLine($"### 3.1 领域模型（置信度 {dm.Confidence:P0}）");
        sb.AppendLine();
        if (dm.SubDomains.Count > 0)
        {
            sb.AppendLine("**子领域：**");
            sb.AppendLine();
            foreach (var sd in dm.SubDomains)
            {
                sb.AppendLine($"- {Esc(sd)}");
            }
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(dm.CoreDomain))
        {
            sb.AppendLine($"**核心域：** {Esc(dm.CoreDomain)}");
            sb.AppendLine();
        }

        // 3.2 聚合设计
        var ad = dddProjection.AggregateDesign;
        sb.AppendLine($"### 3.2 聚合设计（置信度 {ad.Confidence:P0}）");
        sb.AppendLine();
        if (ad.RootEntities.Count > 0)
        {
            sb.AppendLine("**聚合根：**");
            sb.AppendLine();
            foreach (var root in ad.RootEntities)
            {
                sb.AppendLine($"- `{Esc(root)}`");
            }
            sb.AppendLine();
        }
        if (ad.Aggregates.Count > 0)
        {
            sb.AppendLine("**聚合详情：**");
            sb.AppendLine();
            sb.AppendLine("| 聚合根 | 实体 |");
            sb.AppendLine("|--------|------|");
            foreach (var (root, members) in ad.Aggregates)
            {
                sb.AppendLine($"| `{Esc(root)}` | {string.Join(", ", members.Select(m => $"`{Esc(m)}`"))} |");
            }
            sb.AppendLine();
        }

        // 3.3 事件目录
        var ec = dddProjection.EventCatalog;
        sb.AppendLine($"### 3.3 事件目录（置信度 {ec.Confidence:P0}）");
        sb.AppendLine();
        if (ec.Events.Count > 0)
        {
            sb.AppendLine("**领域事件：**");
            sb.AppendLine();
            foreach (var e in ec.Events)
            {
                sb.AppendLine($"- `{Esc(e)}`");
            }
            sb.AppendLine();
        }
        if (ec.Dependencies.Count > 0)
        {
            sb.AppendLine("**事件依赖：**");
            sb.AppendLine();
            sb.AppendLine("| From | To |");
            sb.AppendLine("|------|-----|");
            foreach (var (from, to) in ec.Dependencies)
            {
                sb.AppendLine($"| `{Esc(from)}` | `{Esc(to)}` |");
            }
            sb.AppendLine();
        }

        // 3.4 CQRS
        var cq = dddProjection.Cqrs;
        sb.AppendLine($"### 3.4 CQRS 命令查询分离（置信度 {cq.Confidence:P0}）");
        sb.AppendLine();
        if (cq.Commands.Count > 0)
        {
            sb.AppendLine("**命令：**");
            foreach (var cmd in cq.Commands)
            {
                sb.AppendLine($"- `{Esc(cmd)}`");
            }
            sb.AppendLine();
        }
        if (cq.Queries.Count > 0)
        {
            sb.AppendLine("**查询：**");
            foreach (var q in cq.Queries)
            {
                sb.AppendLine($"- `{Esc(q)}`");
            }
            sb.AppendLine();
        }

        // 3.5 集成点
        var integ = dddProjection.Integration;
        sb.AppendLine($"### 3.5 集成点（置信度 {integ.Confidence:P0}）");
        sb.AppendLine();
        if (integ.IntegrationPoints.Count > 0)
        {
            foreach (var ip in integ.IntegrationPoints)
            {
                sb.AppendLine($"- {Esc(ip)}");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("（无集成点）");
            sb.AppendLine();
        }
    }

    // ──────────────────── §4 全局数据模型 ────────────────────

    private static void RenderSection4DataModel(StringBuilder sb, EntityDesignProjection entityFields)
    {
        sb.AppendLine("## §4 全局数据模型");
        sb.AppendLine();

        var entityNames = entityFields.Fields
            .Select(f => f.EntityName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine("### 4.1 实体清单");
        sb.AppendLine();
        sb.AppendLine("| # | 实体名 | 显示名 | 表名 | 字段数 |");
        sb.AppendLine("|---|--------|--------|------|--------|");

        var idx = 0;
        foreach (var entityName in entityNames)
        {
            idx++;
            var fields = entityFields.ForEntity(entityName);
            var first = fields.FirstOrDefault();
            sb.AppendLine($"| {idx} | `{Esc(entityName)}` | {Esc(first?.EntityDisplayName ?? "—")} | `{Esc(first?.TableName ?? "—")}` | {fields.Count} |");
        }
        sb.AppendLine();

        // 详细字段表
        foreach (var entityName in entityNames)
        {
            var fields = entityFields.ForEntity(entityName);
            var first = fields.FirstOrDefault();

            sb.AppendLine($"### 4.{idx + 1} {Esc(first?.EntityDisplayName ?? entityName)}");
            sb.AppendLine();
            sb.AppendLine($"- 表名：`{Esc(first?.TableName ?? "—")}`");
            if (!string.IsNullOrWhiteSpace(first?.EntityDescription))
                sb.AppendLine($"- 描述：{Esc(first.EntityDescription)}");
            sb.AppendLine();

            sb.AppendLine("| 字段 | 属性名 | DB 列名 | C# 类型 | SQL 类型 | PK | Required | FK | 描述 |");
            sb.AppendLine("|------|--------|---------|---------|----------|----|----------|-----|------|");

            foreach (var f in fields)
            {
                var pk = f.IsPrimaryKey ? "✓" : "";
                var req = f.IsRequired ? "✓" : "";
                var fk = !string.IsNullOrWhiteSpace(f.References)
                    ? $"`{Esc(f.References)}`"
                    : "—";

                sb.AppendLine($"| `{Esc(f.FieldName)}` | `{Esc(f.PropertyName)}` | `{Esc(f.DbColumnName)}` | {Esc(f.CSharpType)} | {Esc(f.SqlType)} | {pk} | {req} | {fk} | {Esc(f.FieldDescription ?? "—")} |");
            }
            sb.AppendLine();
        }

        // §4.2 全局 ER 图（从 References / ReferencesTable 推导）
        sb.AppendLine("### 4.2 全局 ER 关系");
        sb.AppendLine();
        var relations = entityFields.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f.ReferencesTable) || !string.IsNullOrWhiteSpace(f.References))
            .Select(f => (
                From: f.EntityName,
                To: !string.IsNullOrWhiteSpace(f.ReferencesTable) ? f.ReferencesTable! : f.References!,
                Field: f.FieldName))
            .Distinct()
            .ToList();

        if (relations.Count == 0)
        {
            sb.AppendLine("（无外键关系）");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("| 源实体 | 字段 | 目标 |");
            sb.AppendLine("|--------|------|------|");
            foreach (var r in relations)
                sb.AppendLine($"| `{Esc(r.From)}` | `{Esc(r.Field)}` | `{Esc(r.To)}` |");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine("erDiagram");
            foreach (var r in relations)
            {
                var from = SanitizeMermaidId(r.From);
                var to = SanitizeMermaidId(r.To);
                sb.AppendLine($"    {from} ||--o{{ {to} : \"{Esc(r.Field)}\"");
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }

    private static string SanitizeMermaidId(string name)
        => string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '_'));

    // ──────────────────── §5 一致性分析 ────────────────────

    private static void RenderSection5Consistency(StringBuilder sb, IReadOnlyList<ConsistencyFinding> findings)
    {
        sb.AppendLine("## §5 一致性分析");
        sb.AppendLine();

        var criticals = findings.Where(f => f.Severity == "CRITICAL").ToList();
        var warnings = findings.Where(f => f.Severity == "WARNING").ToList();
        var infos = findings.Where(f => f.Severity == "INFO").ToList();

        sb.AppendLine("### 5.1 摘要");
        sb.AppendLine();
        sb.AppendLine("| 严重级别 | 数量 |");
        sb.AppendLine("|----------|------|");
        sb.AppendLine($"| CRITICAL | {criticals.Count} |");
        sb.AppendLine($"| WARNING | {warnings.Count} |");
        sb.AppendLine($"| INFO | {infos.Count} |");
        sb.AppendLine($"| **总计** | **{findings.Count}** |");
        sb.AppendLine();

        if (criticals.Count > 0)
        {
            sb.AppendLine("### 5.2 严重问题（CRITICAL）");
            sb.AppendLine();
            foreach (var f in criticals)
            {
                sb.AppendLine($"- **[{Esc(f.CheckType)}]** {Esc(f.Message)}");
            }
            sb.AppendLine();
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine("### 5.3 警告（WARNING）");
            sb.AppendLine();
            foreach (var f in warnings)
            {
                sb.AppendLine($"- **[{Esc(f.CheckType)}]** {Esc(f.Message)}");
            }
            sb.AppendLine();
        }

        if (infos.Count > 0)
        {
            sb.AppendLine("### 5.4 信息（INFO）");
            sb.AppendLine();
            foreach (var f in infos)
            {
                sb.AppendLine($"- **[{Esc(f.CheckType)}]** {Esc(f.Message)}");
            }
            sb.AppendLine();
        }

        if (findings.Count == 0)
        {
            sb.AppendLine("✅ **所有一致性检查已通过，无发现项。**");
            sb.AppendLine();
        }
    }

    // ──────────────────── §6 质量评估 ────────────────────

    private static void RenderSection6Quality(StringBuilder sb, QualityScore qualityScore)
    {
        sb.AppendLine("## §6 质量评估");
        sb.AppendLine();

        sb.AppendLine("### 6.1 五维度评分");
        sb.AppendLine();
        sb.AppendLine("| 维度 | 权重 | 得分 |");
        sb.AppendLine("|------|------|------|");
        sb.AppendLine($"| 结构完整性 (Structure) | 25% | {qualityScore.StructureScore:F2} |");
        sb.AppendLine($"| 覆盖度 (Coverage) | 25% | {qualityScore.CoverageScore:F2} |");
        sb.AppendLine($"| 一致性 (Consistency) | 20% | {qualityScore.ConsistencyScore:F2} |");
        sb.AppendLine($"| 深度 (Depth) | 15% | {qualityScore.DepthScore:F2} |");
        sb.AppendLine($"| DDD 对齐 (DDD) | 15% | {qualityScore.DddScore:F2} |");
        sb.AppendLine($"| **总计** | **100%** | **{qualityScore.TotalScore:F2}** |");
        sb.AppendLine();

        // 门控判定
        var criticalCount = 0; // 将由上层传入的 finding 统计，这里用 score 自带的判定
        sb.AppendLine("### 6.2 门控判定");
        sb.AppendLine();
        sb.AppendLine("| 判定 | 结果 |");
        sb.AppendLine("|------|------|");
        sb.AppendLine($"| 总分 ≥ 60 | {(qualityScore.TotalScore >= 60 ? "✅ 通过" : "❌ 不通过")} |");
        sb.AppendLine($"| 结构完整度 ≥ 70 | {(qualityScore.StructureScore >= 70 ? "✅ 通过" : "❌ 不通过")} |");
        sb.AppendLine($"| 通过 Gate (0 critical) | {(qualityScore.PassesGate(0) ? "✅ 通过" : "❌ 不通过")} |");
        sb.AppendLine($"| 通过 Gate (≤5 critical) | {(qualityScore.PassesGate(5) ? "✅ 通过" : "❌ 不通过")} |");
        sb.AppendLine();
    }

    // ──────────────────── 轻量增强章节 ────────────────────

    private static void RenderSection7OutOfScope(StringBuilder sb, SaNineViewCompileResult compileResult)
    {
        sb.AppendLine("## 非目标 / Out of Scope");
        sb.AppendLine();

        var boundaryHints = compileResult.Assumptions
            .Where(a => a.Text.Contains("边界", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("不包含", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("暂不", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("外部", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (boundaryHints.Count > 0)
        {
            foreach (var hint in boundaryHints)
                sb.AppendLine($"- {Esc(hint)}");
        }
        else
        {
            sb.AppendLine("- 暂不覆盖三方系统深度改造，仅保留必要对接边界。");
            sb.AppendLine("- 暂不覆盖生产环境数据迁移与历史数据清洗，后续由实施阶段单独评估。");
            sb.AppendLine("- 暂不覆盖移动端、数据大屏等额外端形态，除非后续需求明确纳入。");
        }

        sb.AppendLine();
    }

    private static void RenderSection8FailureCompensation(
        StringBuilder sb,
        PreAnalysisModel model,
        SaNineViewCompileResult compileResult)
    {
        sb.AppendLine("## 失败与补偿");
        sb.AppendLine();

        var exceptionHints = new List<string>();
        exceptionHints.AddRange(compileResult.Assumptions
            .Where(a => a.Text.Contains("异常", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("失败", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("补偿", StringComparison.OrdinalIgnoreCase)
                || a.Text.Contains("重试", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Text));
        exceptionHints.AddRange(model.BusinessRules
            .Where(r => r.Description.Contains("异常", StringComparison.OrdinalIgnoreCase)
                || r.Description.Contains("失败", StringComparison.OrdinalIgnoreCase)
                || r.Description.Contains("补偿", StringComparison.OrdinalIgnoreCase)
                || r.Description.Contains("重试", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Description));

        var items = exceptionHints
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        if (items.Count == 0)
        {
            sb.AppendLine("- 暂未识别明确失败与补偿规则；实施前需补充外部接口失败、审批驳回、并发提交等异常路径。");
        }
        else
        {
            foreach (var item in items)
                sb.AppendLine($"- {Esc(item)}");
        }

        sb.AppendLine();
    }

    private static void RenderSection9AcceptancePoints(StringBuilder sb, PreAnalysisModel model)
    {
        sb.AppendLine("## 验收要点");
        sb.AppendLine();

        if (model.BusinessEvents.Count == 0)
        {
            sb.AppendLine("- 暂无核心业务事件，无法生成 Given/When/Then 验收要点。");
            sb.AppendLine();
            return;
        }

        foreach (var evt in model.BusinessEvents.Take(12))
        {
            var eventName = Esc(evt.EventName);
            sb.AppendLine($"### {eventName}");
            sb.AppendLine();
            sb.AppendLine($"- Given 用户具备执行「{eventName}」的业务权限和必要前置数据。");
            sb.AppendLine($"- When 用户在系统中完成「{eventName}」操作。");
            sb.AppendLine($"- Then 系统记录对应业务数据、状态变化和可审计操作结果。");
            sb.AppendLine();
        }
    }

    // ──────────────────── 附录 ────────────────────

    private static void RenderAppendices(
        StringBuilder sb,
        PreAnalysisModel model,
        SaNineViewCompileResult compileResult,
        IReadOnlyList<ClarificationAnswerAppendix>? clarificationAnswers)
    {
        sb.AppendLine("## 附录");
        sb.AppendLine();

        // 附录 A：状态转换清单
        RenderAppendixA(sb, model);

        // 附录 B：权限矩阵
        RenderAppendixB(sb, model);

        // 附录 C：业务规则清单
        RenderAppendixC(sb, model);

        // 附录 D：编译假设清单
        RenderAppendixD(sb, compileResult);

        // 附录 E：澄清问答作答（31 P0.4 — 有则写，无则空节）
        RenderAppendixE(sb, clarificationAnswers);
    }

    private static void RenderAppendixE(StringBuilder sb, IReadOnlyList<ClarificationAnswerAppendix>? clarificationAnswers)
    {
        sb.AppendLine("### 附录 E — 澄清问答作答");
        sb.AppendLine();

        if (clarificationAnswers == null || clarificationAnswers.Count == 0)
        {
            sb.AppendLine("（本轮无结构化澄清作答记录）");
            sb.AppendLine();
            return;
        }

        foreach (var item in clarificationAnswers.OrderBy(x => x.Round).ThenBy(x => x.Stage, StringComparer.Ordinal))
        {
            sb.AppendLine($"#### 第 {item.Round} 轮（`{Esc(item.Stage)}`）");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(item.AnswersText) ? "（空）" : item.AnswersText.Trim());
            sb.AppendLine();
        }
    }

    private static void RenderAppendixA(StringBuilder sb, PreAnalysisModel model)
    {
        sb.AppendLine("### 附录 A — 状态转换清单");
        sb.AppendLine();

        if (model.StateTransitions.Count == 0)
        {
            sb.AppendLine("（无状态转换）");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| 实体 | 源状态 | 目标状态 | 触发事件 |");
        sb.AppendLine("|------|--------|----------|----------|");

        foreach (var tx in model.StateTransitions)
        {
            sb.AppendLine($"| `{Esc(tx.Entity)}` | {Esc(tx.From)} | {Esc(tx.To)} | `{Esc(tx.TriggerEventId ?? "—")}` |");
        }
        sb.AppendLine();
    }

    private static void RenderAppendixB(StringBuilder sb, PreAnalysisModel model)
    {
        sb.AppendLine("### 附录 B — 权限矩阵");
        sb.AppendLine();

        var rm = model.RoleMatrix;
        if (rm == null || rm.Roles.Count == 0)
        {
            sb.AppendLine("（无权限矩阵）");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"**角色：** {string.Join(", ", rm.Roles.Select(r => $"`{Esc(r)}`"))}");
        sb.AppendLine();

        sb.AppendLine("| 事件 | " + string.Join(" | ", rm.Roles.Select(r => Esc(r))) + " |");
        sb.AppendLine("|------|" + string.Join("|", rm.Roles.Select(_ => "------")) + "|");

        foreach (var (eventId, roleOps) in rm.Matrix)
        {
            var cells = rm.Roles.Select(role =>
                roleOps.TryGetValue(role, out var ops) ? string.Join(", ", ops) : "—");
            sb.AppendLine($"| `{Esc(eventId)}` | {string.Join(" | ", cells)} |");
        }
        sb.AppendLine();
    }

    private static void RenderAppendixC(StringBuilder sb, PreAnalysisModel model)
    {
        sb.AppendLine("### 附录 C — 业务规则清单");
        sb.AppendLine();

        if (model.BusinessRules.Count == 0)
        {
            sb.AppendLine("（无业务规则）");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| RuleId | 作用事件 | 描述 |");
        sb.AppendLine("|--------|----------|------|");

        foreach (var rule in model.BusinessRules)
        {
            sb.AppendLine($"| `{Esc(rule.RuleId)}` | `{Esc(rule.ScopeEventId ?? "全局")}` | {Esc(rule.Description)} |");
        }
        sb.AppendLine();
    }

    private static void RenderAppendixD(StringBuilder sb, SaNineViewCompileResult compileResult)
    {
        sb.AppendLine("### 附录 D — 编译假设清单");
        sb.AppendLine();

        if (compileResult.Assumptions.Count == 0)
        {
            sb.AppendLine("（无编译假设）");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| 事件 | 来源步骤 | 假设内容 | 置信度 |");
        sb.AppendLine("|------|----------|----------|--------|");

        foreach (var a in compileResult.Assumptions)
        {
            sb.AppendLine($"| `{Esc(a.EventId)}` | {Esc(a.SourceStep)} | {Esc(a.Text)} | {a.Confidence:P0} |");
        }
        sb.AppendLine();
    }

    private static void RenderConfirmCta(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("请你确认需求分析说明书，如果同意，推进到下一工作阶段，如果不满意，请在输入框继续提出你的问题和要求。");
        sb.AppendLine();
    }

    // ──────────────────── 工具方法 ────────────────────

    /// <summary>转义 Markdown 表格中的特殊字符。</summary>
    private static string Esc(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "—";
        return text
            .Replace("|", "\\|")
            .Replace("\r", "")
            .Replace("\n", " ");
    }
}
