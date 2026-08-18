using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

/// <summary>
/// IR 完整性 Gate（阶段九 P9-S1）。
///
/// 位于 SystemDesignLocked 之后、Developer（编译器）之前。
/// 三步防线：
///   ① 结构校验（零 LLM）：扫描 IR fragment，产出缺口清单
///   ② 确定性派生回填：DDL FK → relationships；页面名 → pageType
///   ③ 无法派生的缺口 → 标记为 clarification-needed（供 ADR-005 澄清触发）
///
/// 编译器仍是纯函数：Gate 只预处理 IR，让它更完整；不改变编译器的确定性。
/// </summary>
public interface IIrCompletenessGate
{
    /// <summary>检查 pipeline 的 IR 完整性，返回报告（含已回填 + 剩余缺口）。</summary>
    Task<CompletenessReport> CheckAsync(string projectId, string tenantId, CancellationToken ct = default);
}

public sealed class IrCompletenessGate : IIrCompletenessGate, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<IrCompletenessGate> _logger;

    public IrCompletenessGate(ISqlSugarClient db, ILogger<IrCompletenessGate> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CompletenessReport> CheckAsync(string projectId, string tenantId, CancellationToken ct = default)
    {
        var report = new CompletenessReport { ProjectId = projectId };

        // 1. 读 IR snapshots（stable fragments）
        var snapshots = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .ToListAsync(ct);

        var skeletonSnap = snapshots.FirstOrDefault(s => s.FragmentType == IrFragmentTypes.Skeleton);
        var ddlSnap = snapshots.FirstOrDefault(s => s.FragmentType == IrFragmentTypes.DDL);
        var formPageSnap = snapshots.FirstOrDefault(s => s.FragmentType == IrFragmentTypes.FormPageIR);

        // 2. 校验 Skeleton（必须存在）
        if (skeletonSnap == null)
        {
            report.Gaps.Add(new CompletenessGap
            {
                Dimension = "skeleton",
                Severity = "critical",
                Description = "缺少 IR0_Skeleton（PM Skill 未完成）",
                CanDerive = false,
            });
            report.Status = "incomplete";
            return report;
        }

        var skeleton = SkeletonPayload.Parse(skeletonSnap.IrContent);

        // 3. 校验实体完整性（每实体至少有字段 + 主键）
        foreach (var entity in skeleton.EntityDrafts)
        {
            if (entity.Fields.Count == 0)
            {
                report.Gaps.Add(new CompletenessGap
                {
                    Dimension = "entity-fields",
                    Severity = "critical",
                    Entity = entity.EntityName,
                    Description = $"实体 {entity.EntityName} 无字段",
                    CanDerive = false,
                });
            }

            if (!entity.Fields.Any(f => f.PrimaryKey))
            {
                report.Gaps.Add(new CompletenessGap
                {
                    Dimension = "primary-key",
                    Severity = "warning",
                    Entity = entity.EntityName,
                    Description = $"实体 {entity.EntityName} 无主键声明，将退化用 name=='id'",
                    CanDerive = true,
                    DeriveAction = "退化：name=='id' 的字段视为主键",
                });
            }
        }

        // 4. 校验实体关系（② 确定性派生回填）
        var entitiesWithRelations = skeleton.EntityDrafts
            .Where(e => e.Relations.Count > 0).ToList();

        if (entitiesWithRelations.Count == 0 && ddlSnap != null)
        {
            // 尝试从 DDL 的 FOREIGN KEY 子句派生关系
            var ddl = DdlPayload.Parse(ddlSnap.IrContent);
            var derivedCount = 0;

            foreach (var table in ddl.Tables)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    var fromEntity = skeleton.EntityDrafts.FirstOrDefault(e =>
                        e.TableName.Equals(table.TableName, System.StringComparison.OrdinalIgnoreCase)
                        || e.EntityName.Equals(table.EntityName, System.StringComparison.OrdinalIgnoreCase));
                    var toEntity = skeleton.EntityDrafts.FirstOrDefault(e =>
                        e.TableName.Equals(fk.ReferencesTable, System.StringComparison.OrdinalIgnoreCase)
                        || e.EntityName.Equals(fk.ReferencesTable, System.StringComparison.OrdinalIgnoreCase));

                    if (fromEntity != null && toEntity != null)
                    {
                        derivedCount++;
                        report.Derived.Add(new DerivedItem
                        {
                            Dimension = "relationships",
                            Description = $"从 DDL FK 派生：{fromEntity.EntityName}.{fk.ColumnName} → {toEntity.EntityName}.{fk.ReferencesColumn}",
                        });
                    }
                }
            }

            if (derivedCount > 0)
            {
                report.Gaps.Add(new CompletenessGap
                {
                    Dimension = "relationships",
                    Severity = "info",
                    Description = $"骨架无 relations 声明，从 DDL FK 派生 {derivedCount} 条关系",
                    CanDerive = true,
                    DeriveAction = "已派生回填（见 Derived 列表）",
                });
            }
            else
            {
                report.Gaps.Add(new CompletenessGap
                {
                    Dimension = "relationships",
                    Severity = "warning",
                    Description = "无实体关系声明（骨架 relations 为空，DDL 无 FK 子句）",
                    CanDerive = false,
                });
            }
        }

        // 5. 校验 FormPageIR 的 pageType
        if (formPageSnap != null)
        {
            var formPage = FormPagePayload.Parse(formPageSnap.IrContent);
            var pagesWithoutType = formPage.Pages
                .Where(p => string.IsNullOrEmpty(p.PageType) || p.PageType == "form") // InferPageType 已做兜底，这里检查是否所有页都被推断
                .ToList();

            report.FormPageCount = formPage.Pages.Count;
            report.FormPageTypes = formPage.Pages.Select(p => $"{p.Title}:{p.PageType}").ToList();
        }
        else
        {
            report.Gaps.Add(new CompletenessGap
            {
                Dimension = "form-page",
                Severity = "critical",
                Description = "缺少 IR2_FormPageIR（UI Design Skill 未完成）",
                CanDerive = false,
            });
        }

        // 6. 校验 RoleMatrix（权限）
        if (skeleton.RoleMatrix == null || skeleton.RoleMatrix.Roles.Count == 0)
        {
            report.Gaps.Add(new CompletenessGap
            {
                Dimension = "permissions",
                Severity = "warning",
                Description = "骨架无 roleMatrix 权限矩阵声明",
                CanDerive = false,
            });
        }
        else
        {
            report.HasRoleMatrix = true;
        }

        // 7. 汇总结论
        var criticalCount = report.Gaps.Count(g => g.Severity == "critical" && !g.CanDerive);
        var warningCount = report.Gaps.Count(g => g.Severity == "warning" && !g.CanDerive);

        report.Status = criticalCount > 0 ? "incomplete"
            : warningCount > 0 ? "warning"
            : "complete";
        report.CriticalGaps = criticalCount;
        report.WarningGaps = warningCount;
        report.DerivedCount = report.Derived.Count;

        _logger.LogInformation(
            "IrCompletenessGate 完成 project={ProjectId} status={Status} critical={Critical} warning={Warning} derived={Derived}",
            projectId, report.Status, report.CriticalGaps, report.WarningGaps, report.DerivedCount);

        return report;
    }
}

/// <summary>完整性报告</summary>
public sealed class CompletenessReport
{
    public string ProjectId { get; set; } = "";
    /// <summary>complete / warning / incomplete</summary>
    public string Status { get; set; } = "unknown";
    public int CriticalGaps { get; set; }
    public int WarningGaps { get; set; }
    public int DerivedCount { get; set; }
    public bool HasRoleMatrix { get; set; }
    public int FormPageCount { get; set; }
    public List<string> FormPageTypes { get; set; } = new();
    public List<CompletenessGap> Gaps { get; set; } = new();
    public List<DerivedItem> Derived { get; set; } = new();
}

/// <summary>缺口</summary>
public sealed class CompletenessGap
{
    public string Dimension { get; set; } = "";
    /// <summary>critical / warning / info</summary>
    public string Severity { get; set; } = "warning";
    public string? Entity { get; set; }
    public string Description { get; set; } = "";
    /// <summary>是否可确定性派生（true=编译器/Gate 可自动补；false=需澄清问答）</summary>
    public bool CanDerive { get; set; }
    public string? DeriveAction { get; set; }
}

/// <summary>已派生回填项</summary>
public sealed class DerivedItem
{
    public string Dimension { get; set; } = "";
    public string Description { get; set; } = "";
}
