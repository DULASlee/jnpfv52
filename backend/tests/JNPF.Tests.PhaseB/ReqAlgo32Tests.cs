using System.Text.Json;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

public class ReqAlgo32Tests
{
    [Fact]
    public void AmendPatch_AddField_IsDeterministicAndIdempotent()
    {
        var skeleton = """
            {"systemName":"请假系统","requirementSummary":"员工请假审批","businessEvents":[{"eventId":"EV-001","eventName":"提交请假"}],"entityDrafts":[{"entityName":"LeaveRequest","displayName":"请假单","fields":[{"name":"id","type":"string","required":true,"primaryKey":true}]}]}
            """;
        var patch = new AmendmentPatch(
            AmendmentPatchOperation.AddField,
            "LeaveRequest",
            "leaveReason",
            Type: "string",
            Required: true);

        var once = AmendmentPatchApplier.ApplyToSkeletonJson(skeleton, new[] { patch });
        var twice = AmendmentPatchApplier.ApplyToSkeletonJson(once, new[] { patch });

        Assert.Equal(once, twice);
        Assert.Contains("leaveReason", once);
        Assert.Contains("\"required\":true", once);
    }

    [Fact]
    public void SlotGain_KnownSlot_IsNotSelectedAgain()
    {
        var result = BuildCompileResult();

        var slots = SlotInformationGainSelector.SelectTopSlots(
            result,
            "请假类型 已确认：年假、病假、事假",
            take: 3);

        Assert.DoesNotContain(slots, s => s.SlotId == "leave.types");
        Assert.Contains(slots, s => s.SlotId == "leave.approval-levels");
    }

    [Fact]
    public void SlotGain_KeywordOnlyAnswer_MarksSlotFilled()
    {
        var result = BuildCompileResult();
        // 用户常只勾选「年假、病假」而不复述题干「请假类型」
        const string answers = "年假、病假、事假";

        var filled = ClarificationAnswerPatchMapper.DetectFilledSlots(answers);
        var slots = SlotInformationGainSelector.SelectTopSlots(result, answers, take: 5);

        Assert.Contains("leave.types", filled);
        Assert.DoesNotContain(slots, s => s.SlotId == "leave.types");
    }

    [Fact]
    public void ClarificationAnswer_BuildsTypedPatches_AndWritesSkeleton()
    {
        var skeleton = """
            {"systemName":"请假系统","requirementSummary":"员工请假","businessEvents":[{"eventId":"EV-001","eventName":"提交请假"}],"entityDrafts":[{"entityName":"LeaveRequest","displayName":"请假单","fields":[{"name":"id","type":"string","required":true,"primaryKey":true}]}],"businessRules":[],"stateTransitions":[]}
            """;
        var answers = "[slot:leave.types] - 请假类型：年假、病假\n[slot:leave.approval-levels] - 审批层级：两级";
        var filled = ClarificationAnswerPatchMapper.DetectFilledSlots(answers, new[] { "leave.types", "leave.approval-levels" });
        var patches = ClarificationAnswerPatchMapper.BuildPatches(answers, filled, "员工请假");

        Assert.Contains(filled, id => id == "leave.types");
        Assert.Contains(patches, p => p.Operation == AmendmentPatchOperation.AddField && p.Name == "leaveType");
        Assert.Contains(patches, p => p.Operation == AmendmentPatchOperation.AddStateTransition);
        Assert.Contains(patches, p =>
            p.Operation == AmendmentPatchOperation.PatchSummary
            && (p.Description?.Contains("澄清确认", StringComparison.Ordinal) ?? false));

        var patched = AmendmentPatchApplier.ApplyToSkeletonJson(skeleton, patches);
        Assert.Contains("leaveType", patched);
        Assert.Contains("PendingApproval", patched);
        Assert.Contains("requirementSummary", patched);
    }

    [Fact]
    public void AmendmentPatch_Merge_PrefersPrimaryThenFillsGaps()
    {
        var primary = new[]
        {
            new AmendmentPatch(AmendmentPatchOperation.AddField, "LeaveRequest", "leaveType", Type: "string"),
        };
        var secondary = new[]
        {
            new AmendmentPatch(AmendmentPatchOperation.AddField, "LeaveRequest", "leaveType", Type: "string"),
            new AmendmentPatch(AmendmentPatchOperation.PatchRule, "RULE-X", "规则X", Description: "d"),
        };

        var merged = AmendmentPatchApplier.MergePatches(primary, secondary);

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, p => p.Name == "leaveType");
        Assert.Contains(merged, p => p.Target == "RULE-X");
    }

    [Fact]
    public void AmendmentPatch_ParsePatches_ReadsTypedOperations()
    {
        using var doc = JsonDocument.Parse("""
            {"summaryMarkdown":"ok","patches":[{"operation":"AddEvent","target":"EV-009","name":"撤回申请","description":"发起人撤回"}]}
            """);
        var patches = AmendmentPatchApplier.ParsePatches(doc.RootElement);
        Assert.Single(patches);
        Assert.Equal(AmendmentPatchOperation.AddEvent, patches[0].Operation);
        Assert.Equal("EV-009", patches[0].Target);
    }

    [Fact]
    public void ReqAlgo_ConflictGraph_FindsMissingRelationTarget()
    {
        var result = BuildCompileResult(new[]
        {
            new PreAnalysisRelation
            {
                FromField = "employeeId",
                ToEntity = "Employee",
                ToField = "id",
            },
        });

        var gaps = RequirementConflictGraph.FindGaps(result);

        Assert.Contains(gaps, g => g.Source == "graph" && g.Code == "relation.missing-entity");
    }

    [Fact]
    public void ReqAlgo_LowConfidence_CapsPmScore()
    {
        var result = BuildCompileResult(assumptionConfidence: 0.4m);

        var score = RequirementConfidencePolicy.ApplyPmScoreCap(96, result);

        Assert.Equal(84, score);
    }

    [Fact]
    public void ReqSignal_RenderPromptBlock_IncludesSeedIdsAndTags()
    {
        var block = RequirementEvolutionContext.RenderPromptBlock(new[]
        {
            new RequirementEvolutionSeed(1001, "req-amend-success", "[auto_seed:req=x] tenant=t project=p pipeline=1 请假审批补充成功"),
        });

        Assert.Contains("seedId=1001", block);
        Assert.Contains("req-amend-success", block);
    }

    private static SaNineViewCompileResult BuildCompileResult(
        IReadOnlyList<PreAnalysisRelation>? relations = null,
        decimal assumptionConfidence = 0.7m)
    {
        var source = new PreAnalysisModel
        {
            SystemName = "请假审批系统",
            RequirementSummary = "员工提交请假，主管审批。",
            BusinessEvents = new[]
            {
                new PreAnalysisBusinessEvent { EventId = "EV-001", EventName = "提交请假" },
                new PreAnalysisBusinessEvent { EventId = "EV-002", EventName = "审批请假" },
            },
            EntityDrafts = new[]
            {
                new PreAnalysisEntityDraft
                {
                    EntityName = "LeaveRequest",
                    DisplayName = "请假单",
                    Fields = new[]
                    {
                        new PreAnalysisFieldDraft { Name = "id", Type = "string", IsPrimaryKey = true },
                    },
                    Relations = relations ?? Array.Empty<PreAnalysisRelation>(),
                },
            },
            BusinessRules = Array.Empty<PreAnalysisBusinessRule>(),
            StateTransitions = Array.Empty<PreAnalysisStateTransition>(),
        };

        return new SaNineViewCompileResult
        {
            Source = source,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = source.BusinessEvents.Select(e => new SaEventResult
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Complexity = "simple",
                Steps = new Dictionary<string, object?>(),
            }).ToList(),
            CompileDurationMs = 1,
            BundleHash = "test",
            Assumptions = new List<Assumption>
            {
                new("EV-001", "Compiler", "审批层级按部门主管推导", assumptionConfidence),
            },
        };
    }
}
