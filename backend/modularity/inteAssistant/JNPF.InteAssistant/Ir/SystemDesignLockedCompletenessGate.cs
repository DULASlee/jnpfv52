using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Ir;

public interface ISystemDesignLockedCompletenessGate
{
    /// <summary>
    /// Validates IR completeness before Developer Skill compilation.
    /// Checks fragment presence, Skeleton↔DDL coherence, and cross-layer consistency
    /// between Skeleton entities and the EntityDesignProjection (CQRS Read Model).
    /// </summary>
    /// <param name="triple">
    /// 三元组（P2-1 修复 2026-07-10）：传入真实 (tenantId,projectId,pipelineId) 供 EntityDesignProjector 投影。
    /// 传 null 时回退占位三元组 ("gate","gate","gate")——仅结构映射，不涉及租户隔离查询。
    /// 跨层一致性规则只比较实体/字段名称，不涉及三元组值。
    /// </param>
    Task<SkillValidationResult> ValidateAsync(IrSnapshot snapshot, PipelineTriple? triple = null, CancellationToken ct = default);
}

/// <summary>
/// SystemDesignLocked 前置完整性门禁（P3-R03 / 阶段四 developer-skill 激活条件）。
///
/// P9-S5 升级：增加跨层一致性校验（R1-R3），确保 Skeleton ↔ EntityDesignProjection 互为镜像。
///
/// 注意：本门禁在 DeveloperSkill 激活前被调用，此时尚未获得完整三元组 (tenantId,projectId,pipelineId)。
/// EntityDesignProjector 使用占位三元组 ("gate","gate","gate") 仅用于结构映射（不涉及租户隔离查询）。
/// 跨层一致性规则只比较实体/字段名称，不涉及三元组值。
/// </summary>
public sealed class SystemDesignLockedCompletenessGate : ISystemDesignLockedCompletenessGate, ITransient
{
    private readonly ILogger<SystemDesignLockedCompletenessGate> _logger;

    public SystemDesignLockedCompletenessGate(ILogger<SystemDesignLockedCompletenessGate> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SkillValidationResult> ValidateAsync(IrSnapshot snapshot, PipelineTriple? triple = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // ── 结构完整性（P3-R03 基线）─────────────────────────────────────────
        // Analyst Round 3 Finalize（传入真实 triple）时尚无 DDL/FormPageIR：
        // 仅要求 Skeleton stable + R1-R3；Developer 激活前（triple=null）仍要求完整设计片段。
        var analysisStage = triple != null;

        if (snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable) == null)
        {
            _logger.LogWarning("R0 校验失败: Skeleton 片段未 stable");
            return Task.FromResult(SkillValidationResult.Fail("Skeleton 片段未 stable"));
        }

        if (!analysisStage)
        {
            if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) == null)
            {
                _logger.LogWarning("R0 校验失败: DDL 片段未 stable");
                return Task.FromResult(SkillValidationResult.Fail("DDL 片段未 stable"));
            }

            if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) == null)
            {
                _logger.LogWarning("R0 校验失败: FormPageIR 片段未 stable");
                return Task.FromResult(SkillValidationResult.Fail("FormPageIR 片段未 stable"));
            }
        }
        else
        {
            _logger.LogInformation(
                "Analyst Round3 门禁：跳过 DDL/FormPageIR 要求（设计阶段尚未开始），仅校验 Skeleton + R1-R3");
        }

        // ── P9-S5 跨层一致性（R1-R3）────────────────────────────────────────
        var skeletonSnap = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Skeleton);

        if (skeletonSnap == null)
        {
            _logger.LogDebug("跳过跨层校验，Skeleton 片段不存在");
            return Task.FromResult(SkillValidationResult.Ok());
        }

        // P2-1 修复（2026-07-10）：优先用调用方传入的真实三元组，回退占位三元组保持兼容。
        var projectionOptions = triple != null
            ? new EntityDesignProjectionOptions
            {
                TenantId = triple.TenantId,
                ProjectId = triple.ProjectId,
                PipelineId = triple.PipelineId.ToString(),
            }
            : new EntityDesignProjectionOptions
            {
                TenantId = "gate",
                ProjectId = "gate",
                PipelineId = "gate",
            };

        if (triple == null)
        {
            _logger.LogWarning(
                "SystemDesignLockedCompletenessGate 未接收三元组，回退占位 (gate/gate/gate)——建议调用方传入真实三元组");
        }

        var projection = EntityDesignProjector.Project(snapshot, projectionOptions);

        if (projection.Fields.Count > 0)
        {
            SkeletonPayload skeleton;
            try
            {
                skeleton = SkeletonPayload.Parse(skeletonSnap.Payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skeleton payload 解析失败，无法执行跨层校验");
                return Task.FromResult(SkillValidationResult.Fail("Skeleton payload 解析失败，无法执行跨层校验"));
            }

            var violations = new List<string>();

            // R1: Skeleton 每个 entity 在投影中有对应行
            var projectionEntityNames = new HashSet<string>(
                projection.Fields.Select(f => f.EntityName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var entity in skeleton.EntityDrafts)
            {
                if (!string.IsNullOrWhiteSpace(entity.EntityName)
                    && !projectionEntityNames.Contains(entity.EntityName))
                {
                    violations.Add($"R1: Skeleton 实体 '{entity.EntityName}' 在 EntityDesignProjection 中无对应行");
                }
            }

            // R2: 投影每行能映射回 Skeleton 字段
            var skeletonEntityMap = skeleton.EntityDrafts
                .Where(e => !string.IsNullOrWhiteSpace(e.EntityName))
                .ToDictionary(
                    e => e.EntityName,
                    e => new HashSet<string>(
                        e.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Select(f => f.Name),
                        StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var field in projection.Fields)
            {
                // DDL 兜底合成字段（Skeleton 无 fields 时）豁免 R2：DDL 是合法字段源
                if (field.Source == FieldSource.DdlFallback)
                    continue;

                if (skeletonEntityMap.TryGetValue(field.EntityName, out var skeletonFields))
                {
                    if (!skeletonFields.Contains(field.FieldName))
                    {
                        violations.Add(
                            $"R2: 投影字段 '{field.EntityName}.{field.FieldName}' 在 Skeleton 中无对应字段");
                    }
                }
            }

            // R3: Skeleton references 在投影中有对应 FK
            foreach (var entity in skeleton.EntityDrafts)
            {
                foreach (var field in entity.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.References))
                        continue;

                    var hasFk = projection.Fields.Any(f =>
                        string.Equals(f.EntityName, entity.EntityName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.FieldName, field.Name, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(f.References));

                    if (!hasFk)
                    {
                        violations.Add(
                            $"R3: Skeleton '{entity.EntityName}.{field.Name}' 声明 references='{field.References}'，但投影中 FK 缺失");
                    }
                }
            }

            if (violations.Count > 0)
            {
                var sb = new StringBuilder("跨层一致性校验失败：\n");
                foreach (var v in violations)
                    sb.Append("  • ").AppendLine(v);
                var failMsg = sb.ToString();
                _logger.LogWarning("跨层校验失败 violationCount={Count}: {Message}", violations.Count, failMsg.TrimEnd());
                return Task.FromResult(SkillValidationResult.Fail(failMsg));
            }
        }

        _logger.LogDebug("SystemDesignLockedCompletenessGate 校验通过");
        return Task.FromResult(SkillValidationResult.Ok());
    }
}
