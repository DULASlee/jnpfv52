using System.Text.Json;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Testing;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B03 — tester-skill + TestSuiteGenerated + IR3_TestSuite 投影。
/// </summary>
public static class IrPhase4TesterTests
{
    public static async Task RunAllAsync()
    {
        TestDevelopmentSkillIds_TesterDefined();
        TestCaseDeriver_FieldOnly_MinThree();
        TestCaseDeriver_FieldAndStateMachine_MinFive();
        await TestTesterSkill_LeaveSimpleAsync();
        await TestTesterSkill_LeaveWithFlowAsync();
        await TestProjection_TestSuiteGeneratedAsync();
        Console.WriteLine("[Phase4] Tester skill tests passed.");
    }

    private static void TestDevelopmentSkillIds_TesterDefined()
    {
        if (DevelopmentSkillIds.Tester != "tester-skill")
            throw new InvalidOperationException("DevelopmentSkillIds.Tester mismatch");

        if (IrFragmentTypes.TestSuite != "IR3_TestSuite")
            throw new InvalidOperationException("IrFragmentTypes.TestSuite mismatch");
    }

    private static void TestCaseDeriver_FieldOnly_MinThree()
    {
        var fields = new[]
        {
            new TesterConfirmedField { Name = "Reason", Type = "string", Required = true },
            new TesterConfirmedField { Name = "Days", Type = "int", Required = true },
        };

        var cases = TestCaseDeriver.DeriveAll("field-only", fields, Array.Empty<TesterStateTransition>(), Array.Empty<TesterStateNode>());
        if (cases.Count < TestCaseDeriver.MinFieldOnly)
            throw new InvalidOperationException($"field-only cases {cases.Count} < {TestCaseDeriver.MinFieldOnly}");
    }

    private static void TestCaseDeriver_FieldAndStateMachine_MinFive()
    {
        var fields = new[]
        {
            new TesterConfirmedField { Name = "Reason", Type = "string", Required = true },
        };
        var transitions = new[]
        {
            new TesterStateTransition { From = "Draft", To = "Submitted", Event = "Submit" },
            new TesterStateTransition { From = "Submitted", To = "Approved", Event = "Approve" },
        };
        var states = new[]
        {
            new TesterStateNode { StateId = "Draft", IsTerminal = false },
            new TesterStateNode { StateId = "Approved", IsTerminal = true },
        };

        var cases = TestCaseDeriver.DeriveAll("field-and-state-machine", fields, transitions, states);
        if (cases.Count < TestCaseDeriver.MinFieldAndStateMachine)
            throw new InvalidOperationException($"field+sm cases {cases.Count} < {TestCaseDeriver.MinFieldAndStateMachine}");
    }

    private static async Task TestTesterSkill_LeaveSimpleAsync()
    {
        var skill = CreateTesterSkill();
        var snapshot = BuildSnapshotFromSample("leave-simple.json", includeSystemDesign: false);
        snapshot = AddStableCodegen(snapshot, "leave-simple-t8");

        var fail = await skill.ValidateInputAsync(new IrSnapshot
        {
            Fragments = snapshot.Fragments.Where(f => f.FragmentType != IrFragmentTypes.GeneratedCode).ToList(),
        });
        if (fail.IsValid)
            throw new InvalidOperationException("ValidateInput should require stable IR3_GeneratedCode");

        var ok = await skill.ValidateInputAsync(snapshot);
        if (!ok.IsValid)
            throw new InvalidOperationException(ok.ErrorMessage ?? "ValidateInput failed");

        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = "_phase4-tester",
            ProjectId = "leave-simple-t8",
            PipelineId = 9010,
            UserRequirement = "请假 MVP tester",
            Snapshot = snapshot,
            ArchGuardWarnings = new[]
            {
                new SkillArchWarning { RuleId = "AG-002", Message = "warning sample", FilePath = "Services/X.cs" },
            },
        };

        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(context))
            events.Add(evt);

        var output = await skill.ValidateOutputAsync(events);
        if (!output.IsValid)
            throw new InvalidOperationException(output.ErrorMessage ?? "ValidateOutput failed");

        if (events[0].EventType != IrEventTypes.TestSuiteGenerated)
            throw new InvalidOperationException("First event should be TestSuiteGenerated");

        using var doc = JsonDocument.Parse(events[0].Payload);
        if (doc.RootElement.GetProperty("derivationMode").GetString() != "field-only")
            throw new InvalidOperationException("leave-simple should use field-only mode");

        var count = doc.RootElement.GetProperty("scenarioCount").GetInt32();
        if (count < TestCaseDeriver.MinFieldOnly)
            throw new InvalidOperationException($"scenarioCount {count} too low");

        if (!doc.RootElement.TryGetProperty("metadata", out var meta)
            || !meta.TryGetProperty("archGuardWarnings", out var warnings)
            || warnings.GetArrayLength() != 1)
        {
            throw new InvalidOperationException("metadata.archGuardWarnings not forwarded");
        }
    }

    private static async Task TestTesterSkill_LeaveWithFlowAsync()
    {
        var skill = CreateTesterSkill();
        var snapshot = BuildSnapshotFromSample("leave-with-flow.json", includeSystemDesign: true);
        snapshot = AddStableCodegen(snapshot, "leave-with-flow-t9");

        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = "_phase4-tester-flow",
            ProjectId = "leave-with-flow-t9",
            PipelineId = 9012,
            UserRequirement = "请假审批 tester",
            Snapshot = snapshot,
        };

        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(context))
            events.Add(evt);

        var output = await skill.ValidateOutputAsync(events);
        if (!output.IsValid)
            throw new InvalidOperationException(output.ErrorMessage ?? "leave-with-flow ValidateOutput failed");

        using var doc = JsonDocument.Parse(events[0].Payload);
        if (doc.RootElement.GetProperty("derivationMode").GetString() != "field-and-state-machine")
            throw new InvalidOperationException("leave-with-flow should use field-and-state-machine mode");

        var count = doc.RootElement.GetProperty("scenarioCount").GetInt32();
        if (count < TestCaseDeriver.MinFieldAndStateMachine)
            throw new InvalidOperationException($"leave-with-flow scenarioCount {count} < {TestCaseDeriver.MinFieldAndStateMachine}");
    }

    private static async Task TestProjection_TestSuiteGeneratedAsync()
    {
        using var db = CreateSqliteClient();
        var projection = new IrProjectionEngine(db);
        const string tenantId = "_phase4-tester-proj";
        const string projectId = "9011";
        const string fragmentId = $"testsuite:{projectId}";

        var fields = new[]
        {
            new TesterConfirmedField { Name = "Reason", Type = "string", Required = true },
            new TesterConfirmedField { Name = "Days", Type = "int", Required = true },
        };
        var cases = TestCaseDeriver.DeriveAll("field-only", fields, Array.Empty<TesterStateTransition>(), Array.Empty<TesterStateNode>());
        var input = new TesterBuildResult
        {
            DerivationMode = "field-only",
            ConfirmedFields = fields,
            Transitions = Array.Empty<TesterStateTransition>(),
            States = Array.Empty<TesterStateNode>(),
            ArchGuardWarnings = Array.Empty<TesterArchWarning>(),
        };
        var payload = TestSuiteManifestBuilder.BuildTestSuiteGeneratedPayload(projectId, "run-1", input, cases);

        await ProjectAsync(db, projection, tenantId, projectId, 1, IrEventTypes.TestSuiteGenerated, fragmentId, 1, payload);

        var snap = await db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.FragmentId == fragmentId)
            .FirstAsync();

        if (snap.FragmentType != IrFragmentTypes.TestSuite)
            throw new InvalidOperationException($"Expected IR3_TestSuite, got {snap.FragmentType}");

        if (snap.StabilityState != IrStabilityStates.Stable)
            throw new InvalidOperationException($"Expected stable TestSuite, got {snap.StabilityState}");
    }

    private static IrSnapshot AddStableCodegen(IrSnapshot snapshot, string projectId)
    {
        var list = snapshot.Fragments.ToList();
        list.Add(new IrSnapshotFragment
        {
            FragmentId = $"codegen:{projectId}",
            FragmentType = IrFragmentTypes.GeneratedCode,
            StabilityState = IrStabilityStates.Stable,
            Payload = $$"""{"id":"codegen:{{projectId}}","stabilityState":"stable"}""",
        });
        return new IrSnapshot { Fragments = list };
    }

    private static IrSnapshot BuildSnapshotFromSample(string fileName, bool includeSystemDesign)
    {
        var samplesDir = TemplateRenderSamplesTests.ResolveSamplesDirPublic();
        var json = File.ReadAllText(Path.Combine(samplesDir, fileName));
        using var doc = JsonDocument.Parse(json);
        var fragments = new List<IrSnapshotFragment>();
        foreach (var f in doc.RootElement.GetProperty("fragments").EnumerateArray())
        {
            var fragmentType = f.GetProperty("fragmentType").GetString() ?? string.Empty;
            if (!includeSystemDesign && fragmentType == IrFragmentTypes.SystemDesign)
                continue;

            var payloadEl = f.GetProperty("payload");
            var payload = payloadEl.ValueKind == JsonValueKind.String
                ? payloadEl.GetString() ?? "{}"
                : payloadEl.GetRawText();

            fragments.Add(new IrSnapshotFragment
            {
                FragmentId = f.GetProperty("fragmentId").GetString() ?? string.Empty,
                FragmentType = fragmentType,
                StabilityState = f.TryGetProperty("stabilityState", out var st)
                    ? st.GetString() ?? IrStabilityStates.Stable
                    : IrStabilityStates.Stable,
                Payload = payload,
            });
        }

        return new IrSnapshot { Fragments = fragments };
    }

    private static TesterSkillService CreateTesterSkill()
    {
        using var loggerFactory = LoggerFactory.Create(static _ => { });
        return new TesterSkillService(loggerFactory.CreateLogger<TesterSkillService>());
    }

    private static async Task ProjectAsync(
        SqlSugarClient db,
        IrProjectionEngine projection,
        string tenantId,
        string projectId,
        int sequence,
        string eventType,
        string fragmentId,
        int fragmentVersion,
        string payload)
    {
        var id = Guid.NewGuid().ToString("N");
        const string pipelineId = "pipe-tester";
        await db.Ado.ExecuteCommandAsync(
            "INSERT INTO ai_ir_events (F_Id,F_ProjectId,F_TenantId,F_PIPELINE_ID,F_EventType,F_FragmentId,F_FragmentType,F_FragmentVersion,F_Payload,F_SAStepName,F_Sequence,F_CreatedAt,F_IsRollback) VALUES (@id,@pid,@tid,@pplid,@etype,@fid,@ftype,@fver,@payload,NULL,@seq,@at,0)",
            new[]
            {
                new SugarParameter("@id", id),
                new SugarParameter("@pid", projectId),
                new SugarParameter("@tid", tenantId),
                new SugarParameter("@pplid", pipelineId),
                new SugarParameter("@etype", eventType),
                new SugarParameter("@fid", fragmentId),
                new SugarParameter("@ftype", IrFragmentTypes.TestSuite),
                new SugarParameter("@fver", fragmentVersion),
                new SugarParameter("@payload", payload),
                new SugarParameter("@seq", sequence),
                new SugarParameter("@at", DateTime.UtcNow.ToString("o")),
            });

        await projection.ProjectEventAsync(new AiIrEventEntity
        {
            Id = id,
            ProjectId = projectId,
            TenantId = tenantId,
            PipelineId = pipelineId,
            EventType = eventType,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.TestSuite,
            FragmentVersion = fragmentVersion,
            Payload = payload,
            Sequence = sequence,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static SqlSugarClient CreateSqliteClient()
    {
        var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:",
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        });
        client.Open();
        client.Ado.ExecuteCommand("""
            CREATE TABLE ai_ir_events (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_PIPELINE_ID TEXT NOT NULL DEFAULT '',
                F_EventType TEXT NOT NULL,
                F_FragmentType TEXT,
                F_FragmentId TEXT,
                F_FragmentVersion INTEGER NOT NULL DEFAULT 1,
                F_Payload TEXT NOT NULL,
                F_SkillId TEXT,
                F_SAStepName TEXT,
                F_Sequence INTEGER NOT NULL,
                F_CreatedAt TEXT NOT NULL,
                F_IsRollback INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE ai_ir_fragment_snapshots (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_PIPELINE_ID TEXT NOT NULL DEFAULT '',
                F_FragmentId TEXT NOT NULL,
                F_FragmentType TEXT NOT NULL,
                F_CurrentVersion INTEGER NOT NULL,
                F_StabilityState TEXT NOT NULL DEFAULT 'draft',
                F_IrContent TEXT NOT NULL,
                F_SAStepsCompleted TEXT,
                F_LastEventId TEXT NOT NULL,
                F_UpdatedAt TEXT NOT NULL,
                F_DeleteMark INTEGER NOT NULL DEFAULT 0
            );
            """);
        return client;
    }
}
