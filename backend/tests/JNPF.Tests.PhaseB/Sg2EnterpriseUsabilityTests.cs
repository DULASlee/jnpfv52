using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 30 号纠偏 · SG2-E1/E2：02 表头非空 + DDD 低置信度待确认（企业可用，禁止假绿）。
/// </summary>
public class Sg2EnterpriseUsabilityTests
{
    private const string MinimalSkeletonJson = """
        {
          "businessEvents": [
            { "eventId": "EV-001", "eventName": "提交申请", "complexityHint": "简单", "dependsOn": [] }
          ],
          "entityDrafts": [
            {
              "entityName": "Request",
              "tableName": "BIZ_REQUEST",
              "fields": [
                { "name": "id", "type": "String", "required": true },
                { "name": "status", "type": "String", "required": true }
              ]
            }
          ],
          "businessRules": [],
          "stateTransitions": []
        }
        """;

    [Fact]
    public void ResolveIdentity_FillsEmptySystemNameAndSummary_FromRequirementText()
    {
        var model = new PreAnalysisModel
        {
            SystemName = null,
            RequirementSummary = null,
            BusinessEvents = Array.Empty<PreAnalysisBusinessEvent>(),
        };

        var resolved = model.ResolveIdentity(
            pipelineTitle: "积分商城",
            requirementText: "建设《积分商城》系统，支持积分兑换与库存扣减。");

        Assert.False(string.IsNullOrWhiteSpace(resolved.SystemName));
        Assert.DoesNotContain("—", resolved.SystemName);
        Assert.False(string.IsNullOrWhiteSpace(resolved.RequirementSummary));
        Assert.DoesNotContain("—", resolved.RequirementSummary!);
        Assert.Contains("积分", resolved.SystemName);
    }

    [Fact]
    public void ResolveIdentity_NeverReturnsDashPlaceholder()
    {
        var model = new PreAnalysisModel();
        var resolved = model.ResolveIdentity(null, null);

        Assert.Equal("业务", resolved.SystemName);
        Assert.StartsWith("（待补充）", resolved.RequirementSummary);
        Assert.DoesNotContain("—", resolved.SystemName);
        Assert.DoesNotContain("—", resolved.RequirementSummary!);
    }

    [Fact]
    public void CompileFromSkeletonJson_ResolvesIdentity_IntoSource()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var result = compiler.CompileFromSkeletonJson(
            MinimalSkeletonJson,
            requirementSummary: "建设员工请假审批系统，支持多级审批。",
            pipelineTitle: "请假审批");

        Assert.False(string.IsNullOrWhiteSpace(result.Source.SystemName));
        Assert.NotEqual("—", result.Source.SystemName);
        Assert.False(string.IsNullOrWhiteSpace(result.Source.RequirementSummary));
        Assert.NotEqual("—", result.Source.RequirementSummary);
    }

    [Fact]
    public void DddProjection_EmptyIntegration_ProducesPendingConfirmations()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var compile = compiler.CompileFromSkeletonJson(MinimalSkeletonJson, "内部业务系统");
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var ddd = new DddProjection(NullLogger<DddProjection>.Instance).Project(compile, fields);

        Assert.True(ddd.Integration.Confidence < 0.5);
        Assert.NotEmpty(ddd.PendingConfirmations);
        Assert.False(ddd.HasUnguardedLowConfidence);
        Assert.Contains(ddd.PendingConfirmations, p => p.Contains("外部", StringComparison.Ordinal));
    }

    [Fact]
    public void DddProjection_HasUnguardedLowConfidence_WhenPendingMissing()
    {
        var unguarded = new DddProjectionResult
        {
            Integration = new DddIntegration { Confidence = 0.3 },
            PendingConfirmations = Array.Empty<string>(),
        };

        Assert.True(unguarded.HasUnguardedLowConfidence);
        Assert.Contains("集成点", unguarded.CollectLowConfidenceViews());
    }

    [Fact]
    public void RequirementDocumentRenderer_CoverHasNoDash_AndRendersPendingSection()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var compile = compiler.CompileFromSkeletonJson(
            MinimalSkeletonJson,
            requirementSummary: "建设《仓储盘点》系统。",
            pipelineTitle: "仓储盘点");
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };
        var ddd = new DddProjection(NullLogger<DddProjection>.Instance).Project(compile, fields);
        var quality = new QualityScore
        {
            StructureScore = 80,
            CoverageScore = 70,
            ConsistencyScore = 70,
            DepthScore = 60,
            DddScore = 50,
        };

        var md = new RequirementDocumentRenderer(NullLogger<RequirementDocumentRenderer>.Instance)
            .Render(
                new PipelineTriple("t1", "p1", 343),
                compile,
                ddd,
                fields,
                Array.Empty<ConsistencyFinding>(),
                quality,
                roundNumber: 3);

        Assert.DoesNotContain("| 项目名称 | — |", md);
        Assert.DoesNotContain("| 需求概要 | — |", md);
        Assert.Contains("待确认事项", md);
        Assert.Contains(compile.Source.SystemName!, md);
        Assert.Contains("请你确认需求分析说明书，如果同意，推进到下一工作阶段，如果不满意，请在输入框继续提出你的问题和要求。", md);
    }

    [Fact]
    public void ResolveIdentity_PrefersRequirementChineseName_OverE2eSlug()
    {
        var model = new PreAnalysisModel();
        var resolved = model.ResolveIdentity(
            "longchain-leave-ot-1783722400089",
            "员工请假与加班管理系统。角色：员工、部门主管。");
        Assert.Contains("请假", resolved.SystemName);
        Assert.DoesNotContain("longchain", resolved.SystemName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveIdentity_PrefersPipelineTitle_WhenNoRequirementText()
    {
        var model = new PreAnalysisModel();
        var resolved = model.ResolveIdentity("积分商城系统", null);
        Assert.Contains("积分", resolved.SystemName);
        Assert.DoesNotContain("—", resolved.SystemName);
    }

    [Fact]
    public void ResolveIdentity_IgnoresSkeletonJsonAsRequirementText()
    {
        var model = new PreAnalysisModel();
        var resolved = model.ResolveIdentity(
            "仓储盘点",
            """{"businessEvents":[{"eventId":"EV-001"}]}""");
        Assert.Contains("仓储", resolved.SystemName);
        Assert.DoesNotContain("{", resolved.RequirementSummary!);
    }
}
